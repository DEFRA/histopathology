Partial Class BatchesForDispatch
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents BatchesPager As DataGridPager
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
        VLAHeader1.PageTitle = "Submissions Awaiting Quality Data"
        CheckPermissions()
        BatchesPager.SetGrid(grdBatchesForDispatch)
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
            Dim dvBatchesView As DataView
            Dim iPageNumber As Integer = 0
            Dim sSort As String = ""

            iPageNumber = CInt(Session.Item(SessionVars.SV_SearchCriteria))
            sSort = CStr(Session.Item(SessionVars.SV_Sort))

            'Initialise the data table
            Dim objBatch As New HistopathologyLib.clsBatch

            If Not objBatch.GetBatchesForDispatch(dtBatches) Then
                Throw New Exception("Batch.GetBatchesForDispatch returned false.")
            End If

            Session(SessionVars.SV_BatchesWaitingDispatchTable) = dtBatches
            dvBatchesView = dtBatches.DefaultView

            If sSort Is Nothing Or sSort = "" Then
                dvBatchesView.Sort = "ID DESC"
            Else
                dvBatchesView.Sort = sSort
            End If

            Session(SessionVars.SV_BatchesWaitingDispatchView) = dvBatchesView

            ' initialise the grid
            grdBatchesForDispatch.DataSource = dtBatches
            grdBatchesForDispatch.DataKeyField = "ID"
            grdBatchesForDispatch.CurrentPageIndex = iPageNumber
            grdBatchesForDispatch.SelectedIndex = -1
            grdBatchesForDispatch.EditItemIndex = -1
            grdBatchesForDispatch.DataBind()

            ' initialise the pager
            BatchesPager.DataTableSessionID = SessionVars.SV_BatchesWaitingDispatchTable
            BatchesPager.DataViewSessionID = SessionVars.SV_BatchesWaitingDispatchView
            BatchesPager.PageLinkCount = 10
            BatchesPager.AllowAddNew = False
            BatchesPager.AllowEdit = False
            BatchesPager.AllowDelete = False
            BatchesPager.Refresh()

        Catch ex As Exception
            clsAppError.DisplayError("Error initialising the Batches for dispatch page", ex)
        End Try
    End Sub

#End Region

#Region "Event Handlers"

    Private Sub grdBatchesForDispatch_SortCommand(ByVal sender As System.Object, ByVal e As System.Web.UI.WebControls.DataGridSortCommandEventArgs) Handles grdBatchesForDispatch.SortCommand
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

    Private Sub grdBatchesForDispatch_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles grdBatchesForDispatch.SelectedIndexChanged
        If grdBatchesForDispatch.SelectedIndex >= 0 Then
            Dim iBatchID As Integer = grdBatchesForDispatch.DataKeys(grdBatchesForDispatch.SelectedIndex)
            Dim dtData As DataTable = Session.Item(SessionVars.SV_BatchesWaitingDispatchTable)
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
            GetBatchBlockDetailsFromDatabase(iBatchID, Session)
            Session.Item(SessionVars.SV_BatchID) = iBatchID
            Session.Item(SessionVars.SV_SearchCriteria) = grdBatchesForDispatch.CurrentPageIndex
            Session.Item(SessionVars.SV_RedirectCancelPage) = "BatchesForDispatch.aspx"
            Session.Item(SessionVars.SV_RedirectPage) = "BatchesForDispatch.aspx"
            Response.Redirect("QualityData.aspx")
        End If
    End Sub

    Private Sub VLAHeader1_HomeClick(ByVal sender As Object, ByVal e As HistopathologySystem.HomeLinkEventArgs) Handles VLAHeader1.HomeClick
        Response.Redirect("Home.aspx")
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
                Dim iCassetted As Integer

                iBatchID = Convert.ToInt32(txtSubmissionID.Text)
                'Check if the batch ID entered matches one with the required status in the database.
                'If it does load the details and move to the required page, otherwise display an 
                'error.
                If Not objBatch.CheckBatchExists(iBatchID, _
                                                 HistopathologyLib.clsBatch.STATUS_INPROGRESS, _
                                                 iCount, _
                                                 iBatchType, _
                                                 iCassetted) Then
                    Throw New Exception("Batch.CheckBatchExists returned false.")
                End If

                If Not objBatch.CheckBatchExists(iBatchID, _
                                                HistopathologyLib.clsBatch.STATUS_RECEIVED, _
                                                iCount2, _
                                                iBatchType, _
                                                iCassetted) Then
                    Throw New Exception("Batch.CheckBatchExists returned false.")
                End If

                If iCount > 0 Or (iCount2 > 0 And iCassetted = 1) Then
                    If iBatchType = 1 Then
                        Session.Item(SessionVars.SV_SubmissionType) = SUBMISSION_NONTSE
                    Else
                        Session.Item(SessionVars.SV_SubmissionType) = SUBMISSION_TSE
                    End If

                    Session.Item(SessionVars.SV_SearchCriteria) = grdBatchesForDispatch.CurrentPageIndex
                    Session.Item(SessionVars.SV_Cassetted) = True
                    Session.Item(SessionVars.SV_BatchID) = iBatchID
                    GetCommonBatchDetailsFromDatabase(iBatchID, Session)
                    GetBatchBlockDetailsFromDatabase(iBatchID, Session)
                    Session.Item(SessionVars.SV_RedirectCancelPage) = "BatchesForDispatch.aspx"
                    Session.Item(SessionVars.SV_RedirectPage) = "BatchesForDispatch.aspx"
                    bRedirect = True
                Else
                    ctlDiv.InnerHtml = "<p><font color=""Red"">No Batches with the required status were found.</font></p>"
                End If
            Else
                Exit Sub
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Error looking up the Submission ID on the Submissions for dispatch page.", ex)
        End Try

        If bRedirect Then
            Response.Redirect("QualityData.aspx")
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
