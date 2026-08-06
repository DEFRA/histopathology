Partial Class SearchTest
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents StartDate As CalendarDate
    Protected WithEvents EndDate As CalendarDate

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
        'Put user code to initialize the page here
        VLAHeader1.PageTitle = "Search Test Totals"
        CheckPermissions()
        VLAHeader1.SubmissioNoVisible() = False
        'SetCalendarDateHandler(Me.Page)

        If Not IsPostBack Then
            hlbExcel.Visible = False
            LoadCheckBoxLists()
            LoadLookupList()
            Dim SelectedItemArray As New ArrayList()
            Session.Item(SessionVars.SV_SelectedHistologyArray) = SelectedItemArray
        End If

        SetEnterPresses()
        AttachEventHanders()
    End Sub

    Sub SetEnterPresses()
        StartDate.SetCalendarFocus()
        StartDate.SetControlOnEnter(EndDate.FirstClientID)
        EndDate.SetDropDownOnEnter(ddlProjectList.ClientID)
        SetDropDownControlOnEnter(ddlProjectList, ddlProjectList.ClientID)
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

    Private Sub LoadLookupList()
        Try
            Dim objDataTable As DataTable
            Dim objLookup As New HistopathologyLib.LookupData()

            objDataTable = objLookup.GetLookupData(LOOKUP_PROJECTS, False)

            If Not objDataTable Is Nothing Then
                ddlProjectList.DataSource = objDataTable
                ddlProjectList.DataValueField = "Description"
                ddlProjectList.DataTextField = "Description"
                ddlProjectList.DataBind()
                Common.AddItemToDropDownList(ddlProjectList)
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve Project list lists.", ex)
        End Try

    End Sub
    Private Sub LoadCheckBoxLists()
        Dim objDataTable As DataTable
        Dim objLookup As New HistopathologyLib.LookupData()
        Dim dsSubmission As DataSet

        Try
            objDataTable = objLookup.GetHistologyLookupData()

            If Not objDataTable Is Nothing Then
                chkblHistology.DataSource = objDataTable
                chkblHistology.DataValueField = "Code"
                chkblHistology.DataTextField = "Description"
                chkblHistology.DataBind()
            End If

            'Check the submission type and load the correct list
            If CType(Session.Item(SessionVars.SV_SubmissionType), Integer) = SUBMISSION_NONTSE Then
                objDataTable = objLookup.GetLookupData(LOOKUP_NONTSE_ANTIBODIES)
            Else
                objDataTable = objLookup.GetLookupData(LOOKUP_TSE_ANTIBODIES)
            End If

            'Session(SessionVars.SV_SelectedItemsListArray) = objDataTable

            If Not objDataTable Is Nothing Then
                chkblAntibodies.DataSource = objDataTable
                chkblAntibodies.DataValueField = "Code"
                chkblAntibodies.DataTextField = "Description"
                chkblAntibodies.DataBind()
                chkblAntibodies.Enabled = True
                'Add "Other" to the chkBoxList
                Dim li As New ListItem()
                li.Text = "Other"
                li.Value = "Other"
                chkblAntibodies.Items.Add(li)
            End If

            objDataTable = objLookup.GetLookupData(LOOKUP_SPECIAL_STAIN)

            If Not objDataTable Is Nothing Then
                chkblSpecialStain.DataSource = objDataTable
                chkblSpecialStain.DataValueField = "Code"
                chkblSpecialStain.DataTextField = "Description"
                chkblSpecialStain.DataBind()
                chkblSpecialStain.Enabled = True
                Dim li As New ListItem()
                li.Text = "Other"
                li.Value = "Other"
                chkblSpecialStain.Items.Add(li)
            End If

            HideOptions()

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve 'Histology and Antibodies' lists.", ex)
        End Try
    End Sub

    Private Sub grdResults_SortCommand(ByVal source As Object, ByVal e As System.Web.UI.WebControls.DataGridSortCommandEventArgs) Handles grdResults.SortCommand
        Try
            Dim dvData As DataView = CType(Session.Item(SessionVars.SV_ExcelExportView), DataView)

            If Not dvData Is Nothing Then
                Dim sNewSort = e.SortExpression
                Dim sNewSortAsc = sNewSort & " ASC"
                Dim sNewSortDesc = sNewSort & " DESC"

                If dvData.Sort = sNewSort Or dvData.Sort = sNewSortAsc Then
                    dvData.Sort = sNewSortDesc
                ElseIf dvData.Sort = sNewSortDesc Then
                    dvData.Sort = sNewSortAsc
                Else
                    dvData.Sort = sNewSort
                End If

                grdResults.DataSource = dvData
                grdResults.DataBind()

            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to sort the outputs grid.", ex)
        End Try
    End Sub

    Private Sub ProcessHistologyData(ByRef dtFormattedTable As DataTable, ByVal dtPremiumCharges As DataTable, ByVal sHistologyTests As String)
        Dim drPremiumRow As DataRow
        Dim drFindRow As DataRow
        Dim drNewRow As DataRow
        Dim drProjectRow As DataRow
        Dim dtHistologyData As New DataTable
        Dim objBatch As New HistopathologyLib.clsBatch

        For Each drPremiumRow In dtPremiumCharges.Rows
            If Not objBatch.CountHistologysTestItems(CStr(ddlProjectList.SelectedItem.Text), _
                                        StartDate.DateField, _
                                        EndDate.DateField, _
                                        CType(Session.Item(SessionVars.SV_SubmissionType), Integer), _
                                        sHistologyTests, _
                                        drPremiumRow("Description").ToString(), _
                                        dtHistologyData) Then
                Throw New Exception("Batch.CountHistologysTestItems returned False")
            End If

            For Each drProjectRow In dtHistologyData.Rows
                drFindRow = dtFormattedTable.Rows.Find(drProjectRow("Description").ToString())

                If drFindRow Is Nothing Then
                    drNewRow = dtFormattedTable.NewRow()
                    drNewRow("Description") = drProjectRow("Description")
                    drNewRow("TC 0008") = 0
                    drNewRow("TC 1401") = 0
                    drNewRow("TC 1402") = 0
                    drNewRow("TC 1404") = 0
                    drNewRow("TC 1407") = 0
                    drNewRow("TC 1409") = 0
                    drNewRow("TC 1489") = 0
                    drNewRow("TC 1520") = 0
                    drNewRow("Project Totals") = 0

                    drNewRow(drPremiumRow("Description").ToString()) = drProjectRow(drPremiumRow("Description").ToString())
                    drNewRow("Project Totals") = drProjectRow(drPremiumRow("Description").ToString())

                    dtFormattedTable.Rows.Add(drNewRow)
                Else
                    drFindRow(drPremiumRow("Description").ToString()) += drProjectRow(drPremiumRow("Description").ToString())
                    drFindRow("Project Totals") += drProjectRow(drPremiumRow("Description").ToString())
                End If
            Next
        Next
    End Sub

    Private Sub ProcessAntibodiesData(ByRef dtFormattedTable As DataTable, ByVal dtPremiumCharges As DataTable, ByVal sAntibodyTests As String)
        Dim drPremiumRow As DataRow
        Dim drFindRow As DataRow
        Dim drNewRow As DataRow
        Dim drProjectRow As DataRow
        Dim dtAntibodyData As New DataTable
        Dim objBatch As New HistopathologyLib.clsBatch

        For Each drPremiumRow In dtPremiumCharges.Rows
            If Not objBatch.CountAntibodesTestItems(CStr(ddlProjectList.SelectedItem.Text), _
                                        StartDate.DateField, _
                                        EndDate.DateField, _
                                        CType(Session.Item(SessionVars.SV_SubmissionType), Integer), _
                                        sAntibodyTests, _
                                        drPremiumRow("Description").ToString(), _
                                        dtAntibodyData) Then
                Throw New Exception("Batch.CountAntibodesTestItems returned False")
            End If

            For Each drProjectRow In dtAntibodyData.Rows
                drFindRow = dtFormattedTable.Rows.Find(drProjectRow("Description").ToString())

                If drFindRow Is Nothing Then
                    drNewRow = dtFormattedTable.NewRow()
                    drNewRow("Description") = drProjectRow("Description")
                    drNewRow("TC 0008") = 0
                    drNewRow("TC 1401") = 0
                    drNewRow("TC 1402") = 0
                    drNewRow("TC 1404") = 0
                    drNewRow("TC 1407") = 0
                    drNewRow("TC 1409") = 0
                    drNewRow("TC 1489") = 0
                    drNewRow("TC 1520") = 0
                    drNewRow("Project Totals") = 0

                    drNewRow(drPremiumRow("Description").ToString()) = drProjectRow(drPremiumRow("Description").ToString())
                    drNewRow("Project Totals") = drProjectRow(drPremiumRow("Description").ToString())

                    dtFormattedTable.Rows.Add(drNewRow)
                Else
                    drFindRow(drPremiumRow("Description").ToString()) += drProjectRow(drPremiumRow("Description").ToString())
                    drFindRow("Project Totals") += drProjectRow(drPremiumRow("Description").ToString())
                End If
            Next
        Next
    End Sub

    Private Sub ProcessStainData(ByRef dtFormattedTable As DataTable, ByVal dtPremiumCharges As DataTable, ByVal sStainTests As String)
        Dim drPremiumRow As DataRow
        Dim drFindRow As DataRow
        Dim drNewRow As DataRow
        Dim drProjectRow As DataRow
        Dim dtStainData As New DataTable
        Dim objBatch As New HistopathologyLib.clsBatch

        For Each drPremiumRow In dtPremiumCharges.Rows
            If Not objBatch.CountStainTestItems(CStr(ddlProjectList.SelectedItem.Text), _
                                        StartDate.DateField, _
                                        EndDate.DateField, _
                                        CType(Session.Item(SessionVars.SV_SubmissionType), Integer), _
                                        sStainTests, _
                                        drPremiumRow("Description").ToString(), _
                                        dtStainData) Then
                Throw New Exception("Batch.CountStainTestItems returned False")
            End If

            For Each drProjectRow In dtStainData.Rows
                drFindRow = dtFormattedTable.Rows.Find(drProjectRow("Description").ToString())

                If drFindRow Is Nothing Then
                    drNewRow = dtFormattedTable.NewRow()
                    drNewRow("Description") = drProjectRow("Description")
                    drNewRow("TC 0008") = 0
                    drNewRow("TC 1401") = 0
                    drNewRow("TC 1402") = 0
                    drNewRow("TC 1404") = 0
                    drNewRow("TC 1407") = 0
                    drNewRow("TC 1409") = 0
                    drNewRow("TC 1489") = 0
                    drNewRow("TC 1520") = 0
                    drNewRow("Project Totals") = 0

                    drNewRow(drPremiumRow("Description").ToString()) = drProjectRow(drPremiumRow("Description").ToString())
                    drNewRow("Project Totals") = drProjectRow(drPremiumRow("Description").ToString())

                    dtFormattedTable.Rows.Add(drNewRow)
                Else
                    drFindRow(drPremiumRow("Description").ToString()) += drProjectRow(drPremiumRow("Description").ToString())
                    drFindRow("Project Totals") += drProjectRow(drPremiumRow("Description").ToString())
                End If
            Next
        Next
    End Sub

    Private Sub btnCount_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCount.Click
        Try

            ctlDiv.InnerHtml = ""
            If Not IsDateRangeValid(StartDate, EndDate, "Submitted Date") Then
                ctlDiv.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
                grdResults.Visible = False
                hlbExcel.Visible = False
                Exit Sub
            End If

            Dim dtFormatted As New DataTable
            dtFormatted.Columns.Add("Description", System.Type.GetType("System.String"))
            dtFormatted.Columns.Add("TC 0008", System.Type.GetType("System.Int32"))
            dtFormatted.Columns.Add("TC 1401", System.Type.GetType("System.Int32"))
            dtFormatted.Columns.Add("TC 1402", System.Type.GetType("System.Int32"))
            dtFormatted.Columns.Add("TC 1404", System.Type.GetType("System.Int32"))
            dtFormatted.Columns.Add("TC 1407", System.Type.GetType("System.Int32"))
            dtFormatted.Columns.Add("TC 1409", System.Type.GetType("System.Int32"))
            dtFormatted.Columns.Add("TC 1489", System.Type.GetType("System.Int32"))
            dtFormatted.Columns.Add("TC 1520", System.Type.GetType("System.Int32"))
            dtFormatted.Columns.Add("Project Totals", System.Type.GetType("System.Int32"))

            dtFormatted.PrimaryKey = New DataColumn() {dtFormatted.Columns("Description")}
            dtFormatted.TableName = "Quality Totals"

            Dim sStainTests As String = GetSelectedTests(chkblSpecialStain)
            Dim sHistologyTests As String = GetSelectedTests(chkblHistology)
            Dim sAntibodiesTests As String = GetSelectedTests(chkblAntibodies)
            Dim dtPremiumCharges As DataTable
            Dim objLookup As New HistopathologyLib.LookupData
            Dim drNewRow As DataRow
            Dim drRow As DataRow
            Dim iColumns As Integer
            Dim iCount As Integer
            Dim iCount2 As Integer

            dtPremiumCharges = objLookup.GetLookupData(LOOKUP_PREMIUM_CHARGES)

            If sHistologyTests.Length > 0 Then
                ProcessHistologyData(dtFormatted, dtPremiumCharges, sHistologyTests)
            End If

            If sStainTests.Length > 0 Then
                ProcessStainData(dtFormatted, dtPremiumCharges, sStainTests)
            End If

            If sAntibodiesTests.Length > 0 Then
                ProcessAntibodiesData(dtFormatted, dtPremiumCharges, sAntibodiesTests)
            End If

          

            Dim dv As DataView = dtFormatted.DefaultView
            Dim drv As System.Data.DataRowView = dv.AddNew

            'work the totals out
            Dim iProjectTotal As Integer = 0
            Dim iTCCodeTotal As Integer = 0

            'Project totals
            For iCount = 0 To dtFormatted.Rows.Count - 1
                For iCount2 = 1 To dtFormatted.Columns.Count - 2
                    iProjectTotal += CType(dtFormatted.Rows(iCount).Item(iCount2), Integer)
                Next
                dtFormatted.Rows(iCount)("Project Totals") = iProjectTotal
                iProjectTotal = 0
            Next

            For iCount2 = 1 To dtFormatted.Columns.Count - 1
                For iCount = 0 To dtFormatted.Rows.Count - 1
                    iTCCodeTotal += CType(dtFormatted.Rows(iCount).Item(iCount2), Integer)
                Next

                drv(iCount2) = iTCCodeTotal
                iTCCodeTotal = 0
            Next

            drv(0) = "Quality Totals"

            dv.Sort = "Description ASC"
            grdResults.DataSource = dv
            grdResults.DataBind()

            hlbExcel.Visible = True
            Session.Item(SessionVars.SV_ExcelExport) = dtFormatted
            Session.Item(SessionVars.SV_ExcelExportView) = dtFormatted.DefaultView

        Catch ex As Exception
            clsAppError.DisplayError("Failed to analyse test records.", ex)
        End Try
    End Sub


    Private Sub chkblHistology_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles chkblHistology.SelectedIndexChanged
        Dim i As Integer
        Dim sItemSelected As String
        Dim li As ListItem
        Dim aArray As ArrayList = CType(Session(SessionVars.SV_SelectedHistologyArray), ArrayList)
        li = GetCheckListSelectedItem(sItemSelected, aArray)

        If Not li Is Nothing Then
            If li.Text = "Special Stain" Then
                For i = 0 To chkblSpecialStain.Items.Count - 1
                    chkblSpecialStain.Items(i).Selected = li.Selected
                Next
            End If
            If li.Text = "IHC - PrP" Then
                For i = 0 To chkblAntibodies.Items.Count - 1
                    chkblAntibodies.Items(i).Selected = li.Selected
                Next
            End If
            If li.Text = "IHC - Other" Then
                For i = 0 To chkblAntibodies.Items.Count - 1
                    chkblAntibodies.Items(i).Selected = li.Selected
                Next
            End If
        End If
    End Sub

    Private Function GetCheckListSelectedItem(ByRef sText As String, ByVal aArray As ArrayList) As ListItem
        'This function is used to get the item in the ComboboxList that has just been selected.
        'Using comboboxList.selectedItem always returns the lowest indexed selected item rather
        'than the item just selected.

        Dim li As ListItem
        For Each li In chkblHistology.Items
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

    Private Function GetSelectedTests(ByVal chkList As CheckBoxList) As String
        Dim iCount As Integer
        Dim sSelectedTests As String = ""
        Dim iCountSelected As Integer

        iCountSelected = 0
        For iCount = chkList.Items.Count - 1 To 0 Step -1
            If chkList.Items(iCount).Selected = True Then

                If iCountSelected = 0 Then
                    sSelectedTests = chkList.Items(iCount).Value & "','"
                    iCountSelected += 1
                Else
                    sSelectedTests = sSelectedTests & chkList.Items(iCount).Value & "','"
                    iCountSelected += 1
                End If
            End If
        Next
        If sSelectedTests.Length > 3 Then
            sSelectedTests = sSelectedTests.Substring(0, sSelectedTests.Length - 3)
        End If

        Return sSelectedTests
    End Function

    Private Sub HideOptions()
        Dim li As ListItem
        Dim iCount As Integer = 0

        'Only display the Histology columns which are relevent to the type of submission
        If Session(SessionVars.SV_SubmissionType) = SUBMISSION_NONTSE Then
            For iCount = chkblHistology.Items.Count - 1 To 0 Step -1
                'Get rid of the IHC-Prp & H&E(BSE) options for Non TSE
                If chkblHistology.Items(iCount).Value = "4" Or chkblHistology.Items(iCount).Value = "5" Then
                    chkblHistology.Items.RemoveAt(iCount)
                End If
            Next
        Else
            For Each li In chkblHistology.Items
                'Get rid of the IHC-Other option for TSE
                If li.Value = "6" Then
                    chkblHistology.Items.Remove(li)
                    Exit For
                End If
            Next
        End If
    End Sub

    Private Sub btnOutputsMenu_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnOutputsMenu.Click
        Response.Redirect("SearchMenu.aspx")
    End Sub

    Protected Sub bntBatch_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles bntBatch.Click
        Try

            Dim objLookup As New HistopathologyLib.LookupData
            Dim dtPremiumCharges As DataTable
            Dim drNewRow As DataRow
            Dim drRow As DataRow
            Dim iColumns As Integer

            dtPremiumCharges = objLookup.GetLookupData(LOOKUP_PREMIUM_CHARGES)

            grdBatchResult.DataSource = dtPremiumCharges
            grdBatchResult.DataBind()
            AttachEventHanders()

            hlbBatchExcel.Visible = True
           

        Catch ex As Exception
            clsAppError.DisplayError("Failed to analyse submission records.", ex)
        End Try
    End Sub

    Private Sub AttachEventHanders()

        Dim iRowCount As Integer = 0

        For iRowCount = 0 To grdBatchResult.Items.Count - 1
            Dim sCode As String = grdBatchResult.Items(iRowCount).Cells(0).Text
            Dim pnlSubmissions As Panel = Nothing

            pnlSubmissions = CType(grdBatchResult.Items(iRowCount).FindControl("pnlSubmissions"), Panel)

            If Not pnlSubmissions Is Nothing Then
                If Not String.IsNullOrEmpty(sCode) Then
                    Dim objBatch As New HistopathologyLib.clsBatch
                    Dim sStainTests As String = GetSelectedTests(chkblSpecialStain)
                    Dim sHistologyTests As String = GetSelectedTests(chkblHistology)
                    Dim sAntibodiesTests As String = GetSelectedTests(chkblAntibodies)
                    Dim dtHistologyData As DataTable
                    Dim dtAntibodiesData As DataTable
                    Dim dtStainData As DataTable
                    Dim dtFormattedData As New DataTable
                    Dim drRow As DataRow
                    Dim drNewRow As DataRow
                    Dim drFindRow As DataRow

                    dtFormattedData.TableName = "Quality Submissions"
                    dtFormattedData.Columns.Add("ID")
                    dtFormattedData.PrimaryKey = New DataColumn() {dtFormattedData.Columns("ID")}

                    If Not objBatch.CountHistologysTestBatch(CStr(ddlProjectList.SelectedItem.Text), _
                               StartDate.DateField, _
                               EndDate.DateField, _
                               CType(Session.Item(SessionVars.SV_SubmissionType), Integer), _
                               sHistologyTests, _
                               sCode, _
                               dtHistologyData) Then
                        Throw New Exception("Batch.CountHistologysTestBatch returned False")
                    End If

                    For Each drRow In dtHistologyData.Rows
                        drFindRow = dtFormattedData.Rows.Find(drRow("ID"))

                        If drFindRow Is Nothing Then
                            drNewRow = dtFormattedData.NewRow()
                            drNewRow("ID") = drRow("ID")
                            dtFormattedData.Rows.Add(drNewRow)
                        End If
                    Next

                    If Not objBatch.CountAntibodesTestBatch(CStr(ddlProjectList.SelectedItem.Text), _
                               StartDate.DateField, _
                               EndDate.DateField, _
                               CType(Session.Item(SessionVars.SV_SubmissionType), Integer), _
                               sAntibodiesTests, _
                               sCode, _
                               dtAntibodiesData) Then
                        Throw New Exception("Batch.CountAntibodesTestBatch returned False")
                    End If

                    For Each drRow In dtAntibodiesData.Rows
                        drFindRow = dtFormattedData.Rows.Find(drRow("ID"))

                        If drFindRow Is Nothing Then
                            drNewRow = dtFormattedData.NewRow()
                            drNewRow("ID") = drRow("ID")
                            dtFormattedData.Rows.Add(drNewRow)
                        End If
                    Next

                    If Not objBatch.CountStainTestBatch(CStr(ddlProjectList.SelectedItem.Text), _
                               StartDate.DateField, _
                               EndDate.DateField, _
                               CType(Session.Item(SessionVars.SV_SubmissionType), Integer), _
                               sStainTests, _
                               sCode, _
                               dtStainData) Then
                        Throw New Exception("Batch.CountStainTestBatch returned False")
                    End If

                    For Each drRow In dtStainData.Rows
                        drFindRow = dtFormattedData.Rows.Find(drRow("ID"))

                        If drFindRow Is Nothing Then
                            drNewRow = dtFormattedData.NewRow()
                            drNewRow("ID") = drRow("ID")
                            dtFormattedData.Rows.Add(drNewRow)
                        End If
                    Next

                    Dim dvView As DataView = dtFormattedData.DefaultView
                    Dim iCount As Integer
                    dvView.Sort = "ID asc"

                    For iCount = 0 To dvView.Count - 1
                        Dim lnk As New LinkButton
                        lnk.ID = dvView(iCount)("ID").ToString()
                        lnk.Text = dvView(iCount)("ID").ToString()
                        lnk.CommandArgument = dvView(iCount)("ID").ToString()
                        lnk.CausesValidation = False

                        AddHandler lnk.Click, AddressOf Submission_Click
                        pnlSubmissions.Controls.Add(lnk)

                        Dim litSpacer As New LiteralControl
                        litSpacer.Text = "&nbsp;"
                        pnlSubmissions.Controls.Add(litSpacer)
                    Next

                    Session.Item(SessionVars.SV_ExcelExport) = dtFormattedData
                    Session.Item(SessionVars.SV_ExcelExportView) = dtFormattedData.DefaultView
                End If
            End If
        Next

    End Sub

    Public Sub Submission_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Try
            Dim lnkButton As LinkButton = CType(sender, LinkButton)
            Dim iBatchID As Integer = CType(lnkButton.CommandArgument, Integer)

            GetCommonBatchDetailsFromDatabase(iBatchID, Session)
            GetBatchBlockDetailsFromDatabase(iBatchID, Session)

            Session.Item(SessionVars.SV_Cassetted) = True
            Session.Item(SessionVars.SV_BatchID) = iBatchID
            Session.Item(SessionVars.SV_ViewSubmission) = True
            Session.Item(SessionVars.SV_EditingBatch) = False
            Session.Item(SessionVars.SV_RedirectCancelPage) = "SearchTest.aspx"
            Session.Item(SessionVars.SV_RedirectPage) = "SearchTest.aspx"

        Catch ex As Exception
            clsAppError.DisplayError("Failed to view quality data from search outputs.", ex)
        End Try

        Response.Redirect("QualityData.aspx")
    End Sub

End Class

