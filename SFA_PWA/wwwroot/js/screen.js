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
