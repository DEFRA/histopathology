Partial Class PickListMaintenance
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents Pager As DataGridPager
    Private m_bContinueEditing As Boolean = Nothing

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
        VLAHeader1.PageTitle = "Pick List Maintenance"
        CheckPermissions()
        Pager.SetGrid(grdLookup)
        VLAHeader1.SubmissioNoVisible() = False

        If Not IsPostBack Then
            LoadLookupLists()
            Dim sTableID As String = Request.QueryString.Get("TableID")
            If sTableID = "" Then sTableID = 16
            SelectItemInDropDownList(ddlEditableLookups, sTableID)
            RefreshLookupGrid()
            RemoveUserArea()
        End If
    End Sub

#Region "Private Functions"

    Private Sub RemoveUserArea()
        Try
            Dim li As ListItem
            For Each li In ddlEditableLookups.Items
                If li.Text = "User Area" Then
                    ddlEditableLookups.Items.Remove(li)
                    Exit Sub
                End If
            Next
        Catch ex As Exception
            clsAppError.DisplayError("Failed to remove UserArea from editable lists.", ex)
        End Try
    End Sub

#End Region

#Region "Load Lookup Lists"

    Private Sub LoadLookupLists()
        Dim blnResult As Boolean
        Dim objDataTable As DataTable
        Dim objLookup As New HistopathologyLib.LookupData()

        Try
            objDataTable = objLookup.ListEditableLookups()

            If Not (objDataTable Is Nothing) Then
                ddlEditableLookups.DataSource = objDataTable
                ddlEditableLookups.DataValueField = "ID"
                ddlEditableLookups.DataTextField = "Description"
                ddlEditableLookups.DataBind()
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve drop down lists.", ex)
        End Try
    End Sub

#End Region

#Region "Load Grid Contents"

    Private Sub RefreshLookupGrid()
        Dim iTableID As Integer
        Try
            iTableID = CType(ddlEditableLookups.SelectedItem.Value, Integer)
            Dim LookupData As DataTable
            Dim Lookup As New HistopathologyLib.LookupData()
            LookupData = Lookup.GetLookupData(iTableID, True)

            If LookupData Is Nothing Then Throw New Exception()

            Session.Item(SessionVars.SV_LookupDataTable) = LookupData

            ' create a dataview for filtering and sorting
            Dim dv As DataView = LookupData.DefaultView
            Session.Item(SessionVars.SV_LookupDataView) = dv

            grdLookup.DataSource = LookupData
            grdLookup.DataKeyField = "ID"
            grdLookup.CurrentPageIndex = 0
            grdLookup.SelectedIndex = -1
            grdLookup.EditItemIndex = -1
            grdLookup.DataBind()

            ' Note: the primary key for the DataTable has been set in 
            ' LookupData.GetLookupData().

            Pager.SetGrid(grdLookup)
            Pager.DataTableSessionID = SessionVars.SV_LookupDataTable
            Pager.DataViewSessionID = SessionVars.SV_LookupDataView
            Pager.PageLinkCount = 10

            If Session(SessionVars.SV_HeaderGroupName) = "Maintenance" Then
                Pager.AllowAddNew = True
                Pager.AllowEdit = True
                Pager.AllowDelete = False
                Pager.ConfirmDelete = True
            Else
                Pager.AllowAddNew = False
                Pager.AllowEdit = False
                Pager.AllowDelete = False
                Pager.ConfirmDelete = False
            End If
            Pager.Refresh()

        Catch ex As Exception
            Dim sMsg As String
            sMsg = "Failed to retrieve the lookup data in table '" & iTableID & "'"
            clsAppError.DisplayError(sMsg, ex)
        End Try
    End Sub

#End Region

#Region "Event handlers"

    Private Sub ddlEditableLookups_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlEditableLookups.SelectedIndexChanged
        Select Case CType(ddlEditableLookups.SelectedItem.Value, Integer)
            Case LOOKUP_PROJECTS
                Response.Redirect("PickListMaintenanceID.aspx?TableID=" & LOOKUP_PROJECTS)
            Case LOOKUP_CONTACTS
                Response.Redirect("PickListMaintenanceID.aspx?TableID=" & LOOKUP_CONTACTS)
            Case Else
                'Response.Redirect("PickListMaintenance.aspx?TableID=" & ddlEditableLookups.SelectedItem.Value)
        End Select
        RefreshLookupGrid()
    End Sub

    Private Sub Pager_DataChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Pager.DataChanged
        ' save the data in the DataTable to the database
        Try
            If Not m_bContinueEditing Then
                Dim dt As DataTable = CType(Session.Item(SessionVars.SV_LookupDataTable), DataTable)

                If dt Is Nothing Then
                    Throw New Exception("DataTable not found with session ID " & SessionVars.SV_LookupDataTable)
                End If

                Dim iTableID As Integer = CType(ddlEditableLookups.SelectedItem.Value, Integer)

                Dim Lookup As New HistopathologyLib.LookupData()

                If Lookup.SaveLookupData(iTableID, dt, CInt(Session.Item(SessionVars.SV_HeaderUserID))) Then
                    dt.AcceptChanges()
                Else
                    If dt.HasErrors Then
                        ' we have row update errors - tell the data grid to display them
                        Pager.DisplayRowError(blnShowFullerror:=True)
                        Pager.AllowAddNew = False
                        Pager.AllowDelete = False
                        Pager.AllowEdit = False
                    Else
                        Throw New Exception("Lookup.SaveLookupData returned False")
                    End If
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to save look-up data to the database", ex)
        End Try
    End Sub

    Private Sub Pager_RowSave(ByVal sender As System.Object, ByVal e As HistopathologySystem.DataGridPagerEventArgs) Handles Pager.RowSave
        Dim iCount As Integer
        Dim dtData As DataTable = Session.Item(SessionVars.SV_LookupDataTable)

        'Save template values to the dataset here
        Dim cb As CheckBox = CType(e.GridRow.FindControl("cbActiveEdit"), CheckBox)
        e.DataTableRow("IsActive") = cb.Checked

        Dim txtDescription As TextBox = CType(e.GridRow.FindControl("txtDescriptionEdit"), TextBox)
        e.DataTableRow("Description") = txtDescription.Text.Trim

        Dim txtCode As TextBox = CType(e.GridRow.FindControl("txtCodeEdit"), TextBox)
        e.DataTableRow("Code") = txtCode.Text.Trim

        m_bContinueEditing = False
        For iCount = 0 To dtData.Rows.Count - 1
            If dtData.Rows(iCount)("Code") = e.DataTableRow("Code") Then
                If dtData.Rows(iCount)("ID") <> e.DataTableRow("ID") Then
                    m_bContinueEditing = True
                    Pager.ShowErrorString("The Code you have selected is already used")
                    Exit For
                End If
            End If
        Next
    End Sub

    Private Sub Pager_BeforeDataChanged(ByVal sender As System.Object, ByRef e As HistopathologySystem.DataGridPagerEventArgs) Handles Pager.BeforeDataChanged
        e.bCarryOnEditing = m_bContinueEditing
    End Sub

    Private Sub Pager_EditModeStart(ByVal sender As Object, ByVal e As DataGridPagerEventArgs) Handles Pager.EditModeStart
        Dim txtCodeText As TextBox = CType(e.GridRow.FindControl("txtCodeEdit"), TextBox)
        If Not txtCodeText Is Nothing Then
            SetFocus(txtCodeText)
        End If
    End Sub

    Private Sub cbActive_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cbActive.CheckedChanged

        Dim dv As DataView = CType(Session(SessionVars.SV_LookupDataView), DataView)

        If Not dv Is Nothing Then
            If cbActive.Checked Then
                dv.RowFilter = ""
            Else
                dv.RowFilter = "IsActive='True'"
                grdLookup.CurrentPageIndex = 0
                grdLookup.SelectedIndex = -1
                grdLookup.EditItemIndex = -1
            End If

            Pager.Rebind()
            Pager.Refresh()

        End If

    End Sub

    Private Sub grdLookup_ItemDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.DataGridItemEventArgs) Handles grdLookup.ItemDataBound
        'populate template columns here
        Try
            'set up the checkbox column
            Dim drv As DataRowView = CType(e.Item.DataItem, DataRowView)
            Dim cb As CheckBox
            Dim txtCode As TextBox
            Dim lblCode As Label
            Dim txtDescription As TextBox
            Dim lblDescription As Label

            If e.Item.ItemType = ListItemType.EditItem Then
                cb = CType(e.Item.FindControl("cbActiveEdit"), CheckBox)
                txtCode = CType(e.Item.FindControl("txtCodeEdit"), TextBox)
                txtDescription = CType(e.Item.FindControl("txtDescriptionEdit"), TextBox)
            ElseIf e.Item.ItemType = ListItemType.Item _
            OrElse e.Item.ItemType = ListItemType.AlternatingItem _
            OrElse e.Item.ItemType = ListItemType.SelectedItem Then
                cb = CType(e.Item.FindControl("cbActiveDisplay"), CheckBox)
                lblCode = CType(e.Item.FindControl("lblCodeDisplay"), Label)
                lblDescription = CType(e.Item.FindControl("lblDescriptionDisplay"), Label)
            End If

            If Not cb Is Nothing AndAlso Not drv Is Nothing Then
                If IsDBNull(drv("IsActive")) Then
                    cb.Checked = True
                Else
                    cb.Checked = drv("IsActive")
                End If
            End If

            If Not lblCode Is Nothing AndAlso Not drv Is Nothing Then
                If IsDBNull(drv("Code")) Then
                    lblCode.Text = ""
                Else
                    lblCode.Text = drv("Code").ToString()
                End If
            End If

            If Not txtCode Is Nothing AndAlso Not drv Is Nothing Then
                If IsDBNull(drv("Code")) Then
                    txtCode.Text = ""
                Else
                    txtCode.Text = drv("Code").ToString()
                End If
            End If

            If Not lblDescription Is Nothing AndAlso Not drv Is Nothing Then
                If IsDBNull(drv("Description")) Then
                    lblDescription.Text = ""
                Else
                    lblDescription.Text = drv("Description").ToString()
                End If
            End If

            If Not txtDescription Is Nothing AndAlso Not drv Is Nothing Then
                If IsDBNull(drv("Description")) Then
                    txtDescription.Text = ""
                Else
                    txtDescription.Text = drv("Description").ToString()
                End If
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to bind the check box column in the look up data grid", ex)
        End Try
    End Sub

    Private Sub VLAHeader1_HomeClick(ByVal sender As Object, ByVal e As HistopathologySystem.HomeLinkEventArgs) Handles VLAHeader1.HomeClick
        Response.Redirect("Home.aspx")
    End Sub

#End Region

#Region "Permissions"

    Private Sub CheckPermissions()
        VLAHeader1.GetUserDetails()

        Dim sGroupName As String = Session(SessionVars.SV_HeaderGroupName)

        If sGroupName = "Customer" Then
            Response.Redirect("Home.aspx")
        ElseIf sGroupName = "Histopathology User" Then
            Response.Redirect("Home.aspx")
        ElseIf sGroupName = "Maintenance" Then
            'Nothing
        Else
            Response.Redirect("Home.aspx")
        End If
    End Sub

#End Region

End Class
