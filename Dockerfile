FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY MyDiscordBot.csproj ./
RUN dotnet restore MyDiscordBot.csproj

COPY . .
RUN dotnet publish MyDiscordBot.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
USER $APP_UID

ENTRYPOINT ["dotnet", "MyDiscordBot.dll"]