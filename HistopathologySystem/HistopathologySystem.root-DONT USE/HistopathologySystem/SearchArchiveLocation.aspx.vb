Imports System.Text.RegularExpressions

Partial Class SearchArchiveLocation
    Inherits System.Web.UI.Page
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
        VLAHeader1.PageTitle = "Search Archive Location"
        CheckPermissions()
        SetClientValidation()
        VLAHeader1.SubmissioNoVisible() = False

        If Not IsPostBack Then
            'SetTextboxDefaultButton(txtHistRef, btnSearch)
            LoadLookupLists()

            hlExcelExport.Visible = False
            grdBlockArchive.Visible = False
            lblError.Visible = False
            rbTissues.Checked = True
            txtBlockRef.Enabled = False

            SetFocus(txtHistRef)
        End If
    End Sub

#Region "Event Handlers"

    Private Sub btnSearch_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSearch.Click
        revBlockArchive.Validate()
        If (txtSenderRef.Text = "" And txtHistRef.Text = "") Or Not revBlockArchive.IsValid Then
            ctlDiv.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
            lblError.Visible = True
        ElseIf Not txtSenderRef.Text = "" And Not txtHistRef.Text = "" Then
            ctlDiv.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
            lblError.Visible = True
        Else
            ctlDiv.InnerHtml = ""
            lblError.Visible = False
            grdBlockArchive.Visible = True
            hlExcelExport.Visible = True

            ProcessSelectedArchiveSearch()
        End If
    End Sub

    Private Sub btnDone_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnDone.Click
        Response.Redirect("Home.aspx")
    End Sub

    Private Sub grdBlockArchive_ItemCommand(ByVal source As System.Object, ByVal e As System.Web.UI.WebControls.DataGridCommandEventArgs) Handles grdBlockArchive.ItemCommand
        Dim iCount As Int32 = e.Item.ItemIndex

        If e.CommandName = "ExpandTissues" Then
            If CType(e.Item.Cells(5).Controls(0), LinkButton).Text = "<img src=""Images/minus.gif"">" Then
                iCount = iCount + 1
                While grdBlockArchive.Items(iCount).Cells(2).Text = "&nbsp;"
                    grdBlockArchive.Items(iCount).Visible = False
                    iCount = iCount + 1
                    If iCount = grdBlockArchive.Items.Count Then
                        Exit While
                    End If
                End While
                CType(e.Item.Cells(5).Controls(0), LinkButton).Text = "<img src=""Images/plus.gif"">"
            Else
                iCount = iCount + 1
                While grdBlockArchive.Items(iCount).Cells(2).Text = "&nbsp;"
                    grdBlockArchive.Items(iCount).Visible = True
                    iCount = iCount + 1
                    If iCount = grdBlockArchive.Items.Count Then
                        Exit While
                    End If
                End While
                CType(e.Item.Cells(5).Controls(0), LinkButton).Text = "<img src=""Images/minus.gif"">"
            End If
        End If
    End Sub

    Private Sub grdTissueArchive_ItemCommand(ByVal source As System.Object, ByVal e As System.Web.UI.WebControls.DataGridCommandEventArgs) Handles grdTissueArchive.ItemCommand
        Dim iCount As Int32 = e.Item.ItemIndex

        If e.CommandName = "ExpandTissues" Then
            If CType(e.Item.Cells(1).Controls(0), LinkButton).Text = "<img src=""Images/minus.gif"">" Then
                iCount = iCount + 1
                While grdTissueArchive.Items(iCount).Cells(0).Text = "&nbsp;"
                    grdTissueArchive.Items(iCount).Visible = False
                    iCount = iCount + 1
                    If iCount = grdTissueArchive.Items.Count Then
                        Exit While
                    End If
                End While
                CType(e.Item.Cells(1).Controls(0), LinkButton).Text = "<img src=""Images/plus.gif"">"
            Else
                iCount = iCount + 1
                While grdTissueArchive.Items(iCount).Cells(0).Text = "&nbsp;"
                    grdTissueArchive.Items(iCount).Visible = True
                    iCount = iCount + 1
                    If iCount = grdTissueArchive.Items.Count Then
                        Exit While
                    End If
                End While
                CType(e.Item.Cells(1).Controls(0), LinkButton).Text = "<img src=""Images/minus.gif"">"
            End If
        End If
    End Sub

    Private Sub grdSlideArchive_ItemCommand(ByVal source As System.Object, ByVal e As System.Web.UI.WebControls.DataGridCommandEventArgs) Handles grdSlideArchive.ItemCommand
        Dim iCount As Int32 = e.Item.ItemIndex

        If e.CommandName = "ExpandTissues" Then
            If CType(e.Item.Cells(5).Controls(0), LinkButton).Text = "<img src=""Images/minus.gif"">" Then
                iCount = iCount + 1
                While grdSlideArchive.Items(iCount).Cells(0).Text = "&nbsp;"
                    grdSlideArchive.Items(iCount).Visible = False
                    iCount = iCount + 1
                    If iCount = grdSlideArchive.Items.Count Then
                        Exit While
                    End If
                End While
                CType(e.Item.Cells(5).Controls(0), LinkButton).Text = "<img src=""Images/plus.gif"">"
            Else
                iCount = iCount + 1
                While grdSlideArchive.Items(iCount).Cells(0).Text = "&nbsp;"
                    grdSlideArchive.Items(iCount).Visible = True
                    iCount = iCount + 1
                    If iCount = grdSlideArchive.Items.Count Then
                        Exit While
                    End If
                End While
                CType(e.Item.Cells(5).Controls(0), LinkButton).Text = "<img src=""Images/minus.gif"">"
            End If
        End If
    End Sub

    Private Sub rbTissues_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles rbTissues.CheckedChanged
        rbBlock.Checked = False
        txtBlockRef.Text = ""
        rbSlide.Checked = False
        txtBlockRef.Enabled = False
        ddlTissue.Enabled = True
    End Sub

    Private Sub rbBlock_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles rbBlock.CheckedChanged
        rbTissues.Checked = False
        rbSlide.Checked = False
        SelectItemInDropDownList(ddlTissue, "")
        ddlTissue.Enabled = False
        txtBlockRef.Enabled = True
    End Sub

    Private Sub rbSlide_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles rbSlide.CheckedChanged
        rbBlock.Checked = False
        rbTissues.Checked = False
        txtBlockRef.Text = ""
        SelectItemInDropDownList(ddlTissue, "")
        txtBlockRef.Enabled = False
        ddlTissue.Enabled = False
    End Sub

    Private Sub lbExpandAll_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lbExpandAll.Click
        If rbBlock.Checked = True Then
            SetHierarchicalBlockArchive(True)
        ElseIf rbTissues.Checked = True Then
            SetHierarchicalTissueArchive(True)
        Else
            SetHierarchicalSlideArchive(True)
        End If
    End Sub

    Private Sub lbCollapseAll_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lbCollapseAll.Click
        If rbBlock.Checked = True Then
            SetHierarchicalBlockArchive(False)
        ElseIf rbTissues.Checked = True Then
            SetHierarchicalTissueArchive(False)
        Else
            SetHierarchicalSlideArchive(False)
        End If
    End Sub

#End Region

   
#Region "Grid Related"

    Public Shared Sub SetPrimaryKey(ByRef dt As DataTable, _
                                        ByVal sKeyField As String, _
                                        Optional ByVal bAutoIncrement As Boolean = False)

        Dim KeyCol As DataColumn = dt.Columns(sKeyField)
        If bAutoIncrement Then
            KeyCol.AutoIncrement = True
            KeyCol.AutoIncrementSeed = -1
            KeyCol.AutoIncrementStep = -1
        End If
        dt.PrimaryKey = New DataColumn() {KeyCol}

    End Sub

    Private Sub InsertBlockRow(ByRef dtDisplayGrid As DataTable, ByVal drCopyRow As DataRow)
        Dim drNewRow As DataRow

        drNewRow = dtDisplayGrid.NewRow()
        drNewRow("ID") = DBNull.Value
        drNewRow("BlockRef") = ""
        drNewRow("ArchivedDate") = DBNull.Value
        drNewRow("ArchiveComment") = ""
        drNewRow("ArchiveLocation") = ""
        drNewRow("TissueDescription") = drCopyRow("TissueDescription").ToString()
        drNewRow("NoPieces") = drCopyRow("NoPieces").ToString()
        dtDisplayGrid.Rows.Add(drNewRow)
    End Sub

    Private Sub InsertTissueRow(ByRef dtDisplayGrid As DataTable, ByVal drCopyRow As DataRow)
        Dim drNewRow As DataRow

        drNewRow = dtDisplayGrid.NewRow()
        drNewRow("BatchID") = DBNull.Value
        drNewRow("ArchivedDate") = drCopyRow("ArchivedDate")
        drNewRow("ArchiveLocation") = drCopyRow("ArchiveLocation").ToString()
        drNewRow("TissueDescription") = drCopyRow("TissueDescription").ToString()
        drNewRow("NoPieces") = drCopyRow("NoPieces").ToString()
        dtDisplayGrid.Rows.Add(drNewRow)
    End Sub

    Private Sub InsertBlockMarkerRow(ByRef dtDisplayGrid As DataTable, ByVal drCopyRow As DataRow)
        Dim drNewRow As DataRow

        drNewRow = dtDisplayGrid.NewRow()
        drNewRow("ID") = drCopyRow("ID")
        drNewRow("BlockRef") = drCopyRow("BlockRef")
        drNewRow("ArchivedDate") = drCopyRow("ArchivedDate")
        drNewRow("ArchiveLocation") = drCopyRow("ArchiveLocation").ToString()
        drNewRow("ArchiveComment") = drCopyRow("ArchiveComment").ToString()
        drNewRow("TissueDescription") = ""
        drNewRow("NoPieces") = DBNull.Value
        dtDisplayGrid.Rows.Add(drNewRow)
    End Sub

    Private Sub InsertTissueMarkerRow(ByRef dtDisplayGrid As DataTable, ByVal drCopyRow As DataRow)
        Dim drNewRow As DataRow

        drNewRow = dtDisplayGrid.NewRow()
        drNewRow("BatchID") = drCopyRow("BatchID")
        drNewRow("ArchivedDate") = DBNull.Value
        drNewRow("ArchiveLocation") = ""
        drNewRow("TissueDescription") = ""
        drNewRow("NoPieces") = DBNull.Value
        dtDisplayGrid.Rows.Add(drNewRow)
    End Sub

    Private Sub InsertSlideRow(ByRef dtDisplayGrid As DataTable, ByVal drCopyRow As DataRow)
        Dim drNewRow As DataRow

        drNewRow = dtDisplayGrid.NewRow()
        drNewRow("BatchID") = DBNull.Value
        drNewRow("BlockRef") = ""
        drNewRow("ArchivedDate") = DBNull.Value
        drNewRow("ArchiveLocation") = ""
        drNewRow("Description") = ""
        drNewRow("TissueDescription") = drCopyRow("TissueDescription")
        drNewRow("NoPieces") = DBNull.Value
        dtDisplayGrid.Rows.Add(drNewRow)
    End Sub

    Private Sub InsertSlideMarkerRow(ByRef dtDisplayGrid As DataTable, ByVal drCopyRow As DataRow)
        Dim drNewRow As DataRow

        drNewRow = dtDisplayGrid.NewRow()
        drNewRow("BatchID") = drCopyRow("BatchID")
        drNewRow("BlockRef") = drCopyRow("BlockRef")
        drNewRow("ArchivedDate") = drCopyRow("ArchivedDate")
        drNewRow("ArchiveLocation") = drCopyRow("ArchiveLocation").ToString()
        drNewRow("Description") = drCopyRow("Description").ToString()
        drNewRow("TissueDescription") = ""
        drNewRow("NoPieces") = DBNull.Value
        dtDisplayGrid.Rows.Add(drNewRow)
    End Sub

    Private Function CreateSlideArchiveDataTable(ByVal dtSlideData As DataTable) As DataTable
        Try
            Dim dtDisplayGrid As DataTable
            Dim iCount As Integer = 0
            Dim drNewRow As DataRow
            Dim iInnerCount As Integer = 0
            Dim iCountRepeat As Integer = 0

            dtDisplayGrid = dtSlideData.Clone()
            dtDisplayGrid.Columns.Add("NewID", System.Type.GetType("System.Int32"))
            SetPrimaryKey(dtDisplayGrid, "NewID", True)

            'If only one item 
            If dtSlideData.Rows.Count = 1 Then
                InsertSlideMarkerRow(dtDisplayGrid, dtSlideData.Rows(0))
                InsertSlideRow(dtDisplayGrid, dtSlideData.Rows(0))
                Return dtDisplayGrid
            End If

            For iCount = 0 To dtSlideData.Rows.Count - 1
                If iCount = dtSlideData.Rows.Count - 1 Then
                    InsertSlideMarkerRow(dtDisplayGrid, dtSlideData.Rows(iInnerCount))
                    InsertSlideRow(dtDisplayGrid, dtSlideData.Rows(iInnerCount))
                Else
                    For iInnerCount = iCount + 1 To dtSlideData.Rows.Count - 1
                        If iInnerCount = dtSlideData.Rows.Count Then
                            InsertSlideRow(dtDisplayGrid, dtSlideData.Rows(iInnerCount))
                        Else
                            InsertSlideMarkerRow(dtDisplayGrid, dtSlideData.Rows(iCount))

                            For iCountRepeat = iCount To dtSlideData.Rows.Count - 1
                                If iCountRepeat = dtSlideData.Rows.Count - 1 Then
                                    InsertSlideRow(dtDisplayGrid, dtSlideData.Rows(iCountRepeat))
                                Else
                                    If Not dtSlideData.Rows(iCountRepeat)("Description") = dtSlideData.Rows(iCountRepeat + 1)("Description") Then
                                        ' If Not dtSlideData.Rows(iCountRepeat)("BlockRef") = dtSlideData.Rows(iCountRepeat + 1)("BlockRef") Then
                                        InsertSlideRow(dtDisplayGrid, dtSlideData.Rows(iCountRepeat))
                                        'End If
                                        Exit For
                                    ElseIf Not dtSlideData.Rows(iCountRepeat)("BlockRef") = dtSlideData.Rows(iCountRepeat + 1)("BlockRef") Then
                                        InsertSlideRow(dtDisplayGrid, dtSlideData.Rows(iCountRepeat))
                                        Exit For
                                    ElseIf Not dtSlideData.Rows(iCountRepeat)("BatchID") = dtSlideData.Rows(iCountRepeat + 1)("BatchID") Then
                                        InsertSlideRow(dtDisplayGrid, dtSlideData.Rows(iCountRepeat))
                                        Exit For
                                    Else
                                        InsertSlideRow(dtDisplayGrid, dtSlideData.Rows(iCountRepeat))
                                    End If
                                End If
                            Next
                            iCount = iCountRepeat
                            Exit For
                        End If
                    Next
                End If
            Next

            Return dtDisplayGrid
        Catch ex As Exception
            clsAppError.DisplayError("Error creating block archive information grid.", ex)
        End Try
    End Function

    Private Function CreateBlockArchiveDataTable(ByVal dtBlockData As DataTable) As DataTable
        Try
            Dim dtDisplayGrid As DataTable
            Dim iCount As Integer = 0
            Dim drNewRow As DataRow
            Dim iInnerCount As Integer = 0
            Dim iCountRepeat As Integer = 0

            dtDisplayGrid = dtBlockData.Clone()
            dtDisplayGrid.Columns.Add("NewID", System.Type.GetType("System.Int32"))
            SetPrimaryKey(dtDisplayGrid, "NewID", True)

            'If only one item 
            If dtBlockData.Rows.Count = 1 Then
                InsertBlockMarkerRow(dtDisplayGrid, dtBlockData.Rows(0))
                InsertBlockRow(dtDisplayGrid, dtBlockData.Rows(0))
                Return dtDisplayGrid
            End If

            For iCount = 0 To dtBlockData.Rows.Count - 1
                If iCount = dtBlockData.Rows.Count - 1 Then
                    InsertBlockMarkerRow(dtDisplayGrid, dtBlockData.Rows(iInnerCount))
                    InsertBlockRow(dtDisplayGrid, dtBlockData.Rows(iInnerCount))
                Else
                    For iInnerCount = iCount + 1 To dtBlockData.Rows.Count - 1
                        If iInnerCount = dtBlockData.Rows.Count Then
                            InsertBlockRow(dtDisplayGrid, dtBlockData.Rows(iInnerCount))
                        Else
                            InsertBlockMarkerRow(dtDisplayGrid, dtBlockData.Rows(iCount))

                            For iCountRepeat = iCount To dtBlockData.Rows.Count - 1
                                If iCountRepeat = dtBlockData.Rows.Count - 1 Then
                                    InsertBlockRow(dtDisplayGrid, dtBlockData.Rows(iCountRepeat))
                                Else
                                    If Not dtBlockData.Rows(iCountRepeat)("BlockRef") = dtBlockData.Rows(iCountRepeat + 1)("BlockRef") Or _
                                           dtBlockData.Rows(iCountRepeat)("ID") = dtBlockData.Rows(iCountRepeat + 1)("ID") Then
                                        InsertBlockRow(dtDisplayGrid, dtBlockData.Rows(iCountRepeat))
                                        Exit For
                                    Else
                                        InsertBlockRow(dtDisplayGrid, dtBlockData.Rows(iCountRepeat))
                                    End If
                                End If
                            Next
                            iCount = iCountRepeat
                            Exit For
                        End If
                    Next
                End If
            Next

            Return dtDisplayGrid
        Catch ex As Exception
            clsAppError.DisplayError("Error creating block archive information grid.", ex)
        End Try


    End Function

    Private Function CreateTissueArchiveDataTable(ByVal dtTissueData As DataTable) As DataTable
        Try
            Dim dtDisplayGrid As DataTable
            Dim iCount As Integer = 0
            Dim drNewRow As DataRow
            Dim iInnerCount As Integer = 0
            Dim iCountRepeat As Integer = 0

            dtDisplayGrid = dtTissueData.Clone()
            dtDisplayGrid.Columns.Add("NewID", System.Type.GetType("System.Int32"))
            SetPrimaryKey(dtDisplayGrid, "NewID", True)

            'If only one item 
            If dtTissueData.Rows.Count = 1 Then
                InsertTissueMarkerRow(dtDisplayGrid, dtTissueData.Rows(0))
                InsertTissueRow(dtDisplayGrid, dtTissueData.Rows(0))
                Return dtDisplayGrid
            End If

            For iCount = 0 To dtTissueData.Rows.Count - 1
                If iCount = dtTissueData.Rows.Count - 1 Then
                    InsertTissueMarkerRow(dtDisplayGrid, dtTissueData.Rows(iInnerCount))
                    InsertTissueRow(dtDisplayGrid, dtTissueData.Rows(iInnerCount))
                Else
                    For iInnerCount = iCount + 1 To dtTissueData.Rows.Count - 1
                        If iInnerCount = dtTissueData.Rows.Count Then
                            InsertTissueRow(dtDisplayGrid, dtTissueData.Rows(iInnerCount))
                        Else
                            InsertTissueMarkerRow(dtDisplayGrid, dtTissueData.Rows(iCount))

                            For iCountRepeat = iCount To dtTissueData.Rows.Count - 1
                                If iCountRepeat = dtTissueData.Rows.Count - 1 Then
                                    InsertTissueRow(dtDisplayGrid, dtTissueData.Rows(iCountRepeat))
                                Else
                                    If Not dtTissueData.Rows(iCountRepeat)("BatchID") = dtTissueData.Rows(iCountRepeat + 1)("BatchID") Then
                                        InsertTissueRow(dtDisplayGrid, dtTissueData.Rows(iCountRepeat))
                                        Exit For
                                    Else
                                        InsertTissueRow(dtDisplayGrid, dtTissueData.Rows(iCountRepeat))
                                    End If
                                End If
                            Next
                            iCount = iCountRepeat
                            Exit For
                        End If
                    Next
                End If
            Next


            Return dtDisplayGrid
        Catch ex As Exception
            clsAppError.DisplayError("Error creating tissue archive information grid.", ex)
        End Try
    End Function

    Private Sub ProcessSelectedArchiveSearch()
        Try
            Dim dtDisplayGrid As DataTable
            Dim objAnimal As New HistopathologyLib.clsAnimal()
            Dim dtData As New DataTable()
            Dim dvExportView As DataView

            If rbBlock.Checked = True Then
                If Not objAnimal.GetAnimalBlockArchiveInformation(dtData, _
                                                              txtSenderRef.Text, _
                                                              txtHistRef.Text, _
                                                              txtBlockRef.Text, _
                                                              ddlArchiveLocation.SelectedItem.Value) Then
                    Throw New Exception("Animal.GetAnimalBlockArchiveInformation returned false.")
                End If

                dtData.TableName = "Animal Block Archive"
                dvExportView = dtData.DefaultView
                Session.Item(SessionVars.SV_ExcelExport) = dtData
                Session.Item(SessionVars.SV_ExcelExportView) = dvExportView

                dtDisplayGrid = CreateBlockArchiveDataTable(dtData)

                FillBlockViewGrid(dtDisplayGrid)
            ElseIf rbTissues.Checked = True Then
                If Not objAnimal.GetAnimalTissuesArchiveInformation(dtData, _
                                                             txtSenderRef.Text, _
                                                             txtHistRef.Text, _
                                                             ddlArchiveLocation.SelectedItem.Value, _
                                                             ddlTissue.SelectedItem.Value) Then
                    Throw New Exception("Animal.GetAnimalBlockArchiveInformation returned false.")
                End If

                dtData.TableName = "Animal Tissue Archive"
                dvExportView = dtData.DefaultView
                Session.Item(SessionVars.SV_ExcelExport) = dtData
                Session.Item(SessionVars.SV_ExcelExportView) = dvExportView

                dtDisplayGrid = CreateTissueArchiveDataTable(dtData)

                FillTissuesViewGrid(dtDisplayGrid)
            Else
                If Not objAnimal.GetAnimalSlideArchiveInformation(dtData, _
                                                                  txtSenderRef.Text, _
                                                                  txtHistRef.Text, _
                                                                  ddlArchiveLocation.SelectedItem.Value) Then
                    Throw New Exception("Animal.GetAnimalSlideArchiveInformation returned false.")
                End If

                dtData.TableName = "Animal Slide Archive"
                dvExportView = dtData.DefaultView
                Session.Item(SessionVars.SV_ExcelExport) = dtData
                Session.Item(SessionVars.SV_ExcelExportView) = dvExportView

                dtDisplayGrid = CreateSlideArchiveDataTable(dtData)

                FillSlideViewGrid(dtDisplayGrid)
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Error processing selected archive search.", ex)
        End Try
    End Sub

    Private Sub FillSlideViewGrid(ByVal dtDisplayGrid As DataTable)
        Try

            Dim dvSlideRefsView As DataView

            If Not dtDisplayGrid Is Nothing Then
                ctlDivTissueArchive.Visible = False
                ctlDivBlockArchive.Visible = False
                ctlDivSlideArchive.Visible = True

                Session(SessionVars.SV_SearchBatchDetailsTable) = dtDisplayGrid
                dvSlideRefsView = dtDisplayGrid.DefaultView
                Session(SessionVars.SV_SearchBatchDetailsView) = dvSlideRefsView

                ' initialise the grid
                grdSlideArchive.DataSource = dtDisplayGrid
                grdSlideArchive.DataKeyField = "NewID"
                grdSlideArchive.CurrentPageIndex = 0
                grdSlideArchive.SelectedIndex = -1
                grdSlideArchive.EditItemIndex = -1
                grdSlideArchive.DataBind()

                SetHierarchicalSlideArchive(True)
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Error initialising the Search Block Archive grid.", ex)
        End Try
    End Sub

    Private Sub FillBlockViewGrid(ByVal dtDisplayGrid As DataTable)
        Try

            Dim dvBlockRefsView As DataView

            If Not dtDisplayGrid Is Nothing Then
                ctlDivTissueArchive.Visible = False
                ctlDivBlockArchive.Visible = True
                ctlDivSlideArchive.Visible = False

                Session(SessionVars.SV_SearchBatchDetailsTable) = dtDisplayGrid
                dvBlockRefsView = dtDisplayGrid.DefaultView
                Session(SessionVars.SV_SearchBatchDetailsView) = dvBlockRefsView

                ' initialise the grid
                grdBlockArchive.DataSource = dtDisplayGrid
                grdBlockArchive.DataKeyField = "NewID"
                grdBlockArchive.CurrentPageIndex = 0
                grdBlockArchive.SelectedIndex = -1
                grdBlockArchive.EditItemIndex = -1
                grdBlockArchive.DataBind()

                SetHierarchicalBlockArchive(True)
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Error initialising the Search Block Archive grid.", ex)
        End Try
    End Sub

    Private Sub FillTissuesViewGrid(ByVal dtDisplayGrid As DataTable)
        Try

            Dim dvTissuesRefsView As DataView

            If Not dtDisplayGrid Is Nothing Then
                ctlDivBlockArchive.Visible = False
                ctlDivTissueArchive.Visible = True
                ctlDivSlideArchive.Visible = False

                Session(SessionVars.SV_SearchBatchDetailsTable) = dtDisplayGrid
                dvTissuesRefsView = dtDisplayGrid.DefaultView
                Session(SessionVars.SV_SearchBatchDetailsView) = dvTissuesRefsView

                ' initialise the grid
                grdTissueArchive.DataSource = dtDisplayGrid
                grdTissueArchive.DataKeyField = "NewID"
                grdTissueArchive.CurrentPageIndex = 0
                grdTissueArchive.SelectedIndex = -1
                grdTissueArchive.EditItemIndex = -1
                grdTissueArchive.DataBind()

                SetHierarchicalTissueArchive(True)
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Error initialising the Search Block Archive grid.", ex)
        End Try
    End Sub

    Private Sub SetHierarchicalBlockArchive(ByVal bExpanded As Boolean)
        Dim iCount As Int32

        If Not bExpanded Then
            For iCount = 0 To grdBlockArchive.Items.Count - 1
                'If row has tissue details display the row with no +/- sign
                If Not grdBlockArchive.Items(iCount).Cells(6).Text = "&nbsp;" Then
                    grdBlockArchive.Items(iCount).Cells(5).Controls(0).Visible = False
                Else
                    CType(grdBlockArchive.Items(iCount).Cells(5).Controls(0), LinkButton).Text = "<img src=""Images/plus.gif"">"
                    grdBlockArchive.Items(iCount).Cells(5).Controls(0).Visible = True
                End If

                'If block ref is null hide the tissue rows when grid is not expanded
                If grdBlockArchive.Items(iCount).Cells(2).Text = "&nbsp;" Then
                    grdBlockArchive.Items(iCount).Visible = False
                Else
                    grdBlockArchive.Items(iCount).Visible = True
                End If
            Next

        Else
            For iCount = 0 To grdBlockArchive.Items.Count - 1
                If Not grdBlockArchive.Items(iCount).Cells(6).Text = "&nbsp;" Then
                    grdBlockArchive.Items(iCount).Cells(5).Controls(0).Visible = False
                Else
                    CType(grdBlockArchive.Items(iCount).Cells(5).Controls(0), LinkButton).Text = "<img src=""Images/minus.gif"">"
                    grdBlockArchive.Items(iCount).Cells(5).Controls(0).Visible = True
                End If

                If grdBlockArchive.Items(iCount).Cells(2).Text = "&nbsp;" Then
                    grdBlockArchive.Items(iCount).Visible = True
                Else
                    grdBlockArchive.Items(iCount).Visible = True
                End If
            Next
        End If
    End Sub

    Private Sub SetHierarchicalTissueArchive(ByVal bExpanded As Boolean)
        Dim iCount As Int32

        If Not bExpanded Then
            For iCount = 0 To grdTissueArchive.Items.Count - 1
                'If row has tissue details display the row with no +/- sign
                If Not grdTissueArchive.Items(iCount).Cells(2).Text = "&nbsp;" Then
                    grdTissueArchive.Items(iCount).Cells(1).Controls(0).Visible = False
                Else
                    CType(grdTissueArchive.Items(iCount).Cells(1).Controls(0), LinkButton).Text = "<img src=""Images/plus.gif"">"
                    grdTissueArchive.Items(iCount).Cells(1).Controls(0).Visible = True
                End If

                'If block ref is null hide the tissue rows when grid is not expanded
                If grdTissueArchive.Items(iCount).Cells(0).Text = "&nbsp;" Then
                    grdTissueArchive.Items(iCount).Visible = False
                Else
                    grdTissueArchive.Items(iCount).Visible = True
                End If
            Next

        Else
            For iCount = 0 To grdTissueArchive.Items.Count - 1
                If Not grdTissueArchive.Items(iCount).Cells(2).Text = "&nbsp;" Then
                    grdTissueArchive.Items(iCount).Cells(1).Controls(0).Visible = False
                Else
                    CType(grdTissueArchive.Items(iCount).Cells(1).Controls(0), LinkButton).Text = "<img src=""Images/minus.gif"">"
                    grdTissueArchive.Items(iCount).Cells(1).Controls(0).Visible = True
                End If

                If grdTissueArchive.Items(iCount).Cells(0).Text = "&nbsp;" Then
                    grdTissueArchive.Items(iCount).Visible = True
                Else
                    grdTissueArchive.Items(iCount).Visible = True
                End If
            Next
        End If
    End Sub

    Private Sub SetHierarchicalSlideArchive(ByVal bExpanded As Boolean)
        Dim iCount As Int32

        If Not bExpanded Then
            For iCount = 0 To grdSlideArchive.Items.Count - 1
                'If row has tissue details display the row with no +/- sign
                If Not grdSlideArchive.Items(iCount).Cells(6).Text = "&nbsp;" Then
                    grdSlideArchive.Items(iCount).Cells(5).Controls(0).Visible = False
                Else
                    CType(grdSlideArchive.Items(iCount).Cells(5).Controls(0), LinkButton).Text = "<img src=""Images/plus.gif"">"
                    grdSlideArchive.Items(iCount).Cells(5).Controls(0).Visible = True
                End If

                'If block ref is null hide the tissue rows when grid is not expanded
                If grdSlideArchive.Items(iCount).Cells(0).Text = "&nbsp;" Then
                    grdSlideArchive.Items(iCount).Visible = False
                Else
                    grdSlideArchive.Items(iCount).Visible = True
                End If
            Next

        Else
            For iCount = 0 To grdSlideArchive.Items.Count - 1
                If Not grdSlideArchive.Items(iCount).Cells(6).Text = "&nbsp;" Then
                    grdSlideArchive.Items(iCount).Cells(5).Controls(0).Visible = False
                Else
                    CType(grdSlideArchive.Items(iCount).Cells(5).Controls(0), LinkButton).Text = "<img src=""Images/minus.gif"">"
                    grdSlideArchive.Items(iCount).Cells(5).Controls(0).Visible = True
                End If

                If grdSlideArchive.Items(iCount).Cells(0).Text = "&nbsp;" Then
                    grdSlideArchive.Items(iCount).Visible = True
                Else
                    grdSlideArchive.Items(iCount).Visible = True
                End If
            Next
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

    Private Sub LoadLookupLists()
        Try
            Dim objLookup As New HistopathologyLib.LookupData()
            Dim dtData As DataTable

            dtData = objLookup.GetLookupData(LOOKUP_ARCHIVE_LOCATION)
            If Not dtData Is Nothing Then
                ddlArchiveLocation.DataSource = dtData
                ddlArchiveLocation.DataTextField = "Description"
                ddlArchiveLocation.DataValueField = "Code"
                ddlArchiveLocation.DataBind()
                AddItemToDropDownList(ddlArchiveLocation)
            End If

            dtData = objLookup.GetLookupData(LOOKUP_TISSUE_CODE)
            If Not dtData Is Nothing Then
                ddlTissue.DataSource = dtData
                ddlTissue.DataTextField = "Description"
                ddlTissue.DataValueField = "Code"
                ddlTissue.DataBind()
                AddItemToDropDownList(ddlTissue)
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Error initialising the lookup lists on the Search Archive Location page.", ex)
        End Try
    End Sub


#End Region

#Region "Validation"

    Private Function SetClientValidation() As Boolean
        If HttpContext.Current.Request.Browser.JavaScript Then
            Dim scr As New System.Text.StringBuilder()

            scr.Append("<SCRIPT language=""Javascript"">" + vbNewLine)
            scr.Append("function ClientValidateBlockRef(sender, args)" + vbNewLine)
            scr.Append("{" + vbNewLine)
            scr.Append("    var sBlockRef = args.Value;" + vbNewLine)
            scr.Append("    if (sBlockRef.length <=2)" + vbNewLine)
            scr.Append("    {" + vbNewLine)
            scr.Append("        var expBlockRef = /[0-9][0-9]/;" + vbNewLine)
            scr.Append("        if (expBlockRef.test(sBlockRef))" + vbNewLine)
            scr.Append("        {" + vbNewLine)
            scr.Append("            args.IsValid = true;" + vbNewLine)
            scr.Append("            return;" + vbNewLine)
            scr.Append("        }" + vbNewLine)
            scr.Append("        else" + vbNewLine)
            scr.Append("        {" + vbNewLine)
            scr.Append("            args.IsValid = false;" + vbNewLine)
            scr.Append("            return;" + vbNewLine)
            scr.Append("        }" + vbNewLine)
            scr.Append("    }" + vbNewLine)
            scr.Append("    else" + vbNewLine)
            scr.Append("    {" + vbNewLine)
            scr.Append("        var expBlockRef = /[1-9][0-9][0-9]/;" + vbNewLine)
            scr.Append("        if (expBlockRef.test(sBlockRef))" + vbNewLine)
            scr.Append("        {" + vbNewLine)
            scr.Append("            args.IsValid = true;" + vbNewLine)
            scr.Append("            return;" + vbNewLine)
            scr.Append("        }" + vbNewLine)
            scr.Append("        else" + vbNewLine)
            scr.Append("        {" + vbNewLine)
            scr.Append("            args.IsValid = false;" + vbNewLine)
            scr.Append("            return;" + vbNewLine)
            scr.Append("        }" + vbNewLine)
            scr.Append("    }" + vbNewLine)
            scr.Append("}" + vbNewLine)
            scr.Append("</SCRIPT>" + vbNewLine)

            Me.Page.RegisterClientScriptBlock("SetBlockRefClientValidation", scr.ToString())
            Return True
        Else
            Return False
        End If
    End Function

    Public Sub ValidateBlockRefRef(ByVal sender As Object, ByVal args As ServerValidateEventArgs)
        Dim sBlockRef As String = CStr(args.Value)
        Dim match As Match

        If sBlockRef.Length <= 2 Then
            Dim revBlockRef As Regex = New Regex("[0-9][0-9]")
            match = revBlockRef.Match(sBlockRef)

            If match.Success Then
                args.IsValid = True
                Exit Sub
            Else
                args.IsValid = False
                Exit Sub
            End If
        Else
            Dim revBlockRef As Regex = New Regex("[1-9][0-9][0-9]")
            match = revBlockRef.Match(sBlockRef)

            If match.Success Then
                args.IsValid = True
                Exit Sub
            Else
                args.IsValid = False
                Exit Sub
            End If
        End If

    End Sub

#End Region

  
End Class
