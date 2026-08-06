Partial Class EditBatch
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents Batch1 As Batch

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
        VLAHeader1.PageTitle = "Edit Submission Status"
        CheckPermissions()
        SetCalendarDateHandler(Me.Page)

        If Not IsPostBack Then
            Batch1.DisplayDetails()
            LoadLookupLists()
            InitialiseControls()
            'RemoveStatusItems()
            PromptBeforeSaveScript("Are you sure you want to Cancel? Any changes that have been made to the submission will be lost.", btnCancel)
            Session.Item(SessionVars.SV_BreadCrumbs) = Nothing
        End If

        SetEnterPress()

    End Sub

#Region "Private Functions"

    Private Sub SetEnterPress()
        SetFocus(ddlStatus)
        SetDropDownControlOnEnter(ddlStatus, txtReason.ClientID)
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

    Private Sub LoadLookupLists()
        Dim objDataTable As DataTable
        Dim objLookup As New HistopathologyLib.LookupData()
        Dim objUsers As New HistopathologyLib.clsUser()

        Try
            objDataTable = objLookup.GetStatusLookupData()
            If Not objDataTable Is Nothing Then
                ddlStatus.DataSource = objDataTable
                ddlStatus.DataValueField = "Code"
                ddlStatus.DataTextField = "Description"
                ddlStatus.DataBind()
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve 'Receive Submission' drop down lists.", ex)
        End Try
    End Sub

    Private Sub InitialiseControls()
        Try
            Dim dsBatchData As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)

            If Not dsBatchData Is Nothing Then
                Dim dtBatch As DataTable = dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)

                If Not dtBatch Is Nothing And dtBatch.Rows.Count > 0 Then
                    SelectItemInDropDownList(ddlStatus, dtBatch.Rows(0)("BatchStatus").ToString())
                    txtReason.Text = dtBatch.Rows(0)("StatusComments").ToString()
                End If
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to Initialise screen.", ex)
        End Try
    End Sub

    'Private Sub RemoveStatusItems()
    '    Dim iItemCount As Integer = 0
    '    For iItemCount = ddlStatus.Items.Count - 1 To 0 Step -1
    '        'Need to allow the received item to be displayed so the status displays correctly
    '        'when it is edited. Just dont allow the user to save the submission if Received has
    '        'been selected.
    '        'If ddlStatus.Items(iItemCount).Value = HistopathologyLib.clsBatch.STATUS_RECEIVED _
    '        If ddlStatus.Items(iItemCount).Value = HistopathologyLib.clsBatch.STATUS_REJECTED Then
    '            ddlStatus.Items.RemoveAt(iItemCount)
    '        End If
    '    Next
    'End Sub

    Private Sub CancelSubmission()
        Dim sMessage As System.Text.StringBuilder = New System.Text.StringBuilder()
        sMessage.Append("Any changes that have been made will be discarded, are you sure you wish to exit without saving?")
        Page.RegisterStartupScript("navigate", PromptBeforeNavigateScript(sMessage.ToString(), "Home.aspx"))
    End Sub

    Private Sub UpdateSessionWithBatchData()
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dateNow As DateTime

            If Not dsBatchDetails Is Nothing Then
                'Update the batch details
                If dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows.Count > 0 Then
                    With dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)
                        .Item("BatchStatus") = FormatEmptyString(ddlStatus.SelectedItem.Value())
                        If ddlStatus.SelectedItem.Value = HistopathologyLib.clsBatch.STATUS_COMPLETED Then
                            .Item("DateCompleted") = dateNow.Now.Date
                        Else
                            .Item("DateCompleted") = FormatEmptyString("")
                        End If
                        .Item("StatusComments") = FormatEmptyString(txtReason.Text.ToString)
                    End With
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Unable to update session with Batch data.", ex)
        End Try
    End Sub


#End Region

#Region "Event Handlers"

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        RemoveSessionVars(Session)
        Response.Redirect("BatchesForEditing.aspx")
    End Sub

    Private Sub btnSave_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSave.Click
        Dim bRedirect As Boolean = False
        Dim iBatchID As Integer
        Dim objBatch As New HistopathologyLib.clsBatch()
        Dim objErrorlist As New ArrayList()
        Dim iID = CInt(Session.Item(SessionVars.SV_BatchID))
        Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
        Dim bSuccess As Boolean
        Dim sError As String = ""

        If ddlStatus.SelectedItem.Value = HistopathologyLib.clsBatch.STATUS_RECEIVED Then
            If CStr(Session.Item(SessionVars.SV_SubmissionStatus)) = ddlStatus.SelectedItem.Value Then
                UpdateSessionWithBatchData()

                bSuccess = objBatch.UpdateBatchDetails(CInt(Session.Item(SessionVars.SV_HeaderUserID)), _
                                                       dsBatchDetails, _
                                                       objErrorlist, _
                                                       Session.Item(SessionVars.SV_Cassetted), _
                                                       iBatchID, _
                                                       Nothing, _
                                                       IsBatchPreCassetted(dsBatchDetails, iID), _
                                                       CType(Session.Item(SessionVars.SV_UnusedHistologyRef), DataTable))
            Else
                bSuccess = False
                sError = "The submission can only be received using the receive submission option on the home screen."
            End If
        ElseIf ddlStatus.SelectedItem.Value = HistopathologyLib.clsBatch.STATUS_INPROGRESS Then
            If CStr(Session.Item(SessionVars.SV_SubmissionStatus)) = HistopathologyLib.clsBatch.STATUS_SUBMITTED Then
                bSuccess = False
                sError = "The submission cannot be changed to in progress if it has not been received. Receive the submission using the receive submission option on the home screen."
            Else
                UpdateSessionWithBatchData()

                bSuccess = objBatch.UpdateBatchDetails(CInt(Session.Item(SessionVars.SV_HeaderUserID)), _
                                                       dsBatchDetails, _
                                                       objErrorlist, _
                                                       Session.Item(SessionVars.SV_Cassetted), _
                                                       iBatchID, _
                                                       Nothing, _
                                                       IsBatchPreCassetted(dsBatchDetails, iID), _
                                                       CType(Session.Item(SessionVars.SV_UnusedHistologyRef), DataTable))
            End If
        Else
            UpdateSessionWithBatchData()

            bSuccess = objBatch.UpdateBatchDetails(CInt(Session.Item(SessionVars.SV_HeaderUserID)), _
                                                   dsBatchDetails, _
                                                   objErrorlist, _
                                                   Session.Item(SessionVars.SV_Cassetted), _
                                                   iBatchID, _
                                                   Nothing, _
                                                   IsBatchPreCassetted(dsBatchDetails, iID), _
                                                   CType(Session.Item(SessionVars.SV_UnusedHistologyRef), DataTable))
        End If

        If bSuccess Then
            If objErrorlist.Count = 0 Then
                bRedirect = True
            Else
                ctlDIV.InnerHtml = "<p><font color=""Red"">The database has been updated but some errors were encountered:</font></p><p>&nbsp;</p><p><font color=""Red"">" & Join(objErrorlist.ToArray, "</font></p><p>") & "</p>"
            End If
        Else
            If sError = "" Then
                ctlDIV.InnerHtml = "<p><font color=""Red"">The database has not been updated because the following error(s) occurred:</font></p><p>&nbsp;</p><p><font color=""Red"">" & Join(objErrorlist.ToArray, "</font></p><p>") & "</font></p>"
            Else
                ctlDIV.InnerHtml = "<p><font color=""Red"">" & sError & "</font></p>"
            End If
        End If

        If bRedirect Then
            RemoveSessionVars(Session)
            Response.Redirect("BatchesForEditing.aspx")
        End If
    End Sub

    Private Sub btnEditSubmission_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnEditSubmission.Click
        Session.Item(SessionVars.SV_SaveFromBatchDetails) = False
        Session.Item(SessionVars.SV_RedirectPage) = "EditBatch.aspx"
        Session.Item(SessionVars.SV_RedirectCancelPage) = "EditBatch.aspx"
        UpdateSessionWithBatchData()
        Page.SmartNavigation = False
        Session.Item(SessionVars.SV_EditingBatch) = True
        Session.Item(SessionVars.SV_ViewSubmission) = False

        Try
            Dim objBreadCrumbList As New ArrayList()
            objBreadCrumbList.Insert(0, "Edit Submission")
            objBreadCrumbList.Insert(1, "Submission")
            objBreadCrumbList.Insert(2, "Submission Details")
            Session.Item(SessionVars.SV_BreadCrumbs) = objBreadCrumbList
        Catch ex As Exception
            clsAppError.DisplayError("Bread Crumb Error, EditBatch.aspx.", ex)
        End Try

        Response.Redirect("BatchDetails.aspx")
    End Sub

    Private Sub VLAHeader1_HomeClick(ByVal sender As Object, ByVal e As HistopathologySystem.HomeLinkEventArgs) Handles VLAHeader1.HomeClick
        CancelSubmission()
        e.bNavigateHome = False
    End Sub

    Private Sub btnSamplesOnHold_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSamplesOnHold.Click
        UpdateSessionWithBatchData()
        Response.Redirect("SubmissionsOnHold.aspx")
    End Sub

#End Region

End Class
