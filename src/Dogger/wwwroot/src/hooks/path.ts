import { useState, useEffect } from "react";

export const usePath = () => {
    const [path, setPath] = useState(typeof window !== "undefined" && window.location.pathname);
    const listenToPopstate = () => {
      const winPath = typeof window !== "undefined" && window.location.pathname;
      setPath(winPath);
    };
    useEffect(() => {
      typeof window !== "undefined" && window.addEventListener("popstate", listenToPopstate);
      return () => {
        typeof window !== "undefined" && window.removeEventListener("popstate", listenToPopstate);
      };
    }, []);
    return path;
  };