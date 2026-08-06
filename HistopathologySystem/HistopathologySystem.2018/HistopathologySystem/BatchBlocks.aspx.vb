Partial Class BatchBlocks
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents Batch1 As Batch

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
        VLAHeader1.PageTitle = "Submission Block Summary"
        CheckPermissions()

        If Not IsPostBack Then
            'Set this checkbox to true so we can monitor it all tissues have been assigned to
            'blocks for all samples. If not the checkbox.checked will get to false in 
            'dataitembound
            chkAllTissuesAssigned.Checked = True
            Batch1.DisplayDetails()
            InitialiseSummaryGrid()
            DisableEnableControls(False)
            SetToolTips()
            PromptBeforeSaveScript("Are you sure you want to Cancel? Any data you have entered since you last clicked the Done button will be lost.", btnCancel)
            PromptBeforeSaveScript("Are you sure you want to delete the selected sample?", btnDeleteSample)
        End If
    End Sub


#Region "Event Handlers"

    Private Sub btnCopySamples_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCopySamples.Click
        Response.Redirect("CopySamples.aspx")
    End Sub

    Private Sub grdBlockSummary_ItemCommand(ByVal source As System.Object, ByVal e As System.Web.UI.WebControls.DataGridCommandEventArgs) Handles grdBlockSummary.ItemCommand
        Dim iCount As Int32 = e.Item.ItemIndex + 1

        If e.CommandName = "ExpandTissues" Then
            If CType(e.Item.Cells(5).Controls(0), LinkButton).Text = "<img src=""Images/minus.gif"">" Then
                Do While Not grdBlockSummary.Items(iCount).Cells(6).Text = "&nbsp;"
                    grdBlockSummary.Items(iCount).Visible = False
                    iCount += 1
                    If iCount >= grdBlockSummary.Items.Count Then Exit Do
                Loop
                CType(e.Item.Cells(5).Controls(0), LinkButton).Text = "<img src=""Images/plus.gif"">"
            Else
                'While the sender ref isnt empty show the tissue rows
                Do While Not grdBlockSummary.Items(iCount).Cells(6).Text = "&nbsp;"
                    HideControls(iCount)
                    iCount += 1
                    If iCount >= grdBlockSummary.Items.Count Then Exit Do
                Loop
                CType(e.Item.Cells(5).Controls(0), LinkButton).Text = "<img src=""Images/minus.gif"">"
            End If
        End If
    End Sub

    Private Sub btnEditSample_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnEditSample.Click
        Try
            Dim iID As Int32
            If grdBlockSummary.SelectedIndex >= 0 Then
                Dim dtBlocks As DataTable = CType(Session.Item(SessionVars.SV_BlockSummaryTable), DataTable)
                Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
                Dim objAnimal As New HistopathologyLib.clsAnimal
                iID = Convert.ToInt32(grdBlockSummary.DataKeys(grdBlockSummary.SelectedIndex))

                Dim drFoundRow As DataRow()
                Dim sFilter As String

                sFilter = "ID=" & Convert.ToString(iID)
                drFoundRow = dtBlocks.Select(sFilter)

                If Not drFoundRow Is Nothing AndAlso drFoundRow.Length > 0 Then
                    Session.Item(SessionVars.SV_SenderRef) = drFoundRow(0)("SenderRef").ToString()
                    Session.Item(SessionVars.SV_HistologyRef) = drFoundRow(0)("HistologyRef").ToString()
                    Session.Item(SessionVars.SV_AnimalID) = drFoundRow(0)("AnimalID")

                    '----- Pre Booked Block Functionality -----
                    If Not objAnimal.GetPreBookedBlocks(drFoundRow(0)("AnimalID"), dsBatchDetails) Then
                        Throw New Exception("Animal.GetPreBookedBlocks returned false.")
                    End If
                End If

                Session.Item(SessionVars.SV_Editing) = True
                Session.Remove(SessionVars.SV_HistologyRefType)
                Session.Item(SessionVars.SV_CopyBlocks) = False
                Session.Item(SessionVars.SV_AddSamplePrevPage) = "BatchBlocks.aspx"
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to edit submission.", ex)
        End Try

        Response.Redirect("SubmissionDetailsBlock.aspx")
    End Sub

    Private Sub grdBlockSummary_ItemDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.DataGridItemEventArgs) Handles grdBlockSummary.ItemDataBound
        ' populate template column values here
        Try
            ' set up the checkbox and drop-down columns
            Dim drv As DataRowView = CType(e.Item.DataItem, DataRowView)
            If Not drv Is Nothing Then
                Dim cbEO As CheckBox = Nothing
                Dim cbHAndE As CheckBox = Nothing
                Dim cbHAndEBse As CheckBox = Nothing
                Dim cbSpecialStain As CheckBox = Nothing
                Dim cbIHCPrp As CheckBox = Nothing
                Dim cbIHCOther As CheckBox = Nothing

                Dim iBatchID As Integer = CInt(Session.Item(SessionVars.SV_BatchID))
                Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
                Dim dtBatchBlocks As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE)
                Dim dtBlockTissues As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TISSUES)

                Dim foundBlocks As DataRow()
                Dim foundBlockTissues As DataRow()
                Dim sFilter As String
                Dim objTissues As New HistopathologyLib.clsTissue
                Dim objDataTable As DataTable
                Dim iAnimalTissueCount As Integer = 0
                Dim iBlockCount As Integer = 0
                Dim iBlockTissueCount As Integer = 0
                Dim bFoundTissue As Boolean = False
                Dim iAnimalID As Integer = 0
                Dim literal As Literal = Nothing
                Dim cbArchive As CheckBox = Nothing

                If e.Item.ItemType = ListItemType.EditItem Then
                ElseIf e.Item.ItemType = ListItemType.Item _
                OrElse e.Item.ItemType = ListItemType.AlternatingItem _
                OrElse e.Item.ItemType = ListItemType.SelectedItem Then
                    ' populate display mode controls
                    cbEO = CType(e.Item.FindControl("cbEODisplay"), CheckBox)
                    cbHAndE = CType(e.Item.FindControl("cbHAndEDisplay"), CheckBox)
                    cbHAndEBse = CType(e.Item.FindControl("cbHAndEBseDisplay"), CheckBox)
                    cbSpecialStain = CType(e.Item.FindControl("cbSpecialStainDisplay"), CheckBox)
                    cbIHCPrp = CType(e.Item.FindControl("cbIHCPrpDisplay"), CheckBox)
                    cbIHCOther = CType(e.Item.FindControl("cbIHCOtherDisplay"), CheckBox)
                    literal = CType(e.Item.FindControl("litText"), Literal)
                    cbArchive = CType(e.Item.FindControl("cbArchiveDisplay"), CheckBox)
                End If

                If Not literal Is Nothing Then
                    If Not IsDBNull(drv("SenderRef")) Then
                        If Not IsDBNull(drv("AnimalID")) Then
                            bFoundTissue = False
                            iAnimalID = drv("AnimalID")
                            sFilter = "AnimalID=" & iAnimalID

                            foundBlocks = dtBatchBlocks.Select(sFilter)

                            objDataTable = objTissues.GetBatchAnimalTissues(iBatchID, iAnimalID)

                            'For each of the tissues for this animal check if it has been assigned to a block
                            'If not grey the sample on screen so the user knows tissues still have to be 
                            'allocated.
                            For iAnimalTissueCount = objDataTable.Rows.Count - 1 To 0 Step -1

                                'Check each block
                                iBlockCount = 0
                                While iBlockCount <= foundBlocks.Length - 1 And bFoundTissue = False
                                    sFilter = "BlockID=" & foundBlocks(iBlockCount)("ID")

                                    foundBlockTissues = dtBlockTissues.Select(sFilter)

                                    'Check each blocks tisssues
                                    iBlockTissueCount = 0
                                    While iBlockTissueCount <= foundBlockTissues.Length - 1 And bFoundTissue = False
                                        If foundBlockTissues(iBlockTissueCount)("TissueCode").ToString() = objDataTable.Rows(iAnimalTissueCount)("Code").ToString() Then
                                            bFoundTissue = True
                                        End If

                                        iBlockTissueCount = iBlockTissueCount + 1
                                    End While
                                    iBlockCount = iBlockCount + 1
                                End While

                                'If we found the tissue associated with a block remove it from the tissue list
                                If bFoundTissue = True Then
                                    objDataTable.Rows.RemoveAt(iAnimalTissueCount)
                                    bFoundTissue = False
                                Else
                                    Exit For
                                End If
                            Next

                            'If the tissue list is not empty then not all tissues against the sample have 
                            'been assigned to blocks
                            If objDataTable.Rows.Count > 0 Then
                                literal.Text = drv("SenderRef").ToString() & "<font color=""Green"" font size=""Small"">*</font>"
                                chkAllTissuesAssigned.Checked = False
                            Else
                                literal.Text = drv("SenderRef").ToString()
                            End If
                        End If
                    Else
                        literal.Text = ""
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

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        RemoveSessionVars(Session)
        Response.Redirect("BatchesReceived.aspx")
    End Sub

    Private Sub btSubmit_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btSubmit.Click
        Dim bRedirect As Boolean = False
        Dim objErrorlist As New ArrayList
        Dim objBatch As New HistopathologyLib.clsBatch
        Dim iBatchID As Integer
        Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
        If Not dsBatchDetails Is Nothing Then
            Dim dtBatch As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)

            If Not dtBatch Is Nothing And dtBatch.Rows.Count > 0 Then

                'When we have added a block to a batch, mark it. This lets us know what data to
                'retrieve from the database when editing is required. Also set the status to inprogress.
                dtBatch.Rows(0)("IsBlocked") = True
                dtBatch.Rows(0)("BatchStatus") = HistopathologyLib.clsBatch.STATUS_INPROGRESS

                'Update the flag to indicate if all tissues have been assigned to blocks for all samples
                If chkAllTissuesAssigned.Checked = True Then
                    dtBatch.Rows(0)("AllTissuesAssigned") = True
                Else
                    dtBatch.Rows(0)("AllTissuesAssigned") = False
                End If

                Dim bSuccess As Boolean = objBatch.UpdateBatchDetails(CType(Session(SessionVars.SV_HeaderUserID), Integer), dsBatchDetails, objErrorlist, True, iBatchID, Nothing, IsBatchPreCassetted(dsBatchDetails, Session.Item(SessionVars.SV_BatchID)), CType(Session.Item(SessionVars.SV_UnusedHistologyRef), DataTable))
                If bSuccess Then
                    If objErrorlist.Count = 0 Then
                        bRedirect = True
                    Else
                        ctlDIV.InnerHtml = "<p><font color=""Red"">The database has been updated but some errors were encountered:</font></p><p><font color=""Red"">&nbsp;</font></p><p><font color=""Red"">" & Join(objErrorlist.ToArray, "</p></font><p><font color=""Red"">") & "</font></p>"
                    End If
                Else
                    ctlDIV.InnerHtml = "<p><font color=""Red"">The database has not been updated because the following error(s) occurred:</font></p><font color=""Red""><p>&nbsp;</font></p><p><font color=""Red"">" & Join(objErrorlist.ToArray, "</font></p><p><font color=""Red"">") & "</font></p>"
                End If

                Session.Item(SessionVars.SV_BatchDetails) = dsBatchDetails
                Session.Item(SessionVars.SV_BatchID) = iBatchID
                Session.Item(SessionVars.SV_RedirectAfterPrint) = "BatchesReceived.aspx"
                Session.Item(SessionVars.SV_UnusedHistologyRef) = Nothing

                If bRedirect Then
                    Response.Redirect("FinalPrintBatch.aspx")
                End If
            End If
        End If
    End Sub

    Private Sub grdBlockSummary_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles grdBlockSummary.SelectedIndexChanged
        DisableEnableControls(True)
    End Sub

    Private Sub btnDeleteSample_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnDeleteSample.Click
        Try
            Dim iID As Int32
            If grdBlockSummary.SelectedIndex >= 0 Then
                Dim objAnimal As New HistopathologyLib.clsAnimal
                Dim dtData As DataTable = CType(Session.Item(SessionVars.SV_BlockSummaryTable), DataTable)
                Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
                Dim sFilter As String
                Dim iAnimalID As Integer = 0
                Dim foundRows As DataRow()

                iID = Convert.ToInt32(grdBlockSummary.DataKeys(grdBlockSummary.SelectedIndex))

                sFilter = "ID=" & iID
                foundRows = dtData.Select(sFilter)

                If Not foundRows Is Nothing And foundRows.Length > 0 Then
                    iAnimalID = foundRows(0)("AnimalID")
                    If Not objAnimal.RemoveSubmission(dsBatchDetails, _
                                                      iAnimalID, _
                                                      "BATCH_BLOCK_TABLE", _
                                                      IsBatchPreCassetted(dsBatchDetails, CInt(Session.Item(SessionVars.SV_BatchID)))) Then
                        Throw New Exception("Animal.RemoveSubmission returned false")
                    End If

                    AddTounusedHistologyRefTable(foundRows(0)("SenderRef"), foundRows(0)("HistologyRef"))
                End If

                InitialiseSummaryGrid()
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to delete sample.", ex)
        End Try
    End Sub

    Private Sub VLAHeader1_HomeClick(ByVal sender As Object, ByVal e As HistopathologySystem.HomeLinkEventArgs) Handles VLAHeader1.HomeClick
        Dim sMessage As System.Text.StringBuilder = New System.Text.StringBuilder

        If CType(Session.Item(SessionVars.SV_AssignBlocks), Boolean) = True Then
            sMessage.Append("You are currently assigning tissues to blocks. Any block assignment that you have completed since you last saved will be lost. Are you sure you wish to exit?")
        Else
            sMessage.Append("Any changes that have been made will be discarded, are you sure you wish to exit without saving?")
        End If

        Page.RegisterStartupScript("navigate", PromptBeforeNavigateScript(sMessage.ToString(), "Home.aspx"))

        e.bNavigateHome = False
    End Sub

    Private Sub lbExpandAll_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lbExpandAll.Click
        SetHierarchical(True)
    End Sub

    Private Sub lbCollapseAll_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lbCollapseAll.Click
        SetHierarchical(False)
    End Sub

    Private Sub btnAddSample_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAddSample.Click
        Session.Remove(SessionVars.Sv_BlockID)
        Session.Remove(SessionVars.SV_HistologyRefType)
        Session.Item(SessionVars.SV_AddSamplePrevPage) = "BatchBlocks.aspx"
        Session.Item(SessionVars.SV_CopyBlocks) = False
        Session.Item(SessionVars.SV_Editing) = False
        Response.Redirect("AddSubmission.aspx")
    End Sub

#End Region

#Region "Grid Handling"

    Private Sub InitialiseSummaryGrid()
        Try
            Dim dsDataSet As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim objSummary As New HistopathologyLib.clsBatchSummary()
            Dim dtSummary As New DataTable()
            Dim dtTissuesList As DataTable = Common.GetLookupTypeList(LOOKUP_TISSUE_CODE)

            If Not dsDataSet Is Nothing Then
                If Not objSummary.CreateBlockSummaryData(dsDataSet, dtSummary, dtTissuesList) Then
                    Throw New Exception("BatchSummary.CreateBlockSummaryData return false")
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

                CheckAllBatchAnimalsPresent(dsDataSet)
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Error initialising the Summary Grid, BatchBlocks page.", ex)
        End Try
    End Sub

#End Region


#Region "Private Functions"

     Private Sub AddTounusedHistologyRefTable(ByVal sSenderRef As String, ByVal sHistologyRef As String)
        Dim dtUsedHistologyRefs As DataTable = CType(Session.Item(SessionVars.SV_UnusedHistologyRef), DataTable)
        Dim objHistology As New HistopathologyLib.clsHistology
        Dim iHistoNumber
        Dim iPreviousHistoType

        If sHistologyRef.IndexOf("HP") = -1 Then
            iHistoNumber = Convert.ToInt32(Right$(sHistologyRef, 5))
            iPreviousHistoType = CheckRange(iHistoNumber)
        Else
            iPreviousHistoType = 0
        End If

        If dtUsedHistologyRefs Is Nothing Then
            dtUsedHistologyRefs = objHistology.CreateUnusedHistologyRefs()
            objHistology.AddUnusedHistologyRef(dtUsedHistologyRefs, sSenderRef, sHistologyRef, iPreviousHistoType)
            Session.Item(SessionVars.SV_UnusedHistologyRef) = dtUsedHistologyRefs
        End If

    End Sub

    Private Sub CheckAllBatchAnimalsPresent(ByVal dtBatch As DataSet)
        Dim objAnimal As New HistopathologyLib.clsAnimal()
        Dim dtAnimal As DataTable
        Dim drAnimalRow As DataRow
        Dim sFilter As String
        Dim drRows As DataRow()

        'If not all animals entered at batch level have been blocked then not all tissues
        ' have been assigned.

        If Not objAnimal.GetAnimalsForBatch(CInt(Session.Item(SessionVars.SV_BatchID)), dtAnimal) Then
            Throw New Exception("Animal.GetAnimals for batch returned false.")
        End If

        For Each drAnimalRow In dtAnimal.Rows
            sFilter = "AnimalID=" & drAnimalRow("ID")

            drRows = dtBatch.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE).Select(sFilter)

            If Not drRows Is Nothing Then
                If drRows.Length = 0 Then
                    chkAllTissuesAssigned.Checked = False
                    Exit Sub
                End If
            End If
        Next
    End Sub

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

    Private Sub SetHierarchical(ByVal bExpanded As Boolean)
        Dim iCount As Int32

        If Not bExpanded Then
            For iCount = 0 To grdBlockSummary.Items.Count - 1
                If grdBlockSummary.Items(iCount).Cells(1).Text = "&nbsp;" Then
                    grdBlockSummary.Items(iCount).Visible = False
                End If

                grdBlockSummary.Items(iCount).Cells(5).Controls(0).Visible = False

                If iCount + 1 <= grdBlockSummary.Items.Count - 1 Then
                    If Not grdBlockSummary.Items(iCount + 1).Cells(6).Text = "&nbsp;" Then
                        grdBlockSummary.Items(iCount).Cells(5).Controls(0).Visible = True
                        CType(grdBlockSummary.Items(iCount).Cells(5).Controls(0), LinkButton).Text = "<img src=""Images/plus.gif"">"
                    End If
                End If
            Next

        Else
            For iCount = 0 To grdBlockSummary.Items.Count - 1
                'If the sender ref is blank in the datatable it is a expandable tissue row
                If grdBlockSummary.Items(iCount).Cells(1).Text = "&nbsp;" Then
                    grdBlockSummary.Items(iCount).Visible = True
                    grdBlockSummary.Items(iCount).Cells(5).Controls(0).Visible = False
                    HideControls(iCount)
                End If

                If iCount + 1 <= grdBlockSummary.Items.Count - 1 Then
                    'if the next row tissue is blank its a sender ref row
                    If Not grdBlockSummary.Items(iCount + 1).Cells(6).Text = "&nbsp;" Then
                        CType(grdBlockSummary.Items(iCount).Cells(5).Controls(0), LinkButton).Text = "<img src=""Images/minus.gif"">"
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
        Dim cbEO As CheckBox
        Dim cbHE As CheckBox
        Dim cbHEBSE As CheckBox
        Dim cbSpecialStain As CheckBox
        Dim cbIHCPrp As CheckBox
        Dim cbArchive As CheckBox
        Dim cbIHCOther As CheckBox
        Dim litEO As LiteralControl
        Dim litHE As LiteralControl
        Dim liHEBse As LiteralControl
        Dim liSpecialStain As LiteralControl
        Dim liIHCPrp As LiteralControl
        Dim liIHCOther As LiteralControl
        Dim strArchive As String

        grdBlockSummary.Items(iCount).Visible = True
        grdBlockSummary.Items(iCount).Cells(5).Controls(0).Visible = False
        'Hide the row selection button
        grdBlockSummary.Items(iCount).Cells(0).Controls(0).Visible = False
        'Hide the combo boxes 
        litEO = CType(grdBlockSummary.Items(iCount).Cells(7).Controls(0), LiteralControl)
        litHE = CType(grdBlockSummary.Items(iCount).Cells(8).Controls(0), LiteralControl)
        liHEBse = CType(grdBlockSummary.Items(iCount).Cells(9).Controls(0), LiteralControl)
        liSpecialStain = CType(grdBlockSummary.Items(iCount).Cells(10).Controls(0), LiteralControl)
        liIHCPrp = CType(grdBlockSummary.Items(iCount).Cells(11).Controls(0), LiteralControl)
        liIHCOther = CType(grdBlockSummary.Items(iCount).Cells(12).Controls(0), LiteralControl)

        strGridPart = GetGridPart(litEO.UniqueID())
        strEO = strGridPart + "cbEODisplay"
        strHE = strGridPart + "cbHAndEDisplay"
        strHEBse = strGridPart + "cbHAndEBseDisplay"
        strSpecialStain = strGridPart + "cbSpecialStainDisplay"
        strIHCPrp = strGridPart + "cbIHCPrpDisplay"
        strIHCOther = strGridPart + "cbIHCOtherDisplay"
        strArchive = strGridPart + "cbArchiveDisplay"

        cbEO = Page.FindControl(strEO)
        cbHE = Page.FindControl(strHE)
        cbHEBSE = Page.FindControl(strHEBse)
        cbSpecialStain = Page.FindControl(strSpecialStain)
        cbIHCPrp = Page.FindControl(strIHCPrp)
        cbIHCOther = Page.FindControl(strIHCOther)
        cbArchive = Page.FindControl(strArchive)

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

    Private Sub HideColumns(ByVal dsSubmission As DataSet)
        'Only display the Histology columns which are relevent to the type of submission
        If Session(SessionVars.SV_SubmissionType) = SUBMISSION_NONTSE Then
            grdBlockSummary.Columns(10).Visible = False
            grdBlockSummary.Columns(11).Visible = False
            grdBlockSummary.Columns(12).Visible = True
        Else
            grdBlockSummary.Columns(10).Visible = True
            grdBlockSummary.Columns(11).Visible = True
            grdBlockSummary.Columns(12).Visible = False
        End If
    End Sub

    Private Sub DisableEnableControls(ByVal bEnable As Boolean)
        btnEditSample.Enabled = bEnable
        btnDeleteSample.Enabled = bEnable
    End Sub

    Private Sub SetToolTips()
        btnAddSample.ToolTip = ADD_SAMPLE_TOOLTIP
        btnEditSample.ToolTip = EDIT_SAMPLE_TOOLTIP
        btnDeleteSample.ToolTip = DELETE_SAMPLE_TOOLTIP
        btnCopySamples.ToolTip = COPY_BLOCK_TISSUE_SUBMISSION_TOOLTIP
    End Sub

#End Region



    
End Class
