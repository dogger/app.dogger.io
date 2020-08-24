FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /
COPY ["src/Dogger.Setup.Tests/Dogger.Setup.Tests.csproj", "src/Dogger.Setup.Tests/"]
COPY ["src/Dogger.Setup/Dogger.Setup.csproj", "src/Dogger.Setup/"]
RUN dotnet restore "src/Dogger.Setup.Tests/Dogger.Setup.Tests.csproj"
COPY . .
WORKDIR "/src/Dogger.Setup.Tests"
ENTRYPOINT ["sh", "/src/Dogger.Setup.Tests/docker-entrypoint.sh"]