# Build context: solution root (the host references EventSignup.Client).
#   docker build -f EventSignup/Dockerfile -t eventsignup-web .

# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only project files first so `dotnet restore` is cached until dependencies change.
COPY EventSignup/EventSignup.csproj EventSignup/
COPY EventSignup.Client/EventSignup.Client.csproj EventSignup.Client/
RUN dotnet restore EventSignup/EventSignup.csproj

COPY EventSignup/ EventSignup/
COPY EventSignup.Client/ EventSignup.Client/
# No --no-restore here: with it, the publish output is missing _framework/blazor.web.js.
RUN dotnet publish EventSignup/EventSignup.csproj -c Release -o /app/publish

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
# Non-root user provided by the official .NET images.
USER $APP_UID
ENTRYPOINT ["dotnet", "EventSignup.dll"]
