Partial Class SubmissionNotes1
    Inherits System.Web.UI.Page

#Region " Web Form Designer Generated Code "

    'This call is required by the Web Form Designer.
    <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()

    End Sub

    'NOTE: The following placeholder declaration is required by the Web Form Designer.
    'Do not delete or move it.
    Private designerPlaceholderDeclaration As System.Object

    Private Sub Page_Init(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Init
        'CODEGEN: This method call is required by the Web Form Designer
        'Do not modify it using the code editor.
        InitializeComponent()
    End Sub

#End Region

    Const BATCH_COMMENTS As Integer = 0
    Const BATCH_TISSUE_COMMENTS As Integer = 1
    Const BATCH_BLOCK_COMMENTS As Integer = 2
    Const BATCH_BLOCK_ANTIBODIES_COMMENTS As Integer = 3
    Const BATCH_BLOCK_HISTOLOGY_COMMENTS As Integer = 4
    Const BATCH_BLOCK_STAIN_COMMENTS As Integer = 5


    Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        If Not IsPostBack Then
            Dim iBatchID As Integer = CInt(Session.Item(SessionVars.SV_BatchID))

            CreateReport(iBatchID)
        End If
    End Sub

    Private Sub CreateReport(ByVal iBatchID As Integer)
        Try
            Dim rptBatchNotes As New SubmissionNotesReport
            Dim objBatch As New HistopathologyLib.clsBatch
            Dim dsCommentsDataSet As DataSet
            Dim dsReportDataset As New DataSet


            If Not objBatch.GetBatchComments(iBatchID, dsCommentsDataSet) Then
                Throw New Exception("Batch.GetBatchComments returned false.")
            End If

            CreateSubmissionLevelNotes(dsReportDataset, dsCommentsDataSet.Tables(BATCH_COMMENTS))

            CreateSubmissionTissesNotes(dsReportDataset, dsCommentsDataSet.Tables(BATCH_TISSUE_COMMENTS))

            CreateSubmissionBlockNotes(dsReportDataset, dsCommentsDataSet.Tables(BATCH_BLOCK_COMMENTS))

            CreateSubmissionAntibodiesNotes(dsReportDataset, dsCommentsDataSet.Tables(BATCH_BLOCK_ANTIBODIES_COMMENTS))

            CreateSubmissionHistologyNotes(dsReportDataset, dsCommentsDataSet.Tables(BATCH_BLOCK_HISTOLOGY_COMMENTS))

            CreateSubmissionStainNotes(dsReportDataset, dsCommentsDataSet.Tables(BATCH_BLOCK_STAIN_COMMENTS))

            rptBatchNotes.SetDataSource(dsReportDataset)

            'Printing code (export to PDF)
            Dim crExportOptions As CrystalDecisions.Shared.ExportOptions
            Dim crDiskFileDestinationOptions As CrystalDecisions.Shared.DiskFileDestinationOptions
            Dim Fname As String

            Fname = System.Configuration.ConfigurationSettings.AppSettings("Exports") & Session.SessionID.ToString & "SubmissionNotes" & CStr(iBatchID) & ".pdf"
            crDiskFileDestinationOptions = New CrystalDecisions.Shared.DiskFileDestinationOptions
            crDiskFileDestinationOptions.DiskFileName = Fname
            crExportOptions = rptBatchNotes.ExportOptions
            With crExportOptions
                .DestinationOptions = crDiskFileDestinationOptions
                .ExportDestinationType = CrystalDecisions.[Shared].ExportDestinationType.DiskFile
                .ExportFormatType = CrystalDecisions.[Shared].ExportFormatType.PortableDocFormat
            End With
            rptBatchNotes.Export()

            'Display report in browser
            With Response
                .ClearContent()
                .ClearHeaders()
                .ContentType = "application/pdf"
                .WriteFile(Fname)
                .Flush()
                .Close()
            End With

            'Delete the report once it is displayed in the browser
            System.IO.File.Delete(Fname)
        Catch ex As Exception
            clsAppError.DisplayError("Unable to create Submission notes report for submission " & CStr(iBatchID) & ".", ex)
        End Try
    End Sub

    Private Sub CreateSubmissionLevelNotes(ByRef dsReportDataset As DataSet, ByVal dtBatchComments As DataTable)

        Dim dtSubmissionNotes As New DataTable("Submission")
        Dim drSubmissionRow As DataRow

        dtSubmissionNotes.Columns.Add("SubmissionNumber")
        dtSubmissionNotes.Columns.Add("SubmissionComments")
        dtSubmissionNotes.Columns.Add("SubmissionStatusComment")

        If dtBatchComments.Rows.Count > 0 Then
            drSubmissionRow = dtSubmissionNotes.NewRow
            drSubmissionRow("SubmissionNumber") = dtBatchComments.Rows(0)("ID").ToString
            drSubmissionRow("SubmissionComments") = dtBatchComments.Rows(0)("Comments").ToString
            drSubmissionRow("SubmissionStatusComment") = dtBatchComments.Rows(0)("StatusComments").ToString
            dtSubmissionNotes.Rows.Add(drSubmissionRow)
        End If

        dsReportDataset.Tables.Add(dtSubmissionNotes)
    End Sub

    Private Sub CreateSubmissionTissesNotes(ByRef dsReportDataSet As DataSet, ByVal dtBatchTissueComments As DataTable)

        Dim dtSubmissionTissues As New DataTable("SubmissionTissues")
        Dim drSubmissionTissuesRow As DataRow
        Dim drNewTissuesRow As DataRow

        dtSubmissionTissues.Columns.Add("TissueCode")
        dtSubmissionTissues.Columns.Add("TissueComment")
        dtSubmissionTissues.Columns.Add("TissueArchiveComment")
        dtSubmissionTissues.Columns.Add("SenderRef")

        For Each drSubmissionTissuesRow In dtBatchTissueComments.Rows
            If drSubmissionTissuesRow("Comment").ToString.Trim <> "" Or drSubmissionTissuesRow("ArchiveComment").ToString.Trim <> "" Then
                drNewTissuesRow = dtSubmissionTissues.NewRow
                drNewTissuesRow("TissueCode") = drSubmissionTissuesRow("TissueCode").ToString
                drNewTissuesRow("TissueComment") = drSubmissionTissuesRow("Comment").ToString
                drNewTissuesRow("TissueArchiveComment") = drSubmissionTissuesRow("ArchiveComment").ToString
                drNewTissuesRow("SenderRef") = drSubmissionTissuesRow("SenderRef").ToString
                dtSubmissionTissues.Rows.Add(drNewTissuesRow)
            End If
        Next

        dsReportDataSet.Tables.Add(dtSubmissionTissues)

    End Sub

    Private Sub CreateSubmissionBlockNotes(ByRef dsReportDataSet As DataSet, ByVal dtBatchBlockComments As DataTable)

        Dim dtSubmissionBlocks As New DataTable("SubmissionBlocks")
        Dim drSubmissionBlocksRow As DataRow
        Dim drNewBlockRow As DataRow

        dtSubmissionBlocks.Columns.Add("SenderRef")
        dtSubmissionBlocks.Columns.Add("BlockRef")
        dtSubmissionBlocks.Columns.Add("BlockComment")
        dtSubmissionBlocks.Columns.Add("BlockArchiveComment")

        For Each drSubmissionBlocksRow In dtBatchBlockComments.Rows
            If drSubmissionBlocksRow("Comment").ToString.Trim <> "" Or drSubmissionBlocksRow("ArchiveComment").ToString.Trim <> "" Then
                drNewBlockRow = dtSubmissionBlocks.NewRow
                drNewBlockRow("SenderRef") = drSubmissionBlocksRow("SenderRef").ToString
                drNewBlockRow("BlockRef") = drSubmissionBlocksRow("BlockRef").ToString
                drNewBlockRow("BlockComment") = drSubmissionBlocksRow("Comment").ToString
                drNewBlockRow("BlockArchiveComment") = drSubmissionBlocksRow("ArchiveComment").ToString
                dtSubmissionBlocks.Rows.Add(drNewBlockRow)
            End If
        Next

        dsReportDataSet.Tables.Add(dtSubmissionBlocks)

    End Sub

    Private Sub CreateSubmissionAntibodiesNotes(ByRef dsReportDataSet As DataSet, ByVal dtBlockAntibodiesComments As DataTable)

        Dim dtBlockAntibodies As New DataTable("BlockAntibodies")
        Dim drBlockAntibodiesRow As DataRow
        Dim drNewAntibodiesRow As DataRow

        dtBlockAntibodies.Columns.Add("BlockRef")
        dtBlockAntibodies.Columns.Add("Test")
        dtBlockAntibodies.Columns.Add("TestComment")
        dtBlockAntibodies.Columns.Add("TestArchiveComment")

        For Each drBlockAntibodiesRow In dtBlockAntibodiesComments.Rows
            If drBlockAntibodiesRow("Comment").ToString.Trim <> "" Or drBlockAntibodiesRow("ArchiveComment").ToString.Trim <> "" Then
                drNewAntibodiesRow = dtBlockAntibodies.NewRow
                drNewAntibodiesRow("BlockRef") = drBlockAntibodiesRow("BlockRef").ToString
                drNewAntibodiesRow("Test") = drBlockAntibodiesRow("Description").ToString
                drNewAntibodiesRow("TestComment") = drBlockAntibodiesRow("Comment").ToString
                drNewAntibodiesRow("TestArchiveComment") = drBlockAntibodiesRow("ArchiveComment").ToString
                dtBlockAntibodies.Rows.Add(drNewAntibodiesRow)
            End If
        Next

        dsReportDataSet.Tables.Add(dtBlockAntibodies)
    End Sub

    Private Sub CreateSubmissionHistologyNotes(ByRef dsReportDataSet As DataSet, ByVal dtBlockHistologyComments As DataTable)
        Dim dtBlockHistology As New DataTable("BlockHistology")
        Dim drBlockHistologyRow As DataRow
        Dim drNewHistologyRow As DataRow

        dtBlockHistology.Columns.Add("BlockRef")
        dtBlockHistology.Columns.Add("Test")
        dtBlockHistology.Columns.Add("TestComment")
        dtBlockHistology.Columns.Add("TestArchiveComment")

        For Each drBlockHistologyRow In dtBlockHistologyComments.Rows
            If drBlockHistologyRow("Comment").ToString.Trim <> "" Or drBlockHistologyRow("ArchiveComment").ToString.Trim <> "" Then
                drNewHistologyRow = dtBlockHistology.NewRow
                drNewHistologyRow("BlockRef") = drBlockHistologyRow("BlockRef").ToString
                drNewHistologyRow("Test") = drBlockHistologyRow("Description").ToString
                drNewHistologyRow("TestComment") = drBlockHistologyRow("Comment").ToString
                drNewHistologyRow("TestArchiveComment") = drBlockHistologyRow("ArchiveComment").ToString
                dtBlockHistology.Rows.Add(drNewHistologyRow)
            End If
        Next

        dsReportDataSet.Tables.Add(dtBlockHistology)
    End Sub

    Private Sub CreateSubmissionStainNotes(ByRef dsReportDataSet As DataSet, ByVal dtBlockStainComments As DataTable)
        Dim dtBlockStain As New DataTable("BlockSpecialStain")
        Dim drBlockStainRow As DataRow
        Dim drNewStainRow As DataRow

        dtBlockStain.Columns.Add("BlockRef")
        dtBlockStain.Columns.Add("Test")
        dtBlockStain.Columns.Add("TestComment")
        dtBlockStain.Columns.Add("TestArchiveComment")

        For Each drBlockStainRow In dtBlockStainComments.Rows
            If drBlockStainRow("Comment").ToString.Trim <> "" Or drBlockStainRow("ArchiveComment").ToString.Trim <> "" Then
                drNewStainRow = dtBlockStain.NewRow
                drNewStainRow("BlockRef") = drBlockStainRow("BlockRef").ToString
                drNewStainRow("Test") = drBlockStainRow("Description").ToString
                drNewStainRow("TestComment") = drBlockStainRow("Comment").ToString
                drNewStainRow("TestArchiveComment") = drBlockStainRow("ArchiveComment").ToString
                dtBlockStain.Rows.Add(drNewStainRow)
            End If
        Next

        dsReportDataSet.Tables.Add(dtBlockStain)
    End Sub
End Class
