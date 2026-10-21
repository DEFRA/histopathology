using Histo.Histology.Models;
using Histo.QualityControl.Models;
using Histo.Web.Pages.QC;

namespace Histo.Tests.Unit;

public class QCNoteTextTests
{
    [Fact]
    public void Line_IncludesTestTypeNameRefsAndTrimmedComment()
    {
        var test = new BlockTest { TestType = "Histology", Code = "2", HistologyRef = "26/00001", BlockRef = "03" };

        var line = QCNoteText.Line(test, "H&E", "  needs repeat  ");

        Assert.Equal("Histology - H&E (26/00001 / 03): needs repeat", line);
    }

    [Theory]
    [InlineData(QCNoteAppendResult.TooLong, "QC note 12 is full, so the comments were not added to it. Edit the note to make room.")]
    [InlineData(QCNoteAppendResult.Concurrency, "QC note 12 was changed by another user, so the comments were not added to it.")]
    [InlineData(QCNoteAppendResult.NotFound, "QC note 12 could not be found, so the comments were not added to it.")]
    public void AppendWarning_FailedOutcome_ExplainsWhyNothingWasAdded(QCNoteAppendResult outcome, string expected)
    {
        Assert.Equal(expected, QCNoteText.AppendWarning(outcome, 12));
    }

    [Theory]
    [InlineData(QCNoteAppendResult.Appended)]
    [InlineData(QCNoteAppendResult.NothingToAppend)]
    public void AppendWarning_SuccessfulOutcome_ReturnsNull(QCNoteAppendResult outcome)
    {
        Assert.Null(QCNoteText.AppendWarning(outcome, 12));
    }
}
