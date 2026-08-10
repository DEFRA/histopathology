using Histo.Reporting.Reports;
using Histo.Reporting.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Reports;

/// <summary>
/// PDF endpoint for the QC Note report (replaces <c>QCNoteForm.aspx</c>
/// Crystal Reports export). Returns the generated PDF directly to the browser.
///
/// Route: /Reports/QCNote?qcNoteRef={id}
/// Requires an authenticated session.
///
/// Legacy equivalent: <c>QCNoteForm.aspx.vb — CreateReport(iQCNoteRef)</c> which called
/// Crystal Reports <c>QCNote.rpt</c> and streamed the PDF via <c>Response.WriteFile</c>.
/// </summary>
[Authorize]
public class QCNoteModel : HistoPageModel
{
    private readonly QCNoteDataSetBuilder _dataSetBuilder;
    private readonly QCNoteRenderer _renderer;

    public QCNoteModel(
        ISessionService session,
        QCNoteDataSetBuilder dataSetBuilder,
        QCNoteRenderer renderer)
        : base(session)
    {
        _dataSetBuilder = dataSetBuilder;
        _renderer       = renderer;
    }

    public async Task<IActionResult> OnGetAsync(int qcNoteRef, CancellationToken ct = default)
    {
        if (qcNoteRef <= 0)
            return RedirectToPage("/Index");

        var ds  = await _dataSetBuilder.BuildAsync(qcNoteRef, ct);
        var pdf = await _renderer.RenderAsync(ds);

        return File(pdf, "application/pdf", $"QCNote-{qcNoteRef}.pdf");
    }
}
