Partial Class SearchSubmissions
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents SearchResultsPager As DataGridPager
    Protected WithEvents ctlSubmittedDateFrom As CalendarDate
    Protected WithEvents ctlSubmittedDateTo As CalendarDate
    Protected WithEvents ctlReceivedDateFrom As CalendarDate
    Protected WithEvents ctlReceivedDateTo As CalendarDate


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

#Region "Search Criteria"

    Private Class SearchCriteria

#Region "Class member variables"

        Dim m_sSubmissionNumber As String = ""
        Dim m_sStatus As String = ""
        Dim m_sProject As String = ""
        Dim m_sPathologist As String = ""
        Dim m_sSpecies As String = ""
        Dim m_sFixation As String = ""
        Dim m_sSubmittedDateFrom As String = ""
        Dim m_sSubmittedDateTo As String = ""
        Dim m_sReceivedDateFrom As String = ""
        Dim m_sReceivedDateTo As String = ""
        Dim m_sSubmittedArea As String = ""
        Dim m_sSubmittedBy As String = ""
        Dim m_sEnteredBy As String = ""
        Dim m_sHistologyRef As String = ""
        Dim m_sSenderRef As String = ""
        Dim m_iPageNumber As Integer = 0
        Dim m_bAllrecords As Boolean = False
#End Region

#Region "Set and get Functions"

        Public Property AllRecords() As Boolean
            Get
                Return m_bAllrecords
            End Get
            Set(ByVal Value As Boolean)
                AllRecords = Value
            End Set
        End Property

        Public Property SubmissionNumber() As String
            Get
                Return m_sSubmissionNumber
            End Get
            Set(ByVal Value As String)
                m_sSubmissionNumber = Value
            End Set
        End Property

        Public Property Status() As String
            Get
                Return m_sStatus
            End Get
            Set(ByVal Value As String)
                m_sStatus = Value
            End Set
        End Property

        Public Property Project() As String
            Get
                Return m_sProject
            End Get
            Set(ByVal Value As String)
                m_sProject = Value
            End Set
        End Property

        Public Property Pathologist() As String
            Get
                Return m_sPathologist
            End Get
            Set(ByVal Value As String)
                m_sPathologist = Value
            End Set
        End Property

        Public Property Species() As String
            Get
                Return m_sSpecies
            End Get
            Set(ByVal Value As String)
                m_sSpecies = Value
            End Set
        End Property

        Public Property Fixation() As String
            Get
                Return m_sFixation
            End Get
            Set(ByVal Value As String)
                m_sFixation = Value
            End Set
        End Property

        Public Property SubmittedDateFrom() As String
            Get
                Return m_sSubmittedDateFrom
            End Get
            Set(ByVal Value As String)
                m_sSubmittedDateFrom = Value
            End Set
        End Property

        Public Property SubmittedDateTo() As String
            Get
                Return m_sSubmittedDateTo
            End Get
            Set(ByVal Value As String)
                m_sSubmittedDateTo = Value
            End Set
        End Property

        Public Property ReceivedDateFrom() As String
            Get
                Return m_sReceivedDateFrom
            End Get
            Set(ByVal Value As String)
                m_sReceivedDateFrom = Value
            End Set
        End Property

        Public Property ReceivedDateTo() As String
            Get
                Return m_sReceivedDateTo
            End Get
            Set(ByVal Value As String)
                m_sReceivedDateTo = Value
            End Set
        End Property

        Public Property SubmittedArea() As String
            Get
                Return m_sSubmittedArea
            End Get
            Set(ByVal Value As String)
                m_sSubmittedArea = Value
            End Set
        End Property

        Public Property SubmittedBy() As String
            Get
                Return m_sSubmittedBy
            End Get
            Set(ByVal Value As String)
                m_sSubmittedBy = Value
            End Set
        End Property

        Public Property EnteredBy() As String
            Get
                Return m_sEnteredBy
            End Get
            Set(ByVal Value As String)
                m_sEnteredBy = Value
            End Set
        End Property

        Public Property HistologyRef() As String
            Get
                Return m_sHistologyRef
            End Get
            Set(ByVal Value As String)
                m_sHistologyRef = Value
            End Set
        End Property

        Public Property SenderRef() As String
            Get
                Return m_sSenderRef
            End Get
            Set(ByVal Value As String)
                m_sSenderRef = Value
            End Set
        End Property

        Public Property PageNumber() As Integer
            Get
                Return m_iPageNumber
            End Get
            Set(ByVal Value As Integer)
                m_iPageNumber = Value
            End Set
        End Property

#End Region

    End Class

#End Region

    Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        'This solves a problem with when the submission form was printed. 
        'Previously due to not so smart nav the main window would get focus which would
        'hide the popped up window. Disable the code which restores focus to the main window.
        If Page.SmartNavigation = True Then
            DontRestoreFocus(Me.Page)
        End If

        VLAHeader1.SubmissioNoVisible() = False
        VLAHeader1.PageTitle = "Search Submissions"
        CheckPermissions()
        SetCalendarDateHandler(Me.Page)
        SearchResultsPager.SetGrid(grdSearchResults)

        If Not IsPostBack Then
            LoadLookupLists()
            FillSearchGrid(False)
            SetEnterPresses()
        End If

    End Sub


    Private Sub SetEnterPresses()
        SetFocus(txtSubmissionID)
        SetDropDownControlOnEnter(ddlUserArea, ddlStatus.ClientID)
        SetDropDownControlOnEnter(ddlStatus, ddlSubmittedBy.ClientID)
        SetDropDownControlOnEnter(ddlSubmittedBy, ddlProject.ClientID)
        SetDropDownControlOnEnter(ddlProject, ddlEnteredBy.ClientID)
        SetDropDownControlOnEnter(ddlEnteredBy, ddlContact.ClientID)
        SetDropDownControlOnEnter(ddlContact, txtHistRef.ClientID)
        SetDropDownControlOnEnter(ddlSpecies, txtSenderRef.ClientID)
        SetDropDownControlOnEnter(ddlFixation, ctlSubmittedDateFrom.FirstClientID)

        SetTextboxDefaultButton(txtHistRef, btnSearch)
        SetTextboxDefaultButton(txtSubmissionID, btnSearch)
        SetTextboxDefaultButton(txtSenderRef, btnSearch)

        ctlSubmittedDateFrom.SetControlOnEnter(ctlSubmittedDateTo.FirstClientID)
        ctlSubmittedDateTo.SetControlOnEnter(ctlReceivedDateFrom.FirstClientID)
        ctlReceivedDateFrom.SetControlOnEnter(ctlReceivedDateTo.FirstClientID)
        ctlReceivedDateTo.SetControlOnEnter(btnSearch.ClientID)
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

    Private Function SetSearchCriteria(ByVal objSearchCriteria As SearchCriteria) As Integer
        With objSearchCriteria
            txtSubmissionID.Text = .SubmissionNumber.ToString()
            SelectItemInDropDownList(ddlStatus, .Status.ToString())
            SelectItemInDropDownList(ddlProject, .Project.ToString())
            SelectItemInDropDownList(ddlContact, .Pathologist.ToString())
            SelectItemInDropDownList(ddlSpecies, .Species.ToString())
            SelectItemInDropDownList(ddlFixation, .Fixation.ToString())
            SelectItemInDropDownList(ddlUserArea, .SubmittedArea.ToString())
            SelectItemInDropDownList(ddlSubmittedBy, .SubmittedBy.ToString())
            SelectItemInDropDownList(ddlEnteredBy, .EnteredBy.ToString())
            txtHistRef.Text = .HistologyRef.ToString()
            txtSenderRef.Text = .SenderRef.ToString()
            ctlSubmittedDateFrom.DateField = .SubmittedDateFrom.ToString()
            ctlSubmittedDateTo.DateField = .SubmittedDateTo.ToString()
            ctlReceivedDateFrom.DateField = .ReceivedDateFrom.ToString()
            ctlReceivedDateTo.DateField = .ReceivedDateTo.ToString()
            Return .PageNumber
        End With

    End Function

    Private Sub FillSearchGrid(ByVal bAllRecords As Boolean)
        Try
            Dim dtBatches As DataTable
            Dim dvBatchesView As DataView
            Dim objSearchCriteria As New SearchCriteria
            Dim iPageNumber As Integer = 0
            Dim sSort As String = ""

            objSearchCriteria = CType(Session.Item(SessionVars.SV_SearchCriteria), SearchCriteria)

            'Initialise the data table
            Dim objBatch As New HistopathologyLib.clsBatch

            If Not objSearchCriteria Is Nothing Then
                iPageNumber = SetSearchCriteria(objSearchCriteria)
                bAllRecords = CType(Session.Item(SessionVars.SV_AllRecords), Boolean)
            End If

            If Not objBatch.SearchBatchDetails(ddlSubmittedBy.SelectedItem.Value, _
                           ddlProject.SelectedItem.Value, _
                           ddlContact.SelectedItem.Value, _
                           ddlSpecies.SelectedItem.Value, _
                           ddlUserArea.SelectedItem.Value, _
                           ctlSubmittedDateFrom.DateField, _
                           ctlSubmittedDateTo.DateField, _
                           ctlReceivedDateFrom.DateField, _
                           ctlReceivedDateTo.DateField, _
                           ddlFixation.SelectedItem.Value, _
                           txtHistRef.Text, _
                           txtSenderRef.Text, _
                           txtSubmissionID.Text, _
                           ddlStatus.SelectedItem.Value, _
                           ddlEnteredBy.SelectedItem.Value, _
                           bAllRecords, _
                           dtBatches) Then
                Throw New Exception("Batch.SearchBatchDetails returned False")
            End If


            Session(SessionVars.SV_SearchBatchDetailsTable) = dtBatches
            dvBatchesView = dtBatches.DefaultView

            sSort = CStr(Session.Item(SessionVars.SV_Sort))

            If sSort Is Nothing Or sSort = "" Then
                dvBatchesView.Sort = "ID DESC"
            Else
                dvBatchesView.Sort = sSort
            End If

            Session(SessionVars.SV_SearchBatchDetailsView) = dvBatchesView
            lbExportExcel.Visible = True


            dtBatches.TableName = "BatchSearchResults"
            Session.Item(SessionVars.SV_ExcelExport) = dtBatches
            Session.Item(SessionVars.SV_ExcelExportView) = dvBatchesView

            ' initialise the grid
            grdSearchResults.DataSource = dvBatchesView
            grdSearchResults.DataKeyField = "ID"
            grdSearchResults.CurrentPageIndex = iPageNumber
            grdSearchResults.SelectedIndex = -1
            grdSearchResults.EditItemIndex = -1
            grdSearchResults.DataBind()

            ' initialise the pager
            SearchResultsPager.DataTableSessionID = SessionVars.SV_SearchBatchDetailsTable
            SearchResultsPager.DataViewSessionID = SessionVars.SV_SearchBatchDetailsView
            SearchResultsPager.PageLinkCount = 10
            SearchResultsPager.AllowAddNew = False
            SearchResultsPager.AllowEdit = False
            SearchResultsPager.AllowDelete = False
            SearchResultsPager.Refresh()

        Catch ex As Exception
            clsAppError.DisplayError("Error initialising the Batch Search page", ex)
        End Try
    End Sub

    Private Sub LoadLookupLists()
        Dim objDataTable As DataTable
        Dim objLookup As New HistopathologyLib.LookupData
        Dim objUsers As New HistopathologyLib.clsUser

        Try
            'objDataTable = objLookup.GetLookupData(LOOKUP_SPECIES_TYPE)
            objDataTable = objLookup.GetSpeciesLookup()
            If Not (objDataTable Is Nothing) Then
                ddlSpecies.DataSource = objDataTable
                ddlSpecies.DataValueField = "SpeciesID"
                ddlSpecies.DataTextField = "Species"
                ddlSpecies.DataBind()
                Common.AddItemToDropDownList(ddlSpecies)
            End If

            objDataTable = objLookup.GetLookupData(LOOKUP_USER_AREA)
            If Not (objDataTable Is Nothing) Then
                ddlUserArea.DataSource = objDataTable
                ddlUserArea.DataValueField = "Code"
                ddlUserArea.DataTextField = "Description"
                ddlUserArea.DataBind()
                Common.AddItemToDropDownList(ddlUserArea)
            End If

            objDataTable = objLookup.GetLookupData(LOOKUP_FIXATIVE)
            If Not (objDataTable Is Nothing) Then
                ddlFixation.DataSource = objDataTable
                ddlFixation.DataValueField = "Code"
                ddlFixation.DataTextField = "Description"
                ddlFixation.DataBind()
                Common.AddItemToDropDownList(ddlFixation)
            End If

            objDataTable = objLookup.GetStatusLookupData()
            If Not (objDataTable Is Nothing) Then
                ddlStatus.DataSource = objDataTable
                ddlStatus.DataValueField = "Code"
                ddlStatus.DataTextField = "Description"
                ddlStatus.DataBind()
                Common.AddItemToDropDownList(ddlStatus)
            End If

            objDataTable = objUsers.GetUsers()
            If Not (objDataTable Is Nothing) Then
                ddlEnteredBy.DataSource = objDataTable
                ddlEnteredBy.DataValueField = "ID"
                ddlEnteredBy.DataTextField = "Name"
                ddlEnteredBy.DataBind()
                Common.AddItemToDropDownList(ddlEnteredBy)
            End If

            If Not (objDataTable Is Nothing) Then
                ddlSubmittedBy.DataSource = objDataTable
                ddlSubmittedBy.DataValueField = "ID"
                ddlSubmittedBy.DataTextField = "Name"
                ddlSubmittedBy.DataBind()
                Common.AddItemToDropDownList(ddlSubmittedBy)
            End If

            objDataTable = objLookup.GetLookupData(LOOKUP_PROJECTS, False)
            If Not (objDataTable Is Nothing) Then
                ddlProject.DataSource = objDataTable
                ddlProject.DataValueField = "Description"
                ddlProject.DataTextField = "Description"
                ddlProject.DataBind()
                Common.AddItemToDropDownList(ddlProject)
            End If

            objDataTable = objLookup.GetLookupData(LOOKUP_CONTACTS, False)
            If Not (objDataTable Is Nothing) Then
                ddlContact.DataSource = objDataTable
                ddlContact.DataValueField = "Description"
                ddlContact.DataTextField = "Description"
                ddlContact.DataBind()
                Common.AddItemToDropDownList(ddlContact)
            End If


        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve 'Search Submission' drop down lists.", ex)
        End Try

    End Sub

    Private Sub grdSearchResults_SortCommand(ByVal sender As System.Object, ByVal e As System.Web.UI.WebControls.DataGridSortCommandEventArgs) Handles grdSearchResults.SortCommand
        Try
            Dim sSort As String = CType(e.SortExpression, String)
            Dim sStoredSort As String = CType(Session.Item(SessionVars.SV_Sort), String)
            Dim sNewSortAsc As String = sSort & " ASC"
            Dim sNewSortDesc As String = sSort & " DESC"
            Dim sNewSort As String = ""

            If sSort = sStoredSort Or sSort = sNewSortAsc Then
                sNewSort = sNewSortDesc
            ElseIf sSort = sNewSortDesc Then
                sNewSort = sNewSortAsc
            Else
                sNewSort = sSort
            End If
            Session.Item(SessionVars.SV_Sort) = sNewSort
        Catch ex As Exception
            clsAppError.DisplayError("Failed to store new Sort order.", ex)
        End Try
    End Sub

    Private Sub grdSearchResults_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles grdSearchResults.SelectedIndexChanged
        Try
            If grdSearchResults.SelectedIndex >= 0 Then

                Dim dtResultsTable As DataTable = CType(Session.Item(SessionVars.SV_SearchBatchDetailsTable), DataTable)

                If Not dtResultsTable Is Nothing And dtResultsTable.Rows.Count > 0 Then
                    Dim iID As Integer = Convert.ToInt32(grdSearchResults.DataKeys(grdSearchResults.SelectedIndex))
                    Dim sStatus As String
                    Dim drFoundRow As DataRow()
                    Dim sFilter As String

                    sFilter = "ID=" & iID
                    drFoundRow = dtResultsTable.Select(sFilter)

                    If Not drFoundRow Is Nothing Then
                        If drFoundRow.Length > 0 Then
                            sStatus = drFoundRow(0)("BatchStatus").ToString()

                            If sStatus = HistopathologyLib.clsBatch.STATUS_SUBMITTED Then
                                btnPrintSubmission.Enabled = True
                                btnEditSubmission.Enabled = False
                                btnViewSubmission.Enabled = False
                                btnViewQualityData.Enabled = False
                                btnViewArchiveData.Enabled = False
                                btnViewReceipt.Enabled = False
                            ElseIf sStatus = HistopathologyLib.clsBatch.STATUS_COMPLETED Then
                                btnPrintSubmission.Enabled = True
                                btnEditSubmission.Enabled = False
                                btnViewSubmission.Enabled = True
                                btnViewQualityData.Enabled = True
                                btnViewArchiveData.Enabled = True
                                btnViewReceipt.Enabled = True
                            ElseIf sStatus = HistopathologyLib.clsBatch.STATUS_REJECTED Then
                                btnPrintSubmission.Enabled = False
                                btnEditSubmission.Enabled = False
                                btnViewSubmission.Enabled = True
                                btnViewQualityData.Enabled = False
                                btnViewArchiveData.Enabled = False
                                btnViewReceipt.Enabled = True
                            Else
                                btnPrintSubmission.Enabled = True
                                btnEditSubmission.Enabled = True
                                btnViewSubmission.Enabled = True
                                btnViewQualityData.Enabled = True
                                btnViewArchiveData.Enabled = True
                                btnViewReceipt.Enabled = True
                            End If
                        End If
                    End If
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to enable buttons on View Submissions.", ex)
        End Try

    End Sub

    Private Sub btnSearch_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSearch.Click
        revSubmissionNumber.Validate()

        If Not IsDateRangeValid(ctlSubmittedDateFrom, ctlSubmittedDateTo, "Submitted Date") Then
            ctlDiv.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
            grdSearchResults.Visible = False
            SearchResultsPager.Visible = False
            lbExportExcel.Visible = False
            Exit Sub
        End If

        If Not IsDateRangeValid(ctlReceivedDateFrom, ctlReceivedDateTo, "Received Date") Then
            ctlDiv.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
            grdSearchResults.Visible = False
            SearchResultsPager.Visible = False
            lbExportExcel.Visible = False
            Exit Sub
        End If

        If Not revSubmissionNumber.IsValid Then
            ctlDiv.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
            grdSearchResults.Visible = False
            SearchResultsPager.Visible = False
            lbExportExcel.Visible = False
            Exit Sub
        End If

        ctlDiv.InnerHtml = ""
        grdSearchResults.Visible = True
        SearchResultsPager.Visible = True
        lbExportExcel.Visible = True

        Session(SessionVars.SV_SearchBatchDetailsTable) = Nothing
        Session(SessionVars.SV_SearchBatchDetailsView) = Nothing
        Session(SessionVars.SV_SearchCriteria) = Nothing
        Session.Item(SessionVars.SV_AllRecords) = True
        FillSearchGrid(True)
    End Sub

    Private Sub StoreSearchCriteria()
        Dim objSearchCriteria As New SearchCriteria

        With objSearchCriteria
            .SubmissionNumber = txtSubmissionID.Text
            .Status = ddlStatus.SelectedItem.Value
            .Project = ddlProject.SelectedItem.Value
            .Pathologist = ddlContact.SelectedItem.Value
            .Species = ddlSpecies.SelectedItem.Value
            .Fixation = ddlFixation.SelectedItem.Value
            .SubmittedArea = ddlUserArea.SelectedItem.Value
            .SubmittedBy = ddlSubmittedBy.SelectedItem.Value
            .EnteredBy = ddlEnteredBy.SelectedItem.Value
            .HistologyRef = txtHistRef.Text
            .SenderRef = txtSenderRef.Text
            .SubmittedDateFrom = ctlSubmittedDateFrom.DateField
            .SubmittedDateTo = ctlSubmittedDateTo.DateField
            .ReceivedDateFrom = ctlReceivedDateFrom.DateField
            .ReceivedDateTo = ctlReceivedDateTo.DateField
            .PageNumber = grdSearchResults.CurrentPageIndex
        End With

        Session.Item(SessionVars.SV_SearchCriteria) = objSearchCriteria
    End Sub

    Private Sub btnSearchMenu_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSearchMenu.Click
        RemoveSessionVars(Session)
        Response.Redirect("Home.aspx")
    End Sub

    Private Sub btnPrintSubmission_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnPrintSubmission.Click
        Try
            If grdSearchResults.SelectedIndex >= 0 Then
                Dim iBatchID As Integer = grdSearchResults.DataKeys(grdSearchResults.SelectedIndex)
                Session.Item(SessionVars.SV_BatchID) = iBatchID
                Session.Item(SessionVars.SV_SearchSubmission) = True
                Session.Item(SessionVars.SV_RedirectAfterPrint) = "SearchSubmissions.aspx"
                StoreSearchCriteria()
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to print submission.", ex)
        End Try

        Response.Redirect("FinalPrintBatch.aspx")
    End Sub

    Private Sub LoadSelectedBatchDetails()
        If grdSearchResults.SelectedIndex >= 0 Then
            Dim iBatchID As Integer = grdSearchResults.DataKeys(grdSearchResults.SelectedIndex)
            Dim dtData As DataTable = Session.Item(SessionVars.SV_SearchBatchDetailsTable)
            Dim dsBatchDetails As DataSet
            Dim dtBatch As DataTable
            Dim sFilter As String
            Dim drFoundRows As DataRow()

            sFilter = "ID=" & iBatchID
            drFoundRows = dtData.Select(sFilter)
            If Not drFoundRows Is Nothing Then
                If CInt(drFoundRows(0)("BatchType")) = 1 Then
                    Session.Item(SessionVars.SV_SubmissionType) = SUBMISSION_NONTSE
                Else
                    Session.Item(SessionVars.SV_SubmissionType) = SUBMISSION_TSE
                End If
            End If

            GetCommonBatchDetailsFromDatabase(iBatchID, Session)

            dsBatchDetails = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            If Not dsBatchDetails Is Nothing Then
                dtBatch = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)
                If Not dtBatch Is Nothing And dtBatch.Rows.Count <> 0 Then
                    If dtBatch.Rows(0)("BatchStatus").ToString() = HistopathologyLib.clsBatch.STATUS_INPROGRESS Or _
                    dtBatch.Rows(0)("BatchStatus").ToString() = HistopathologyLib.clsBatch.STATUS_COMPLETED Or _
                    dtBatch.Rows(0)("BatchStatus").ToString() = HistopathologyLib.clsBatch.STATUS_ONHOLD Then
                        If dtBatch.Rows(0)("IsBlocked") = True Then
                            GetBatchBlockDetailsFromDatabase(iBatchID, Session)
                            Session.Item(SessionVars.SV_Cassetted) = True
                        Else
                            GetBatchSubmissionDetailsFromDatabase(iBatchID, Session)
                            Session.Item(SessionVars.SV_Cassetted) = False
                        End If
                    Else
                        If dtBatch.Rows(0)("Cassetted") = False Then
                            GetBatchSubmissionDetailsFromDatabase(iBatchID, Session)
                            Session.Item(SessionVars.SV_Cassetted) = False
                        Else
                            GetBatchBlockDetailsFromDatabase(iBatchID, Session)
                            Session.Item(SessionVars.SV_Cassetted) = True
                        End If
                    End If
                End If
            End If

            Session.Item(SessionVars.SV_BatchID) = iBatchID
        End If
    End Sub

    Private Sub btnEditSubmission_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnEditSubmission.Click
        Try
            LoadSelectedBatchDetails()

            Session.Item(SessionVars.SV_ViewSubmission) = False
            Session.Item(SessionVars.SV_EditingBatch) = True
            Session.Item(SessionVars.SV_ReceiveBatch) = False
            Session.Item(SessionVars.SV_SaveFromBatchDetails) = True
            Session.Item(SessionVars.SV_RedirectCancelPage) = "SearchSubmissions.aspx"
            Session.Item(SessionVars.SV_RedirectPage) = "SearchSubmissions.aspx"

            StoreSearchCriteria()

        Catch ex As Exception
            clsAppError.DisplayError("Failed to edit submission.", ex)
        End Try

        Try
            Dim objBreadCrumbList As New ArrayList
            objBreadCrumbList.Insert(0, "Edit Submission")
            objBreadCrumbList.Insert(1, "Submission")
            objBreadCrumbList.Insert(2, "Submission Details")
            Session.Item(SessionVars.SV_BreadCrumbs) = objBreadCrumbList
        Catch ex As Exception
            clsAppError.DisplayError("Bread Crumb Error, SearchSubmissions.aspx.", ex)
        End Try

        Response.Redirect("BatchDetails.aspx")
    End Sub

    Private Sub btnViewSubmission_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnViewSubmission.Click
        Try
            LoadSelectedBatchDetails()

            Session.Item(SessionVars.SV_ViewSubmission) = True
            Session.Item(SessionVars.SV_EditingBatch) = False
            Session.Item(SessionVars.SV_RedirectCancelPage) = "SearchSubmissions.aspx"
            Session.Item(SessionVars.SV_RedirectPage) = "SearchSubmissions.aspx"

            StoreSearchCriteria()
        Catch ex As Exception
            clsAppError.DisplayError("Failed to edit submission.", ex)
        End Try

        Response.Redirect("BatchDetails.aspx")
    End Sub

    Private Sub btnViewReceipt_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnViewReceipt.Click
        Try
            LoadSelectedBatchDetails()

            Session.Item(SessionVars.SV_ViewSubmission) = True
            Session.Item(SessionVars.SV_EditingBatch) = False
            Session.Item(SessionVars.SV_RedirectCancelPage) = "SearchSubmissions.aspx"
            Session.Item(SessionVars.SV_RedirectPage) = "SearchSubmissions.aspx"

            StoreSearchCriteria()

        Catch ex As Exception
            clsAppError.DisplayError("Failed to edit submission.", ex)
        End Try

        Response.Redirect("ReceiveBatch.aspx")
    End Sub

    Private Sub btnViewQualityData_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnViewQualityData.Click
        Try
            If grdSearchResults.SelectedIndex >= 0 Then
                Dim iBatchID As Integer = grdSearchResults.DataKeys(grdSearchResults.SelectedIndex)

                GetCommonBatchDetailsFromDatabase(iBatchID, Session)
                GetBatchBlockDetailsFromDatabase(iBatchID, Session)

                Session.Item(SessionVars.SV_Cassetted) = True
                Session.Item(SessionVars.SV_BatchID) = iBatchID
                Session.Item(SessionVars.SV_ViewSubmission) = True
                Session.Item(SessionVars.SV_EditingBatch) = False
                Session.Item(SessionVars.SV_RedirectCancelPage) = "SearchSubmissions.aspx"
                Session.Item(SessionVars.SV_RedirectPage) = "SearchSubmissions.aspx"

                StoreSearchCriteria()
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to view quality data.", ex)
        End Try
        Response.Redirect("QualityData.aspx")
    End Sub

    Private Sub btnViewArchiveData_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnViewArchiveData.Click
        Try
            If grdSearchResults.SelectedIndex >= 0 Then

                Dim iBatchID As Integer = grdSearchResults.DataKeys(grdSearchResults.SelectedIndex)

                Session.Item(SessionVars.SV_BatchID) = iBatchID
                Session.Item(SessionVars.SV_ViewSubmission) = True
                Session.Item(SessionVars.SV_EditingBatch) = False
                Session.Item(SessionVars.SV_RedirectCancelPage) = "SearchSubmissions.aspx"
                Session.Item(SessionVars.SV_RedirectPage) = "SearchSubmissions.aspx"

                StoreSearchCriteria()
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to view archive data.", ex)
        End Try
        Response.Redirect("ArchiveMenu.aspx")
    End Sub

    Private Sub lbExportExcel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lbExportExcel.Click
        Try
            Dim dtBatches As DataTable = CType(Session(SessionVars.SV_SearchBatchDetailsTable), DataTable)
            Dim dtNewExportData As New DataTable
            Dim dr As DataRow
            'Dim iSubmittedBy As Integer = 0
            'Dim iOldSubmittedBy As Integer = 0
            Dim iReceivedBy As Integer = 0
            Dim iOtherSubmittedBy As Integer = 0
            Dim sSubmissionType As String
            Dim sSafeToHandle As String
            Dim drNewRow As DataRow
            Dim objContactData As DataTable
            Dim objProjectData As DataTable
            Dim objUsersData As DataTable
            Dim objSpeciesData As DataTable
            Dim objAllUsersData As DataTable
            Dim sUserName As String
            Dim sUserArea As String
            Dim sUserGroup As String
            Dim sUserAreaID As String
            Dim dvBatchesView As DataView

            dtNewExportData.TableName = "SubmissionsViewResults"

            'Setup the columns for the export datatable
            dtNewExportData.Columns.Add("ID", System.Type.GetType("System.String"))
            dtNewExportData.Columns("ID").ColumnName = "Submission Number"
            dtNewExportData.Columns.Add("ProjectContractCode", System.Type.GetType("System.String"))
            dtNewExportData.Columns("ProjectContractCode").ColumnName = "Project/Contract"
            dtNewExportData.Columns.Add("ContactName", System.Type.GetType("System.String"))
            dtNewExportData.Columns("ContactName").ColumnName = "Pathologist"
            dtNewExportData.Columns.Add("Species", System.Type.GetType("System.String"))
            dtNewExportData.Columns("Species").ColumnName = "Species"
            dtNewExportData.Columns.Add("BatchDate", System.Type.GetType("System.String"))
            dtNewExportData.Columns("BatchDate").ColumnName = "Submitted Date"
            dtNewExportData.Columns.Add("BatchType", System.Type.GetType("System.String"))
            dtNewExportData.Columns("BatchType").ColumnName = "Submission Type"
            dtNewExportData.Columns.Add("SubmittedBy", System.Type.GetType("System.String"))
            dtNewExportData.Columns("SubmittedBy").ColumnName = "Submitted By"
            dtNewExportData.Columns.Add("SafeToHandle", System.Type.GetType("System.String"))
            dtNewExportData.Columns("SafeToHandle").ColumnName = "Safe To Handle"
            dtNewExportData.Columns.Add("DateReceived", System.Type.GetType("System.String"))
            dtNewExportData.Columns("DateReceived").ColumnName = "Received Date"
            dtNewExportData.Columns.Add("ReceivedTime", System.Type.GetType("System.String"))
            dtNewExportData.Columns("ReceivedTime").ColumnName = "Time Received/Rejected"
            dtNewExportData.Columns.Add("ReceivedBy", System.Type.GetType("System.String"))
            dtNewExportData.Columns("ReceivedBy").ColumnName = "Received By"
            dtNewExportData.Columns.Add("OtherSubmittedBy", System.Type.GetType("System.String"))
            dtNewExportData.Columns("OtherSubmittedBy").ColumnName = "Other Submitted By"
            dtNewExportData.Columns.Add("Comments", System.Type.GetType("System.String"))
            dtNewExportData.Columns("Comments").ColumnName = "Comments"
            dtNewExportData.Columns.Add("CustomerReceivedDate", System.Type.GetType("System.String"))
            dtNewExportData.Columns("CustomerReceivedDate").ColumnName = "Customer Received Date"
            dtNewExportData.Columns.Add("Status", System.Type.GetType("System.String"))
            dtNewExportData.Columns("Status").ColumnName = "Status"
            dtNewExportData.Columns.Add("DateCompleted", System.Type.GetType("System.String"))
            dtNewExportData.Columns("DateCompleted").ColumnName = "Completed Date"

            For Each dr In dtBatches.Rows
                drNewRow = dtNewExportData.NewRow()

                If Not IsDBNull(dr("ID")) Then
                    drNewRow("Submission Number") = dr("ID")
                End If

                If Not IsDBNull(dr("ProjectDescription")) Then
                    drNewRow("Project/Contract") = dr("ProjectDescription").ToString()
                End If

                If Not IsDBNull(dr("ContactDescription")) Then
                    drNewRow("Pathologist") = dr("ContactDescription").ToString()
                End If

                If Not IsDBNull(dr("Species")) Then
                    drNewRow("Species") = dr("Species").ToString()
                End If

                If Not IsDBNull(dr("BatchDate")) Then
                    drNewRow("Submitted Date") = dr("BatchDate").ToString()
                End If

                If Not IsDBNull(dr("DateReceived")) Then
                    drNewRow("Received Date") = dr("DateReceived").ToString()
                End If

                If Not IsDBNull(dr("ReceivedTime")) Then
                    drNewRow("Time Received/Rejected") = dr("ReceivedTime").ToString()
                End If

                If Not IsDBNull(dr("SubmittedBy")) Then
                    drNewRow("Submitted By") = dr("SubmittedBy").ToString()
                End If

                If Not IsDBNull(dr("ReceivedBy")) Then
                    drNewRow("Received By") = dr("ReceivedBy").ToString()
                End If

                If Not IsDBNull(dr("OtherSubmittedBy")) Then
                    drNewRow("Other Submitted By") = dr("OtherSubmittedBy").ToString()
                End If

                If Not IsDBNull(dr("Comments")) Then
                    drNewRow("Comments") = dr("Comments").ToString()
                End If

                If Not IsDBNull(dr("CustomerReceivedDate")) Then
                    drNewRow("Customer Received Date") = dr("CustomerReceivedDate").ToString()
                End If

                If Not IsDBNull(dr("Status")) Then
                    drNewRow("Status") = dr("Status")
                End If

                If Not IsDBNull(dr("DateCompleted")) Then
                    drNewRow("Completed Date") = dr("DateCompleted").ToString()
                End If

                If Not IsDBNull(dr("BatchType")) Then
                    sSubmissionType = dr("BatchType").ToString()
                    If sSubmissionType = "0" Then
                        drNewRow("Submission Type") = "TSE"
                    Else
                        drNewRow("Submission Type") = "NON TSE"
                    End If
                End If

                If Not IsDBNull(dr("SafeToHandle")) Then
                    sSafeToHandle = dr("SafeToHandle").ToString()
                    If sSafeToHandle = "1" Then
                        drNewRow("Safe To Handle") = "Yes"
                    Else
                        drNewRow("Safe To Handle") = "No"
                    End If
                End If

                dtNewExportData.Rows.Add(drNewRow)
            Next

            Session.Item(SessionVars.SV_ExcelExport) = dtNewExportData
            dvBatchesView = dtNewExportData.DefaultView
            Session.Item(SessionVars.SV_ExcelExportView) = dvBatchesView

        Catch ex As Exception
            clsAppError.DisplayError("Failed to receive submission.", ex)
        End Try

        OpenDownloadPopup("ExcelExport.aspx", Me.Page)
    End Sub

    Private Sub btnClearFilter_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnClearFilter.Click
        ctlDiv.InnerHtml = ""
        grdSearchResults.Visible = True
        SearchResultsPager.Visible = True
        lbExportExcel.Visible = True

        txtSubmissionID.Text = ""

        txtHistRef.Text = ""
        txtSenderRef.Text = ""

        ctlSubmittedDateFrom.DateField = ""
        ctlReceivedDateFrom.DateField = ""
        ctlSubmittedDateTo.DateField = ""
        ctlReceivedDateTo.DateField = ""

        SelectItemInDropDownList(ddlStatus, "")
        SelectItemInDropDownList(ddlSpecies, "")
        SelectItemInDropDownList(ddlFixation, "")
        SelectItemInDropDownList(ddlSubmittedBy, "")
        SelectItemInDropDownList(ddlContact, "")
        SelectItemInDropDownList(ddlProject, "")
        SelectItemInDropDownList(ddlEnteredBy, "")
        SelectItemInDropDownList(ddlUserArea, "")

        Session.Item(SessionVars.SV_Sort) = Nothing
        Session.Item(SessionVars.SV_SearchCriteria) = Nothing
        Session.Item(SessionVars.SV_AllRecords) = False
        FillSearchGrid(False)
    End Sub

    Private Sub SearchResultsPager_PageChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SearchResultsPager.PageChanged
        btnEditSubmission.Enabled = False
        btnPrintSubmission.Enabled = False
        btnViewQualityData.Enabled = False
        btnViewSubmission.Enabled = False
        btnViewArchiveData.Enabled = False
        btnViewReceipt.Enabled = False
    End Sub
End Class
