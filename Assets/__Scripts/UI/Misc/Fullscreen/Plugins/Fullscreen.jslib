mergeInto(LibraryManager.library, {

    ToggleFullscreenWeb: function() {
        if(!document.fullscreenEnabled) return;

        if(!document.fullscreenElement) {
            const canvasElement = document.getElementById("unity-canvas");
            if(!canvasElement) return;
            if (canvasElement.requestFullScreen) {
                canvasElement.requestFullScreen();
            } else if (canvasElement.mozRequestFullScreen) {
                canvasElement.mozRequestFullScreen();
            } else if (canvasElement.webkitRequestFullScreen) {
                canvasElement.webkitRequestFullScreen(Element.ALLOW_KEYBOARD_INPUT);
            } else if (canvasElement.msRequestFullscreen) {
                canvasElement.msRequestFullscreen();
            }
        }
        else if(document.exitFullscreen) {
            if (document.cancelFullScreen) {
                document.cancelFullScreen();
            } else if (document.mozCancelFullScreen) {
                document.mozCancelFullScreen();
            } else if (document.webkitCancelFullScreen) {
                document.webkitCancelFullScreen();
            } else if (document.msExitFullscreen) {
                document.msExitFullscreen();
            }
        }
    },

    GetFullscreen: function() {
        return document.fullscreenElement != null;
    }
});