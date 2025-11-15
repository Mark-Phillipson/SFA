// JS helpers for PWA install and device detection
(function () {
    // Keep the install prompt event for later
    let deferredPrompt = null;

    // Capture the beforeinstallprompt event so we can show it later
    window.addEventListener('beforeinstallprompt', (e) => {
        e.preventDefault();
        deferredPrompt = e;
        window.deferredPwaPrompt = e; // expose for manual check
        console.log('beforeinstallprompt captured');
    });

    // Helper: check if deferred prompt is present
    window.hasDeferredPwaPrompt = function () {
        return !!window.deferredPwaPrompt;
    };

    // Helper: get device info (OS, isMobile, UA, browser)
    window.getDeviceInfo = function () {
        const ua = navigator.userAgent || '';
        const uaLower = ua.toLowerCase();
        const isMobile = /android|iphone|ipad|ipod|mobile/i.test(uaLower) || (window.isMobileScreen && window.isMobileScreen());
        const os = /android/i.test(ua) ? 'Android' : /iphone|ipad|ipod/i.test(ua) ? 'iOS' : /windows/i.test(ua) ? 'Windows' : /macintosh/i.test(ua) ? 'Mac' : 'Unknown';
        let browser = 'Unknown';
        if (/chrome/i.test(ua) && !/edg/i.test(ua)) browser = 'Chrome';
        else if (/safari/i.test(ua) && !/chrome/i.test(ua)) browser = 'Safari';
        else if (/firefox/i.test(ua)) browser = 'Firefox';
        else if (/edg/i.test(ua) || /edge/i.test(ua)) browser = 'Edge';

        return { os, isMobile, browser, ua };
    };

    // Helper: detect if app is already installed (display-mode standalone)
    window.isPwaInstalled = function () {
        try {
            return window.matchMedia('(display-mode: standalone)').matches || window.navigator.standalone === true;
        }
        catch (e) {
            return false;
        }
    };

    // Prompt the saved beforeinstallprompt. Return a promise with the user choice or fallback
    window.promptInstall = async function () {
        if (deferredPrompt) {
            try {
                await deferredPrompt.prompt();
                const choice = await deferredPrompt.userChoice;
                // Clear saved prompt after use
                deferredPrompt = null;
                return { prompted: true, outcome: choice.outcome };
            }
            catch (err) {
                console.error('promptInstall failed', err);
                return { prompted: false, error: err };
            }
        }

        // No prompt available - return a fallback so .NET can show friendly instructions
        return { prompted: false, fallback: true };
    };

    // Build a simple google search query for 'how to add to home screen' for device
    window.openInstallSearch = function (deviceLabel) {
        const query = `how to add this website to home screen ${deviceLabel}`;
        const url = `https://www.google.com/search?q=${encodeURIComponent(query)}`;
        window.open(url, '_blank');
    };

    // Expose for diagnostics
    window.pwaInstallHelpers = {
        isInstalled: window.isPwaInstalled,
        promptInstall: window.promptInstall,
        getDeviceInfo: window.getDeviceInfo,
        openInstallSearch: window.openInstallSearch
    };
})();
