import { useState, useEffect } from "react";

export const usePath = () => {
    const [path, setPath] = useState(window && window.location.pathname);
    const listenToPopstate = () => {
      const winPath = window && window.location.pathname;
      setPath(winPath);
    };
    useEffect(() => {
      window && window.addEventListener("popstate", listenToPopstate);
      return () => {
        window && window.removeEventListener("popstate", listenToPopstate);
      };
    }, []);
    return path;
  };