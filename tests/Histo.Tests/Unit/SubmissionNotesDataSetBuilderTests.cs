using Histo.Reporting.Services;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for the <c>internal static HasContent</c> helper extracted from
/// <see cref="SubmissionNotesDataSetBuilder"/>.
///
/// <c>HasContent</c> encapsulates the row-filter rule from the legacy
/// <c>SubmissionNotes.aspx.vb</c>: only include a row when at least one of
/// Comment or ArchiveComment is non-empty and non-whitespace.
/// </summary>
public class SubmissionNotesDataSetBuilderTests
{
    private static Dictionary<string, object> Row(string comment, string archiveComment)
        => new(StringComparer.OrdinalIgnoreCase)
        {
            ["Comment"]        = comment,
            ["ArchiveComment"] = archiveComment
        };

    // ── HasContent ───────────────────────────────────────────────────────────

    [Fact]
    public void HasContent_BothEmpty_ReturnsFalse()
    {
        var src = Row("", "");
        Assert.False(SubmissionNotesDataSetBuilder.HasContent(src, "Comment", "ArchiveComment"));
    }

    [Fact]
    public void HasContent_CommentNonEmpty_ReturnsTrue()
    {
        var src = Row("Some comment", "");
        Assert.True(SubmissionNotesDataSetBuilder.HasContent(src, "Comment", "ArchiveComment"));
    }

    [Fact]
    public void HasContent_ArchiveCommentNonEmpty_ReturnsTrue()
    {
        var src = Row("", "Some archive comment");
        Assert.True(SubmissionNotesDataSetBuilder.HasContent(src, "Comment", "ArchiveComment"));
    }

    [Fact]
    public void HasContent_BothNonEmpty_ReturnsTrue()
    {
        var src = Row("Comment value", "Archive value");
        Assert.True(SubmissionNotesDataSetBuilder.HasContent(src, "Comment", "ArchiveComment"));
    }

    [Fact]
    public void HasContent_WhitespaceOnlyComment_ReturnsFalse()
    {
        // Trim() is applied — whitespace-only is treated the same as empty
        var src = Row("   ", "");
        Assert.False(SubmissionNotesDataSetBuilder.HasContent(src, "Comment", "ArchiveComment"));
    }

    [Fact]
    public void HasContent_WhitespaceOnlyArchiveComment_ReturnsFalse()
    {
        var src = Row("", "\t\n  ");
        Assert.False(SubmissionNotesDataSetBuilder.HasContent(src, "Comment", "ArchiveComment"));
    }

    [Fact]
    public void HasContent_MissingCommentKey_ReturnsFalse()
    {
        // When the key is absent Str() returns "" → both sides empty → false
        var src = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["ArchiveComment"] = ""
        };
        Assert.False(SubmissionNotesDataSetBuilder.HasContent(src, "Comment", "ArchiveComment"));
    }

    [Fact]
    public void HasContent_DbNullCommentValue_ReturnsFalse()
    {
        // DBNull.Value in dictionary → Str() returns "" → false
        var src = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["Comment"]        = DBNull.Value,
            ["ArchiveComment"] = DBNull.Value
        };
        Assert.False(SubmissionNotesDataSetBuilder.HasContent(src, "Comment", "ArchiveComment"));
    }
}
