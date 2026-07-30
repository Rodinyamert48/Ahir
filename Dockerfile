FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/Ahir.Core/Ahir.Core.csproj src/Ahir.Core/
COPY src/Ahir.Database/Ahir.Database.csproj src/Ahir.Database/
COPY src/Ahir.Security/Ahir.Security.csproj src/Ahir.Security/
COPY src/Ahir.Storage/Ahir.Storage.csproj src/Ahir.Storage/
COPY src/Ahir.Realtime/Ahir.Realtime.csproj src/Ahir.Realtime/
COPY src/Ahir.Plugin/Ahir.Plugin.csproj src/Ahir.Plugin/
COPY src/Ahir.Server/Ahir.Server.csproj src/Ahir.Server/
COPY src/Ahir.CLI/Ahir.CLI.csproj src/Ahir.CLI/
RUN dotnet restore src/Ahir.CLI/Ahir.CLI.csproj

COPY . .
RUN dotnet publish src/Ahir.CLI/Ahir.CLI.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080 8443 9090 3000
VOLUME ["/data", "/backups", "/logs", "/plugins"]
ENTRYPOINT ["dotnet", "Ahir.CLI.dll", "start"]
