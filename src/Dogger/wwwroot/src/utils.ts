import { useEffect } from "react";

export async function delay(milliseconds: number) {
    await new Promise(resolve => setTimeout(resolve, milliseconds));
}

type ValidationCode = 
    "ALREADY_PROVISIONED";

export interface ValidationErrorDetails {
    type: ValidationCode;
    detail: string;
}

export async function handleValidationErrors<T>(action: () => Promise<T>, handlers: { [code: string]: () => Promise<void>|void }) {
    try {
        await action();
    } catch(e) {
        if(e instanceof Response && e.status === 400) {
            var validationError = await e.json() as ValidationErrorDetails;
            if(validationError.type in handlers) {
                await Promise.resolve(handlers[validationError.type]());
            } else {
                console.error("An unknown validation error occured: " + validationError.detail);
            }
        } else {
            throw e;
        }
    }
}

export function useAsyncEffect(effect: () => Promise<void>, deps?: ReadonlyArray<any>): void {
    /* eslint-disable react-hooks/exhaustive-deps */
    useEffect(() => {
        effect();
    }, deps);
}

export function trackGoal(goalName: string) {
    if(typeof window === "undefined")
        return;

    window["plausible"] = window["plausible"] || function() { (window["plausible"].q = window["plausible"].q || []).push(arguments) };
    window["plausible"](goalName);
}