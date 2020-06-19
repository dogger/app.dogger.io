import { navigate } from "@reach/router";

export const onRedirectCallback = async appState => {
  navigate(
    appState && appState.targetUrl
      ? appState.targetUrl
      : (typeof window !== "undefined" && window.location.pathname)
  );
};
  
export const auth0Config = {
    domain: 'dogger.eu.auth0.com',
    clientId: 'lgtxnFKrO2Z4dGAXBOhw29qfkESBZrQB',
    audience: 'https://dogger.io/api'
};