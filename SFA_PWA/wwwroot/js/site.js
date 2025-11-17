window.focusElement = (element) => {
    if (element) element.focus();
};

window.hardRefresh = () => {
    // Force a hard refresh by reloading from the server, not cache
    location.reload(true);
};

window.isMobileDevice = () => {
    // Check if device is mobile based on user agent and screen size
    const userAgent = navigator.userAgent || navigator.vendor || window.opera;
    const isMobileUA = /android|webos|iphone|ipad|ipod|blackberry|iemobile|opera mini/i.test(userAgent.toLowerCase());
    const isMobileScreen = window.innerWidth <= 768;
    return isMobileUA || isMobileScreen;
};

window.openWeatherUrl = (webUrl) => {
    const isMobile = window.isMobileDevice();
    
    if (isMobile) {
        // Try to open BBC Weather app using custom scheme
        // BBC Weather app uses bbc-weather:// scheme on some devices
        // We'll attempt the app first, then fallback to web
        const locationId = webUrl.match(/\/weather\/(\d+)/);
        
        if (locationId && locationId[1]) {
            // Try app deep link first
            const appUrl = `bbc-weather://location/${locationId[1]}`;
            
            // Create a hidden iframe to attempt app launch
            const iframe = document.createElement('iframe');
            iframe.style.display = 'none';
            iframe.src = appUrl;
            document.body.appendChild(iframe);
            
            // Fallback to web URL after short delay if app doesn't open
            setTimeout(() => {
                document.body.removeChild(iframe);
                window.open(webUrl, '_blank');
            }, 1500);
        } else {
            // No location ID found, just open web URL
            window.open(webUrl, '_blank');
        }
    } else {
        // Desktop - just open web URL
        window.open(webUrl, '_blank');
    }
};