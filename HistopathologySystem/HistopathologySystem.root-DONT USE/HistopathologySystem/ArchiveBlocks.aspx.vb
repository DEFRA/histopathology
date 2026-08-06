Imports System.Text.RegularExpressions

Partial Class ArchiveBlocks
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents BlocksPager As DataGridPager
    Protected WithEvents Batch1 As Batch
    Protected WithEvents ctlArchivedDate As CalendarDate

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
        VLAHeader1.PageTitle = "Archive Blocks"
        CheckPermissions()
        BlocksPager.SetGrid(grdBlocks)
        SetCalendarDateHandler(Me.Page)
        ctlArchivedDate.Mandatory = True
        SetClientValidation()

        If Not IsPostBack Then
            Batch1.DisplayDetails()
            InitialiseArchiveGrid()
            LoadLookupLists()
            SetupRadioButtonList()

            If CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean) = True Then
                EnableControls(False)
            Else
                PromptBeforeSaveScript("Are you sure you want to Cancel? Any archive information entered since you last clicked the Done button will be lost.", btnBack)
            End If

            btnSave.Enabled = False
            SetEnterKeyPress()
        End If



    End Sub

#Region "Grid Handling"

    Private Sub InitialiseArchiveGrid()
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtSummary As New DataTable
            Dim objSummary As New HistopathologyLib.clsBatchSummary
            Dim dtBlockRefList As DataTable
            Dim dtHistologyRefList As DataTable

            If Not dsBatchDetails Is Nothing Then
                If Not objSummary.CreateArchiveBlockSummaryData(dsBatchDetails, dtSummary, dtBlockRefList, dtHistologyRefList) Then
                    Throw New Exception("BatchSummary.CreateArchiveBlockSummary returned false.")
                End If
            End If

            ' create a dataview for filtering and sorting
            Dim dv As DataView = dtSummary.DefaultView

            Session.Item(SessionVars.SV_BlockSummaryTable) = dtSummary
            Session.Item(SessionVars.SV_BlockSummaryView) = dv

            grdBlocks.DataSource = dtSummary
            grdBlocks.DataKeyField = "ID"
            grdBlocks.DataBind()
            grdBlocks.Enabled = True

            ' initialise the pager
            BlocksPager.DataTableSessionID = SessionVars.SV_BlockSummaryTable
            BlocksPager.DataViewSessionID = SessionVars.SV_BlockSummaryView
            BlocksPager.PageLinkCount = 10
            BlocksPager.AllowAddNew = False
            BlocksPager.AllowEdit = False
            BlocksPager.AllowDelete = False
            BlocksPager.Rebind()
            BlocksPager.Refresh()

            'Setup the filter dropdown lists
            If Not dtBlockRefList Is Nothing Then
                ddlBlockRefList.DataSource = dtBlockRefList
                ddlBlockRefList.DataValueField = "ID"
                ddlBlockRefList.DataTextField = "Description"
                ddlBlockRefList.DataBind()
                AddItemToDropDownList(ddlBlockRefList, "")
            End If

            If Not dtHistologyRefList Is Nothing Then
                ddlHistologyRefList.DataSource = dtHistologyRefList
                ddlHistologyRefList.DataValueField = "ID"
                ddlHistologyRefList.DataTextField = "Description"
                ddlHistologyRefList.DataBind()
                AddItemToDropDownList(ddlHistologyRefList, "")
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to Initialise the Blocks Archive grid.", ex)
        End Try
    End Sub

#End Region

#Region "Lookup Lists"

    Private Sub LoadLookupLists()
        Try
            Dim objLookup As New HistopathologyLib.LookupData
            Dim dtData As DataTable

            dtData = objLookup.GetLookupData(LOOKUP_ARCHIVE_LOCATION)

            If Not dtData Is Nothing Then
                ddlArchiveLocation.DataSource = dtData
                ddlArchiveLocation.DataTextField = "Description"
                ddlArchiveLocation.DataValueField = "Code"
                ddlArchiveLocation.DataBind()
                AddItemToDropDownList(ddlArchiveLocation)
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to load the lookup lists, Archive Blocks page.", ex)
        End Try
    End Sub

#End Region

#Region "Event Handlers"

    Private Sub btnGoToPage_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnGoToPage.Click
        Try
            Dim iPageNumber As Integer = 0

            If ValidatePageNumberFields() Then
                iPageNumber = CInt(txtPage.Text)
                If iPageNumber > 0 Then
                    grdBlocks.CurrentPageIndex = iPageNumber - 1
                Else
                    grdBlocks.CurrentPageIndex = 0
                End If
                grdBlocks.SelectedIndex = -1
                grdBlocks.EditItemIndex = -1
                BlocksPager.Rebind()
                BlocksPager.Refresh()
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to go to selected page.", ex)
        End Try
    End Sub

    Public Sub Check_Clicked(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Try
            If Not CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean) = True Then
                Dim dgItem As DataGridItem
                Dim chkSelected As CheckBox
                Dim bSelected As Boolean = False
                Dim iCount As Integer = 0
                Dim iIndex As Integer
                Dim iID As Int32
                Dim sFilter As String
                Dim dtData As DataTable = CType(Session.Item(SessionVars.SV_BlockSummaryTable), DataTable)
                Dim drData As DataRow()
                Dim iRowCount As Integer = 0
                Dim drRow As DataRow
                Dim iIDBackup As Int32

                'Update the tick boxes and Refind the datarow we need to update
                For Each dgItem In grdBlocks.Items
                    chkSelected = dgItem.FindControl("cbSelected")

                    iIndex = dgItem.ItemIndex
                    grdBlocks.SelectedIndex = iIndex

                    iID = Convert.ToInt32(grdBlocks.DataKeys(grdBlocks.SelectedIndex))
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

                For Each dgItem In grdBlocks.Items
                    chkSelected = dgItem.FindControl("cbSelected")

                    iIndex = dgItem.ItemIndex
                    grdBlocks.SelectedIndex = iIndex

                    iID = Convert.ToInt32(grdBlocks.DataKeys(grdBlocks.SelectedIndex))
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

                            'If something has been de-selected the select all
                            cbSelectAll.Checked = False
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

                    For Each dgItem In grdBlocks.Items
                        chkSelected = dgItem.FindControl("cbSelected")

                        If Not chkSelected Is Nothing Then
                            If chkSelected.Checked = True Then
                                grdBlocks.SelectedIndex = dgItem.ItemIndex
                                Exit For
                            End If
                        End If
                    Next
                Else
                    grdBlocks.SelectedIndex = -1
                End If

                sFilter = "ID = " & CStr(iIDBackup)
                drData = dtData.Select(sFilter)

                'Depending on the number of items selected...
                If Not drData Is Nothing Then
                    If iCount = 2 Then
                        If bSelected Then
                            InitialiseControls()
                            btnUpdate.Enabled = True
                        End If
                    ElseIf iCount = 1 Then
                        With drData(0)
                            SelectItemInDropDownList(ddlArchiveLocation, .Item("ArchiveLocation").ToString())
                            ctlArchivedDate.DateField = .Item("ArchivedDate").ToString()
                            txtComment.Text = .Item("ArchiveComment").ToString()
                        End With
                        btnUpdate.Enabled = True
                    ElseIf iCount = 0 Then
                        btnUpdate.Enabled = False
                        InitialiseControls()
                    End If
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to enable to update selected button.", ex)
        End Try
    End Sub

    Private Sub btnBack_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnBack.Click
        Response.Redirect("ArchiveMenu.aspx")
    End Sub

    Private Sub cbSelectAll_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cbSelectAll.CheckedChanged
        Try
            Dim dgItem As DataGridItem
            Dim chkSelected As CheckBox
            Dim bSelected As Boolean = cbSelectAll.Checked
            Dim drRow As DataRow
            Dim dtData As DataTable = CType(Session.Item(SessionVars.SV_BlockSummaryTable), DataTable)
            Dim dv As DataView = CType(Session(SessionVars.SV_BlockSummaryView), DataView)
            Dim iFoundRow As Integer = 0

            If Not dtData Is Nothing AndAlso Not dv Is Nothing Then
                For Each dgItem In grdBlocks.Items
                    chkSelected = dgItem.FindControl("cbSelected")

                    If Not chkSelected Is Nothing Then
                        If chkSelected.Enabled Then
                            chkSelected.Checked = bSelected
                        End If
                    End If
                Next

                For Each drRow In dtData.Rows
                    drRow("Selected") = bSelected
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
                        If cbSelectAll.Checked = True Then
                            drRow("Selected") = True
                        End If
                    End If
                Next
                '----
            End If

            btnUpdate.Enabled = cbSelectAll.Checked

            InitialiseControls()

        Catch ex As Exception
            clsAppError.DisplayError("Failed to select all tests.", ex)
        End Try
    End Sub

    Private Sub btnUpdate_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnUpdate.Click
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dReceivedDate As Date
            Dim dDate As Date

            If Not dsBatchDetails Is Nothing AndAlso dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE).Rows.Count > 0 Then
                dReceivedDate = CType(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("DateReceived"), Date)
            End If

            If ValidateMandatoryFields() Then
                If Not ctlArchivedDate.DateField = "" Then
                    lblError.Visible = False
                    If ctlArchivedDate.Validate(dDate, CDate(dReceivedDate.ToShortDateString), ctlArchivedDate.ValidationType.eValidateEarliest, "Archive date must be the same or later than the Submission received date of " & dReceivedDate.ToShortDateString) And _
                       ctlArchivedDate.Validate(dDate, CDate(dDate.Now.ToShortDateString), ctlArchivedDate.ValidationType.eValidateLatest, "Must be today or earlier") Then
                        Dim drData() As DataRow
                        Dim drRow As DataRow
                        Dim dtData As DataTable = CType(Session.Item(SessionVars.SV_BlockSummaryTable), DataTable)
                        Dim dgItem As DataGridItem
                        Dim cbSelected As CheckBox
                        Dim sFilter As String
                        Dim iID As Int32
                        Dim iCount As Integer = 0
                        Dim iSelectedItemCount As Integer = 0

                        If Not dtData Is Nothing Then
                            'Check if more than 2 items have been selected. 
                            For Each drRow In dtData.Rows
                                If Not IsDBNull(drRow("Selected")) Then
                                    If drRow("Selected") = True Then
                                        iSelectedItemCount = iSelectedItemCount + 1
                                    End If
                                End If
                            Next

                            'For each item in the grid that has been selected update the grid bound datatable with the
                            'archive data that the user has entered.
                            For Each drRow In dtData.Rows
                                If Not IsDBNull(drRow("Selected")) Then
                                    If drRow("Selected") = True Then
                                        drRow("Selected") = False

                                        'If more than 2 items have been selected only update the fields that have changed, i.e from blanks
                                        If iSelectedItemCount >= 2 Then
                                            With drRow
                                                If ddlArchiveLocation.SelectedIndex <> 0 Then
                                                    .Item("ArchiveLocation") = FormatEmptyString(ddlArchiveLocation.SelectedItem.Value())
                                                End If

                                                If ctlArchivedDate.DateField <> "" Then
                                                    .Item("ArchivedDate") = FormatEmptyString(ctlArchivedDate.DateField)
                                                End If

                                                If txtComment.Text <> "" Then
                                                    .Item("ArchiveComment") = FormatEmptyString(txtComment.Text())
                                                End If
                                            End With
                                        Else
                                            With drRow
                                                .Item("ArchiveLocation") = FormatEmptyString(ddlArchiveLocation.SelectedItem.Value())
                                                .Item("ArchivedDate") = FormatEmptyString(ctlArchivedDate.DateField)
                                                .Item("ArchiveComment") = FormatEmptyString(txtComment.Text())
                                            End With
                                        End If
                                    End If
                                End If
                            Next

                            InitialiseControls()

                            BlocksPager.Rebind()
                            BlocksPager.Refresh()
                            cbSelectAll.Checked = False
                            btnUpdate.Enabled = False
                            lblError.Visible = False
                            btnSave.Enabled = True
                        End If
                    End If
                Else
                    lblError.Visible = True
                    ctlDIV.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
                End If
            End If
            ctlDIV.InnerHtml = ""
        Catch ex As Exception
            clsAppError.DisplayError("Failed to update the selected records with the archive information.", ex)
        End Try
    End Sub

    Private Sub grdBlocks_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles grdBlocks.SelectedIndexChanged
        Try
            Dim drData As DataRow()
            Dim dtData As DataTable = CType(Session.Item(SessionVars.SV_BlockSummaryTable), DataTable)
            Dim dgItem As DataGridItem = grdBlocks.SelectedItem
            Dim iID As Int32 = Convert.ToInt32(grdBlocks.DataKeys(grdBlocks.SelectedIndex))
            Dim sFilter As String = "ID = " & CStr(iID)
            Dim cbSelected As CheckBox
            Dim sTestResult As String
            Dim cbOtherSelected As CheckBox
            Dim drRow As DataRow

            If CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean) = True Then
                EnableControls(False)
            Else
                EnableControls(True)
            End If

            If Not dgItem Is Nothing Then
                cbSelected = dgItem.FindControl("cbSelected")
                If Not cbSelected Is Nothing Then
                    For Each dgItem In grdBlocks.Items
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

            drData = dtData.Select(sFilter)
            With drData(0)
                drData(0)("Selected") = True
                SelectItemInDropDownList(ddlArchiveLocation, .Item("ArchiveLocation").ToString())
                ctlArchivedDate.DateField = .Item("ArchivedDate").ToString()
                txtComment.Text = .Item("ArchiveComment").ToString()
            End With

            If Not CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean) = True Then
                btnUpdate.Enabled = True
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to display details for selected item.", ex)
        End Try
    End Sub

    Private Sub btnSave_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSave.Click
        Dim bRedirect As Boolean = False
        Dim objErrorlist As New ArrayList
        Dim objBatch As New HistopathologyLib.clsBatch
        Dim iBatchID As Integer

        Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)

        UpdateSessionWithArchiveData()

        Dim bSuccess As Boolean = objBatch.UpdateBatchDetails(CType(Session(SessionVars.SV_HeaderUserID), Integer), dsBatchDetails, objErrorlist, True, iBatchID)
        If bSuccess Then
            If objErrorlist.Count = 0 Then
                bRedirect = True
            Else
                ctlDIV.InnerHtml = "<p><font color=""Red"">The database has been updated but some errors were encountered:</color></p><p>&nbsp;</p><p><font color=""Red"">" & Join(objErrorlist.ToArray, "</color></p><p>") & "</p>"
            End If
        Else
            ctlDIV.InnerHtml = "<p><font color=""Red"">The database has not been updated because the following error(s) occurred:</color></p><p>&nbsp;</p><p><font color=""Red"">" & Join(objErrorlist.ToArray, "</color></p><p>") & "</p>"
        End If

        If bRedirect Then
            Response.Redirect(CStr(Session.Item(SessionVars.SV_RedirectPage)))
        End If
    End Sub

    Private Sub btnFilter_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnFilter.Click
        Try
            Dim dv As DataView = CType(Session(SessionVars.SV_BlockSummaryView), DataView)
            Dim dtData As DataTable = CType(Session.Item(SessionVars.SV_BlockSummaryTable), DataTable)
            Dim sFilter As String
            Dim sHistoRef As String
            Dim sBlock As String
            Dim li As ListItem
            Dim drRow As DataRow
            Dim iFoundRow As Integer = 0

            For Each li In rblFilter.Items
                'If li.Value = "All" And li.Selected = True Then
                'Exit Sub
                If li.Value = "Filter" Then
                    li.Selected = True
                Else
                    li.Selected = False
                End If
            Next

            If Not dv Is Nothing AndAlso Not dtData Is Nothing Then
                sHistoRef = "HistologyRef=" & "'" & ddlHistologyRefList.SelectedItem.Text() & "'"
                sBlock = "BlockRef=" & "'" & ddlBlockRefList.SelectedItem.Text() & "'"

                If ddlHistologyRefList.SelectedItem.Value <> "" Then
                    sFilter = sHistoRef
                    If ddlBlockRefList.SelectedItem.Value <> "" Then
                        sFilter = sFilter & " AND " & sBlock
                    End If
                Else
                    If ddlBlockRefList.SelectedItem.Value <> "" Then
                        sFilter = sBlock
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
                        If cbSelectAll.Checked = True Then
                            drRow("Selected") = True
                        End If
                    End If
                Next
                '----

                InitialiseControls()
                grdBlocks.CurrentPageIndex = 0
                grdBlocks.SelectedIndex = -1
                grdBlocks.EditItemIndex = -1

                BlocksPager.Rebind()
                BlocksPager.Refresh()

                'cbSelectAll.Checked = False
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to filter the archive block information.", ex)
        End Try
    End Sub

    Private Sub rblFilter_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles rblFilter.SelectedIndexChanged
        Try
            If rblFilter.SelectedItem.Value = "All" Then
                Dim dtData As DataTable = CType(Session.Item(SessionVars.SV_BlockSummaryTable), DataTable)
                Dim dv As DataView = CType(Session.Item(SessionVars.SV_BlockSummaryView), DataView)
                Dim sFilter As String
                Dim sHistoRef As String
                Dim sTest As String
                Dim dgItem As DataGridItem
                Dim iFoundRow As Integer = 0
                Dim drRow As DataRow
                Dim bSelected As Boolean = cbSelectAll.Checked
                Dim chkSelected As CheckBox

                If Not dv Is Nothing And Not dtData Is Nothing Then
                    dv.RowFilter = ""

                    grdBlocks.CurrentPageIndex = 0
                    grdBlocks.SelectedIndex = -1
                    grdBlocks.EditItemIndex = -1

                    BlocksPager.Rebind()
                    BlocksPager.Refresh()
                End If

                SelectItemInDropDownList(ddlHistologyRefList, "")
                SelectItemInDropDownList(ddlBlockRefList, "")

                '----
                'If any items that are in the view were selected before filtering
                'then leave them selected, otherwise selected
                For Each drRow In dtData.Rows
                    If cbSelectAll.Checked = True Then
                        drRow("Selected") = True
                    End If
                Next

                'Check all the columns
                For Each dgItem In grdBlocks.Items
                    chkSelected = dgItem.FindControl("cbSelected")

                    If Not chkSelected Is Nothing Then
                        If bSelected Then
                            chkSelected.Checked = True
                        End If
                    End If
                Next
                '----
                'cbSelectAll.Checked = False
                InitialiseControls()
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to filter the block archive information.", ex)
        End Try
    End Sub

    Private Sub grdBlocks_ItemDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.DataGridItemEventArgs) Handles grdBlocks.ItemDataBound
        ' populate template column values here
        Try
            ' set up the checkbox and drop-down columns
            Dim drv As DataRowView = CType(e.Item.DataItem, DataRowView)
            Dim bViewSubmission As Boolean = CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean)

            If Not drv Is Nothing Then
                Dim lblArchiveLocation As Label = Nothing
                Dim cbSelect As CheckBox = Nothing

                If e.Item.ItemType = ListItemType.EditItem Then
                ElseIf e.Item.ItemType = ListItemType.Item _
                OrElse e.Item.ItemType = ListItemType.AlternatingItem _
                OrElse e.Item.ItemType = ListItemType.SelectedItem Then
                    ' populate display mode controls
                    lblArchiveLocation = CType(e.Item.FindControl("lblArchiveLocationDisplay"), Label)
                    cbSelect = CType(e.Item.FindControl("cbSelected"), CheckBox)
                End If

                If Not lblArchiveLocation Is Nothing Then
                    If Not IsDBNull(drv("ArchiveLocation")) Then
                        lblArchiveLocation.Text = GetListType(drv("ArchiveLocation"), LOOKUP_ARCHIVE_LOCATION)
                    Else
                        lblArchiveLocation.Text = ""
                    End If
                End If

                If Not cbSelect Is Nothing Then
                    If Not IsDBNull(drv("Selected")) Then
                        cbSelect.Checked = drv("Selected")
                    End If

                    If bViewSubmission Then
                        cbSelect.Enabled = False
                    End If
                End If
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to bind template columns in the archive blocks grid", ex)
        End Try
    End Sub

    Private Sub VLAHeader1_HomeClick(ByVal sender As Object, ByVal e As HistopathologySystem.HomeLinkEventArgs) Handles VLAHeader1.HomeClick
        If CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean) = False Then
            Dim sMessage As System.Text.StringBuilder = New System.Text.StringBuilder

            sMessage.Append("You are currently entering block archive information. Any data that you have entered since you last saved will be lost. Are you sure you wish to exit?")
            Page.RegisterStartupScript("navigate", PromptBeforeNavigateScript(sMessage.ToString(), "Home.aspx"))
            e.bNavigateHome = False
        Else
            Response.Redirect("Home.aspx")
        End If
    End Sub

#End Region

#Region "Private Functions"

    Private Sub SetEnterKeyPress()
        SetFocus(txtPage)
        SetDropDownControlOnEnter(ddlHistologyRefList, ddlBlockRefList.ClientID)
        SetDropDownControlOnEnter(ddlBlockRefList, btnFilter.ClientID)
        SetDropDownControlOnEnter(ddlArchiveLocation, ctlArchivedDate.FirstClientID)
        ctlArchivedDate.SetControlOnEnter(txtComment.ClientID)
        SetTextboxDefaultButton(txtPage, btnGoToPage)
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

    Private Sub EnableControls(ByVal bEnabled)
        ddlArchiveLocation.Enabled = bEnabled
        ctlArchivedDate.Enabled = bEnabled
        txtComment.Enabled = bEnabled

        If CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean) = True Then
            cbSelectAll.Enabled = False
            btnBack.Enabled = True
            btnUpdate.Enabled = False
            btnSave.Enabled = False
        End If
    End Sub

    Private Sub InitialiseControls()
        EnableControls(True)
        ctlArchivedDate.DateField = ""
        SelectItemInDropDownList(ddlArchiveLocation, "")
        txtComment.Text = ""
        grdBlocks.SelectedIndex = -1
    End Sub

    Private Function ValidatePageNumberFields() As Boolean
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

    Private Sub UpdateSessionWithArchiveData()
        Try
            Dim dtArchiveData As DataTable = CType(Session.Item(SessionVars.SV_BlockSummaryTable), DataTable)
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtBatchBlocks As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE)
            Dim drArchiveRow As DataRow
            Dim foundRow As DataRow()
            Dim sFilter As String


            'Copy the data from the datatable bound grid to the Session dataset
            If Not dtBatchBlocks Is Nothing Then
                For Each drArchiveRow In dtArchiveData.Rows

                    sFilter = "ID=" & drArchiveRow("BlockID")
                    foundRow = dtBatchBlocks.Select(sFilter)

                    If Not foundRow Is Nothing And foundRow.Length > 0 Then
                        foundRow(0)("ArchiveLocation") = drArchiveRow("ArchiveLocation").ToString()
                        foundRow(0)("ArchivedDate") = drArchiveRow("ArchivedDate")
                        foundRow(0)("ArchiveComment") = drArchiveRow("ArchiveComment").ToString()
                    End If
                Next
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve Save the archive data information.", ex)
        End Try
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

    Private Function ValidateMandatoryFields() As Boolean
        Try
            rfvArchiveLocation.Validate()

            If Not rfvArchiveLocation.IsValid Or _
                ctlDIV.InnerHtml = "<p><font color=""Red"">Not all mandatory fields have been completed, hover the mouse pointer over the red stars for details.</font></p>" Then
                Return False
            End If

            Return True
        Catch ex As Exception
            clsAppError.DisplayError("Failed to validate Mandatory fields.", ex)
        End Try

    End Function


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
