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
                // Use location.href so Universal Links/App Links can open the app instead of the browser
                window.location.href = webUrl;
            }, 1500);
        } else {
            // No numeric location ID found - use location.href so Universal Links/App Links
            // (if configured by BBC) can open the native app. This improves mobile behaviour.
            window.location.href = webUrl;
        }
    } else {
        // Desktop - just open web URL
        window.open(webUrl, '_blank');
    }
};

// Helpers for NavMenu responsiveness: auto-expand on small phone portrait and notify .NET on changes
window.navMenu = {
    _handler: null,
    isSmallPhonePortrait: function() {
        // Pixel 6a width is ~412px; 430px gives a small safety margin
        return window.matchMedia('(max-width: 430px) and (orientation: portrait)').matches;
    },
    registerNavMenuResizeCallback: function(dotNetObj) {
        if (this._handler) {
            window.removeEventListener('resize', this._handler);
            window.removeEventListener('orientationchange', this._handler);
        }
        this._handler = () => {
            const matches = window.matchMedia('(max-width: 430px) and (orientation: portrait)').matches;
            // Notify .NET about the current state
            dotNetObj.invokeMethodAsync('UpdateMenuForScreen', matches);
        };
        window.addEventListener('resize', this._handler);
        window.addEventListener('orientationchange', this._handler);
    },
    unregisterNavMenuResizeCallback: function() {
        if (this._handler) {
            window.removeEventListener('resize', this._handler);
            window.removeEventListener('orientationchange', this._handler);
            this._handler = null;
        }
    }
};