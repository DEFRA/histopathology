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
        const candidates = container.querySelectorAll("a[href], button");
        let refocused = false;
        for (const candidate of candidates) {
            const label = candidate.getAttribute("aria-label") || candidate.textContent.trim();
            if (label === clickedLabel && candidate.getAttribute("aria-disabled") !== "true") {
                candidate.focus();
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

        let region = document.getElementById("grid-status");
        if (!region) {
            region = document.createElement("div");
            region.id = "grid-status";
            region.className = "govuk-visually-hidden";
            region.setAttribute("role", "status");
            document.body.appendChild(region);
        }
        const summary = container.querySelector(".app-pagination-window__summary");
        region.textContent = "Results updated" + (summary ? ". " + summary.textContent.trim() : "");
    }

    // Only same-page query-string links (pagination/sort) are intercepted; real navigation links
    // (e.g. "Change", "Return to submission") and row-action links to other pages are left alone.
    function isGridQueryLink(link) {
        return link.search?.startsWith("?") && link.pathname === window.location.pathname;
    }

    function initGridContainer(container) {
        container.addEventListener("click", function (event) {
            const link = event.target.closest("a[href]");
            if (!link || !container.contains(link) || !isGridQueryLink(link)) {
                return;
            }

            const href = link.search;

            event.preventDefault();
            if (link.classList.contains("app-pagination-window__nav-link--disabled")) {
                return;
            }

            const gridUrl = container.dataset.gridUrl;
            if (!gridUrl) {
                return;
            }

            const query = href.slice(1); // drop leading "?"
            const requestUrl = gridUrl + (gridUrl.includes("?") ? "&" : "?") + query;
            const clickedLabel = link.getAttribute("aria-label") || link.textContent.trim();

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
                    const displayUrl = `${window.location.pathname}?${query}`;
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
