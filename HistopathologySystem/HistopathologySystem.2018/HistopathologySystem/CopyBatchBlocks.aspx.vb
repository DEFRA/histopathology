Partial Class CopyBatchBlocks
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
        Session.Item(SessionVars.SV_Cassetted) = True
        If Not IsPostBack Then
            InitialiseSummaryGrid()
            PromptBeforeSaveScript("Are you sure you want to create a new submission based on the existing submission. Continue?", btnCopyBatch)
            SetToolTips()
        End If
    End Sub

#Region "Grid Related"

    Private Sub InitialiseSummaryGrid()
        Try
            Dim dsDataSet As DataSet = CType(Session.Item(SessionVars.SV_OldBatchDetails), DataSet)

            Dim dsTemp As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtTempAnimal As DataTable = dsTemp.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL)
            Dim dtAnimal As DataTable
            Dim dvAnimalsView As DataView

            If Not dsDataSet Is Nothing Then
                If CType(Session.Item(SessionVars.SV_Cassetted), Boolean) = True Then
                    dtAnimal = dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL)
                Else
                    dtAnimal = dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE)
                End If
            End If

            Dim dv As DataView = dtAnimal.DefaultView
            dv.Sort = "SenderRef ASC"

            Session.Item(SessionVars.SV_BatchSummaryTable) = dtAnimal
            Session.Item(SessionVars.SV_BatchSummaryView) = dv

            grdBatchSummary.DataSource = dtAnimal
            grdBatchSummary.DataKeyField = "ID"
            grdBatchSummary.DataBind()
            grdBatchSummary.Enabled = True

        Catch ex As Exception
            clsAppError.DisplayError("Error initialising the Summary Grid", ex)
        End Try
    End Sub

#End Region

#Region "Event Handlers"

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
                Dim dsOldBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_OldBatchDetails), DataSet)
                Dim dtAnimal As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL)
                Dim dtData As DataTable = CType(Session.Item(SessionVars.SV_BatchSummaryTable), DataTable)
                Dim iSelectedID As Integer = grdBatchSummary.DataKeys(grdBatchSummary.SelectedIndex)
                Dim foundRows As DataRow()
                Dim foundAnimal As DataRow()
                Dim sFilter As String
                Dim iCount As Integer
                Dim objArrayList As ArrayList = CType(Session.Item(SessionVars.SV_AnimalIDs), ArrayList)
                Dim objIds As New HistopathologyLib.clsIDPairs()
                Dim objAnimal As New HistopathologyLib.clsAnimal
                Dim iNumberOfSamples As Integer = 0

                sFilter = "ID=" & iSelectedID
                foundRows = dtData.Select(sFilter)

                'Remove it from the animal array list so we can add it back again with
                'any changes made.
                If Not foundRows Is Nothing And foundRows.Length > 0 Then
                    For iCount = objArrayList.Count - 1 To 0 Step -1
                        objIds = objArrayList(iCount)
                        If objIds.OldID = foundRows(0)("ID") Then
                            objArrayList.RemoveAt(iCount)

                            sFilter = "ID=" & objIds.NewID
                            foundAnimal = dtAnimal.Select(sFilter)
                            If Not foundAnimal Is Nothing And foundAnimal.Length = 1 Then
                                dtAnimal.Rows.Remove(foundAnimal(0))
                            End If
                        End If
                    Next

                    Session.Item(SessionVars.SV_SelectedAnimal) = foundRows(0)("ID")
                    objAnimal.GetNumberOfBlocks(dsOldBatchDetails, foundRows(0)("ID"), iNumberOfSamples)
                    Session.Item(SessionVars.SV_SelectedAnimalNumberBlocks) = iNumberOfSamples
                End If
            Catch ex As Exception
                clsAppError.DisplayError("Failed to change sender ref.", ex)
            End Try
            Session.Remove(SessionVars.SV_SenderRef)
            Session.Item(SessionVars.SV_AddSamplePrevPage) = "CopyBatchBlocks.aspx"
            Session.Item(SessionVars.SV_AddSampleNextPage) = "CopyBatchBlocks.aspx"
            Session.Item(SessionVars.SV_OldPGNumber) = ""
            Session.Item(SessionVars.SV_PMDate) = ""
            Session.Item(SessionVars.SV_Species) = ""
            Response.Redirect("AddSubmission.aspx")
        End If
    End Sub

    Private Sub grdBatchSummary_ItemDataBound(ByVal sender As System.Object, ByVal e As System.Web.UI.WebControls.DataGridItemEventArgs) Handles grdBatchSummary.ItemDataBound
        '' populate template column values here
        Try
            ' set up the checkbox and drop-down columns
            Dim drv As DataRowView = CType(e.Item.DataItem, DataRowView)
            If Not drv Is Nothing Then
                Dim iAnimalID As Integer
                Dim lblSenderRef As Label = Nothing
                Dim objArrayList As ArrayList = CType(Session.Item(SessionVars.SV_AnimalIDs), ArrayList)
                Dim objIds As New HistopathologyLib.clsIDPairs()
                Dim iCount As Integer

                Dim lblFixationCode As Label = Nothing
                Dim cbEO As CheckBox = Nothing
                Dim cbHAndE As CheckBox = Nothing
                Dim cbHAndEBse As CheckBox = Nothing
                Dim cbSpecialStain As CheckBox = Nothing
                Dim cbIHCPrp As CheckBox = Nothing
                Dim cbIHCOther As CheckBox = Nothing
                Dim cbRepeatBlock As CheckBox = Nothing

                If e.Item.ItemType = ListItemType.EditItem Then
                ElseIf e.Item.ItemType = ListItemType.Item _
                OrElse e.Item.ItemType = ListItemType.AlternatingItem _
                OrElse e.Item.ItemType = ListItemType.SelectedItem Then
                    ' populate display mode controls
                    lblSenderRef = CType(e.Item.FindControl("lblNewSenderRefDisplay"), Label)
                    cbEO = CType(e.Item.FindControl("cbEODisplay"), CheckBox)
                    cbHAndE = CType(e.Item.FindControl("cbHAndEDisplay"), CheckBox)
                    cbHAndEBse = CType(e.Item.FindControl("cbHAndEBseDisplay"), CheckBox)
                    cbSpecialStain = CType(e.Item.FindControl("cbSpecialStainDisplay"), CheckBox)
                    cbIHCPrp = CType(e.Item.FindControl("cbIHCPrpDisplay"), CheckBox)
                    cbIHCOther = CType(e.Item.FindControl("cbIHCOtherDisplay"), CheckBox)
                    cbRepeatBlock = CType(e.Item.FindControl("cbRepeatBlockDisplay"), CheckBox)
                End If

                If Not IsDBNull(drv("ID")) Then
                    iAnimalID = drv("ID")
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
        Dim iBatchID As Integer = CInt(Session.Item(SessionVars.SV_BatchID))
        Try
            Dim dsOldBatch As DataSet = CType(Session.Item(SessionVars.SV_OldBatchDetails), DataSet)
            Dim objAnimalIDs As ArrayList = CType(Session.Item(SessionVars.SV_AnimalIDs), ArrayList)
            Dim objBatch As New HistopathologyLib.clsBatch()
            Dim dtBatch As DataTable
            Dim objErrorlist As New ArrayList()
            Dim sErrorMessage As String = ""

            If Not objBatch.CopyBatch(dsOldBatch, dsNewBatch, True, objAnimalIDs, _
                                      CInt(Session.Item(SessionVars.SV_HeaderUserAreaID)), _
                                      CInt(Session.Item(SessionVars.SV_HeaderUserID)), _
                                      IsBatchPreCassetted(dsOldBatch, iBatchID)) Then
                Throw New Exception("Batch.Copybatch returned false.")
            End If

            If Not dsNewBatch Is Nothing Then
                dtBatch = dsNewBatch.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)
                If Not dtBatch Is Nothing And dtBatch.Rows.Count > 0 Then
                    dtBatch.Rows(0)("IsBlocked") = True
                End If
            End If

            Dim bSuccess As Boolean = objBatch.UpdateBatchDetails(CType(Session(SessionVars.SV_HeaderUserID), Integer), _
                                                                  dsNewBatch, _
                                                                  objErrorlist, _
                                                                  True, _
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
            GetBatchBlockDetailsFromDatabase(iBatchID, Session)
            Session.Remove(SessionVars.SV_OldBatchDetails)
            Session.Item(SessionVars.SV_Cassetted) = True
            Response.Redirect("BatchDetails.aspx")
        End If
    End Sub

    Private Sub grdBatchSummary_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles grdBatchSummary.SelectedIndexChanged
        If grdBatchSummary.SelectedIndex >= 0 Then
            btnCopySample.Enabled = True
        End If
    End Sub

    Private Sub btnSummary_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSummary.Click
        Response.Redirect("CopyBatchBlocksSummary.aspx")
    End Sub

#End Region

#Region "Private Functions"

    Private Sub SetToolTips()
        btnSummary.ToolTip = SUBMISSION_SUMMARY_TOOLTIP
        btnCopySample.ToolTip = COPY_BATCH_BLOCK_TOOLTIP
    End Sub

#End Region


End Class
