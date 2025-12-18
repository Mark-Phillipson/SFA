// site.js copied from SFA_PWA/wwwroot/js/site.js
window.focusElement = (element) => {
    if (element) element.focus();
};

window.focusElementBySelector = (selector) => {
    setTimeout(() => {
        const element = document.querySelector(selector);
        if (element) element.focus();
    }, 0);
};

window.hardRefresh = () => {
    // Force a hard refresh by reloading from the server, not cache
    location.reload(true);
};

window.isMobileDevice = () => {
    // Check if device is mobile based on user agent and screen size
    const userAgent = navigator.userAgent || navigator.vendor || window.opera;
    const isMobileUA = /android|webos|iphone|ipad|ipod|blackberry|iemobile|opera mini/i.test(userAgent.toLowerCase());
    return isMobileUA || (window.innerWidth <= 800 && window.innerHeight <= 600);
};
