# EventSignup

A small app for browsing events and signing up for them. You can't sign up for an event that's full or
already over, and you can only sign up once per email. There's also a search box where you can type what
you're looking for in normal language. An LLM turns that into a filter and the database does the actual
search. If the LLM isn't available, it falls back to a simple keyword search.

Built with .NET 10, ASP.NET Core Web API, EF Core, SQL Server and Blazor WebAssembly. The LLM is
`openai/gpt-oss-120b` on Groq.

The UI and the sample data are in Slovenian, the code is in English.

## Running it

### With Docker

You need Docker Desktop. A Groq API key is optional, you can get one for free at
https://console.groq.com/keys.

```bash
cp .env.example .env
# optional: put your key in GROQ_API_KEY
docker compose up --build
```

The app runs on http://localhost:5200 and the API on http://localhost:5100/events.

The database is created and filled with 11 sample events on the first start. Their dates are relative to
today, and one of them is full, one almost full and one already over, so all the rules can be tried out.
Without a key everything still works, the search just uses keywords.

To reset the database, run `docker compose down -v`. You also need that if you change
`MSSQL_SA_PASSWORD` in `.env`, because SQL Server only reads it when the volume is created.

### Without Docker

You need the .NET 10 SDK and SQL Server LocalDB, which comes with Visual Studio.

```bash
# optional, for the LLM search
dotnet user-secrets set "Llm:ApiKey" "<your-key>" --project EventSignup.Api

dotnet run --project EventSignup.Api --launch-profile http   # API on http://localhost:5100
dotnet run --project EventSignup --launch-profile http       # UI on http://localhost:5215
```

In Visual Studio you can set both `EventSignup.Api` and `EventSignup` as startup projects with the `http`
profile. The API creates the database in `(localdb)\MSSQLLocalDB` when it starts.

Docker and the local setup both use port 5100, so run only one of them at a time.

### Trying the API and looking at the database

`EventSignup.Api/EventSignup.Api.http` has requests for all endpoints and error cases. You can run them
in Visual Studio, or in VS Code with the REST Client extension.

For the Docker database, connect to `localhost,14333` with user `sa` and the password from `.env`
(`EventSignup_Dev_2026!` by default), and tick "Trust server certificate". The local one is in
Visual Studio's SQL Server Object Explorer under `(localdb)\MSSQLLocalDB`.

## Where to look

Paths are in `EventSignup.Api/` unless they say otherwise.

- Events: `Controllers/EventsController.cs` and `Services/EventService.cs`
- Registrations: `Services/RegistrationService.cs` has the rules, `Controllers/RegistrationsController.cs`
  turns them into status codes
- Search: `Search/EventSearchService.cs` and `Search/LlmQueryParser.cs`, which has the prompt
- Database, migrations and seed data: `EventSignup.Data/`
- UI: `EventSignup.Client/Pages/`

## Decisions

I went with Blazor WebAssembly and the rest of the .NET stack (ASP.NET Core, EF Core, SQL Server) because
I've worked with it before and the whole app is in one language.

For the LLM I picked Groq because it has a free tier and is fast, which matters since the search waits for
the answer. It uses the OpenAI API format, so switching to another provider only means changing
`Llm:BaseUrl` and `Llm:Model` in the config.

Everything runs in Docker Compose so the setup is one command. The database is created and seeded on
startup, and the app works without an LLM key.

The events list has month tabs instead of a from–to date picker. Two date fields looked cluttered for
something people mostly use to see what's on this month, and the search can handle specific dates.

## Cost limiting

Every search can mean an LLM call, so I tried to limit both how often it gets called and how much each
call costs. The limits are in `appsettings.json` under `Llm`.

- Each IP can search 10 times per minute, after that the API returns 429 without calling the LLM.
- The result for a query is cached for 10 minutes, so the same search doesn't call the LLM again.
- If the LLM fails, the search falls back to keywords instead of retrying.
- The model only gets the query and the instructions, not the events, so a call doesn't get more
  expensive as the database grows.
- The query is limited to 200 characters and the answer to 1000 tokens, and the request times out after
  10 seconds.

In production I'd also set a spending limit on the Groq account.

## Prompt injection

The model doesn't have access to anything worth attacking. It never sees events, registrations or emails and can't call any
tools.

From its answer I only read the dates, the location and the keywords. Each value is checked and anything
else is thrown away, and the answer is never shown to the user as text. The filter then goes into a
normal parameterized EF Core query.

So the worst thing an injection can do is make the search return the wrong events for that one user.

## With more time

- Tests. Mainly for the registration rules and the checks on the LLM answer, plus a set of search queries
  to test the prompt against.
- Better search matching. Right now a keyword has to appear in the event literally, so a query that
  describes an event in different words won't find it. Full-text search or embeddings would help.
- A smarter fallback search. Without the LLM, any event that contains one of the query's words is shown,
  so the results are pretty broad. Ranking events by how many words they match would already help.
- Time zones. All dates are counted in Ljubljana time, so an event in another time zone can end up on the
  wrong day. I'd store a time zone for each event.
- A from–to date filter in the UI. The API already supports it.
- Cancelling a registration, email confirmation and a way to add events.

## Use of AI

I used Claude Code while building this, mostly to discuss design options, write code that I then went
through and changed, look things up and test edge cases.
