Partial Class ReceivedBatches
    Inherits System.Web.UI.Page
    Protected WithEvents Datagrid1 As System.Web.UI.WebControls.DataGrid
    Protected WithEvents BatchesPager As DataGridPager
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
        VLAHeader1.PageTitle = "Submissions Received"
        CheckPermissions()
        BatchesPager.SetGrid(grdBatches)
        SetFocus(txtSubmissionID)
        SetTextboxDefaultButton(txtSubmissionID, btnGo)
        VLAHeader1.SubmissioNoVisible() = False

        If Not IsPostBack Then
            InitialiseBatchesGrid()
        End If
    End Sub

#Region "Grid Handling"

    Private Sub InitialiseBatchesGrid()
        Try
            Dim dtBatches As DataTable
            Dim dtBatchesView As DataView
            Dim iPageNumber As Integer = 0
            Dim sSort As String = ""

            sSort = CStr(Session.Item(SessionVars.SV_Sort))
            iPageNumber = CInt(Session.Item(SessionVars.SV_SearchCriteria))

            'Initialise the data table
            Dim objBatch As New HistopathologyLib.clsBatch()

            If Not objBatch.GetBatchesToBeBlocked(dtBatches) Then
                Throw New Exception("Batch.GetBatchesToBeBlocked returned False.")
            End If

            Session(SessionVars.SV_BatchesTable) = dtBatches
            dtBatchesView = dtBatches.DefaultView

            If sSort Is Nothing Or sSort = "" Then
                dtBatchesView.Sort = "ID DESC"
            Else
                dtBatchesView.Sort = sSort
            End If

            Session(SessionVars.SV_BatchesView) = dtBatchesView

            ' initialise the grid
            grdBatches.DataSource = dtBatches
            grdBatches.DataKeyField = "ID"
            grdBatches.CurrentPageIndex = iPageNumber
            grdBatches.SelectedIndex = -1
            grdBatches.EditItemIndex = -1
            grdBatches.DataBind()

            ' initialise the pager
            BatchesPager.DataTableSessionID = SessionVars.SV_BatchesTable
            BatchesPager.DataViewSessionID = SessionVars.SV_BatchesView
            BatchesPager.PageLinkCount = 10
            BatchesPager.AllowAddNew = False
            BatchesPager.AllowEdit = False
            BatchesPager.AllowDelete = False
            BatchesPager.Refresh()

        Catch ex As Exception
            clsAppError.DisplayError("Error initialising the Batches grid.", ex)
        End Try
    End Sub

#End Region

#Region "Event Handlers"

    Private Sub grdBatches_SortCommand(ByVal sender As System.Object, ByVal e As System.Web.UI.WebControls.DataGridSortCommandEventArgs) Handles grdBatches.SortCommand
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

    Private Sub grdBatches_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles grdBatches.SelectedIndexChanged
        If grdBatches.SelectedIndex >= 0 Then
            Dim iBatchID As Integer = grdBatches.DataKeys(grdBatches.SelectedIndex)
            Dim dtData As DataTable = Session.Item(SessionVars.SV_BatchesTable)
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

            Session.Item(SessionVars.SV_AssignBlocks) = True
            Session.Item(SessionVars.SV_SearchCriteria) = grdBatches.CurrentPageIndex
            Session.Item(SessionVars.SV_Cassetted) = True
            Session.Item(SessionVars.SV_BatchID) = iBatchID
            GetCommonBatchDetailsFromDatabase(iBatchID, Session)
            GetBatchBlockDetailsFromDatabase(iBatchID, Session)
            Response.Redirect("BatchBlocks.aspx")
        End If
    End Sub

    Private Sub VLAHeader1_HomeClick(ByVal sender As Object, ByVal e As HistopathologySystem.HomeLinkEventArgs) Handles VLAHeader1.HomeClick
        Response.Redirect("Home.aspx")
    End Sub

    Private Sub grdBatches_ItemDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.DataGridItemEventArgs) Handles grdBatches.ItemDataBound
        'populate template columns here
        Try
            Dim cbAllTissuesAssigned As CheckBox = Nothing
            Dim drv As DataRowView = CType(e.Item.DataItem, DataRowView)

            If e.Item.ItemType = ListItemType.EditItem Then
            ElseIf e.Item.ItemType = ListItemType.Item _
            OrElse e.Item.ItemType = ListItemType.AlternatingItem _
            OrElse e.Item.ItemType = ListItemType.SelectedItem Then
                cbAllTissuesAssigned = CType(e.Item.FindControl("cbAllTissuesAssigned"), CheckBox)
            End If

            If Not cbAllTissuesAssigned Is Nothing Then
                If Not IsDBNull(drv("AllTissuesAssigned")) Then
                    cbAllTissuesAssigned.Checked = drv("AllTissuesAssigned")
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to bind the species column to the grid.", ex)
        End Try
    End Sub

    Private Sub btnHome_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnHome.Click
        Response.Redirect("Home.aspx")
    End Sub

    Private Sub btnGo_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnGo.Click
        Dim bRedirect As Boolean = False
        ctlDiv.InnerHtml = ""
        Try
            If ValidateMandatoryFields() Then
                Dim objBatch As New HistopathologyLib.clsBatch
                Dim iCount As Integer
                Dim iCount2 As Integer
                Dim iBatchType As Integer
                Dim iBatchID As Integer
                Dim iIsCassetted As Integer

                iBatchID = Convert.ToInt32(txtSubmissionID.Text)
                'Check if the batch ID entered matches one with the required status in the database.
                'If it does load the details and move to the required page, otherwise display an 
                'error.
                If Not objBatch.CheckBatchExists(iBatchID, _
                                                 HistopathologyLib.clsBatch.STATUS_INPROGRESS, _
                                                 iCount, _
                                                 iBatchType, _
                                                 iIsCassetted, _
                                                 0) Then
                    Throw New Exception("Batch.CheckBatchExists returned false.")
                End If

                If Not objBatch.CheckBatchExists(iBatchID, _
                                                 HistopathologyLib.clsBatch.STATUS_RECEIVED, _
                                                 iCount2, _
                                                 iBatchType, _
                                                 iIsCassetted, _
                                                 0) Then
                    Throw New Exception("Batch.CheckBatchExists returned false.")
                End If

                If iCount > 0 Or iCount2 > 0 Then
                    If iBatchType = 1 Then
                        Session.Item(SessionVars.SV_SubmissionType) = SUBMISSION_NONTSE
                    Else
                        Session.Item(SessionVars.SV_SubmissionType) = SUBMISSION_TSE
                    End If

                    Session.Item(SessionVars.SV_AssignBlocks) = True
                    Session.Item(SessionVars.SV_Cassetted) = True
                    Session.Item(SessionVars.SV_BatchID) = iBatchID
                    GetCommonBatchDetailsFromDatabase(iBatchID, Session)
                    GetBatchBlockDetailsFromDatabase(iBatchID, Session)
                    bRedirect = True
                Else
                    ctlDiv.InnerHtml = "<p><font color=""Red"">No Submissions with the required status were found.</font></p>"
                End If
            Else
                Exit Sub
            End If

            Session.Item(SessionVars.SV_SearchCriteria) = grdBatches.CurrentPageIndex
        Catch ex As Exception
            clsAppError.DisplayError("Error looking up the Submission ID on the Submissions Received page.", ex)
        End Try

        If bRedirect Then
            Response.Redirect("BatchBlocks.aspx")
        End If

    End Sub

#End Region

#Region "Private functions"

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

    Private Function ValidateMandatoryFields() As Boolean
        Try
            rfvSubmissionID.Validate()
            revSubmissionID.Validate()

            If Not rfvSubmissionID.IsValid Or _
            Not revSubmissionID.IsValid() Then
                ctlDiv.InnerHtml = "<p><font color=""Red"">Not all mandatory fields have been completed, hover the mouse pointer over the red stars for details.</font></p>"
                Return False
            End If

            Return True
        Catch ex As Exception
            clsAppError.DisplayError("Failed to validate Mandatory fields.", ex)
        End Try

    End Function

#End Region
End Class
