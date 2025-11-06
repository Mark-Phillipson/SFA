window.getScreenWidth = function() {
    return window.innerWidth || document.documentElement.clientWidth || document.body.clientWidth;
};

window.isMobileScreen = function() {
    // Use 768px as mobile threshold (covers most phones/tablets)
    var width = window.innerWidth || document.documentElement.clientWidth || document.body.clientWidth;
    var mobile = width < 768;
    console.log('[screen.js] isMobileScreen:', mobile, 'width:', width);
    return mobile;
};

window.registerResizeHandler = function(dotnetHelper) {
    window.addEventListener('resize', function() {
        var mobile = window.isMobileScreen();
        console.log('[screen.js] resize event, mobile:', mobile);
        dotnetHelper.invokeMethodAsync('OnResize', mobile);
    });
};

// Attach onload handlers to all calendar iframes and call Blazor when loaded
window.attachCalendarLoadHandlers = function (dotnetHelper) {
    var iframes = document.querySelectorAll('iframe[id^="calendar-"]');
    iframes.forEach(function (iframe) {
        var groupName = iframe.id.replace('calendar-', '');
        var called = false;
        function markLoaded() {
            if (!called) {
                called = true;
                dotnetHelper.invokeMethodAsync('OnCalendarLoaded', groupName);
            }
        }
        // If already loaded, call immediately
        if (iframe.contentDocument && iframe.contentDocument.readyState === 'complete') {
            markLoaded();
        } else {
            iframe.onload = markLoaded;
            // Fallback: hide spinner after 5 seconds if onload doesn't fire
            setTimeout(markLoaded, 5000);
        }
    });
};
