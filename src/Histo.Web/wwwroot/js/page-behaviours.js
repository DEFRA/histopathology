// Replaces inline event handlers (blocked by the Content-Security-Policy) with data attributes.
(function () {
    "use strict";

    // <a|button data-confirm="message" [data-confirm-unless-filled="inputId"]>
    document.addEventListener("click", function (event) {
        const target = event.target instanceof Element ? event.target.closest("[data-confirm]") : null;
        if (!target) return;

        const fieldId = target.dataset.confirmUnlessFilled;
        const field = fieldId ? document.getElementById(fieldId) : null;
        if (field && field.value !== "") return;

        if (!window.confirm(target.dataset.confirm)) event.preventDefault();
    });

    // <input data-auto-submit> submits its form as soon as it changes.
    document.addEventListener("change", function (event) {
        const target = event.target;
        if (target instanceof HTMLInputElement && "autoSubmit" in target.dataset && target.form) target.form.submit();
    });
})();
