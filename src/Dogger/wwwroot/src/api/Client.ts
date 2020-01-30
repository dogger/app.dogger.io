import { GeneralApi, Configuration, ConfigurationParameters, FetchAPI } from "./openapi";
import { auth0Container } from "../auth/Auth0Provider";

async function isAuthenticated() {
    return auth0Container.client && await auth0Container.client.isAuthenticated();
}

class DoggerConfigurationParameters implements ConfigurationParameters {
    get basePath() {
        if(typeof window === "undefined")
            return "";
        
        return "//" + window.location.host;
    }

    get fetchApi(): FetchAPI {
        return async (input: RequestInfo, init: RequestInit) => {
            if(await isAuthenticated()) {
                const token = await auth0Container.client.getTokenSilently();

                init.headers = {
                    ...(init.headers || {}),
                    "Authorization": `Bearer ${token}`
                };
            }

            if(init.method === "GET") {
                init.mode = "same-origin";
                init.cache = "default";

                while(true) {
                    try {
                        return await fetch(input, init);
                    } catch(ex) {
                        console.error(ex);
                        await new Promise(resolve => setTimeout(resolve, 1000));
                    }
                }
            } else {
                return await fetch(input, init);
            }
        };
    }
}

export const apiClient = new GeneralApi(new Configuration(new DoggerConfigurationParameters()));

export function withAuthenticatedApiClient<T>(action: (signal: AbortSignal) => Promise<T>)
{
    return async (signal: AbortSignal) => {
        if(signal.aborted || !await isAuthenticated())
            throw new Error('Not authenticated yet.');

        return await action(signal);
    };
}