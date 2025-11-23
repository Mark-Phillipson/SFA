// Network status monitoring for PWA offline support
window.networkStatusHelper = {
    initialize: function(dotnetHelper) {
        window.networkStatusServiceRef = dotnetHelper;
        
        window.addEventListener('online', () => {
            if (window.networkStatusServiceRef) {
                window.networkStatusServiceRef.invokeMethodAsync('NotifyOnlineStatusChanged', true);
            }
        });
        
        window.addEventListener('offline', () => {
            if (window.networkStatusServiceRef) {
                window.networkStatusServiceRef.invokeMethodAsync('NotifyOnlineStatusChanged', false);
            }
        });
    },
    
    isOnline: function() {
        return navigator.onLine;
    },
    
    dispose: function() {
        delete window.networkStatusServiceRef;
    }
};
