Partial Class PickListMaintenanceID
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
        Pager.SetGrid(grdLookup)

        If Not IsPostBack Then
            CheckPermissions()
            LoadLookupLists()
            RemoveUserArea()

            Dim sTableID As String = Request.QueryString.Get("TableID")
            If sTableID = "" Then sTableID = 18
            SelectItemInDropDownList(ddlEditableLookups, sTableID)
            RefreshLookupGrid()
        End If
    End Sub

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
        Dim sTableID As String
        Try
            sTableID = ddlEditableLookups.SelectedItem.Value
            Dim LookupData As DataTable
            Dim Lookup As New HistopathologyLib.LookupData()
            LookupData = Lookup.GetLookupData(CInt(sTableID), True)

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
            sMsg = "Failed to retrieve the lookup data in table '" & sTableID & "'"
            clsAppError.DisplayError(sMsg, ex)
        End Try
    End Sub

#End Region

#Region "Event handlers"
    Private Sub Pager_BeforeDataChanged(ByVal sender As System.Object, ByRef e As HistopathologySystem.DataGridPagerEventArgs) Handles Pager.BeforeDataChanged
        e.bCarryOnEditing = m_bContinueEditing
    End Sub

    Private Sub Pager_DataChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Pager.DataChanged
        ' save the data in the DataTable to the database
        Try
            If Not m_bContinueEditing Then
                Dim dt As DataTable = CType(Session.Item(SessionVars.SV_LookupDataTable), DataTable)

                If dt Is Nothing Then
                    Throw New Exception("DataTable not found with session ID " & SessionVars.SV_LookupDataTable)
                End If

                Dim iTableID As Integer = ddlEditableLookups.SelectedItem.Value

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

    Private Sub VLAHeader1_HomeClick(ByVal sender As Object, ByVal e As HistopathologySystem.HomeLinkEventArgs) Handles VLAHeader1.HomeClick
        Response.Redirect("Home.aspx")
    End Sub

    Private Sub Pager_RowSave(ByVal sender As System.Object, ByVal e As HistopathologySystem.DataGridPagerEventArgs) Handles Pager.RowSave
        Dim iCount As Integer
        Dim dtData As DataTable = Session.Item(SessionVars.SV_LookupDataTable)
        Dim bUpperCase As Boolean = False

        Select Case CType(ddlEditableLookups.SelectedItem.Value, Integer)
            Case LOOKUP_PROJECTS
                bUpperCase = True
            Case Else
                bUpperCase = False
        End Select

        'Save template values to the dataset here
        Dim cb As CheckBox = CType(e.GridRow.FindControl("cbActiveEdit"), CheckBox)
        e.DataTableRow("IsActive") = cb.Checked

        Dim txt As TextBox = CType(e.GridRow.FindControl("txtDescriptionEdit"), TextBox)

        If bUpperCase Then
            e.DataTableRow("Description") = txt.Text.ToUpper.Trim
        Else
            e.DataTableRow("Description") = txt.Text.Trim
        End If

        Dim ddl As DropDownList = CType(e.GridRow.FindControl("ddlAreaEdit"), DropDownList)
        e.DataTableRow("Area") = ddl.SelectedItem.Value

        m_bContinueEditing = False
        For iCount = 0 To dtData.Rows.Count - 1
            If dtData.Rows(iCount)("Description") = e.DataTableRow("Description") Then
                If dtData.Rows(iCount)("Area") = e.DataTableRow("Area") Then
                    If dtData.Rows(iCount)("ID") <> e.DataTableRow("ID") Then
                        m_bContinueEditing = True
                        Pager.ShowErrorString("The Description you have selected is already used")
                        Exit For
                    End If
                End If
            End If
        Next

    End Sub

    Private Sub grdLookup_ItemDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.DataGridItemEventArgs) Handles grdLookup.ItemDataBound
        'populate template columns here
        Try
            'set up the checkbox column
            Dim drv As DataRowView = CType(e.Item.DataItem, DataRowView)
            Dim lblArea As Label = Nothing
            Dim ddlArea As DropDownList = Nothing

            If Not drv Is Nothing Then

                Dim cb As CheckBox = Nothing
                Dim lblDescriptionDisplay As Label = Nothing
                Dim txtDescriptionEdit As TextBox = Nothing

                If e.Item.ItemType = ListItemType.EditItem Then
                    cb = CType(e.Item.FindControl("cbActiveEdit"), CheckBox)
                    txtDescriptionEdit = CType(e.Item.FindControl("txtDescriptionEdit"), TextBox)
                    ddlArea = CType(e.Item.FindControl("ddlAreaEdit"), DropDownList)
                ElseIf e.Item.ItemType = ListItemType.Item _
            OrElse e.Item.ItemType = ListItemType.AlternatingItem _
            OrElse e.Item.ItemType = ListItemType.SelectedItem Then
                    cb = CType(e.Item.FindControl("cbActiveDisplay"), CheckBox)
                    lblDescriptionDisplay = CType(e.Item.FindControl("lblDescriptionDisplay"), Label)
                    lblArea = CType(e.Item.FindControl("lblAreaDisplay"), Label)
                End If

                If Not lblArea Is Nothing Then
                    If Not IsDBNull(drv("Area")) Then
                        lblArea.Text = GetAreaDescription(drv("Area"))
                    Else
                        lblArea.Text = ""
                    End If
                End If

                If Not ddlArea Is Nothing Then
                    LoadLookupAreaList(ddlArea)

                    If Not IsDBNull(drv("Area")) Then
                        SelectItemInDropDownList(ddlArea, drv("Area").ToString())
                    Else
                        SelectItemInDropDownList(ddlArea, "")
                    End If
                End If

                If Not lblDescriptionDisplay Is Nothing Then
                    If Not IsDBNull(drv("Description")) Then
                        lblDescriptionDisplay.Text = drv("Description").ToString()
                    Else
                        lblDescriptionDisplay.Text = ""
                    End If
                End If

                If Not txtDescriptionEdit Is Nothing Then
                    If Not IsDBNull(drv("Description")) Then
                        txtDescriptionEdit.Text = drv("Description").ToString()
                    Else
                        txtDescriptionEdit.Text = ""
                    End If
                End If

                If Not cb Is Nothing Then
                    If IsDBNull(drv("IsActive")) Then
                        cb.Checked = True
                    Else
                        cb.Checked = drv("IsActive")
                    End If
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to bind the check box column in the look up data grid", ex)
        End Try
    End Sub

    Private Sub ddlEditableLookups_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ddlEditableLookups.SelectedIndexChanged
        Select Case CType(ddlEditableLookups.SelectedItem.Value, Integer)
            Case LOOKUP_SUBMISSION_PRIORITY
                Response.Redirect("PickListMaintenance.aspx?TableID=" & LOOKUP_SUBMISSION_PRIORITY)
            Case LOOKUP_TIME_RECEIVED
                Response.Redirect("PickListMaintenance.aspx?TableID=" & LOOKUP_TIME_RECEIVED)
            Case LOOKUP_TSE_ANTIBODIES
                Response.Redirect("PickListMaintenance.aspx?TableID=" & LOOKUP_TSE_ANTIBODIES)
            Case LOOKUP_NONTSE_ANTIBODIES
                Response.Redirect("PickListMaintenance.aspx?TableID=" & LOOKUP_NONTSE_ANTIBODIES)
            Case LOOKUP_SPECIAL_STAIN
                Response.Redirect("PickListMaintenance.aspx?TableID=" & LOOKUP_SPECIAL_STAIN)
            Case LOOKUP_TISSUE_CODE
                Response.Redirect("PickListMaintenance.aspx?TableID=" & LOOKUP_TISSUE_CODE)
            Case LOOKUP_FIXATIVE
                Response.Redirect("PickListMaintenance.aspx?TableID=" & LOOKUP_FIXATIVE)
            Case LOOKUP_SUBMITTEDAS
                Response.Redirect("PickListMaintenance.aspx?TableID=" & LOOKUP_SUBMITTEDAS)
            Case LOOKUP_POSTFIXATION
                Response.Redirect("PickListMaintenance.aspx?TableID=" & LOOKUP_POSTFIXATION)
            Case LOOKUP_QC_CODE
                Response.Redirect("PickListMaintenance.aspx?TableID=" & LOOKUP_QC_CODE)
            Case LOOKUP_REMEDIAL_ACTION
                Response.Redirect("PickListMaintenance.aspx?TableID=" & LOOKUP_REMEDIAL_ACTION)
            Case LOOKUP_ARCHIVE_LOCATION
                Response.Redirect("PickListMaintenance.aspx?TableID=" & LOOKUP_ARCHIVE_LOCATION)
            Case LOOKUP_PREMIUM_CHARGES
                Response.Redirect("PickListMaintenance.aspx?TableID=" & LOOKUP_PREMIUM_CHARGES)
        End Select
        RefreshLookupGrid()
    End Sub

    Private Sub Pager_EditModeStart(ByVal sender As Object, ByVal e As DataGridPagerEventArgs) Handles Pager.EditModeStart

        Dim bUpperCase As Boolean = False

        Select Case CType(ddlEditableLookups.SelectedItem.Value, Integer)
            Case LOOKUP_PROJECTS
                bUpperCase = True
            Case Else
                bUpperCase = False
        End Select

        Dim txtDescriptionText As TextBox = CType(e.GridRow.FindControl("txtDescriptionEdit"), TextBox)
        If Not txtDescriptionText Is Nothing Then
            SetFocus(txtDescriptionText)

            If bUpperCase Then
                txtDescriptionText.CssClass = "uppertext"
            End If
        End If
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

#Region "Private Functions"

    Private Function GetAreaDescription(ByVal sCode As String) As String
        Dim objLookup As New HistopathologyLib.LookupData()
        Dim dt As DataTable = objLookup.GetUserAreas()

        If Not dt Is Nothing Then
            Dim dv As New DataView(dt, "", "Code", DataViewRowState.CurrentRows)
            Dim iRow As Integer = dv.Find(sCode)
            If iRow >= 0 Then
                Return dv(iRow).Item("Description").ToString()
            Else
                Return ""
            End If
        Else
            Return ""
        End If
    End Function

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

    Private Sub LoadLookupAreaList(ByRef ddl As DropDownList)

        Dim blnResult As Boolean
        Dim objDataTable As DataTable
        Dim objLookup As New HistopathologyLib.LookupData()

        Try
            objDataTable = objLookup.GetUserAreas()
            If Not (objDataTable Is Nothing) Then
                ddl.DataSource = objDataTable
                ddl.DataValueField = "Code"
                ddl.DataTextField = "Description"
                ddl.DataBind()
                Common.AddItemToDropDownList(ddl)
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve User Area list.", ex)
        End Try
    End Sub

#End Region


End Class
