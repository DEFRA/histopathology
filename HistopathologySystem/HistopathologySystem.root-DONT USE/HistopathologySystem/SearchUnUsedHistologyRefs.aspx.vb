

Partial Class SearchUnUsedHistologyRefs
    Inherits System.Web.UI.Page
    Protected WithEvents btnSearch As System.Web.UI.WebControls.Button
    Protected WithEvents lblHistRef As System.Web.UI.WebControls.Label
    Protected WithEvents txtHistRef As System.Web.UI.WebControls.TextBox
    Protected WithEvents ResultsPager As DataGridPager
    Protected WithEvents lblError As System.Web.UI.WebControls.Label
    Protected WithEvents btnDone As System.Web.UI.WebControls.Button
    Protected WithEvents DIV1 As System.Web.UI.HtmlControls.HtmlGenericControl
    Protected WithEvents lblSenderRef As System.Web.UI.WebControls.Label
    Protected WithEvents txtSenderRef As System.Web.UI.WebControls.TextBox
    Protected WithEvents lbViewImportedData As System.Web.UI.WebControls.LinkButton
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
        VLAHeader1.PageTitle = "Un Used Histology Refs"
        CheckPermissions()
        ResultsPager.SetGrid(grdResults)

        If Not IsPostBack Then
            VLAHeader1.SubmissioNoVisible() = False
            FillviewGrid()
        End If
    End Sub

#Region "Event Handlers"

    Private Sub btnDone_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnDone.Click
        Response.Redirect("Home.aspx")
    End Sub

    Private Sub VLAHeader1_HomeClick(ByVal sender As Object, ByVal e As HistopathologySystem.HomeLinkEventArgs) Handles VLAHeader1.HomeClick


    End Sub

#End Region

#Region "Grid Related"

    Private Sub FillviewGrid()
        Try
            Dim dtHistologyRefs As New DataTable
            Dim dtHistologyRefsView As DataView
            Dim objHistology As New HistopathologyLib.clsHistology

            If Not objHistology.GetUnUsedHistologyRefsTable(dtHistologyRefs) Then
                Throw New Exception("Histology.GetUnUsedHistologyRefsTable returned false.")
            End If

            Session(SessionVars.SV_SearchBatchDetailsTable) = dtHistologyRefs
            dtHistologyRefsView = dtHistologyRefs.DefaultView
            Session(SessionVars.SV_SearchBatchDetailsView) = dtHistologyRefsView

            dtHistologyRefs.TableName = "UnUsedHistologyRefs"
            Session.Item(SessionVars.SV_ExcelExport) = dtHistologyRefs
            Session.Item(SessionVars.SV_ExcelExportView) = dtHistologyRefsView

            ' initialise the grid
            grdResults.DataSource = dtHistologyRefs
            grdResults.DataKeyField = "HistologyRef"
            grdResults.CurrentPageIndex = 0
            grdResults.SelectedIndex = -1
            grdResults.EditItemIndex = -1
            grdResults.DataBind()

            ' initialise the pager
            ResultsPager.DataTableSessionID = SessionVars.SV_SearchBatchDetailsTable
            ResultsPager.DataViewSessionID = SessionVars.SV_SearchBatchDetailsView
            ResultsPager.PageLinkCount = 10
            ResultsPager.AllowAddNew = False
            ResultsPager.AllowEdit = False
            ResultsPager.AllowDelete = False
            ResultsPager.Refresh()


        Catch ex As Exception
            clsAppError.DisplayError("Error initialising the Search Block refs page.", ex)
        End Try
    End Sub

#End Region

#Region "Private Functions"

    Sub SetEnterPresses()
        SetFocus(txtSenderRef)
        SetTextboxDefaultButton(txtSenderRef, btnSearch)
        SetTextboxDefaultButton(txtHistRef, btnSearch)
    End Sub

    Private Sub CheckPermissions()
        VLAHeader1.getUserDetails()

        Dim sGroupName As String = Session(SessionVars.SV_HeaderGroupName)

        If sGroupName = "Customer" Then
            'Response.Redirect("Home.aspx")
        ElseIf sGroupName = "Histopathology User" Then
            'nothing
        ElseIf sGroupName = "Maintenance" Then
            'Nothing
        Else
            Response.Redirect("Home.aspx")
        End If
    End Sub

#End Region


End Class
