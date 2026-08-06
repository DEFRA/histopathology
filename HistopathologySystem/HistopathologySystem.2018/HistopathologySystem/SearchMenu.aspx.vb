Partial Class SearchMenu
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
        VLAHeader1.PageTitle = "Search Outputs Menu"
        VLAHeader1.SubmissioNoVisible() = False
        CheckPermissions()
    End Sub

    Private Sub hlSearchTSETests_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlSearchTSETests.Click
        Session.Item(SessionVars.SV_SubmissionType) = 0
        Response.Redirect("SearchTest.aspx")
    End Sub

    Private Sub hlSearchNonTSETests_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlSearchNonTSETests.Click
        Session.Item(SessionVars.SV_SubmissionType) = 1
        Response.Redirect("SearchTest.aspx")
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
End Class
