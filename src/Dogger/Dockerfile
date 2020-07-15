FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build-dotnet
WORKDIR /src
COPY ["Dogger/Dogger.csproj", "Dogger/"]
RUN dotnet tool restore
RUN dotnet restore "Dogger/Dogger.csproj"
COPY . .
WORKDIR "/src/Dogger"
RUN dotnet build "Dogger.csproj" -c Release -o /app/build

FROM build-dotnet AS publish-dotnet
RUN dotnet publish "Dogger.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish-dotnet /app/publish .
COPY --from=build-dotnet /src/docker-compose.deploy.yml .
ENTRYPOINT ["dotnet", "Dogger.dll"]