Partial Class QCNotes
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents QCNotePager As DataGridPager

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
        VLAHeader1.PageTitle = "QC Notes"
        CheckPermissions()
        QCNotePager.SetGrid(grdQCNotes)
        SetTextboxDefaultButton(txtQCNote, btnGo)
        VLAHeader1.SubmissioNoVisible() = False

        'This solves a problem with when the submission form was printed. 
        'Previously due to not so smart nav the main window would get focus which would
        'hide the popped up window. Disable the code which restores focus to the main window.
        If Page.SmartNavigation = True Then
            DontRestoreFocus(Me.Page)
        End If

        If Not IsPostBack Then
            SetFocus(txtQCNote)
            InitialiseGrid()
            EnableDisableButtons(False)
        End If

    End Sub

#Region "Grid Initialisation"
    Private Sub InitialiseGrid()
        Try
            Dim objQCNote As New HistopathologyLib.clsQCNote()
            Dim dtData As New DataTable()
            Dim dvBatchesView As DataView
            Dim iPageNumber As Integer = 0
            Dim sSort As String = ""

            iPageNumber = CInt(Session.Item(SessionVars.SV_SearchCriteria))
            sSort = CStr(Session.Item(SessionVars.SV_Sort))

            If Not objQCNote.GetBatchQCNotes(dtData) Then
                Throw New Exception("Batch.GetBatchQCNotes returned false.")
            End If

            Session(SessionVars.SV_BatchQCNotesTable) = dtData
            dvBatchesView = dtData.DefaultView
            If sSort Is Nothing Or sSort = "" Then
                dvBatchesView.Sort = "QCNoteRef DESC"
            Else
                dvBatchesView.Sort = sSort
            End If
            Session(SessionVars.SV_BatchQCNotesView) = dvBatchesView

            ' initialise the grid
            grdQCNotes.DataSource = dtData
            grdQCNotes.DataKeyField = "QCNoteRef"
            grdQCNotes.CurrentPageIndex = iPageNumber
            grdQCNotes.SelectedIndex = -1
            grdQCNotes.EditItemIndex = -1
            grdQCNotes.DataBind()

            ' initialise the pager
            QCNotePager.DataTableSessionID = SessionVars.SV_BatchQCNotesTable
            QCNotePager.DataViewSessionID = SessionVars.SV_BatchQCNotesView
            QCNotePager.PageLinkCount = 10
            QCNotePager.AllowAddNew = False
            QCNotePager.AllowEdit = False
            QCNotePager.AllowDelete = False
            QCNotePager.Refresh()


        Catch ex As Exception
            clsAppError.DisplayError("Failed to initialise the QC Notes grid.", ex)
        End Try
    End Sub

#End Region
    
#Region "Event Handlers"

    Private Sub grdQCNotes_SortCommand(ByVal sender As System.Object, ByVal e As System.Web.UI.WebControls.DataGridSortCommandEventArgs) Handles grdQCNotes.SortCommand
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

    Private Sub btnPrint_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnPrint.Click
        Dim iQCNoteRef
        Try
            If grdQCNotes.SelectedIndex >= 0 Then
                iQCNoteRef = grdQCNotes.DataKeys(grdQCNotes.SelectedIndex)

                OpenDownloadPopup("QCNoteForm.aspx?QCNoteRef=" & iQCNoteRef, Me.Page)
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to print submission.", ex)
        End Try
    End Sub

    Private Sub btnGo_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnGo.Click
        Dim bRedirect As Boolean = False
        Try
            If ValidateMandatoryFields() Then
                Dim objQCNote As New HistopathologyLib.clsQCNote
                Dim dtData As New DataTable
                Dim dvBatchesView As DataView

                If Not objQCNote.GetBatchQCNotes(dtData, CInt(txtQCNote.Text())) Then
                    Throw New Exception("Batch.GetBatchQCNotes returned false.")
                End If

                If dtData.Rows.Count = 0 Then
                    ctlDiv.InnerHtml = "<p><font color=""Red"">QC Note Ref entered could not be found, please try again.</font></p>"
                Else
                    bRedirect = True
                End If
            End If

            Session.Item(SessionVars.SV_SearchCriteria) = grdQCNotes.CurrentPageIndex
        Catch ex As Exception
            clsAppError.DisplayError("Failed to redirect to QC Note edit page.", ex)
        End Try

        If bRedirect Then
            Response.Redirect("EditQCNote.aspx?QCNoteRef=" & CInt(txtQCNote.Text()))
        End If
    End Sub

    Private Sub btnHome_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Response.Redirect("Home.aspx")
    End Sub

    Private Sub btnDone_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnDone.Click
        Response.Redirect("Home.aspx")
    End Sub

    Private Sub btnEdit_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnEdit.Click
        Dim iQCNoteRef
        Try
            If grdQCNotes.SelectedIndex >= 0 Then
                iQCNoteRef = grdQCNotes.DataKeys(grdQCNotes.SelectedIndex)
                Session.Item(SessionVars.SV_SearchCriteria) = grdQCNotes.CurrentPageIndex
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to print submission.", ex)
        End Try
        Response.Redirect("EditQCNote.aspx?QCNoteRef=" & iQCNoteRef)
    End Sub

    Private Sub grdQCNotes_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles grdQCNotes.SelectedIndexChanged
        If grdQCNotes.SelectedIndex >= 0 Then
            EnableDisableButtons(True)
        Else
            EnableDisableButtons(False)
        End If
    End Sub

#End Region

#Region "Private Functions"

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

    Private Function EnableDisableButtons(ByVal bEnabled As Boolean)
        btnPrint.Enabled = bEnabled
        btnEdit.Enabled = bEnabled
    End Function

    Private Function ValidateMandatoryFields() As Boolean
        Try
            rfvQcNote.Validate()
            revQCNoteRef.Validate()

            If Not rfvQcNote.IsValid Or _
            Not revQCNoteRef.IsValid() Then
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
