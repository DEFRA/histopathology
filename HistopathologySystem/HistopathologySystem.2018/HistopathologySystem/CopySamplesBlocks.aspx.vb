Partial Class CopySamplesBlocks
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
#Region " Web Form Designer Generated Code "

    'This call is required by the Web Form Designer.
    <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()

    End Sub

    'NOTE: The following placeholder declaration is required by the Web Form Designer.
    'Do not delete or move it.
    Private designerPlaceholderDeclaration As System.Object

    Private Sub Page_Init(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Init
        'CODEGEN: This method call is required by the Web Form Designer
        'Do not modify it using the code editor.
        InitializeComponent()
    End Sub

#End Region

    Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        VLAHeader1.PageTitle = "Sample Blocks"
        CheckPermissions()

        If Not IsPostBack Then
            InitialiseSummaryGrid()
            PromptBeforeSaveScript("Are you sure you want to copy the selected blocks?", btnFinish)
            SelectAll(True)
        End If

    End Sub

#Region "Grid Handling"

    Private Sub InitialiseSummaryGrid()
        Try
            Dim dsDataSet As DataSet = CType(Session.Item(SessionVars.SV_OldBatchDetails), DataSet)
            Dim objSummary As New HistopathologyLib.clsBatchSummary
            Dim dtSummary As New DataTable
            Dim sFilter As String = ""
            Dim dtTissuesList As DataTable = Common.GetLookupTypeList(LOOKUP_TISSUE_CODE)

            Dim iAnimalID As Integer = CType(Session.Item(SessionVars.SV_AnimalIDs), Integer)
            sFilter = "AnimalID=" & Convert.ToString(iAnimalID)

            If Not objSummary.CreateAnimalSummaryData(dsDataSet, dtSummary, dtTissuesList, sFilter) Then
                Throw New Exception("BatchSummary.CreateAnimalSummaryData return false")
            End If

            ' create a dataview for filtering and sorting
            Dim dv As DataView = dtSummary.DefaultView

            Session.Item(SessionVars.SV_BlockSummaryTable) = dtSummary
            Session.Item(SessionVars.SV_BlockSummaryView) = dv

            grdBlockSummary.DataSource = dtSummary
            grdBlockSummary.DataKeyField = "ID"
            grdBlockSummary.DataBind()
            grdBlockSummary.Enabled = True

            HideColumns(dsDataSet)

            SetHierarchical(False)

        Catch ex As Exception
            clsAppError.DisplayError("Error initialising the Summary Grid, BatchBlocks page.", ex)
        End Try
    End Sub

#End Region

#Region "Private Functions"

    Private Sub CheckPermissions()
        VLAHeader1.getUserDetails()
        Dim sGroupName As String = Session(SessionVars.SV_HeaderGroupName)
        If sGroupName = "Customer" Then
            Response.Redirect("Home.aspx")
        ElseIf sGroupName = "Histopathology User" Then
            'Nothing
        ElseIf sGroupName = "Maintenance" Then
            'Nothing
        Else
            Response.Redirect("Home.aspx")
        End If
    End Sub

    Private Sub HideColumns(ByVal dsSubmission As DataSet)
        'Only display the Histology columns which are relevent to the type of submission
        If Session(SessionVars.SV_SubmissionType) = SUBMISSION_NONTSE Then
            grdBlockSummary.Columns(6).Visible = False
            grdBlockSummary.Columns(7).Visible = False
            grdBlockSummary.Columns(8).Visible = True
        Else
            grdBlockSummary.Columns(6).Visible = True
            grdBlockSummary.Columns(7).Visible = True
            grdBlockSummary.Columns(8).Visible = False
        End If
    End Sub

    Private Sub SetHierarchical(ByVal bExpanded As Boolean)
        Dim iCount As Int32

        If Not bExpanded Then
            For iCount = 0 To grdBlockSummary.Items.Count - 1
                If grdBlockSummary.Items(iCount).Cells(0).Text = "&nbsp;" Then
                    grdBlockSummary.Items(iCount).Visible = False
                End If

                grdBlockSummary.Items(iCount).Cells(1).Controls(0).Visible = False

                If iCount + 1 <= grdBlockSummary.Items.Count - 1 Then
                    If Not grdBlockSummary.Items(iCount + 1).Cells(2).Text = "&nbsp;" Then
                        grdBlockSummary.Items(iCount).Cells(1).Controls(0).Visible = True
                        CType(grdBlockSummary.Items(iCount).Cells(1).Controls(0), LinkButton).Text = "<img src=""Images/plus.gif"">"
                    End If
                End If
            Next

        Else
            For iCount = 0 To grdBlockSummary.Items.Count - 1
                'If the sender ref is blank in the datatable it is a expandable tissue row
                If grdBlockSummary.Items(iCount).Cells(0).Text = "&nbsp;" Then
                    grdBlockSummary.Items(iCount).Visible = True
                    grdBlockSummary.Items(iCount).Cells(1).Controls(0).Visible = False
                    HideControls(iCount)
                End If

                If iCount + 1 <= grdBlockSummary.Items.Count - 1 Then
                    'if the next row tissue is blank its a sender ref row
                    If Not grdBlockSummary.Items(iCount + 1).Cells(2).Text = "&nbsp;" Then
                        CType(grdBlockSummary.Items(iCount).Cells(1).Controls(0), LinkButton).Text = "<img src=""Images/minus.gif"">"
                    End If
                End If
            Next
        End If
    End Sub

    Private Sub HideControls(ByVal iCount As Integer)
        Dim strGridPart As String
        Dim strEO As String
        Dim strHE As String
        Dim strHEBse As String
        Dim strSpecialStain As String
        Dim strIHCPrp As String
        Dim strIHCOther As String
        Dim strArchive As String
        Dim strSelected As String
        Dim cbEO As CheckBox
        Dim cbHE As CheckBox
        Dim cbHEBSE As CheckBox
        Dim cbSpecialStain As CheckBox
        Dim cbIHCPrp As CheckBox
        Dim cbIHCOther As CheckBox
        Dim cbArchive As CheckBox
        Dim cbSelected As CheckBox
        Dim litEO As LiteralControl
        Dim litHE As LiteralControl
        Dim liHEBse As LiteralControl
        Dim liSpecialStain As LiteralControl
        Dim liIHCPrp As LiteralControl
        Dim liIHCOther As LiteralControl
        Dim liSelected As LiteralControl

        grdBlockSummary.Items(iCount).Visible = True
        grdBlockSummary.Items(iCount).Cells(1).Controls(0).Visible = False

        litEO = CType(grdBlockSummary.Items(iCount).Cells(3).Controls(0), LiteralControl)
        litHE = CType(grdBlockSummary.Items(iCount).Cells(4).Controls(0), LiteralControl)
        liHEBse = CType(grdBlockSummary.Items(iCount).Cells(5).Controls(0), LiteralControl)
        liSpecialStain = CType(grdBlockSummary.Items(iCount).Cells(6).Controls(0), LiteralControl)
        liIHCPrp = CType(grdBlockSummary.Items(iCount).Cells(7).Controls(0), LiteralControl)
        liIHCOther = CType(grdBlockSummary.Items(iCount).Cells(8).Controls(0), LiteralControl)
        liSelected = CType(grdBlockSummary.Items(iCount).Cells(9).Controls(0), LiteralControl)

        strGridPart = GetGridPart(litEO.UniqueID())
        strEO = strGridPart + "cbEODisplay"
        strHE = strGridPart + "cbHAndEDisplay"
        strHEBse = strGridPart + "cbHAndEBseDisplay"
        strSpecialStain = strGridPart + "cbSpecialStainDisplay"
        strIHCPrp = strGridPart + "cbIHCPrpDisplay"
        strIHCOther = strGridPart + "cbIHCOtherDisplay"
        strArchive = strGridPart + "cbArchiveDisplay"
        strSelected = strGridPart + "cbSelected"
        cbEO = Page.FindControl(strEO)
        cbHE = Page.FindControl(strHE)
        cbHEBSE = Page.FindControl(strHEBse)
        cbSpecialStain = Page.FindControl(strSpecialStain)
        cbIHCPrp = Page.FindControl(strIHCPrp)
        cbIHCOther = Page.FindControl(strIHCOther)
        cbArchive = Page.FindControl(strArchive)
        cbSelected = Page.FindControl(strSelected)

        If Not cbSelected Is Nothing Then
            cbSelected.Visible = False
        End If

        If Not cbArchive Is Nothing Then
            cbArchive.Visible = False
        End If

        If Not cbEO Is Nothing Then
            cbEO.Visible = False
        End If

        If Not cbHE Is Nothing Then
            cbHE.Visible = False
        End If

        If Not cbHEBSE Is Nothing Then
            cbHEBSE.Visible = False
        End If

        If Not cbSpecialStain Is Nothing Then
            cbSpecialStain.Visible = False
        End If

        If Not cbIHCPrp Is Nothing Then
            cbIHCPrp.Visible = False
        End If

        If Not cbIHCOther Is Nothing Then
            cbIHCOther.Visible = False
        End If
    End Sub
#End Region

#Region "Event Handlers"

    Private Sub VLAHeader1_HomeClick(ByVal sender As Object, ByVal e As HistopathologySystem.HomeLinkEventArgs) Handles VLAHeader1.HomeClick
        Dim sMessage As System.Text.StringBuilder = New System.Text.StringBuilder

        sMessage.Append("You are currently assigning tissues to blocks. Any changes you have made since you last saved will be lost. Are you sure you wish to exit without saving?")
        Page.RegisterStartupScript("navigate", PromptBeforeNavigateScript(sMessage.ToString(), "Home.aspx"))
        e.bNavigateHome = False
    End Sub

    Private Sub grdBlockSummary_ItemDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.DataGridItemEventArgs) Handles grdBlockSummary.ItemDataBound
        ' populate template column values here
        Try
            ' set up the checkbox and drop-down columns
            Dim drv As DataRowView = CType(e.Item.DataItem, DataRowView)
            If Not drv Is Nothing Then
                Dim lblFixationCode As Label = Nothing
                Dim cbEO As CheckBox = Nothing
                Dim cbHAndE As CheckBox = Nothing
                Dim cbHAndEBse As CheckBox = Nothing
                Dim cbSpecialStain As CheckBox = Nothing
                Dim cbIHCPrp As CheckBox = Nothing
                Dim cbIHCOther As CheckBox = Nothing
                Dim cbArchive As CheckBox = Nothing

                If e.Item.ItemType = ListItemType.EditItem Then
                ElseIf e.Item.ItemType = ListItemType.Item _
                OrElse e.Item.ItemType = ListItemType.AlternatingItem _
                OrElse e.Item.ItemType = ListItemType.SelectedItem Then
                    ' populate display mode controls
                    lblFixationCode = CType(e.Item.FindControl("lblFixationCodeDisplay"), Label)
                    cbEO = CType(e.Item.FindControl("cbEODisplay"), CheckBox)
                    cbHAndE = CType(e.Item.FindControl("cbHAndEDisplay"), CheckBox)
                    cbHAndEBse = CType(e.Item.FindControl("cbHAndEBseDisplay"), CheckBox)
                    cbSpecialStain = CType(e.Item.FindControl("cbSpecialStainDisplay"), CheckBox)
                    cbIHCPrp = CType(e.Item.FindControl("cbIHCPrpDisplay"), CheckBox)
                    cbIHCOther = CType(e.Item.FindControl("cbIHCOtherDisplay"), CheckBox)
                    cbArchive = CType(e.Item.FindControl("cbArchiveDisplay"), CheckBox)
                End If

                If Not lblFixationCode Is Nothing Then
                    If Not IsDBNull(drv("Fixation")) Then
                        lblFixationCode.Text = GetListType(drv("ation"), LOOKUP_FIXATIVE)
                    Else
                        lblFixationCode.Text = ""
                    End If
                End If

                If Not cbArchive Is Nothing Then
                    If Not IsDBNull(drv("Archive")) Then
                        cbArchive.Checked = drv("Archive")
                    Else
                        cbArchive.Checked = False
                    End If
                End If

                If Not cbEO Is Nothing Then
                    If Not IsDBNull(drv("EO")) Then
                        cbEO.Checked = drv("EO")
                    Else
                        cbEO.Checked = False
                    End If
                End If

                If Not cbHAndE Is Nothing Then
                    If Not IsDBNull(drv("HAndE")) Then
                        cbHAndE.Checked = drv("HAndE")
                    Else
                        cbHAndE.Checked = False
                    End If
                End If

                If Not cbHAndEBse Is Nothing Then
                    If Not IsDBNull(drv("HAndEBSE")) Then
                        cbHAndEBse.Checked = drv("HAndEBSE")
                    Else
                        cbHAndEBse.Checked = False
                    End If
                End If

                If Not cbSpecialStain Is Nothing Then
                    If Not IsDBNull(drv("SpecialStain")) Then
                        cbSpecialStain.Checked = drv("SpecialStain")
                    Else
                        cbSpecialStain.Checked = False
                    End If
                End If

                If Not cbIHCPrp Is Nothing Then
                    If Not IsDBNull(drv("IHCPrp")) Then
                        cbIHCPrp.Checked = drv("IHCPrp")
                    Else
                        cbIHCPrp.Checked = False
                    End If
                End If

                If Not cbIHCOther Is Nothing Then
                    If Not IsDBNull(drv("IHCOther")) Then
                        cbIHCOther.Checked = drv("IHCOther")
                    Else
                        cbIHCOther.Checked = False
                    End If
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to bind template columns in the Batch summary grid", ex)
        End Try
    End Sub

    Private Sub lbExpandAll_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lbExpandAll.Click
        SetHierarchical(True)
    End Sub

    Private Sub lbCollapseAll_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lbCollapseAll.Click
        SetHierarchical(False)
    End Sub

    Private Sub grdBlockSummary_ItemCommand(ByVal source As System.Object, ByVal e As System.Web.UI.WebControls.DataGridCommandEventArgs) Handles grdBlockSummary.ItemCommand
        Dim iCount As Int32 = e.Item.ItemIndex + 1

        If e.CommandName = "ExpandTissues" Then
            If CType(e.Item.Cells(1).Controls(0), LinkButton).Text = "<img src=""Images/minus.gif"">" Then
                Do While Not grdBlockSummary.Items(iCount).Cells(2).Text = "&nbsp;"
                    grdBlockSummary.Items(iCount).Visible = False
                    iCount += 1
                    If iCount >= grdBlockSummary.Items.Count Then Exit Do
                Loop
                CType(e.Item.Cells(1).Controls(0), LinkButton).Text = "<img src=""Images/plus.gif"">"
            Else
                'While the sender ref isnt empty show the tissue rows
                Do While Not grdBlockSummary.Items(iCount).Cells(2).Text = "&nbsp;"
                    HideControls(iCount)
                    iCount += 1
                    If iCount >= grdBlockSummary.Items.Count Then Exit Do
                Loop
                CType(e.Item.Cells(1).Controls(0), LinkButton).Text = "<img src=""Images/minus.gif"">"
            End If
        End If
    End Sub

    Private Sub SelectAll(ByVal bSelectAll As Boolean)
        Dim dgItem As DataGridItem
        Dim cbSelected As CheckBox = Nothing

        cbSelectAll.Checked = bSelectAll

        For Each dgItem In grdBlockSummary.Items
            cbSelected = CType(dgItem.FindControl("cbSelected"), CheckBox)

            If Not cbSelected Is Nothing Then
                cbSelected.Checked = bSelectAll
            End If
        Next

    End Sub

    Private Sub cbSelectAll_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cbSelectAll.CheckedChanged
        Try
            SelectAll(cbSelectAll.Checked)
        Catch ex As Exception
            clsAppError.DisplayError("Failed to select all checkboxes.", ex)
        End Try
    End Sub

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        Session.Item(SessionVars.SV_OldBatchDetails) = Nothing
        Session.Item(SessionVars.SV_CopySampleBlocksSummaryTable) = Nothing
        Session.Item(SessionVars.SV_CopySampleBlocksSummaryView) = Nothing
        Response.Redirect("BatchBlocks.aspx")
    End Sub

    Private Sub btnBack_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnBack.Click
        Response.Redirect("CopySamples.aspx")
    End Sub

    Private Sub btnFinish_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnFinish.Click
        Try
            Dim dsCopyBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_OldBatchDetails), DataSet)
            Dim dsCurrentbatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim iSelectedAnimal As Integer = CInt(Session.Item(SessionVars.SV_AnimalIDs))
            Dim iCurrentAnimal As Integer = CInt(Session.Item(SessionVars.SV_AnimalID))
            Dim iBatchID As Integer = CInt(Session.Item(SessionVars.SV_BatchID))
            Dim iOldBlockID As Integer = CInt(Session.Item(SessionVars.Sv_BlockID))
            Dim dgItem As DataGridItem
            Dim objBlocksIDList As New ArrayList
            Dim iBlockId As Integer
            Dim objBlocks As New HistopathologyLib.clsBlock
            Dim cbSelected As CheckBox = Nothing

            For Each dgItem In grdBlockSummary.Items
                If Not IsDBNull(grdBlockSummary.DataKeys(dgItem.ItemIndex)) Then
                    cbSelected = CType(dgItem.FindControl("cbSelected"), CheckBox)
                    If Not cbSelected Is Nothing Then
                        If cbSelected.Checked = True Then
                            iBlockId = grdBlockSummary.DataKeys(dgItem.ItemIndex)
                            objBlocksIDList.Add(iBlockId)
                        End If
                    End If
                End If
            Next

            If Not objBlocks.CopyBlocksFromPreviousSubmission(dsCopyBatchDetails, dsCurrentbatchDetails, iSelectedAnimal, iCurrentAnimal, objBlocksIDList, iBatchID, iOldBlockID) Then
                Throw New Exception("Blocks.CopyBlocksFromPreviousSubmission returned false.")
            End If
            Session.Item(SessionVars.SV_OldBatchDetails) = Nothing
            Session.Item(SessionVars.SV_CopySampleBlocksSummaryTable) = Nothing
            Session.Item(SessionVars.SV_CopySampleBlocksSummaryView) = Nothing
            Session.Item(SessionVars.Sv_BlockID) = Nothing
        Catch ex As Exception
            clsAppError.DisplayError("Failed to copy blocks to submission.", ex)
        End Try

        Response.Redirect("BatchBlocks.aspx")
    End Sub
#End Region


End Class
