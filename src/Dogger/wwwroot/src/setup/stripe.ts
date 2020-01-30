import {loadStripe} from '@stripe/stripe-js';

const STRIPE_API_KEY = process.env.GATSBY_STRIPE_PUBLISHABLE_KEY || "pk_test_wlSYPpZrd6AXgjaQtQRt2NAu";

export const stripePromise = loadStripe(STRIPE_API_KEY);