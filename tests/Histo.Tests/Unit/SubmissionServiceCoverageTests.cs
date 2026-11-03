using System.Linq.Expressions;
using Histo.Infrastructure;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Submissions.Services;
using Moq;

namespace Histo.Tests.Unit;

public class SubmissionServiceCoverageTests
{
    private readonly Mock<ISubmissionRepository> _repo = new();
    private readonly Mock<IAppLogger> _logger = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static InvalidOperationException Boom => new("boom");

    private SubmissionService Sut() => new(_repo.Object, _logger.Object);

    private void VerifyLogged() =>
        _logger.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<Exception?>(), It.IsAny<object[]>()), Times.AtLeastOnce);

    private async Task AssertListAsync<T>(
        Expression<Func<ISubmissionRepository, Task<IReadOnlyList<T>>>> repoCall,
        Func<SubmissionService, Task<IReadOnlyList<T>>> serviceCall,
        T item)
    {
        IReadOnlyList<T> rows = [item];
        _repo.Setup(repoCall).ReturnsAsync(rows);
        Assert.Same(rows, await serviceCall(Sut()));

        _repo.Setup(repoCall).ThrowsAsync(Boom);
        Assert.Empty(await serviceCall(Sut()));
        VerifyLogged();
    }

    // ---------- submissions ----------

    [Fact]
    public Task GetSubmissionsByBatchAsync_ReturnsRowsOrEmptyOnFailure() =>
        AssertListAsync(r => r.GetSubmissionsByBatchAsync(1, Ct), s => s.GetSubmissionsByBatchAsync(1, Ct), new BatchSubmission());

    [Fact]
    public async Task AddSubmissionAsync_ReturnsIdOrZeroOnFailure()
    {
        var sub = new BatchSubmission();
        _repo.Setup(r => r.AddSubmissionAsync(sub, 2, Ct)).ReturnsAsync(11);
        Assert.Equal(11, await Sut().AddSubmissionAsync(sub, 2, Ct));

        _repo.Setup(r => r.AddSubmissionAsync(sub, 2, Ct)).ThrowsAsync(Boom);
        Assert.Equal(0, await Sut().AddSubmissionAsync(sub, 2, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task CreateMouseRangeAsync_ReturnsResultOrFalseOnFailure()
    {
        _repo.Setup(r => r.CreateMouseRangeAsync(1, 5, "1", "3", 2, Ct)).ReturnsAsync(true);
        Assert.True(await Sut().CreateMouseRangeAsync(1, 5, "1", "3", 2, Ct));

        _repo.Setup(r => r.CreateMouseRangeAsync(1, 5, "1", "3", 2, Ct)).ThrowsAsync(Boom);
        Assert.False(await Sut().CreateMouseRangeAsync(1, 5, "1", "3", 2, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task CreateBatchWithCopiedSamplesAsync_ReturnsIdZeroOrRethrowsInvalidOperation()
    {
        var batch = new Batch();
        var codes = new SelectedTestCodes([], [], []);
        IReadOnlyList<CopiedSamplePlan> plan = [new CopiedSamplePlan
        {
            SourceAnimal = new Animal(),
            NewSenderRef = "N",
            SourceSubmission = new BatchSubmission(),
            Tissues = [],
        }];
        var setup = _repo.Setup(r => r.CreateBatchWithCopiedSamplesAsync(batch, codes, "SA", plan, 2, Ct));

        setup.ReturnsAsync(42);
        Assert.Equal(42, await Sut().CreateBatchWithCopiedSamplesAsync(batch, codes, "SA", plan, 2, Ct));

        setup.ThrowsAsync(new ArgumentException("db"));
        Assert.Equal(0, await Sut().CreateBatchWithCopiedSamplesAsync(batch, codes, "SA", plan, 2, Ct));
        VerifyLogged();

        setup.ThrowsAsync(new InvalidOperationException("dup"));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Sut().CreateBatchWithCopiedSamplesAsync(batch, codes, "SA", plan, 2, Ct));
        Assert.Equal("dup", ex.Message);
    }

    [Fact]
    public async Task CopySubmissionAsync_MapsSourceOntoNewBatch()
    {
        var source = new BatchSubmission { ID = 3, SubmissionName = "Grp", Order = 4 };
        BatchSubmission? captured = null;
        _repo.Setup(r => r.AddSubmissionAsync(It.IsAny<BatchSubmission>(), 2, Ct))
            .Callback<BatchSubmission, int, CancellationToken>((s, _, _) => captured = s)
            .ReturnsAsync(9);

        var id = await Sut().CopySubmissionAsync(source, 77, 2, 6, Ct);

        Assert.Equal(9, id);
        Assert.Equal(77, captured!.BatchID);
        Assert.Equal(6, captured.AnimalID);
        Assert.Equal("Grp", captured.SubmissionName);
        Assert.Equal(4, captured.Order);
    }

    [Fact]
    public async Task CopySubmissionAsync_RepositoryThrows_ReturnsZero()
    {
        _repo.Setup(r => r.AddSubmissionAsync(It.IsAny<BatchSubmission>(), 2, Ct)).ThrowsAsync(Boom);

        Assert.Equal(0, await Sut().CopySubmissionAsync(new BatchSubmission { ID = 3 }, 77, 2, ct: Ct));
        VerifyLogged();
    }

    // ---------- animals ----------

    [Fact]
    public Task GetAnimalsByBatchAsync_ReturnsRowsOrEmptyOnFailure() =>
        AssertListAsync(r => r.GetAnimalsByBatchAsync(1, Ct), s => s.GetAnimalsByBatchAsync(1, Ct), new Animal());

    [Fact]
    public Task GetBlockAnimalsByBatchAsync_ReturnsRowsOrEmptyOnFailure() =>
        AssertListAsync(r => r.GetBlockAnimalsByBatchAsync(1, Ct), s => s.GetBlockAnimalsByBatchAsync(1, Ct), new Animal());

    [Fact]
    public async Task AddAnimalAsync_PassesPmDateAndDefaults()
    {
        Animal? captured = null;
        _repo.Setup(r => r.AddAnimalAsync(It.IsAny<Animal>(), 2, Ct))
            .Callback<Animal, int, CancellationToken>((a, _, _) => captured = a)
            .ReturnsAsync(5);

        var id = await Sut().AddAnimalAsync(8, "S1", 2, "2026-01-01", true, Ct);

        Assert.Equal(5, id);
        Assert.Equal(8, captured!.BatchSubmissionID);
        Assert.Equal("S1", captured.SenderRef);
        Assert.Equal("2026-01-01", captured.PMDate);
        Assert.True(captured.PMDateSet);
    }

    [Fact]
    public async Task CopyAnimalAsync_CopiesStateWithNewSenderRef()
    {
        var source = new Animal
        {
            ID = 1, NextBlockRef = "04", HistoRefSet = true, HistologyRef = "H1", OnHold = true,
            PMDate = "d", PMDateSet = true, IsPGNumber = true, BookedHistologyRef = true,
        };
        Animal? captured = null;
        _repo.Setup(r => r.AddAnimalAsync(It.IsAny<Animal>(), 2, Ct))
            .Callback<Animal, int, CancellationToken>((a, _, _) => captured = a)
            .ReturnsAsync(6);

        Assert.Equal(6, await Sut().CopyAnimalAsync(source, 12, "NEW", 2, Ct));

        Assert.Equal(12, captured!.BatchSubmissionID);
        Assert.Equal("NEW", captured.SenderRef);
        Assert.Equal("04", captured.NextBlockRef);
        Assert.Equal("H1", captured.HistologyRef);
        Assert.True(captured.OnHold);
        Assert.True(captured.IsPGNumber);
        Assert.True(captured.BookedHistologyRef);
    }

    [Fact]
    public async Task CopyAnimalAsync_RepositoryThrows_ReturnsZero()
    {
        _repo.Setup(r => r.AddAnimalAsync(It.IsAny<Animal>(), 2, Ct)).ThrowsAsync(Boom);

        Assert.Equal(0, await Sut().CopyAnimalAsync(new Animal { ID = 1 }, 12, "NEW", 2, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task UpdateAnimalAsync_SuccessAndFailure()
    {
        var animal = new Animal { ID = 1 };
        Assert.True(await Sut().UpdateAnimalAsync(animal, 2, Ct));
        _repo.Verify(r => r.UpdateAnimalAsync(animal, 2, Ct), Times.Once);

        _repo.Setup(r => r.UpdateAnimalAsync(animal, 2, Ct)).ThrowsAsync(Boom);
        Assert.False(await Sut().UpdateAnimalAsync(animal, 2, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task DeleteAnimalAsync_SuccessAndFailure()
    {
        Assert.True(await Sut().DeleteAnimalAsync(1, 2, Ct));
        _repo.Verify(r => r.DeleteAnimalAsync(1, 2, Ct), Times.Once);

        _repo.Setup(r => r.DeleteAnimalAsync(1, 2, Ct)).ThrowsAsync(Boom);
        Assert.False(await Sut().DeleteAnimalAsync(1, 2, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task DeleteSampleFromBatchAsync_SuccessAndFailure()
    {
        Assert.True(await Sut().DeleteSampleFromBatchAsync(1, 2, 3, Ct));
        _repo.Verify(r => r.DeleteSampleFromBatchAsync(1, 2, 3, Ct), Times.Once);

        _repo.Setup(r => r.DeleteSampleFromBatchAsync(1, 2, 3, Ct)).ThrowsAsync(Boom);
        Assert.False(await Sut().DeleteSampleFromBatchAsync(1, 2, 3, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task DeleteSampleSubmissionFromBatchAsync_SuccessAndFailure()
    {
        Assert.True(await Sut().DeleteSampleSubmissionFromBatchAsync(1, 2, 3, Ct));
        _repo.Verify(r => r.DeleteSampleSubmissionFromBatchAsync(1, 2, 3, Ct), Times.Once);

        _repo.Setup(r => r.DeleteSampleSubmissionFromBatchAsync(1, 2, 3, Ct)).ThrowsAsync(Boom);
        Assert.False(await Sut().DeleteSampleSubmissionFromBatchAsync(1, 2, 3, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task AdvanceNextBlockRefAsync_SuccessAndFailure()
    {
        Assert.True(await Sut().AdvanceNextBlockRefAsync(1, "03", Ct));
        _repo.Verify(r => r.AdvanceNextBlockRefAsync(1, "03", Ct), Times.Once);

        _repo.Setup(r => r.AdvanceNextBlockRefAsync(1, "03", Ct)).ThrowsAsync(Boom);
        Assert.False(await Sut().AdvanceNextBlockRefAsync(1, "03", Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task UpdateAnimalSenderRefAsync_DelegatesAndPropagatesFailure()
    {
        await Sut().UpdateAnimalSenderRefAsync("A", "B", 2, Ct);
        _repo.Verify(r => r.UpdateAnimalSenderRefAsync("A", "B", 2, Ct), Times.Once);

        _repo.Setup(r => r.UpdateAnimalSenderRefAsync("A", "B", 2, Ct)).ThrowsAsync(Boom);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Sut().UpdateAnimalSenderRefAsync("A", "B", 2, Ct));
    }

    [Fact]
    public async Task UpdateAnimalHistologyRefAsync_DelegatesAndPropagatesFailure()
    {
        await Sut().UpdateAnimalHistologyRefAsync("A", null, 2, Ct);
        _repo.Verify(r => r.UpdateAnimalHistologyRefAsync("A", null, 2, Ct), Times.Once);

        _repo.Setup(r => r.UpdateAnimalHistologyRefAsync("A", "H", 2, Ct)).ThrowsAsync(Boom);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Sut().UpdateAnimalHistologyRefAsync("A", "H", 2, Ct));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsHistologyRefInUseAsync_ReturnsRepositoryAnswer(bool inUse)
    {
        _repo.Setup(r => r.IsHistologyRefInUseAsync("H1", Ct)).ReturnsAsync(inUse);

        Assert.Equal(inUse, await Sut().IsHistologyRefInUseAsync("H1", Ct));
    }

    // ---------- tissues ----------

    [Fact]
    public Task GetTissuesBySubmissionAsync_ReturnsRowsOrEmptyOnFailure() =>
        AssertListAsync(r => r.GetTissuesBySubmissionAsync(1, 2, Ct), s => s.GetTissuesBySubmissionAsync(1, 2, Ct), new Tissue());

    [Fact]
    public Task GetBatchSubmissionTissuesAsync_ReturnsRowsOrEmptyOnFailure() =>
        AssertListAsync(r => r.GetBatchSubmissionTissuesAsync(1, Ct), s => s.GetBatchSubmissionTissuesAsync(1, Ct), new Tissue());

    [Fact]
    public Task GetTissuesByBatchAsync_ReturnsRowsOrEmptyOnFailure() =>
        AssertListAsync(r => r.GetTissuesByBatchAsync(1, Ct), s => s.GetTissuesByBatchAsync(1, Ct), new Tissue());

    [Fact]
    public Task GetTissuesByBlockAsync_ReturnsRowsOrEmptyOnFailure() =>
        AssertListAsync(r => r.GetTissuesByBlockAsync(1, 2, Ct), s => s.GetTissuesByBlockAsync(1, 2, Ct), new Tissue());

    [Fact]
    public async Task AddTissueAsync_ReturnsIdOrZeroOnFailure()
    {
        var tissue = new Tissue();
        _repo.Setup(r => r.AddTissueAsync(tissue, 2, Ct)).ReturnsAsync(15);
        Assert.Equal(15, await Sut().AddTissueAsync(tissue, 2, Ct));

        _repo.Setup(r => r.AddTissueAsync(tissue, 2, Ct)).ThrowsAsync(Boom);
        Assert.Equal(0, await Sut().AddTissueAsync(tissue, 2, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task CopyTissueAsync_CopiesFieldsButNotArchiveData()
    {
        var source = new Tissue
        {
            ID = 1, Owner = TissueOwner.Block, TissueCode = "BR", NoPieces = 3, Comment = "c",
            ArchiveLocation = "Loc", ArchivedDate = new DateTime(2026, 1, 1), ArchiveComment = "x",
        };
        Tissue? captured = null;
        _repo.Setup(r => r.AddTissueAsync(It.IsAny<Tissue>(), 2, Ct))
            .Callback<Tissue, int, CancellationToken>((t, _, _) => captured = t)
            .ReturnsAsync(20);

        Assert.Equal(20, await Sut().CopyTissueAsync(source, 99, 2, Ct));

        Assert.Equal(99, captured!.OwnerID);
        Assert.Equal(TissueOwner.Block, captured.Owner);
        Assert.Equal("BR", captured.TissueCode);
        Assert.Equal(3, captured.NoPieces);
        Assert.Equal("c", captured.Comment);
        Assert.Null(captured.ArchiveLocation);
        Assert.Null(captured.ArchivedDate);
    }

    [Fact]
    public async Task CopyTissueAsync_RepositoryThrows_ReturnsZero()
    {
        _repo.Setup(r => r.AddTissueAsync(It.IsAny<Tissue>(), 2, Ct)).ThrowsAsync(Boom);

        Assert.Equal(0, await Sut().CopyTissueAsync(new Tissue { ID = 1 }, 99, 2, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task UpdateTissueAsync_SuccessAndFailure()
    {
        var tissue = new Tissue { ID = 1 };
        Assert.True(await Sut().UpdateTissueAsync(tissue, 2, Ct));
        _repo.Verify(r => r.UpdateTissueAsync(tissue, 2, Ct), Times.Once);

        _repo.Setup(r => r.UpdateTissueAsync(tissue, 2, Ct)).ThrowsAsync(Boom);
        Assert.False(await Sut().UpdateTissueAsync(tissue, 2, Ct));
        VerifyLogged();
    }

    [Theory]
    [InlineData(TissueOwner.Submission)]
    [InlineData(TissueOwner.Block)]
    public async Task DeleteTissueAsync_SuccessAndFailure(TissueOwner owner)
    {
        Assert.True(await Sut().DeleteTissueAsync(1, owner, 2, Ct));
        _repo.Verify(r => r.DeleteTissueAsync(1, owner, 2, Ct), Times.Once);

        _repo.Setup(r => r.DeleteTissueAsync(1, owner, 2, Ct)).ThrowsAsync(Boom);
        Assert.False(await Sut().DeleteTissueAsync(1, owner, 2, Ct));
        VerifyLogged();
    }

    // ---------- searches ----------

    [Fact]
    public Task GetByPmDateRangeAsync_ReturnsRowsOrEmptyOnFailure() =>
        AssertListAsync(r => r.GetByPmDateRangeAsync(null, null, Ct), s => s.GetByPmDateRangeAsync(null, null, Ct), new PmDateSearchResult());

    [Fact]
    public Task GetAnimalsBySenderRefAsync_ReturnsRowsOrEmptyOnFailure() =>
        AssertListAsync(r => r.GetAnimalsBySenderRefAsync("S", Ct), s => s.GetAnimalsBySenderRefAsync("S", Ct), new SenderSearchResult());

    [Fact]
    public Task GetAnimalBySenderAsync_ReturnsRowsOrEmptyOnFailure() =>
        AssertListAsync(r => r.GetAnimalBySenderAsync("S", Ct), s => s.GetAnimalBySenderAsync("S", Ct), new SenderSearchResult());

    [Fact]
    public Task GetTissueArchiveAsync_ReturnsRowsOrEmptyOnFailure() =>
        AssertListAsync(r => r.GetTissueArchiveAsync("S", "H", "L", "T", Ct), s => s.GetTissueArchiveAsync("S", "H", "L", "T", Ct), new TissueArchiveInfo());

    [Theory]
    [InlineData("12")]
    [InlineData(null)]
    public async Task GetImportedDataAsync_ReturnsRowsOrEmptyOnFailure(string? table)
    {
        await AssertListAsync(r => r.GetImportedDataAsync(table, Ct), s => s.GetImportedDataAsync(table, Ct), new ImportedDataRow());
    }

    [Fact]
    public Task GetAnimalTissuesAsync_ReturnsRowsOrEmptyOnFailure() =>
        AssertListAsync(r => r.GetAnimalTissuesAsync("S", "H", "T", "P", Ct), s => s.GetAnimalTissuesAsync("S", "H", "T", "P", Ct), new AnimalTissueSearchResult());

    [Fact]
    public Task GetAnimalBlockTissuesAsync_ReturnsRowsOrEmptyOnFailure() =>
        AssertListAsync(r => r.GetAnimalBlockTissuesAsync("S", "H", "T", "P", Ct), s => s.GetAnimalBlockTissuesAsync("S", "H", "T", "P", Ct), new AnimalTissueSearchResult());
}
