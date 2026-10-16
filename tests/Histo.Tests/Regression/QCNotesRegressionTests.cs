using Histo.QualityControl.Interfaces;
using Histo.QualityControl.Models;
using Histo.Web.Pages.QC;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Regression;

/// <summary>
/// Regression suite for the QC notes workflow (Add/Edit/List), chained against a shared
/// <see cref="FakeQCNoteService"/> instance, covering rowstamp-based optimistic concurrency
/// (scenario QC-03 in .github/agents/histo-testing-agent.agent.md §5).
/// </summary>
public class QCNotesRegressionTests
{
    private readonly FakeQCNoteService _qc = new();
    private readonly Mock<ISessionService> _session = new();

    public QCNotesRegressionTests()
    {
        _session.Setup(s => s.UserID).Returns(7);
        _session.Setup(s => s.BatchID).Returns(100);
    }

    private static Microsoft.AspNetCore.Http.DefaultHttpContext NewHttpContext() => new();

    private AddQCNoteModel CreateAdd() => new(_session.Object, _qc)
    {
        PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = NewHttpContext() },
    };

    private EditQCNoteModel CreateEdit() => new(_session.Object, _qc)
    {
        PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = NewHttpContext() },
    };

    private QCNotesModel CreateList()
    {
        var httpContext = NewHttpContext();
        return new(_session.Object, _qc)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = httpContext },
        };
    }

    [Fact]
    public async Task AddThenList_NewNoteAppearsInTheGrid()
    {
        var add = CreateAdd();
        add.Text = "Initial note text";

        var result = await add.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        var list = CreateList();
        await list.OnGetAsync();
        Assert.Contains(list.PagedEntries, n => n.Text == "Initial note text");
    }

    [Fact]
    public async Task AddThenEdit_SavesNewText()
    {
        var add = CreateAdd();
        add.Text = "Original";
        await add.OnPostAsync();
        var noteId = _qc.Notes.Single().QCNoteRef!.Value;

        var edit = CreateEdit();
        edit.NoteId = noteId;
        await edit.OnGetAsync();
        Assert.Equal("Original", edit.Text);

        edit.Text = "Updated text";
        var result = await edit.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Updated text", _qc.Notes.Single().Text);
    }

    [Fact]
    public async Task Edit_ConcurrentSave_KnownGap_SecondSaverSilentlyOverwritesTheFirst()
    {
        // Documents a real, open finding rather than the intended behaviour: EditQCNoteModel.OnPostAsync
        // re-fetches the note (and its RowStamp) from the service immediately before saving instead of
        // carrying the RowStamp captured on the GET through a hidden form field. There is no genuine
        // "page open for minutes while someone else saves" concurrency check as a result — only a
        // microseconds-wide TOCTOU window within one request. EditQualityDataTestModel has the identical
        // pattern. Left unfixed per product decision (2026-10-15) — do not "fix" this test to assert
        // rejection without also fixing EditQCNoteModel.OnPostAsync to bind/compare a posted RowStamp.
        var add = CreateAdd();
        add.Text = "Original";
        await add.OnPostAsync();
        var noteId = _qc.Notes.Single().QCNoteRef!.Value;

        var editorA = CreateEdit();
        editorA.NoteId = noteId;
        await editorA.OnGetAsync();

        var editorB = CreateEdit();
        editorB.NoteId = noteId;
        await editorB.OnGetAsync();

        editorB.Text = "Editor B's change";
        await editorB.OnPostAsync();

        editorA.Text = "Editor A's change";
        var result = await editorA.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result); // no concurrency error raised...
        Assert.Equal("Editor A's change", _qc.Notes.Single().Text); // ...B's save is silently lost
    }

    [Fact]
    public async Task Edit_UnknownNoteId_RedirectsToList()
    {
        var edit = CreateEdit();
        edit.NoteId = 9999;

        var result = await edit.OnGetAsync();

        Assert.IsType<RedirectToPageResult>(result);
        var redirect = (RedirectToPageResult)result;
        Assert.Equal("/QC/QCNotes", redirect.PageName);
    }

    [Fact]
    public async Task UpdateAsync_StaleRowstampPassedDirectly_ServiceLayerDoesRejectIt()
    {
        // The service/repository contract itself does enforce optimistic concurrency correctly —
        // it's only reachable via EditQCNoteModel's own page flow that it's bypassed (see the
        // "KnownGap" test above), because the page never gives the service a stale value to reject.
        var add = CreateAdd();
        add.Text = "Original";
        await add.OnPostAsync();
        var note = _qc.Notes.Single();
        var staleRowStamp = note.RowStamp!;

        await _qc.UpdateAsync(note.QCNoteRef!.Value, "First save", staleRowStamp, userId: 7);

        await Assert.ThrowsAsync<QCNoteConcurrencyException>(() =>
            _qc.UpdateAsync(note.QCNoteRef!.Value, "Second save with the now-stale stamp", staleRowStamp, userId: 7));
    }

    /// <summary>In-memory stand-in for the QC note stored procedures, with real rowstamp semantics so stale-write rejection is genuinely exercised.</summary>
    private sealed class FakeQCNoteService : IQCNoteService
    {
        private int _nextId = 1;
        public List<QCNote> Notes { get; } = [];

        public Task<QCNote?> GetByIdAsync(int qcNoteId, CancellationToken ct = default) =>
            Task.FromResult(Notes.FirstOrDefault(n => n.QCNoteRef == qcNoteId));

        public Task<IReadOnlyList<QCNote>> GetAllAsync(CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<QCNote>)Notes.ToList());

        public Task<int> AddAsync(int submissionId, int createdByUserId, CancellationToken ct = default)
        {
            var id = _nextId++;
            Notes.Add(new QCNote { ID = submissionId, QCNoteRef = id, Text = string.Empty, RowStamp = [1] });
            return Task.FromResult(id);
        }

        public Task UpdateAsync(int qcNoteId, string text, byte[] rowStamp, int userId, CancellationToken ct = default)
        {
            var index = Notes.FindIndex(n => n.QCNoteRef == qcNoteId);
            if (index < 0) return Task.CompletedTask;

            var current = Notes[index];
            if (!(current.RowStamp ?? []).SequenceEqual(rowStamp))
                throw new QCNoteConcurrencyException();

            Notes[index] = new QCNote
            {
                ID = current.ID,
                QCNoteRef = current.QCNoteRef,
                StainRef = current.StainRef,
                ProjectDescription = current.ProjectDescription,
                Species = current.Species,
                Text = text,
                CreatedBy = current.CreatedBy,
                DateCreated = current.DateCreated,
                RowStamp = [(byte)((current.RowStamp?[0] ?? 0) + 1)], // bump the rowstamp, like a real rowversion would
            };
            return Task.CompletedTask;
        }
    }
}
