Partial Class SearchPMDates
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents ctlFromDate As CalendarDate
    Protected WithEvents ctlToDate As CalendarDate
    Protected WithEvents SearchResultsPager As DataGridPager

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
        VLAHeader1.PageTitle = "Search PM Dates"
        SetCalendarDateHandler(Me.Page)
        SearchResultsPager.SetGrid(grdSearchResults)
        SetEnterPresses()
        VLAHeader1.SubmissioNoVisible() = False

        If Not IsPostBack Then
            grdSearchResults.Visible = False
            SearchResultsPager.Visible = False
        End If
    End Sub

    Private Sub SetEnterPresses()
        ctlFromDate.SetCalendarFocus()
        ctlFromDate.SetControlOnEnter(ctlToDate.FirstClientID)
        ctlToDate.SetControlOnEnter(btnSearch.ClientID)
    End Sub

    Private Sub btnSearch_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSearch.Click
        If Not IsDateRangeValid(ctlFromDate, ctlToDate, "PM Date Range") Then
            ctlDiv.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
            grdSearchResults.Visible = False
            SearchResultsPager.Visible = False
            hlbExcel.Visible = False
            Exit Sub
        End If

        ctlDiv.InnerHtml = ""
        grdSearchResults.Visible = True
        SearchResultsPager.Visible = True
        hlbExcel.Visible = True

        FillSearchGrid()
    End Sub

    Private Sub FillSearchGrid()
        Try
            Dim dtBatches As DataTable
            Dim dvBatchesView As DataView

            'Initialise the data table
            Dim objAnimal As New HistopathologyLib.clsAnimal()

            If Not objAnimal.GetSearchPMDates(dtBatches, _
                                              ctlFromDate.DateField, _
                                              ctlToDate.DateField) Then
                Throw New Exception("Animal.GetSearchPMDates returned False")
            End If

            Session(SessionVars.SV_SearchBatchDetailsTable) = dtBatches
            dvBatchesView = dtBatches.DefaultView
            Session(SessionVars.SV_SearchBatchDetailsView) = dvBatchesView

            dtBatches.TableName = "BatchSearchResults"
            Session.Item(SessionVars.SV_ExcelExport) = dtBatches
            Session.Item(SessionVars.SV_ExcelExportView) = dvBatchesView

            ' initialise the grid
            grdSearchResults.DataSource = dtBatches
            grdSearchResults.DataKeyField = "SenderRef"
            grdSearchResults.CurrentPageIndex = 0
            grdSearchResults.SelectedIndex = -1
            grdSearchResults.EditItemIndex = -1
            grdSearchResults.DataBind()

            '' initialise the pager
            SearchResultsPager.DataTableSessionID = SessionVars.SV_SearchBatchDetailsTable
            SearchResultsPager.DataViewSessionID = SessionVars.SV_SearchBatchDetailsView
            SearchResultsPager.PageLinkCount = 10
            SearchResultsPager.AllowAddNew = False
            SearchResultsPager.AllowEdit = False
            SearchResultsPager.AllowDelete = False
            SearchResultsPager.Refresh()

        Catch ex As Exception
            clsAppError.DisplayError("Error initialising the PM Date Search page", ex)
        End Try
    End Sub
End Class
