#!/bin/bash
cd src/Dogger.Tests
dotnet test --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
[ $? -eq 0 ] || exit 1

cp $(find ./TestResults -name "*.opencover.xml" | head -1) ./TestResults/Dogger