#!/bin/bash
dotnet test --filter TestCategory=Unit --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

cp $(find ./TestResults -name "*.opencover.xml" | head -1) ./TestResults