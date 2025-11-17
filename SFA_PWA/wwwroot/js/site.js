window.focusElement = (element) => {
    if (element) element.focus();
};

window.hardRefresh = () => {
    // Force a hard refresh by reloading from the server, not cache
    location.reload(true);
};