Partial Class FinalPrintBatch
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader

#Region " Web Form Designer Generated Code "

    'This call is required by the Web Form Designer.
    <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()

    End Sub

    Private Sub Page_Init(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Init
        'CODEGEN: This method call is required by the Web Form Designer
        'Do not modify it using the code editor.
        InitializeComponent()
    End Sub

#End Region

    Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        VLAHeader1.PageTitle = "Print Submission"

        If Not IsPostBack Then
            EnableSubmissionNotes()
        End If
    End Sub

    Private Sub btnHome_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnHome.Click
        Dim sRedirectPage As String = CStr(Session.Item(SessionVars.SV_RedirectAfterPrint))

        If sRedirectPage <> "" Then
            Response.Redirect(sRedirectPage)
        Else
            Response.Redirect("Home.aspx")
        End If

    End Sub

    Private Sub btnPrintBatch_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnPrintBatch.Click
        OpenDownloadPopup("SubmissionForm.aspx", Me.Page)
    End Sub

    Private Sub btnSubmissionNotes_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSubmissionNotes.Click
        OpenDownloadPopup("SubmissionNotes.aspx", Me.Page)
    End Sub

    Private Sub EnableSubmissionNotes()
        Try
            Dim iSubmissionID As Integer
            Dim objBatch As New HistopathologyLib.clsBatch
            Dim dsCommentsDataSet As New DataSet
            Dim iCount As Integer = 0
            Dim bFoundComment As Boolean = False

            iSubmissionID = CInt(Session.Item(SessionVars.SV_BatchID))

            If Not objBatch.GetBatchComments(iSubmissionID, dsCommentsDataSet) Then
                Throw New Exception("Batch.GetBatchComments returned false.")
            End If

            For iCount = 0 To dsCommentsDataSet.Tables.Count - 1
                If iCount = 0 Then
                    ' Will always be one row in the submission table at position 0.
                    If dsCommentsDataSet.Tables(0).Rows(0)("Comments").ToString.Trim <> "" Or _
                        dsCommentsDataSet.Tables(0).Rows(0)("StatusComments").ToString.Trim <> "" Then
                        bFoundComment = True
                        Exit For
                    End If
                Else
                    If dsCommentsDataSet.Tables(iCount).Rows.Count > 0 Then
                        bFoundComment = True
                        Exit For
                    End If
                End If
            Next

            btnSubmissionNotes.Enabled = bFoundComment

        Catch ex As Exception
            clsAppError.DisplayError("Failed to disable or enable the submission notes button.", ex)
        End Try
    End Sub

End Class
