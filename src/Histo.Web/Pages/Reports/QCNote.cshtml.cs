using Histo.Reporting.Reports;
using Histo.Reporting.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Histo.Web.Pages.Reports;

/// <summary>
/// PDF endpoint for the QC Note report (replaces <c>QCNoteForm.aspx</c>
/// Crystal Reports export). Returns the generated PDF directly to the browser.
///
/// Route: /Reports/QCNote?qcNoteRef={id}
///
/// Legacy equivalent: <c>QCNoteForm.aspx.vb — CreateReport(iQCNoteRef)</c> which called
/// Crystal Reports <c>QCNote.rpt</c> and streamed the PDF via <c>Response.WriteFile</c>.
/// </summary>
public class QCNoteModel : HistoPageModel
{
    private readonly QCNoteDataSetBuilder _dataSetBuilder;

    public QCNoteModel(
        ISessionService session,
        QCNoteDataSetBuilder dataSetBuilder)
        : base(session)
    {
        _dataSetBuilder = dataSetBuilder;
    }

    public async Task<IActionResult> OnGetAsync(int qcNoteRef, CancellationToken ct = default)
    {
        if (qcNoteRef <= 0)
            return RedirectToPage("/Index");

        var ds  = await _dataSetBuilder.BuildAsync(qcNoteRef, ct);
        var pdf = await QCNoteRenderer.RenderAsync(ds);

        // "inline" makes the browser display the PDF; the filename is used if the user saves it.
        Response.Headers[HeaderNames.ContentDisposition] =
            new ContentDispositionHeaderValue("inline") { FileName = $"QCNote-{qcNoteRef}.pdf" }.ToString();
        return File(pdf, "application/pdf");
    }
}
