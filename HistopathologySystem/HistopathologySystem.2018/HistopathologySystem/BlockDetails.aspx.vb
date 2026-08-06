Imports System.Text.RegularExpressions

Partial Class BlockDetails
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents TissuesPager As DataGridPager
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
        VLAHeader1.PageTitle = "Block Details"
        TissuesPager.SetGrid(grdTissues)
        SetClientValidation()
        SetTextboxDefaultButton(txtNoBlocks, btnDone)
        SetTextboxDefaultButton(txtBlockReference, btnDone)
        SetTextboxDefaultButton(txtCustomerRef, btnDone)
        SetFocus(txtBlockReference)

        If Not IsPostBack Then
            Dim iBlockID As Integer = CType(Session.Item(SessionVars.Sv_BlockID), Integer)

            'This array is used to keep track of which Histology has been selected
            Dim SelectedItemArray As New ArrayList()
            Session.Item(SessionVars.SV_SelectedHistologyArray) = SelectedItemArray

            LoadCheckBoxLists()
            txtNoBlocks.Text = "1"
            DisplayStaticData()

            If iBlockID = 0 Then
                CreateNewRecord()
                HideTests()
                DisplayBatchLevelTests()
                'DefaultTestLists()
            Else
                InitialiseScreenWithBlockDetails()
            End If

            InitialiseTissuesGrid()
            EnableDisableAdditionalRequest()
        End If

    End Sub

#Region "Grid Handling"

    Private Sub InitialiseTissuesGrid()
        Try
            Dim dsDataSet As DataSet = Session(SessionVars.SV_BatchDetails)
            Dim dtTissueData As DataTable = dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TISSUES)
            Dim iBlockID As Integer = CType(Session.Item(SessionVars.Sv_BlockID), Integer)
            Dim sRowFilter As String
            Dim dvData As DataView
            Dim sFilter As String

            Session(SessionVars.SV_TissuesTable) = dtTissueData

            sRowFilter = "BlockID=" & Convert.ToString(iBlockID)
            dtTissueData.DefaultView.RowFilter = sRowFilter

            dvData = dtTissueData.DefaultView
            Session(SessionVars.SV_TissuesView) = dvData

            'Backup the old tissues so we can revert back if the user cancels the block
            Dim dtOldData As New DataTable()
            sFilter = "BlockID=" & Convert.ToString(iBlockID)
            dtOldData = CopyDataTable(dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_TISSUES_TABLE), sFilter)
            Session.Item(SessionVars.SV_TissuesBeforeChanges) = dtOldData

            ' initialise the grid
            grdTissues.DataSource = dtTissueData
            grdTissues.DataKeyField = "ID"
            grdTissues.CurrentPageIndex = 0
            grdTissues.SelectedIndex = -1
            grdTissues.EditItemIndex = -1
            grdTissues.DataBind()
            grdTissues.Enabled = True

            ' initialise the pager
            TissuesPager.DataTableSessionID = SessionVars.SV_TissuesTable
            TissuesPager.DataViewSessionID = SessionVars.SV_TissuesView
            TissuesPager.PageLinkCount = 10
            TissuesPager.AllowAddNew = True
            TissuesPager.AllowEdit = True
            TissuesPager.AllowDelete = True
            TissuesPager.Rebind()
            TissuesPager.Refresh()

        Catch ex As Exception
            clsAppError.DisplayError("Error initialising the Tissues grid on the Block Details page", ex)
        End Try
    End Sub

#End Region

#Region "Private Functions"

    Private Sub EnableDisableAdditionalRequest()
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)

            If Not dsBatchDetails Is Nothing Then
                Dim dtSubmittedAs As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_SUBMITTEDAS_TABLE)
                Dim drRow As DataRow
                Dim bEnable As Boolean = True

                'Disable the additional request if the submission isnt wax block or
                'unstained section.
                For Each drRow In dtSubmittedAs.Rows
                    'If drRow("Code") = "2" Or _
                    '   drRow("Code") = "4" Then
                    If drRow("Code") = "1" Or _
                       drRow("Code") = "3" Or _
                       drRow("Code") = "5" Then
                        bEnable = False
                        Exit For
                    End If
                Next

                chkRepeatBlock.Enabled = bEnable

            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to enable/disable the Additional request option.", ex)
        End Try
    End Sub

    'Private Sub UsePreBookedBlock(ByRef iOldBlockId As Integer, ByRef sNextBlockRef As String)
    '    Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
    '    Dim iAnimalID As Integer = CType(Session.Item(SessionVars.SV_AnimalID), Integer)
    '    Dim objAnimal As New HistopathologyLib.clsAnimal
    '    Dim objBatch As New HistopathologyLib.clsBatch
    '    Dim objBlock As New HistopathologyLib.clsBlock
    '    Dim iNewBlockId As Integer

    '    ' If we are doing a precassetted submission then we will want to use one of the pre booked blocks ID's.
    '    ' Do this by getting the pre booked ID and replacing the new ID created.

    '    objAnimal.GetPreBookedBlock(dsBatchDetails, iAnimalID, iNewBlockId, sNextBlockRef)
    '    objAnimal.RemovePreBlocked(dsBatchDetails, iNewBlockId)

    '    ' Now update the new block that was created.
    '    objBlock.UsePreBookedBlockID(dsBatchDetails, iOldBlockId, iNewBlockId)
    '    iOldBlockId = iNewBlockId
    'End Sub

    Private Sub CreateNewRecord()
        Try
            Dim objBlock As New HistopathologyLib.clsBlock
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtBatchBlocks As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE)
            Dim dtAnimals As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL)
            Dim objAnimal As New HistopathologyLib.clsAnimal
            Dim dtPreBooked As DataTable
            Dim sHistologyRef As String
            Dim sSenderRef As String
            Dim sNextBlockRef As String
            Dim sPreBookedBlockRef As String
            Dim GetAnimalData As String
            Dim aRowStamp As System.Array
            Dim iBlockID As Integer
            Dim iBatchID As Integer = CType(Session.Item(SessionVars.SV_BatchID), Integer)
            Dim iAnimalID As Integer = CType(Session.Item(SessionVars.SV_AnimalID), Integer)
            Dim drFoundRow As DataRow()
            Dim sFilter As String
            Dim bHistoRefSetInDatabase As Boolean
            Dim bPMDateSetInDatabase As Boolean
            Dim foundRows As DataRow()
            Dim bIsPreCassetted As Boolean
            Dim bHistoRefLinked As Boolean
            Dim objBatch As New HistopathologyLib.clsBatch

            bIsPreCassetted = IsBatchPreCassetted(dsBatchDetails, CInt(Session.Item(SessionVars.SV_BatchID)))

            'If bIsPreCassetted Then
            dtPreBooked = dsBatchDetails.Tables(HistopathologyLib.clsBatch.ANIMAL_PREBOOKED_BLOCKS)
            'End If

            If Not objBlock.NewBlock(dtBatchBlocks, iBlockID, iBatchID, iAnimalID, dtPreBooked, sPreBookedBlockRef, bIsPreCassetted) Then
                Throw New Exception("Block.NewRecord return false")
            End If

            If Not objAnimal.GetAnimalData(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL), _
                                           iAnimalID, _
                                           sHistologyRef, _
                                           sSenderRef, _
                                           sNextBlockRef, _
                                           aRowStamp, _
                                           bHistoRefSetInDatabase, _
                                           GetAnimalData, _
                                           bPMDateSetInDatabase, _
                                           bHistoRefLinked, _
                                           dtPreBooked) Then
                Throw New Exception("Animal.GetAnimalData returned false.")
            End If

            Session.Item(SessionVars.Sv_BlockID) = iBlockID

            If bIsPreCassetted Then
                txtBlockReference.Text = sPreBookedBlockRef
            Else
                txtBlockReference.Text = sNextBlockRef
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Error initialising the Block Details page", ex)
        End Try
    End Sub

    Private Sub LoadLookupTypeList(ByRef ddl As DropDownList, ByVal lookuplist As Integer)
        Dim objDataTable As DataTable
        Dim objLookup As New HistopathologyLib.LookupData
        Dim objTissues As New HistopathologyLib.clsTissue
        Dim iAnimalID As Integer = CInt(Session.Item(SessionVars.SV_AnimalID))
        Dim iBatchID As Integer = CInt(Session.Item(SessionVars.SV_BatchID))

        Try
            If chkUseWholeTissueList.Visible = True Then
                If chkUseWholeTissueList.Checked = True Then
                    objDataTable = objLookup.GetLookupData(lookuplist)
                Else
                    objDataTable = objTissues.GetBatchAnimalTissues(iBatchID, iAnimalID)
                End If
            Else
                objDataTable = objLookup.GetLookupData(lookuplist)
            End If

            If Not (objDataTable Is Nothing) Then
                ddl.DataSource = objDataTable
                ddl.DataValueField = "Code"
                ddl.DataTextField = "Description"
                ddl.DataBind()
                Common.AddItemToDropDownList(ddl)
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve 'Lookup' list.", ex)
        End Try
    End Sub

    Private Sub InitialiseScreenWithBlockDetails()
        Try
            Dim dsBatchData As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            If Not dsBatchData Is Nothing Then
                Dim iBlockID As Integer = CType(Session.Item(SessionVars.Sv_BlockID), Integer)
                Dim sFilter As String
                Dim foundRows As DataRow()

                sFilter = "ID=" & iBlockID
                foundRows = dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE).Select(sFilter)

                'Initialise the block details
                If foundRows.Length > 0 Then
                    txtBlockReference.Text = foundRows(0)("BlockRef").ToString()
                    txtCustomerRef.Text = foundRows(0)("CustomerRef").ToString()
                    chkRepeatBlock.Checked = GetRowColumnData(foundRows(0)("RepeatBlock"))
                    txtComments.Text = foundRows(0)("Comment").ToString()
                Else
                    Throw New Exception("No Block to initialise screen from.")
                End If

                'Remember the original block reference. When validating if the block is being editing and
                ' and the block ref hasnt changed no need to check it its already been used.
                Session.Item(SessionVars.SV_BlockRef) = txtBlockReference.Text()

                If CType(Session.Item(SessionVars.SV_EditingBlock), Boolean) = True Then
                    HideTests()
                    DisplayBlockLevelTests(iBlockID)
                End If
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Error initialising the Block Details page.", ex)
        End Try
    End Sub

    Private Sub CreateMultiBlocks()
        Dim iNumberBlocks As Integer
        Dim iCurrentBlockID As Integer = CType(Session.Item(SessionVars.Sv_BlockID), Integer)
        Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
        Dim dtAnimal As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL)
        Dim objAnimal As New HistopathologyLib.clsAnimal
        Dim sBlockRef As String = txtBlockReference.Text
        Dim bIsPreCassetted As Boolean = IsBatchPreCassetted(dsBatchDetails, CInt(Session.Item(SessionVars.SV_BatchID)))
        Dim sNextBlockRef As String

        If Not txtNoBlocks.Text = "" Then
            iNumberBlocks = Convert.ToInt32(txtNoBlocks.Text)
            If iNumberBlocks > 1 And iCurrentBlockID <> 0 Then
                Dim objBlocks As New HistopathologyLib.clsBlock
                If Not objBlocks.CreateMultiBlocks(dsBatchDetails, iNumberBlocks, iCurrentBlockID, sBlockRef, CInt(Session.Item(SessionVars.SV_AnimalID)), CInt(Session.Item(SessionVars.SV_BatchID)), bIsPreCassetted) Then
                    Throw New Exception("Block.CreateMultiBlocks return false")
                End If
            End If
        End If

        Dim iBlockRef As Integer = CInt(sBlockRef)
        iBlockRef += 1
        If iBlockRef < 10 Then
            sBlockRef = "0" & CStr(iBlockRef)
        Else
            sBlockRef = CStr(iBlockRef)
        End If

        If Not objAnimal.GetAnimalNextBlock(dtAnimal, CInt(Session.Item(SessionVars.SV_AnimalID)), sNextBlockRef) Then
            Throw New Exception("Animal.GetAnimalNextBlock returned false.")
        End If

        If Not IsBatchPreCassetted(dsBatchDetails, CInt(Session.Item(SessionVars.SV_BatchID))) Then
            If Convert.ToInt32(sBlockRef) > Convert.ToInt32(sNextBlockRef) Then
                If Not objAnimal.UpdateAnimalNextBlock(dtAnimal, CInt(Session.Item(SessionVars.SV_AnimalID)), sBlockRef) Then
                    Throw New Exception("Animal.UpdateAnimalNextBlock returned false.")
                End If
            End If
        End If
        'If IsBatchPreCassetted(dsBatchDetails, CInt(Session.Item(SessionVars.SV_BatchID))) Then
        '    objAnimal.test(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE), dsBatchDetails.Tables(HistopathologyLib.clsBatch.ANIMAL_PREBOOKED_BLOCKS), CInt(Session.Item(SessionVars.Sv_BlockID)), CInt(Session.Item(SessionVars.SV_AnimalID)), txtBlockReference.Text())
        'End If
    End Sub

    Private Function FormatBlockRef(ByVal sBlockRef As String) As String
        Dim iBlockRef As Integer
        iBlockRef = Convert.ToInt32(sBlockRef) + 1
        If iBlockRef < 10 Then
            Return "0" & Convert.ToString(iBlockRef)
        Else
            Return Convert.ToString(iBlockRef)
        End If
    End Function


    Private Function UpdateSessionWithBlockDetails() As Boolean
        Try
            If ValidateRequiredData() Then
                UpdateBlockDetails()
                CreateMultiBlocks()
                Return True
            Else
                Return False
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to Save Block Details.", ex)
        End Try

    End Function

    Private Function UpdateBlockDetails() As Boolean
        Dim dsBatchData As DataSet = Session.Item(SessionVars.SV_BatchDetails)
        If Not dsBatchData Is Nothing Then

            Dim dtPreBooked As DataTable = dsBatchData.Tables(HistopathologyLib.clsBatch.ANIMAL_PREBOOKED_BLOCKS)
            Dim iBlockID As Integer = CType(Session.Item(SessionVars.Sv_BlockID), Integer)
            Dim iBatchID As Integer = CType(Session.Item(SessionVars.SV_BatchID), Integer)
            Dim iAnimalID As Integer = CType(Session.Item(SessionVars.SV_AnimalID), Integer)
            Dim sFilter As String
            Dim foundRows As DataRow()
            Dim foundBatch As DataRow()
            Dim li As ListItem
            Dim objAnimal As New HistopathologyLib.clsAnimal
            Dim objBlock As New HistopathologyLib.clsBlock

            sFilter = "ID=" & iBlockID
            foundRows = dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE).Select(sFilter)

            If foundRows.Length > 0 Then
                foundRows(0)("BlockRef") = FormatEmptyString(txtBlockReference.Text())
                foundRows(0)("CustomerRef") = FormatEmptyString(txtCustomerRef.Text())
                foundRows(0)("RepeatBlock") = FormatEmptyString(chkRepeatBlock.Checked())
                foundRows(0)("Comment") = FormatEmptyString(txtComments.Text())
            Else
                Throw New Exception("No Block to add block details.")
            End If

            If IsBatchPreCassetted(dsBatchData, CInt(Session.Item(SessionVars.SV_BatchID))) Then
                objAnimal.test(dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE), dtPreBooked, iBlockID, iAnimalID, txtBlockReference.Text(), CInt(Session.Item(SessionVars.SV_BatchID)))
            End If

            'Check the values in the antibodies, stains checkbox list and add to their respective datatables.
            UpdateCheckBoxData(chkblSpecialStain, dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_STAIN))

            UpdateCheckBoxData(chkblAntibodies, dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANTIBODIES))

            UpdateCheckBoxData(chkblHistology, dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_HISTOLOGY))

        End If
    End Function

    Private Function ValidateRequiredData() As Boolean
        Dim iSelectedIndex As Integer = chkblHistology.SelectedIndex
        Dim dsBatchDetails As DataSet = Session.Item(SessionVars.SV_BatchDetails)
        Dim dtView As DataView = Session.Item(SessionVars.SV_TissuesView)
        Dim objAnimal As New HistopathologyLib.clsAnimal
        Dim sFilter As String
        Dim drFoundRows As DataRow()
        Dim drRow As DataRow
        Dim iAnimalID As Integer = CInt(Session.Item(SessionVars.SV_AnimalID))
        Dim sOriginalBlockRef As String = CStr(Session.Item(SessionVars.SV_BlockRef))
        Dim dtPreBookedBlocks As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.ANIMAL_PREBOOKED_BLOCKS)
        Dim iBlockId As Integer = CInt(Session.Item(SessionVars.Sv_BlockID))
        Dim bEoSelected As Boolean = False
        Dim bIsPreBooked As Boolean = False
        Dim objBlock As New HistopathologyLib.clsBlock
        Dim bBatchPreCassetted As Boolean = False
        Dim iNumberOfPreBooked As Integer = 0
        Dim iNumberRequired As Integer = 0
        Dim bAssignBlocks As Boolean = CType(Session.Item(SessionVars.SV_AssignBlocks), Boolean)
        iNumberRequired = CInt(txtNoBlocks.Text)
        bBatchPreCassetted = IsBatchPreCassetted(dsBatchDetails, CInt(Session.Item(SessionVars.SV_BatchID)))

        If objAnimal.CheckBlockIsPreBooked(iAnimalID, txtBlockReference.Text, dtPreBookedBlocks) Then
            ' If batch is precassetted check that the block has been prebooked. If not check that a pre booked block has not been used.
            If Not bBatchPreCassetted Then
                ' Take out the used check if the user can always re use a booked block for unstained etc
                If bAssignBlocks Or Not objBlock.CheckPreBlockedUsed(dtPreBookedBlocks, iAnimalID, txtBlockReference.Text) Then
                    lblError.Visible = True
                    lblError.ToolTip = "The block reference entered has been pre-booked. Please use another available block."
                    ctlErrorDiv.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
                    Return False
                End If
            Else
                If Not sOriginalBlockRef = txtBlockReference.Text() Then
                    If iNumberRequired > 1 Then
                        objAnimal.CheckPreBookedBlocksExist(iAnimalID, dsBatchDetails, iNumberOfPreBooked)
                        If iNumberRequired > iNumberOfPreBooked Then
                            lblError.Visible = True
                            lblError.ToolTip = "Cannot create the required number of blocks as not enough pre-booked blocks are available."
                            ctlErrorDiv.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
                            Return False
                        End If
                    End If

                    If Not objBlock.CheckPreBlockedFree(dtPreBookedBlocks, iAnimalID, txtBlockReference.Text) Then
                        lblError.Visible = True
                        lblError.ToolTip = "The pre-booked block reference entered has been used. Please use another available block."
                        ctlErrorDiv.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
                        Return False
                    End If
                End If

                bIsPreBooked = True
            End If
        Else
            If bBatchPreCassetted Then
                If Not sOriginalBlockRef = txtBlockReference.Text() Then
                    lblError.Visible = True
                    lblError.ToolTip = "The block reference entered has either been used or has not been pre-booked. Please use another available block."
                    ctlErrorDiv.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
                    Return False
                End If
            End If
        End If

        If chkRepeatBlock.Enabled = False Then
            If CType(Session.Item(SessionVars.SV_EditingBlock), Boolean) = False Then
                If Not bIsPreBooked Then
                    sFilter = "AnimalID=" & iAnimalID & " AND BlockRef=" & "'" & txtBlockReference.Text & "'"
                    drFoundRows = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE).Select(sFilter)
                    If Not drFoundRows Is Nothing Then
                        If drFoundRows.Length >= 1 Then
                            lblError.Visible = True
                            lblError.ToolTip = "You can only enter a used Block Ref if you tick the additional request box"
                            ctlErrorDiv.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
                            Return False
                        End If
                    End If
                End If
            Else
                If Not sOriginalBlockRef = txtBlockReference.Text() Then
                    If Not bIsPreBooked Then
                        sFilter = "AnimalID=" & iAnimalID & " AND BlockRef=" & "'" & txtBlockReference.Text & "'"
                        drFoundRows = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE).Select(sFilter)
                        If Not drFoundRows Is Nothing Then
                            If drFoundRows.Length >= 1 Then
                                lblError.Visible = True
                                lblError.ToolTip = "You can only enter a used Block Ref if you tick the additional request box"
                                ctlErrorDiv.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
                                Return False
                            End If
                        End If
                    End If
                End If
            End If
        End If

        'Check atleast one histology has have been selected
        If iSelectedIndex = -1 Then
            lblError.Visible = True
            lblError.ToolTip = "Must add atleast one histology to the block."
            ctlErrorDiv.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
            Return False
        Else
            Dim li As ListItem
            For Each li In chkblHistology.Items

                'Eo cannot be selected with any other tests
                If li.Value = 1 And li.Selected = True Then
                    bEoSelected = True
                Else
                    If li.Selected = True And bEoSelected Then
                        lblError.Visible = True
                        lblError.ToolTip = "EO selected, no other tests can be selected."
                        ctlErrorDiv.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
                        Return False
                    End If
                End If

                'Special Stain selected check atleast 1 stain has been selected
                If (li.Value = 3 And li.Selected = True) Then
                    If chkblSpecialStain.SelectedIndex = -1 Then
                        lblError.Visible = True
                        lblError.ToolTip = "Special stain selected, atleast one stain must be selected."
                        ctlErrorDiv.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
                        Return False
                    End If
                End If

                'IHC Selected, check atleast 1 test has been selected
                If (li.Value = 4 And li.Selected) _
                    Or (li.Value = 6 And li.Selected = True) Then
                    If chkblAntibodies.SelectedIndex = -1 Then
                        lblError.Visible = True
                        lblError.ToolTip = "IHC selected, atleast one test must be selected."
                        ctlErrorDiv.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
                        Return False
                    End If
                End If
            Next
        End If

        'If this isnt a repeat block, the block must have a tissue list
        If chkRepeatBlock.Checked = False Then
            If dtView.Count = 0 Then
                lblError.Visible = True
                lblError.ToolTip = "Must add atleast one tissue to the block."
                ctlErrorDiv.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
                Return False
            End If
        End If

        'Check that the block reference will not be greater than 999
        If Not txtBlockReference.Text = "" And Not txtNoBlocks.Text = "" Then
            If CInt(txtBlockReference.Text) + CInt(txtNoBlocks.Text) > 1000 Then
                lblError.Visible = True
                lblError.ToolTip = "Adding the required blocks will take the Block Ref above 999. Any sample is only allowed a maximum of 999 blocks."
                ctlErrorDiv.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
                Return False
            End If
        End If

        lblError.Visible = False
        ctlErrorDiv.InnerHtml = ""
        Return True
    End Function

    Private Sub DisplayStaticData()
        Try
            Dim objAnimal As New HistopathologyLib.clsAnimal
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtBatch As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)
            Dim iAnimalID As Integer = CInt(Session.Item(SessionVars.SV_AnimalID))
            Dim bAssignBlocks As Boolean = CType(Session.Item(SessionVars.SV_AssignBlocks), Boolean)
            Dim sHistologyRef As String
            Dim sSenderRef As String
            Dim sNextBlockRef As String
            Dim aRowStamp As System.Array
            Dim bHistoRefSetInDatabase As Boolean
            Dim sPMDate As String
            Dim bPMDateSetInDatabase As Boolean
            Dim bHistoRefLinked As Boolean

            If Not objAnimal.GetAnimalData(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL), _
                                           iAnimalID, _
                                           sHistologyRef, _
                                           sSenderRef, _
                                           sNextBlockRef, _
                                           aRowStamp, _
                                           bHistoRefSetInDatabase, _
                                           sPMDate, _
                                           bPMDateSetInDatabase, _
                                           bHistoRefLinked) Then

                Throw New Exception("Animal.GetAnimalData returned false.")
            End If

            txtSenderRef.Text = sSenderRef
            If Not sHistologyRef = "" Then
                txtHistologyRef.Text = sHistologyRef
            End If

            ctlDiv.Visible = False
            If Not dtBatch Is Nothing And dtBatch.Rows.Count > 0 Then
                If dtBatch.Rows(0)("Cassetted") = 0 And bAssignBlocks = True Then
                    ctlDiv.Visible = True
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Block Details failed to display static data", ex)
        End Try

    End Sub

    Private Sub ClearControls()
        Dim SelectedArray As ArrayList = CType(Session.Item(SessionVars.SV_SelectedHistologyArray), ArrayList)
        chkRepeatBlock.Checked = False
        txtComments.Text = ""
        txtCustomerRef.Text = ""
        txtNoBlocks.Text = "1"
        lblError.Visible = False
        InitialiseTissuesGrid()
        If chkbCarryTests.Checked = False Then
            'If use doesnt want to carry forward the tests clear them
            chkblHistology.SelectedIndex = -1
            'Clear the array which indicates what items are selected
            SelectedArray.Clear()
            chkblSpecialStain.SelectedIndex = -1
            chkblSpecialStain.Enabled = False
            chkblAntibodies.SelectedIndex = -1
            chkblAntibodies.Enabled = False
        End If
    End Sub

    Private Sub UpdateCheckBoxData(ByVal chkblist As CheckBoxList, ByRef dtData As DataTable)
        Dim objChkbl As New HistopathologyLib.clsCheckBoxData
        Dim iID As Integer = CInt(Session.Item(SessionVars.Sv_BlockID))
        Dim li As ListItem
        Dim sFilter As String
        Dim drFoundRow As DataRow()

        For Each li In chkblist.Items
            sFilter = "Code=" & "'" & li.Value & "'" & " AND BlockID=" & iID
            drFoundRow = dtData.Select(sFilter)
            If li.Selected = True Then
                'if its a new item
                If Not drFoundRow Is Nothing And drFoundRow.Length = 0 Then
                    If Not objChkbl.NewItem(dtData, li.Value, iID, "BlockID") Then
                        Throw New Exception("CheckBoxList.NewItem returned false.")
                    End If
                End If
            Else
                'If its been unchecked
                If Not drFoundRow Is Nothing And drFoundRow.Length = 1 Then
                    drFoundRow(0).Delete()
                End If
            End If
        Next
    End Sub

#End Region

#Region "Event Handlers"

    Private Sub grdTissues_ItemDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.DataGridItemEventArgs) Handles grdTissues.ItemDataBound
        ' populate template column values here
        Try
            Dim drv As DataRowView = CType(e.Item.DataItem, DataRowView)
            If Not drv Is Nothing Then
                Dim lblTissueCode As Label = Nothing
                Dim ddlTissueCode As DropDownList = Nothing
                Dim lblNoPieces As Label = Nothing
                Dim txtNoPieces As TextBox = Nothing

                If e.Item.ItemType = ListItemType.EditItem Then
                    ' populate edit mode controls
                    ddlTissueCode = CType(e.Item.FindControl("ddlTissueCodeEdit"), DropDownList)
                    txtNoPieces = CType(e.Item.FindControl("txtNoPiecesEdit"), TextBox)
                ElseIf e.Item.ItemType = ListItemType.Item _
                OrElse e.Item.ItemType = ListItemType.AlternatingItem _
                OrElse e.Item.ItemType = ListItemType.SelectedItem Then
                    ' populate display mode controls
                    lblTissueCode = CType(e.Item.FindControl("lblTissueCodeDisplay"), Label)
                    lblNoPieces = CType(e.Item.FindControl("lblNoPiecesDisplay"), Label)
                End If

                If Not lblNoPieces Is Nothing Then
                    If Not IsDBNull(drv("NoPieces")) Then
                        lblNoPieces.Text = drv("NoPieces")
                    Else
                        lblNoPieces.Text = ""
                    End If
                End If
                If Not txtNoPieces Is Nothing Then
                    If Not IsDBNull(drv("NoPieces")) Then
                        txtNoPieces.Text = drv("NoPieces")
                    Else
                        txtNoPieces.Text = "1"
                    End If
                End If

                If Not lblTissueCode Is Nothing Then
                    If Not IsDBNull(drv("TissueCode")) Then
                        lblTissueCode.Text = GetListType(drv("TissueCode"), LOOKUP_TISSUE_CODE)
                    Else
                        lblTissueCode.Text = ""
                    End If
                End If
                If Not ddlTissueCode Is Nothing Then
                    LoadLookupTypeList(ddlTissueCode, LOOKUP_TISSUE_CODE)
                    If IsDBNull(drv("TissueCode")) Then
                        SelectItemInDropDownList(ddlTissueCode, "-1")
                    Else
                        SelectItemInDropDownList(ddlTissueCode, drv("TissueCode"))
                    End If
                End If
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to bind template columns in the Tissues grid", ex)
        End Try
    End Sub

    Private Sub TissuesPager_BeforeDataChanged(ByVal sender As System.Object, ByRef e As HistopathologySystem.DataGridPagerEventArgs) Handles TissuesPager.BeforeDataChanged
        e.bCarryOnEditing = m_bContinueEditing
    End Sub

    Private Sub TissuesPager_Save(ByVal sender As Object, ByVal e As HistopathologySystem.DataGridPagerEventArgs) Handles TissuesPager.RowSave
        Try
            'save template column values to the dataset here
            Dim lblTissueError As Label = Nothing
            lblTissueError = CType(e.GridRow.FindControl("lblTissueError"), Label)
            Dim lst As DropDownList = CType(e.GridRow.FindControl("ddlTissueCodeEdit"), DropDownList)

            m_bContinueEditing = False

            If Not lblTissueError Is Nothing Then
                If lst.SelectedItem.Value = "" Then
                    lblTissueError.Visible = True
                    m_bContinueEditing = True
                Else
                    lblTissueError.Visible = False
                    m_bContinueEditing = False

                    e.DataTableRow("TissueCode") = lst.SelectedItem.Value

                    Dim txtNoPieces As TextBox = CType(e.GridRow.FindControl("txtNoPiecesEdit"), TextBox)
                    e.DataTableRow("NoPieces") = txtNoPieces.Text

                    'if the row is new, add a reference to the Block it belongs to
                    If e.DataTableRow.RowState = DataRowState.Added Then
                        e.DataTableRow("BlockID") = Session(SessionVars.Sv_BlockID)
                    End If
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to save tissue details to datatable.", ex)
        End Try
    End Sub

    Private Sub TissuesPager_EditModeStart(ByVal sender As Object, ByVal e As DataGridPagerEventArgs) Handles TissuesPager.EditModeStart
        Try
            Dim lblTissueError As Label = Nothing
            btnDone.Enabled = False
            btnCancel.Enabled = False
            btnAddBlock.Enabled = False
            rfvBlockRef.Enabled = False
            revBlockRef.Enabled = False
            rfvNoBlocks.Enabled = False
            revNoBlocks.Enabled = False
            chkUseWholeTissueList.Enabled = False

            lblTissueError = CType(e.GridRow.FindControl("lblTissueError"), Label)

            If Not lblTissueError Is Nothing Then
                lblTissueError.Visible = False
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to initialise editmodestart", ex)
        End Try

    End Sub

    Private Sub TissuesPager_EditModeStop(ByVal sender As Object, ByVal e As System.EventArgs) Handles TissuesPager.EditModeStop
        btnDone.Enabled = True
        btnCancel.Enabled = True
        btnAddBlock.Enabled = True
        btnAddBlock.Enabled = True
        rfvBlockRef.Enabled = True
        revBlockRef.Enabled = True
        rfvNoBlocks.Enabled = True
        revNoBlocks.Enabled = True

        chkUseWholeTissueList.Enabled = True
    End Sub

    Private Sub chkblHistology_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkblHistology.SelectedIndexChanged
        Dim sItemSelected As String
        Dim iPosition As Integer
        Dim li As ListItem
        Dim aArray As ArrayList = CType(Session(SessionVars.SV_SelectedHistologyArray), ArrayList)
        li = GetCheckListSelectedItem(sItemSelected, aArray)
        Dim liCheck As ListItem

        If Not li Is Nothing Then
            If li.Text = "EO" Then
                If li.Selected = True Then
                    DisableSpecialStains(aArray)
                    DisableAntibodies(aArray)
                    For Each liCheck In chkblHistology.Items
                        If Not liCheck.Text = "EO" Then
                            liCheck.Selected = False
                            aArray.Remove(liCheck.Text.ToString())
                        End If
                    Next
                    UnCheckArchive(aArray)
                End If
            ElseIf li.Text = "Archive" Then
                If li.Selected = True Then
                    DisableSpecialStains(aArray)
                    DisableAntibodies(aArray)
                    For Each liCheck In chkblHistology.Items
                        If Not liCheck.Text = "Archive" Then
                            liCheck.Selected = False
                            aArray.Remove(liCheck.Text.ToString())
                        End If
                    Next
                End If
            ElseIf li.Text = "Special Stain" Then
                If li.Selected = True Then
                    chkblSpecialStain.Enabled = True
                    UnCheckEO(aArray)
                    UnCheckArchive(aArray)
                Else
                    DisableSpecialStains(aArray)
                End If
            ElseIf li.Text = "IHC - PrP" Or li.Text = "IHC - Other" Then
                If li.Selected = True Then
                    UnCheckEO(aArray)
                    UnCheckArchive(aArray)
                    chkblAntibodies.Enabled = True
                Else
                    DisableAntibodies(aArray)
                End If
            Else
                If li.Selected = True Then
                    UnCheckEO(aArray)
                    UnCheckArchive(aArray)
                End If
            End If
        End If
    End Sub

    Private Function ValidateMandatoryFields() As Boolean
        Try
            revBlockRef.Validate()
            rfvBlockRef.Validate()
            rfvNoBlocks.Validate()
            revNoBlocks.Validate()

            If Not revBlockRef.IsValid Or _
                Not rfvBlockRef.IsValid Or _
                Not rfvNoBlocks.IsValid Or _
                Not revNoBlocks.IsValid Then
                lblError.Visible = False
                ctlErrorDiv.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
                Return False
            End If

            Return True
        Catch ex As Exception
            clsAppError.DisplayError("Failed to validate Mandatory fields.", ex)
        End Try
    End Function

    Private Sub btnAddBlock_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAddBlock.Click
        Try
            If ValidateMandatoryFields() Then
                If ValidateRequiredData() Then
                    UpdateBlockDetails()
                    CreateMultiBlocks()
                    CreateNewRecord()
                    ClearControls()
                Else
                    Exit Sub
                End If
            Else
                Exit Sub
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to Save Block Details.", ex)
        End Try

        Dim dsBatchDetails As DataSet
        dsBatchDetails = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
        If IsBatchPreCassetted(dsBatchDetails, CInt(Session.Item(SessionVars.SV_BatchID))) Then
            Dim objAnimal As New HistopathologyLib.clsAnimal
            If Not objAnimal.CheckPreBookedBlocksAvailable(CInt(Session.Item(SessionVars.SV_AnimalID)), dsBatchDetails.Tables(HistopathologyLib.clsBatch.ANIMAL_PREBOOKED_BLOCKS)) Then
                Response.Redirect("SubmissionDetailsBlock.aspx")
            End If
        End If
    End Sub

    Private Sub btnDone_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnDone.Click
        Try
            If ValidateMandatoryFields() Then
                If ValidateRequiredData() Then
                    UpdateBlockDetails()
                    CreateMultiBlocks()
                Else
                    Exit Sub
                End If
            Else
                Exit Sub
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to Save Block Details.", ex)
        End Try

        Try
            Dim objCrumbArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
            If Not objCrumbArrayList Is Nothing Then
                objCrumbArrayList(1) = "Submission Samples"
                objCrumbArrayList(2) = "Blocking"
                objCrumbArrayList(3) = "Sample Blocks"
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Bread Crumb Error, BlockDetails.aspx.", ex)
        End Try

        Response.Redirect("SubmissionDetailsBlock.aspx")
    End Sub

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        Try
            Dim dsBatchDetails As DataSet = CType(Session(SessionVars.SV_BatchDetails), DataSet)
            Dim dtTissues As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TISSUES)
            Dim dtOldData As DataTable = CType(Session.Item(SessionVars.SV_TissuesBeforeChanges), DataTable)
            Dim iCurrentBlockID As Integer = CType(Session(SessionVars.Sv_BlockID), Integer)
            Dim drRow As DataRow
            Dim drFoundRows As DataRow()
            Dim drFoundRow As DataRow
            Dim sFilter As String
            Dim iArrayCount As Integer = 0

            If Session.Item(SessionVars.SV_EditingBlock) = False Then
                Dim objBlock As New HistopathologyLib.clsBlock()
                If Not objBlock.DeleteBlockData(dsBatchDetails, iCurrentBlockID, IsBatchPreCassetted(dsBatchDetails, CInt(Session.Item(SessionVars.SV_BatchID)))) Then
                    Throw New Exception("Block.DeleteBlockData returned false.")
                End If
            Else
                Dim bFound = False
                ' Need to revert all tissue changes back to how they were before the edit.
                sFilter = "BlockID=" & Convert.ToString(iCurrentBlockID)
                drFoundRows = dtTissues.Select(sFilter)

                If Not drFoundRows Is Nothing Then
                    For Each drFoundRow In drFoundRows
                        dtTissues.Rows.Remove(drFoundRow)
                        drFoundRow.Delete()
                    Next
                End If
                If Not dtOldData Is Nothing Then
                    For Each drRow In dtOldData.Rows
                        dtTissues.ImportRow(drRow)
                    Next
                End If
            End If

            Session.Item(SessionVars.Sv_BlockID) = 0
            Session.Remove(SessionVars.SV_TissuesBeforeChanges)

        Catch ex As Exception
            clsAppError.DisplayError("BlockDetails, Failed to Cancel changes", ex)
        End Try

        Try
            Dim objCrumbArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
            If Not objCrumbArrayList Is Nothing Then
                objCrumbArrayList(1) = "Submission Samples"
                objCrumbArrayList(2) = "Blocking"
                objCrumbArrayList(3) = "Sample Blocks"
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Bread Crumb Error, BlockDetails.aspx.", ex)
        End Try

        Response.Redirect("SubmissionDetailsBlock.aspx")
    End Sub

    Private Sub VLAHeader1_HomeClick(ByVal sender As Object, ByVal e As HistopathologySystem.HomeLinkEventArgs) Handles VLAHeader1.HomeClick
        Dim sMessage As System.Text.StringBuilder = New System.Text.StringBuilder()

        If CType(Session.Item(SessionVars.SV_EditingBatch), Boolean) Then
            sMessage.Append("You are currently editing a submission. Any changes that you have made since you last saved the submission will be lost. Are you sure you wish to exit?")
        ElseIf CType(Session.Item(SessionVars.SV_CreatingNewBatch), Boolean) Then
            sMessage.Append("You are currently creating a new submission. If you exit now all the data you have entered will be lost. Are you sure you wish to exit?")
        ElseIf CType(Session.Item(SessionVars.SV_AssignBlocks), Boolean) Then
            sMessage.Append("You are currently assigning tissues to blocks. Any block assignment that you have completed since you last saved will be lost. Are you sure you wish to exit?")
        Else
            sMessage.Append("Any changes that have been made will be discarded, are you sure you wish to exit without saving?")
        End If

        Page.RegisterStartupScript("navigate", PromptBeforeNavigateScript(sMessage.ToString(), "Home.aspx"))
        e.bNavigateHome = False
    End Sub

#End Region

#Region "Lookup list population"

    Private Sub LoadCheckBoxLists()
        Dim objDataTable As DataTable
        Dim objLookup As New HistopathologyLib.LookupData()
        Dim li As New ListItem()
        li.Text = "Other"
        li.Value = "Other"
        Dim li2 As New ListItem()
        li2.Text = "Other"
        li2.Value = "Other"

        Try
            objDataTable = objLookup.GetHistologyLookupData()

            If Not objDataTable Is Nothing Then
                chkblHistology.DataSource = objDataTable
                chkblHistology.DataValueField = "Code"
                chkblHistology.DataTextField = "Description"
                chkblHistology.DataBind()
            End If

            HideTSENonTSEOptions()

            'Check the submission type and load the correct list
            If CType(Session.Item(SessionVars.SV_SubmissionType), Integer) = SUBMISSION_NONTSE Then
                objDataTable = objLookup.GetLookupData(LOOKUP_NONTSE_ANTIBODIES)
            Else
                objDataTable = objLookup.GetLookupData(LOOKUP_TSE_ANTIBODIES)
            End If

            If Not objDataTable Is Nothing Then
                chkblAntibodies.DataSource = objDataTable
                chkblAntibodies.DataValueField = "Code"
                chkblAntibodies.DataTextField = "Description"
                chkblAntibodies.DataBind()
                chkblAntibodies.Enabled = False
                chkblAntibodies.Items.Add(li)
            End If

            objDataTable = objLookup.GetLookupData(LOOKUP_SPECIAL_STAIN)

            If Not objDataTable Is Nothing Then
                chkblSpecialStain.DataSource = objDataTable
                chkblSpecialStain.DataValueField = "Code"
                chkblSpecialStain.DataTextField = "Description"
                chkblSpecialStain.DataBind()
                chkblSpecialStain.Enabled = False
                chkblSpecialStain.Items.Add(li2)
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve 'Histology and Antibodies' lists.", ex)
        End Try
    End Sub

#End Region

#Region "Checkbox list functions"

    Private Sub HideTSENonTSEOptions()
        Dim li As ListItem
        Dim iCount As Integer = 0

        'Only display the Histology columns which are relevent to the type of submission
        If Session(SessionVars.SV_SubmissionType) = SUBMISSION_NONTSE Then
            For iCount = chkblHistology.Items.Count - 1 To 0 Step -1
                'Get rid of the IHC-Prp & H&E(BSE) options for Non TSE
                If chkblHistology.Items(iCount).Value = 4 Or chkblHistology.Items(iCount).Value = 5 Then
                    chkblHistology.Items.RemoveAt(iCount)
                End If
            Next
        Else
            For Each li In chkblHistology.Items
                'Get rid of the IHC-Other option for TSE
                If li.Value = 6 Then
                    chkblHistology.Items.Remove(li)
                    Exit For
                End If
            Next
        End If
    End Sub

    Private Sub HideTests()
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtHistology As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_HISTOLOGY_TABLE)
            Dim dtAntibodies As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_ANTIBODIES_TABLE)
            Dim dtSpecialStain As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_STAIN_TABLE)
            Dim iCount As Integer
            Dim sFilter As String
            Dim drFoundRows As DataRow()

            'Remove any histology types that were not chosen at batch level
            For iCount = chkblHistology.Items.Count - 1 To 0 Step -1
                sFilter = "Code=" & "'" & chkblHistology.Items(iCount).Value & "'"
                drFoundRows = dtHistology.Select(sFilter)

                If Not drFoundRows Is Nothing And drFoundRows.Length = 0 Then
                    chkblHistology.Items.RemoveAt(iCount)
                End If
            Next

            'Remove any special stains that were not chosen at batch level
            For iCount = chkblSpecialStain.Items.Count - 1 To 0 Step -1
                sFilter = "Code=" & "'" & chkblSpecialStain.Items(iCount).Value & "'"
                drFoundRows = dtSpecialStain.Select(sFilter)

                If Not drFoundRows Is Nothing And drFoundRows.Length = 0 Then
                    chkblSpecialStain.Items.RemoveAt(iCount)
                End If
            Next

            'Remove any antibodies that were not chosen at batch level
            For iCount = chkblAntibodies.Items.Count - 1 To 0 Step -1
                sFilter = "Code=" & "'" & chkblAntibodies.Items(iCount).Value & "'"
                drFoundRows = dtAntibodies.Select(sFilter)

                If Not drFoundRows Is Nothing And drFoundRows.Length = 0 Then
                    chkblAntibodies.Items.RemoveAt(iCount)
                End If
            Next

        Catch ex As Exception
            clsAppError.DisplayError("Failed to hide tests not selected at batch level.", ex)
        End Try
    End Sub

    Private Sub DisplayBatchLevelTests()
        Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
        Try
            If Not dsBatchDetails Is Nothing Then
                Dim dtDataTable As DataTable
                Dim aArray As ArrayList = CType(Session(SessionVars.SV_SelectedHistologyArray), ArrayList)
                Dim iCount As Integer
                Dim sFilter As String
                Dim drFoundRows As DataRow()
                Dim dr As DataRow
                Dim li As ListItem
                'Initialise the histology

                dtDataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_HISTOLOGY_TABLE)
                drFoundRows = dtDataTable.Select(sFilter)
                For Each dr In drFoundRows
                    For Each li In chkblHistology.Items
                        If dr("Code") = li.Value Then
                            li.Selected = True
                            aArray.Add(li.Text)
                        End If
                    Next
                Next

                'Initialise the Antibodies

                dtDataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_ANTIBODIES_TABLE)
                drFoundRows = dtDataTable.Select(sFilter)
                For Each dr In drFoundRows
                    For Each li In chkblAntibodies.Items
                        If dr("Code") = li.Value Then
                            chkblAntibodies.Enabled = True
                            li.Selected = True
                        End If
                    Next
                Next

                'Initialsise the Special Stains
                dtDataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_STAIN_TABLE)
                drFoundRows = dtDataTable.Select(sFilter)
                For Each dr In drFoundRows
                    For Each li In chkblSpecialStain.Items
                        If dr("Code") = li.Value Then
                            chkblSpecialStain.Enabled = True
                            li.Selected = True
                        End If
                    Next
                Next
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to initialise the batch level tests.", ex)
        End Try
       
    End Sub

    Private Sub DisplayBlockLevelTests(ByVal iBlockID As Integer)
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim aArray As ArrayList = CType(Session(SessionVars.SV_SelectedHistologyArray), ArrayList)
            Dim sFilter As String = ""
            Dim dtDataTable As DataTable
            Dim foundRows As DataRow()
            Dim dr As DataRow
            Dim li As ListItem
            sFilter = "BlockID=" & Convert.ToString(iBlockID)

            'Initialise the histology
            If dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_HISTOLOGY).Rows.Count <> 0 Then
                dtDataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_HISTOLOGY)
                foundRows = dtDataTable.Select(sFilter)
                For Each dr In foundRows
                    For Each li In chkblHistology.Items
                        If dr("Code") = li.Value Then
                            li.Selected = True
                            aArray.Add(li.Text)
                        End If
                    Next
                Next
            End If

            'Initialise the Antibodies
            If dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANTIBODIES).Rows.Count <> 0 Then
                dtDataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANTIBODIES)
                foundRows = dtDataTable.Select(sFilter)
                For Each dr In foundRows
                    For Each li In chkblAntibodies.Items
                        If dr("Code") = li.Value Then
                            chkblAntibodies.Enabled = True
                            li.Selected = True
                        End If
                    Next
                Next
            End If

            'Initialsise the Special Stains
            If dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_STAIN).Rows.Count <> 0 Then
                dtDataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_STAIN)
                foundRows = dtDataTable.Select(sFilter)
                For Each dr In foundRows
                    For Each li In chkblSpecialStain.Items
                        If dr("Code") = li.Value Then
                            chkblSpecialStain.Enabled = True
                            li.Selected = True
                        End If
                    Next
                Next
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to initialise the block level tests.", ex)
        End Try
    End Sub

    Private Sub DisableSpecialStains(ByRef aArray As ArrayList)
        Dim li As ListItem
        For Each li In chkblSpecialStain.Items
            If li.Selected = True Then
                aArray.Remove(li.Text)
                li.Selected = False
            End If
        Next
        chkblSpecialStain.Enabled = False
    End Sub

    Private Sub DisableAntibodies(ByRef aArray As ArrayList)
        Dim li As ListItem
        For Each li In chkblAntibodies.Items
            If li.Selected = True Then
                aArray.Remove(li.Text)
                li.Selected = False
            End If
        Next
        chkblAntibodies.Enabled = False
    End Sub

    Private Sub UnCheckEO(ByRef aArray As ArrayList)
        Dim li As ListItem
        For Each li In chkblHistology.Items
            If li.Text = "EO" Then
                li.Selected = False
                aArray.Remove(li.Text.ToString())
            End If
        Next
    End Sub

    Private Sub UnCheckArchive(ByRef aArray As ArrayList)
        Dim li As ListItem
        For Each li In chkblHistology.Items
            If li.Text = "Archive" Then
                li.Selected = False
                aArray.Remove(li.Text.ToString())
            End If
        Next
    End Sub

    Private Function GetCheckListSelectedItem(ByRef sText As String, ByVal aArray As ArrayList) As ListItem
        'This function is used to get the item in the ComboboxList that has just been selected.
        'Using comboboxList.selectedItem always returns the lowest indexed selected item rather
        'than the item just selected.

        Dim li As ListItem
        For Each li In chkblHistology.Items
            If li.Selected = True Then
                sText = li.Text
                If Not aArray.Contains(sText) Then
                    aArray.Add(sText)
                    Return li
                End If
            Else
                sText = li.Text
                If aArray.Contains(sText) Then
                    aArray.Remove(sText)
                    Return li
                End If
            End If
        Next
        Return li
    End Function

#End Region

#Region "Validation"

    Private Function SetClientValidation() As Boolean
        If HttpContext.Current.Request.Browser.JavaScript Then
            Dim scr As New System.Text.StringBuilder()

            scr.Append("<SCRIPT language=""Javascript"">" + vbNewLine)
            scr.Append("function ClientValidateBlockRef(sender, args)" + vbNewLine)
            scr.Append("{" + vbNewLine)
            scr.Append("    var sBlockRef = args.Value;" + vbNewLine)
            scr.Append("    if (sBlockRef == ""00"")" + vbNewLine)
            scr.Append("    {" + vbNewLine)
            scr.Append("        args.IsValid = false;" + vbNewLine)
            scr.Append("        return;" + vbNewLine)
            scr.Append("    }" + vbNewLine)
            scr.Append(vbNewLine)
            scr.Append("    if (sBlockRef == ""000"")" + vbNewLine)
            scr.Append("    {" + vbNewLine)
            scr.Append("        args.IsValid = false;" + vbNewLine)
            scr.Append("        return;" + vbNewLine)
            scr.Append("    }" + vbNewLine)
            scr.Append(vbNewLine)
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

        If sBlockRef = "00" Then
            args.IsValid = False
            Exit Sub
        ElseIf sBlockRef = "000" Then
            args.IsValid = False
            Exit Sub
        Else
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
        End If

    End Sub

#End Region

End Class
