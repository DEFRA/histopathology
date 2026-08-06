Partial Class Home
    Inherits System.Web.UI.Page
    Protected WithEvents HyperLink1 As System.Web.UI.WebControls.HyperLink
    Protected WithEvents hlSearchTestTotals As System.Web.UI.WebControls.HyperLink
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
        VLAHeader1.SubmissioNoVisible() = False
        If Not IsPostBack Then
            Session.Clear()
        End If
        VLAHeader1.PageTitle = "Home"
        EnableControls()
    End Sub

#Region "Enable / Disable Controls"

    Private Sub EnableControls()
        VLAHeader1.getUserDetails()

        Dim sGroupName As String = Session(SessionVars.SV_HeaderGroupName)

        If sGroupName = "Customer" Then
            EnableCustomerLinks()
        ElseIf sGroupName = "Histopathology User" Then
            EnableHistologyUserLinks()
        ElseIf sGroupName = "Maintenance" Then
            EnableHistologyMaintenanceLinks()
        Else
            EnableCustomerLinks()
        End If
    End Sub

    Private Sub EnableCustomerLinks()
        hlTSESubmission.Enabled = True
        hlTSESubmission.Visible = True
        hlNonTSESubmission.Enabled = True
        hlNonTSESubmission.Visible = True
        lbViewSubmissions.Enabled = True
        lbViewSubmissions.Visible = True
        Panel2.Visible = False
        Panel2.Enabled = False
        hlReceiveSubmissions.Enabled = False
        hlReceiveSubmissions.Visible = False
        hlQualityData.Enabled = False
        hlQualityData.Visible = False
        lbSearchSubmissions.Enabled = False
        lbSearchSubmissions.Visible = False
        hlSearchOutputs.Enabled = False
        hlSearchOutputs.Visible = False
        Panel1.Visible = False
        Panel1.Enabled = False
        lbUserMaintenance.Enabled = False
        lbUserMaintenance.Visible = False
        hlPickListMaintenance.Enabled = False
        hlPickListMaintenance.Visible = False
        hlAuditLogs.Enabled = False
        hlAuditLogs.Visible = False
        hlEditSubmission.Enabled = False
        hlEditSubmission.Visible = False
        hlBlockBookHisto.Visible = False
        hlBlockBookHisto.Enabled = False
        hlArchiveSubmission.Enabled = False
        hlArchiveSubmission.Visible = False
        hlEnterBlocks.Enabled = False
        hlEnterBlocks.Visible = False
        ctlDivHistopath.Visible = False
        ctlDivMaintenance.Visible = False
        lbBlockRefSearch.Enabled = False
        lbBlockRefSearch.Visible = False
        hlSearchArciveLocation.Enabled = False
        hlSearchArciveLocation.Visible = False
        lbEditQcNotes.Enabled = False
        lbEditQcNotes.Visible = False
        hlEditHistologyRef.Enabled = False
        hlEditHistologyRef.Visible = False
        hlViewHistoricdata.Enabled = False
        hlViewHistoricdata.Visible = False
        hlUnUsedHistologyRefs.Enabled = False
        hlUnUsedHistologyRefs.Visible = False
    End Sub

    Private Sub EnableHistologyUserLinks()
        hlTSESubmission.Enabled = True
        hlTSESubmission.Visible = True
        hlNonTSESubmission.Enabled = True
        hlNonTSESubmission.Visible = True
        lbViewSubmissions.Enabled = True
        lbViewSubmissions.Visible = True
        Panel2.Visible = True
        Panel2.Enabled = True
        hlReceiveSubmissions.Enabled = True
        hlReceiveSubmissions.Visible = True
        hlQualityData.Enabled = True
        hlQualityData.Visible = True
        lbSearchSubmissions.Enabled = True
        lbSearchSubmissions.Visible = True
        hlSearchOutputs.Enabled = True
        hlSearchOutputs.Visible = True
        hlEditSubmission.Enabled = True
        hlEditSubmission.Visible = True
        hlBlockBookHisto.Visible = True
        hlBlockBookHisto.Enabled = True
        Panel1.Visible = False
        Panel1.Enabled = False
        lbUserMaintenance.Enabled = False
        lbUserMaintenance.Visible = False
        hlPickListMaintenance.Enabled = False
        hlPickListMaintenance.Visible = False
        hlAuditLogs.Enabled = False
        hlAuditLogs.Visible = False
        hlArchiveSubmission.Enabled = True
        hlArchiveSubmission.Visible = True
        hlEnterBlocks.Enabled = True
        hlEnterBlocks.Visible = True
        ctlDivHistopath.Visible = True
        ctlDivMaintenance.Visible = False
        lbBlockRefSearch.Enabled = True
        lbBlockRefSearch.Visible = True
        hlSearchArciveLocation.Enabled = True
        hlSearchArciveLocation.Visible = True
        lbEditQcNotes.Enabled = True
        lbEditQcNotes.Visible = True
        hlEditHistologyRef.Enabled = False
        hlEditHistologyRef.Visible = False
        hlViewHistoricdata.Visible = True
        hlViewHistoricdata.Enabled = True
        hlUnUsedHistologyRefs.Enabled = True
        hlUnUsedHistologyRefs.Visible = True
    End Sub

    Private Sub EnableHistologyMaintenanceLinks()
        hlTSESubmission.Enabled = True
        hlTSESubmission.Visible = True
        hlNonTSESubmission.Enabled = True
        hlNonTSESubmission.Visible = True
        lbViewSubmissions.Enabled = True
        lbViewSubmissions.Visible = True
        Panel2.Visible = True
        Panel2.Enabled = True
        hlReceiveSubmissions.Enabled = True
        hlReceiveSubmissions.Visible = True
        hlQualityData.Enabled = True
        hlQualityData.Visible = True
        lbSearchSubmissions.Enabled = True
        lbSearchSubmissions.Visible = True
        Panel1.Visible = True
        Panel1.Enabled = True
        lbUserMaintenance.Enabled = True
        lbUserMaintenance.Visible = True
        hlPickListMaintenance.Enabled = True
        hlPickListMaintenance.Visible = True
        hlAuditLogs.Enabled = True
        hlAuditLogs.Visible = True
        hlEditSubmission.Enabled = True
        hlEditSubmission.Visible = True
        hlBlockBookHisto.Visible = True
        hlBlockBookHisto.Enabled = True
        hlArchiveSubmission.Enabled = True
        hlArchiveSubmission.Visible = True
        hlEnterBlocks.Enabled = True
        hlEnterBlocks.Visible = True
        hlSearchOutputs.Enabled = True
        hlSearchOutputs.Visible = True
        ctlDivHistopath.Visible = True
        ctlDivMaintenance.Visible = True
        lbBlockRefSearch.Visible = True
        lbBlockRefSearch.Enabled = True
        hlSearchArciveLocation.Enabled = True
        hlSearchArciveLocation.Visible = True
        lbEditQcNotes.Enabled = True
        lbEditQcNotes.Visible = True
        hlEditHistologyRef.Enabled = True
        hlEditHistologyRef.Enabled = True
        hlEditHistologyRef.Enabled = True
        hlEditHistologyRef.Visible = True
        hlViewHistoricdata.Visible = True
        hlViewHistoricdata.Enabled = True
        hlUnUsedHistologyRefs.Enabled = True
        hlUnUsedHistologyRefs.Visible = True
    End Sub
#End Region

#Region "Event Handlers"
    Private Sub hlTSESubmission_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlTSESubmission.Click
        Session(SessionVars.SV_SubmissionType) = SUBMISSION_TSE
        Session(SessionVars.SV_CreatingNewBatch) = True
        Try
            Dim objBreadCrumbList As New ArrayList
            objBreadCrumbList.Insert(0, "New TSE Submission")
            objBreadCrumbList.Insert(1, "Submission Type")
            Session.Item(SessionVars.SV_BreadCrumbs) = objBreadCrumbList
        Catch ex As Exception
            clsAppError.DisplayError("Bread Crumb Error, Home.aspx.", ex)
        End Try
        Response.Redirect("Cassetted.aspx")
    End Sub

    Private Sub hlNonTSESubmission_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlNonTSESubmission.Click
        Session(SessionVars.SV_SubmissionType) = SUBMISSION_NONTSE
        Session(SessionVars.SV_CreatingNewBatch) = True
        Try
            Dim objBreadCrumbList As New ArrayList
            objBreadCrumbList.Insert(0, "New Non TSE Submission")
            objBreadCrumbList.Insert(1, "Submission Type")
            Session.Item(SessionVars.SV_BreadCrumbs) = objBreadCrumbList
        Catch ex As Exception
            clsAppError.DisplayError("Bread Crumb Error, Home.aspx.", ex)
        End Try

        Response.Redirect("Cassetted.aspx")
    End Sub

    Private Sub hlEnterBlocks_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlEnterBlocks.Click
        Session.Item(SessionVars.SV_Cassetted) = False
        Response.Redirect("BatchesReceived.aspx")
    End Sub

    Private Sub lbUserMaintenance_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lbUserMaintenance.Click
        Session.Item(SessionVars.SV_PassUserArea) = Nothing
        Response.Redirect("UserMaintenance.aspx")
    End Sub

    Private Sub lbBlockRefSearch_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lbBlockRefSearch.Click
        Session.Item(SessionVars.SV_SearchBlockRefsRedirectPage) = "Home.aspx"
        Response.Redirect("SearchBlockRefs.aspx")
    End Sub

    Private Sub lbEditQcNotes_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lbEditQcNotes.Click
        Response.Redirect("QCNotes.aspx")
    End Sub

    Private Sub lbSearchSubmissions_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lbSearchSubmissions.Click
        Session.Item(SessionVars.SV_SearchCriteria) = Nothing
        Response.Redirect("SearchSubmissions.aspx")
    End Sub

    Private Sub lbViewSubmissions_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lbViewSubmissions.Click
        Session.Item(SessionVars.SV_SearchCriteria) = Nothing
        Response.Redirect("ViewSubmissions.aspx")
    End Sub

#End Region



End Class
