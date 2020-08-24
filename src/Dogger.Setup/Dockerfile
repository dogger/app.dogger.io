FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build-dotnet
WORKDIR /src
COPY ["Dogger.Setup/Dogger.Setup.csproj", "Dogger.Setup/"]
RUN dotnet tool restore
RUN dotnet restore "Dogger.Setup/Dogger.Setup.csproj"
COPY ./src .
WORKDIR "/src/Dogger.Setup"
RUN dotnet build "Dogger.Setup.csproj" -c Release -o /app/build

FROM build-dotnet AS publish-dotnet
RUN dotnet publish "Dogger.Setup.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish-dotnet /app/publish .
ENTRYPOINT ["dotnet", "Dogger.Setup.dll"]