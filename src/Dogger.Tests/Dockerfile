FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /
COPY ["src/Dogger.Tests/Dogger.Tests.csproj", "src/Dogger.Tests/"]
COPY ["src/Dogger/Dogger.csproj", "src/Dogger/"]
RUN dotnet restore "src/Dogger.Tests/Dogger.Tests.csproj"
COPY . .
WORKDIR "/src/Dogger.Tests"
ENTRYPOINT ["sh", "/src/Dogger.Tests/docker-entrypoint.sh"]