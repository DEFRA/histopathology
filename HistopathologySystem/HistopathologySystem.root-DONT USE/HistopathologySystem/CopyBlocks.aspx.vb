Partial Class CopyBlocks
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents SenderRef1 As SenderRef
    Protected WithEvents HistologyRef1 As HistologyRef

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
        VLAHeader1.PageTitle = "Copy Blocks"
        CheckPermissions()

        If Not IsPostBack Then
            InitialiseAnimalGrid()
            SenderRef1.SetEnabled(False)
            HistologyRef1.SetEnabled(False)
        End If

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

    Private Sub InitialiseAnimalGrid()
        Try
            Dim objAnimal As New HistopathologyLib.clsAnimal()
            Dim dtAnimal As DataTable

            If Not objAnimal.GetAnimalsForBatch(CInt(Session.Item(SessionVars.SV_BatchID)), dtAnimal) Then
                Throw New Exception("Animal.GetAnimals for batch returned false.")
            End If

            Dim sSenderRef As String = CStr(Session.Item(SessionVars.SV_SenderRef))

            'Remove the currently selected animal from the grid
            Dim sFilter As String
            Dim drFoundRows As DataRow()

            sFilter = "SenderRef=" & "'" & sSenderRef & "'"
            drFoundRows = dtAnimal.Select(sFilter)
            If Not drFoundRows Is Nothing And drFoundRows.Length = 1 Then
                HistologyRef1.Text = CStr(Session.Item(SessionVars.SV_HistologyRef))
                SenderRef1.Text = drFoundRows(0)("SenderRef").ToString()
                dtAnimal.Rows.Remove(drFoundRows(0))
            End If

            Session.Item(SessionVars.SV_AnimalTable) = dtAnimal

            grdAnimal.DataSource = dtAnimal
            grdAnimal.DataKeyField = "ID"
            grdAnimal.DataBind()
            grdAnimal.Enabled = True

        Catch ex As Exception
            clsAppError.DisplayError("Failed to load lookup lists.", ex)
        End Try
    End Sub

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        Response.Redirect("SubmissionDetailsBlock.aspx")
    End Sub

    Private Function GetNextHistoNumber(ByRef iMaxLimit As Integer) As String
        Dim objAnimal As New HistopathologyLib.clsAnimal
        Dim objHistology As New HistopathologyLib.clsHistology
        Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
        Dim dtData As DataTable
        Dim sFilter As String
        Dim iCurrentHistoNumber As String
        Dim iHistoType As Integer
        Dim foundRows As DataRow()
        Dim sNextHistoRef As String
        Dim dDate As Date

        If HistologyRef1.IsValid() And Not objAnimal.CheckIfPGAnimal(dsBatchDetails, CInt(Session.Item(SessionVars.SV_AnimalID))) And HistologyRef1.Text.IndexOf("HP") = -1 Then
            iCurrentHistoNumber = Right$(HistologyRef1.Text(), 5)

            If iCurrentHistoNumber >= 10000 And iCurrentHistoNumber < 20000 Then
                iHistoType = HistologyRefType.eNeuropath
                iMaxLimit = 19999
            ElseIf iCurrentHistoNumber >= 20000 And iCurrentHistoNumber < 30000 Then
                iHistoType = HistologyRefType.eAbattoirSurvey
                iMaxLimit = 29999
            ElseIf iCurrentHistoNumber >= 30000 And iCurrentHistoNumber < 40000 Then
                iHistoType = HistologyRefType.eTBDiag
                iMaxLimit = 39999
            ElseIf iCurrentHistoNumber >= 40000 And iCurrentHistoNumber < 60000 Then
                iHistoType = HistologyRefType.eGeneralPool
                iMaxLimit = 59999
            ElseIf iCurrentHistoNumber >= 60000 And iCurrentHistoNumber < 90000 Then
                iHistoType = HistologyRefType.eMouseProjects
                iMaxLimit = 89999
            End If

            If Not objHistology.GetNextAvailableHistologyRef(iHistoType, sNextHistoRef) Then
                Throw New Exception("Histology.GetNextAvailableHistologyRef returned false.")
            Else
                Return Right$(dDate.Now().Year(), 2) + "/" + sNextHistoRef
            End If
        Else
            Return ""
        End If

    End Function

    Private Sub btnFinish_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnFinish.Click
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtBlockAnimals As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL)
            Dim dtAnimal As DataTable = CType(Session.Item(SessionVars.SV_AnimalTable), DataTable)
            Dim objAnimal As New HistopathologyLib.clsAnimal
            Dim objBlock As New HistopathologyLib.clsBlock
            Dim dgItem As DataGridItem
            Dim cbCopy As CheckBox
            Dim iAnimalID As Integer
            Dim iBatchID As Integer = CInt(Session.Item(SessionVars.SV_BatchID))
            Dim foundAnimal As DataRow()
            Dim foundAnimalDataSet As DataRow()
            Dim sFilter As String
            Dim iNextHistoNumber As Integer
            Dim sHistologyRef As String = ""
            Dim sSenderRef As String = ""
            Dim dDate As Date
            Dim iMaxLimit As Integer
            Dim bPGAnimal As Boolean = False
            Dim objBlockIds As ArrayList = CType(Session.Item(SessionVars.SV_BlockIDs), ArrayList)
            Dim iCount As Integer = 0

            bPGAnimal = objAnimal.CheckIfPGAnimal(dsBatchDetails, CInt(Session.Item(SessionVars.SV_AnimalID)))

            'Copy the block to all the animals that have been selected in the grid
            For Each dgItem In grdAnimal.Items
                cbCopy = dgItem.FindControl("cbCopy")

                If Not cbCopy Is Nothing Then
                    If cbCopy.Checked = True Then
                        iAnimalID = Convert.ToInt32(grdAnimal.DataKeys(dgItem.ItemIndex))

                        sFilter = "ID=" & iAnimalID

                        foundAnimal = dtAnimal.Select(sFilter)
                        foundAnimalDataSet = dtBlockAnimals.Select(sFilter)
                        sHistologyRef = ""

                        If Not foundAnimal Is Nothing Then
                            If foundAnimal.Length > 0 Then
                                If Not foundAnimalDataSet Is Nothing Then
                                    If foundAnimalDataSet.Length = 0 Then
                                        'Put the animal into the dataset if its not already in it
                                        'Carry the histology refs on from the currently selected animal

                                        sHistologyRef = foundAnimal(0)("HistologyRef").ToString()
                                        sSenderRef = foundAnimal(0)("SenderRef").ToString()

                                        If cbAutoGenerateHisto.Checked = True Then
                                            If sHistologyRef = "" And HistologyRef1.IsValid Then
                                                sHistologyRef = GetNextHistoNumber(iMaxLimit)
                                                If sHistologyRef <> "" Then
                                                    iNextHistoNumber = Right$(sHistologyRef, 5)
                                                Else
                                                    iNextHistoNumber = 0
                                                End If

                                                If iNextHistoNumber = 0 Then
                                                    'If use pg number has been selected for the Animal we are
                                                    'copying from the generate the histology refs if the animals are 
                                                    'PG animals
                                                    If bPGAnimal AndAlso CheckIsPGNumber(sSenderRef) Then
                                                        sHistologyRef = GenerateHistoFromPG(sSenderRef)
                                                    End If
                                                ElseIf iNextHistoNumber > 0 Then
                                                    If iNextHistoNumber > iMaxLimit Then
                                                        ctlDiv.InnerHtml = "<p><font color=""Red"">The Histo ref number is greater than the maximum value for this area. Not all Histo refs will be applied.</font></p>"
                                                    End If
                                                End If
                                            End If
                                            End If

                                        If Not objAnimal.NewExistingRecord(dtBlockAnimals, _
                                                                            foundAnimal(0)("SenderRef").ToString(), _
                                                                            sHistologyRef, _
                                                                            foundAnimal(0)("NextBlockRef").ToString(), _
                                                                            foundAnimal(0)("RowStamp"), _
                                                                            foundAnimal(0)("ID"), _
                                                                            foundAnimal(0)("HistoRefSet"), _
                                                                            foundAnimal(0)("OnHold"), _
                                                                            foundAnimal(0)("PMDate").ToString(), _
                                                                            foundAnimal(0)("PMDateSet")) Then
                                            Throw New Exception("Animal.NewExistingRecord returned false.")
                                        End If

                                        If Not objAnimal.GetPreBookedBlocks(foundAnimal(0)("ID"), dsBatchDetails) Then
                                            Throw New Exception("Animal.GetPreBookedBlocks returned false.")
                                        End If
                                    End If
                                End If
                                If Not foundAnimal Is Nothing And foundAnimal.Length > 0 Then
                                    iCount = 0
                                    For iCount = 0 To objBlockIds.Count - 1
                                        If Not objBlock.CopyBlock(dsBatchDetails, objBlockIds(iCount), foundAnimal(0)("ID"), iBatchID) Then
                                            Throw New Exception("Block.CopyBlock returned false.")
                                        End If
                                    Next
                                End If
                            End If
                        End If
                    End If
                End If
            Next

            Session.Item(SessionVars.SV_CopyBlocks) = True
        Catch ex As Exception
            clsAppError.DisplayError("Failed to copy block.", ex)
        End Try
        Response.Redirect("SubmissionDetailsBlock.aspx")
    End Sub

    Private Sub cbSelectAll_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cbSelectAll.CheckedChanged
        Try
            Dim dgItem As DataGridItem
            Dim chkSelected As CheckBox
            Dim bSelected As Boolean = cbSelectAll.Checked

            For Each dgItem In grdAnimal.Items
                chkSelected = dgItem.FindControl("cbCopy")

                If Not chkSelected Is Nothing Then
                    chkSelected.Checked = bSelected
                End If
            Next

        Catch ex As Exception
            clsAppError.DisplayError("Failed to select all samples.", ex)
        End Try
    End Sub

    Private Sub VLAHeader1_HomeClick(ByVal sender As Object, ByVal e As HistopathologySystem.HomeLinkEventArgs) Handles VLAHeader1.HomeClick
        Dim sMessage As System.Text.StringBuilder = New System.Text.StringBuilder

        If CType(Session.Item(SessionVars.SV_AssignBlocks), Boolean) Then
            sMessage.Append("You are currently assigning tissues to blocks. Any block assignment that you have completed since you last saved will be lost. Are you sure you wish to exit?")
        Else
            sMessage.Append("Any changes that have been made will be discarded, are you sure you wish to exit without saving?")
        End If

        Page.RegisterStartupScript("navigate", PromptBeforeNavigateScript(sMessage.ToString(), "Home.aspx"))

        e.bNavigateHome = False
    End Sub

    Private Sub cbAutoGenerateHisto_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cbAutoGenerateHisto.CheckedChanged

    End Sub
End Class
