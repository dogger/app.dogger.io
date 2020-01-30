import React from "react"
import { useAuth0 } from "./Auth0Provider";
import { usePath } from "../hooks/path";

export default ({ component: Component, location, ...rest }) => {
  const { loading, isAuthenticated } = useAuth0();
  const pathname = usePath();

  if (loading || isAuthenticated)
    return null;
    
  if(pathname.indexOf(location.pathname) !== 0)
    return null;

  return <Component {...rest} />
}