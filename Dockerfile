FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["PlayCityDiscordBot/PlayCityDiscordBot.csproj", "PlayCityDiscordBot/"]
RUN dotnet restore "PlayCityDiscordBot/PlayCityDiscordBot.csproj"
COPY . .
WORKDIR "/src/PlayCityDiscordBot"
RUN dotnet build "PlayCityDiscordBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PlayCityDiscordBot.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PlayCityDiscordBot.dll"]
