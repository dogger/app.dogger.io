FROM mcr.microsoft.com/dotnet/sdk:5.0-buster AS build
WORKDIR /
COPY ["src/Dogger.Tests/Dogger.Tests.csproj", "src/Dogger.Tests/"]
COPY ["src/Dogger/Dogger.csproj", "src/Dogger/"]
RUN dotnet restore "src/Dogger.Tests/Dogger.Tests.csproj"
COPY . .
ENTRYPOINT ["sh", "/src/Dogger.Tests/docker-entrypoint.sh"]