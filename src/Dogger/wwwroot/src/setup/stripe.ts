import {loadStripe} from '@stripe/stripe-js';

const providedKey = process.env.GATSBY_STRIPE_PUBLISHABLE_KEY || process.env.REACT_APP_STRIPE_PUBLISHABLE_KEY;
if(!providedKey) {
    console.warn("No provided Stripe key was found! Will fall back to the test key.");
}

const STRIPE_API_KEY = providedKey || "pk_test_wlSYPpZrd6AXgjaQtQRt2NAu";

export const stripePromise = loadStripe(STRIPE_API_KEY);