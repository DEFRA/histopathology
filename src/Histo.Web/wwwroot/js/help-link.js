// A plain link click navigates the existing named Help tab in the background, so nothing appears
// to happen. Opening it by name from script and focusing it brings that tab to the front.
(function () {
    document.addEventListener('click', function (event) {
        var link = event.target.closest('a[target="histo-help"]');
        // Plain left-clicks only; leave modified clicks and any click another handler (e.g. the
        // unsaved-changes warning) has already cancelled alone.
        if (!link || event.defaultPrevented || event.button !== 0
            || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) {
            return;
        }

        var helpWindow = window.open(link.href, link.target);
        if (!helpWindow) {
            return; // popup blocked: let the browser follow the link normally
        }

        event.preventDefault();
        helpWindow.focus();
    });
})();
