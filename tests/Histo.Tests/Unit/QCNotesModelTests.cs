using Histo.QualityControl.Interfaces;
using Histo.QualityControl.Models;
using Histo.Web.Pages.QC;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="QCNotesModel"/>.</summary>
public class QCNotesModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IQCNoteService> _qc = new();

    private QCNotesModel CreateSut() =>
        new(_session.Object, _qc.Object)
        {
            PageContext = new PageContext
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            },
        };

    [Fact]
    public async Task OnGetAsync_NoSortColumn_DefaultsToIdDescending()
    {
        _qc.Setup(q => q.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<QCNote>)[
                new QCNote { ID = 1 }, new QCNote { ID = 3 }, new QCNote { ID = 2 }]);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Equal([3, 2, 1], sut.PagedEntries.Select(n => n.ID));
    }
}
