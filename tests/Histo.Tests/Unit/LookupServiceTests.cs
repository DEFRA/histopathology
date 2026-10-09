using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Administration.Services;
using Histo.Infrastructure;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="LookupService"/> — a thin try/catch wrapper over
/// <see cref="ILookupRepository"/>. Every public method follows the same shape (delegate to the
/// repository; on exception, log and return a safe default), so each is tested for both the
/// happy path (delegates and returns the repository's result) and the exception path (catches,
/// logs via <see cref="IAppLogger.LogError"/>, and returns the safe default instead of throwing).
/// </summary>
public class LookupServiceTests
{
    private readonly Mock<ILookupRepository> _repo = new();
    private readonly Mock<IAppLogger> _logger = new();

    private LookupService CreateSut() => new(_repo.Object, _logger.Object);

    private static readonly IReadOnlyList<LookupItem> SampleLookupItems =
        [new LookupItem { ID = 1, Code = "A", Name = "Alpha" }];

    // ── GetLookupDataAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetLookupDataAsync_RepositorySucceeds_ReturnsRepositoryResult()
    {
        _repo.Setup(r => r.GetLookupDataAsync(11, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleLookupItems);

        var result = await CreateSut().GetLookupDataAsync(11, includeInactive: true);

        Assert.Same(SampleLookupItems, result);
    }

    [Fact]
    public async Task GetLookupDataAsync_RepositoryThrows_LogsAndReturnsEmptyList()
    {
        _repo.Setup(r => r.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await CreateSut().GetLookupDataAsync(11);

        Assert.Empty(result);
        _logger.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<object[]>()), Times.Once);
    }

    // ── GetUserAreaDataAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserAreaDataAsync_RepositorySucceeds_ReturnsRepositoryResult()
    {
        _repo.Setup(r => r.GetUserAreaDataAsync(11, "Histopath", It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleLookupItems);

        var result = await CreateSut().GetUserAreaDataAsync(11, "Histopath");

        Assert.Same(SampleLookupItems, result);
    }

    [Fact]
    public async Task GetUserAreaDataAsync_RepositoryThrows_LogsAndReturnsEmptyList()
    {
        _repo.Setup(r => r.GetUserAreaDataAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await CreateSut().GetUserAreaDataAsync(11, "Histopath");

        Assert.Empty(result);
        _logger.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<object[]>()), Times.Once);
    }

    // ── ListEditableLookupsAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task ListEditableLookupsAsync_RepositorySucceeds_ReturnsRepositoryResult()
    {
        IReadOnlyList<EditableLookup> expected = [new EditableLookup { ID = 1, TableName = "Species" }];
        _repo.Setup(r => r.ListEditableLookupsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await CreateSut().ListEditableLookupsAsync();

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task ListEditableLookupsAsync_RepositoryThrows_LogsAndReturnsEmptyList()
    {
        _repo.Setup(r => r.ListEditableLookupsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await CreateSut().ListEditableLookupsAsync();

        Assert.Empty(result);
        _logger.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
    }

    // ── GetContactsByAreaAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetContactsByAreaAsync_RepositorySucceeds_ReturnsRepositoryResult()
    {
        _repo.Setup(r => r.GetContactsByAreaAsync("Histopath", It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleLookupItems);

        var result = await CreateSut().GetContactsByAreaAsync("Histopath");

        Assert.Same(SampleLookupItems, result);
    }

    [Fact]
    public async Task GetContactsByAreaAsync_RepositoryThrows_LogsAndReturnsEmptyList()
    {
        _repo.Setup(r => r.GetContactsByAreaAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await CreateSut().GetContactsByAreaAsync("Histopath");

        Assert.Empty(result);
        _logger.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<object[]>()), Times.Once);
    }

    // ── GetProjectsByAreaAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetProjectsByAreaAsync_RepositorySucceeds_ReturnsRepositoryResult()
    {
        _repo.Setup(r => r.GetProjectsByAreaAsync("Histopath", It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleLookupItems);

        var result = await CreateSut().GetProjectsByAreaAsync("Histopath");

        Assert.Same(SampleLookupItems, result);
    }

    [Fact]
    public async Task GetProjectsByAreaAsync_RepositoryThrows_LogsAndReturnsEmptyList()
    {
        _repo.Setup(r => r.GetProjectsByAreaAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await CreateSut().GetProjectsByAreaAsync("Histopath");

        Assert.Empty(result);
        _logger.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<object[]>()), Times.Once);
    }

    // ── GetUserGroupsAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserGroupsAsync_RepositorySucceeds_ReturnsRepositoryResult()
    {
        _repo.Setup(r => r.GetUserGroupsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SampleLookupItems);

        var result = await CreateSut().GetUserGroupsAsync();

        Assert.Same(SampleLookupItems, result);
    }

    [Fact]
    public async Task GetUserGroupsAsync_RepositoryThrows_LogsAndReturnsEmptyList()
    {
        _repo.Setup(r => r.GetUserGroupsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await CreateSut().GetUserGroupsAsync();

        Assert.Empty(result);
        _logger.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
    }

    // ── GetUserAreasAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserAreasAsync_RepositorySucceeds_ReturnsRepositoryResult()
    {
        _repo.Setup(r => r.GetUserAreasAsync(true, It.IsAny<CancellationToken>())).ReturnsAsync(SampleLookupItems);

        var result = await CreateSut().GetUserAreasAsync(includeInactive: true);

        Assert.Same(SampleLookupItems, result);
    }

    [Fact]
    public async Task GetUserAreasAsync_RepositoryThrows_LogsAndReturnsEmptyList()
    {
        _repo.Setup(r => r.GetUserAreasAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await CreateSut().GetUserAreasAsync();

        Assert.Empty(result);
        _logger.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
    }

    // ── GetImportedTablesAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetImportedTablesAsync_RepositorySucceeds_ReturnsRepositoryResult()
    {
        _repo.Setup(r => r.GetImportedTablesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SampleLookupItems);

        var result = await CreateSut().GetImportedTablesAsync();

        Assert.Same(SampleLookupItems, result);
    }

    [Fact]
    public async Task GetImportedTablesAsync_RepositoryThrows_LogsAndReturnsEmptyList()
    {
        _repo.Setup(r => r.GetImportedTablesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await CreateSut().GetImportedTablesAsync();

        Assert.Empty(result);
        _logger.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
    }

    // ── GetSpeciesLookupAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetSpeciesLookupAsync_RepositorySucceeds_ReturnsRepositoryResult()
    {
        _repo.Setup(r => r.GetSpeciesLookupAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SampleLookupItems);

        var result = await CreateSut().GetSpeciesLookupAsync();

        Assert.Same(SampleLookupItems, result);
    }

    [Fact]
    public async Task GetSpeciesLookupAsync_RepositoryThrows_LogsAndReturnsEmptyList()
    {
        _repo.Setup(r => r.GetSpeciesLookupAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await CreateSut().GetSpeciesLookupAsync();

        Assert.Empty(result);
        _logger.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
    }

    // ── GetHistologyTypesAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetHistologyTypesAsync_RepositorySucceeds_ReturnsRepositoryResult()
    {
        _repo.Setup(r => r.GetHistologyTypesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SampleLookupItems);

        var result = await CreateSut().GetHistologyTypesAsync();

        Assert.Same(SampleLookupItems, result);
    }

    [Fact]
    public async Task GetHistologyTypesAsync_RepositoryThrows_LogsAndReturnsEmptyList()
    {
        _repo.Setup(r => r.GetHistologyTypesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await CreateSut().GetHistologyTypesAsync();

        Assert.Empty(result);
        _logger.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
    }

    // ── GetPremiumChargesAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetPremiumChargesAsync_RepositorySucceeds_ReturnsRepositoryResult()
    {
        _repo.Setup(r => r.GetPremiumChargesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SampleLookupItems);

        var result = await CreateSut().GetPremiumChargesAsync();

        Assert.Same(SampleLookupItems, result);
    }

    [Fact]
    public async Task GetPremiumChargesAsync_RepositoryThrows_LogsAndReturnsEmptyList()
    {
        _repo.Setup(r => r.GetPremiumChargesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await CreateSut().GetPremiumChargesAsync();

        Assert.Empty(result);
        _logger.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
    }

    // ── GetHistologyRefTypesAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetHistologyRefTypesAsync_RepositorySucceeds_ReturnsRepositoryResult()
    {
        _repo.Setup(r => r.GetHistologyRefTypesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SampleLookupItems);

        var result = await CreateSut().GetHistologyRefTypesAsync();

        Assert.Same(SampleLookupItems, result);
    }

    [Fact]
    public async Task GetHistologyRefTypesAsync_RepositoryThrows_LogsAndReturnsEmptyList()
    {
        _repo.Setup(r => r.GetHistologyRefTypesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await CreateSut().GetHistologyRefTypesAsync();

        Assert.Empty(result);
        _logger.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
    }

    // ── CreateLookupItemAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateLookupItemAsync_RepositorySucceeds_ReturnsTrue()
    {
        var item = new LookupItem { ID = 0, Code = "A", Name = "Alpha" };
        _repo.Setup(r => r.CreateLookupItemAsync(11, item, 7, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await CreateSut().CreateLookupItemAsync(11, item, 7);

        Assert.True(result);
    }

    [Fact]
    public async Task CreateLookupItemAsync_RepositoryThrows_LogsAndReturnsFalse()
    {
        var item = new LookupItem { ID = 0, Code = "A", Name = "Alpha" };
        _repo.Setup(r => r.CreateLookupItemAsync(It.IsAny<int>(), It.IsAny<LookupItem>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await CreateSut().CreateLookupItemAsync(11, item, 7);

        Assert.False(result);
        _logger.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<object[]>()), Times.Once);
    }

    // ── UpdateLookupItemAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateLookupItemAsync_RepositorySucceeds_ReturnsTrue()
    {
        var item = new LookupItem { ID = 1, Code = "A", Name = "Alpha" };
        _repo.Setup(r => r.UpdateLookupItemAsync(11, item, 7, "A", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await CreateSut().UpdateLookupItemAsync(11, item, 7, "A");

        Assert.True(result);
    }

    [Fact]
    public async Task UpdateLookupItemAsync_RepositoryThrows_LogsAndReturnsFalse()
    {
        var item = new LookupItem { ID = 1, Code = "A", Name = "Alpha" };
        _repo.Setup(r => r.UpdateLookupItemAsync(It.IsAny<int>(), It.IsAny<LookupItem>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await CreateSut().UpdateLookupItemAsync(11, item, 7);

        Assert.False(result);
        _logger.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<object[]>()), Times.Once);
    }

    // ── GetSpeciesItemsAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetSpeciesItemsAsync_RepositorySucceeds_ReturnsRepositoryResult()
    {
        IReadOnlyList<SpeciesItem> expected = [new SpeciesItem { SpeciesID = 1, Species = "Bovine" }];
        _repo.Setup(r => r.GetSpeciesItemsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await CreateSut().GetSpeciesItemsAsync();

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetSpeciesItemsAsync_RepositoryThrows_LogsAndReturnsEmptyList()
    {
        _repo.Setup(r => r.GetSpeciesItemsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await CreateSut().GetSpeciesItemsAsync();

        Assert.Empty(result);
        _logger.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
    }

    // ── AddSpeciesItemAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task AddSpeciesItemAsync_RepositorySucceeds_ReturnsTrue()
    {
        _repo.Setup(r => r.AddSpeciesItemAsync(1, "Bovine", "Cattle", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await CreateSut().AddSpeciesItemAsync(1, "Bovine", "Cattle");

        Assert.True(result);
    }

    [Fact]
    public async Task AddSpeciesItemAsync_RepositoryThrows_LogsAndReturnsFalse()
    {
        _repo.Setup(r => r.AddSpeciesItemAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await CreateSut().AddSpeciesItemAsync(1, "Bovine", "Cattle");

        Assert.False(result);
        _logger.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<object[]>()), Times.Once);
    }

    // ── UpdateSpeciesItemAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task UpdateSpeciesItemAsync_RepositorySucceeds_ReturnsTrue()
    {
        _repo.Setup(r => r.UpdateSpeciesItemAsync(1, "Bovine", "Cattle", 7, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await CreateSut().UpdateSpeciesItemAsync(1, "Bovine", "Cattle", 7);

        Assert.True(result);
    }

    [Fact]
    public async Task UpdateSpeciesItemAsync_RepositoryThrows_LogsAndReturnsFalse()
    {
        _repo.Setup(r => r.UpdateSpeciesItemAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await CreateSut().UpdateSpeciesItemAsync(1, "Bovine", "Cattle", 7);

        Assert.False(result);
        _logger.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<object[]>()), Times.Once);
    }
}
