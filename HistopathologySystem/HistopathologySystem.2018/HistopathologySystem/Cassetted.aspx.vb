Partial Class Cassetted
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
        VLAHeader1.PageTitle = "Submission Type"
        If Not IsPostBack Then
            Dim SelectedItemArray As New ArrayList()
            Session.Item(SessionVars.SV_SelectedHistologyArray) = SelectedItemArray

            LoadCheckBoxLists()
        End If
    End Sub

#Region "Event Handlers"

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        Dim sMessage As System.Text.StringBuilder = New System.Text.StringBuilder()
        sMessage.Append("Any changes that have been made will be discarded, are you sure you wish to exit without saving?")
        Page.RegisterStartupScript("navigate", PromptBeforeNavigateScript(sMessage.ToString(), "Home.aspx"))
    End Sub

    Private Sub VLAHeader1_HomeClick(ByVal sender As Object, ByVal e As HistopathologySystem.HomeLinkEventArgs) Handles VLAHeader1.HomeClick
        Response.Redirect("Home.aspx")
    End Sub

    Private Sub btnYes_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnYes.Click
        If ValidateRequiredData() Then
            Dim iBatchID As Integer = CType(Session.Item(SessionVars.SV_BatchID), Integer)
            GetCommonBatchDetailsFromDatabase(iBatchID, Session)
            Session.Item(SessionVars.SV_ViewSubmission) = False
            Session.Item(SessionVars.SV_RedirectPage) = "FinalPrintBatch.aspx"
            Session.Item(SessionVars.SV_RedirectCancelPage) = "Home.aspx"
            Session.Item(SessionVars.SV_SaveFromBatchDetails) = True
            Session.Item(SessionVars.SV_SubmittedAs) = chkblSubmittedAs.SelectedItem.Value.ToString()
            Session.Item(SessionVars.SV_ImportedFromDayBook) = False

            'If wet tissue has been selected get the tissue details, otherwise get the block details.
            If chkblSubmittedAs.SelectedItem.Text.ToString() = "Wet Tissue" Then
                GetBatchSubmissionDetailsFromDatabase(CType(Session.Item(SessionVars.SV_BatchID), Integer), Session)
                Session.Item(SessionVars.SV_Cassetted) = False
            Else
                GetBatchBlockDetailsFromDatabase(CType(Session.Item(SessionVars.SV_BatchID), Integer), Session)
                Session.Item(SessionVars.SV_Cassetted) = True
            End If

            CreateNewBatch()
            UpdateSessionWithBatchDetails()

            Try
                Dim objBreadCrumbList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
                If Not objBreadCrumbList Is Nothing Then
                    objBreadCrumbList(1) = "Submission"
                    objBreadCrumbList.Insert(2, "Submission Details")
                    Session.Item(SessionVars.SV_BreadCrumbs) = objBreadCrumbList
                End If
            Catch ex As Exception
                clsAppError.DisplayError("Bread Crumb Error, Cassetted.aspx.", ex)
            End Try

            Response.Redirect("BatchDetails.aspx")
        End If
    End Sub

    Private Sub chkblSubmittedAs_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkblSubmittedAs.SelectedIndexChanged
        Dim sItemSelected As String
        Dim iPosition As Integer
        Dim li As ListItem
        Dim aArray As ArrayList = CType(Session(SessionVars.SV_SelectedHistologyArray), ArrayList)
        li = GetCheckListSelectedItem(sItemSelected, aArray)
        Dim liCheck As ListItem

        If Not li Is Nothing Then
            If li.Selected = True Then
                For Each liCheck In chkblSubmittedAs.Items
                    If Not liCheck Is li Then
                        liCheck.Selected = False
                        aArray.Remove(liCheck.Text.ToString())
                    End If
                Next
            End If
        End If

    End Sub

#End Region

#Region "Private Functions"

    Private Sub LoadCheckBoxLists()
        Dim objDataTable As DataTable
        Dim objLookup As New HistopathologyLib.LookupData()
        Dim li As ListItem

        Try
            objDataTable = objLookup.GetLookupData(LOOKUP_SUBMITTEDAS)

            If Not objDataTable Is Nothing Then
                chkblSubmittedAs.DataSource = objDataTable
                chkblSubmittedAs.DataValueField = "Code"
                chkblSubmittedAs.DataTextField = "Description"
                chkblSubmittedAs.DataBind()
                chkblSubmittedAs.Enabled = True
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve the submitted as list.", ex)
        End Try
    End Sub

    Private Function GetCheckListSelectedItem(ByRef sText As String, ByVal aArray As ArrayList) As ListItem
        'This function is used to get the item in the ComboboxList that has just been selected.
        'Using comboboxList.selectedItem always returns the lowest indexed selected item rather
        'than the item just selected.

        Dim li As ListItem
        For Each li In chkblSubmittedAs.Items
            If li.Selected = True Then
                sText = li.Text
                If Not aArray.Contains(sText) Then
                    aArray.Add(sText)
                    Return li
                End If
            Else
                sText = li.Text
                If aArray.Contains(sText) Then
                    aArray.Remove(sText)
                    Return li
                End If
            End If
        Next
        Return li
    End Function

    Private Function ValidateRequiredData() As Boolean

        If chkblSubmittedAs.SelectedIndex = -1 Then
            lblError.Visible = True
            ctlDiv.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
            Return False
        End If

        Return True
    End Function

    Private Sub UnCheckWetTissue(ByRef aArray As ArrayList)
        Dim li As ListItem
        For Each li In chkblSubmittedAs.Items
            If li.Text = "Wet Tissue" Then
                li.Selected = False
                aArray.Remove(li.Text.ToString())
            End If
        Next
    End Sub

    Private Sub CreateNewBatch()
        Try
            Dim objBatch As New HistopathologyLib.clsBatch()
            Dim dtBatch As DataTable = CType(Session.Item(SessionVars.SV_BatchDetails).Tables(HistopathologyLib.clsBatch.BATCH_TABLE), DataTable)
            Dim iBatchID As Integer = CType(Session.Item(SessionVars.SV_BatchID), Integer)

            If Not objBatch.NewBatch(dtBatch, iBatchID) Then
                Throw New Exception("Batch.NewBatch return false")
            End If

            Session.Item(SessionVars.SV_BatchID) = iBatchID

        Catch ex As Exception
            clsAppError.DisplayError("Failed to create new batch.", ex)
        End Try
    End Sub

    Private Function UpdateSessionWithBatchDetails() As Boolean
        Dim dsBatchData As DataSet = Session.Item(SessionVars.SV_BatchDetails)
        Dim iID As Integer = CInt(Session.Item(SessionVars.SV_BatchID))
        Try
            If Not dsBatchData Is Nothing Then
                'Update the submitted as for the batch
                UpdateCheckBoxData(chkblSubmittedAs, dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_SUBMITTEDAS_TABLE), iID)
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to Save Batch Details.", ex)
            Return False
        End Try
        Return True
    End Function

#End Region

End Class
