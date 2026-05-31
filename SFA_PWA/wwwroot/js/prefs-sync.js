// Helper to notify Blazor components when groupPreferences in localStorage change.
(function () {
    window._sfaGroupPrefsListeners = window._sfaGroupPrefsListeners || [];

    window.sfaRegisterGroupPrefsChanged = function (dotNetRef) {
        if (!dotNetRef) return;
        var handler = function () {
            try { dotNetRef.invokeMethodAsync('NotifyGroupPreferencesChanged'); } catch (e) { console.error(e); }
        };
        window._sfaGroupPrefsListeners.push({ dotNetRef: dotNetRef, handler: handler });
        window.addEventListener('groupPreferencesChanged', handler);
        // Also map native storage events (other tabs) to our custom event
        if (!window._sfaGroupPrefsStorageHooked) {
            window.addEventListener('storage', function (e) {
                if (e.key === 'groupPreferences') {
                    window.dispatchEvent(new Event('groupPreferencesChanged'));
                }
            });
            window._sfaGroupPrefsStorageHooked = true;
        }
    };

    window.sfaUnregisterGroupPrefsChanged = function (dotNetRef) {
        if (!dotNetRef || !window._sfaGroupPrefsListeners) return;
        var idx = window._sfaGroupPrefsListeners.findIndex(function (x) { return x.dotNetRef === dotNetRef; });
        if (idx >= 0) {
            var entry = window._sfaGroupPrefsListeners[idx];
            window.removeEventListener('groupPreferencesChanged', entry.handler);
            window._sfaGroupPrefsListeners.splice(idx, 1);
        }
    };

    window.sfaNotifyGroupPrefsChanged = function () {
        try { window.dispatchEvent(new Event('groupPreferencesChanged')); } catch (e) { console.error(e); }
    };

})();
