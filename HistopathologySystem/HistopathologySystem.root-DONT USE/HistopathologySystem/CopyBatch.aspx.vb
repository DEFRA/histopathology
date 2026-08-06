Partial Class CopyBatch1
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
        VLAHeader1.PageTitle = "Copy Submission"
        Session.Item(SessionVars.SV_Cassetted) = False
        If Not IsPostBack Then
            SetToolTips()
            InitialiseSummaryGrid()
            PromptBeforeSaveScript("Are you sure you want to create a new submission based on the existing submission. Continue?", btnCopyBatch)
        End If
    End Sub

#Region "Grid Related"

    Private Sub InitialiseSummaryGrid()
        Try
            Dim dsDataSet As DataSet = CType(Session.Item(SessionVars.SV_OldBatchDetails), DataSet)
            Dim objSummary As New HistopathologyLib.clsBatchSummary()
            Dim dtSummary As New DataTable()
            Dim dtTissuesList As DataTable = Common.GetLookupTypeList(LOOKUP_TISSUE_CODE)
            Dim dtData As DataTable

            If Not objSummary.CreateBatchSummaryData(dsDataSet, dtSummary, dtTissuesList) Then
                Throw New Exception("BatchSummary.CreateBatchSummaryData return false")
            End If

            ' create a dataview for filtering and sorting
            Dim dv As DataView = dtSummary.DefaultView

            Session.Item(SessionVars.SV_BatchSummaryTable) = dtSummary
            Session.Item(SessionVars.SV_BatchSummaryView) = dv

            grdBatchSummary.DataSource = dtSummary
            grdBatchSummary.DataKeyField = "ID"
            grdBatchSummary.DataBind()
            grdBatchSummary.Enabled = True

            SetHierarchical(False)

        Catch ex As Exception
            clsAppError.DisplayError("Error initialising the Summary Grid", ex)
        End Try
    End Sub

    Private Sub SetHierarchical(ByVal bExpanded As Boolean)
        Dim iCount As Int32

        'Cell 0 = Select button
        'Cell 1 = Sender Ref field
        'Cell 2 = +/- button
        'Cell 3 = Tissue field
        If Not bExpanded Then
            For iCount = 0 To grdBatchSummary.Items.Count - 1
                'if the sender ref is null then it is a tissue row
                If grdBatchSummary.Items(iCount).Cells(1).Text = "&nbsp;" Then
                    grdBatchSummary.Items(iCount).Visible = False
                End If

                grdBatchSummary.Items(iCount).Cells(2).Controls(0).Visible = False

                If iCount + 1 <= grdBatchSummary.Items.Count - 1 Then
                    If Not grdBatchSummary.Items(iCount + 1).Cells(3).Text = "&nbsp;" Then
                        grdBatchSummary.Items(iCount).Cells(2).Controls(0).Visible = True
                        CType(grdBatchSummary.Items(iCount).Cells(2).Controls(0), LinkButton).Text = "<img src=""Images/plus.gif"">"
                    End If
                End If
            Next
        Else
            For iCount = 0 To grdBatchSummary.Items.Count - 1
                'If the sender ref is blank in the datatable it is a expandable tissue row
                If grdBatchSummary.Items(iCount).Cells(1).Text = "&nbsp;" Then
                    grdBatchSummary.Items(iCount).Visible = True
                    grdBatchSummary.Items(iCount).Cells(2).Controls(0).Visible = False
                    grdBatchSummary.Items(iCount).Cells(0).Controls(0).Visible = False
                End If

                If iCount + 1 <= grdBatchSummary.Items.Count - 1 Then
                    'if the next row tissue is blank its a sender ref row
                    If Not grdBatchSummary.Items(iCount + 1).Cells(3).Text = "&nbsp;" Then
                        CType(grdBatchSummary.Items(iCount).Cells(2).Controls(0), LinkButton).Text = "<img src=""Images/minus.gif"">"
                    End If
                End If
            Next
        End If
    End Sub

#End Region

#Region "Event Handlers"

    Private Sub grdBatchSummary_ItemCommand(ByVal source As System.Object, ByVal e As System.Web.UI.WebControls.DataGridCommandEventArgs) Handles grdBatchSummary.ItemCommand
        Dim iCount As Int32 = e.Item.ItemIndex + 1

        'Cell 0 = Select button
        'Cell 1 = Sender Ref field
        'Cell 2 = +/- button
        'Cell 3 = Tissue field

        If e.CommandName = "ExpandTissues" Then
            If CType(e.Item.Cells(2).Controls(0), LinkButton).Text = "<img src=""Images/minus.gif"">" Then
                Do While Not grdBatchSummary.Items(iCount).Cells(3).Text = "&nbsp;"
                    grdBatchSummary.Items(iCount).Visible = False
                    iCount += 1
                    If iCount >= grdBatchSummary.Items.Count Then Exit Do
                Loop
                CType(e.Item.Cells(2).Controls(0), LinkButton).Text = "<img src=""Images/plus.gif"">"
            Else
                'While the sender ref isnt empty show the tissue rows
                Do While Not grdBatchSummary.Items(iCount).Cells(3).Text = "&nbsp;"
                    grdBatchSummary.Items(iCount).Visible = True
                    grdBatchSummary.Items(iCount).Cells(0).Controls(0).Visible = False
                    grdBatchSummary.Items(iCount).Cells(2).Controls(0).Visible = False
                    iCount += 1
                    If iCount >= grdBatchSummary.Items.Count Then Exit Do
                Loop
                CType(e.Item.Cells(2).Controls(0), LinkButton).Text = "<img src=""Images/minus.gif"">"
            End If
        End If
    End Sub

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        Dim dsOldBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_OldBatchDetails), DataSet)

        Session.Item(SessionVars.SV_BatchDetails) = dsOldBatchDetails
        Session.Remove(SessionVars.SV_OldBatchDetails)

        Response.Redirect("ViewSubmissions.aspx")
    End Sub

    Private Sub btnCopySample_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCopySample.Click
        If grdBatchSummary.SelectedIndex >= 0 Then
            Try
                Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
                Dim dtAnimal As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE)
                Dim dtData As DataTable = CType(Session.Item(SessionVars.SV_BatchSummaryTable), DataTable)
                Dim iSelectedID As Integer = grdBatchSummary.DataKeys(grdBatchSummary.SelectedIndex)
                Dim foundRows As DataRow()
                Dim foundAnimal As DataRow()
                Dim sFilter As String
                Dim iCount As Integer
                Dim objArrayList As ArrayList = CType(Session.Item(SessionVars.SV_AnimalIDs), ArrayList)
                Dim objIds As New HistopathologyLib.clsIDPairs()

                sFilter = "ID=" & iSelectedID
                foundRows = dtData.Select(sFilter)

                'Remove it from the animal array list and animal datatable so we can add it back again with
                'any changes made.
                If Not foundRows Is Nothing And foundRows.Length > 0 Then
                    For iCount = objArrayList.Count - 1 To 0 Step -1
                        objIds = objArrayList(iCount)
                        If objIds.OldID = foundRows(0)("AnimalID") Then
                            objArrayList.RemoveAt(iCount)

                            'Check this
                            sFilter = "ID=" & objIds.NewID
                            foundAnimal = dtAnimal.Select(sFilter)
                            If Not foundAnimal Is Nothing And foundAnimal.Length = 1 Then
                                dtAnimal.Rows.Remove(foundAnimal(0))
                            End If
                        End If
                    Next

                    Session.Item(SessionVars.SV_SelectedAnimal) = foundRows(0)("AnimalID")
                End If
            Catch ex As Exception
                clsAppError.DisplayError("Failed to change sender ref.", ex)
            End Try
            Session.Remove(SessionVars.SV_SenderRef)
            Session.Item(SessionVars.SV_AddSamplePrevPage) = "CopyBatch.aspx"
            Session.Item(SessionVars.SV_AddSampleNextPage) = "CopyBatch.aspx"
            Session.Item(SessionVars.SV_OldPGNumber) = ""
            Session.Item(SessionVars.SV_PMDate) = ""
            Session.Item(SessionVars.SV_Species) = ""
            Response.Redirect("AddSubmission.aspx")
        End If
    End Sub

    Private Sub grdBatchSummary_ItemDataBound(ByVal sender As System.Object, ByVal e As System.Web.UI.WebControls.DataGridItemEventArgs) Handles grdBatchSummary.ItemDataBound
        ' populate template column values here
        Try
            ' set up the checkbox and drop-down columns
            Dim drv As DataRowView = CType(e.Item.DataItem, DataRowView)
            If Not drv Is Nothing Then
                Dim iAnimalID As Integer
                Dim lblSenderRef As Label = Nothing
                Dim objArrayList As ArrayList = CType(Session.Item(SessionVars.SV_AnimalIDs), ArrayList)
                Dim objIds As New HistopathologyLib.clsIDPairs()
                Dim iCount As Integer

                If e.Item.ItemType = ListItemType.EditItem Then
                ElseIf e.Item.ItemType = ListItemType.Item _
                OrElse e.Item.ItemType = ListItemType.AlternatingItem _
                OrElse e.Item.ItemType = ListItemType.SelectedItem Then
                    ' populate display mode controls
                    lblSenderRef = CType(e.Item.FindControl("lblNewSenderRefDisplay"), Label)
                End If

                If Not IsDBNull(drv("AnimalID")) Then
                    iAnimalID = drv("AnimalID")
                    For iCount = 0 To objArrayList.Count - 1
                        objIds = objArrayList(iCount)
                        If objIds.OldID = iAnimalID Then
                            lblSenderRef.Text = objIds.OtherValue
                        End If
                    Next
                End If
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to bind template columns in the Submission summary grid", ex)
        End Try
    End Sub

    Private Sub btnCopyBatch_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCopyBatch.Click
        Dim bRedirect As Boolean = False
        Dim dsNewBatch As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
        Dim iBatchID As Integer
        Try
            Dim dsOldBatch As DataSet = CType(Session.Item(SessionVars.SV_OldBatchDetails), DataSet)
            Dim objAnimalIDs As ArrayList = CType(Session.Item(SessionVars.SV_AnimalIDs), ArrayList)
            Dim objBatch As New HistopathologyLib.clsBatch()
            Dim dtBatch As DataTable
            Dim objErrorlist As New ArrayList()
            Dim sErrorMessage As String

            If Not objBatch.CopyBatch(dsOldBatch, dsNewBatch, False, objAnimalIDs, _
                                      CInt(Session.Item(SessionVars.SV_HeaderUserAreaID)), _
                                      CInt(Session.Item(SessionVars.SV_HeaderUserID)), _
                                      False) Then
                Throw New Exception("Batch.Copybatch returned false.")
            End If

            If Not dsNewBatch Is Nothing Then
                dtBatch = dsNewBatch.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)
                If Not dtBatch Is Nothing And dtBatch.Rows.Count > 0 Then
                    dtBatch.Rows(0)("IsBlocked") = False
                End If
            End If

            Dim bSuccess As Boolean = objBatch.UpdateBatchDetails(CType(Session(SessionVars.SV_HeaderUserID), Integer), _
                                                                  dsNewBatch, _
                                                                  objErrorlist, _
                                                                  False, _
                                                                  iBatchID)
            If bSuccess Then
                If objErrorlist.Count = 0 Then
                    bRedirect = True
                Else
                    ctlDIV.InnerHtml = "<p><font color=""Red"">The database has been updated but some errors were encountered:</font></p><p>&nbsp;</p><p><font color=""Red"">" & Join(objErrorlist.ToArray, "</font></p><p>") & "</p>"
                End If
            Else
                ctlDIV.InnerHtml = "<p><font color=""Red"">The database has not been updated because the following error(s) occurred:</font></p><p>&nbsp;</p><p><font color=""Red"">" & Join(objErrorlist.ToArray, "</font></p><p>") & "</p>"
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to Copy submission.", ex)
        End Try

        If bRedirect Then
            Session.Item(SessionVars.SV_BatchID) = iBatchID
            GetCommonBatchDetailsFromDatabase(iBatchID, Session)
            GetBatchSubmissionDetailsFromDatabase(iBatchID, Session)
            Session.Remove(SessionVars.SV_OldBatchDetails)
            Session.Item(SessionVars.SV_Cassetted) = False
            Response.Redirect("BatchDetails.aspx")
        End If
    End Sub

    Private Sub lbCollapseAll_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lbCollapseAll.Click
        SetHierarchical(False)
    End Sub

    Private Sub lbExpandAll_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lbExpandAll.Click
        SetHierarchical(True)
    End Sub

    Private Sub grdBatchSummary_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles grdBatchSummary.SelectedIndexChanged
        If grdBatchSummary.SelectedIndex >= 0 Then
            btnCopySample.Enabled = True
        End If
    End Sub

#End Region

#Region "Private functions"

    Private Sub SetToolTips()
        btnCopySample.ToolTip = COPY_BATCH_SAMPLE_TOOLTIP
    End Sub

#End Region
End Class
