Partial Class BatchSummary
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
        VLAHeader1.PageTitle = "Sample Summary"

        If Not IsPostBack Then
            DisplayDetails()
            InitialiseSummaryGrid()
            EnableDisableButtons(False)
            SetToolTips()
            DisplayNumberSamples()
            PromptBeforeSaveScript("Are you sure you want to delete the selected sample?", btnDeleteSubmission)
        End If
    End Sub

#Region "Grid Related"

    Private Sub InitialiseSummaryGrid()
        Try
            Dim dsDataSet As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim objSummary As New HistopathologyLib.clsBatchSummary()
            Dim dtSummary As New DataTable()
            Dim dtTissuesList As DataTable = Common.GetLookupTypeList(LOOKUP_TISSUE_CODE)

            If Not objSummary.CreateBatchSummaryData(dsDataSet, dtSummary, dtTissuesList) Then
                Throw New Exception("BatchSummary.CreateBatchSummaryData return false")
            End If

            ' create a dataview for filtering and sorting
            Dim dv As DataView = dtSummary.DefaultView

            Session.Item(SessionVars.SV_BatchSummaryTable) = dtSummary
            Session.Item(SessionVars.SV_BatchSummaryView) = dv

            grdBatchSummary.DataSource = dtSummary
            grdBatchSummary.DataKeyField = "ID"
            grdBatchSummary.DataBind()
            grdBatchSummary.Enabled = True

            SetHierarchical(False)

            grdBatchSummary.SelectedIndex = -1
        Catch ex As Exception
            clsAppError.DisplayError("Error initialising the Summary Grid", ex)
        End Try
    End Sub

    Private Sub SetHierarchical(ByVal bExpanded As Boolean)
        Dim iCount As Int32

        'Cell 0 = Select button
        'Cell 1 = Sender Ref field
        'Cell 2 = Histology Ref field
        'Cell 3 = +/- button
        'Cell 4 = Tissue field
        If Not bExpanded Then
            For iCount = 0 To grdBatchSummary.Items.Count - 1
                'if the sender ref is null then it is a tissue row
                If grdBatchSummary.Items(iCount).Cells(1).Text = "&nbsp;" Then
                    grdBatchSummary.Items(iCount).Visible = False
                End If

                grdBatchSummary.Items(iCount).Cells(3).Controls(0).Visible = False

                If iCount + 1 <= grdBatchSummary.Items.Count - 1 Then
                    If Not grdBatchSummary.Items(iCount + 1).Cells(4).Text = "&nbsp;" Then
                        grdBatchSummary.Items(iCount).Cells(3).Controls(0).Visible = True
                        CType(grdBatchSummary.Items(iCount).Cells(3).Controls(0), LinkButton).Text = "<img src=""Images/plus.gif"">"
                    End If
                End If
            Next
        Else
            For iCount = 0 To grdBatchSummary.Items.Count - 1
                'If the sender ref is blank in the datatable it is a expandable tissue row
                If grdBatchSummary.Items(iCount).Cells(1).Text = "&nbsp;" Then
                    grdBatchSummary.Items(iCount).Visible = True
                    grdBatchSummary.Items(iCount).Cells(3).Controls(0).Visible = False
                    grdBatchSummary.Items(iCount).Cells(0).Controls(0).Visible = False
                End If

                If iCount + 1 <= grdBatchSummary.Items.Count - 1 Then
                    'if the next row tissue is blank its a sender ref row
                    If Not grdBatchSummary.Items(iCount + 1).Cells(4).Text = "&nbsp;" Then
                        CType(grdBatchSummary.Items(iCount).Cells(3).Controls(0), LinkButton).Text = "<img src=""Images/minus.gif"">"
                    End If
                End If
            Next
        End If
    End Sub

#End Region

#Region "Event Handlers"

    Protected Sub chkByPassSort_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) Handles chkByPassSort.CheckedChanged
        Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
        Dim dtBatch As DataTable

        dtBatch = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)
        dtBatch.Rows(0)("ByPassSort") = chkByPassSort.Checked

        InitialiseSummaryGrid()
    End Sub

    Private Sub btnEditSubmission_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnEditSubmission.Click
        Try
            Dim iID As Int32
            If grdBatchSummary.SelectedIndex >= 0 Then
                Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
                Dim dtData As DataTable = CType(Session.Item(SessionVars.SV_BatchSummaryTable), DataTable)
                Dim sFilter As String
                Dim dtOldData As DataTable
                Dim drFoundRow As DataRow()

                iID = Convert.ToInt32(grdBatchSummary.DataKeys(grdBatchSummary.SelectedIndex))
                Session.Item(SessionVars.SV_BatchSubmissionID) = iID
                Session.Item(SessionVars.SV_Editing) = True

                sFilter = "ID=" & Convert.ToString(iID)
                drFoundRow = dtData.Select(sFilter)

                If Not drFoundRow Is Nothing AndAlso drFoundRow.Length > 0 Then
                    Session.Item(SessionVars.SV_SenderRef) = drFoundRow(0)("SenderRef").ToString()
                    Session.Item(SessionVars.SV_AnimalID) = drFoundRow(0)("AnimalID")
                End If

                'get the backup here as we know it will not have been edited yet
                sFilter = "BatchSubmissionID=" & Convert.ToString(iID)
                dtOldData = CopyDataTable(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TISSUES_TABLE), sFilter)
                Session.Item(SessionVars.SV_TissuesBeforeChanges) = dtOldData

            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to edit submission.", ex)
        End Try

        Try
            Dim objArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
            If Not objArrayList Is Nothing Then
                objArrayList(1) = "Submission Samples"
                objArrayList(2) = "Tissuing"
                objArrayList.Insert(3, "Sample Details")
                Session.Item(SessionVars.SV_BreadCrumbs) = objArrayList
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Bread Crumb Error, BatchSummary.aspx.", ex)
        End Try

        Session.Item(SessionVars.SV_AddSamplePrevPage) = "BatchSummary.aspx"
        Session.Item(SessionVars.SV_AddSampleNextPage) = "BatchSummary.aspx"
        Response.Redirect("SubmissionDetails.aspx")
    End Sub

    Private Sub btnDeleteSubmission_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnDeleteSubmission.Click
        Try
            Dim iID As Int32
            If grdBatchSummary.SelectedIndex >= 0 Then
                Dim objAnimal As New HistopathologyLib.clsAnimal()
                Dim dtData As DataTable = CType(Session.Item(SessionVars.SV_BatchSummaryTable), DataTable)
                Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
                Dim sFilter As String
                Dim iAnimalID As Integer = 0
                Dim foundRows As DataRow()

                iID = Convert.ToInt32(grdBatchSummary.DataKeys(grdBatchSummary.SelectedIndex))

                sFilter = "ID=" & iID
                foundRows = dtData.Select(sFilter)

                If Not foundRows Is Nothing And foundRows.Length > 0 Then
                    iAnimalID = foundRows(0)("AnimalID")
                    If Not objAnimal.RemoveSubmission(dsBatchDetails, _
                                                      iAnimalID, _
                                                      "BATCH_SUBMISSION_TABLE") Then
                        Throw New Exception("Animal.RemoveSubmission returned false")
                    End If
                End If

                InitialiseSummaryGrid()

                If grdBatchSummary.Items.Count = 0 Then
                    Session.Item(SessionVars.SV_ImportedFromDayBook) = False
                    EnableDisableButtons(False)
                End If

            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to delete sample.", ex)
        End Try
    End Sub

    Private Sub grdBatchSummary_ItemCommand(ByVal source As System.Object, ByVal e As System.Web.UI.WebControls.DataGridCommandEventArgs) Handles grdBatchSummary.ItemCommand
        Dim iCount As Int32 = e.Item.ItemIndex + 1

        'Cell 0 = Select button
        'Cell 1 = Sender Ref field
        'Cell 2 = +/- button
        'Cell 3 = Tissue field

        If e.CommandName = "ExpandTissues" Then
            If CType(e.Item.Cells(3).Controls(0), LinkButton).Text = "<img src=""Images/minus.gif"">" Then
                Do While Not grdBatchSummary.Items(iCount).Cells(4).Text = "&nbsp;"
                    grdBatchSummary.Items(iCount).Visible = False
                    iCount += 1
                    If iCount >= grdBatchSummary.Items.Count Then Exit Do
                Loop
                CType(e.Item.Cells(3).Controls(0), LinkButton).Text = "<img src=""Images/plus.gif"">"
            Else
                'While the sender ref isnt empty show the tissue rows
                Do While Not grdBatchSummary.Items(iCount).Cells(4).Text = "&nbsp;"
                    grdBatchSummary.Items(iCount).Visible = True
                    grdBatchSummary.Items(iCount).Cells(3).Controls(0).Visible = False
                    grdBatchSummary.Items(iCount).Cells(0).Controls(0).Visible = False
                    iCount += 1
                    If iCount >= grdBatchSummary.Items.Count Then Exit Do
                Loop
                CType(e.Item.Cells(3).Controls(0), LinkButton).Text = "<img src=""Images/minus.gif"">"
            End If
        End If
    End Sub

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        Try
            Dim objArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
            If Not objArrayList Is Nothing Then
                objArrayList(1) = "Submission"
                objArrayList(2) = "Submission Details"
                Session.Item(SessionVars.SV_BreadCrumbs) = objArrayList
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Bread Crumb Error, BatchSummary.aspx.", ex)
        End Try

        Response.Redirect("BatchDetails.aspx")
    End Sub

    Private Sub grdBatchSummary_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles grdBatchSummary.SelectedIndexChanged
        EnableDisableButtons(True)
    End Sub

    Private Sub VLAHeader1_HomeClick(ByVal sender As Object, ByVal e As HistopathologySystem.HomeLinkEventArgs) Handles VLAHeader1.HomeClick
        If CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean) = False Then
            Dim sMessage As System.Text.StringBuilder = New System.Text.StringBuilder

            If CType(Session.Item(SessionVars.SV_EditingBatch), Boolean) Then
                sMessage.Append("You are currently editing a submission. Any changes that you have made since you last saved the submission will be lost. Are you sure you wish to exit?")
            ElseIf CType(Session.Item(SessionVars.SV_CreatingNewBatch), Boolean) Then
                sMessage.Append("You are currently creating a new submission. If you exit now all the data you have entered will be lost. Are you sure you wish to exit?")
            Else
                sMessage.Append("Any changes that have been made will be discarded, are you sure you wish to exit without saving?")
            End If

            Page.RegisterStartupScript("navigate", PromptBeforeNavigateScript(sMessage.ToString(), "Home.aspx"))
            e.bNavigateHome = False
        Else
            Response.Redirect("Home.aspx")
        End If
    End Sub

    Private Sub btnCopySubmission_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCopySubmission.Click
        Try
            If grdBatchSummary.SelectedIndex >= 0 Then
                Dim iID As Int32 = grdBatchSummary.DataKeys(grdBatchSummary.SelectedIndex)
                Dim dtData As DataTable = CType(Session.Item(SessionVars.SV_BatchSummaryTable), DataTable)
                Dim drRow As DataRow()
                Dim iAnimalID As Integer
                Dim sFilter As String
                Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
                Dim clsBatchSubmission As New HistopathologyLib.clsBatchSubmission()

                sFilter = "ID=" & iID
                drRow = dtData.Select(sFilter)

                If Not drRow(0) Is Nothing Then
                    Session.Item(SessionVars.SV_AnimalID) = drRow(0)("AnimalID")
                Else
                    Session.Item(SessionVars.SV_AnimalID) = 0
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to copy sample.", ex)
        End Try

        Try
            Dim objArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
            If Not objArrayList Is Nothing Then
                objArrayList(1) = "Submission Samples"
                objArrayList(2) = "Copy Sample"
                Session.Item(SessionVars.SV_BreadCrumbs) = objArrayList
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Bread Crumb Error, BatchSummary.aspx.", ex)
        End Try

        Session.Item(SessionVars.SV_CopySample) = True
        Session.Item(SessionVars.SV_AddSamplePrevPage) = "BatchSummary.aspx"
        Session.Item(SessionVars.SV_AddSampleNextPage) = "BatchSummary.aspx"
        Session.Remove(SessionVars.SV_SenderRef)
        Response.Redirect("AddSample.aspx")
    End Sub

    Private Sub lbCollapseAll_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lbCollapseAll.Click
        SetHierarchical(False)
    End Sub

    Private Sub lbExpandAll_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lbExpandAll.Click
        SetHierarchical(True)
    End Sub

#End Region

#Region "Private Functions"

    Private Sub EnableDisableButtons(ByVal bEnable As Boolean)
        Try
            If CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean) = True Then
                btnDeleteSubmission.Enabled = False
                btnCopySubmission.Enabled = False
                btnEditSubmission.Enabled = False
                btnAddSubmission.Enabled = False
            ElseIf CType(Session.Item(SessionVars.SV_EditingBatch), Boolean) = True Then
                btnDeleteSubmission.Enabled = bEnable
                btnCopySubmission.Enabled = bEnable
                btnEditSubmission.Enabled = bEnable
                btnAddSubmission.Enabled = True
            Else
                btnDeleteSubmission.Enabled = bEnable
                btnCopySubmission.Enabled = bEnable
                btnEditSubmission.Enabled = bEnable
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Unable to enable or disable controls.", ex)
        End Try
    End Sub

    Private Sub SetToolTips()
        btnAddSubmission.ToolTip = ADD_SAMPLE_TOOLTIP
        btnEditSubmission.ToolTip = EDIT_SAMPLE_TOOLTIP
        btnDeleteSubmission.ToolTip = DELETE_SAMPLE_TOOLTIP
        btnCopySubmission.ToolTip = COPY_SAMPLE_TOOLTIP
    End Sub

    Private Sub DisplayNumberSamples()
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtBatchSubmission As DataTable

            If Not dsBatchDetails Is Nothing Then
                'Find the number of samples that have been added against the submission
                dtBatchSubmission = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_SUBMISSION_TABLE)

                lblNumberSamples.Text = "There are " & dtBatchSubmission.Rows.Count _
                                               & " samples on the current submission."
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to display number of samples.", ex)
        End Try
    End Sub

    Private Sub DisplayDetails()
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtBatch As DataTable

            If Not dsBatchDetails Is Nothing Then
                dtBatch = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)

                If Not IsDBNull(dtBatch.Rows(0)("ByPassSort")) Then
                    chkByPassSort.Checked = dtBatch.Rows(0)("ByPassSort")
                Else
                    dtBatch.Rows(0)("ByPassSort") = False
                End If
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to display batch details.", ex)
        End Try
    End Sub
#End Region


    Private Sub btnAddSubmission_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAddSubmission.Click
        Session.Item(SessionVars.SV_Editing) = False
        Session.Item(SessionVars.Sv_CopySubmission) = False

        Session.Remove(SessionVars.SV_BatchSubmissionID)
        Session.Remove(SessionVars.SV_AnimalID)
        Session.Remove(SessionVars.SV_SenderRef)

        Session.Item(SessionVars.SV_AddSampleNextPage) = "SubmissionDetails.aspx"
        Session.Item(SessionVars.SV_AddSamplePrevPage) = "BatchSummary.aspx"
        Session.Item(SessionVars.SV_OldPGNumber) = ""
        Session.Item(SessionVars.SV_PMDate) = ""
        Session.Item(SessionVars.SV_Species) = ""

        Try
            Dim objArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
            If Not objArrayList Is Nothing Then
                objArrayList(1) = "Submission Samples"
                objArrayList(2) = "Tissuing"
                objArrayList.Insert(3, "Add Sample")
                Session.Item(SessionVars.SV_BreadCrumbs) = objArrayList
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Bread Crumb Error, BatchSummary.aspx.", ex)
        End Try

        Response.Redirect("AddSubmission.aspx")
    End Sub
End Class
