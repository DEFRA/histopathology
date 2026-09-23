using Histo.Reporting.Reports;
using Histo.Reporting.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Histo.Web.Pages.Reports;

/// <summary>
/// PDF endpoint for the Histology Report (replaces <c>SubmissionForm.aspx</c>
/// Crystal Reports export). Returns the generated PDF directly to the browser.
///
/// Route: /Reports/HistologyReport
///
/// Legacy equivalent: <c>SubmissionForm.aspx.vb — Page_Load</c> which called
/// Crystal Reports <c>HistologyReport.rpt</c> (with embedded sub-report
/// <c>HistologySubReport.rpt</c>) and streamed the PDF via <c>Response.WriteFile</c>.
/// </summary>
public class HistologyReportModel : HistoPageModel
{
    private readonly HistologyReportDataSetBuilder _dataSetBuilder;
    private readonly HistologyReportRenderer _renderer;
    private readonly IConfiguration _config;

    public HistologyReportModel(
        ISessionService session,
        HistologyReportDataSetBuilder dataSetBuilder,
        HistologyReportRenderer renderer,
        IConfiguration config)
        : base(session)
    {
        _dataSetBuilder = dataSetBuilder;
        _renderer       = renderer;
        _config         = config;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct = default)
    {
        var batchId = Session.BatchID;
        if (batchId is null or <= 0)
            return RedirectToPage("/Index");

        // Rows-per-page is configurable (appsettings Reporting:HistologyReportRowsPerPage); default 13.
        var rowsPerPage = _config.GetValue<int?>("Reporting:HistologyReportRowsPerPage") ?? 13;

        var ds  = await _dataSetBuilder.BuildAsync(batchId.Value, ct);
        var pdf = await _renderer.RenderAsync(ds, rowsPerPage);

        return File(pdf, "application/pdf", $"HistologyReport-{batchId}.pdf");
    }
}
