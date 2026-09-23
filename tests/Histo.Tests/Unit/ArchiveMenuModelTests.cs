using Histo.Web.Pages.Archive;
using Histo.Web.Services;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="ArchiveMenuModel"/>.</summary>
public class ArchiveMenuModelTests
{
    private readonly Mock<ISessionService> _session = new();

    public ArchiveMenuModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.SetupProperty(s => s.ReturnPage, string.Empty);
    }

    private ArchiveMenuModel CreateSut() => new(_session.Object);

    [Fact]
    public void BackLinkPage_NoReturnPageSet_FallsBackToBatchesForArchiving()
    {
        var sut = CreateSut();

        Assert.Equal("/Batches/BatchesForArchiving", sut.BackLinkPage);
    }

    [Fact]
    public void BackLinkPage_ReturnPageSet_UsesReturnPage()
    {
        _session.Object.ReturnPage = "/Search/SearchSubmissions";
        var sut = CreateSut();

        Assert.Equal("/Search/SearchSubmissions", sut.BackLinkPage);
    }

    [Fact]
    public void OnGet_BatchIdInQuery_SetsSessionBatchId()
    {
        var sut = CreateSut();
        sut.BatchId = 42;

        sut.OnGet();

        Assert.Equal(42, _session.Object.BatchID);
    }
}
