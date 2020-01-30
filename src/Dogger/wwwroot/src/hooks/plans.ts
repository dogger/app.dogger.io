import { PlanResponse } from "../api/openapi";
import { apiClient } from "../api/Client";
import { createGlobalResource } from '@fluffy-spoon/react-globalize';

export const demoPlanAccessor = createGlobalResource(async () => 
    await apiClient.apiPlansDemoGet());

export const plansAccessor = createGlobalResource(async () => 
    await apiClient.apiPlansGet());

export function getCheapestPlan(plans: PlanResponse[]) {
    const plansByPrice = getPlansByPrice(plans);
    return plansByPrice && plansByPrice[0];
}

export function getMostExpensivePlan(plans: PlanResponse[]) {
    const plansByPrice = getPlansByPrice(plans);
    return plansByPrice && plansByPrice[plansByPrice.length - 1];
}

export function getPlansByPrice(plans: PlanResponse[]) {
    return plans
        ?.slice()
        .sort((a, b) => 
            a.priceInHundreds - b.priceInHundreds);
}