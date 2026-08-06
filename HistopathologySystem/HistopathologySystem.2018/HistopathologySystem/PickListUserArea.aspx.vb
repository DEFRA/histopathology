Partial Class PickListUserArea
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
        SetTitle()
        Pager.SetGrid(grdLookup)

        If Not IsPostBack Then
            RefreshLookupGrid()
        End If
    End Sub

#Region "Load Grid Contents"

    Private Sub RefreshLookupGrid()
        Dim iTableID As Integer
        Try
            iTableID = CInt(Session.Item(SessionVars.SV_PickListTableID))
            Dim LookupData As DataTable
            Dim Lookup As New HistopathologyLib.LookupData()
            Dim sUserArea As String = CStr(Session.Item(SessionVars.SV_PassUserArea))
            LookupData = Lookup.GetLookupData(CInt(Session.Item(SessionVars.SV_PickListTableID)), True)

            If LookupData Is Nothing Then Throw New Exception()

            Session.Item(SessionVars.SV_LookupDataTable) = LookupData
            LookupData.TableName = "UserList"

            If Not sUserArea = "" Then
                LookupData.DefaultView.RowFilter = "Area=" & sUserArea
            Else
                LookupData.DefaultView.RowFilter = "Area=" & CStr(Session.Item(SessionVars.SV_HeaderUserAreaID))
            End If

            ' create a dataview for filtering and sorting
            Dim dv As DataView = LookupData.DefaultView
            Session.Item(SessionVars.SV_LookupDataView) = dv

            grdLookup.DataSource = LookupData
            grdLookup.DataKeyField = "ID"
            grdLookup.CurrentPageIndex = 0
            grdLookup.SelectedIndex = -1
            grdLookup.EditItemIndex = -1
            grdLookup.DataBind()

            Pager.SetGrid(grdLookup)
            Pager.DataTableSessionID = SessionVars.SV_LookupDataTable
            Pager.DataViewSessionID = SessionVars.SV_LookupDataView
            Pager.PageLinkCount = 10
            Pager.AllowAddNew = True
            Pager.AllowEdit = True
            Pager.AllowDelete = False
            Pager.ConfirmDelete = True
            Pager.Refresh()

        Catch ex As Exception
            Dim sMsg As String
            sMsg = "Failed to retrieve the lookup data in table '" & iTableID & "'"
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

                Dim iTableID As Integer = CInt(Session.Item(SessionVars.SV_PickListTableID))

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
        Dim sMessage As System.Text.StringBuilder = New System.Text.StringBuilder

        sMessage.Append("Any changes that have been made to the submission will be discarded, are you sure you wish to exit without saving?")
        Page.RegisterStartupScript("navigate", PromptBeforeNavigateScript(sMessage.ToString(), "Home.aspx"))
        e.bNavigateHome = False
    End Sub

    Private Sub Pager_EditModeStart(ByVal sender As Object, ByVal e As HistopathologySystem.DataGridPagerEventArgs) Handles Pager.EditModeStart

        Dim bUpperCase As Boolean = False

        Select Case Session.Item(SessionVars.SV_PickListTableID)
            Case LOOKUP_PROJECTS
                bUpperCase = True
            Case Else
                bUpperCase = False
        End Select

        btnDone.Enabled = False

        Dim txtDescriptionText As TextBox = CType(e.GridRow.FindControl("txtDescriptionEdit"), TextBox)
        If Not txtDescriptionText Is Nothing Then

            SetFocus(txtDescriptionText)

            If bUpperCase Then
                txtDescriptionText.CssClass = "uppertext"
            End If
        End If


    End Sub

    Private Sub Pager_EditModeStop(ByVal sender As Object, ByVal e As System.EventArgs) Handles Pager.EditModeStop
        btnDone.Enabled = True
    End Sub

    Private Sub btnDone_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnDone.Click
        Response.Redirect("BatchDetails.aspx")
    End Sub

    Private Sub Pager_RowSave(ByVal sender As System.Object, ByVal e As HistopathologySystem.DataGridPagerEventArgs) Handles Pager.RowSave
        'Save template values to the dataset here
        Dim iCount As Integer
        Dim dtData As DataTable = Session.Item(SessionVars.SV_LookupDataTable)
        Dim bUpperCase As Boolean = False

        Select Case Session.Item(SessionVars.SV_PickListTableID)
            Case LOOKUP_PROJECTS
                bUpperCase = True
            Case Else
                bUpperCase = False
        End Select

        Dim cb As CheckBox = CType(e.GridRow.FindControl("cbActiveEdit"), CheckBox)
        e.DataTableRow("IsActive") = cb.Checked

        Dim txt As TextBox = CType(e.GridRow.FindControl("txtDescriptionEdit"), TextBox)
        If bUpperCase Then
            e.DataTableRow("Description") = txt.Text.ToUpper
        Else
            e.DataTableRow("Description") = txt.Text
        End If

        If IsDBNull(e.DataTableRow("Area")) Or e.DataTableRow("Area").ToString() = "" Then
            If Not CStr(Session.Item(SessionVars.SV_HeaderUserAreaID)) = "" Then
                e.DataTableRow("Area") = CStr(Session.Item(SessionVars.SV_PassUserArea))
            Else
                e.DataTableRow("Area") = CStr(Session.Item(SessionVars.SV_HeaderUserAreaID))
            End If
        End If

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
            Dim cb As CheckBox
            Dim lblDescriptionDisplay As Label = Nothing
            Dim txtDescriptionEdit As TextBox = Nothing

            If e.Item.ItemType = ListItemType.EditItem Then
                cb = CType(e.Item.FindControl("cbActiveEdit"), CheckBox)
                txtDescriptionEdit = CType(e.Item.FindControl("txtDescriptionEdit"), TextBox)
            ElseIf e.Item.ItemType = ListItemType.Item _
        OrElse e.Item.ItemType = ListItemType.AlternatingItem _
        OrElse e.Item.ItemType = ListItemType.SelectedItem Then
                cb = CType(e.Item.FindControl("cbActiveDisplay"), CheckBox)
                lblDescriptionDisplay = CType(e.Item.FindControl("lblDescriptionDisplay"), Label)
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

            If Not cb Is Nothing AndAlso Not drv Is Nothing Then
                If IsDBNull(drv("IsActive")) Then
                    cb.Checked = True
                Else
                    cb.Checked = drv("IsActive")
                End If
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to bind the check box column in the look up data grid", ex)
        End Try
    End Sub
#End Region

#Region "Private Functions"

    Private Sub SetTitle()
        Try
            Dim iTableID As Integer = CType(Session.Item(SessionVars.SV_PickListTableID), Integer)

            If iTableID = LOOKUP_CONTACTS Then
                VLAHeader1.PageTitle = "Edit Pathologist Pick List"
            ElseIf iTableID = LOOKUP_PROJECTS Then
                VLAHeader1.PageTitle = "Edit Projects Pick List"
            Else
                VLAHeader1.PageTitle = "Pick List Maintenance"
            End If


        Catch ex As Exception
            clsAppError.DisplayError("Failed to set title.", ex)
        End Try
    End Sub
#End Region
End Class
