// Progressive enhancement: copies each column header into its cells' data-label so
// Styles.css can stack rows as labelled cards on narrow viewports (WCAG 1.4.10 reflow).
(function () {
    function labelTables(root) {
        root.querySelectorAll('table.govuk-table').forEach(function (table) {
            const headRow = table.querySelector('thead tr');
            if (!headRow || table.classList.contains('app-table--scroll') || table.querySelector('[colspan], [rowspan]')) {
                return;
            }

            const labels = Array.prototype.map.call(headRow.children, function (th) {
                return th.dataset.label || th.textContent.replace(/\s+/g, ' ').trim();
            });

            table.querySelectorAll('tbody tr').forEach(function (row) {
                Array.prototype.forEach.call(row.children, function (cell, i) {
                    if (labels[i] && !('label' in cell.dataset)) {
                        cell.dataset.label = labels[i];
                    }
                });
            });

            table.classList.add('app-responsive-table');
        });
    }

    // Exposed so AJAX grid refreshes can re-label the markup they swap in.
    window.appLabelTables = labelTables;

    labelTables(document.querySelector('main') || document);
})();
