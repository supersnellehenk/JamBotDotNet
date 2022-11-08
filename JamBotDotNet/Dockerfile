FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["JamBotDotNet/JamBotDotNet.csproj", "JamBotDotNet/"]
RUN dotnet restore "JamBotDotNet/JamBotDotNet.csproj"
COPY . .
WORKDIR "/src/JamBotDotNet"
RUN dotnet build "JamBotDotNet.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "JamBotDotNet.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JamBotDotNet.dll"]
