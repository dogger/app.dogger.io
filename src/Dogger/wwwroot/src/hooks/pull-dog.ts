import { apiClient, withAuthenticatedApiClient } from "../api/Client";
import { createGlobalResource } from '@fluffy-spoon/react-globalize';

export const pullDogSettingsAccessor = createGlobalResource(
    withAuthenticatedApiClient(async () => 
        await apiClient.apiPullDogSettingsGet()));