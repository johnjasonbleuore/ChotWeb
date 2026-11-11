# Builds and runs ChotWeb in a container (used by Render: New > Web Service > Docker).

# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ChotWeb/ChotWeb.csproj ChotWeb/
RUN dotnet restore ChotWeb/ChotWeb.csproj
COPY ChotWeb/ ChotWeb/
RUN dotnet publish ChotWeb/ChotWeb.csproj -c Release -o /app/publish --no-restore

# ---- run ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

# Site content, uploads and sign-in keys are stored here.
# On Render, attach a persistent disk with mount path /var/data, or they are wiped on every deploy/restart.
ENV Storage__DataPath=/var/data
RUN mkdir -p /var/data

# Render's proxy handles HTTPS; trust its X-Forwarded-For / X-Forwarded-Proto headers
# so sign-in rate limiting sees each visitor's real IP and the app knows the request was HTTPS.
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

# Listen on the port Render provides in $PORT (8080 when run elsewhere).
EXPOSE 8080
CMD ["sh", "-c", "ASPNETCORE_HTTP_PORTS=${PORT:-8080} exec dotnet ChotWeb.dll"]
