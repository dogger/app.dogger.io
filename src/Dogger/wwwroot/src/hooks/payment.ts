/* eslint-disable react-hooks/exhaustive-deps */
import { apiClient, withAuthenticatedApiClient } from "../api/Client";
import HttpStatus from 'http-status-codes';
import { useState, useMemo } from "react";
import { useAuth0 } from "../auth/Auth0Provider";
import { createGlobalResource, useGlobalResource } from '@fluffy-spoon/react-globalize';

const paymentMethodAccessor = createGlobalResource(
    withAuthenticatedApiClient(async () => {
        const paymentMethodResponse = await apiClient.apiPaymentMethodsCurrentGetRaw();
        if(paymentMethodResponse.raw.status === HttpStatus.NO_CONTENT)
            return null;

        return await paymentMethodResponse.value();
    }));

export function usePaymentMethod() {
    const {isAuthenticated} = useAuth0();

    const [paymentMethodId, setPaymentMethodId] = useState<string>(void 0);

    const [paymentMethod, paymentMethodControls] = useGlobalResource(paymentMethodAccessor);

    useMemo(
        () => {
            if(!paymentMethodId || !isAuthenticated) 
                return;

            apiClient
                .apiPaymentMethodsPaymentMethodIdPut(paymentMethodId)
                .then(paymentMethodControls.refresh);

            setPaymentMethodId(void 0);
        }, 
        [
            paymentMethodId,
            isAuthenticated
        ]);

    return [paymentMethod, setPaymentMethodId] as [
        typeof paymentMethod,
        typeof setPaymentMethodId
    ];
}
