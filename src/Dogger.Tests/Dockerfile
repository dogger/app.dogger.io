FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["Dogger.Tests/Dogger.Tests.csproj", "Dogger.Tests/"]
COPY ["Dogger/Dogger.csproj", "Dogger/"]
RUN dotnet restore "Dogger.Tests/Dogger.Tests.csproj"
COPY . .
WORKDIR "/src/Dogger.Tests"
ENTRYPOINT ["sh", "/src/Dogger.Tests/docker-entrypoint.sh"]
#ENTRYPOINT ["dotnet", "test"]