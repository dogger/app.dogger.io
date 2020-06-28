import React from "react";
import { useAuth0 } from "../auth/Auth0Provider";
import { AppBar, Toolbar, Button, Typography, MenuItem, Menu, CircularProgress, Avatar, Box, IconButton, useTheme } from "@material-ui/core";
import { ProgressBar } from "./FetchSpinner";
import { Brightness7, Brightness5 } from "@material-ui/icons";
import { navigate } from "gatsby"

export const NavigationBar = (props: { onThemeToggle: () => void }) => {
  const theme = useTheme();
  const { isAuthenticated, logout, user, loading, loginWithRedirect } = useAuth0();
  const [anchorElement, setAnchorElement] = React.useState(null);

  const handleMenuClick = event => {
    setAnchorElement(event.currentTarget);
  };

  const handleMenuClose = () => {
    setAnchorElement(null);
  };

  return (
    <AppBar position="fixed">
      <Toolbar>
        <Button color="inherit" style={{ textTransform: 'uppercase', marginRight: 11 }} onClick={() => navigate('/')}>
          <Typography variant="h6">
            Dogger
          </Typography>
        </Button>

        <Button color="inherit" onClick={() => navigate('/blog')}>
          Blog
        </Button>

        <Button color="inherit" onClick={() => navigate('/documentation')}>
          Docs
        </Button>

        <Button color="inherit" target="_blank" href="https://github.com/dogger/dogger.io/issues/new" rel="nofollow">
          Contact
        </Button>

        <Box style={{
          marginLeft: 12,
          width: 35,
          alignContent: 'center'
        }}>
          <ProgressBar render={() => (
            <CircularProgress
              color="inherit"
              size={20}
              style={{
                marginTop: 4
              }} />)}
          />
        </Box>

        <div style={{ flexGrow: 1 }}></div>

        <IconButton color="inherit" onClick={() => props.onThemeToggle()} style={{marginRight: 4}}>
              {theme.palette.type === "dark" ? 
                <Brightness7 /> :
                <Brightness5 />}
        </IconButton>

        {loading ?
          <CircularProgress color="inherit" /> :
          (!isAuthenticated ?
            <Button color="inherit" onClick={() => loginWithRedirect({
              appState: {
                targetUrl: '/dashboard'
              }
            })}>Log in</Button> :
            <>
              <Button color="inherit" onClick={handleMenuClick} style={{
                marginLeft: 8
              }}>
                <Typography noWrap>
                  {user.name}
                </Typography>
                <Avatar
                  alt={user.name}
                  src={user.picture}
                  style={{
                    marginLeft: 16
                  }} />
              </Button>
              <Menu
                id="simple-menu"
                anchorEl={anchorElement}
                anchorOrigin={{
                  vertical: 'bottom',
                  horizontal: 'right'
                }}
                keepMounted
                open={Boolean(anchorElement)}
                onClose={handleMenuClose}
              >
                <MenuItem onClick={() => {
                  handleMenuClose();
                  logout();
                }}>
                  Logout
                      </MenuItem>
                <MenuItem onClick={() => navigate('/dashboard')}>
                  Dashboard
                      </MenuItem>
              </Menu>
            </>)}
      </Toolbar>
    </AppBar>
  );
};