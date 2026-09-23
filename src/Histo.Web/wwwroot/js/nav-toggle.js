// Right-aligned header "Menu" disclosure — always shown (desktop and mobile), independent
// of govuk-frontend's built-in Service Navigation module (which only toggles below the
// tablet breakpoint). Click or Enter/Space (native <button> semantics) toggles the nav list;
// Escape closes it while focus is inside.
(function () {
    var toggle = document.querySelector('.app-nav-toggle');
    var menu = document.getElementById('navigation');
    if (!toggle || !menu) return;

    function setExpanded(expanded) {
        toggle.setAttribute('aria-expanded', expanded ? 'true' : 'false');
        menu.hidden = !expanded;
    }

    // Progressive enhancement: collapse the menu once JS is active.
    setExpanded(false);

    toggle.addEventListener('click', function () {
        setExpanded(toggle.getAttribute('aria-expanded') !== 'true');
    });

    menu.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') {
            setExpanded(false);
            toggle.focus();
        }
    });
})();
