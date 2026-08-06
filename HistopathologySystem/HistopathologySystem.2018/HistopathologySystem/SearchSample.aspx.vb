Partial Class SearchSample
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents SenderPager As DataGridPager

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
        VLAHeader1.PageTitle = "Sample"
        SenderPager.SetGrid(grdSenders)

        If Not IsPostBack Then
            PopulateSendersTable()
            txtSenderRef.Text = CStr(Session.Item(SessionVars.SV_SenderRef))
        End If
    End Sub

#Region "Private Functions"

    Private Sub PopulateSendersTable()
        Try
            Dim dtData As DataTable = Nothing

            'This data will will have been populate in AddSubmission.aspx in btnLookup_Click
            dtData = CType(Session.Item(SessionVars.SV_TempPickSenderList), DataTable)

            grdSenders.DataSource = dtData
            grdSenders.DataKeyField = "ID"
            grdSenders.CurrentPageIndex = 0
            grdSenders.SelectedIndex = -1
            grdSenders.EditItemIndex = -1
            grdSenders.DataBind()

            SenderPager.DataTableSessionID = SessionVars.SV_TempPickSenderList
            SenderPager.AllowAddNew = False
            SenderPager.AllowEdit = False
            SenderPager.AllowDelete = False
            SenderPager.PageLinkCount = 10
            SenderPager.Refresh()

        Catch ex As Exception
            clsAppError.DisplayError("Failed to populate the sender table.", ex)
        End Try
    End Sub

#End Region

#Region "Event Handlers"

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        Session.Remove(SessionVars.SV_TempPickSenderList)

        Try
            Dim objArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
            If Not objArrayList Is Nothing Then
                objArrayList(1) = "Submission Samples"
                objArrayList(2) = "Copy Sample"
                Session.Item(SessionVars.SV_BreadCrumbs) = objArrayList
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Bread Crumb Error, SearchSample.aspx.", ex)
        End Try

        Response.Redirect("AddSample.aspx")
    End Sub

    Private Sub grdSenders_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles grdSenders.SelectedIndexChanged
        Try
            If grdSenders.SelectedIndex >= 0 Then
                Dim iID As Integer = grdSenders.DataKeys(grdSenders.SelectedIndex)
                Dim dtData As DataTable = CType(Session.Item(SessionVars.SV_TempPickSenderList), DataTable)
                Dim sFilter As String
                Dim foundRows As DataRow()

                sFilter = "ID=" & iID
                foundRows = dtData.Select(sFilter)

                If Not foundRows Is Nothing And foundRows.Length > 0 Then
                    Session.Item(SessionVars.SV_SenderRef) = foundRows(0)("SenderRef")
                End If
                Session.Remove(SessionVars.SV_TempPickSenderList)
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Unable to process the selected sender ref.", ex)
        End Try

        Try
            Dim objArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
            If Not objArrayList Is Nothing Then
                objArrayList(1) = "Submission Samples"
                objArrayList(2) = "Copy Sample"
                Session.Item(SessionVars.SV_BreadCrumbs) = objArrayList
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Bread Crumb Error, SearchSample.aspx.", ex)
        End Try

        Response.Redirect("AddSample.aspx")
    End Sub

    Private Sub VLAHeader1_HomeClick(ByVal sender As Object, ByVal e As HistopathologySystem.HomeLinkEventArgs) Handles VLAHeader1.HomeClick
        Dim sMessage As System.Text.StringBuilder = New System.Text.StringBuilder()

        If CType(Session.Item(SessionVars.SV_EditingBatch), Boolean) Then
            sMessage.Append("You are currently editing a submission. Any changes that you have made since you last saved the submission will be lost. Are you sure you wish to exit?")
        ElseIf CType(Session.Item(SessionVars.SV_CreatingNewBatch), Boolean) Then
            sMessage.Append("You are currently creating a new submission. If you exit now all the data you have entered will be lost. Are you sure you wish to exit?")
        Else
            sMessage.Append("Any changes that have been made will be discarded, are you sure you wish to exit without saving?")
        End If

        Page.RegisterStartupScript("navigate", PromptBeforeNavigateScript(sMessage.ToString(), "Home.aspx"))
        e.bNavigateHome = False
    End Sub

#End Region


End Class
