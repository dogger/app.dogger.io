import React, { useState, useEffect } from 'react';
import { apiClient } from '../../api/Client';
import { trackGoal } from '../../utils';
import { RepositoryResponse, RepositoriesResponse } from '../../api/openapi';
import { ListItem, ListItemIcon, ListItemText, IconButton, ListItemSecondaryAction, List, Typography, Card, Dialog, DialogTitle, DialogContent, CircularProgress, Button, DialogActions } from '@material-ui/core';
import { Add, GitHub } from '@material-ui/icons';
import { PullDogPricingTable } from './PullDogPricingTable';
import { pullDogSettingsAccessor } from '../../hooks/pull-dog';
import { useGlobalResource } from '@fluffy-spoon/react-globalize';
import { RouteComponentProps } from '@reach/router';

const PullDogRepositorySettingsDialog = (props: { repository: RepositoryResponse, onDismiss: () => void }) => {
    return <>
        {!!props.repository && <Dialog open>
            <DialogTitle>
                Configure repository
            </DialogTitle>
            <DialogContent>
                <Typography variant="body1">
                    A repository will automatically be installed if you open a pull request for the repository, and a <pre style={{display: 'inline'}}>pull-dog.json</pre> is present in the root directory of the <pre style={{display: 'inline'}}>master</pre> branch.
                </Typography>
                <Typography variant="body1">
                    Repository handle: <pre style={{display: 'inline'}}>{props.repository.handle}</pre>
                </Typography>
            </DialogContent>
            <DialogActions>
                <Button autoFocus onClick={() => props.onDismiss()} color="primary">
                    OK
                </Button>
            </DialogActions>
        </Dialog>}
    </>;
};

export const PullDogPage = (props: RouteComponentProps) => {
    const [settingsResponse] = useGlobalResource(pullDogSettingsAccessor);

    const [repositoriesResponse, setRepositoriesResponse] = useState<RepositoriesResponse>(void 0);
    const [selectedRepository, setSelectedRepository] = useState<RepositoryResponse>(null);

    useEffect(() => {
        if(settingsResponse && settingsResponse.isInstalled)
            apiClient.apiPullDogRepositoriesGet().then(setRepositoriesResponse);
    }, [settingsResponse]);

    useEffect(() => {
        if(!!selectedRepository)
            trackGoal("NotImplementedManualRepositorySetup");
    }, [selectedRepository]);

    if(!settingsResponse)
        return <CircularProgress />;

    const areRepositoriesLoading = settingsResponse.isInstalled && !repositoriesResponse;

    if(!settingsResponse.isInstalled) {
        return <>
            <Typography variant="h3">
                Pull Dog
            </Typography>
            <Typography variant="body1" style={{opacity: 0.4}}>
                Automatic test environments for your pull requests, based on a Docker Compose file.
            </Typography>
            <Button 
                variant="contained"
                color="primary"
                startIcon={<GitHub />}
                style={{ alignSelf: 'flex-start', marginTop: 16 }}
                onClick={() => {
                    if(typeof window === "undefined")
                        return;

                    window.location.href = window.location.href.indexOf('://localhost') > -1 ?
                        'https://github.com/settings/apps/pull-dog-debugging/installations' :
                        'https://github.com/apps/pull-dog/installations/new'
                }}
            >
                Install
            </Button>
        </>;
    }

    const repositories = repositoriesResponse?.repositories;
    const managedRepositories = repositories?.filter(x => !!x.pullDogId);
    const availableRepositories = repositories?.filter(x => !x.pullDogId);

    return <>
        <PullDogRepositorySettingsDialog 
            repository={selectedRepository} 
            onDismiss={() => setSelectedRepository(null)} />
        
        <Typography variant="h3">
            Your plan
        </Typography>
        <PullDogPricingTable />

        <Typography variant="h3" style={{marginTop: 24}}>
            API key
        </Typography>
        <Typography variant="body1" style={{opacity: 0.4}}>
            The API key is used for certain operations like advanced configuration and lazy provisioning of servers from build environments.
        </Typography>
        <Typography variant="body1" style={{opacity: 0.4}}>
            Your API key is: <span style={{display: 'inline'}}>{settingsResponse.apiKey}</span>
        </Typography>

        <Typography variant="h3" style={{ marginTop: 24 }}>
            Repositories
        </Typography>
        {areRepositoriesLoading ?
            <CircularProgress /> :
            managedRepositories.length > 0 ?
                <> 
                <Typography variant="h4" style={{ marginTop: 16 }}>
                    Installed
                </Typography>
                <Typography variant="body1" style={{opacity: 0.4}}>
                    Click on a repository to see how to set it up.
                </Typography>
                <Card style={{
                    marginTop: 12
                }}>
                    <List>
                        {managedRepositories.map(value =>
                            <ListItem 
                                key={value.handle} 
                                button
                                onClick={() => setSelectedRepository(value)}
                            >
                                <ListItemIcon style={{
                                    minWidth: 40
                                }}>
                                    <GitHub/>
                                </ListItemIcon>
                                <ListItemText>
                                    {value.name}
                                </ListItemText>
                                {/* <ListItemSecondaryAction>
                                    <IconButton edge="end">
                                        <Remove />
                                    </IconButton>
                                </ListItemSecondaryAction> */}
                            </ListItem>)}
                    </List>
                </Card>
                </> :
                <>
                <Typography variant="body1" style={{opacity: 0.4}}>
                    You have no installed repositories.<br/>Install a repository from the list of available repositories below.
                </Typography>

                <Typography variant="h4" style={{ marginTop: 16 }}>
                    Available
                </Typography>
                <Typography variant="body1" style={{opacity: 0.4}}>
                    Click on a repository to set it up for Pull Dog.
                </Typography>
                {availableRepositories.length > 0 && 
                    <Card style={{
                        marginTop: 12
                    }}>
                        <List>
                            {availableRepositories.filter(x => !x.pullDogId).map(value =>
                                <ListItem 
                                    key={value.handle} 
                                    button
                                    onClick={() => setSelectedRepository(value)}
                                >
                                    <ListItemIcon style={{
                                        minWidth: 40
                                    }}>
                                        <GitHub/>
                                    </ListItemIcon>
                                    <ListItemText>
                                        {value.name}
                                    </ListItemText>
                                    <ListItemSecondaryAction>
                                        <IconButton edge="end">
                                            <Add />
                                        </IconButton>
                                    </ListItemSecondaryAction>
                                </ListItem>)}
                        </List>
                    </Card>}
                </>}
    </>
}