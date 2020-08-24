FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build-dotnet
WORKDIR /src
COPY ["src/Dogger/Dogger.csproj", "Dogger/"]
RUN dotnet tool restore
RUN dotnet restore "Dogger/Dogger.csproj"
COPY ./src .
WORKDIR "/src/Dogger"
RUN dotnet build "Dogger.csproj" -c Release -o /app/build

FROM build-dotnet AS publish-dotnet
RUN dotnet publish "Dogger.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish-dotnet /app/publish .
ENTRYPOINT ["dotnet", "Dogger.dll"]