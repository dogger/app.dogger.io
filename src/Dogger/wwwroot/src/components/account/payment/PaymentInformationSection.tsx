import React from 'react';
import { Typography, Card, CardContent, CircularProgress } from '@material-ui/core';
import { Elements } from '@stripe/react-stripe-js';
import { PaymentForm } from './PaymentForm';
import { stripePromise } from '../../../setup/stripe';
import { PaymentMethod } from '@stripe/stripe-js';
import { usePaymentMethod } from '../../../hooks/payment';

export const PaymentInformationSection = () => {
    const [paymentMethod, setPaymentMethod] = usePaymentMethod();

    function onUpdatePaymentMethodClicked(method: PaymentMethod) {
        setPaymentMethod(method.id);
        alert("Payment method added!");
    }

    if(paymentMethod === void 0)
        return <CircularProgress />;

    return <>
        <Typography variant="h3">
            {paymentMethod ? 
                "Payment method" : 
                "Set payment method"}
        </Typography>
        {paymentMethod ?
            <Card style={{
                padding: 24,
                marginTop: 16,
                display: 'flex'
            }}>
                <CardContent>
                    <Typography variant="h6" style={{ textAlign: 'center' }}>
                        {paymentMethod.brand}
                    </Typography>
                </CardContent>
            </Card> :
            <Elements stripe={stripePromise}>
                <PaymentForm 
                    onSubmit={onUpdatePaymentMethodClicked} 
                />
            </Elements>}
    </>;
}