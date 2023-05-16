mergeInto(LibraryManager.library, {

    ToggleFullscreenWeb: function() {
        ToggleFullscreen();
    },

    GetFullscreen: function() {
        return document.fullscreenElement != null;
    }
});