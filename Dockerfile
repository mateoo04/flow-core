FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Install Node.js for Tailwind build invoked by the FlowCore.csproj target.
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl gnupg \
    && mkdir -p /etc/apt/keyrings \
    && curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key \
       | gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg \
    && echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_20.x nodistro main" \
       > /etc/apt/sources.list.d/nodesource.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends nodejs \
    && rm -rf /var/lib/apt/lists/*

# Restore JavaScript dependencies first for better layer caching.
COPY package.json package-lock.json ./
RUN npm ci

# Restore .NET dependencies.
COPY FlowCore.sln ./
COPY FlowCore/FlowCore.csproj FlowCore/
COPY FlowCore.Tests/FlowCore.Tests.csproj FlowCore.Tests/
RUN dotnet restore FlowCore.sln

# Copy remaining source and publish.
COPY . .
RUN dotnet publish FlowCore/FlowCore.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Railway provides PORT; default to 8080 for local container runs.
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} dotnet FlowCore.dll"]
