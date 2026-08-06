Partial Class ReceiveBatch
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents ctlBatchDate As CalendarDate
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
        VLAHeader1.PageTitle = "Receive Submission"
        CheckPermissions()
        SetCalendarDateHandler(Me.Page)

        If Not IsPostBack Then
            Batch1.DisplayDetails()
            LoadLookupLists()
            LoadCheckBoxList()
            InitialiseControls()
            RemoveStatusItems()
            HideErrorLabels()
            SetEnterPresses()
        End If


    End Sub

#Region "Private Functions"

    Private Sub SetEnterPresses()
        If ddlStatus.Enabled = True Then
            If ddlStatus.SelectedItem.Value = HistopathologyLib.clsBatch.STATUS_RECEIVED Then
                SetFocus(txtReason)
            ElseIf ddlStatus.SelectedItem.Value = HistopathologyLib.clsBatch.STATUS_RECEIVED Then
                SetFocus(txtReason)
            ElseIf ddlStatus.SelectedItem.Value = HistopathologyLib.clsBatch.STATUS_SUBMITTED Then
                SetFocus(txtReason)
            Else
                SetFocus(ddlStatus)
            End If

        End If

        SetDropDownControlOnEnter(ddlStatus, ctlBatchDate.FirstClientID)
        ctlBatchDate.SetDropDownOnEnter(ddlReceivedBy.ClientID)
        SetDropDownControlOnEnter(ddlReceivedBy, ddlTimeReceived.ClientID)
        SetDropDownControlOnEnter(ddlTimeReceived, ddlTimeReceived.ClientID)
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
            objDataTable = objLookup.GetLookupData(LOOKUP_TIME_RECEIVED)
            If Not objDataTable Is Nothing Then
                ddlTimeReceived.DataSource = objDataTable
                ddlTimeReceived.DataValueField = "Code"
                ddlTimeReceived.DataTextField = "Description"
                ddlTimeReceived.DataBind()
                Common.AddItemToDropDownList(ddlTimeReceived)
            End If

            objDataTable = objLookup.GetStatusLookupData()
            If Not objDataTable Is Nothing Then
                ddlStatus.DataSource = objDataTable
                ddlStatus.DataValueField = "Code"
                ddlStatus.DataTextField = "Description"
                ddlStatus.DataBind()
            End If

            objDataTable = objUsers.GetUsers()
            If Not objDataTable Is Nothing Then
                ddlReceivedBy.DataSource = objDataTable
                ddlReceivedBy.DataValueField = "ID"
                ddlReceivedBy.DataTextField = "Name"
                ddlReceivedBy.DataBind()
                Common.AddItemToDropDownList(ddlReceivedBy)
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve 'Receive Submission' drop down lists.", ex)
        End Try
    End Sub

    Private Sub LoadCheckBoxList()
        Dim objDataTable As DataTable
        Dim objLookup As New HistopathologyLib.LookupData()

        Try
            objDataTable = objLookup.GetLookupData(LOOKUP_POSTFIXATION)
            If Not objDataTable Is Nothing Then
                chkblPostFixation.DataSource = objDataTable
                chkblPostFixation.DataValueField = "Code"
                chkblPostFixation.DataTextField = "Description"
                chkblPostFixation.DataBind()
            End If

            Dim li As New ListItem()
            li.Value = "Other"
            li.Text = "Other"
            chkblPostFixation.Items.Add(li)

        Catch ex As Exception
            clsAppError.DisplayError("Failed to populate the Checkbox lists.", ex)
        End Try
    End Sub

    Private Sub InitialiseControls()
        Try
            Dim dsBatchData As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)

            If Not dsBatchData Is Nothing Then
                Dim dtPostFix As DataTable = dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_POSTFIXATION_TABLE)
                Dim dr As DataRow
                Dim li As ListItem
                Dim dtBatch As DataTable = dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)
                Dim dtBlocks As DataTable
                Dim drRow As DataRow
                Dim sBatchStatus As String = ""

                chkRepeatBlocks.Enabled = False
                If Not dsBatchData Is Nothing Then
                    dtBatch = dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)
                    If dtBatch.Rows(0)("IsBlocked") = True Then
                        dtBlocks = dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE)
                        If Not dtBlocks Is Nothing Then
                            For Each drRow In dtBlocks.Rows
                                If drRow("RepeatBlock") = True Then
                                    chkRepeatBlocks.Checked = True
                                    Exit For
                                End If
                            Next
                        End If
                    End If
                End If

                If Not dtBatch Is Nothing Then
                    If dtBatch.Rows.Count > 0 Then
                        SelectItemInDropDownList(ddlReceivedBy, dtBatch.Rows(0)("ReceivedBy").ToString())
                        mtxtPostFixationOther.Text = dtBatch.Rows(0)("PostFixationOther").ToString()
                        sBatchStatus = dtBatch.Rows(0)("BatchStatus").ToString()
                        SelectItemInDropDownList(ddlStatus, sBatchStatus)
                        ctlBatchDate.DateField = dtBatch.Rows(0)("DateReceived").ToString()
                        SelectItemInDropDownList(ddlTimeReceived, dtBatch.Rows(0)("TimeReceived").ToString())
                        txtReason.Text = dtBatch.Rows(0)("StatusComments").ToString()
                    End If

                    If sBatchStatus = "1" Then
                        ctlBatchDate.Enabled = False
                        ctlBatchDate.Mandatory = False
                        ddlTimeReceived.Enabled = False
                        ddlReceivedBy.Enabled = False
                    Else
                        ctlBatchDate.Mandatory = True
                    End If
                End If

                If Not dtPostFix Is Nothing Then
                    For Each dr In dtPostFix.Rows
                        If Not dr.RowState = DataRowState.Deleted Then
                            For Each li In chkblPostFixation.Items
                                If dr("Code") = li.Value Then
                                    li.Selected = True
                                End If
                                If dr("Code") = "Other" Then
                                    mtxtPostFixationOther.Enabled = True
                                End If
                            Next
                        End If
                    Next
                End If
            End If

            If CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean) = True Then
                chkRepeatBlocks.Enabled = False
                chkblPostFixation.Enabled = False
                ddlReceivedBy.Enabled = False
                mtxtPostFixationOther.Enabled = False
                ddlStatus.Enabled = False
                ctlBatchDate.Enabled = False
                ddlTimeReceived.Enabled = False
                txtReason.Enabled = False
                btnEditSubmission.Enabled = False
                btnSave.Enabled = False
            Else
                PromptBeforeSaveScript("Are you sure you want to Cancel? Any changes that have been made to the submission will be lost.", btnCancel)
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to Initialise screen.", ex)
        End Try
    End Sub

    Public Sub HideErrorLabels()
        lblErrorTimeReceived.Visible = False
        lblErrorReceivedBy.Visible = False
        lblErrorReason.Visible = False
    End Sub

    Public Function ValidateData() As Boolean
        Dim bReturnValue As Boolean = True
        HideErrorLabels()

        'If batch received, check the received by and time received are completed
        If ddlStatus.SelectedItem.Value = HistopathologyLib.clsBatch.STATUS_RECEIVED Then
            If ddlReceivedBy.SelectedIndex = 0 Then
                If ddlTimeReceived.SelectedIndex = 0 Then
                    lblErrorTimeReceived.Visible = True
                End If
                lblErrorReceivedBy.Visible = True
                ctlDIV.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
                bReturnValue = False
            Else
                If ddlTimeReceived.SelectedIndex = 0 Then
                    lblErrorTimeReceived.Visible = True
                    ctlDIV.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
                    bReturnValue = False
                End If
            End If
        ElseIf ddlStatus.SelectedItem.Value = HistopathologyLib.clsBatch.STATUS_REJECTED Then
            If txtReason.Text = "" Then
                lblErrorReason.Visible = True
                ctlDIV.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
                bReturnValue = False
            End If

            If ddlReceivedBy.SelectedIndex = 0 Then
                If ddlTimeReceived.SelectedIndex = 0 Then
                    lblErrorTimeReceived.Visible = True
                End If
                lblErrorReceivedBy.Visible = True
                ctlDIV.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
                bReturnValue = False
            Else
                If ddlTimeReceived.SelectedIndex = 0 Then
                    lblErrorTimeReceived.Visible = True
                    ctlDIV.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
                    bReturnValue = False
                End If
            End If
        End If

        Return bReturnValue
    End Function

    Private Sub RemoveStatusItems()
        If CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean) = False Then
            Dim iItemCount As Integer = 0
            For iItemCount = ddlStatus.Items.Count - 1 To 0 Step -1
                If ddlStatus.Items(iItemCount).Value = HistopathologyLib.clsBatch.STATUS_ONHOLD Or _
                    ddlStatus.Items(iItemCount).Value = HistopathologyLib.clsBatch.STATUS_INPROGRESS Or _
                    ddlStatus.Items(iItemCount).Value = HistopathologyLib.clsBatch.STATUS_COMPLETED Then
                    ddlStatus.Items.RemoveAt(iItemCount)
                End If
            Next
        End If
    End Sub

    Private Sub CancelSubmission()
        Dim sMessage As System.Text.StringBuilder = New System.Text.StringBuilder()
        sMessage.Append("Any changes that have been made will be discarded, are you sure you wish to exit without saving?")
        Page.RegisterStartupScript("navigate", PromptBeforeNavigateScript(sMessage.ToString(), "Home.aspx"))
    End Sub

    Private Sub UpdateSessionWithBatchData()
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtFixation As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_POSTFIXATION_TABLE)
            Dim objChkBoxList As New HistopathologyLib.clsCheckBoxData()
            Dim iID = CInt(Session.Item(SessionVars.SV_BatchID))
            Dim li As ListItem
            Dim dr As DataRow
            Dim drFoundRow As DataRow()
            Dim sFilter As String


            If Not dsBatchDetails Is Nothing Then
                'Update the batch details
                If dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows.Count > 0 Then
                    With dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)
                        ' .Item("BatchStatus") = HistopathologyLib.clsBatch.STATUS_INPROGRESS
                        .Item("BatchStatus") = FormatEmptyString(ddlStatus.SelectedItem.Value())
                        .Item("DateReceived") = FormatEmptyString(ctlBatchDate.DateField)
                        .Item("TimeReceived") = FormatEmptyString(ddlTimeReceived.SelectedItem.Value)
                        .Item("ReceivedBy") = FormatEmptyString(ddlReceivedBy.SelectedItem.Value)
                        .Item("StatusComments") = FormatEmptyString(txtReason.Text())
                        .Item("PostFixationOther") = FormatEmptyString(mtxtPostFixationOther.Text())
                    End With
                End If
            End If

            If Not dtFixation Is Nothing Then
                UpdateCheckBoxData(chkblPostFixation, dtFixation, iID)
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Unable to update session with Batch data.", ex)
        End Try
    End Sub

    Private Sub UpdateSubmissionStatus()
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtBatch As DataTable

            If Not dsBatchDetails Is Nothing Then
                dtBatch = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)
                If Not dtBatch Is Nothing And dtBatch.Rows.Count > 0 Then
                    'if we are receiving the batch set its status to in progress
                    'If ddlStatus.SelectedItem.Value = HistopathologyLib.clsBatch.STATUS_RECEIVED Then
                    'dtBatch.Rows(0)("BatchStatus") = HistopathologyLib.clsBatch.STATUS_INPROGRESS
                    'Else
                    dtBatch.Rows(0)("BatchStatus") = FormatEmptyString(ddlStatus.SelectedItem.Value())
                    'End If
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Unable to update submission status.", ex)
        End Try
    End Sub
#End Region


#Region "Event Handlers"

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        If CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean) = True Then
            Response.Redirect("SearchSubmissions.aspx")
        Else
            RemoveSessionVars(Session)
            Response.Redirect("BatchesNotReceived.aspx")
        End If
    End Sub

    Private Sub btnSave_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSave.Click
        Dim bRedirect As Boolean = False
        Dim dDate As Date
        Dim iBatchID As Integer
        Dim dSubmittedDate As Date

        If Not ValidateData() Then
            Exit Sub
        End If

        Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)

        If Not dsBatchDetails Is Nothing AndAlso dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE).Rows.Count > 0 Then
            dSubmittedDate = CType(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("BatchDate"), Date)
        End If

        If ctlBatchDate.Validate(dDate, CDate(dSubmittedDate.ToShortDateString), ctlBatchDate.ValidationType.eValidateEarliest, "Must be the same or later than the submission date of " & dSubmittedDate.ToShortDateString) And _
           ctlBatchDate.Validate(dDate, CDate(dDate.Date.Now().ToShortDateString), ctlBatchDate.ValidationType.eValidateLatest, "Must be today or earlier") Then
            Dim objBatch As New HistopathologyLib.clsBatch
            Dim objErrorlist As New ArrayList
            Dim iID = CInt(Session.Item(SessionVars.SV_BatchID))
            UpdateSessionWithBatchData()
            UpdateSubmissionStatus()

            Dim bSuccess As Boolean = objBatch.UpdateBatchDetails(CInt(Session.Item(SessionVars.SV_HeaderUserID)), _
                                                                  dsBatchDetails, _
                                                                  objErrorlist, _
                                                                  Session.Item(SessionVars.SV_Cassetted), _
                                                                  iBatchID, _
                                                                  Nothing, _
                                                                  IsBatchPreCassetted(dsBatchDetails, iBatchID))
            If bSuccess Then
                If objErrorlist.Count = 0 Then
                    bRedirect = True
                Else
                    ctlDIV.InnerHtml = "<p><font color=""Red"">The database has been updated but some errors were encountered:</font></p><p>&nbsp;</p><p><font color=""Red"">" & Join(objErrorlist.ToArray, "</font></p><p>") & "</p>"
                End If
            Else
                ctlDIV.InnerHtml = "<p><font color=""Red"">The database has not been updated because the following error(s) occurred:</font></p><p>&nbsp;</p><p><font color=""Red"">" & Join(objErrorlist.ToArray, "</font></p><p>") & "</p>"
            End If
        End If

        Session.Item(SessionVars.SV_BatchDetails) = dsBatchDetails
        Session.Item(SessionVars.SV_BatchID) = iBatchID
        Session.Item(SessionVars.SV_RedirectAfterPrint) = "BatchesNotReceived.aspx"
        Session.Item(SessionVars.SV_UnusedHistologyRef) = Nothing

        If bRedirect Then
            If CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean) = True Then
                Response.Redirect("SearchSubmissions.aspx")
            Else
                Response.Redirect("FinalPrintBatch.aspx")
            End If
        End If
    End Sub

    Private Sub ddlStatus_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ddlStatus.SelectedIndexChanged
        HideErrorLabels()
        If ddlStatus.SelectedIndex >= 0 Then
            'if the submission has been rejected

            If ddlStatus.SelectedItem.Value = HistopathologyLib.clsBatch.STATUS_RECEIVED Or _
               ddlStatus.SelectedItem.Value = HistopathologyLib.clsBatch.STATUS_REJECTED Then
                Dim dDate As Date
                ctlBatchDate.DateField = dDate.Now
                ctlBatchDate.Mandatory = True
                ctlBatchDate.Enabled = True
                ddlTimeReceived.Enabled = True
                ddlReceivedBy.Enabled = True
                SelectItemInDropDownList(ddlReceivedBy, CStr(Session.Item(SessionVars.SV_HeaderUserID)))
            Else
                SelectItemInDropDownList(ddlTimeReceived, "")
                SelectItemInDropDownList(ddlReceivedBy, "")
                ctlBatchDate.DateField = ""
                ctlBatchDate.Enabled = False
                ddlTimeReceived.Enabled = False
                ddlReceivedBy.Enabled = False
                ctlBatchDate.Mandatory = False
            End If
        End If
        ctlDIV.InnerHtml = ""
    End Sub

    Private Sub btnEditSubmission_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnEditSubmission.Click
        Session.Item(SessionVars.SV_SaveFromBatchDetails) = False
        Session.Item(SessionVars.SV_RedirectPage) = "ReceiveBatch.aspx"
        Session.Item(SessionVars.SV_RedirectCancelPage) = "ReceiveBatch.aspx"
        UpdateSessionWithBatchData()
        Session.Item(SessionVars.SV_EditingBatch) = True
        Session.Item(SessionVars.SV_ViewSubmission) = False
        Response.Redirect("BatchDetails.aspx")
    End Sub

    Private Sub VLAHeader1_HomeClick(ByVal sender As Object, ByVal e As HistopathologySystem.HomeLinkEventArgs) Handles VLAHeader1.HomeClick
        If CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean) = False Then
            CancelSubmission()
            e.bNavigateHome = False
        Else
            Response.Redirect("Home.aspx")
        End If
        
    End Sub

    Private Sub chkblPostFixation_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkblPostFixation.SelectedIndexChanged
        Dim li As ListItem
        For Each li In chkblPostFixation.Items
            If li.Text = "Other" Then
                If li.Selected = True Then
                    mtxtPostFixationOther.Enabled = True
                Else
                    mtxtPostFixationOther.Enabled = False
                    mtxtPostFixationOther.Text = ""
                End If
            End If
        Next
    End Sub

#End Region

End Class
