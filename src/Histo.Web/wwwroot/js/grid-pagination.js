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

    // The clicked link is replaced by the refresh, which would drop keyboard focus to <body> and give
    // screen-reader users no sign that anything changed.
    function restoreFocusAndAnnounce(container, clickedLabel) {
        var candidates = container.querySelectorAll("a[href], button");
        var refocused = false;
        for (var i = 0; i < candidates.length; i++) {
            var label = candidates[i].getAttribute("aria-label") || candidates[i].textContent.trim();
            if (label === clickedLabel && candidates[i].getAttribute("aria-disabled") !== "true") {
                candidates[i].focus();
                refocused = true;
                break;
            }
        }

        // The clicked control may no longer exist (e.g. it became the current page); keep focus inside the grid.
        if (!refocused) {
            container.setAttribute("tabindex", "-1");
            container.style.outline = "none";
            container.focus({ preventScroll: true });
        }

        var region = document.getElementById("grid-status");
        if (!region) {
            region = document.createElement("div");
            region.id = "grid-status";
            region.className = "govuk-visually-hidden";
            region.setAttribute("role", "status");
            document.body.appendChild(region);
        }
        var summary = container.querySelector(".app-pagination-window__summary");
        region.textContent = "Results updated" + (summary ? ". " + summary.textContent.trim() : "");
    }

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
            // Row-action links (e.g. "Change") also carry query-string state (returnUrl,
            // sortColumn, etc.) but point at a different page — only the query string alone
            // isn't enough to identify a pagination/sort link, so the pathname must match too.
            if (link.pathname !== window.location.pathname) {
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
            var clickedLabel = link.getAttribute("aria-label") || link.textContent.trim();

            fetch(requestUrl, { headers: { "X-Requested-With": "XMLHttpRequest" } })
                .then(function (response) {
                    if (!response.ok) {
                        throw new Error(`Grid refresh failed: ${response.status}`);
                    }
                    return response.text();
                })
                .then(function (html) {
                    container.innerHTML = html;
                    if (window.appLabelTables) {
                        window.appLabelTables(container);
                    }
                    restoreFocusAndAnnounce(container, clickedLabel);
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
