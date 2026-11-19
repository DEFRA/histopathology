using Histo.Reporting.Reports;
using Histo.Reporting.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;

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
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class HistologyReportModel : HistoPageModel
{
    private readonly HistologyReportDataSetBuilder _dataSetBuilder;
    private readonly IConfiguration _config;

    public HistologyReportModel(
        ISessionService session,
        HistologyReportDataSetBuilder dataSetBuilder,
        IConfiguration config)
        : base(session)
    {
        _dataSetBuilder = dataSetBuilder;
        _config         = config;
    }

    // In the URL so a refreshed report tab keeps showing the batch it was opened for.
    [BindProperty(SupportsGet = true)] public int? BatchId { get; set; }

    protected override int? RequestedBatchId => BatchId is > 0 ? BatchId : Session.BatchID;

    public async Task<IActionResult> OnGetAsync(CancellationToken ct = default)
    {
        var batchId = RequestedBatchId;
        if (batchId is null or <= 0)
            return RedirectToPage("/Index");

        // Rows-per-page is configurable (appsettings Reporting:HistologyReportRowsPerPage); default 13.
        var rowsPerPage = _config.GetValue<int?>("Reporting:HistologyReportRowsPerPage") ?? 13;

        var ds  = await _dataSetBuilder.BuildAsync(batchId.Value, ct);
        var pdf = await HistologyReportRenderer.RenderAsync(ds, rowsPerPage);

        // "inline" makes the browser display the PDF; the filename is used if the user saves it.
        Response.Headers[HeaderNames.ContentDisposition] =
            new ContentDispositionHeaderValue("inline") { FileName = $"HistologyReport-{batchId}.pdf" }.ToString();
        return File(pdf, "application/pdf");
    }
}
