import {Theme} from '@material-ui/core';

export function isDarkTheme(theme: Theme) {
    return theme.palette.type === "dark";
}