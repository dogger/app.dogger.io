export function getWindow(): Window {
    if(typeof window === "undefined")
        return null;

    return window;
}

export function localStorageSet(key: string, value: string) {
    if(!getWindow())
        return;

    localStorage.setItem(key, value);
}

export function localStorageGet(key: string) {
    if(!getWindow())
        return;

    return localStorage.getItem(key);
}