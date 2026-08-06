Imports System.Text.RegularExpressions

Partial Class QualityData
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents BlockPager As DataGridPager
    Protected WithEvents Batch1 As Batch
    Protected WithEvents ctlDispatchDate As CalendarDate
    Protected WithEvents txtTmpDispatchedTo As System.Web.UI.WebControls.TextBox
    Protected WithEvents ctlArchiveDate As CalendarDate

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
        VLAHeader1.PageTitle = "Quality Data"
        CheckPermissions()
        BlockPager.SetGrid(grdQuality)
        SetCalendarDateHandler(Me.Page)
        SetClientValidation()

        If Not IsPostBack Then
            Batch1.DisplayDetails()
            InitialiseTestGrid()
            LoadLookupLists()
            LoadCheckBoxLists()
            InitialiseControls()
            SetupRadioButtonList()
            If CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean) = True Then
                EnableControls(False)
            Else
                PromptBeforeSaveScript("Are you sure you want to Cancel? Any quality information entered since you last clicked the Done button will be lost.", btnCancel)
            End If
            CreateQCNoteTable()
            btnSave.Enabled = False
            SetEnterKeyPress()
        End If

    End Sub

#Region "Lookup List Population"

    Private Sub LoadLookupLists()
        Dim objDataTable As DataTable
        Dim objLookup As New HistopathologyLib.LookupData
        Dim objUsers As New HistopathologyLib.clsUser

        Try
            objDataTable = objLookup.GetLookupData(LOOKUP_QC_CODE)
            If Not (objDataTable Is Nothing) Then
                ddlQCCode.DataSource = objDataTable
                ddlQCCode.DataValueField = "Code"
                ddlQCCode.DataTextField = "Description"
                ddlQCCode.DataBind()
                Common.AddItemToDropDownList(ddlQCCode)
            End If

            objDataTable = objUsers.GetUsers()
            If Not (objDataTable Is Nothing) Then
                ddlDispatchedBy.DataSource = objDataTable
                ddlDispatchedBy.DataValueField = "ID"
                ddlDispatchedBy.DataTextField = "Name"
                ddlDispatchedBy.DataBind()
                Common.AddItemToDropDownList(ddlDispatchedBy)
            End If

            objDataTable = objLookup.GetTestResultLookupData()
            If Not (objDataTable Is Nothing) Then
                ddlTestResult.DataSource = objDataTable
                ddlTestResult.DataValueField = "Code"
                ddlTestResult.DataTextField = "Description"
                ddlTestResult.DataBind()
                Common.AddItemToDropDownList(ddlTestResult)
            End If

            objDataTable = objLookup.GetLookupData(LOOKUP_REMEDIAL_ACTION)
            If Not (objDataTable Is Nothing) Then
                ddlRemedialAction.DataSource = objDataTable
                ddlRemedialAction.DataValueField = "Code"
                ddlRemedialAction.DataTextField = "Description"
                ddlRemedialAction.DataBind()
                Common.AddItemToDropDownList(ddlRemedialAction)
            End If

            objDataTable = objLookup.GetLookupData(LOOKUP_ARCHIVE_LOCATION)
            If Not (objDataTable Is Nothing) Then
                ddlArchiveLocation.DataSource = objDataTable
                ddlArchiveLocation.DataValueField = "Code"
                ddlArchiveLocation.DataTextField = "Description"
                ddlArchiveLocation.DataBind()
                Common.AddItemToDropDownList(ddlArchiveLocation)
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve 'Quality Data' drop down lists.", ex)
        End Try

    End Sub

    Private Sub LoadCheckBoxLists()
        Try
            Dim objDataTable As DataTable
            Dim objLookup As New HistopathologyLib.LookupData

            objDataTable = objLookup.GetLookupData(LOOKUP_PREMIUM_CHARGES)
            If Not (objDataTable Is Nothing) Then
                chkblTCCodes.DataSource = objDataTable
                chkblTCCodes.DataValueField = "Code"
                chkblTCCodes.DataTextField = "Description"
                chkblTCCodes.DataBind()
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve 'Quality Data' check box lists.", ex)
        End Try
    End Sub
#End Region

#Region "Grid Handling"

    Private Sub InitialiseTestGrid()
        Try
            Dim dsDataSet As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)

            If Not dsDataSet Is Nothing Then

                Dim objSummary As New HistopathologyLib.clsBatchSummary
                Dim dtBlockTests As New DataTable
                Dim dtTestList As DataTable
                Dim dtHistologyRefList As DataTable

                If Not objSummary.CreateTestSummaryData(dsDataSet, dtBlockTests, dtTestList, dtHistologyRefList) Then
                    Throw New Exception("BatchSummary.CreateTestSummaryData return false")
                End If

                ' create a dataview for filtering and sorting
                Dim dv As DataView = dtBlockTests.DefaultView

                Session.Item(SessionVars.SV_BlockSummaryTable) = dtBlockTests
                Session.Item(SessionVars.SV_BlockSummaryView) = dv

                grdQuality.DataSource = dtBlockTests
                grdQuality.DataKeyField = "ID"
                grdQuality.DataBind()
                grdQuality.Enabled = True

                ' initialise the pager
                BlockPager.DataTableSessionID = SessionVars.SV_BlockSummaryTable
                BlockPager.DataViewSessionID = SessionVars.SV_BlockSummaryView
                BlockPager.PageLinkCount = 10
                BlockPager.AllowAddNew = False
                BlockPager.AllowEdit = False
                BlockPager.AllowDelete = False
                BlockPager.Rebind()
                BlockPager.Refresh()

                'Setup the filter dropdown lists
                If Not dtTestList Is Nothing Then
                    ddlTestList.DataSource = dtTestList
                    ddlTestList.DataValueField = "ID"
                    ddlTestList.DataTextField = "Description"
                    ddlTestList.DataBind()
                    AddItemToDropDownList(ddlTestList, "")
                End If

                If Not dtHistologyRefList Is Nothing Then
                    ddlHistologyRefList.DataSource = dtHistologyRefList
                    ddlHistologyRefList.DataValueField = "ID"
                    ddlHistologyRefList.DataTextField = "Description"
                    ddlHistologyRefList.DataBind()
                    AddItemToDropDownList(ddlHistologyRefList, "")
                End If
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Error initialising the Tests Grid", ex)
        End Try
    End Sub

#End Region

#Region "Event Handlers"

    Private Sub btnGoToPage_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnGoToPage.Click
        Try
            Dim iPageNumber As Integer = 0

            If ValidateMandatoryPageFields() Then
                iPageNumber = CInt(txtPage.Text)
                If iPageNumber > 0 Then
                    grdQuality.CurrentPageIndex = iPageNumber - 1
                Else
                    grdQuality.CurrentPageIndex = 0
                End If
                grdQuality.SelectedIndex = -1
                grdQuality.EditItemIndex = -1
                BlockPager.Rebind()
                BlockPager.Refresh()
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to go to selected page.", ex)
        End Try
    End Sub

    Private Sub ddlArchiveLocation_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ddlArchiveLocation.SelectedIndexChanged
        If ddlArchiveLocation.SelectedIndex = 0 Then
            ctlArchiveDate.Mandatory = False
        Else
            ctlArchiveDate.Mandatory = True
            lblArchiveLocationError.Visible = False
        End If
    End Sub

    Private Sub grdQuality_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles grdQuality.SelectedIndexChanged
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtStainTCCodes As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BLOCK_SPECIALSTAIN_TCCODES)
            Dim dtAntibodiesTCCodes As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BLOCK_ANTIBODIES_TCCODES)
            Dim dtHistologyTCCodes As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BLOCK_HISTOLOGY_TCCODES)
            Dim drData As DataRow()
            Dim drRow As DataRow
            Dim dtData As DataTable = CType(Session.Item(SessionVars.SV_BlockSummaryTable), DataTable)
            Dim dgItem As DataGridItem = grdQuality.SelectedItem
            Dim iID As Int32 = Convert.ToInt32(grdQuality.DataKeys(grdQuality.SelectedIndex))
            Dim sFilter As String = "ID = " & CStr(iID)
            Dim cbSelected As CheckBox
            Dim cbOnHOld As CheckBox
            Dim cbOtherSelected As CheckBox
            Dim sTestResult As String
            Dim sTestType As String
            Dim bViewSubmission As Boolean = CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean)

            If bViewSubmission Then
                EnableControls(False)
            Else
                EnableControls(True)
            End If

            If Not bViewSubmission Then
                If Not dgItem Is Nothing Then
                    cbSelected = dgItem.FindControl("cbSelected")

                    If Not cbSelected Is Nothing Then
                        If cbSelected.Enabled = False Then
                            Exit Sub
                        End If
                        For Each dgItem In grdQuality.Items
                            cbOtherSelected = dgItem.FindControl("cbSelected")

                            If Not cbOtherSelected Is Nothing Then
                                cbOtherSelected.Checked = False
                            End If
                        Next
                        'Also set the datatable value selected to false
                        For Each drRow In dtData.Rows
                            drRow("Selected") = False
                        Next
                        cbSelected.Checked = True
                    End If
                End If
            End If

            drData = dtData.Select(sFilter)
            With drData(0)
                drData(0)("Selected") = True
                sTestResult = .Item("Result").ToString()
                If sTestResult = "0" Or sTestResult = "" Then
                    ddlTestResult.SelectedIndex = 0
                    chkDispatched.Enabled = False
                Else
                    SelectItemInDropDownList(ddlTestResult, sTestResult)
                    chkDispatched.Enabled = True
                End If
                chkQCNote.Checked = GetRowColumnData(.Item("QCNote"))
                SelectItemInDropDownList(ddlQCCode, .Item("QCCode").ToString())

                If Not CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean) = True Then
                    If sTestResult = "1" Then 'pass
                        chkQCNote.Enabled = True
                        If ddlQCCode.SelectedIndex = -1 Then
                            ddlQCCode.Enabled = False
                        End If
                        rfvQCCode.Enabled = False
                    ElseIf sTestResult = "2" Then
                        chkQCNote.Enabled = False
                        chkQCNote.Checked = False
                        ddlQCCode.Enabled = True
                        rfvQCCode.Enabled = True
                        txtQCNoteRef.Text = ""
                    Else
                        chkQCNote.Enabled = False
                        If ddlQCCode.SelectedIndex = -1 Then
                            ddlQCCode.Enabled = False
                        End If
                        rfvQCCode.Enabled = False
                    End If
                End If

                'Only display the QCNote if it has a correct index
                If Not IsDBNull(.Item("QCNoteRef")) Then
                    If CInt(.Item("QCNoteRef")) > 0 Then
                        txtQCNoteRef.Text = .Item("QCNoteRef").ToString()
                    Else
                        txtQCNoteRef.Text = ""
                    End If
                Else
                    txtQCNoteRef.Text = ""
                End If

                txtStainRef.Text = .Item("StainRef").ToString()
                ctlDispatchDate.DateField = .Item("DispatchedDate").ToString()
                chkDispatched.Checked = GetRowColumnData(.Item("Dispatched"))
                SelectItemInDropDownList(ddlDispatchedBy, .Item("DispatchedBy").ToString())
                txtComment.Text = .Item("Comment").ToString()
                txtDispatchedTo.Text = .Item("DispatchedTo").ToString()
                SelectItemInDropDownList(ddlRemedialAction, .Item("RemedialAction").ToString())
                SelectItemInDropDownList(ddlArchiveLocation, .Item("ArchiveLocation").ToString())
                ctlArchiveDate.DateField = .Item("ArchivedDate").ToString()
                txtArchiveComment.Text = .Item("ArchiveComment").ToString()
                txtNumberOfSlides.Text = .Item("NumberOfSlides").ToString()

                sTestType = .Item("TestType").ToString

                chkblTCCodes.SelectedIndex = -1
                If sTestType = "Histology" Then
                    SetTCCodesForTest(dtHistologyTCCodes, .Item("TestID"))
                ElseIf sTestType = "Antibodies" Then
                    SetTCCodesForTest(dtAntibodiesTCCodes, .Item("TestID"))
                ElseIf sTestType = "Stain" Then
                    SetTCCodesForTest(dtStainTCCodes, .Item("TestID"))
                End If
            End With

            If Not CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean) = True Then
                btnEdit.Enabled = True
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to display details for selected item.", ex)
        End Try
    End Sub

    Private Sub SetTCCodesForTest(ByVal dtData As DataTable, ByVal iID As Integer)
        Dim drFoundRows As DataRow()
        Dim sFilter As String
        Dim li As ListItem
        Dim dr As DataRow

        sFilter = "TestID=" & iID
        drFoundRows = dtData.Select(sFilter)

        For Each dr In drFoundRows
            For Each li In chkblTCCodes.Items
                If li.Value = dr("Code") Then
                    li.Selected = True
                End If
            Next
        Next
    End Sub


    Private Function ValidateMandatoryFields(ByVal dsBatchDetails As DataSet) As Boolean
        Dim dDate As Date
        Dim bNoError As Boolean = True
        Dim dReceivedDate As Date

        If Not dsBatchDetails Is Nothing AndAlso dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE).Rows.Count > 0 Then
            If Not IsDBNull(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("DateReceived")) Then
                dReceivedDate = CType(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("DateReceived"), Date)
            End If
        End If

        If chkDispatched.Checked = True Then
            If ctlDispatchDate.DateField = "" Then
                lblError.Visible = True
                bNoError = False
            End If
        End If

        If ctlArchiveDate.DateField <> "" And ddlArchiveLocation.SelectedIndex = 0 Then
            lblArchiveLocationError.Visible = True
            bNoError = False
        End If

        If ddlArchiveLocation.SelectedIndex <> 0 Then
            If Not ctlArchiveDate.IsComplete() Then
                bNoError = False
            End If
        End If

        If Not ctlDispatchDate.Validate(dDate, CDate(dReceivedDate.ToShortDateString), ctlDispatchDate.ValidationType.eValidateEarliest, "Dispatch date must be the same or later than the Submission received date of " & dReceivedDate.ToShortDateString) Or _
           Not ctlDispatchDate.Validate(dDate, CDate(dDate.Date.Now.ToShortDateString), ctlDispatchDate.ValidationType.eValidateLatest, "Must be today or earlier") Or _
           Not ctlArchiveDate.Validate(dDate, CDate(dReceivedDate.ToShortDateString), ctlArchiveDate.ValidationType.eValidateEarliest, "Archive date must be the same or later than the Submission received date of " & dReceivedDate.ToShortDateString) Or _
           Not ctlArchiveDate.Validate(dDate, CDate(dDate.Now.ToShortDateString), ctlArchiveDate.ValidationType.eValidateLatest, "Must be today or earlier") Then
            bNoError = False
        End If

        rfvDispatchedTo.Validate()
        rfvDispatchedBy.Validate()
        revNumberOfSlides.Validate()
        rfvNumberOfSlides.Validate()
        rfvQCCode.Validate()
        rfvRemedialAction.Validate()

        If Not rfvDispatchedTo.IsValid Or _
           Not rfvDispatchedBy.IsValid Or _
           Not revNumberOfSlides.IsValid Or _
           Not rfvQCCode.IsValid Or _
           Not rfvRemedialAction.IsValid Or _
           Not rfvNumberOfSlides.IsValid Then
            ctlDIV.InnerHtml = "<p><font color=""Red"">Not all mandatory fields have been completed, hover the mouse pointer over the red stars for details.</font></p>"
            bNoError = False
        End If

        Return bNoError
    End Function

    Private Sub btnEdit_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnEdit.Click
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtStainTCCodes As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BLOCK_SPECIALSTAIN_TCCODES)
            Dim dtAntibodiesTCCodes As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BLOCK_ANTIBODIES_TCCODES)
            Dim dtHistologyTCCodes As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BLOCK_HISTOLOGY_TCCODES)
            Dim dtQCNOtes As DataTable = CType(Session.Item(SessionVars.SV_QCNoteNumbers), DataTable)
            Dim objQCNote As New HistopathologyLib.clsQCNote
            Dim drRow As DataRow
            Dim dtData As DataTable = CType(Session.Item(SessionVars.SV_BlockSummaryTable), DataTable)
            Dim sFilter As String
            Dim iID As Int32
            Dim iCount As Integer = 0
            Dim sTestType As String
            Dim dgItem As DataGridItem
            Dim cbSelected As CheckBox
            Dim iSelectedItemCount As Integer = 0
            Dim iQCNoteID As Integer = 0

            If Not ValidateMandatoryFields(dsBatchDetails) Then
                Exit Sub
            End If

            If Not dtData Is Nothing Then
                For Each drRow In dtData.Rows
                    If Not IsDBNull(drRow("Selected")) Then
                        If drRow("Selected") = True Then
                            iSelectedItemCount = iSelectedItemCount + 1
                        End If
                    End If
                Next

                'For each item in the grid that has been selected update the grid bound datatable with the
                'quality data that the user has entered.
                For Each drRow In dtData.Rows
                    If Not IsDBNull(drRow("Selected")) Then
                        If drRow("Selected") = True Then
                            drRow("Selected") = False

                            'If more than 2 items have been selected only update the fields that have changed, i.e from blanks
                            If iSelectedItemCount >= 2 Then
                                With drRow
                                    If ddlTestResult.SelectedIndex <> 0 Then
                                        .Item("Result") = ddlTestResult.SelectedItem.Value()
                                        If .Item("Result").ToString() = "1" Then
                                            .Item("Passed") = True
                                            .Item("Failed") = False
                                        ElseIf .Item("Result").ToString() = "2" Then
                                            .Item("Passed") = False
                                            .Item("Failed") = True
                                        End If
                                    End If
                                    If txtStainRef.Text <> "" Then
                                        .Item("StainRef") = txtStainRef.Text
                                    End If

                                    If ddlQCCode.SelectedIndex <> 0 Then
                                        .Item("QCCode") = ddlQCCode.SelectedItem.Value()
                                    End If

                                    If chkQCNote.Checked = True Then
                                        If .Item("QCNoteRef").ToString = "" Then
                                            .Item("QCNote") = FormatEmptyString(chkQCNote.Checked())
                                            If Not iQCNoteID <> 0 Then
                                                If Not objQCNote.NewQCNote(dtQCNOtes, CInt(Session.Item(SessionVars.SV_HeaderUserID)), iQCNoteID) Then
                                                    Throw New Exception("QCNote.NewQCNote returned false")
                                                End If
                                                .Item("QCNoteRef") = iQCNoteID
                                            Else
                                                .Item("QCNoteRef") = iQCNoteID
                                            End If
                                        End If
                                    End If

                                    If ctlDispatchDate.DateField <> "" Then
                                        .Item("DispatchedDate") = FormatEmptyString(ctlDispatchDate.DateField)
                                    End If

                                    If ddlDispatchedBy.SelectedIndex <> 0 Then
                                        .Item("DispatchedBy") = ddlDispatchedBy.SelectedItem.Value()
                                    End If

                                    If chkDispatched.Checked = True Then
                                        .Item("Dispatched") = FormatEmptyString(chkDispatched.Checked())
                                    End If

                                    If txtDispatchedTo.Text <> "" Then
                                        .Item("DispatchedTo") = txtDispatchedTo.Text
                                    End If

                                    .Item("EnteredBy") = CInt(Session.Item(SessionVars.SV_HeaderUserID))

                                    If txtComment.Text <> "" Then
                                        .Item("Comment") = txtComment.Text
                                    End If

                                    If ddlRemedialAction.SelectedIndex <> 0 Then
                                        .Item("RemedialAction") = ddlRemedialAction.SelectedItem.Value()
                                    End If

                                    If ddlArchiveLocation.SelectedIndex <> 0 Then
                                        .Item("ArchiveLocation") = ddlArchiveLocation.SelectedItem.Value()
                                    End If

                                    If ctlArchiveDate.DateField <> "" Then
                                        .Item("ArchivedDate") = FormatEmptyString(ctlArchiveDate.DateField)
                                    End If

                                    If ctlArchiveDate.DateField <> "" And ddlArchiveLocation.SelectedIndex <> 0 Then
                                        .Item("Archived") = True
                                    End If

                                    If txtArchiveComment.Text <> "" Then
                                        .Item("ArchiveComment") = txtArchiveComment.Text
                                    End If

                                    If txtNumberOfSlides.Text <> txtTmpNoSlides.Text Then
                                        .Item("NumberOfSlides") = Convert.ToInt16(txtNumberOfSlides.Text)
                                    End If

                                    sTestType = .Item("TestType").ToString()

                                    If chkblTCCodes.SelectedIndex <> -1 Then
                                        If sTestType = "Histology" Then
                                            UpdateCheckBoxData(dtHistologyTCCodes, .Item("TestID"))
                                        ElseIf sTestType = "Antibodies" Then
                                            UpdateCheckBoxData(dtAntibodiesTCCodes, .Item("TestID"))
                                        ElseIf sTestType = "Stain" Then
                                            UpdateCheckBoxData(dtStainTCCodes, .Item("TestID"))
                                        End If
                                    End If
                                End With
                            Else
                                With drRow
                                    .Item("Result") = ddlTestResult.SelectedItem.Value()
                                    If .Item("Result").ToString() = "1" Then
                                        .Item("Passed") = True
                                        .Item("Failed") = False
                                    ElseIf .Item("Result").ToString() = "2" Then
                                        .Item("Passed") = False
                                        .Item("Failed") = True
                                    End If
                                    .Item("StainRef") = txtStainRef.Text
                                    .Item("QCCode") = ddlQCCode.SelectedItem.Value()
                                    .Item("QCNote") = chkQCNote.Checked()

                                    If chkQCNote.Checked = True Then
                                        If .Item("QCNoteRef").ToString = "" Then
                                            If Not iQCNoteID <> 0 Then
                                                If Not objQCNote.NewQCNote(dtQCNOtes, CInt(Session.Item(SessionVars.SV_HeaderUserID)), iQCNoteID) Then
                                                    Throw New Exception("QCNote.NewQCNote returned false")
                                                End If
                                                .Item("QCNoteRef") = iQCNoteID
                                            Else
                                                .Item("QCNoteRef") = iQCNoteID
                                            End If
                                        End If
                                    Else
                                        If Not IsDBNull(.Item("QCNoteRef")) Then
                                            RemoveQCNoteRef(.Item("QCNoteRef"), CType(Session.Item(SessionVars.SV_QCNoteNumbers), DataTable), dtData)
                                        End If
                                        txtQCNoteRef.Text = ""
                                        .Item("QCNoteRef") = DBNull.Value
                                    End If

                                    .Item("DispatchedDate") = FormatEmptyString(ctlDispatchDate.DateField)
                                    .Item("DispatchedBy") = ddlDispatchedBy.SelectedItem.Value()
                                    .Item("Dispatched") = FormatEmptyString(chkDispatched.Checked())
                                    .Item("DispatchedTo") = txtDispatchedTo.Text
                                    .Item("EnteredBy") = CInt(Session.Item(SessionVars.SV_HeaderUserID))
                                    .Item("Comment") = txtComment.Text
                                    .Item("RemedialAction") = ddlRemedialAction.SelectedItem.Value()
                                    .Item("ArchiveLocation") = ddlArchiveLocation.SelectedItem.Value()
                                    .Item("ArchivedDate") = FormatEmptyString(ctlArchiveDate.DateField)
                                    .Item("ArchiveComment") = txtArchiveComment.Text

                                    If ctlArchiveDate.DateField <> "" And ddlArchiveLocation.SelectedIndex <> 0 Then
                                        .Item("Archived") = True
                                    End If

                                    .Item("NumberOfSlides") = Convert.ToInt16(txtNumberOfSlides.Text)

                                    sTestType = .Item("TestType").ToString()

                                    If sTestType = "Histology" Then
                                        UpdateCheckBoxData(dtHistologyTCCodes, .Item("TestID"))
                                    ElseIf sTestType = "Antibodies" Then
                                        UpdateCheckBoxData(dtAntibodiesTCCodes, .Item("TestID"))
                                    ElseIf sTestType = "Stain" Then
                                        UpdateCheckBoxData(dtStainTCCodes, .Item("TestID"))
                                    End If
                                End With
                            End If
                        End If
                    End If
                Next

                InitialiseControls()
                BlockPager.Rebind()
                BlockPager.Refresh()
                chkSelectAll.Checked = False
                btnEdit.Enabled = False

                btnSave.Enabled = True
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to update the selected records with the quality information.", ex)
        End Try
    End Sub

    Private Sub RemoveQCNoteRef(ByVal iQCNoteRef As Integer, ByRef dtQCNoteTable As DataTable, ByVal dtDataGridTable As DataTable)
        Dim drFoundRows As DataRow()
        Dim drRow As DataRow = Nothing

        drFoundRows = dtDataGridTable.Select("QCNoteRef=" & iQCNoteRef)

        If Not drFoundRows Is Nothing Then
            If drFoundRows.Length = 1 Then
                drRow = dtQCNoteTable.Rows.Find(iQCNoteRef)
                If Not drRow Is Nothing Then
                    dtQCNoteTable.Rows.Remove(drRow)
                End If
            End If
        End If

    End Sub

    Private Sub btnSave_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSave.Click
        Dim bRedirect As Boolean = False
        Dim objErrorlist As New ArrayList
        Dim objBatch As New HistopathologyLib.clsBatch
        Dim iBatchID As Integer
        Dim dtQCNOtes As DataTable = CType(Session.Item(SessionVars.SV_QCNoteNumbers), DataTable)
        Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)

        'Update the submission status first as UpdateSessionWithQualityData may set
        ' the overall status to completed
        UpdateSubmissionStatus()
        UpdateSessionWithQualityData()

        Dim bSuccess As Boolean = objBatch.UpdateBatchDetails(CType(Session(SessionVars.SV_HeaderUserID), Integer), _
                                  dsBatchDetails, _
                                  objErrorlist, _
                                  True, _
                                  iBatchID, _
                                  dtQCNOtes)
        If bSuccess Then
            If objErrorlist.Count = 0 Then
                bRedirect = True
            Else
                ctlDIV.InnerHtml = "<p><font color=""Red"">The database has been updated but some errors were encountered:</font></p><p>&nbsp;</p><p><font color=""Red"">" & Join(objErrorlist.ToArray, "</font></p><p>") & "</p>"
            End If
        Else
            ctlDIV.InnerHtml = "<p><font color=""Red"">The database has not been updated because the following error(s) occurred:</font></p><p>&nbsp;</p><p><font color=""Red"">" & Join(objErrorlist.ToArray, "</font></p><p>") & "</p>"
        End If

        If bRedirect Then
            If dtQCNOtes.Rows.Count > 0 Then
                Response.Redirect("QCNotes.aspx")
            Else
                Response.Redirect("BatchesForDispatch.aspx")
            End If
        End If

    End Sub

    Private Sub grdQuality_ItemDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.DataGridItemEventArgs) Handles grdQuality.ItemDataBound
        ' populate template column values here
        Try
            Dim bViewSubmission As Boolean = CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean)
            ' set up the checkbox and drop-down columns
            Dim drv As DataRowView = CType(e.Item.DataItem, DataRowView)
            If Not drv Is Nothing Then
                Dim cbDispatched As CheckBox = Nothing
                Dim cbPassed As CheckBox = Nothing
                Dim cbFailed As CheckBox = Nothing
                Dim sResult As String
                Dim cbSelect As CheckBox = Nothing
                Dim cbOnHold As CheckBox = Nothing
                Dim cbArchived As CheckBox = Nothing

                If e.Item.ItemType = ListItemType.EditItem Then
                ElseIf e.Item.ItemType = ListItemType.Item _
                OrElse e.Item.ItemType = ListItemType.AlternatingItem _
                OrElse e.Item.ItemType = ListItemType.SelectedItem Then
                    ' populate display mode controls
                    cbDispatched = CType(e.Item.FindControl("cbDispatchedDisplay"), CheckBox)
                    cbPassed = CType(e.Item.FindControl("cbPassed"), CheckBox)
                    cbFailed = CType(e.Item.FindControl("cbFailed"), CheckBox)
                    cbSelect = CType(e.Item.FindControl("cbSelected"), CheckBox)
                    cbOnHold = CType(e.Item.FindControl("cbOnHold"), CheckBox)
                    cbArchived = CType(e.Item.FindControl("cbArchived"), CheckBox)
                End If

                If Not cbSelect Is Nothing Then
                    If Not IsDBNull(drv("OnHold")) Then
                        cbSelect.Enabled = Not drv("OnHold")

                        If cbSelect.Enabled = False Then
                            chkSelectAll.Enabled = False
                        End If

                        If cbSelect.Enabled = True Then
                            If Not IsDBNull(drv("Selected")) Then
                                cbSelect.Checked = drv("Selected")
                            End If
                        End If
                    End If

                    If drv("HistologyRef").ToString() = "" Or IsDBNull(drv("HistologyRef")) Then
                        chkSelectAll.Enabled = False
                        cbSelect.Enabled = False
                        cbSelect.ToolTip = "Unable to enter Quality information as the Histology Ref has not been entered for the sample."
                    End If

                    If bViewSubmission Then
                        cbSelect.Enabled = False
                    End If
                End If

                If Not cbArchived Is Nothing Then
                    If Not IsDBNull(drv("Archived")) Then
                        cbArchived.Checked = drv("Archived")
                    End If
                End If

                If Not cbOnHold Is Nothing Then
                    If Not IsDBNull(drv("OnHold")) Then
                        cbOnHold.Checked = drv("OnHold")
                    End If
                End If

                If Not IsDBNull(drv("Result")) Then
                    sResult = drv("Result").ToString()
                End If

                e.Item.CssClass = "GridDefaultBackGround"
                'let the row colours get overridden by a status that is more important, i.e Dispatched > pass

                If Not cbPassed Is Nothing Then
                    If Not IsDBNull(drv("Passed")) Then
                        cbPassed.Checked = drv("Passed")
                        If cbPassed.Checked Then
                            e.Item.CssClass = "GridPassedBackGround"
                        End If
                    End If
                End If

                If Not cbFailed Is Nothing Then
                    If Not IsDBNull(drv("Failed")) Then
                        cbFailed.Checked = drv("Failed")
                        If cbFailed.Checked Then
                            e.Item.CssClass = "GridFailedBackGround"
                        End If
                    End If
                End If

                If Not cbDispatched Is Nothing Then
                    If Not IsDBNull(drv("Dispatched")) Then
                        cbDispatched.Checked = drv("Dispatched").ToString()

                        If cbDispatched.Checked = True Then
                            e.Item.CssClass = "GridItem"
                        End If
                    Else
                        cbDispatched.Checked = False
                    End If
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to bind template columns in the Quality data grid", ex)
        End Try
    End Sub

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        Dim sRedirectCancelPage As String = CStr(Session.Item(SessionVars.SV_RedirectCancelPage))
        If sRedirectCancelPage = "BatchesForDispatch.aspx" Then
            RemoveSessionVars(Session)
        End If

        Response.Redirect(sRedirectCancelPage)
    End Sub

    Private Sub VLAHeader1_HomeClick(ByVal sender As Object, ByVal e As HistopathologySystem.HomeLinkEventArgs) Handles VLAHeader1.HomeClick
        If CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean) = False Then
            Dim sMessage As System.Text.StringBuilder = New System.Text.StringBuilder

            sMessage.Append("You are currently entering Quality data. Any data that you have entered since you last saved will be lost. Are you sure you wish to exit?")

            Page.RegisterStartupScript("navigate", PromptBeforeNavigateScript(sMessage.ToString(), "Home.aspx"))
            e.bNavigateHome = False
        Else
            Response.Redirect("Home.aspx")
        End If
    End Sub

    Private Sub rblFilter_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles rblFilter.SelectedIndexChanged
        Try
            If rblFilter.SelectedItem.Value = "All" Then
                Dim dtData As DataTable = CType(Session.Item(SessionVars.SV_BlockSummaryTable), DataTable)
                Dim dv As DataView = CType(Session.Item(SessionVars.SV_BlockSummaryView), DataView)
                Dim dgItem As DataGridItem
                Dim iFoundRow As Integer = 0
                Dim drRow As DataRow
                Dim sFilter As String
                Dim sHistoRef As String
                Dim sTest As String
                Dim chkSelected As CheckBox
                Dim bSelected As Boolean = chkSelectAll.Checked

                If Not dv Is Nothing AndAlso Not dtData Is Nothing Then
                    dv.Sort = "ID DESC"
                    dv.RowFilter = ""

                    grdQuality.CurrentPageIndex = 0
                    grdQuality.SelectedIndex = -1
                    grdQuality.EditItemIndex = -1

                    BlockPager.Rebind()
                    BlockPager.Refresh()
                End If

                SelectItemInDropDownList(ddlHistologyRefList, "")
                SelectItemInDropDownList(ddlTestList, "")

                '----
                'If any items that are in the view were selected before filtering
                'then leave them selected, otherwise selected
                For Each drRow In dtData.Rows
                    If chkSelectAll.Checked = True Then
                        drRow("Selected") = True
                    End If
                Next

                'Check all the columns
                For Each dgItem In grdQuality.Items
                    chkSelected = dgItem.FindControl("cbSelected")

                    If Not chkSelected Is Nothing Then
                        If bSelected Then
                            chkSelected.Checked = True
                        End If
                    End If
                Next
                '----
                'chkSelectAll.Checked = False
                InitialiseControls()
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to filter the dispatch information.", ex)
        End Try
    End Sub

    Private Sub btnFilter_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnFilter.Click
        Try
            Dim dv As DataView = CType(Session(SessionVars.SV_BlockSummaryView), DataView)
            Dim dtData As DataTable = CType(Session.Item(SessionVars.SV_BlockSummaryTable), DataTable)
            Dim iFoundRow As Integer = 0
            Dim drRow As DataRow
            Dim sFilter As String
            Dim sHistoRef As String
            Dim sTest As String
            Dim li As ListItem

            For Each li In rblFilter.Items
                If li.Value = "Filter" Then
                    li.Selected = True
                Else
                    li.Selected = False
                End If
            Next

            If Not dv Is Nothing AndAlso Not dtData Is Nothing Then
                sHistoRef = "HistologyRef=" & "'" & ddlHistologyRefList.SelectedItem.Text() & "'"
                sTest = "TestDetails=" & "'" & ddlTestList.SelectedItem.Text() & "'"

                If ddlHistologyRefList.SelectedItem.Value <> "" Then
                    sFilter = sHistoRef
                    If ddlTestList.SelectedItem.Value <> "" Then
                        sFilter = sFilter & " AND " & sTest
                    End If
                Else
                    If ddlTestList.SelectedItem.Value <> "" Then
                        sFilter = sTest
                    End If
                End If

                dv.RowFilter = sFilter

                '----
                dv.Sort = "ID DESC"

                'If any items that are in the view were selected before filtering
                'then leave them selected, otherwise selected

                For Each drRow In dtData.Rows
                    iFoundRow = dv.Find(drRow("ID"))

                    If Not iFoundRow <> -1 Then
                        drRow("Selected") = False
                    Else
                        If chkSelectAll.Checked = True Then
                            drRow("Selected") = True
                        End If
                    End If
                Next
                '----

                InitialiseControls()
                grdQuality.CurrentPageIndex = 0
                grdQuality.SelectedIndex = -1
                grdQuality.EditItemIndex = -1
                BlockPager.Rebind()
                BlockPager.Refresh()
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to filter the dispatch information.", ex)
        End Try
    End Sub

    Private Sub chkQCNote_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkQCNote.CheckedChanged
        txtQCNoteRef.Text = ""
    End Sub

    Private Sub chkSelectAll_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkSelectAll.CheckedChanged
        Try
            Dim dtData As DataTable = CType(Session.Item(SessionVars.SV_BlockSummaryTable), DataTable)
            Dim dv As DataView = CType(Session(SessionVars.SV_BlockSummaryView), DataView)
            Dim drRow As DataRow
            Dim dgItem As DataGridItem
            Dim chkSelected As CheckBox
            Dim bSelected As Boolean = chkSelectAll.Checked
            Dim iFoundRow As Integer = 0

            'Check all the columns
            For Each dgItem In grdQuality.Items
                chkSelected = dgItem.FindControl("cbSelected")

                If Not chkSelected Is Nothing Then
                    If chkSelected.Enabled Then
                        chkSelected.Checked = bSelected
                    End If
                End If
            Next

            If Not dtData Is Nothing AndAlso Not dv Is Nothing Then
                For Each drRow In dtData.Rows
                    If Not IsDBNull(drRow("OnHold")) Then
                        If drRow("OnHold") = False Then
                            drRow("Selected") = bSelected
                        Else
                            drRow("Selected") = False
                        End If
                    End If

                    If drRow("HistologyRef").ToString() = "" Or IsDBNull(drRow("HistologyRef")) Then
                        drRow("Selected") = False
                    End If
                Next

                '----
                dv.Sort = "ID DESC"

                'If any items that are in the view were selected before filtering
                'then leave them selected, otherwise selected
                For Each drRow In dtData.Rows
                    iFoundRow = dv.Find(drRow("ID"))

                    If Not iFoundRow <> -1 Then
                        drRow("Selected") = False
                    Else
                        If chkSelectAll.Checked = True Then
                            drRow("Selected") = True
                        End If
                    End If
                Next
                '----

            End If

            btnEdit.Enabled = chkSelectAll.Checked

            InitialiseControls()

        Catch ex As Exception
            clsAppError.DisplayError("Failed to select all tests.", ex)
        End Try
    End Sub

    Private Sub ddlTestResult_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ddlTestResult.SelectedIndexChanged
        If ddlTestResult.SelectedItem.Value = "2" Then 'fail
            ddlRemedialAction.Enabled = True
            rfvRemedialAction.Enabled = True
            ddlQCCode.Enabled = True
            rfvQCCode.Enabled = True
            chkQCNote.Enabled = False
            chkQCNote.Checked = False
            txtQCNoteRef.Text = ""
            txtQCNoteRef.Enabled = False
            chkDispatched.Enabled = True
        ElseIf ddlTestResult.SelectedItem.Value = "1" Then
            chkQCNote.Enabled = True
            ddlQCCode.Enabled = True
            rfvQCCode.Enabled = False
            ddlRemedialAction.Enabled = True
            rfvRemedialAction.Enabled = False
            SelectItemInDropDownList(ddlQCCode, "")
            SelectItemInDropDownList(ddlRemedialAction, "")
            txtQCNoteRef.Text = ""
            txtQCNoteRef.Enabled = False
            chkDispatched.Enabled = True
        Else
            chkQCNote.Enabled = True
            ddlQCCode.Enabled = True
            rfvQCCode.Enabled = False
            ddlRemedialAction.Enabled = True
            rfvRemedialAction.Enabled = False
            SelectItemInDropDownList(ddlQCCode, "")
            SelectItemInDropDownList(ddlRemedialAction, "")
            txtQCNoteRef.Text = ""
            txtQCNoteRef.Enabled = False
            chkDispatched.Enabled = False
        End If
    End Sub

    Private Sub chkDispatched_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkDispatched.CheckedChanged
        'if dispatch has been selected prepopulate the dispath fields
        If chkDispatched.Checked = True Then
            If ctlDispatchDate.DateField = "" Then
                ctlDispatchDate.DateField = New Date().Now()
            End If

            rfvDispatchedBy.Enabled = True
            If ddlDispatchedBy.SelectedItem.Value = "" Then
                SelectItemInDropDownList(ddlDispatchedBy, CType(Session.Item(SessionVars.SV_HeaderUserID), String))
            End If

            rfvDispatchedTo.Enabled = True
            If txtDispatchedTo.Text = "" Then
                Dim txtContact As Label
                txtContact = Batch1.FindControl("lblSubmittedByVal")
                If Not txtContact Is Nothing Then
                    txtDispatchedTo.Text = txtContact.Text
                End If
            End If
        Else
            ctlDispatchDate.DateField = ""

            rfvDispatchedTo.Enabled = False
            txtDispatchedTo.Text = ""

            rfvDispatchedBy.Enabled = False
            SelectItemInDropDownList(ddlDispatchedBy, "")

            rfvRemedialAction.Enabled = False
        End If
    End Sub

    Public Sub Check_Clicked(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Try
            If Not CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean) = True Then
                Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
                Dim iID As Int32
                Dim sFilter As String
                Dim dtStainTCCodes As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BLOCK_SPECIALSTAIN_TCCODES)
                Dim dtAntibodiesTCCodes As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BLOCK_ANTIBODIES_TCCODES)
                Dim dtHistologyTCCodes As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BLOCK_HISTOLOGY_TCCODES)
                Dim dtData As DataTable = CType(Session.Item(SessionVars.SV_BlockSummaryTable), DataTable)
                Dim drData As DataRow()
                Dim sTestResult As String
                Dim sTestType As String
                Dim dgItem As DataGridItem
                Dim chkSelected As CheckBox
                Dim bSelected As Boolean = False
                Dim iCount As Integer = 0
                Dim iRowCount As Integer = 0
                Dim iIndex As Integer
                Dim drRow As DataRow
                Dim iIDBackup As Int32

                'Update the tick boxes and Refind the datarow we need to update
                For Each dgItem In grdQuality.Items
                    chkSelected = dgItem.FindControl("cbSelected")

                    iIndex = dgItem.ItemIndex
                    grdQuality.SelectedIndex = iIndex

                    iID = Convert.ToInt32(grdQuality.DataKeys(grdQuality.SelectedIndex))
                    sFilter = "ID = " & CStr(iID)
                    drData = dtData.Select(sFilter)

                    If Not chkSelected Is Nothing And Not drData Is Nothing Then
                        If chkSelected.Checked = True Then
                            'By default if the selected flag in the datatable is false this will be 
                            'the row item just selected
                            If drData(0)("Selected") = False Then
                                iIDBackup = iID
                                bSelected = True
                            End If
                            drData(0)("Selected") = True
                        End If
                    End If
                Next

                For Each dgItem In grdQuality.Items
                    chkSelected = dgItem.FindControl("cbSelected")

                    iIndex = dgItem.ItemIndex
                    grdQuality.SelectedIndex = iIndex

                    iID = Convert.ToInt32(grdQuality.DataKeys(grdQuality.SelectedIndex))
                    sFilter = "ID = " & CStr(iID)
                    drData = dtData.Select(sFilter)

                    If Not chkSelected Is Nothing And Not drData Is Nothing Then
                        If chkSelected.Checked = False Then
                            'By default if the selected flag in the datatable is true this will be 
                            'the row item just de-selected
                            If drData(0)("Selected") = True Then
                                iIDBackup = iID
                                bSelected = False
                            End If
                            drData(0)("Selected") = False

                            'If something has been de-selected untick the select all
                            chkSelectAll.Checked = False
                        End If
                    End If
                Next

                iCount = 0
                'Count the number of items actually selected
                For iRowCount = dtData.Rows.Count - 1 To 0 Step -1
                    If Not IsDBNull(dtData.Rows(iRowCount)("Selected")) Then
                        If dtData.Rows(iRowCount)("Selected") = True Then
                            iCount = iCount + 1
                        End If
                    End If
                Next

                If iCount = 1 Then
                    For Each drRow In dtData.Rows
                        If Not IsDBNull(drRow("Selected")) Then
                            If drRow("Selected") = True Then
                                iIDBackup = drRow("ID")
                            End If
                        End If
                    Next

                    For Each dgItem In grdQuality.Items
                        chkSelected = dgItem.FindControl("cbSelected")

                        If Not chkSelected Is Nothing Then
                            If chkSelected.Checked = True Then
                                grdQuality.SelectedIndex = dgItem.ItemIndex
                                Exit For
                            End If
                        End If
                    Next
                End If

                sFilter = "ID = " & CStr(iIDBackup)
                drData = dtData.Select(sFilter)

                'Depending on the number of items selected...
                If Not drData Is Nothing Then
                    If iCount = 2 Then
                        If bSelected Then
                            InitialiseControls()
                            btnEdit.Enabled = True
                        End If
                    ElseIf iCount = 1 Then
                        With drData(0)
                            sTestResult = .Item("Result").ToString()
                            If sTestResult = "0" Or sTestResult = "" Then
                                ddlTestResult.SelectedIndex = 0
                                chkDispatched.Enabled = False
                            Else
                                SelectItemInDropDownList(ddlTestResult, sTestResult)
                                chkDispatched.Enabled = True
                            End If
                            chkQCNote.Checked = GetRowColumnData(.Item("QCNote"))
                            SelectItemInDropDownList(ddlQCCode, .Item("QCCode").ToString())

                            'Only display the QCNote if it has a correct index
                            If Not IsDBNull(.Item("QCNoteRef")) Then
                                If CInt(.Item("QCNoteRef")) > 0 Then
                                    txtQCNoteRef.Text = .Item("QCNoteRef").ToString()
                                Else
                                    txtQCNoteRef.Text = ""
                                End If
                            Else
                                txtQCNoteRef.Text = ""
                            End If

                            If Not CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean) = True Then
                                If sTestResult = "1" Then 'pass
                                    chkQCNote.Enabled = True
                                    If ddlQCCode.SelectedIndex = -1 Then
                                        ddlQCCode.Enabled = False
                                    End If
                                    rfvQCCode.Enabled = False
                                ElseIf sTestResult = "2" Then
                                    chkQCNote.Enabled = False
                                    chkQCNote.Checked = False
                                    ddlQCCode.Enabled = True
                                    rfvQCCode.Enabled = True
                                    txtQCNoteRef.Text = ""
                                Else
                                    If Not chkQCNote.Enabled = True Then
                                        chkQCNote.Enabled = False
                                    End If
                                    If ddlQCCode.SelectedIndex = -1 Then
                                        ddlQCCode.Enabled = False
                                    End If
                                    rfvQCCode.Enabled = False
                                End If
                            End If

                            txtStainRef.Text = .Item("StainRef").ToString()
                            ctlDispatchDate.DateField = .Item("DispatchedDate").ToString()
                            chkDispatched.Checked = GetRowColumnData(.Item("Dispatched"))
                            SelectItemInDropDownList(ddlDispatchedBy, .Item("DispatchedBy").ToString())
                            txtComment.Text = .Item("Comment").ToString()
                            txtDispatchedTo.Text = .Item("DispatchedTo").ToString()
                            SelectItemInDropDownList(ddlRemedialAction, .Item("RemedialAction").ToString())
                            SelectItemInDropDownList(ddlArchiveLocation, .Item("ArchiveLocation").ToString())
                            ctlArchiveDate.DateField = .Item("ArchivedDate").ToString()
                            txtArchiveComment.Text = .Item("ArchiveComment").ToString()
                            txtNumberOfSlides.Text = .Item("NumberOfSlides").ToString()

                            sTestType = .Item("TestType").ToString

                            chkblTCCodes.SelectedIndex = -1
                            If sTestType = "Histology" Then
                                SetTCCodesForTest(dtHistologyTCCodes, .Item("TestID"))
                            ElseIf sTestType = "Antibodies" Then
                                SetTCCodesForTest(dtAntibodiesTCCodes, .Item("TestID"))
                            ElseIf sTestType = "Stain" Then
                                SetTCCodesForTest(dtStainTCCodes, .Item("TestID"))
                            End If
                        End With
                        btnEdit.Enabled = True
                    ElseIf iCount = 0 Then
                        btnEdit.Enabled = False
                        InitialiseControls()
                    End If
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to enable to update selected button.", ex)
        End Try
    End Sub

#End Region

#Region "Private Functions"

    Private Sub SetEnterKeyPress()
        SetFocus(txtPage)
        SetDropDownControlOnEnter(ddlHistologyRefList, ddlTestList.ClientID)
        SetDropDownControlOnEnter(ddlTestList, btnFilter.ClientID)
        SetDropDownControlOnEnter(ddlTestResult, txtStainRef.ClientID)
        SetTextboxDefaultButton(txtStainRef, btnEdit)
        SetTextboxDefaultButton(txtDispatchedTo, btnEdit)
        ctlDispatchDate.SetDropDownOnEnter(ddlDispatchedBy.ClientID)
        SetDropDownControlOnEnter(ddlDispatchedBy, txtNumberOfSlides.ClientID)
        SetTextboxDefaultButton(txtNumberOfSlides, btnEdit)
        SetDropDownControlOnEnter(ddlQCCode, ddlQCCode.ClientID)
        SetTextboxDefaultButton(txtQCNoteRef, btnEdit)
        SetDropDownControlOnEnter(ddlRemedialAction, ddlArchiveLocation.ClientID)
        SetDropDownControlOnEnter(ddlArchiveLocation, ctlArchiveDate.FirstClientID)
        ctlArchiveDate.SetControlOnEnter(txtArchiveComment.ClientID)
        SetTextboxDefaultButton(txtArchiveComment, btnEdit)
        SetTextboxDefaultButton(txtPage, btnGoToPage)
    End Sub

    Private Function ValidateMandatoryPageFields() As Boolean
        Try
            rfvPageNumber.Validate()
            revPageNumber.Validate()

            If Not rfvPageNumber.IsValid Or _
            Not revPageNumber.IsValid() Then
                Return False
            End If

            Return True
        Catch ex As Exception
            clsAppError.DisplayError("Failed to validate page number fields.", ex)
        End Try

    End Function

    Private Sub CreateQCNoteTable()
        Try
            Dim objQCNote As New HistopathologyLib.clsQCNote
            Dim dtQCNoteIDs As New DataTable

            If Not objQCNote.CreateQCNoteTable(dtQCNoteIDs) Then
                Throw New Exception("QCNote.CreateQCNoteTable returned false.")
            End If

            Session.Item(SessionVars.SV_QCNoteNumbers) = dtQCNoteIDs

        Catch ex As Exception
            clsAppError.DisplayError("Failed to create QC Note table.", ex)
        End Try
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

    Private Sub UpdateSubmissionStatus()
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtBatch As DataTable

            If Not dsBatchDetails Is Nothing Then
                dtBatch = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)
                If Not dtBatch Is Nothing And dtBatch.Rows.Count > 0 Then
                    dtBatch.Rows(0)("BatchStatus") = HistopathologyLib.clsBatch.STATUS_INPROGRESS
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Unable to update submission status.", ex)
        End Try
    End Sub

    Private Sub UpdateSessionWithQualityData()
        Try
            Dim dtQualityData As DataTable = CType(Session.Item(SessionVars.SV_BlockSummaryTable), DataTable)
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtBatch As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)
            Dim dtHistology As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_HISTOLOGY)
            Dim dtAntibodies As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANTIBODIES)
            Dim dtStain As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_STAIN)
            Dim drQualityRow As DataRow
            Dim foundRow As DataRow()
            Dim sTestType As String
            Dim sFilter As String
            Dim bOverallDispatch As Boolean = True
            Dim dtDispatchedDate As DateTime

            'Copy the data from the datatable bound grid to the Session dataset
            If Not dtQualityData Is Nothing Then
                For Each drQualityRow In dtQualityData.Rows
                    sTestType = drQualityRow("TestType").ToString()
                    sFilter = "ID=" & drQualityRow("TestID").ToString()

                    If sTestType = "Histology" Or sTestType = "Archive" Then

                        foundRow = dtHistology.Select(sFilter)
                        UpdateSessionRow(foundRow(0), drQualityRow)

                        'If all tests on the batch have been dispatched the batch status can be set to 
                        'completed.
                        If IsDBNull(foundRow(0)("Dispatched")) Then
                            bOverallDispatch = False
                        ElseIf foundRow(0)("Dispatched") = False Then
                            bOverallDispatch = False
                        Else
                            If foundRow(0)("DispatchedDate") > dtDispatchedDate Then
                                dtDispatchedDate = foundRow(0)("DispatchedDate")
                            End If
                        End If
                    ElseIf sTestType = "Antibodies" Then
                        foundRow = dtAntibodies.Select(sFilter)

                        UpdateSessionRow(foundRow(0), drQualityRow)

                        'If all tests on the batch have been dispatched the batch status can be set to 
                        'completed.
                        If IsDBNull(foundRow(0)("Dispatched")) Then
                            bOverallDispatch = False
                        ElseIf foundRow(0)("Dispatched") = False Then
                            bOverallDispatch = False
                        Else
                            If foundRow(0)("DispatchedDate") > dtDispatchedDate Then
                                dtDispatchedDate = foundRow(0)("DispatchedDate")
                            End If
                        End If
                    ElseIf sTestType = "Stain" Then
                        foundRow = dtStain.Select(sFilter)

                        UpdateSessionRow(foundRow(0), drQualityRow)

                        'If all tests on the batch have been dispatched the batch status can be set to 
                        'completed.
                        If IsDBNull(foundRow(0)("Dispatched")) Then
                            bOverallDispatch = False
                        ElseIf foundRow(0)("Dispatched") = False Then
                            bOverallDispatch = False
                        Else
                            If foundRow(0)("DispatchedDate") > dtDispatchedDate Then
                                dtDispatchedDate = foundRow(0)("DispatchedDate")
                            End If
                        End If
                    Else
                        Throw New Exception("Unknown Test type found.")
                    End If
                Next
            End If

            'If all tests have been dispatched set the overall status of the batch to dispatched
            If bOverallDispatch = True Then
                If Not dtBatch Is Nothing And dtBatch.Rows.Count > 0 Then
                    dtBatch.Rows(0)("BatchStatus") = HistopathologyLib.clsBatch.STATUS_COMPLETED
                    dtBatch.Rows(0)("DateCompleted") = dtDispatchedDate.ToShortDateString
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to Save the Quality Data information.", ex)
        End Try
    End Sub

    Private Sub EnableControls(ByVal bEnabled)
        ddlTestResult.Enabled = bEnabled
        txtStainRef.Enabled = bEnabled
        chkDispatched.Enabled = bEnabled
        txtDispatchedTo.Enabled = bEnabled
        ctlDispatchDate.Enabled = bEnabled
        ddlDispatchedBy.Enabled = bEnabled
        txtNumberOfSlides.Enabled = bEnabled
        ddlQCCode.Enabled = bEnabled
        chkQCNote.Enabled = bEnabled
        txtComment.Enabled = bEnabled
        ddlArchiveLocation.Enabled = bEnabled
        ctlArchiveDate.Enabled = bEnabled
        txtArchiveComment.Enabled = bEnabled
        chkblTCCodes.Enabled = bEnabled
        ddlRemedialAction.Enabled = bEnabled

        If CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean) = True Then
            chkSelectAll.Enabled = False
            btnCancel.Enabled = True
            btnEdit.Enabled = False
        End If
    End Sub

    Private Sub InitialiseControls()
        EnableControls(True)
        grdQuality.SelectedIndex = -1
        ctlDispatchDate.DateField = ""
        SelectItemInDropDownList(ddlDispatchedBy, "")
        txtDispatchedTo.Text = ""
        chkblTCCodes.SelectedIndex = -1
        chkDispatched.Checked = False
        chkDispatched.Enabled = False
        txtStainRef.Text = ""
        SelectItemInDropDownList(ddlQCCode, "")
        SelectItemInDropDownList(ddlTestResult, "")
        SelectItemInDropDownList(ddlRemedialAction, "")
        chkQCNote.Checked = False
        chkQCNote.Enabled = False
        txtQCNoteRef.Text = ""
        txtQCNoteRef.Enabled = False
        txtComment.Text = ""
        ctlDispatchDate.Mandatory = False
        rfvDispatchedTo.Enabled = False
        rfvDispatchedBy.Enabled = False
        rfvQCCode.Enabled = False
        rfvRemedialAction.Enabled = False
        SelectItemInDropDownList(ddlArchiveLocation, "")
        ctlArchiveDate.DateField = ""
        txtArchiveComment.Text = ""
        txtNumberOfSlides.Text = "1"
        lblError.Visible = False
        lblArchiveLocationError.Visible = False
        ctlDIV.InnerHtml = ""
        ctlArchiveDate.Mandatory = False
    End Sub

    Private Sub SetupRadioButtonList()
        Try
            Dim liAll As New ListItem
            Dim liFilter As New ListItem
            liAll.Value = "All"
            liAll.Text = "All"
            liAll.Selected = True
            rblFilter.Items.Add(liAll)
            liFilter.Value = "Filter"
            liFilter.Text = "Filter"
            rblFilter.Items.Add(liFilter)

        Catch ex As Exception
            clsAppError.DisplayError("Failed to setup the radio butt list.", ex)
        End Try
    End Sub

    Private Sub UpdateCheckBoxData(ByRef dtData As DataTable, ByVal iID As Integer)
        Dim objChkbl As New HistopathologyLib.clsCheckBoxData
        Dim li As ListItem
        Dim sFilter As String
        Dim drFoundRow As DataRow()

        For Each li In chkblTCCodes.Items
            sFilter = "Code=" & "'" & li.Value & "'" & " AND TestID=" & iID
            drFoundRow = dtData.Select(sFilter)
            If li.Selected = True Then
                'if its a new item
                If Not drFoundRow Is Nothing And drFoundRow.Length = 0 Then
                    If Not objChkbl.NewItem(dtData, li.Value, iID, "TestID") Then
                        Throw New Exception("CheckBoxList.NewItem returned false.")
                    End If
                End If
            Else
                'If its been unchecked
                If Not drFoundRow Is Nothing And drFoundRow.Length = 1 Then
                    drFoundRow(0).Delete()
                End If
            End If
        Next
    End Sub

    Private Sub UpdateSessionRow(ByRef drTo As DataRow, ByRef drFrom As DataRow)
        drTo("Result") = drFrom("Result")
        drTo("QCCode") = drFrom("QCCode")
        drTo("QCNote") = drFrom("QCNote")
        drTo("QCNoteRef") = drFrom("QCNoteRef")
        drTo("StainRef") = drFrom("StainRef")
        drTo("Dispatched") = drFrom("Dispatched")
        drTo("DispatchedDate") = drFrom("DispatchedDate")
        drTo("DispatchedBy") = drFrom("DispatchedBy")
        drTo("DispatchedTo") = drFrom("DispatchedTo")
        drTo("EnteredBy") = drFrom("EnteredBy")
        drTo("PremiumCharge") = drFrom("PremiumCharge")
        drTo("Comment") = drFrom("Comment")
        drTo("RemedialAction") = drFrom("RemedialAction")
        drTo("ArchiveLocation") = drFrom("ArchiveLocation")
        drTo("ArchivedDate") = drFrom("ArchivedDate")
        drTo("ArchiveComment") = drFrom("ArchiveComment")
        drTo("NumberOfSlides") = drFrom("NumberOfSlides")
    End Sub

#End Region

#Region "Validation"

    Private Function SetClientValidation() As Boolean
        If HttpContext.Current.Request.Browser.JavaScript Then
            Dim scr As New System.Text.StringBuilder

            scr.Append("<SCRIPT language=""Javascript"">" + vbNewLine)
            scr.Append("function ClientValidatePageNumber(sender, args)" + vbNewLine)
            scr.Append("{" + vbNewLine)
            scr.Append("    var sBlockRef = args.Value;" + vbNewLine)
            scr.Append("    if (sBlockRef.length == 1)" + vbNewLine)
            scr.Append("    {" + vbNewLine)
            scr.Append("        var expBlockRef = /[0-9]/;" + vbNewLine)
            scr.Append("        if (expBlockRef.test(sBlockRef))" + vbNewLine)
            scr.Append("        {" + vbNewLine)
            scr.Append("            args.IsValid = true;" + vbNewLine)
            scr.Append("            return;" + vbNewLine)
            scr.Append("        }" + vbNewLine)
            scr.Append("        else" + vbNewLine)
            scr.Append("        {" + vbNewLine)
            scr.Append("            args.IsValid = false;" + vbNewLine)
            scr.Append("            return;" + vbNewLine)
            scr.Append("        }" + vbNewLine)
            scr.Append("    }" + vbNewLine)
            scr.Append("    else if (sBlockRef.length == 2)" + vbNewLine)
            scr.Append("    {" + vbNewLine)
            scr.Append("        var expBlockRef = /[0-9][0-9]/;" + vbNewLine)
            scr.Append("        if (expBlockRef.test(sBlockRef))" + vbNewLine)
            scr.Append("        {" + vbNewLine)
            scr.Append("            args.IsValid = true;" + vbNewLine)
            scr.Append("            return;" + vbNewLine)
            scr.Append("        }" + vbNewLine)
            scr.Append("        else" + vbNewLine)
            scr.Append("        {" + vbNewLine)
            scr.Append("            args.IsValid = false;" + vbNewLine)
            scr.Append("            return;" + vbNewLine)
            scr.Append("        }" + vbNewLine)
            scr.Append("    }" + vbNewLine)
            scr.Append("    else if (sBlockRef.length == 3)" + vbNewLine)
            scr.Append("    {" + vbNewLine)
            scr.Append("        var expBlockRef = /[0-9][0-9][0-9]/;" + vbNewLine)
            scr.Append("        if (expBlockRef.test(sBlockRef))" + vbNewLine)
            scr.Append("        {" + vbNewLine)
            scr.Append("            args.IsValid = true;" + vbNewLine)
            scr.Append("            return;" + vbNewLine)
            scr.Append("        }" + vbNewLine)
            scr.Append("        else" + vbNewLine)
            scr.Append("        {" + vbNewLine)
            scr.Append("            args.IsValid = false;" + vbNewLine)
            scr.Append("            return;" + vbNewLine)
            scr.Append("        }" + vbNewLine)
            scr.Append("    }" + vbNewLine)
            scr.Append("    else if (sBlockRef.length == 4)" + vbNewLine)
            scr.Append("    {" + vbNewLine)
            scr.Append("        var expBlockRef = /[0-9][0-9][0-9][0-9]/;" + vbNewLine)
            scr.Append("        if (expBlockRef.test(sBlockRef))" + vbNewLine)
            scr.Append("        {" + vbNewLine)
            scr.Append("            args.IsValid = true;" + vbNewLine)
            scr.Append("            return;" + vbNewLine)
            scr.Append("        }" + vbNewLine)
            scr.Append("        else" + vbNewLine)
            scr.Append("        {" + vbNewLine)
            scr.Append("            args.IsValid = false;" + vbNewLine)
            scr.Append("            return;" + vbNewLine)
            scr.Append("        }" + vbNewLine)
            scr.Append("    }" + vbNewLine)
            scr.Append("    else" + vbNewLine)
            scr.Append("    {" + vbNewLine)
            scr.Append("        var expBlockRef = /[0-9][0-9][0-9][0-9][0-9]/;" + vbNewLine)
            scr.Append("        if (expBlockRef.test(sBlockRef))" + vbNewLine)
            scr.Append("        {" + vbNewLine)
            scr.Append("            args.IsValid = true;" + vbNewLine)
            scr.Append("            return;" + vbNewLine)
            scr.Append("        }" + vbNewLine)
            scr.Append("        else" + vbNewLine)
            scr.Append("        {" + vbNewLine)
            scr.Append("            args.IsValid = false;" + vbNewLine)
            scr.Append("            return;" + vbNewLine)
            scr.Append("        }" + vbNewLine)
            scr.Append("    }" + vbNewLine)
            scr.Append("}" + vbNewLine)
            scr.Append("</SCRIPT>" + vbNewLine)

            Me.Page.RegisterClientScriptBlock("SetPageNumberClientValidation", scr.ToString())
            Return True
        Else
            Return False
        End If
    End Function

    Public Sub ValidatePageNumber(ByVal sender As Object, ByVal args As ServerValidateEventArgs)
        Dim sPageNumber As String = CStr(args.Value)
        Dim match As Match

        If sPageNumber.Length = 1 Then
            Dim revBlockRef As Regex = New Regex("[0-9]")
            match = revBlockRef.Match(sPageNumber)

            If match.Success Then
                args.IsValid = True
                Exit Sub
            Else
                args.IsValid = False
                Exit Sub
            End If
        ElseIf sPageNumber.Length = 2 Then
            Dim revBlockRef As Regex = New Regex("[0-9][0-9]")
            match = revBlockRef.Match(sPageNumber)

            If match.Success Then
                args.IsValid = True
                Exit Sub
            Else
                args.IsValid = False
                Exit Sub
            End If
        ElseIf sPageNumber.Length = 3 Then
            Dim revBlockRef As Regex = New Regex("[0-9][0-9][0-9]")
            match = revBlockRef.Match(sPageNumber)

            If match.Success Then
                args.IsValid = True
                Exit Sub
            Else
                args.IsValid = False
                Exit Sub
            End If
        ElseIf sPageNumber.Length = 4 Then
            Dim revBlockRef As Regex = New Regex("[0-9][0-9][0-9][0-9]")
            match = revBlockRef.Match(sPageNumber)

            If match.Success Then
                args.IsValid = True
                Exit Sub
            Else
                args.IsValid = False
                Exit Sub
            End If
        Else
            Dim revBlockRef As Regex = New Regex("[0-9][0-9][0-9][0-9][0-9]")
            match = revBlockRef.Match(sPageNumber)

            If match.Success Then
                args.IsValid = True
                Exit Sub
            Else
                args.IsValid = False
                Exit Sub
            End If

        End If
    End Sub

#End Region



End Class

