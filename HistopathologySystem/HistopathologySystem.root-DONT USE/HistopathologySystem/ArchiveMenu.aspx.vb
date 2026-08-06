Partial Class ArchiveMenu
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
        VLAHeader1.PageTitle = "Archive Menu"
        VLAHeader1.SubmissioNoVisible() = False
        CheckPermissions()
    End Sub

    Private Sub hlArchiveBlocks_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlArchiveBlocks.Click
        Dim iBatchID As Integer = CInt(Session.Item(SessionVars.SV_BatchID))

        GetCommonBatchDetailsFromDatabase(iBatchID, Session)
        GetBatchBlockDetailsFromDatabase(iBatchID, Session)

        Response.Redirect("ArchiveBlocks.aspx")
    End Sub

    Private Sub hlArchiveTissues_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlArchiveTissues.Click
        Dim iBatchID As Integer = CInt(Session.Item(SessionVars.SV_BatchID))
        GetCommonBatchDetailsFromDatabase(iBatchID, Session)
        GetBatchSubmissionDetailsFromDatabase(iBatchID, Session)

        Response.Redirect("ArchiveTissues.aspx")
    End Sub

    Private Sub CheckPermissions()
        VLAHeader1.GetUserDetails()

        Dim sGroupName As String = Session(SessionVars.SV_HeaderGroupName)

        If sGroupName = "Customer" Then
            Response.Redirect("Home.aspx")
        ElseIf sGroupName = "Histopathology User" Then
            'nothing
        ElseIf sGroupName = "Maintenance" Then
            'Nothing
        Else
            Response.Redirect("Home.aspx")
        End If
    End Sub

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        Response.Redirect(CStr(Session.Item(SessionVars.SV_RedirectCancelPage)))
    End Sub
End Class
