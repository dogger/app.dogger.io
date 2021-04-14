FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster AS build-dotnet
WORKDIR /
COPY ["src/Dogger/Dogger.csproj", "src/Dogger/"]
RUN dotnet tool restore
RUN dotnet restore "src/Dogger/Dogger.csproj"
COPY . .
RUN dotnet build "src/Dogger/Dogger.csproj" -c Release -o /app/build

FROM build-dotnet AS publish-dotnet
RUN dotnet publish "src/Dogger/Dogger.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish-dotnet /app/publish .
ENTRYPOINT ["dotnet", "Dogger.dll"]