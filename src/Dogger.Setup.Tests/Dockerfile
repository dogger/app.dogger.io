FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["src/Dogger.Setup.Tests/Dogger.Setup.Tests.csproj", "Dogger.Setup.Tests/"]
COPY ["src/Dogger.Setup/Dogger.Setup.csproj", "Dogger.Setup/"]
RUN dotnet restore "Dogger.Setup.Tests/Dogger.Setup.Tests.csproj"
COPY . .
WORKDIR "/src/Dogger.Setup.Tests"
ENTRYPOINT ["sh", "/src/Dogger.Setup.Tests/docker-entrypoint.sh"]