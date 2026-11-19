using System.Linq.Expressions;
using Histo.Core.Domain;
using Histo.Infrastructure;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Submissions.Services;
using Moq;

namespace Histo.Tests.Unit;

public class BatchServiceCoverageTests
{
    private readonly Mock<IBatchRepository> _repo = new();
    private readonly Mock<IAppLogger> _logger = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static InvalidOperationException Boom => new("boom");

    private BatchService Sut() => new(_repo.Object, _logger.Object);

    private void VerifyLogged() =>
        _logger.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<Exception?>(), It.IsAny<object[]>()), Times.AtLeastOnce);

    // ---------- list getters ----------

    private async Task AssertListAsync(
        Expression<Func<IBatchRepository, Task<IReadOnlyList<BatchListResult>>>> repoCall,
        Func<BatchService, Task<IReadOnlyList<BatchListResult>>> serviceCall)
    {
        IReadOnlyList<BatchListResult> rows = [new BatchListResult()];
        _repo.Setup(repoCall).ReturnsAsync(rows);
        Assert.Same(rows, await serviceCall(Sut()));

        _repo.Setup(repoCall).ThrowsAsync(Boom);
        Assert.Empty(await serviceCall(Sut()));
        VerifyLogged();
    }

    [Fact]
    public Task GetReceivedAsync_ReturnsRowsOrEmptyOnFailure() =>
        AssertListAsync(r => r.GetReceivedAsync(Ct), s => s.GetReceivedAsync(Ct));

    [Fact]
    public Task GetInProgressAsync_ReturnsRowsOrEmptyOnFailure() =>
        AssertListAsync(r => r.GetInProgressAsync(Ct), s => s.GetInProgressAsync(Ct));

    [Fact]
    public Task GetOnHoldAsync_ReturnsRowsOrEmptyOnFailure() =>
        AssertListAsync(r => r.GetOnHoldAsync(Ct), s => s.GetOnHoldAsync(Ct));

    [Fact]
    public Task GetCompletedAsync_ReturnsRowsOrEmptyOnFailure() =>
        AssertListAsync(r => r.GetCompletedAsync(Ct), s => s.GetCompletedAsync(Ct));

    [Fact]
    public Task GetNotReceivedAsync_ReturnsRowsOrEmptyOnFailure() =>
        AssertListAsync(r => r.GetNotReceivedAsync(Ct), s => s.GetNotReceivedAsync(Ct));

    [Fact]
    public Task GetAllBatchesAsync_ReturnsRowsOrEmptyOnFailure() =>
        AssertListAsync(r => r.GetAllBatchesAsync(Ct), s => s.GetAllBatchesAsync(Ct));

    [Fact]
    public Task GetForDispatchAsync_ReturnsRowsOrEmptyOnFailure() =>
        AssertListAsync(r => r.GetForDispatchAsync(Ct), s => s.GetForDispatchAsync(Ct));

    // ---------- GetById / Add / Update (rethrow) ----------

    [Fact]
    public async Task GetByIdAsync_ReturnsBatchFromRepository()
    {
        var batch = new Batch { ID = 4 };
        _repo.Setup(r => r.GetByIdAsync(4, Ct)).ReturnsAsync(batch);

        Assert.Same(batch, await Sut().GetByIdAsync(4, Ct));
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryThrows_LogsAndRethrows()
    {
        _repo.Setup(r => r.GetByIdAsync(4, Ct)).ThrowsAsync(Boom);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Sut().GetByIdAsync(4, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task AddAsync_ReturnsNewId()
    {
        var batch = new Batch();
        _repo.Setup(r => r.AddAsync(batch, 9, Ct)).ReturnsAsync(55);

        Assert.Equal(55, await Sut().AddAsync(batch, 9, Ct));
    }

    [Fact]
    public async Task AddAsync_RepositoryThrows_LogsAndRethrows()
    {
        _repo.Setup(r => r.AddAsync(It.IsAny<Batch>(), 9, Ct)).ThrowsAsync(Boom);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Sut().AddAsync(new Batch(), 9, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task UpdateAsync_Success_ReturnsTrue()
    {
        var batch = new Batch { ID = 3 };

        Assert.True(await Sut().UpdateAsync(batch, 9, Ct));
        _repo.Verify(r => r.UpdateAsync(batch, 9, Ct), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrows_LogsAndRethrows()
    {
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Batch>(), 9, Ct)).ThrowsAsync(Boom);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Sut().UpdateAsync(new Batch { ID = 3 }, 9, Ct));
        VerifyLogged();
    }

    // ---------- bool-returning commands ----------

    [Fact]
    public async Task SetCustomerReceivedDateAsync_SuccessAndFailure()
    {
        var stamp = new byte[] { 1 };
        var date = new DateTime(2026, 1, 2);

        Assert.True(await Sut().SetCustomerReceivedDateAsync(1, date, stamp, 2, Ct));
        _repo.Verify(r => r.SetCustomerReceivedDateAsync(1, date, stamp, 2, Ct), Times.Once);

        _repo.Setup(r => r.SetCustomerReceivedDateAsync(1, date, stamp, 2, Ct)).ThrowsAsync(Boom);
        Assert.False(await Sut().SetCustomerReceivedDateAsync(1, date, stamp, 2, Ct));
        VerifyLogged();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CompleteBlockAssignmentAsync_SuccessAndFailure(bool allAssigned)
    {
        Assert.True(await Sut().CompleteBlockAssignmentAsync(1, allAssigned, 2, Ct));
        _repo.Verify(r => r.CompleteBlockAssignmentAsync(1, allAssigned, 2, Ct), Times.Once);

        _repo.Setup(r => r.CompleteBlockAssignmentAsync(1, allAssigned, 2, Ct)).ThrowsAsync(Boom);
        Assert.False(await Sut().CompleteBlockAssignmentAsync(1, allAssigned, 2, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task SetCompletedAsync_SuccessAndFailure()
    {
        var date = new DateTime(2026, 3, 4);

        Assert.True(await Sut().SetCompletedAsync(1, date, 2, Ct));
        _repo.Verify(r => r.SetCompletedAsync(1, date, 2, Ct), Times.Once);

        _repo.Setup(r => r.SetCompletedAsync(1, date, 2, Ct)).ThrowsAsync(Boom);
        Assert.False(await Sut().SetCompletedAsync(1, date, 2, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task SetInProgressAsync_SuccessAndFailure()
    {
        Assert.True(await Sut().SetInProgressAsync(1, 2, Ct));
        _repo.Verify(r => r.SetInProgressAsync(1, 2, Ct), Times.Once);

        _repo.Setup(r => r.SetInProgressAsync(1, 2, Ct)).ThrowsAsync(Boom);
        Assert.False(await Sut().SetInProgressAsync(1, 2, Ct));
        VerifyLogged();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetByPassSortAsync_SuccessAndFailure(bool byPass)
    {
        Assert.True(await Sut().SetByPassSortAsync(1, byPass, 2, Ct));
        _repo.Verify(r => r.SetByPassSortAsync(1, byPass, 2, Ct), Times.Once);

        _repo.Setup(r => r.SetByPassSortAsync(1, byPass, 2, Ct)).ThrowsAsync(Boom);
        Assert.False(await Sut().SetByPassSortAsync(1, byPass, 2, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task RefreshAllTissuesAssignedAsync_RepositoryThrows_IsSwallowedAndLogged()
    {
        _repo.Setup(r => r.RefreshAllTissuesAssignedAsync(1, 2, Ct)).ThrowsAsync(Boom);

        await Sut().RefreshAllTissuesAssignedAsync(1, 2, Ct);

        VerifyLogged();
    }

    // ---------- CopyBatchHeader ----------

    [Fact]
    public async Task CopyBatchHeaderAsync_CopiesFieldsAndForcesSubmittedStatus()
    {
        var source = new Batch
        {
            ID = 7,
            Status = BatchStatus.Completed,
            Comments = "c",
            UserAreaCode = 3,
            ProjectContractCode = "PC",
            ContactName = "Contact",
            BatchDate = new DateTime(2025, 5, 6),
            OtherSubmittedArea = null,
        };
        Batch? captured = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<Batch>(), 9, Ct))
            .Callback<Batch, int, CancellationToken>((b, _, _) => captured = b)
            .ReturnsAsync(88);

        var id = await Sut().CopyBatchHeaderAsync(source, 9, Ct);

        Assert.Equal(88, id);
        Assert.NotNull(captured);
        Assert.Equal(BatchStatus.Submitted, captured.Status);
        Assert.Equal(9, captured.SubmittedByUserID);
        Assert.Equal("c", captured.Comments);
        Assert.Equal(3, captured.UserAreaCode);
        Assert.Equal("PC", captured.ProjectContractCode);
        Assert.Equal("Contact", captured.ContactName);
        Assert.Equal(new DateTime(2025, 5, 6), captured.BatchDate);
        Assert.Equal("", captured.OtherSubmittedArea);
    }

    [Fact]
    public async Task CopyBatchHeaderAsync_NoSourceDate_DefaultsToToday()
    {
        Batch? captured = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<Batch>(), 9, Ct))
            .Callback<Batch, int, CancellationToken>((b, _, _) => captured = b)
            .ReturnsAsync(1);

        await Sut().CopyBatchHeaderAsync(new Batch { BatchDate = null, OtherSubmittedArea = "Area" }, 9, Ct);

        Assert.Equal(DateTime.Today, captured!.BatchDate);
        Assert.Equal("Area", captured.OtherSubmittedArea);
    }

    [Fact]
    public async Task CopyBatchHeaderAsync_RepositoryThrows_ReturnsZero()
    {
        _repo.Setup(r => r.AddAsync(It.IsAny<Batch>(), 9, Ct)).ThrowsAsync(Boom);

        Assert.Equal(0, await Sut().CopyBatchHeaderAsync(new Batch { ID = 7 }, 9, Ct));
        VerifyLogged();
    }

    // ---------- search ----------

    [Fact]
    public async Task SearchAsync_ReturnsRowsOrEmptyOnFailure()
    {
        var criteria = new BatchSearchCriteria();
        IReadOnlyList<BatchSearchResult> rows = [new BatchSearchResult()];
        _repo.Setup(r => r.SearchAsync(criteria, Ct)).ReturnsAsync(rows);
        Assert.Same(rows, await Sut().SearchAsync(criteria, Ct));

        _repo.Setup(r => r.SearchAsync(criteria, Ct)).ThrowsAsync(Boom);
        Assert.Empty(await Sut().SearchAsync(criteria, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task GetTestItemRowsAsync_ReturnsRowsOrEmptyOnFailure()
    {
        IReadOnlyList<TestItemRow> rows = [new TestItemRow()];
        _repo.Setup(r => r.GetTestItemRowsAsync("P", 2, Ct)).ReturnsAsync(rows);
        Assert.Same(rows, await Sut().GetTestItemRowsAsync("P", 2, Ct));

        _repo.Setup(r => r.GetTestItemRowsAsync("P", 2, Ct)).ThrowsAsync(Boom);
        Assert.Empty(await Sut().GetTestItemRowsAsync("P", 2, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task GetTestPremiumChargeCountsAsync_ReturnsRowsOrEmptyOnFailure()
    {
        var codes = new TestCodeSelection([], [], []);
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 2, 1);
        IReadOnlyList<TestPremiumChargeCount> rows = [new TestPremiumChargeCount()];
        _repo.Setup(r => r.GetTestPremiumChargeCountsAsync("P", 2, codes, from, to, Ct)).ReturnsAsync(rows);
        Assert.Same(rows, await Sut().GetTestPremiumChargeCountsAsync("P", 2, codes, from, to, Ct));

        _repo.Setup(r => r.GetTestPremiumChargeCountsAsync("P", 2, codes, from, to, Ct)).ThrowsAsync(Boom);
        Assert.Empty(await Sut().GetTestPremiumChargeCountsAsync("P", 2, codes, from, to, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task GetTestPremiumChargeBatchesAsync_ReturnsRowsOrEmptyOnFailure()
    {
        var codes = new TestCodeSelection([], [], []);
        IReadOnlyList<TestPremiumChargeBatchRef> rows = [new TestPremiumChargeBatchRef()];
        _repo.Setup(r => r.GetTestPremiumChargeBatchesAsync("P", 2, codes, null, null, Ct)).ReturnsAsync(rows);
        Assert.Same(rows, await Sut().GetTestPremiumChargeBatchesAsync("P", 2, codes, ct: Ct));

        _repo.Setup(r => r.GetTestPremiumChargeBatchesAsync("P", 2, codes, null, null, Ct)).ThrowsAsync(Boom);
        Assert.Empty(await Sut().GetTestPremiumChargeBatchesAsync("P", 2, codes, ct: Ct));
        VerifyLogged();
    }

    // ---------- FixCompletedDates ----------

    private static IReadOnlyList<TestDispatchStatus> Rows(params (bool Dispatched, DateTime? Date)[] rows) =>
        [.. rows.Select(r => new TestDispatchStatus { Dispatched = r.Dispatched, DispatchedDate = r.Date })];

    private void SetupBatch(int id,
        IReadOnlyList<TestDispatchStatus> histology,
        IReadOnlyList<TestDispatchStatus> stain,
        IReadOnlyList<TestDispatchStatus> antibodies)
    {
        _repo.Setup(r => r.GetHistologyDispatchStatusAsync(id, Ct)).ReturnsAsync(histology);
        _repo.Setup(r => r.GetStainDispatchStatusAsync(id, Ct)).ReturnsAsync(stain);
        _repo.Setup(r => r.GetAntibodiesDispatchStatusAsync(id, Ct)).ReturnsAsync(antibodies);
    }

    [Fact]
    public async Task FixCompletedDatesAsync_AllDispatched_UpdatesWithLatestDate()
    {
        var d1 = new DateTime(2026, 1, 1);
        var d2 = new DateTime(2026, 3, 1);
        var d3 = new DateTime(2026, 2, 1);
        _repo.Setup(r => r.GetBatchIdsLinkedToBlocksAsync(Ct)).ReturnsAsync([1]);
        SetupBatch(1, Rows((true, d1), (true, null)), Rows((true, d2)), Rows((true, d3)));

        var updated = await Sut().FixCompletedDatesAsync(Ct);

        Assert.Equal(1, updated);
        _repo.Verify(r => r.UpdateCompletedDateAsync(1, d2, Ct), Times.Once);
    }

    [Fact]
    public async Task FixCompletedDatesAsync_AnyTestTypeNotDispatched_SkipsBatch()
    {
        var d = new DateTime(2026, 1, 1);
        _repo.Setup(r => r.GetBatchIdsLinkedToBlocksAsync(Ct)).ReturnsAsync([1, 2, 3]);
        SetupBatch(1, Rows((false, null)), Rows((true, d)), Rows((true, d)));
        SetupBatch(2, Rows((true, d)), Rows((false, null)), Rows((true, d)));
        SetupBatch(3, Rows((true, d)), Rows((true, d)), Rows((false, null)));

        var updated = await Sut().FixCompletedDatesAsync(Ct);

        Assert.Equal(0, updated);
        _repo.Verify(r => r.UpdateCompletedDateAsync(It.IsAny<int>(), It.IsAny<DateTime>(), Ct), Times.Never);
    }

    [Fact]
    public async Task FixCompletedDatesAsync_OneBatchFails_ContinuesWithOthers()
    {
        var d = new DateTime(2026, 1, 1);
        _repo.Setup(r => r.GetBatchIdsLinkedToBlocksAsync(Ct)).ReturnsAsync([1, 2]);
        _repo.Setup(r => r.GetHistologyDispatchStatusAsync(1, Ct)).ThrowsAsync(Boom);
        SetupBatch(2, Rows((true, d)), Rows(), Rows());

        var updated = await Sut().FixCompletedDatesAsync(Ct);

        Assert.Equal(1, updated);
        _repo.Verify(r => r.UpdateCompletedDateAsync(2, d, Ct), Times.Once);
        VerifyLogged();
    }

    [Fact]
    public async Task FixCompletedDatesAsync_BatchListFails_ReturnsZero()
    {
        _repo.Setup(r => r.GetBatchIdsLinkedToBlocksAsync(Ct)).ThrowsAsync(Boom);

        Assert.Equal(0, await Sut().FixCompletedDatesAsync(Ct));
        VerifyLogged();
    }

    // ---------- test selections ----------

    [Fact]
    public async Task GetBatchTestSelectionsAsync_ReturnsSelectionsOrEmptyOnFailure()
    {
        var selections = new BatchTestSelections();
        _repo.Setup(r => r.GetBatchTestSelectionsAsync(1, Ct)).ReturnsAsync(selections);
        Assert.Same(selections, await Sut().GetBatchTestSelectionsAsync(1, Ct));

        _repo.Setup(r => r.GetBatchTestSelectionsAsync(1, Ct)).ThrowsAsync(Boom);
        var fallback = await Sut().GetBatchTestSelectionsAsync(1, Ct);
        Assert.NotNull(fallback);
        Assert.NotSame(selections, fallback);
        VerifyLogged();
    }

    [Fact]
    public async Task SaveBatchTestSelectionsAsync_SuccessAndFailure()
    {
        IReadOnlyList<string> h = ["H"];
        IReadOnlyList<string> a = ["A"];
        IReadOnlyList<string> s = ["S"];

        Assert.True(await Sut().SaveBatchTestSelectionsAsync(1, h, a, s, 2, Ct));
        _repo.Verify(r => r.SaveBatchTestSelectionsAsync(1, h, a, s, 2, Ct), Times.Once);

        _repo.Setup(r => r.SaveBatchTestSelectionsAsync(1, h, a, s, 2, Ct)).ThrowsAsync(Boom);
        Assert.False(await Sut().SaveBatchTestSelectionsAsync(1, h, a, s, 2, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task GetSubmittedAsCodeAsync_ReturnsCodeOrNullOnFailure()
    {
        _repo.Setup(r => r.GetSubmittedAsCodeAsync(1, Ct)).ReturnsAsync("X");
        Assert.Equal("X", await Sut().GetSubmittedAsCodeAsync(1, Ct));

        _repo.Setup(r => r.GetSubmittedAsCodeAsync(1, Ct)).ThrowsAsync(Boom);
        Assert.Null(await Sut().GetSubmittedAsCodeAsync(1, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task SaveSubmittedAsAsync_DelegatesAndSwallowsFailure()
    {
        await Sut().SaveSubmittedAsAsync(1, "X", 2, Ct);
        _repo.Verify(r => r.SaveSubmittedAsAsync(1, "X", 2, Ct), Times.Once);

        _repo.Setup(r => r.SaveSubmittedAsAsync(1, "X", 2, Ct)).ThrowsAsync(Boom);
        await Sut().SaveSubmittedAsAsync(1, "X", 2, Ct);
        VerifyLogged();
    }

    [Fact]
    public async Task GetPostFixationCodesAsync_ReturnsCodesOrEmptyOnFailure()
    {
        IReadOnlyList<string> codes = ["P1"];
        _repo.Setup(r => r.GetPostFixationCodesAsync(1, Ct)).ReturnsAsync(codes);
        Assert.Same(codes, await Sut().GetPostFixationCodesAsync(1, Ct));

        _repo.Setup(r => r.GetPostFixationCodesAsync(1, Ct)).ThrowsAsync(Boom);
        Assert.Empty(await Sut().GetPostFixationCodesAsync(1, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task SavePostFixationCodesAsync_SuccessAndFailure()
    {
        IReadOnlyList<string> codes = ["P1"];

        Assert.True(await Sut().SavePostFixationCodesAsync(1, codes, 2, Ct));
        _repo.Verify(r => r.SavePostFixationCodesAsync(1, codes, 2, Ct), Times.Once);

        _repo.Setup(r => r.SavePostFixationCodesAsync(1, codes, 2, Ct)).ThrowsAsync(Boom);
        Assert.False(await Sut().SavePostFixationCodesAsync(1, codes, 2, Ct));
        VerifyLogged();
    }
}
