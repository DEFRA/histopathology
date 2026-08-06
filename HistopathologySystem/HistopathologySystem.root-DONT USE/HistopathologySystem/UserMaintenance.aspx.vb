Partial Class UserMaintenance
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents Pager As DataGridPager

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
        VLAHeader1.PageTitle = "User Maintenance"
        Pager.SetGrid(grdUsers)

        If Not IsPostBack Then
            InitialiseUsersGrid()
        End If

    End Sub

#Region "Users Grid"

    Private Sub InitialiseUsersGrid()
        Try
            Dim dtUsersData As DataTable
            Dim Lookup As New HistopathologyLib.clsUser()
            Dim sUserID As String

            sUserID = CStr(Session.Item(SessionVars.SV_PassUserArea))

            If sUserID = Nothing Then
                dtUsersData = Lookup.GetUsers()
                btnDone.Visible = False
                lblDescription2.Visible = False
                VLAHeader1.SubmissioNoVisible() = False
            Else
                dtUsersData = Lookup.GetUsersByArea(sUserID)
                btnDone.Visible = True
                lblDescription2.Visible = True
            End If

            If dtUsersData Is Nothing Then Throw New Exception()

            Session.Item(SessionVars.SV_UsersTable) = dtUsersData

            ' create a dataview for filtering and sorting
            Dim dv As DataView = dtUsersData.DefaultView
            Session.Item(SessionVars.SV_UsersView) = dv

            ' initialise the grid
            grdUsers.DataSource = dtUsersData
            grdUsers.DataKeyField = "ID"
            grdUsers.CurrentPageIndex = 0
            grdUsers.SelectedIndex = -1
            grdUsers.EditItemIndex = -1
            grdUsers.DataBind()
            grdUsers.Enabled = True

            ' initialise the pager
            Pager.DataTableSessionID = SessionVars.SV_UsersTable
            Pager.DataViewSessionID = SessionVars.SV_UsersView
            Pager.PageLinkCount = 10
            Pager.AllowAddNew = True
            If Not Session.Item(SessionVars.SV_PassUserArea) Is Nothing Then
                Pager.AllowEdit = False
            Else
                Pager.AllowEdit = True
            End If
            Pager.AllowDelete = False
            Pager.Rebind()
            Pager.Refresh()

        Catch ex As Exception
            clsAppError.DisplayError("Error initialising the Add block details page", ex)
        End Try
    End Sub
#End Region

#Region "Private Functions"

    Private Sub LoadLookupGroupList(ByRef ddl As DropDownList)

        Dim blnResult As Boolean
        Dim objDataTable As DataTable
        Dim objLookup As New HistopathologyLib.LookupData()

        Try
            objDataTable = objLookup.GetUserGroups()
            If Not (objDataTable Is Nothing) Then
                ddl.DataSource = objDataTable
                ddl.DataValueField = "Code"
                ddl.DataTextField = "Description"
                ddl.DataBind()
                Common.AddItemToDropDownList(ddl)
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve User Group list.", ex)
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

#Region "Event Handlers"

    Private Sub Pager_DataChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles Pager.DataChanged
        ' save the data in the DataTable to the database
        Try
            Dim dt As DataTable
            dt = CType(Session.Item(SessionVars.SV_UsersTable), DataTable)

            If dt Is Nothing Then
                Throw New Exception("DataTable not found with session ID """ _
                                    & SessionVars.SV_UsersTable & """")
            End If

            Dim objUser As New HistopathologyLib.clsUser()
            If objUser.SaveUserData(dt, CInt(Session.Item(SessionVars.SV_HeaderUserID))) Then
                dt.AcceptChanges()
            Else
                If dt.HasErrors Then
                    ' we have row update errors - tell the data grid to
                    ' display them
                    Pager.DisplayRowError(Nothing, True)
                Else
                    Throw New Exception("clsUser.SaveUserData returned False")
                End If
            End If

            InitialiseUsersGrid()

        Catch ex As Exception
            clsAppError.DisplayError("Failed to save user data to the database", ex)
        End Try
    End Sub

    Private Sub grdUsers_ItemDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.DataGridItemEventArgs) Handles grdUsers.ItemDataBound
        ' populate template column values here
        Try
            ' set up the checkbox and drop-down columns
            Dim drv As DataRowView = CType(e.Item.DataItem, DataRowView)
            If Not drv Is Nothing Then
                Dim cb As CheckBox = Nothing
                Dim lblArea As Label = Nothing
                Dim lblGroup As Label = Nothing
                Dim ddlArea As DropDownList = Nothing
                Dim ddlGroup As DropDownList = Nothing
                Dim lblEmail As Label = Nothing
                Dim txtEmail As TextBox = Nothing
                Dim lblName As Label = Nothing
                Dim txtName As TextBox = Nothing
                Dim lblNTLogin As Label = Nothing
                Dim txtNTLogin As TextBox = Nothing


                If e.Item.ItemType = ListItemType.EditItem Then
                    ' populate edit mode controls
                    cb = CType(e.Item.FindControl("cbActiveEdit"), CheckBox)
                    ddlArea = CType(e.Item.FindControl("ddlAreaEdit"), DropDownList)
                    ddlGroup = CType(e.Item.FindControl("ddlGroupEdit"), DropDownList)
                    txtEmail = CType(e.Item.FindControl("txtEmailEdit"), TextBox)
                    txtName = CType(e.Item.FindControl("txtNameEdit"), TextBox)
                    txtNTLogin = CType(e.Item.FindControl("txtNTLoginEdit"), TextBox)
                ElseIf e.Item.ItemType = ListItemType.Item _
                OrElse e.Item.ItemType = ListItemType.AlternatingItem _
                OrElse e.Item.ItemType = ListItemType.SelectedItem Then
                    ' populate display mode controls
                    cb = CType(e.Item.FindControl("cbActiveDisplay"), CheckBox)
                    lblArea = CType(e.Item.FindControl("lblAreaDisplay"), Label)
                    lblGroup = CType(e.Item.FindControl("lblGroupDisplay"), Label)
                    lblEmail = CType(e.Item.FindControl("lblEmail"), Label)
                    lblName = CType(e.Item.FindControl("lblNameDisplay"), Label)
                    lblNTLogin = CType(e.Item.FindControl("lblNTLoginDisplay"), Label)
                End If

                If Not lblNTLogin Is Nothing Then
                    If Not IsDBNull(drv("NTLogin")) Then
                        lblNTLogin.Text = drv("NTLogin").ToString()
                    End If
                End If

                If Not txtNTLogin Is Nothing Then
                    If Not IsDBNull(drv("NTLogin")) Then
                        txtNTLogin.Text = drv("NTLogin").ToString()
                    End If
                End If

                If Not lblName Is Nothing Then
                    If Not IsDBNull(drv("Name")) Then
                        lblName.Text = drv("Name").ToString()
                    End If
                End If

                If Not txtName Is Nothing Then
                    If Not IsDBNull(drv("Name")) Then
                        txtName.Text = drv("Name").ToString()
                    End If
                End If

                If Not lblEmail Is Nothing Then
                    If Not IsDBNull(drv("Email")) Then
                        lblEmail.Text = drv("Email").ToString()
                    End If
                End If

                If Not txtEmail Is Nothing Then
                    If Not IsDBNull(drv("Email")) Then
                        txtEmail.Text = drv("Email").ToString()
                    End If
                End If

                If Not cb Is Nothing Then
                    If Not IsDBNull(drv("Active")) Then
                        cb.Checked = drv("Active")
                    End If
                End If

                If Not lblArea Is Nothing Then
                    If Not IsDBNull(drv("UserArea")) Then
                        lblArea.Text = GetAreaDescription(drv("UserArea"))
                    Else
                        lblArea.Text = ""
                    End If
                End If

                If Not lblGroup Is Nothing Then
                    If Not IsDBNull(drv("UserGroup")) Then
                        lblGroup.Text = GetGroupDescription(drv("UserGroup"))
                    Else
                        lblGroup.Text = ""
                    End If
                End If

                If Not ddlArea Is Nothing Then
                    LoadLookupAreaList(ddlArea)

                    If Not IsDBNull(drv("UserArea")) Then
                        SelectItemInDropDownList(ddlArea, drv("UserArea"))
                    Else
                        Dim sUserID As String = Request.QueryString.Get("UserAreaID")
                        If Not sUserID = "" Then
                            SelectItemInDropDownList(ddlArea, sUserID)
                        End If
                    End If
                End If

                If Not ddlGroup Is Nothing Then
                    LoadLookupGroupList(ddlGroup)
                    If Not IsDBNull(drv("UserGroup")) Then
                        SelectItemInDropDownList(ddlGroup, drv("UserGroup"))
                    End If
                End If
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to bind template columns in the User data grid", ex)
        End Try
    End Sub

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

    Private Function GetGroupDescription(ByVal sCode As String) As String
        Dim objLookup As New HistopathologyLib.LookupData()
        Dim dt As DataTable = objLookup.GetUserGroups()

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

    Private Sub Pager_RowSave(ByVal sender As Object, ByVal e As HistopathologySystem.DataGridPagerEventArgs) Handles Pager.RowSave
        ' save template column values to the dataset here
        Dim cb As CheckBox = CType(e.GridRow.FindControl("cbActiveEdit"), CheckBox)
        e.DataTableRow("Active") = cb.Checked

        Dim ddlArea As DropDownList = CType(e.GridRow.FindControl("ddlAreaEdit"), DropDownList)
        e.DataTableRow("UserArea") = ddlArea.SelectedItem.Value

        Dim ddlGroup As DropDownList = CType(e.GridRow.FindControl("ddlGroupEdit"), DropDownList)
        e.DataTableRow("UserGroup") = ddlGroup.SelectedItem.Value

        Dim txtName As TextBox = CType(e.GridRow.FindControl("txtNameEdit"), TextBox)
        e.DataTableRow("Name") = txtName.Text.Trim

        Dim txtNTLogin As TextBox = CType(e.GridRow.FindControl("txtNTLoginEdit"), TextBox)
        e.DataTableRow("NTLogin") = txtNTLogin.Text.Trim
    End Sub

    Private Sub cbActive_CheckedChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles cbActive.CheckedChanged
        Dim dv As DataView = CType(Session(SessionVars.SV_UsersView), DataView)

        If Not dv Is Nothing Then
            If cbActive.Checked Then
                dv.RowFilter = ""
            Else
                dv.RowFilter = "Active='True'"
                grdUsers.CurrentPageIndex = 0
                grdUsers.SelectedIndex = -1
                grdUsers.EditItemIndex = -1
            End If

            Pager.Rebind()
            Pager.Refresh()

        End If
    End Sub

    Private Sub VLAHeader1_HomeClick(ByVal sender As Object, ByVal e As HistopathologySystem.HomeLinkEventArgs) Handles VLAHeader1.HomeClick
        If Not CStr(Session.Item(SessionVars.SV_PassUserArea)) Is Nothing Then
            Dim sMessage As System.Text.StringBuilder = New System.Text.StringBuilder

            sMessage.Append("Any changes that have been made to the submission will be discarded, are you sure you wish to exit without saving?")
            Page.RegisterStartupScript("navigate", PromptBeforeNavigateScript(sMessage.ToString(), "Home.aspx"))
            e.bNavigateHome = False
        Else
            Response.Redirect("Home.aspx")
        End If
    End Sub

    Private Sub Pager_AddNew(ByVal sender As System.Object, ByVal e As HistopathologySystem.DataGridPagerEventArgs) Handles Pager.EditModeStart
        If Not Session.Item(SessionVars.SV_PassUserArea) Is Nothing Then

            Dim ddlUserGroup As DropDownList = CType(e.GridRow.FindControl("ddlGroupEdit"), DropDownList)
            Dim cbIsActive As CheckBox = CType(e.GridRow.FindControl("cbActiveEdit"), CheckBox)

            btnDone.Enabled = False

            If Not CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Histopath" Then
                Dim ddlUserArea As DropDownList = CType(e.GridRow.FindControl("ddlAreaEdit"), DropDownList)

                If Not ddlUserArea Is Nothing Then
                    SelectItemInDropDownList(ddlUserArea, CStr(Session.Item(SessionVars.SV_HeaderUserAreaID)))
                    ddlUserArea.Enabled = False
                End If
            End If

            If Not ddlUserGroup Is Nothing Then
                If ddlUserGroup.SelectedValue.ToString = "" Then
                    SelectItemInDropDownList(ddlUserGroup, "1")
                End If
                ddlUserGroup.Enabled = False
            End If

            cbIsActive.Enabled = False
        End If

        Dim txtLogin As TextBox = CType(e.GridRow.FindControl("txtNTLoginEdit"), TextBox)
        If Not txtLogin Is Nothing Then
            SetFocus(txtLogin)
        End If

    End Sub

    Private Sub Pager_EditModeStop(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Pager.EditModeStop
        If Not Session.Item(SessionVars.SV_PassUserArea) Is Nothing Then
            btnDone.Enabled = True
        End If

    End Sub

    Private Sub btnDone_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnDone.Click
        Response.Redirect("BatchDetails.aspx")
    End Sub

#End Region

    
End Class
