#!/bin/bash
dotnet test --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=json,cobertura,lcov,teamcity,opencover

pwd
echo "finding files"
find ./src/Dogger.Tests/TestResults -name "*.opencover.xml"