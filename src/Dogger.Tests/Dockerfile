FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["src/Dogger.Tests/Dogger.Tests.csproj", "Dogger.Tests/"]
COPY ["src/Dogger/Dogger.csproj", "Dogger/"]
RUN dotnet restore "Dogger.Tests/Dogger.Tests.csproj"
COPY ./src .
WORKDIR "/src/Dogger.Tests"
ENTRYPOINT ["sh", "/src/Dogger.Tests/docker-entrypoint.sh"]