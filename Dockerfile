FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore Bot.Main/Bot.Main.csproj
RUN dotnet publish Bot.Main/Bot.Main.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .
RUN mkdir -p /app/logs

ENTRYPOINT ["dotnet", "Bot.Main.dll"]
