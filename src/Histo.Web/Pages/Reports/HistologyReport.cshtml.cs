using Histo.Reporting.Reports;
using Histo.Reporting.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

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

    public HistologyReportModel(
        ISessionService session,
        HistologyReportDataSetBuilder dataSetBuilder,
        HistologyReportRenderer renderer)
        : base(session)
    {
        _dataSetBuilder = dataSetBuilder;
        _renderer       = renderer;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct = default)
    {
        var batchId = Session.BatchID;
        if (batchId is null or <= 0)
            return RedirectToPage("/Index");

        var ds  = await _dataSetBuilder.BuildAsync(batchId.Value, ct);
        var pdf = await _renderer.RenderAsync(ds);

        return File(pdf, "application/pdf", $"HistologyReport-{batchId}.pdf");
    }
}
