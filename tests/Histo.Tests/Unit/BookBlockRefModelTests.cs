using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Web.Pages.Bookings;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="BookBlockRefModel"/>.
///
/// NOTE: <c>OnPostAsync</c> is a documented placeholder in the production code
/// ("Booking logic delegates to BlockService update — placeholder until full
/// workflow confirmed.") — it ignores the posted <c>blockId</c> entirely and never
/// calls any block-booking repository method. The tests below assert the current
/// (incomplete) behaviour; see the accompanying bug report.
/// </summary>
public class BookBlockRefModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBlockService> _blocks = new();
    private readonly Mock<IHistologyRefService> _refs = new();

    public BookBlockRefModelTests()
    {
        _session.SetupProperty(s => s.AnimalID);
    }

    private BookBlockRefModel CreateSut() =>
        new(_session.Object, _blocks.Object, _refs.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
        };

    [Fact]
    public async Task OnGetAsync_NoAnimalIdInSession_DoesNotLoadPreBookedBlocks()
    {
        _session.Object.AnimalID = null;
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Empty(sut.PreBookedBlocks);
        _blocks.Verify(b => b.GetPreBookedByAnimalAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnGetAsync_AnimalIdInSession_LoadsPreBookedBlocks()
    {
        _session.Object.AnimalID = 42;
        _blocks.Setup(b => b.GetPreBookedByAnimalAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[new Block { ID = 1, BlockRef = "01" }]);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Single(sut.PreBookedBlocks);
    }

    [Fact]
    public async Task OnPostAsync_AlwaysRedirectsToBookingMenu_RegardlessOfBlockId()
    {
        // BUG: OnPostAsync never actually books the block — it ignores blockId entirely.
        var sut = CreateSut();

        var result = await sut.OnPostAsync(blockId: 12345);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Bookings/BookingMenu", redirect.PageName);
        _blocks.VerifyNoOtherCalls();
    }
}
