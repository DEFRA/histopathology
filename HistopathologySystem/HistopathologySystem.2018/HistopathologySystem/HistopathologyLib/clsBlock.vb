Imports libDataAccess.libDataAccess
Imports libDataAccess.libDataAccess.TBCultureDA
Imports System.Data.SqlClient



Public Class clsBlock

    Dim BlockIDs As New ArrayList()

    Public Const STATUS_USED As Integer = 1
    Public Const STATUS_PREBOOKED As Integer = 2
    Public Const STATUS_PREBOOKED_USED As Integer = 3

#Region "Handle Datatable"

    Public Function NewBlock(ByRef dtBlocks As DataTable, _
                             ByRef iBlockID As Integer, _
                             ByVal iBatchID As Integer, _
                             ByVal iAnimalID As Integer, _
                             ByRef dtPreBooked As DataTable, _
                             ByRef sBlockRef As String, _
                             Optional ByVal bIsPreCassetted As Boolean = False) As Boolean
        Try
            'If pre cassetted need to use pre booked block
            If bIsPreCassetted Then
                GetPreBookedBlock(dtPreBooked, dtBlocks, iAnimalID, iBlockID, sBlockRef)
            Else
                Dim dr As DataRow
                dr = dtBlocks.NewRow()
                iBlockID = dr("ID")
                dr("BatchID") = iBatchID
                dr("AnimalID") = iAnimalID
                dr("RepeatBlock") = False
                dr("Status") = STATUS_USED
                dr("Order") = GetOrder(dtBlocks)
                dtBlocks.Rows.Add(dr)
            End If

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBlockObject)
            Return False
        End Try
    End Function

    Public Function NewBlock(ByRef dtBlocks As DataTable, _
                             ByRef iBlockID As Integer, _
                             ByVal iBatchID As Integer, _
                             ByVal iAnimalID As Integer, _
                             ByVal sBlockRef As String) As Boolean
        Try
            Dim dr As DataRow
            dr = dtBlocks.NewRow()
            iBlockID = dr("ID")
            dr("BatchID") = iBatchID
            dr("AnimalID") = iAnimalID
            dr("BlockRef") = sBlockRef
            dr("RepeatBlock") = False
            dr("Status") = STATUS_USED
            dr("Order") = GetOrder(dtBlocks)
            dtBlocks.Rows.Add(dr)
            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBlockObject)
            Return False
        End Try
    End Function

    Private Function GetOrder(ByVal dtBlocks As DataTable) As Integer
        Dim max As Object
        max = dtBlocks.Compute("Max(Order)", "")
        If Not IsDBNull(max) Then
            Return CInt(max) + 1
        Else
            Return 0
        End If
    End Function
    Public Function NewBlock(ByRef dtBlocks As DataTable, _
                             ByRef iBlockID As Integer, _
                             ByVal dr As DataRow, _
                             ByVal sBlockRef As String, _
                             Optional ByVal bIsPreCassetted As Boolean = False, _
                             Optional ByRef dtPreBooked As DataTable = Nothing, _
                             Optional ByVal iBatchID As Integer = 0) As Boolean
        Try
            Dim drFoundRows As DataRow()

            'If pre cassetted need to use pre booked block
            If bIsPreCassetted And Not dtPreBooked Is Nothing Then
                GetPreBookedBlock(dtPreBooked, dtBlocks, dr("AnimalID"), iBlockID, sBlockRef)

                drFoundRows = dtBlocks.Select("ID=" & iBlockID)
                drFoundRows(0)("AnimalID") = dr("AnimalID")
                drFoundRows(0)("BatchID") = iBatchID
                drFoundRows(0)("CustomerRef") = dr("CustomerRef")
                drFoundRows(0)("Comment") = dr("Comment")
                drFoundRows(0)("RepeatBlock") = dr("RepeatBlock")
                drFoundRows(0)("BlockRef") = sBlockRef
                drFoundRows(0)("Status") = STATUS_PREBOOKED_USED
                drFoundRows(0)("Order") = GetOrder(dtBlocks)
                EditPreBookedBlockStatus(dtPreBooked, iBlockID, STATUS_PREBOOKED_USED)

                SetBatchID(dtPreBooked, iBlockID, iBatchID)
            Else
                Dim drNew As DataRow
                drNew = dtBlocks.NewRow()
                iBlockID = drNew("ID")
                drNew("AnimalID") = dr("AnimalID")
                drNew("BatchID") = dr("BatchID")
                drNew("CustomerRef") = dr("CustomerRef")
                drNew("Comment") = dr("Comment")
                drNew("RepeatBlock") = dr("RepeatBlock")
                drNew("BlockRef") = sBlockRef
                drNew("Status") = STATUS_USED
                drNew("Order") = GetOrder(dtBlocks)
                dtBlocks.Rows.Add(drNew)
            End If

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBlockObject)
            Return False
        End Try
    End Function

    Public Function NewBlock(ByRef dtBlocks As DataTable, _
                             ByRef iBlockID As Integer, _
                             ByVal iBatchID As Integer, _
                             ByVal iAnimalID As Integer, _
                             ByVal dr As DataRow, _
                             ByVal sBlockRef As String, _
                             Optional ByVal bIsPreCassetted As Boolean = False, _
                             Optional ByVal dtPreBooked As DataTable = Nothing) As Boolean
        Try
            Dim drFoundRows As DataRow()
            'If pre cassetted need to use pre booked block
            If bIsPreCassetted And Not dtPreBooked Is Nothing Then
                GetPreBookedBlock(dtPreBooked, dtBlocks, iAnimalID, iBlockID, sBlockRef)

                drFoundRows = dtBlocks.Select("ID=" & iBlockID)
                drFoundRows(0)("AnimalID") = iAnimalID
                drFoundRows(0)("BatchID") = iBatchID
                drFoundRows(0)("CustomerRef") = dr("CustomerRef")
                drFoundRows(0)("Comment") = dr("Comment")
                drFoundRows(0)("RepeatBlock") = dr("RepeatBlock")
                drFoundRows(0)("BlockRef") = sBlockRef
                drFoundRows(0)("Status") = STATUS_PREBOOKED_USED
                drFoundRows(0)("Order") = GetOrder(dtBlocks)
                EditPreBookedBlockStatus(dtPreBooked, iBlockID, STATUS_PREBOOKED_USED)
                SetBatchID(dtPreBooked, iBlockID, iBatchID)
            Else
                Dim drNew As DataRow
                drNew = dtBlocks.NewRow()
                iBlockID = drNew("ID")
                drNew("AnimalID") = iAnimalID
                drNew("BatchID") = iBatchID
                drNew("CustomerRef") = dr("CustomerRef")
                drNew("Comment") = dr("Comment")
                drNew("RepeatBlock") = dr("RepeatBlock")
                drNew("BlockRef") = sBlockRef
                drNew("Status") = STATUS_USED
                drNew("Order") = GetOrder(dtBlocks)
                dtBlocks.Rows.Add(drNew)
            End If

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBlockObject)
            Return False
        End Try
    End Function

    Public Function NewPreBookedBlock(ByRef dtBlocks As DataTable, _
                                      ByRef iBlockID As Integer, _
                                      ByVal iBatchID As Integer, _
                                      ByVal iAnimalID As Integer, _
                                      ByVal dr As DataRow, _
                                      ByVal sBlockRef As String, _
                                      ByRef dtPreBooked As DataTable) As Boolean
        Try
            Dim drFoundRows As DataRow()
            'If pre cassetted need to use pre booked block

            GetPreBookedBlockByBlockRef(dtPreBooked, dtBlocks, iAnimalID, iBlockID, sBlockRef)

            drFoundRows = dtBlocks.Select("ID=" & iBlockID)
            drFoundRows(0)("AnimalID") = iAnimalID
            drFoundRows(0)("BatchID") = iBatchID
            drFoundRows(0)("CustomerRef") = dr("CustomerRef")
            drFoundRows(0)("Comment") = dr("Comment")
            drFoundRows(0)("RepeatBlock") = dr("RepeatBlock")
            drFoundRows(0)("BlockRef") = sBlockRef
            drFoundRows(0)("Status") = STATUS_PREBOOKED_USED
            drFoundRows(0)("Order") = GetOrder(dtBlocks)
            EditPreBookedBlockStatus(dtPreBooked, iBlockID, STATUS_PREBOOKED_USED)
            SetBatchID(dtPreBooked, iBlockID, iBatchID)

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBlockObject)
            Return False
        End Try
    End Function


    Public Function DeleteBlockData(ByRef dsBatchData As DataSet, ByVal iBlockID As Integer, Optional ByVal bIsPreCassetted As Boolean = False) As Boolean
        Try
            Dim bSuccess As Boolean = False
            Dim dr As DataRow
            Dim sFilter As String
            Dim foundRows As DataRow()

            If Not dsBatchData Is Nothing Then
                dr = dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE).Rows.Find(iBlockID)
                If Not dr Is Nothing Then
                    If bIsPreCassetted Then
                        dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE).Rows.Remove(dr)
                    Else
                        dr.Delete()
                    End If

                    sFilter = "BlockID=" & Convert.ToString(iBlockID)
                    foundRows = dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TISSUES).Select(sFilter)
                    For Each dr In foundRows
                        dr.Delete()
                    Next

                    foundRows = dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_HISTOLOGY).Select(sFilter)
                    For Each dr In foundRows
                        dr.Delete()
                    Next

                    foundRows = dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_STAIN).Select(sFilter)
                    For Each dr In foundRows
                        dr.Delete()
                    Next

                    foundRows = dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANTIBODIES).Select(sFilter)
                    For Each dr In foundRows
                        dr.Delete()
                    Next

                    bSuccess = True
                End If

                ' If its a precasetted block, free the block so it can be assigned again.
                If bIsPreCassetted Then
                    EditPreBookedBlockStatus(dsBatchData.Tables(clsBatch.ANIMAL_PREBOOKED_BLOCKS), iBlockID, STATUS_PREBOOKED)
                    ClearBatchID(dsBatchData.Tables(clsBatch.ANIMAL_PREBOOKED_BLOCKS), iBlockID)
                End If
            End If

            Return bSuccess
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBlockObject)
            Return False
        End Try

    End Function

    Private Function IfBlockRefUsed(ByVal dtBlockTable As DataRow(), ByVal iBlockRef As Integer)
        Dim drRow As DataRow

        For Each drRow In dtBlockTable
            If Not IsDBNull(drRow("BlockRef")) Then
                If CInt(drRow("BlockRef")) = iBlockRef Then
                    Return True
                End If
            End If
        Next

        Return False
    End Function

    Public Function CreateMultiBlocks(ByRef dsBatchDetails As DataSet, _
                                      ByVal iNumberBlocks As Integer, _
                                      ByVal iCurrentBlockID As Integer, _
                                      ByRef sBlockRef As String, _
                                      ByVal iAnimalID As Integer, _
                                      ByVal iBatchID As Integer, _
                                      Optional ByVal bIsPreCassetted As Boolean = False) As Boolean
        Try
            Dim objHistology As New HistopathologyLib.clsHistology
            Dim objTissues As New HistopathologyLib.clsTissue
            Dim objChkbl As New HistopathologyLib.clsCheckBoxData
            Dim dtBlocks As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE)
            Dim iNewBlockID As Integer
            Dim iCount As Integer
            Dim foundRows As DataRow()
            Dim sFilter As String
            Dim dr As DataRow
            Dim iBlockRef As Int32 = Convert.ToInt32(sBlockRef)
            Dim bBlockRefUsed As Boolean = True
            Dim foundAnimalBlocks As DataRow()
            Dim objAnimal As New HistopathologyLib.clsAnimal

            If Not dsBatchDetails Is Nothing Then
                'By default have already created 1 of the blocks
                For iCount = 0 To iNumberBlocks - 2
                    sFilter = "ID=" & iCurrentBlockID
                    foundRows = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE).Select(sFilter)

                    sFilter = "AnimalID=" & iAnimalID
                    foundAnimalBlocks = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE).Select(sFilter)

                    iBlockRef += 1

                    'Need to check if the number has already been used.
                    While bBlockRefUsed
                        bBlockRefUsed = IfBlockRefUsed(foundAnimalBlocks, iBlockRef)

                        sBlockRef = ConvertBlockRefToString(iBlockRef)
                        If objAnimal.CheckBlockIsPreBooked(iAnimalID, sBlockRef, dsBatchDetails.Tables(HistopathologyLib.clsBatch.ANIMAL_PREBOOKED_BLOCKS)) Then
                            If Not bIsPreCassetted Then
                                bBlockRefUsed = True
                            End If
                        End If

                        If bBlockRefUsed Then
                            iBlockRef += 1
                        End If
                    End While
                    bBlockRefUsed = True

                    sBlockRef = ConvertBlockRefToString(iBlockRef)

                    If bIsPreCassetted Then
                        If Not NewBlock(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE), iNewBlockID, foundRows(0), sBlockRef, bIsPreCassetted, dsBatchDetails.Tables(HistopathologyLib.clsBatch.ANIMAL_PREBOOKED_BLOCKS), iBatchID) Then
                            Throw New Exception("Block.NewBlock returned false.")
                        End If
                    Else
                        If Not NewBlock(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE), iNewBlockID, foundRows(0), sBlockRef) Then
                            Throw New Exception("Block.NewBlock returned false.")
                        End If
                    End If

                    sFilter = "BlockID=" & iCurrentBlockID

                    foundRows = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TISSUES).Select(sFilter)
                    For Each dr In foundRows
                        If Not objTissues.NewBlockTissue(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TISSUES), iNewBlockID, dr) Then
                            Throw New Exception("Tissues.NewBlockTissue returned false.")
                        End If
                    Next

                    foundRows = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_HISTOLOGY).Select(sFilter)
                    For Each dr In foundRows
                        If Not objChkbl.NewItem(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_HISTOLOGY), dr("Code"), iNewBlockID, "BlockID") Then
                            Throw New Exception("Histology.NewBlockItem returned false.")
                        End If
                    Next

                    foundRows = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_STAIN).Select(sFilter)
                    For Each dr In foundRows
                        If Not objChkbl.NewItem(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_STAIN), dr("Code"), iNewBlockID, "BlockID") Then
                            Throw New Exception("Stain.NewBlockItem returned false.e")
                        End If
                    Next

                    foundRows = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANTIBODIES).Select(sFilter)
                    For Each dr In foundRows
                        If Not objChkbl.NewItem(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANTIBODIES), dr("Code"), iNewBlockID, "BlockID") Then
                            Throw New Exception("Antibodies.NewBlockItem returned false.")
                        End If
                    Next
                Next
            End If
            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBlockObject)
            Return False
        End Try
    End Function

    Public Function FixBlockRefs(ByRef dtBlockTable As DataTable, ByVal iErrorAnimalID As Integer, ByRef sLatestBlockRef As String, ByVal objNewAnimalIDs As ArrayList) As Boolean
        Try
            Dim sFilter As String
            Dim drFoundRows As DataRow()
            Dim dr As DataRow
            Dim iBlockRef As Integer

            'First fix the animal ids in the dataset
            UpdateAnimalID(dtBlockTable, objNewAnimalIDs)

            sFilter = "AnimalID=" & iErrorAnimalID
            drFoundRows = dtBlockTable.Select(sFilter, "BlockRef ASC")

            'Sorting will mean the lowest block ref is the first item in the list.
            'Fix the block refs using the latest version in the database.
            If Not drFoundRows Is Nothing Then
                iBlockRef = Convert.ToInt32(sLatestBlockRef)
                For Each dr In drFoundRows

                    If iBlockRef < 10 Then
                        sLatestBlockRef = "0" + CStr(iBlockRef)
                    Else
                        sLatestBlockRef = CStr(iBlockRef)
                    End If

                    dr("BlockRef") = sLatestBlockRef
                    iBlockRef += 1
                Next
            End If

            If iBlockRef < 10 Then
                sLatestBlockRef = "0" + CStr(iBlockRef)
            Else
                sLatestBlockRef = CStr(iBlockRef)
            End If

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function

#End Region

#Region "Update Block Data"

    Public Function CreatePreBookedBlock(ByVal iAnimalId As Integer, ByVal sBlockRef As String) As Boolean

        Dim objParamList As New ParameterList

        Try
            objParamList.AddParameter("ID", DbtType.dbtInteger, "@ID", "ID", , 0)

            objParamList.AddParameter("AnimalID", DbtType.dbtInteger, "@AnimalID", "ID", , iAnimalId)
            objParamList.AddParameter("BatchID", DbtType.dbtInteger, "@BatchID", "BatchID", , DBNull.Value)
            objParamList.AddParameter("BlockRef", DbtType.dbtString, "@BlockRef", "BlockRef", 4, sBlockRef)
            objParamList.AddParameter("CustomerRef", DbtType.dbtString, "@CustomerRef", "CustomerRef", 20, " ")
            objParamList.AddParameter("RepeatBlock", DbtType.dbtBoolean, "@RepeatBlock", "RepeatBlock", , False)
            objParamList.AddParameter("Comment", DbtType.dbtString, "@Comment", "Comment", 500, " ")
            objParamList.AddParameter("Status", DbtType.dbtInteger, "@Status", "Status", , STATUS_PREBOOKED)
            objParamList.AddParameter("Order", DbtType.dbtInteger, "@Order", "Order", , DBNull.Value)
            objParamList.AddParameter("ReturnValue", DbtType.dbtInteger, "@Error", , , -1, ParameterDirection.ReturnValue)
            objParamList.AddParameter("NewID", DbtType.dbtInteger, "@NewID", "ID", , , ParameterDirection.Output)
            objParamList.AddParameter("OldID", DbtType.dbtInteger, "@OldID", "ID", , , ParameterDirection.Output)

            ExecuteNonQuery("AddBlock", _
                            CommandType.StoredProcedure, _
                            objParamList)

            Return True
        Catch exSP As StoredProcException
            clsLog.LogException(exSP, clsLog.LogSource.lsStoredProcedure)
            Return False
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function

    Public Function UpdateBlocks(ByVal iBatchID As Integer, _
                                 ByRef dtBlockData As DataTable, _
                                 ByRef objDBConn As SqlConnection, _
                                 ByRef objDBTran As SqlTransaction, _
                                 ByRef objErrorList As ArrayList, _
                                 ByRef objNewAnimalIDs As ArrayList, _
                                 ByVal iUserID As Integer) As ArrayList
        UpdateBatchID(dtBlockData, iBatchID, iUserID)
        If Not objNewAnimalIDs Is Nothing Then
            UpdateAnimalID(dtBlockData, objNewAnimalIDs)
        End If
        UpdateBlockData(dtBlockData, objDBConn, objDBTran, objErrorList, iUserID)

        If objErrorList.Count > 0 Then
            Throw New Exception
        End If

        Return BlockIDs
    End Function

    Public Sub UpdatePreBookedBlocks(ByRef dtPreBooked As DataTable, _
                                          ByRef objDBConn As SqlConnection, _
                                          ByRef objDBTran As SqlTransaction, _
                                          ByRef objErrorList As ArrayList, _
                                          ByVal iBatchID As Integer)

        UpdateBatchID(dtPreBooked, iBatchID)
        UpdateBlockDataPreBookedBlock(dtPreBooked, objDBConn, objDBTran, objErrorList)

        If objErrorList.Count > 0 Then
            Throw New Exception
        End If
    End Sub

    Private Function UpdateBlockDataPreBookedBlock(ByRef dtBlockData As DataTable, _
                                                    ByRef objDBConn As SqlConnection, _
                                                    ByRef objDBTran As SqlTransaction, _
                                                    ByRef objErrorList As ArrayList)

        Dim drPreBookedRow As DataRow
        Dim objRelationParamList As libDataAccess.libDataAccess.ParameterList

        For Each drPreBookedRow In dtBlockData.Rows
            objRelationParamList = New libDataAccess.libDataAccess.ParameterList
            With objRelationParamList
                .QuickAddInputParam("ID", DbtType.dbtInteger, drPreBookedRow("ID"))
                .QuickAddInputParam("BlockRef", DbtType.dbtString, drPreBookedRow("BlockRef"))
                .QuickAddInputParam("SubmissionID", DbtType.dbtInteger, drPreBookedRow("BatchID"))
                .QuickAddInputParam("Status", DbtType.dbtInteger, drPreBookedRow("Status"))
            End With

            Try
                TBCultureDA.ExecuteNonQuery(objDBConn, objDBTran, "EditPreBookedBlock", CommandType.StoredProcedure, objRelationParamList)
            Catch ex As Exception
                objErrorList.Add(ex.Message)
            End Try
        Next

    End Function

    Private Function UpdateBlockData(ByRef dtBlockData As DataTable, _
                                    ByRef objDBConn As SqlConnection, _
                                    ByRef objDBTran As SqlTransaction, _
                                    ByRef objErrorList As ArrayList, _
                                    ByVal iUserID As Integer)
        Dim objRelationParamList As New libDataAccess.libDataAccess.UpdateParameterList

        With objRelationParamList
            .AddInsertParam("ID", DbtType.dbtInteger)
            .AddInsertParam("BatchID", DbtType.dbtInteger)
            .AddInsertParam("AnimalID", DbtType.dbtInteger)
            .AddInsertParam("BlockRef", DbtType.dbtString)
            .AddInsertParam("CustomerRef", DbtType.dbtString)
            .AddInsertParam("RepeatBlock", DbtType.dbtBoolean)
            .AddInsertParam("Comment", DbtType.dbtString)
            .AddInsertParam("Status", DbtType.dbtInteger)
            .AddInsertParam("Order", DbtType.dbtInteger)
            .AddInsertParam("OldID", DbtType.dbtInteger, , ParameterDirection.Output)
            .AddInsertParam("NewID", DbtType.dbtInteger, , ParameterDirection.Output)

            .AddUpdateParam("ID", DbtType.dbtInteger)
            .AddUpdateParam("BatchID", DbtType.dbtInteger)
            .AddUpdateParam("AnimalID", DbtType.dbtInteger)
            .AddUpdateParam("BlockRef", DbtType.dbtString)
            .AddUpdateParam("CustomerRef", DbtType.dbtString)
            .AddUpdateParam("RepeatBlock", DbtType.dbtBoolean)
            .AddUpdateParam("ArchiveLocation", DbtType.dbtString)
            .AddUpdateParam("ArchivedDate", DbtType.dbtDateTime)
            .AddUpdateParam("ArchiveComment", DbtType.dbtString)
            .AddUpdateParam("Comment", DbtType.dbtString)
            .AddUpdateParam("Status", DbtType.dbtInteger)
            .AddUpdateParam("UserID", DbtType.dbtInteger)
            .AddUpdateParam("Order", DbtType.dbtInteger)

            .AddDeleteParam("ID", DbtType.dbtInteger)

        End With
        OptimisticUpdateDataTable(objDBConn, _
                        objDBTran, _
                        AddressOf OnBlockRowUpdated, _
                        "", _
                        "AddBlock", _
                        "EditBlock", _
                        "DeleteBlock", _
                        CommandType.StoredProcedure, _
                        dtBlockData, _
                        objRelationParamList)

        AddRowErrorsToList("Block Data", "ID", dtBlockData, objErrorList)
    End Function

#End Region

#Region "Private Functions"

    Private Function ConvertBlockRefToString(ByVal iBlockRef As Integer) As String
        If iBlockRef < 10 Then
            Return "0" & Convert.ToString(iBlockRef)
        Else
            Return Convert.ToString(iBlockRef)
        End If
    End Function

    Private Sub OnBlockRowUpdated(ByVal sender As Object, ByVal args As SqlRowUpdatedEventArgs)

        If args.Status = UpdateStatus.ErrorsOccurred Then
            args.Row.RowError = args.Errors.Message
            args.Status = UpdateStatus.SkipCurrentRow
        Else
            If args.RecordsAffected = 0 Then
                args.Row.RowError = "Failed to update the block"
                args.Status = UpdateStatus.SkipCurrentRow
            Else
                If args.StatementType = System.Data.StatementType.Insert Then
                    Dim Ids As New HistopathologyLib.clsIDPairs
                    Ids.OldID = args.Command.Parameters("@OldID").Value
                    Ids.NewID = args.Command.Parameters("@NewID").Value
                    BlockIDs.Add(Ids)
                End If
            End If
        End If

    End Sub

    Private Sub AddRowErrorsToList(ByVal sTableName As String, ByVal sReportColumn As String, ByRef dtData As DataTable, ByRef objErrorList As ArrayList)

        Dim drData As DataRow
        For Each drData In dtData.Rows
            If drData.HasErrors Then
                Dim objMessage As New System.Text.StringBuilder
                objMessage.Append("Failed to ")
                Select Case drData.RowState
                    Case DataRowState.Added
                        objMessage.Append("add ")
                    Case DataRowState.Modified
                        objMessage.Append("update ")
                    Case DataRowState.Deleted
                        objMessage.Append("delete ")
                End Select
                objMessage.Append(sTableName)
                objMessage.Append(" with ")
                objMessage.Append(sReportColumn)
                objMessage.Append(" """)
                objMessage.Append(drData.Item(sReportColumn))
                objMessage.Append(""" :")
                objMessage.Append(drData.RowError)

                objErrorList.Add(objMessage.ToString())
            End If
        Next

    End Sub

    Private Sub UpdateBatchID(ByRef dtBlockData As DataTable, ByVal iBatchID As Integer, ByVal userID As Integer)
        Dim iRowsCount As Integer
        Dim dr As DataRow

        dtBlockData.Columns.Add("UserID")
        For Each dr In dtBlockData.Rows
            If Not dr.RowState = DataRowState.Deleted Then
                dr("BatchID") = iBatchID
            End If
            If dr.RowState = DataRowState.Modified Then
                dr("UserID") = userID
            End If
        Next
    End Sub

    Private Sub UpdateBatchID(ByRef dtBlockData As DataTable, ByVal iBatchID As Integer)
        Dim iRowsCount As Integer
        Dim dr As DataRow

        For Each dr In dtBlockData.Rows
            If Not dr.RowState = DataRowState.Deleted Then
                If Not IsDBNull(dr("BatchID")) Then
                    If dr.RowState = DataRowState.Modified Then
                        If dr("BatchID") Then
                            dr("BatchID") = iBatchID
                        End If
                    End If
                End If
            End If
        Next
    End Sub

    Private Sub UpdateAnimalID(ByRef dtBlocks As DataTable, ByVal objNewIDsList As ArrayList)
        Dim iListCount As Integer
        Dim objNewIDs As New HistopathologyLib.clsIDPairs
        Dim dr As DataRow

        For iListCount = 0 To objNewIDsList.Count - 1
            objNewIDs = objNewIDsList(iListCount)
            For Each dr In dtBlocks.Rows
                If Not dr.RowState = DataRowState.Deleted Then
                    If dr("AnimalID") = objNewIDs.OldID Then
                        dr("AnimalID") = objNewIDs.NewID
                    End If
                End If
            Next
        Next
    End Sub

#End Region

#Region "Copy Blocks"

    Public Function CopyBlocksFromPreviousSubmission(ByVal dsPreviousBatchDetails As DataSet, _
                                                     ByRef dsCurrentBatchDetails As DataSet, _
                                                     ByVal iPreviousAnimalID As Integer, _
                                                     ByVal iCurrentAnimalID As Integer, _
                                                     ByVal objBlocksList As ArrayList, _
                                                     ByVal iBatchID As Integer, _
                                                     ByVal iOldBlockID As Integer) As Boolean
        Try
            Dim dtPreviousAnimalTable As DataTable = dsPreviousBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL)
            Dim dtCurrentAnimalTable As DataTable = dsCurrentBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL)
            Dim dtPreviousBlockTable As DataTable = dsPreviousBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE)
            Dim dtCurrentBlockTable As DataTable = dsCurrentBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE)
            Dim dtPreviousBlockTissues As DataTable = dsPreviousBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TISSUES)
            Dim dtPreviousBlockHistology As DataTable = dsPreviousBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_HISTOLOGY)
            Dim dtPreviousBlockStains As DataTable = dsPreviousBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_STAIN)
            Dim dtPreviousBlockAntibodies As DataTable = dsPreviousBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANTIBODIES)
            Dim dtCurrentBlockTissues As DataTable = dsCurrentBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TISSUES)
            Dim dtCurrentBlockHistology As DataTable = dsCurrentBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_HISTOLOGY)
            Dim dtCurrentBlockStains As DataTable = dsCurrentBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_STAIN)
            Dim dtCurrentBlockAntibodies As DataTable = dsCurrentBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANTIBODIES)
            Dim objAnimal As New HistopathologyLib.clsAnimal
            Dim objTissues As New HistopathologyLib.clsTissue
            Dim objChkbl As New HistopathologyLib.clsCheckBoxData
            Dim iBlockCount As Integer
            Dim sNextBlockRef As String
            Dim iNextBlockRef As Integer
            Dim sNextAnimalBlockRef As String
            Dim drPreviousAnimalBlocks As DataRow()
            Dim drPreviousAnimal As DataRow
            Dim drPreviousBlock As DataRow
            Dim iBlockID As Integer
            Dim sFilter As String
            Dim drFoundRows As DataRow()
            Dim drRow As DataRow

            drPreviousAnimalBlocks = dtPreviousBlockTable.Select("AnimalID=" & iPreviousAnimalID)

            objAnimal.GetPreBookedBlocks(iCurrentAnimalID, dsCurrentBatchDetails)

            If Not drPreviousAnimalBlocks Is Nothing Then
                iNextBlockRef = 0
                For Each drPreviousBlock In drPreviousAnimalBlocks
                    For iBlockCount = 0 To objBlocksList.Count - 1
                        If drPreviousBlock("ID") = objBlocksList(iBlockCount) Then
                            If Not objAnimal.GetNextFreeBlockRef(dsCurrentBatchDetails, iCurrentAnimalID, sNextBlockRef) Then
                                Throw New Exception("Animal.GetNextFreeBlockRef returned false.")
                            End If

                            NewBlock(dtCurrentBlockTable, iBlockID, iBatchID, iCurrentAnimalID, drPreviousBlock, sNextBlockRef)

                            objAnimal.GetAnimalNextBlock(dtCurrentAnimalTable, iCurrentAnimalID, sNextAnimalBlockRef)

                            sFilter = "BlockID=" & drPreviousBlock("ID")

                            'Copy the tissues
                            drFoundRows = dtPreviousBlockTissues.Select(sFilter)
                            For Each drRow In drFoundRows
                                If Not objTissues.NewBlockTissue(dtCurrentBlockTissues, iBlockID, drRow) Then
                                    Throw New Exception("Tissues.NewBlockTissue returned false.")
                                End If
                            Next

                            'Copy the histology
                            drFoundRows = dtPreviousBlockHistology.Select(sFilter)
                            For Each drRow In drFoundRows
                                If Not objChkbl.NewItem(dtCurrentBlockHistology, drRow("Code"), iBlockID, "BlockID") Then
                                    Throw New Exception("Histology.NewBlockItem returned false.")
                                End If
                            Next

                            'Copy the stains
                            drFoundRows = dtPreviousBlockStains.Select(sFilter)
                            For Each drRow In drFoundRows
                                If Not objChkbl.NewItem(dtCurrentBlockStains, drRow("Code"), iBlockID, "BlockID") Then
                                    Throw New Exception("Stain.NewBlockItem returned false.e")
                                End If
                            Next

                            'Copy the antibodies
                            drFoundRows = dtPreviousBlockAntibodies.Select(sFilter)
                            For Each drRow In drFoundRows
                                If Not objChkbl.NewItem(dtCurrentBlockAntibodies, drRow("Code"), iBlockID, "BlockID") Then
                                    Throw New Exception("Antibodies.NewBlockItem returned false.")
                                End If
                            Next

                            iNextBlockRef = CInt(sNextBlockRef)
                            iNextBlockRef += 1

                            If iNextBlockRef > CInt(sNextAnimalBlockRef) Then
                                sNextAnimalBlockRef = ConvertBlockRefToString(iNextBlockRef)
                            End If

                            objAnimal.UpdateAnimalNextBlock(dtCurrentAnimalTable, iCurrentAnimalID, sNextAnimalBlockRef)
                        End If
                    Next
                Next
            End If

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function

    Public Function CopyDataToNewBatch(ByRef dtOriginal As DataTable, _
                                       ByRef dtNew As DataTable, _
                                       ByVal iBatchID As Integer, _
                                       ByVal objAnimalIDs As ArrayList, _
                                       ByRef objBlockIDs As ArrayList, _
                                       ByVal dtAnimal As DataTable) As Boolean
        Try
            Dim objAnimal As New HistopathologyLib.clsAnimal
            Dim iBlockID As Integer
            Dim objIDs As New HistopathologyLib.clsIDPairs
            Dim iCount As Integer = 0
            Dim sFilter As String
            Dim iHighestBlockRef As Integer
            Dim iNextBlockRef As Integer
            Dim drOldAnimalBlocks As DataRow()
            Dim drNewAnimalRows As DataRow()
            Dim drAnimalRow As DataRow
            Dim drOldBlock As DataRow
            Dim sNextAnimalBlockRef As String
            drNewAnimalRows = dtAnimal.Select()
            Dim sNextBlockRef As String

            If Not drNewAnimalRows Is Nothing Then
                For Each drAnimalRow In drNewAnimalRows
                    For iCount = 0 To objAnimalIDs.Count - 1
                        iHighestBlockRef = 0
                        iNextBlockRef = 0
                        objIDs = objAnimalIDs(iCount)
                        If objIDs.NewID = drAnimalRow("ID") Then
                            sFilter = "AnimalID=" & objIDs.OldID
                            drOldAnimalBlocks = dtOriginal.Select(sFilter)

                            If Not drOldAnimalBlocks Is Nothing Then
                                For Each drOldBlock In drOldAnimalBlocks
                                    NewBlock(dtNew, iBlockID, iBatchID, objIDs.NewID, drOldBlock, drOldBlock("BlockRef").ToString())

                                    iNextBlockRef = CInt(drOldBlock("BlockRef"))


                                    If iNextBlockRef > iHighestBlockRef Then
                                        iHighestBlockRef = iNextBlockRef
                                    End If

                                    Dim objBlockID As New HistopathologyLib.clsIDPairs
                                    objBlockID.OldID = drOldBlock("ID")
                                    objBlockID.NewID = iBlockID
                                    objBlockIDs.Add(objBlockID)

                                Next
                            End If
                            'sort out the next animal block ref
                            objAnimal.GetAnimalNextBlock(dtAnimal, objIDs.NewID, sNextAnimalBlockRef)

                            iHighestBlockRef += 1
                            If iHighestBlockRef > CInt(sNextAnimalBlockRef) Then
                                If iHighestBlockRef < 10 Then
                                    sNextAnimalBlockRef = "0" & Convert.ToString(iHighestBlockRef)
                                Else
                                    sNextAnimalBlockRef = Convert.ToString(iHighestBlockRef)
                                End If

                                objAnimal.UpdateAnimalNextBlock(dtAnimal, objIDs.NewID, sNextAnimalBlockRef)
                            End If
                        End If
                    Next
                Next
            End If

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function

    Public Function ValidatePreBookedBlocks(ByRef dtOriginal As DataTable, _
                                            ByRef dtNew As DataTable, _
                                            ByVal iBatchID As Integer, _
                                            ByVal objAnimalIDs As ArrayList, _
                                            ByRef objBlockIDs As ArrayList, _
                                            ByVal dtAnimal As DataTable, _
                                            ByVal bPreCassetted As Boolean, _
                                            ByRef dsOldBatch As DataSet, _
                                            ByRef sErrorMessage As String) As Boolean
        Try
            Dim objAnimal As New HistopathologyLib.clsAnimal
            Dim iBlockID As Integer
            Dim objIDs As New HistopathologyLib.clsIDPairs
            Dim iCount As Integer = 0
            Dim sFilter As String
            Dim iHighestBlockRef As Integer
            Dim iNextBlockRef As Integer
            Dim drOldAnimalBlocks As DataRow()
            Dim drNewAnimalRows As DataRow()
            Dim drAnimalRow As DataRow
            Dim drOldBlock As DataRow
            Dim sNextAnimalBlockRef As String
            Dim dtPreBooked As DataTable
            Dim iCopyToNumberOfPreBooked As Integer
            Dim iOriginalNumberOfSamples As Integer
            Dim bProcessSample As Boolean = True

            drNewAnimalRows = dtAnimal.Select()

            If Not drNewAnimalRows Is Nothing Then
                For Each drAnimalRow In drNewAnimalRows
                    bProcessSample = True
                    objAnimal.GetPreBookedBlocks(drAnimalRow("ID"), dsOldBatch)

                    If Not objAnimal.CheckPreBookedBlocksExist(drAnimalRow("ID"), dsOldBatch, iCopyToNumberOfPreBooked) Then
                        sErrorMessage = sErrorMessage & "<br><p><font color=""Red"">SSample " & drAnimalRow("SenderRef") & " does not have any pre-booked blocks. Blocks have not been copied.</font></p>"
                        bProcessSample = False
                    End If

                    If bProcessSample Then
                        For iCount = 0 To objAnimalIDs.Count - 1
                            objIDs = objAnimalIDs(iCount)
                            If objIDs.NewID = drAnimalRow("ID") Then
                                bProcessSample = True

                                objAnimal.GetNumberOfBlocks(dsOldBatch, objIDs.OldID, iOriginalNumberOfSamples)

                                If iCopyToNumberOfPreBooked < iOriginalNumberOfSamples Then
                                    sErrorMessage = sErrorMessage & "<br><p><font color=""Red"">Sample " & drAnimalRow("SenderRef") & " does not have enough pre-booked blocks to complete the copy.</font></p>"
                                    bProcessSample = False
                                End If

                                If Not bProcessSample = True Then
                                    ' Set it to an ID so this animal does not get copied in the CopyDataToNewBatchBookedBlocks function
                                    objIDs.NewID = 0
                                End If
                            End If
                        Next
                    End If
                Next
            End If

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function

    Public Function CopyDataToNewBatchBookedBlocks(ByRef dtOriginal As DataTable, _
                                                    ByRef dtNew As DataTable, _
                                                    ByVal iBatchID As Integer, _
                                                    ByVal objAnimalIDs As ArrayList, _
                                                    ByRef objBlockIDs As ArrayList, _
                                                    ByVal dtAnimal As DataTable, _
                                                    ByVal bPreCassetted As Boolean, _
                                                    ByRef dsOldBatch As DataSet) As Boolean
        Try
            Dim objAnimal As New HistopathologyLib.clsAnimal
            Dim iBlockID As Integer
            Dim objIDs As New HistopathologyLib.clsIDPairs
            Dim iCount As Integer = 0
            Dim sFilter As String
            Dim iHighestBlockRef As Integer
            Dim iNextBlockRef As Integer
            Dim drOldAnimalBlocks As DataRow()
            Dim drNewAnimalRows As DataRow()
            Dim drAnimalRow As DataRow
            Dim drOldBlock As DataRow
            Dim sNextAnimalBlockRef As String
            Dim dtPreBooked As DataTable
            Dim iCopyToNumberOfPreBooked As Integer
            Dim iOriginalNumberOfSamples As Integer
            Dim bProcessSample As Boolean = True

            drNewAnimalRows = dtAnimal.Select()

            If Not drNewAnimalRows Is Nothing Then
                For Each drAnimalRow In drNewAnimalRows
                    bProcessSample = True
                    objAnimal.GetPreBookedBlocks(drAnimalRow("ID"), dsOldBatch)

                    For iCount = 0 To objAnimalIDs.Count - 1
                        iHighestBlockRef = 0
                        iNextBlockRef = 0
                        objIDs = objAnimalIDs(iCount)
                        If objIDs.NewID = drAnimalRow("ID") Then
                            sFilter = "AnimalID=" & objIDs.OldID
                            drOldAnimalBlocks = dtOriginal.Select(sFilter)

                            If Not drOldAnimalBlocks Is Nothing Then

                                For Each drOldBlock In drOldAnimalBlocks
                                    NewPreBookedBlock(dtNew, iBlockID, iBatchID, objIDs.NewID, drOldBlock, drOldBlock("BlockRef").ToString(), dsOldBatch.Tables(HistopathologyLib.clsBatch.ANIMAL_PREBOOKED_BLOCKS))

                                    Dim objBlockID As New HistopathologyLib.clsIDPairs
                                    objBlockID.OldID = drOldBlock("ID")
                                    objBlockID.NewID = iBlockID
                                    objBlockIDs.Add(objBlockID)
                                Next
                            End If
                        End If
                    Next
                Next
            End If

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function

    Public Function CopySampleBlocks(ByRef dsBatchDetails As DataSet, _
                                     ByVal iIDToCopy As Integer, _
                                     ByVal iNewAnimalID As Integer) As Boolean
        Try
            Dim dtBlockTable As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE)
            Dim dtBatch As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)
            Dim dtAnimal As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL)
            Dim objChkbl As New HistopathologyLib.clsCheckBoxData
            Dim objTissues As New HistopathologyLib.clsTissue
            Dim objAnimal As New HistopathologyLib.clsAnimal
            Dim foundRows As DataRow()
            Dim foundAnimal As DataRow
            Dim sFilter As String
            Dim iNewBlockID As Integer
            Dim drBlocks As DataRow
            Dim dr As DataRow
            Dim sNextBlockRef As String
            Dim iNextBlockRef As Integer
            Dim iBatchID As Integer = dtBatch.Rows(0)("ID")
            Dim iHighestBlockRef As Integer = 0
            Dim iBlockRef As Integer = 0

            foundAnimal = dtAnimal.Rows.Find(iNewAnimalID)
            If Not foundAnimal Is Nothing Then
                sNextBlockRef = foundAnimal("NextBlockRef")
            Else
                Throw New Exception("Block.CopySampleBlocks could not find specified animal")
            End If

            sFilter = "AnimalID=" & iIDToCopy
            foundRows = dtBlockTable.Select(sFilter)

            If Not foundRows Is Nothing AndAlso Not foundRows.Length = 0 Then
                For Each drBlocks In foundRows
                    If Not NewBlock(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE), iNewBlockID, drBlocks("BatchID"), iNewAnimalID, drBlocks, drBlocks("BlockRef").ToString()) Then
                        Throw New Exception("Block.NewBlock returned false.")
                    End If

                    iBlockRef = CInt(drBlocks("BlockRef"))

                    If iBlockRef > iHighestBlockRef Then
                        iHighestBlockRef = iBlockRef
                    End If

                    sFilter = "BlockID=" & drBlocks("ID")

                    'Copy the tissues
                    foundRows = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TISSUES).Select(sFilter)
                    For Each dr In foundRows
                        If Not objTissues.NewBlockTissue(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TISSUES), iNewBlockID, dr) Then
                            Throw New Exception("Tissues.NewBlockTissue returned false.")
                        End If
                    Next

                    'Copy the histology
                    foundRows = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_HISTOLOGY).Select(sFilter)
                    For Each dr In foundRows
                        If Not objChkbl.NewItem(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_HISTOLOGY), dr("Code"), iNewBlockID, "BlockID") Then
                            Throw New Exception("Histology.NewBlockItem returned false.")
                        End If
                    Next

                    'Copy the stains
                    foundRows = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_STAIN).Select(sFilter)
                    For Each dr In foundRows
                        If Not objChkbl.NewItem(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_STAIN), dr("Code"), iNewBlockID, "BlockID") Then
                            Throw New Exception("Stain.NewBlockItem returned false.e")
                        End If
                    Next

                    'Copy the antibodies
                    foundRows = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANTIBODIES).Select(sFilter)
                    For Each dr In foundRows
                        If Not objChkbl.NewItem(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANTIBODIES), dr("Code"), iNewBlockID, "BlockID") Then
                            Throw New Exception("Antibodies.NewBlockItem returned false.")
                        End If
                    Next
                Next

                'Update the next animal block ref if necessary
                iHighestBlockRef += 1
                If iHighestBlockRef > CInt(sNextBlockRef) Then
                    If iHighestBlockRef < 10 Then
                        sNextBlockRef = "0" & Convert.ToString(iHighestBlockRef)
                    Else
                        sNextBlockRef = Convert.ToString(iHighestBlockRef)
                    End If
                End If

                'Update the next block ref for the animal
                If Not objAnimal.UpdateAnimalNextBlock(dtAnimal, iNewAnimalID, sNextBlockRef) Then
                    Throw New Exception("Animal.UpdateAnimalNextBlock returned false.")
                End If

            End If

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBlockObject)
            Return False
        End Try
    End Function

    Public Function CopySamplePreBookedBlocks(ByRef dsBatchDetails As DataSet, _
                                              ByVal iIDToCopy As Integer, _
                                              ByVal iNewAnimalID As Integer) As Boolean
        Try
            Dim dtBlockTable As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE)
            Dim dtPreBookedBlocks As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.ANIMAL_PREBOOKED_BLOCKS)
            Dim dtBatch As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)
            Dim dtAnimal As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL)
            Dim objChkbl As New HistopathologyLib.clsCheckBoxData
            Dim objTissues As New HistopathologyLib.clsTissue
            Dim objAnimal As New HistopathologyLib.clsAnimal
            Dim foundRows As DataRow()
            Dim foundAnimal As DataRow
            Dim sFilter As String
            Dim iNewBlockID As Integer
            Dim drBlocks As DataRow
            Dim dr As DataRow
            Dim sNextBlockRef As String
            Dim iNextBlockRef As Integer
            Dim iBatchID As Integer = dtBatch.Rows(0)("ID")
            Dim iHighestBlockRef As Integer = 0
            Dim iBlockRef As Integer = 0
            Dim iOriginalNumberBlocks As Integer = 0
            Dim iCopyToNumberOfPreBooked As Integer = 0
            Dim bProcessSample As Boolean
            Dim sErrorMessage As String
            Dim sSenderRef As String

            foundAnimal = dtAnimal.Rows.Find(iNewAnimalID)
            If Not foundAnimal Is Nothing Then
                sSenderRef = foundAnimal("SenderRef")
            Else
                Throw New Exception("Block.CopySampleBlocks could not find specified animal")
            End If

            sFilter = "AnimalID=" & iIDToCopy
            foundRows = dtBlockTable.Select(sFilter)

            If Not foundRows Is Nothing AndAlso Not foundRows.Length = 0 Then
                iOriginalNumberBlocks = foundRows.Length

                bProcessSample = True
                If Not objAnimal.CheckPreBookedBlocksExist(iNewAnimalID, dsBatchDetails, iCopyToNumberOfPreBooked) Then
                    sErrorMessage = sErrorMessage & "<br>Sample " & sSenderRef & " does not have any pre-booked blocks. Blocks have not been copied."
                    bProcessSample = False
                Else
                    If iOriginalNumberBlocks > iCopyToNumberOfPreBooked Then
                        sErrorMessage = sErrorMessage & "<br>Sample " & sSenderRef & " does not have enough pre-booked blocks to complete the copy."
                        bProcessSample = False
                    End If
                End If

                If bProcessSample Then
                    For Each drBlocks In foundRows

                        If Not NewPreBookedBlock(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE), iNewBlockID, iBatchID, iNewAnimalID, drBlocks, drBlocks("BlockRef").ToString(), dtPreBookedBlocks) Then
                            Throw New Exception("Block.NewBlock returned false.")
                        End If

                        sFilter = "BlockID=" & drBlocks("ID")
                        'Copy the tissues
                        foundRows = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TISSUES).Select(sFilter)
                        For Each dr In foundRows
                            If Not objTissues.NewBlockTissue(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TISSUES), iNewBlockID, dr) Then
                                Throw New Exception("Tissues.NewBlockTissue returned false.")
                            End If
                        Next

                        'Copy the histology
                        foundRows = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_HISTOLOGY).Select(sFilter)
                        For Each dr In foundRows
                            If Not objChkbl.NewItem(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_HISTOLOGY), dr("Code"), iNewBlockID, "BlockID") Then
                                Throw New Exception("Histology.NewBlockItem returned false.")
                            End If
                        Next

                        'Copy the stains
                        foundRows = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_STAIN).Select(sFilter)
                        For Each dr In foundRows
                            If Not objChkbl.NewItem(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_STAIN), dr("Code"), iNewBlockID, "BlockID") Then
                                Throw New Exception("Stain.NewBlockItem returned false.e")
                            End If
                        Next

                        'Copy the antibodies
                        foundRows = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANTIBODIES).Select(sFilter)
                        For Each dr In foundRows
                            If Not objChkbl.NewItem(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANTIBODIES), dr("Code"), iNewBlockID, "BlockID") Then
                                Throw New Exception("Antibodies.NewBlockItem returned false.")
                            End If
                        Next
                    Next
                End If
            End If

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBlockObject)
            Return False
        End Try
    End Function

    Public Function CopyBlock(ByRef dsBatchDetails As DataSet, _
                              ByVal iBlockIDToCopy As Integer, _
                              ByVal iNewAnimalID As Integer, _
                              ByVal iBatchID As Integer) As Boolean
        Try
            Dim dtBlockTable As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE)
            Dim dtBatch As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)
            Dim dtAnimal As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL)
            Dim dtPreBooked As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.ANIMAL_PREBOOKED_BLOCKS)
            Dim objChkbl As New HistopathologyLib.clsCheckBoxData
            Dim objTissues As New HistopathologyLib.clsTissue
            Dim objAnimal As New HistopathologyLib.clsAnimal
            Dim iNewBlockID As Integer = 0
            Dim sFilter As String = ""
            Dim foundAnimal As DataRow = Nothing
            Dim foundBlock As DataRow = Nothing
            Dim foundRows As DataRow()
            Dim sNextBlockRef As String = ""
            Dim dr As DataRow
            Dim iBlockRef As Integer = 0

            'foundAnimal = dtAnimal.Rows.Find(iNewAnimalID)
            'If Not foundAnimal Is Nothing Then
            '    sNextBlockRef = foundAnimal("NextBlockRef")
            'Else
            '    Throw New Exception("Block.CopyBlock could not find specified animal")
            'End If

            ' If the block is not a pre booked block use it.
            If Not objAnimal.GetNextFreeBlockRef(dsBatchDetails, iNewAnimalID, sNextBlockRef) Then
                Throw New Exception("Animal.GetNextFreeBlockRef returned false.")
            End If

            foundBlock = dtBlockTable.Rows.Find(iBlockIDToCopy)

            If Not foundBlock Is Nothing Then
                If Not NewBlock(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE), iNewBlockID, iBatchID, iNewAnimalID, foundBlock, sNextBlockRef) Then
                    Throw New Exception("Block.NewBlock returned false.")
                End If

                iBlockRef = CInt(sNextBlockRef)

                sFilter = "BlockID=" & iBlockIDToCopy

                'Copy the tissues
                foundRows = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TISSUES).Select(sFilter)
                For Each dr In foundRows
                    If Not objTissues.NewBlockTissue(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TISSUES), iNewBlockID, dr) Then
                        Throw New Exception("Tissues.NewBlockTissue returned false.")
                    End If
                Next

                'Copy the histology
                foundRows = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_HISTOLOGY).Select(sFilter)
                For Each dr In foundRows
                    If Not objChkbl.NewItem(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_HISTOLOGY), dr("Code"), iNewBlockID, "BlockID") Then
                        Throw New Exception("Histology.NewBlockItem returned false.")
                    End If
                Next

                'Copy the stains
                foundRows = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_STAIN).Select(sFilter)
                For Each dr In foundRows
                    If Not objChkbl.NewItem(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_STAIN), dr("Code"), iNewBlockID, "BlockID") Then
                        Throw New Exception("Stain.NewBlockItem returned false.e")
                    End If
                Next

                'Copy the antibodies
                foundRows = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANTIBODIES).Select(sFilter)
                For Each dr In foundRows
                    If Not objChkbl.NewItem(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANTIBODIES), dr("Code"), iNewBlockID, "BlockID") Then
                        Throw New Exception("Antibodies.NewBlockItem returned false.")
                    End If
                Next

                iBlockRef += 1
                If iBlockRef > CInt(sNextBlockRef) Then
                    If iBlockRef < 10 Then
                        sNextBlockRef = "0" & Convert.ToString(iBlockRef)
                    Else
                        sNextBlockRef = Convert.ToString(iBlockRef)
                    End If
                End If

                'Update the next block ref for the animal
                If Not objAnimal.UpdateAnimalNextBlock(dtAnimal, iNewAnimalID, sNextBlockRef) Then
                    Throw New Exception("Animal.UpdateAnimalNextBlock returned false.")
                End If
            End If

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBlockObject)
            Return False
        End Try
    End Function


#End Region

#Region "Get Functions"

    Public Function GetNextBlockRefDatabase(ByVal sSenderRef As String, _
                                            ByRef iBlockRef As Integer) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            objInParamList.QuickAddInputParam("SenderRef", DbtType.dbtString, sSenderRef)
            objInParamList.AddParameter("@LastBlockRef", DbtType.dbtSmallInt, "@LastBlockRef", daDirection:=ParameterDirection.Output)

            ExecuteNonQuery("GetBlockRefForSender", _
                            CommandType.StoredProcedure, _
                            objInParamList)

            iBlockRef = CInt(objInParamList("@LastBlockRef").Value) + 1
            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBlockObject)
            Return False
        End Try

    End Function

#End Region

    Public Function UsePreBookedBlockID(ByRef dsBatchDetails As DataSet, ByVal iBlockID As Integer, ByVal iNewBlockID As Integer) As Integer
        Try
            Dim foundRows As DataRow()
            Dim dtBlockTable As DataTable

            dtBlockTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE)

            foundRows = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE).Select("ID=" & iBlockID)
            foundRows(0)("ID") = iNewBlockID
            foundRows(0)("Status") = STATUS_PREBOOKED

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBlockObject)
            Return False
        End Try
    End Function

    Public Function CheckPreBlockedFree(ByVal dtPreBooked As DataTable, _
                                        ByVal iAnimalID As Integer, _
                                        ByVal sBlockref As String)

        Try
            Dim drFoundRows As DataRow()

            drFoundRows = dtPreBooked.Select("AnimalID=" & iAnimalID & " AND BlockRef='" & sBlockref & "'")

            If Not drFoundRows Is Nothing Then
                If drFoundRows.Length > 0 Then
                    If drFoundRows(0)("Status") = STATUS_USED Or _
                       drFoundRows(0)("Status") = STATUS_PREBOOKED_USED Then
                        Return False
                    Else
                        Return True
                    End If
                Else
                    Return False
                End If
            Else
                Return False
            End If

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function

    Public Function CheckPreBlockedUsed(ByVal dtPreBooked As DataTable, _
                                        ByVal iAnimalID As Integer, _
                                        ByVal sBlockref As String)

        Try
            Dim drFoundRows As DataRow()

            drFoundRows = dtPreBooked.Select("AnimalID=" & iAnimalID & " AND BlockRef='" & sBlockref & "'")

            If Not drFoundRows Is Nothing Then
                If drFoundRows.Length > 0 Then
                    If drFoundRows(0)("Status") = STATUS_PREBOOKED_USED Then
                        Return True
                    Else
                        Return False
                    End If
                Else
                    Return False
                End If
            Else
                Return False
            End If

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function

    Public Function GetPreBookedBlock(ByRef dtPreBooked As DataTable, _
                                      ByRef dtBlocks As DataTable, _
                                      ByVal iAnimalID As Integer, _
                                      ByRef iBlockID As Integer, _
                                      ByRef sBlockref As String) As Boolean
        Dim drFoundAnimalBlocks As DataRow()

        drFoundAnimalBlocks = dtPreBooked.Select("AnimalID=" & iAnimalID & " AND Status=" & HistopathologyLib.clsBlock.STATUS_PREBOOKED)
        Array.Sort(drFoundAnimalBlocks, New CustomBlockRefAscSort)
        If Not drFoundAnimalBlocks Is Nothing Then
            If drFoundAnimalBlocks.Length > 0 Then
                sBlockref = drFoundAnimalBlocks(0)("BlockRef").ToString
                iBlockID = drFoundAnimalBlocks(0)("ID")
                dtBlocks.ImportRow(drFoundAnimalBlocks(0))
                Return True
            Else
                Return False
            End If
        Else
            Return False
        End If

    End Function

    Public Function GetPreBookedBlockByBlockRef(ByRef dtPreBooked As DataTable, _
                                                ByRef dtBlocks As DataTable, _
                                                ByVal iAnimalID As Integer, _
                                                ByRef iBlockID As Integer, _
                                                ByRef sBlockref As String) As Boolean
        Dim drFoundAnimalBlocks As DataRow()

        drFoundAnimalBlocks = dtPreBooked.Select("AnimalID=" & iAnimalID & " AND Status=" & HistopathologyLib.clsBlock.STATUS_PREBOOKED & " AND BlockRef = '" & sBlockref & "'")

        Array.Sort(drFoundAnimalBlocks, New CustomBlockRefAscSort)

        If Not drFoundAnimalBlocks Is Nothing Then
            If drFoundAnimalBlocks.Length > 0 Then
                'sBlockref = drFoundAnimalBlocks(0)("BlockRef").ToString
                iBlockID = drFoundAnimalBlocks(0)("ID")
                dtBlocks.ImportRow(drFoundAnimalBlocks(0))
                Return True
            Else
                Return False
            End If
        Else
            Return False
        End If

    End Function

    Public Function EditPreBookedBlockStatus(ByRef dtPreBooked As DataTable, ByVal iBlockID As Integer, ByVal iStatus As Integer) As Boolean

        Try
            Dim drFoundRows As DataRow()

            drFoundRows = dtPreBooked.Select("ID=" & iBlockID)

            If Not drFoundRows Is Nothing Then
                If drFoundRows.Length > 0 Then
                    drFoundRows(0)("Status") = iStatus
                End If
            End If

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function

    Public Function ClearBatchID(ByRef dtPreBooked As DataTable, ByVal iBlockID As Integer) As Boolean

        Try
            Dim drFoundRows As DataRow()

            drFoundRows = dtPreBooked.Select("ID=" & iBlockID)

            If Not drFoundRows Is Nothing Then
                If drFoundRows.Length > 0 Then
                    drFoundRows(0)("BatchID") = DBNull.Value
                End If
            End If

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function

    Public Function SetBatchID(ByRef dtPreBooked As DataTable, ByVal iBlockID As Integer, ByVal iBatchID As Integer) As Boolean

        Try
            Dim drFoundRows As DataRow()

            drFoundRows = dtPreBooked.Select("ID=" & iBlockID)

            If Not drFoundRows Is Nothing Then
                If drFoundRows.Length > 0 Then
                    drFoundRows(0)("BatchID") = iBatchID
                End If
            End If

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function
End Class
