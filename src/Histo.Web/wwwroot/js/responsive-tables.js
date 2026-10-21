// Progressive enhancement: copies each column header into its cells' data-label so
// Styles.css can stack rows as labelled cards on narrow viewports (WCAG 1.4.10 reflow).
(function () {
    document.querySelectorAll('main table.govuk-table').forEach(function (table) {
        var headRow = table.querySelector('thead tr');
        if (!headRow || table.querySelector('[colspan], [rowspan]')) {
            return;
        }

        var labels = Array.prototype.map.call(headRow.children, function (th) {
            return th.textContent.replace(/\s+/g, ' ').trim();
        });

        table.querySelectorAll('tbody tr').forEach(function (row) {
            Array.prototype.forEach.call(row.children, function (cell, i) {
                if (labels[i] && !cell.hasAttribute('data-label')) {
                    cell.setAttribute('data-label', labels[i]);
                }
            });
        });

        table.classList.add('app-responsive-table');
    });
})();
