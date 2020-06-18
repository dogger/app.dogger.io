import React, { PropsWithChildren } from 'react';
import { Container, Drawer, List, ListItem, ListItemText, makeStyles, createStyles, Theme, ListItemIcon, CircularProgress } from '@material-ui/core';
import { AccountCircle, Code } from '@material-ui/icons';
import {Helmet} from "react-helmet";
import { useAuth0 } from '../../auth/Auth0Provider';
import { navigate } from 'gatsby';
import { AccountPage } from '../../components/account/AccountPage';
import { usePath } from '../../hooks/path';

const useStyles = makeStyles((theme: Theme) =>
    createStyles({
        root: {
            display: 'flex',
            flexDirection: 'row',
            height: '100%',
            paddingTop: 56
        },
        drawer: {
            width: "auto",
            flexShrink: 0,
            zIndex: theme.zIndex.appBar - 1
        },
        drawerPaper: {
            top: "initial",
            left: "initial",
            right: "initial",
            width: "auto",
            position: "initial"
        },
        content: {
            flexGrow: 1,
            padding: theme.spacing(3)
        }
    }),
);

type MenuItem = {
    title: string;
    url: string;
    renderIcon: () => JSX.Element;
}

export const DashboardPage = (props: PropsWithChildren<any>) => {
    const classes = useStyles();
    const {isAuthenticated, loading } = useAuth0();
    const pathName = usePath();
    if(!isAuthenticated || loading)
        return <CircularProgress />;

    const menuItems: MenuItem[] = [
        {
            title: "Account",
            url: "/dashboard",
            renderIcon: () => <AccountCircle />
        },
        // {
        //     title: "Instances",
        //     url: "/dashboard/instances",
        //     renderIcon: () => <Storage />,
        //     renderPage: () => <InstancesPage />
        // },
        // {
        //     title: "CLI",
        //     url: "/dashboard/cli",
        //     renderIcon: () => <SettingsApplications />,
        //     renderPage: () => <CliPage />
        // },
        {
            title: "Pull Dog",
            url: "/dashboard/pull-dog",
            renderIcon: () => <Code />
        }
    ];

    const matchingPage = menuItems.find(x => x.url === pathName) || menuItems[0];
    return <>
        <Helmet>
            <title>Administration</title>
            <meta name="robots" content="none" />
        </Helmet>
        <div className={classes.root}>
            <Drawer open
                className={classes.drawer}
                variant="permanent"
                classes={{
                    paper: classes.drawerPaper,
                }}
            >
                <List>
                    {menuItems.map(item => (
                        <ListItem
                            button
                            key={item.title}
                            onClick={() => navigate(item.url)}
                            selected={matchingPage === item}
                        >
                            <ListItemIcon style={{ minWidth: 44, paddingLeft: 12 }}>{item.renderIcon()}</ListItemIcon>
                            <ListItemText primary={item.title} style={{ textAlign: 'left', paddingRight: 16 }} />
                        </ListItem>
                    ))}
                </List>
            </Drawer>
            <Container className={classes.content} style={{
                display: 'flex',
                flexDirection: 'column'
            }}>
                {props.children}
            </Container>
        </div>
    </>;
};

export default () => (
    <DashboardPage>
        <AccountPage />
    </DashboardPage>
)