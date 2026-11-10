# Build context: solution root (the API references EventSignup.Data).
#   docker build -f EventSignup.Api/Dockerfile -t eventsignup-api .

# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only project files first so `dotnet restore` is cached until dependencies change.
COPY EventSignup.Api/EventSignup.Api.csproj EventSignup.Api/
COPY EventSignup.Data/EventSignup.Data.csproj EventSignup.Data/
RUN dotnet restore EventSignup.Api/EventSignup.Api.csproj

COPY EventSignup.Api/ EventSignup.Api/
COPY EventSignup.Data/ EventSignup.Data/
RUN dotnet publish EventSignup.Api/EventSignup.Api.csproj -c Release -o /app/publish --no-restore

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
# Non-root user provided by the official .NET images.
USER $APP_UID
ENTRYPOINT ["dotnet", "EventSignup.Api.dll"]
