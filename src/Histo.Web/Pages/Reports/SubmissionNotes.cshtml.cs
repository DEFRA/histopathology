using Histo.Reporting.Reports;
using Histo.Reporting.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Histo.Web.Pages.Reports;

/// <summary>
/// PDF endpoint for the Submission Notes report (replaces <c>SubmissionNotes.aspx</c>
/// Crystal Reports export). Returns the generated PDF directly to the browser.
///
/// Route: /Reports/SubmissionNotes
///
/// Legacy equivalent: <c>SubmissionNotes.aspx.vb — CreateReport(iBatchID)</c> which called
/// Crystal Reports <c>SubmissionNotesReport.rpt</c> and streamed the PDF via <c>Response.WriteFile</c>.
/// </summary>
public class SubmissionNotesModel : HistoPageModel
{
    private readonly SubmissionNotesDataSetBuilder _dataSetBuilder;
    private readonly SubmissionNotesRenderer _renderer;

    public SubmissionNotesModel(
        ISessionService session,
        SubmissionNotesDataSetBuilder dataSetBuilder,
        SubmissionNotesRenderer renderer)
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

        // "inline" makes the browser display the PDF; the filename is used if the user saves it.
        Response.Headers[HeaderNames.ContentDisposition] =
            new ContentDispositionHeaderValue("inline") { FileName = $"SubmissionNotes-{batchId}.pdf" }.ToString();
        return File(pdf, "application/pdf");
    }
}
