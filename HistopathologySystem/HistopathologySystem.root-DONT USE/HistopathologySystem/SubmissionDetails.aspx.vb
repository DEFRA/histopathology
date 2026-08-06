Partial Class SubmissionDetails
    Inherits System.Web.UI.Page
    Protected WithEvents TissuesPager As DataGridPager
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents ctlPMDate As CalendarDate

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
        VLAHeader1.PageTitle = "Sample Details"
        TissuesPager.SetGrid(grdTissues)
        SetCalendarDateHandler(Me.Page)

        If Not IsPostBack Then
            CheckForExistingBatchSubmission()
            LoadLookupLists()
            InitialiseScreenWithDetails()
            EnableDisableControls()
            InitialiseTissuesGrid()
            If CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean) = False Then
                PromptBeforeSaveScript("Are you sure you want to Cancel? Any tissues you have added since last clicking the Next button will be lost.", btnCancel)
                PromptBeforeSaveScript("Are you sure you want to go Back? Any tissues you have added since last clicking the Next button will be lost.", btnBack)
            End If
            ctlPMDate.SetControlOnEnter(btnSave.ClientID)
        End If
    End Sub

#Region "Grid Handling"

    Private Sub InitialiseTissuesGrid()
        Try
            Dim dsDataSet As DataSet = Session(SessionVars.SV_BatchDetails)
            Dim dtTissueData As DataTable = dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_TISSUES_TABLE)
            Dim iBatchSubmissionID As Integer = CType(Session.Item(SessionVars.SV_BatchSubmissionID), Integer)
            Dim sRowFilter As String
            Dim dvData As DataView
            Dim dtOldData As New DataTable()

            Session(SessionVars.SV_TissuesTable) = dtTissueData

            sRowFilter = "BatchSubmissionID=" & Convert.ToString(iBatchSubmissionID)
            dtTissueData.DefaultView.RowFilter = sRowFilter

            dvData = dtTissueData.DefaultView
            Session(SessionVars.SV_TissuesView) = dvData

            ' initialise the grid
            grdTissues.DataSource = dtTissueData
            grdTissues.DataKeyField = "ID"
            grdTissues.CurrentPageIndex = 0
            grdTissues.SelectedIndex = -1
            grdTissues.EditItemIndex = -1
            grdTissues.DataBind()
            grdTissues.Enabled = True

            ' initialise the pager
            TissuesPager.DataTableSessionID = SessionVars.SV_TissuesTable
            TissuesPager.DataViewSessionID = SessionVars.SV_TissuesView
            TissuesPager.PageLinkCount = 10
            TissuesPager.AllowAddNew = True
            TissuesPager.AllowEdit = True
            TissuesPager.AllowDelete = True
            TissuesPager.Rebind()
            TissuesPager.Refresh()

        Catch ex As Exception
            clsAppError.DisplayError("Error initialising the Tissues grid on the Sample Details page", ex)
        End Try
    End Sub

#End Region

#Region "Event Handlers"

    Private Sub grdTissues_ItemDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.DataGridItemEventArgs) Handles grdTissues.ItemDataBound
        ' populate template column values here
        Try
            Dim drv As DataRowView = CType(e.Item.DataItem, DataRowView)
            If Not drv Is Nothing Then
                Dim lblTissueCode As Label = Nothing
                Dim ddlTissueCode As DropDownList = Nothing
                Dim lblNoPieces As Label = Nothing
                Dim txtNoPieces As TextBox = Nothing
                Dim lblComments As Label = Nothing
                Dim txtComments As TextBox = Nothing

                If e.Item.ItemType = ListItemType.EditItem Then
                    ' populate edit mode controls
                    ddlTissueCode = CType(e.Item.FindControl("ddlTissueCodeEdit"), DropDownList)
                    txtNoPieces = CType(e.Item.FindControl("txtNoPiecesEdit"), TextBox)
                    txtComments = CType(e.Item.FindControl("txtCommentsEdit"), TextBox)
                ElseIf e.Item.ItemType = ListItemType.Item _
                OrElse e.Item.ItemType = ListItemType.AlternatingItem _
                OrElse e.Item.ItemType = ListItemType.SelectedItem Then
                    ' populate display mode controls
                    lblTissueCode = CType(e.Item.FindControl("lblTissueCodeDisplay"), Label)
                    lblNoPieces = CType(e.Item.FindControl("lblNoPiecesDisplay"), Label)
                    lblComments = CType(e.Item.FindControl("lblCommentsDisplay"), Label)
                End If

                If Not lblNoPieces Is Nothing Then
                    If Not IsDBNull(drv("NoPieces")) Then
                        lblNoPieces.Text = drv("NoPieces")
                    Else
                        lblNoPieces.Text = ""
                    End If
                End If
                If Not txtNoPieces Is Nothing Then
                    If Not IsDBNull(drv("NoPieces")) Then
                        txtNoPieces.Text = drv("NoPieces")
                    Else
                        txtNoPieces.Text = "1"
                    End If
                End If

                If Not lblTissueCode Is Nothing Then
                    If Not IsDBNull(drv("TissueCode")) Then
                        lblTissueCode.Text = GetListType(drv("TissueCode"), LOOKUP_TISSUE_CODE)
                    Else
                        lblTissueCode.Text = ""
                    End If
                End If
                If Not ddlTissueCode Is Nothing Then
                    LoadLookupTypeList(ddlTissueCode, LOOKUP_TISSUE_CODE)
                    If IsDBNull(drv("TissueCode")) Then
                        SelectItemInDropDownList(ddlTissueCode, "-1")
                    Else
                        SelectItemInDropDownList(ddlTissueCode, drv("TissueCode"))
                    End If
                End If

                If Not lblComments Is Nothing Then
                    If Not IsDBNull(drv("Comment")) Then
                        lblComments.Text = drv("Comment").ToString()
                    Else
                        lblComments.Text = ""
                    End If
                End If
                If Not txtComments Is Nothing Then
                    If Not IsDBNull(drv("Comment")) Then
                        txtComments.Text = drv("Comment").ToString()
                    Else
                        txtComments.Text = ""
                    End If
                End If
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to bind template columns in the Tissues grid on the Sample Details page.", ex)
        End Try
    End Sub

    Private Sub TissuesPager_Save(ByVal sender As Object, ByVal e As HistopathologySystem.DataGridPagerEventArgs) Handles TissuesPager.RowSave
        'save template column values to the dataset here

        'if the row is new, add a reference to the Block it belongs to
        If e.DataTableRow.RowState = DataRowState.Added Then
            e.DataTableRow("BatchSubmissionID") = Session(SessionVars.SV_BatchSubmissionID)
        End If

        Dim lst As DropDownList = CType(e.GridRow.FindControl("ddlTissueCodeEdit"), DropDownList)
        e.DataTableRow("TissueCode") = lst.SelectedItem.Value

        Dim txtNoPieces As TextBox = CType(e.GridRow.FindControl("txtNoPiecesEdit"), TextBox)
        e.DataTableRow("NoPieces") = txtNoPieces.Text

        Dim txtComments As TextBox = CType(e.GridRow.FindControl("txtCommentsEdit"), TextBox)
        e.DataTableRow("Comment") = txtComments.Text

    End Sub

    Private Sub TissuesPager_EditModeStart(ByVal sender As Object, ByVal e As DataGridPagerEventArgs) Handles TissuesPager.EditModeStart
        btnSave.Enabled = False
        btnCancel.Enabled = False
        btnBack.Enabled = False
    End Sub

    Private Sub TissuesPager_EditModeStop(ByVal sender As Object, ByVal e As System.EventArgs) Handles TissuesPager.EditModeStop
        btnSave.Enabled = True
        btnCancel.Enabled = True

        If CType(Session.Item(SessionVars.SV_Editing), Boolean) = True Then
            btnBack.Enabled = False
        Else
            btnBack.Enabled = True
        End If

    End Sub

    Private Sub btnSave_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSave.Click
        Dim dNow As Date
        Dim sPrevPage As String = CStr(Session.Item(SessionVars.SV_AddSamplePrevPage))
        If ValidateData() Then
            If ctlPMDate.Validate(dNow) Then
                UpdateSessionWithSubmission()

                Try
                    If sPrevPage = "BatchSummary.aspx" Then
                        'Bread crumbs
                        Dim objCrumbArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
                        If Not objCrumbArrayList Is Nothing Then
                            objCrumbArrayList(1) = "Submission Samples"
                            objCrumbArrayList(2) = "Sample Summary"
                            objCrumbArrayList.RemoveAt(3)
                            Session.Item(SessionVars.SV_BreadCrumbs) = objCrumbArrayList
                        End If
                    End If
                Catch ex As Exception
                    clsAppError.DisplayError("Bread Crumb Error, SubmissionDetails.aspx.", ex)
                End Try

                Response.Redirect(sPrevPage)
            Else
                ctlDiv.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
            End If
        End If
    End Sub

    Private Sub btnLookup_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Response.Redirect("SearchSender.aspx")
    End Sub

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtBatch As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)
            Dim dtBatchSubmission As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_SUBMISSION_TABLE)
            Dim dtTissues As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TISSUES_TABLE)
            Dim dtAnimal As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE)
            Dim iBatchSubmission As Integer = CType(Session.Item(SessionVars.SV_BatchSubmissionID), Integer)
            Dim iAnimalID As Integer = CType(Session.Item(SessionVars.SV_AnimalID), Integer)
            Dim sFilter As String
            Dim drFoundRows As DataRow()
            Dim drRow As DataRow
            Dim drFoundRow As DataRow
            Dim bFound As Boolean = False
            'This gets set from the 
            Dim dtOldData As DataTable = CType(Session.Item(SessionVars.SV_TissuesBeforeChanges), DataTable)

            sFilter = "BatchSubmissionID=" & Convert.ToString(iBatchSubmission)
            drFoundRows = dtTissues.Select(sFilter)

            'If we are editing the Submission we need to reverse all changes made, and delete any tissues added
            'If we are not editing the submission just remove all tissues that have the current batchSubmissionID link
            If Not drFoundRows Is Nothing Then
                For Each drFoundRow In drFoundRows
                    dtTissues.Rows.Remove(drFoundRow)
                Next
            End If

            If Not dtOldData Is Nothing Then
                For Each drRow In dtOldData.Rows
                    dtTissues.ImportRow(drRow)
                Next
            End If

            If CType(Session.Item(SessionVars.SV_Editing), Boolean) = False Then
                Dim objBatchSub As New HistopathologyLib.clsBatchSubmission()

                If Not objBatchSub.DeleteRecord(dtTissues, dtBatchSubmission, iBatchSubmission) Then
                    Throw New Exception("BatchSubmission.DeleteSubmission returned false.")
                End If

                'Also remove the Animal record. Check its not associated with another BatchSubmission record before removing.
                sFilter = "AnimalID=" & Convert.ToString(iAnimalID)
                drFoundRows = dtBatchSubmission.Select(sFilter)

                If Not drFoundRows Is Nothing And drFoundRows.Length = 0 Then
                    'The animal isnt associated with another record so delete it
                    sFilter = "ID=" & Convert.ToString(iAnimalID)
                    drFoundRows = dtAnimal.Select(sFilter)
                    'Should only be the one
                    If Not drFoundRows Is Nothing And drFoundRows.Length = 1 Then
                        dtAnimal.Rows.Remove(drFoundRows(0))

                        '---------
                        'neuropath stuff
                        'if there is only one animal remove the defaulted species and project
                        If CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Neuropath" Then
                            If dtBatchSubmission.Rows.Count = 0 Then
                                If Not dtBatch Is Nothing And dtBatch.Rows.Count > 0 Then
                                    dtBatch.Rows(0)("Species") = ""
                                    dtBatch.Rows(0)("ProjectContractCode") = DBNull.Value
                                    Session.Item(SessionVars.SV_ProjectCode) = ""
                                End If
                            End If
                        End If
                        '--------- 
                    End If
                End If
            End If

            Session.Item(SessionVars.SV_OldPGNumber) = ""
            Session.Item(SessionVars.SV_PMDate) = ""
            Session.Item(SessionVars.SV_Species) = ""
            Session.Remove(SessionVars.SV_TissuesBeforeChanges)
        Catch ex As Exception
            clsAppError.DisplayError("Failed to Cancel the Submission.", ex)
        End Try

        Dim sPrevPage As String = CStr(Session.Item(SessionVars.SV_AddSamplePrevPage))
        Try
            If sPrevPage = "BatchSummary.aspx" Then
                'Bread crumbs
                Dim objCrumbArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
                If Not objCrumbArrayList Is Nothing Then
                    objCrumbArrayList(1) = "Submission Samples"
                    objCrumbArrayList(2) = "Sample Summary"
                    objCrumbArrayList.RemoveAt(3)
                    Session.Item(SessionVars.SV_BreadCrumbs) = objCrumbArrayList
                End If
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Bread Crumb Error, AddSubmission.aspx.", ex)
        End Try

        Response.Redirect(sPrevPage)
    End Sub

    Private Sub VLAHeader1_HomeClick(ByVal sender As Object, ByVal e As HistopathologySystem.HomeLinkEventArgs) Handles VLAHeader1.HomeClick
        Dim sMessage As System.Text.StringBuilder = New System.Text.StringBuilder()

        If CType(Session.Item(SessionVars.SV_EditingBatch), Boolean) Then
            sMessage.Append("You are currently editing a submission. Any changes that you have made since you last saved the submission will be lost. Are you sure you wish to exit?")
        ElseIf CType(Session.Item(SessionVars.SV_AssignBlocks), Boolean) Then
            sMessage.Append("You are currently assigning tissues to blocks. Any block assignment that you have completed since you last saved will be lost. Are you sure you wish to exit?")
        ElseIf CType(Session.Item(SessionVars.SV_CreatingNewBatch), Boolean) Then
            sMessage.Append("You are currently creating a new submission. If you exit now all the data you have entered will be lost. Are you sure you wish to exit?")
        Else
            sMessage.Append("Any changes that have been made will be discarded, are you sure you wish to exit without saving?")
        End If

        Page.RegisterStartupScript("navigate", PromptBeforeNavigateScript(sMessage.ToString(), "Home.aspx"))
        e.bNavigateHome = False
    End Sub

    Private Sub btnBack_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnBack.Click
        'UpdateSessionWithSubmission()
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtBatch As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)
            Dim dtBatchSubmission As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_SUBMISSION_TABLE)
            Dim dtTissues As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TISSUES_TABLE)
            Dim dtAnimal As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE)
            Dim iBatchSubmission As Integer = CType(Session.Item(SessionVars.SV_BatchSubmissionID), Integer)
            Dim iAnimalID As Integer = CType(Session.Item(SessionVars.SV_AnimalID), Integer)
            Dim sFilter As String
            Dim drFoundRows As DataRow()
            Dim drRow As DataRow
            Dim drFoundRow As DataRow
            Dim bFound As Boolean = False
            'This gets set from the submission summary page
            Dim dtOldData As DataTable = CType(Session.Item(SessionVars.SV_TissuesBeforeChanges), DataTable)

            sFilter = "BatchSubmissionID=" & Convert.ToString(iBatchSubmission)
            drFoundRows = dtTissues.Select(sFilter)

            ''Remove any tissues that have been added
            If Not drFoundRows Is Nothing Then
                For Each drFoundRow In drFoundRows
                    dtTissues.Rows.Remove(drFoundRow)
                Next
            End If

            'Before going back to the add submission page check if the animal that is selected is currently assigned to a
            'BatchSubmission record. If not remove the animal from the dataset.
            sFilter = "AnimalID=" & Convert.ToString(iAnimalID)
            drFoundRows = dtBatchSubmission.Select(sFilter)

            If Not drFoundRows Is Nothing And drFoundRows.Length = 0 Then
                'The animal isnt associated with another record so delete it
                sFilter = "ID=" & Convert.ToString(iAnimalID)
                drFoundRows = dtAnimal.Select(sFilter)
                'Should only be the one
                If Not drFoundRows Is Nothing And drFoundRows.Length = 1 Then
                    dtAnimal.Rows.Remove(drFoundRows(0))
                End If
            End If

            If CType(Session.Item(SessionVars.SV_Editing), Boolean) = False Then
                Dim objBatchSub As New HistopathologyLib.clsBatchSubmission()

                '---------
                'neuropath stuff
                'if there is only one animal remove the defaulted species
                If CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Neuropath" Then
                    If dtBatchSubmission.Rows.Count = 1 Then
                        If Not dtBatch Is Nothing And dtBatch.Rows.Count > 0 Then
                            dtBatch.Rows(0)("Species") = ""
                            dtBatch.Rows(0)("ProjectContractCode") = DBNull.Value
                            Session.Item(SessionVars.SV_ProjectCode) = ""
                        End If
                    End If
                End If
                '--------- 

                If Not objBatchSub.DeleteRecord(dtTissues, dtBatchSubmission, iBatchSubmission) Then
                    Throw New Exception("BatchSubmission.DeleteSubmission returned false.")
                End If

                'Also remove the Animal record. Check its not associated with another BatchSubmission record before removing.
                sFilter = "AnimalID=" & Convert.ToString(iAnimalID)
                drFoundRows = dtBatchSubmission.Select(sFilter)

                If Not drFoundRows Is Nothing And drFoundRows.Length = 0 Then
                    'The animal isnt associated with another record so delete it
                    sFilter = "ID=" & Convert.ToString(iAnimalID)
                    drFoundRows = dtAnimal.Select(sFilter)
                    'Should only be the one
                    If Not drFoundRows Is Nothing And drFoundRows.Length = 1 Then
                        dtAnimal.Rows.Remove(drFoundRows(0))
                    End If
                End If
            End If

            Session.Item(SessionVars.SV_OldPGNumber) = ""
            Session.Item(SessionVars.SV_PMDate) = ""
            Session.Item(SessionVars.SV_Species) = ""
        Catch ex As Exception
            clsAppError.DisplayError("Failed to return to the Add Submission page.", ex)
        End Try

        Try
            Dim objArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
            If Not objArrayList Is Nothing Then
                objArrayList(1) = "Submission Samples"
                objArrayList(2) = "Tissuing"
                objArrayList(3) = "Add Sample"
                Session.Item(SessionVars.SV_BreadCrumbs) = objArrayList
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Bread Crumb Error, AddSubmission.aspx.", ex)
        End Try

        Response.Redirect("AddSubmission.aspx")
    End Sub

#End Region

#Region "Private Functions"

    Private Sub LoadLookupTypeList(ByRef ddl As DropDownList, ByVal lookuplist As Integer)

        Dim blnResult As Boolean
        Dim objDataTable As DataTable
        Dim objLookup As New HistopathologyLib.LookupData()

        Try
            objDataTable = objLookup.GetLookupData(lookuplist)
            If Not (objDataTable Is Nothing) Then
                ddl.DataSource = objDataTable
                ddl.DataValueField = "Code"
                ddl.DataTextField = "Description"
                ddl.DataBind()
                Common.AddItemToDropDownList(ddl)
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve Lookup lists on the Sample Details page.", ex)
        End Try
    End Sub

    Private Sub UpdateSessionWithSubmission()
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)

            If Not dsBatchDetails Is Nothing Then
                Dim dtBatchSubmission As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_SUBMISSION_TABLE)
                Dim iBatchSubmissionID As Integer = CType(Session.Item(SessionVars.SV_BatchSubmissionID), Integer)
                Dim dtAnimal As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE)

                Dim iAnimalID As Integer = CType(Session.Item(SessionVars.SV_AnimalID), Integer)
                Dim sFilter As String
                Dim foundRow As DataRow()

                'Just update the animalID for the current record in the BatchSubmission datatable
                sFilter = "ID=" & iBatchSubmissionID
                foundRow = dtBatchSubmission.Select(sFilter)
                If Not foundRow Is Nothing And foundRow.Length > 0 Then
                    foundRow(0)("AnimalID") = iAnimalID
                End If

                'Update the PMDate for the animal
                sFilter = "ID=" & iAnimalID
                foundRow = dtAnimal.Select(sFilter)
                If Not foundRow Is Nothing And foundRow.Length > 0 Then
                    foundRow(0)("PMDate") = FormatEmptyString(ctlPMDate.DateField)
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Error Updating the session with Sample Details.", ex)
        End Try
    End Sub

    Private Sub CreateNewRecord()
        Dim objBatchSubmission As New HistopathologyLib.clsBatchSubmission()
        Dim dtBatchSubmission As DataTable = _
                    CType(Session.Item(SessionVars.SV_BatchDetails).Tables(HistopathologyLib.clsBatch.BATCH_SUBMISSION_TABLE), DataTable)
        Dim iBatchSubmissionID As Integer
        Dim iBatchID As Integer = CType(Session.Item(SessionVars.SV_BatchID), Integer)

        If Not objBatchSubmission.NewRecord(dtBatchSubmission, iBatchSubmissionID, iBatchID) Then
            Throw New Exception("BatchSubmission.NewRecord return false")
        End If

        Session.Item(SessionVars.SV_BatchSubmissionID) = iBatchSubmissionID
    End Sub

    Private Sub EnableDisableControls()
        If CType(Session.Item(SessionVars.SV_HeaderUserArea), String) <> "Histopath" Then
            ddlHistologyType.Enabled = False
        End If

        If CType(Session.Item(SessionVars.SV_Editing), Boolean) = True Then
            btnBack.Enabled = False
        End If
    End Sub

    Private Sub InitialiseScreenWithDetails()
        Try
            Dim objAnimal As New HistopathologyLib.clsAnimal()
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim iAnimalID As Integer = CInt(Session.Item(SessionVars.SV_AnimalID))
            Dim sHistologyRef As String
            Dim sSenderRef As String
            Dim sNextBlockRef As String
            Dim aRowStamp As System.Array
            Dim bHistoRefSetInDatabase As Boolean
            Dim bPMDateSetInDatabase As Boolean
            Dim sPMDate As String
            Dim bHistoRefLinked As Boolean

            If Not objAnimal.GetAnimalData(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE), _
                                           iAnimalID, _
                                           sHistologyRef, _
                                           sSenderRef, _
                                           sNextBlockRef, _
                                           aRowStamp, _
                                           bHistoRefSetInDatabase, _
                                           sPMDate, _
                                           bPMDateSetInDatabase, _
                                           bHistoRefLinked) Then
                Throw New Exception("Animal.GetAnimalData returned false.")
            End If

            If bPMDateSetInDatabase Then
                ctlPMDate.Enabled = False
            End If
            ctlPMDate.DateField = sPMDate
            txtSenderRef.Text = sSenderRef
            ctlHistologyDiv.Visible = False
        Catch ex As Exception
            clsAppError.DisplayError("Failed to Initialise 'Sample Details' page.", ex)
        End Try
    End Sub

    Private Sub CheckForExistingBatchSubmission()
        Try
            'check if an existing batchsubmission already exists for the animal selected. If it does add the tissues against
            'the old batch submission record
            Dim iAnimalID As Integer = CInt(Session.Item(SessionVars.SV_AnimalID))
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtBatchSubmission As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_SUBMISSION_TABLE)
            Dim foundRows As DataRow()
            Dim sFilter As String

            If iAnimalID <> 0 Then
                sFilter = "AnimalID=" & iAnimalID
                foundRows = dtBatchSubmission.Select(sFilter)
                If Not foundRows Is Nothing And foundRows.Length > 0 Then
                    Session.Item(SessionVars.SV_BatchSubmissionID) = foundRows(0)("ID")
                End If
            End If

            'if there isnt an existing record create new
            If CType(Session.Item(SessionVars.SV_BatchSubmissionID), Integer) = 0 Then
                CreateNewRecord()
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to Initialise 'Sample Details' page.", ex)
        End Try
    End Sub

    Private Function ValidateData() As Boolean
        Try
            Dim dvData As DataView
            dvData = CType(Session.Item(SessionVars.SV_TissuesView), DataView)

            If Not dvData Is Nothing Then
                If dvData.Count = 0 Then
                    ctlDiv.InnerHtml = "<p><font color=""Red"">Atleast one tissue must be added against the sample.</font></p>"
                    Return False
                End If
            End If

            Return True

        Catch ex As Exception
            clsAppError.DisplayError("Failed to Validate the data on the 'Sample Details' page.", ex)
        End Try
    End Function

#End Region

#Region "Load Lookup Lists"

    Private Sub LoadLookupLists()
        Try
            AddItemToDropDownList(ddlHistologyType, "use pg number", "5")
            AddItemToDropDownList(ddlHistologyType, "Mouse Projects (60000-89999)", "4")
            AddItemToDropDownList(ddlHistologyType, "General Pool (40000-59999)", "3")
            AddItemToDropDownList(ddlHistologyType, "TB Diag (30000-39999)", "2")
            AddItemToDropDownList(ddlHistologyType, "Abattoir Survey (20000-29999)", "1")
            AddItemToDropDownList(ddlHistologyType, "Neuropath (10000-19999)", "0")
            AddItemToDropDownList(ddlHistologyType, "", "-1")

        Catch ex As Exception
            clsAppError.DisplayError("Failed to poupulate the Histology Ref control on the Sample Details page.", ex)
        End Try
    End Sub

#End Region


End Class
