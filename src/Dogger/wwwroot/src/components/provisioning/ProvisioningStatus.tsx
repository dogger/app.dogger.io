export {}

// import React from 'react';
// import { Button, Dialog, DialogTitle, DialogActions, DialogContentText, DialogContent, CircularProgress, Table, TableBody, TableRow, TableCell, Grid, Paper, Typography, AppBar, Toolbar, IconButton, Slide } from '@material-ui/core';
// import { JobStatusResponse, ExposedPort, LogsResponse } from '../../api/openapi';
// import { apiClient } from '../../api/Client';
// import moment from 'moment';
// import { delay, handleValidationErrors, trackGoal } from '../../utils';
// import { LazyLog } from 'react-lazylog';

// import { Close } from '@material-ui/icons';

// import styles from './ProvisioningStatus.module.css';
// import { TransitionProps } from '@material-ui/core/transitions/transition';
// import { isDarkTheme } from '../../services/ThemeService';

// const Transition = React.forwardRef(function Transition(
//     props: TransitionProps & { children?: React.ReactElement },
//     ref: React.Ref<unknown>,
//   ) {
//     return <Slide direction="up" ref={ref} {...props} />;
//   });

// interface Props {

// }

// interface State {
//     operationStatus: JobStatusResponse;
//     isStatusWindowOpen: boolean;
//     lastUpdated: Date;

//     isSummaryWindowOpen: boolean;
//     serverHostName: string;
//     serverPorts: ExposedPort[];
//     serverLogs: LogsResponse[];
// }

// export class ProvisioningStatus extends React.Component<Props, State> {
//     constructor(props) {
//         super(props);
        
//         this.state = {
//             operationStatus: null,
//             isStatusWindowOpen: false,
//             lastUpdated: null,
            
//             isSummaryWindowOpen: false,
//             serverHostName: null,
//             serverPorts: null,
//             serverLogs: null
//         };
//     }

//     private async waitForJobCompletion(jobId: string) {
//         this.setState({
//             isStatusWindowOpen: true,
//             operationStatus: {
//                 isEnded: false,
//                 stateDescription: "Sending request to Dogger"
//             },
//             lastUpdated: new Date()
//         });

//         this.setState({
//             operationStatus: null,
//             lastUpdated: new Date()
//         });

//         let status: JobStatusResponse = {
//             isEnded: false
//         };
//         while(!status.isEnded) {
//             status = await apiClient.apiJobsJobIdStatusGet(jobId);

//             if(status !== this.state.operationStatus)
//                 trackGoal("State: " + status);

//             this.setState({
//                 operationStatus: status,
//                 lastUpdated: new Date()
//             });

//             await delay(2000);
//         }
        
//         if(status.isFailed) {
//             alert("Failed to provision server - try again later.\n" + status.stateDescription);

//             this.setState({
//                 isStatusWindowOpen: false,
//                 operationStatus: null
//             });
//         }
//     }

//     public async startDemoProvisioningFlow(dockerComposeYmlContents?: string) {
//         const alreadyInUseError = () => {
//             trackGoal("AlreadyInUse");
//             alert("Someone else is unfortunately using our demo server at the moment. Demo sessions last half an hour, so feel free to try again later! If you sign up, all demo instances will be 'claimed' by you, allowing for subsequent demo deploys to the same instance.");
//         };

//         await handleValidationErrors(
//             async () => {
//                 const provisioningResponse = await apiClient.apiPlansProvisionDemoPost();
//                 await this.waitForJobCompletion(provisioningResponse.jobId);

//                 const deployResponse = await apiClient.apiClustersDemoDeployPost({
//                     dockerComposeYmlContents: [dockerComposeYmlContents]
//                 });
//                 await this.waitForJobCompletion(deployResponse.jobId);

//                 this.setState({
//                     isStatusWindowOpen: false,
//                     operationStatus: null
//                 });

//                 await this.fetchConnectionInformation();
//                 await this.refreshLogs();
//             },
//             {
//                 "ALREADY_PROVISIONED": alreadyInUseError,
//                 "NOT_AUTHORIZED": alreadyInUseError
//             });
//     }

//     private async refreshLogs() {
//         if(this.state.serverPorts.length === 0)
//             return;

//         while(this.state.isSummaryWindowOpen) {
//             const logs = await apiClient.apiClustersDemoLogsGet();
//             this.setState({ serverLogs: logs });

//             await delay(1000);
//         }
//     }

//     private async fetchConnectionInformation() {
//         const connectionInformation = await apiClient.apiClustersDemoConnectionDetailsGet();
//         this.setState({
//             isSummaryWindowOpen: true,
//             serverHostName: connectionInformation.hostName ||
//                 connectionInformation.ipAddress,
//             serverPorts: connectionInformation.ports
//         });
//     }

//     private renderSummaryWindow() {let cellStyle: React.CSSProperties = {};
//         return <Dialog 
//             open={this.state.isSummaryWindowOpen} 
//             fullScreen
//             TransitionComponent={Transition}
//         >
//             <AppBar position="sticky">
//                 <Toolbar>
//                     <IconButton edge="start" color="inherit" onClick={() => {
//                         this.setState({isSummaryWindowOpen: false});
//                     }}>
//                         <Close />
//                     </IconButton>
//                     <Typography variant="h6">
//                         Your server is up and running!
//                     </Typography>
//                 </Toolbar>
//             </AppBar>
//             <DialogContent style={{
//                 paddingTop: 24,
//                 backgroundColor: isDarkTheme(theme) ? '#333' : '#f5f5f5'
//             }}>
//                 <Grid>
//                     <Paper>
//                         <Table size="small">
//                             <TableBody>
//                                 <TableRow>
//                                     <TableCell style={{...cellStyle, fontWeight: 'bold'}} component="th" scope="row">
//                                         Protocol
//                                     </TableCell>
//                                     <TableCell style={{...cellStyle, fontWeight: 'bold'}} component="th" scope="row">
//                                         Hostname
//                                     </TableCell>
//                                     <TableCell style={{...cellStyle, fontWeight: 'bold'}} component="th" scope="row">
//                                         Port
//                                     </TableCell>
//                                 </TableRow>
//                                 {this.state.serverPorts && this.state.serverPorts.map(exposedPort => (
//                                     <TableRow key={exposedPort.protocol + exposedPort.port}>
//                                         <TableCell style={cellStyle} scope="row">
//                                             {exposedPort.protocol}
//                                         </TableCell>
//                                         <TableCell style={cellStyle}>
//                                             {this.state.serverHostName}
//                                         </TableCell>
//                                         <TableCell style={cellStyle}>
//                                             {exposedPort.port}
//                                         </TableCell>
//                                     </TableRow>
//                                 ))}
//                             </TableBody>
//                         </Table>
//                     </Paper>
//                     {this.state.serverLogs && this.state.serverLogs.map(logsResponse => (
//                         logsResponse.logs && <Paper key={logsResponse.containerId} style={{
//                             marginTop: 16,
//                             paddingTop: 16
//                         }}>
//                             <Typography variant="h5" noWrap style={{
//                                 paddingLeft: 16,
//                                 paddingRight: 16
//                             }}>
//                                 {logsResponse.containerId}
//                             </Typography>
//                             <Typography color="textSecondary" noWrap style={{
//                                 marginBottom: 12,
//                                 paddingLeft: 16,
//                                 paddingRight: 16
//                             }}>
//                                 {logsResponse.containerImage}
//                             </Typography>
//                             <LazyLog 
//                                 height={200}
//                                 follow
//                                 text={logsResponse.logs}
//                                 lineClassName={styles.line}
//                                 style={{
//                                     borderBottomLeftRadius: 3,
//                                     borderBottomRightRadius: 3,
//                                     paddingTop: 12,
//                                     paddingBottom: 12,
//                                     backgroundColor: isDarkTheme ? '#222' : '#fbfbfb',
//                                     borderTop: isDarkTheme ? '#111' : '1px solid #eee'
//                                 }} />
//                         </Paper>
//                     ))}
//                 </Grid>
//             </DialogContent>
//             <DialogActions>
//                 <Button color="primary" onClick={() => this.setState({ isSummaryWindowOpen: false })}>
//                     Close
//                 </Button>
//             </DialogActions>
//         </Dialog>;
//     }

//     render() {
//         if(this.state.isSummaryWindowOpen) {    
//             return this.renderSummaryWindow();
//         }

//         return this.state.operationStatus && <Dialog open={this.state.isStatusWindowOpen}>
//             <DialogTitle>
//                 Your server is on its way
//             </DialogTitle>
//             <DialogContent>
//                 <DialogContentText>
//                     We have requested a new server in AWS Lightsail just for you. This might take a couple of minutes, but it'll be worth the wait.
//                 </DialogContentText>
//                 <div>
//                     {!this.state.operationStatus.isEnded && 
//                         <><span style={{
//                             paddingRight: 8
//                         }}>
//                             <CircularProgress size={12} />
//                         </span>
//                         <span style={{ 
//                             fontWeight: 'lighter',
//                             opacity: 0.75,
//                             fontSize: 14
//                         }}>
//                             {this.state.operationStatus.stateDescription} <br/>
//                             <span style={{
//                                 fontSize: 12,
//                                 paddingLeft: 20
//                             }}>
//                                 Last updated: {moment(this.state.lastUpdated).format("hh:mm:ss A")}
//                             </span>
//                         </span></>}
//                 </div>
//             </DialogContent>
//             <DialogActions>
//             </DialogActions>
//         </Dialog>;
//     }
// }