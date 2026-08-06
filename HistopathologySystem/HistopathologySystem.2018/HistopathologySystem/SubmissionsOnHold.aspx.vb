Partial Class SubmissionsOnHold
    Inherits System.Web.UI.Page
    Protected WithEvents PagerSubmissions As DataGridPager
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
        VLAHeader1.PageTitle = "Samples On Hold"
        PagerSubmissions.SetGrid(grdSubmissions)
        If Not IsPostBack Then
            CheckPermissions()
            InitialiseSubmissionsGrid()
        End If
    End Sub

#Region "Event Handlers"

    Private Sub grdSubmissions_ItemDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.DataGridItemEventArgs) Handles grdSubmissions.ItemDataBound
        ' populate template column values here
        Try
            ' set up the checkbox and drop-down columns
            Dim drv As DataRowView = CType(e.Item.DataItem, DataRowView)
            If Not drv Is Nothing Then

                Dim cbOnHold As CheckBox = Nothing
                Dim cbOnHoldEdit As CheckBox = Nothing

                If e.Item.ItemType = ListItemType.EditItem Then
                    cbOnHoldEdit = CType(e.Item.FindControl("cbOnHoldEdit"), CheckBox)
                ElseIf e.Item.ItemType = ListItemType.Item _
                OrElse e.Item.ItemType = ListItemType.AlternatingItem _
                OrElse e.Item.ItemType = ListItemType.SelectedItem Then
                    ' populate display mode controls
                    cbOnHold = CType(e.Item.FindControl("cbOnHoldDisplay"), CheckBox)
                End If

                If Not cbOnHold Is Nothing Then
                    If Not IsDBNull(drv("OnHold")) Then
                        cbOnHold.Checked = drv("OnHold")
                    Else
                        cbOnHold.Checked = False
                    End If
                End If

                If Not cbOnHoldEdit Is Nothing Then
                    If Not IsDBNull(drv("OnHold")) Then
                        cbOnHoldEdit.Checked = drv("OnHold")
                    Else
                        cbOnHoldEdit.Checked = False
                    End If
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to bind template columns in the Samples grid, on the Submissions On Hold page.", ex)
        End Try
    End Sub

    Private Sub PagerSubmissions_Save(ByVal sender As Object, ByVal e As HistopathologySystem.DataGridPagerEventArgs) Handles PagerSubmissions.RowSave
        Dim cbOnHold As CheckBox = CType(e.GridRow.FindControl("cbOnHoldEdit"), CheckBox)
        If Not cbOnHold Is Nothing Then
            e.DataTableRow("OnHold") = cbOnHold.Checked
        End If
    End Sub

    Private Sub btnDone_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnDone.Click
        Response.Redirect("EditBatch.aspx")
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

    Private Sub InitialiseSubmissionsGrid()
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtAnimal As DataTable
            Dim dvAnimalsView As DataView

            If Not dsBatchDetails Is Nothing Then
                If CType(Session.Item(SessionVars.SV_Cassetted), Boolean) = True Then
                    dtAnimal = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL)
                Else
                    dtAnimal = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE)
                End If
            End If

            Session.Item(SessionVars.SV_AnimalTable) = dtAnimal
            dvAnimalsView = dtAnimal.DefaultView
            Session.Item(SessionVars.SV_AnimalView) = dvAnimalsView
            Session.Item(SessionVars.SV_AnimalTableBackup) = CopyDataTable(dtAnimal)

            ' initialise the grid
            grdSubmissions.DataSource = dtAnimal
            grdSubmissions.DataKeyField = "ID"
            grdSubmissions.CurrentPageIndex = 0
            grdSubmissions.SelectedIndex = -1
            grdSubmissions.EditItemIndex = -1
            grdSubmissions.DataBind()

            PagerSubmissions.DataTableSessionID = SessionVars.SV_AnimalTable
            PagerSubmissions.DataViewSessionID = SessionVars.SV_AnimalView
            PagerSubmissions.PageLinkCount = 10
            PagerSubmissions.AllowAddNew = False
            PagerSubmissions.AllowEdit = True
            PagerSubmissions.AllowDelete = False
            PagerSubmissions.Refresh()

        Catch ex As Exception
            clsAppError.DisplayError("Failed to initialise the Samples grid on the Samples On Hold page.", ex)
        End Try
    End Sub

#End Region

    End Class
