// Generic grid-refresh pagination behaviour: intercepts clicks on in-page query-string
// links (pagination First/Previous/numbers/Next/Last, and sortable column headers) inside
// a grid container and re-fetches just that container's markup via the page's "Grid" AJAX
// handler, instead of letting the browser navigate and reload the whole page.
//
// Usage: wrap the table + pagination partial in a container with
//   <div data-grid-url="@Url.Page("/CurrentPage", "Grid")"> ... </div>
// Any <a href="?..."> inside the container is intercepted automatically.
(function () {
    "use strict";

    function initGridContainer(container) {
        container.addEventListener("click", function (event) {
            var link = event.target.closest("a[href]");
            if (!link || !container.contains(link)) {
                return;
            }

            var href = link.search;
            // Only intercept same-page query-string links (pagination/sort) — real
            // navigation links (e.g. "Change", "Return to submission") are left alone.
            if (!href || !href.startsWith("?") || href === "#") {
                return;
            }

            event.preventDefault();
            if (link.classList.contains("app-pagination-window__nav-link--disabled")) {
                return;
            }

            var gridUrl = container.dataset.gridUrl;
            if (!gridUrl) {
                return;
            }

            var query = href.slice(1); // drop leading "?"
            var requestUrl = gridUrl + (gridUrl.includes("?") ? "&" : "?") + query;

            fetch(requestUrl, { headers: { "X-Requested-With": "XMLHttpRequest" } })
                .then(function (response) {
                    if (!response.ok) {
                        throw new Error(`Grid refresh failed: ${response.status}`);
                    }
                    return response.text();
                })
                .then(function (html) {
                    container.innerHTML = html;
                    // Keep the browser URL/history in sync with the current page/sort state
                    // without triggering a navigation or reload.
                    var displayUrl = `${window.location.pathname}?${query}`;
                    window.history.replaceState(null, "", displayUrl);
                })
                .catch(function (err) {
                    // Fall back to a normal navigation if the AJAX refresh fails for any reason.
                    console.error(err);
                    window.location.href = link.href;
                });
        });
    }

    document.querySelectorAll("[data-grid-url]").forEach(initGridContainer);
})();
