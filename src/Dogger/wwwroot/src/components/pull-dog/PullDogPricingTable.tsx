import React, { useState, useEffect } from 'react';
import { useTheme, Typography, Card, CardContent, Button, Grid, Slider, IconButton, makeStyles, Theme, CircularProgress } from '@material-ui/core';
import { GitHub, Add, Remove, TrendingUp, TrendingDown } from '@material-ui/icons';
import { getPlansByPrice, getCheapestPlan, getMostExpensivePlan, plansAccessor, demoPlanAccessor } from '../../hooks/plans';
import { PlanResponse, PullDogPlanResponse } from '../../api/openapi';
import { pullDogSettingsAccessor } from '../../hooks/pull-dog';
import { usePaymentMethod } from '../../hooks/payment';
import { apiClient } from '../../api/Client';
import { useGlobalResource } from '@fluffy-spoon/react-globalize';

const useStyles = makeStyles({
    header: () => ({}),
    accentColor: (existingTheme: Theme) => ({
        color: existingTheme.palette.type === "dark" ? 
            'white' : 
            existingTheme.palette.primary.main
    })
});

export const PullDogPricingPlanAttribute = (props: { 
    title: string, 
    value: string | number, 
    emphasize?: boolean 
}) => {
    const theme = useTheme();
    const isDarkTheme = theme.palette.type === "dark";
    return <div style={{
        marginTop: 12
    }}>
        <Typography variant="body1" component="h6" style={{
            fontSize: '12px',
            fontWeight: 'bold',
            textTransform: 'uppercase'
        }}>
            {props.title}
        </Typography>
        <Typography variant="body1" component="p" style={{
            fontSize: '16px',
            fontWeight: props.emphasize ? 'bold' : 100,
            opacity: isDarkTheme ? 0.6 : 1,
            color: isDarkTheme ? 'white' : theme.palette.primary.main
        }}>
            {props.value}
        </Typography>
    </div>
}

type PullDogPlan = {
    doggerPlanId: string;
    ramSizeInMegabytes: number;
    poolSize: number;
    price: number;
    cpuCount: number;
    title: string;
    isCurrent: boolean;
    isUnavailable: boolean;
    upgradeType: 'install' | 'upgrade' | 'downgrade';
};

export const PullDogPricingPlan = (props: {
    plan: PullDogPlan
}) => {
    const isFreePlan = props.plan.poolSize === 0;

    const settings = {
        install: {
            icon: <GitHub />,
            text: 'Install'
        },
        upgrade: {
            icon: <TrendingUp />,
            text: 'Upgrade'
        },
        downgrade: {
            icon: <TrendingDown />,
            text: 'Downgrade'
        }
    };

    const [alwaysDisabled, setAlwaysDisabled] = useState(false);
    const [pullDogSettings, pullDogSettingsController] = useGlobalResource(pullDogSettingsAccessor);
    const [paymentMethod] = usePaymentMethod();

    return <>
        <Card style={{
            opacity: props.plan.isUnavailable ?
                0.4 :
                1.0
        }}>
            <CardContent>
                <Typography variant="body1" component="h5" style={{
                    fontSize: '30px',
                    fontWeight: 100,
                    opacity: 0.5,
                    textTransform: 'uppercase'
                }}>
                    {props.plan.title}
                </Typography>
                <Typography variant="body1" component="h5" style={{
                    fontSize: '40px'
                }}>
                    {isFreePlan ?
                        <>Free!</> :
                        <>
                            <span style={{ fontWeight: 100, paddingRight: 4 }}>$</span>{props.plan.price / 100}<span style={{ opacity: 0.25, fontSize: '0.5em' }}>/mo</span>
                        </>}
                </Typography>
                <PullDogPricingPlanAttribute
                    title="Test environment pool *"
                    emphasize
                    value={isFreePlan ?
                        "Shared globally **" :
                        `${props.plan.poolSize} environment${props.plan.poolSize !== 1 ? "s" : ""}`} />
                <PullDogPricingPlanAttribute
                    title="Environment lifespan"
                    value={isFreePlan ?
                        "Minimum 15 minutes" :
                        "Indefinite"} />
                <PullDogPricingPlanAttribute
                    title="RAM"
                    value={`${props.plan.ramSizeInMegabytes / 1024} GB`} />
                <PullDogPricingPlanAttribute
                    title="CPUs"
                    value={props.plan.cpuCount} />
                
                <Button 
                    disabled={
                        props.plan.isCurrent || 
                        props.plan.isUnavailable || 
                        alwaysDisabled
                    } 
                    onClick={async () => {
                        setAlwaysDisabled(true);
                        
                        if(pullDogSettings?.isInstalled) {
                            if(!paymentMethod) {
                                alert("You first need to add a payment method under 'Account'.");
                                return;
                            }

                            await apiClient.apiPullDogChangePlanPost({
                                planId: props.plan.doggerPlanId,
                                poolSize: props.plan.poolSize
                            });

                            alert("Your plan has been changed!");

                            await pullDogSettingsController.refresh();
                        } else if(typeof window !== "undefined") {
                            window.location.href = 'https://github.com/apps/pull-dog/installations/new';
                        }
                        
                        setAlwaysDisabled(false);
                    }}
                    variant="contained" 
                    style={{ marginTop: 16 }} 
                    startIcon={!props.plan.isCurrent && settings[props.plan.upgradeType].icon} 
                    color="primary"
                >
                    {props.plan.isCurrent ? 
                        <span>
                            Current plan
                        </span> : 
                        <span>
                            {settings[props.plan.upgradeType].text}
                        </span>}
                </Button>
            </CardContent>
        </Card>
    </>;
}

export const PullDogPricingTable = () => {
    const [allPlans] = useGlobalResource(plansAccessor);
    const [demoPlan] = useGlobalResource(demoPlanAccessor);

    const [settings] = useGlobalResource(pullDogSettingsAccessor);

    const theme = useTheme();
    const styles = useStyles(theme);

    const doggerPlansByPrice = getPlansByPrice(allPlans);
    const mostExpensiveDoggerPlan = getMostExpensivePlan(doggerPlansByPrice);
    const cheapestDoggerPlan = getCheapestPlan(doggerPlansByPrice);

    const currentActiveDoggerPlan = doggerPlansByPrice?.find(x => 
        x.id === settings?.planId);

    const currentActivePullDogPlan = 
        currentActiveDoggerPlan
            ?.pullDogPlans
            ?.find(x => x.poolSize === settings?.poolSize);

    const [ram, setRam] = useState<number>(0);
    useEffect(() =>
        setRam(currentActiveDoggerPlan && settings && settings.poolSize > 0 ?
            currentActiveDoggerPlan.ramSizeInMegabytes :
            cheapestDoggerPlan?.ramSizeInMegabytes || 0),
        [
            currentActiveDoggerPlan, 
            cheapestDoggerPlan, 
            settings
        ]);

    if (!allPlans || !demoPlan || ram === 0)
        return <CircularProgress />;

    const demoPullDogPlan: PullDogPlanResponse = {
        id: demoPlan.id,
        poolSize: 0,
        priceInHundreds: 0
    };

    const ramConfigurations = doggerPlansByPrice.map(plan => plan.ramSizeInMegabytes / 1024);
    const getCurrentRamConfigurationIndex = () => ramConfigurations.indexOf(ram / 1024);

    const shiftRam = (increment: number) => {
        const currentIndex = getCurrentRamConfigurationIndex();
        const newRam = ramConfigurations[currentIndex + increment];
        if (!newRam)
            return;

        setRam(newRam * 1024);
    }

    const shiftRamIconStyle = {
        border: '1px solid rgba(127, 127, 127, 0.25)',
        marginTop: -6
    };

    const pickPlan = (index?: number) => doggerPlansByPrice[index === void 0 ? getCurrentRamConfigurationIndex() : index];

    const pickPullDogPlan = (doggerPlan: PlanResponse, title: string, amount: number): PullDogPlan => {
        const pullDogPlan = 
            doggerPlan
                .pullDogPlans
                .find(x => x.poolSize === amount) ||
            demoPullDogPlan;
        const pullDogPoolSize = pullDogPlan.poolSize || 0;
        return {
            poolSize: pullDogPoolSize,
            price: pullDogPlan.priceInHundreds || 0,
            ramSizeInMegabytes: doggerPlan.ramSizeInMegabytes,
            cpuCount: doggerPlan.cpuCount,
            doggerPlanId: doggerPlan.id,
            isCurrent: currentActivePullDogPlan ?
                currentActivePullDogPlan.id === pullDogPlan.id :
                pullDogPlan.poolSize === 0,
            isUnavailable: doggerPlan.ramSizeInMegabytes < ram,
            upgradeType: settings?.isInstalled ?
                (!currentActivePullDogPlan || pullDogPlan.priceInHundreds > currentActivePullDogPlan.priceInHundreds ?
                    'upgrade' :
                    'downgrade') :
                'install',
            title
        };
    };

    return <>
        <Typography variant="body1" component="p" className={styles.header} style={{
            fontSize: '20px',
            textAlign: 'center',
            padding: 8
        }}>
            How much RAM does your application require?
        </Typography>
        <Typography variant="body1" component="p" style={{
            fontSize: '40px',
            textAlign: 'center',
            paddingBottom: 48
        }}>
            <IconButton
                color="primary"
                style={shiftRamIconStyle}
                onClick={() => shiftRam(-1)}
                disabled={getCurrentRamConfigurationIndex() === 0}
                size="small"
            >
                <Remove />
            </IconButton>
            <span style={{ opacity: 0.7, padding: 8, width: 150, display: 'inline-block' }}>{ram / 1024} GB</span>
            <IconButton
                color="primary"
                style={shiftRamIconStyle}
                onClick={() => shiftRam(1)}
                disabled={getCurrentRamConfigurationIndex() === ramConfigurations.length - 1}
                size="small"
            >
                <Add />
            </IconButton>
        </Typography>
        <Slider
            defaultValue={cheapestDoggerPlan.ramSizeInMegabytes / 1024}
            valueLabelDisplay="on"
            onChange={(_, value) => setRam(value as number * 1024)}
            max={mostExpensiveDoggerPlan.ramSizeInMegabytes / 1024}
            min={0}
            value={ram / 1024}
            step={null}
            marks={ramConfigurations.map(ramConfiguration => ({
                value: ramConfiguration
            }))}
            style={{
                paddingBottom: 48
            }}
        />
        <Grid container spacing={2}>
            <Grid item xs={12} sm={6} md={3}>
                <PullDogPricingPlan
                    plan={pickPullDogPlan(
                        demoPlan,
                        'Shared', 
                        0)} />
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
                <PullDogPricingPlan
                    plan={pickPullDogPlan(
                        pickPlan(),
                        'Personal', 
                        1)} />
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
                <PullDogPricingPlan
                    plan={pickPullDogPlan(
                        pickPlan(),
                        'Pro', 
                        2)} />
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
                <PullDogPricingPlan
                    plan={pickPullDogPlan(
                        pickPlan(),
                        'Business', 
                        5)} />
            </Grid>
        </Grid>
    </>;
}