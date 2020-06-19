#!/bin/bash
dotnet test --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

cp $(find ./TestResults -name "*.opencover.xml" | head -1) ./TestResults