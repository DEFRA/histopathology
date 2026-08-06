Imports libDataAccess.libDataAccess
Imports libDataAccess.libDataAccess.TBCultureDA
Imports System.Data.SqlClient

Public Class clsBatchSubmission

    Dim BatchSubmissionIDs As New ArrayList()

#Region "DataTable Handling"

    Public Function NewRecord(ByRef dtBatchSubmission As DataTable, _
                              ByRef iBatchSubmissionID As Integer, _
                              ByVal iBatchID As Integer, _
                              ByVal iAnimalID As Integer) As Boolean
        Try
            Dim dr As DataRow
            dr = dtBatchSubmission.NewRow()
            iBatchSubmissionID = dr("ID")
            dr("BatchID") = iBatchID
            dr("AnimalID") = iAnimalID
            dr("Order") = GetOrder(dtBatchSubmission)
            dtBatchSubmission.Rows.InsertAt(dr, 0)

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBatchSubmissionObject)
            Return False
        End Try
    End Function

    Public Function NewRecord(ByRef dtBatchSubmission As DataTable, _
                             ByRef iBatchSubmissionID As Integer, _
                             ByVal iBatchID As Integer) As Boolean
        Try
            Dim dr As DataRow
            dr = dtBatchSubmission.NewRow()
            iBatchSubmissionID = dr("ID")
            dr("BatchID") = iBatchID
            dr("Order") = GetOrder(dtBatchSubmission)
            dtBatchSubmission.Rows.InsertAt(dr, 0)

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBatchSubmissionObject)
            Return False
        End Try
    End Function

    Private Function GetOrder(ByVal dtBatchSubmission As DataTable) As Integer
        Dim max As Object
        max = dtBatchSubmission.Compute("Max(Order)", "")
        If Not IsDBNull(max) Then
            Return CInt(max) + 1
        Else
            Return 0
        End If
    End Function

    Public Function DeleteRecord(ByRef dtTissues As DataTable, _
                                ByRef dtBatchSub As DataTable, _
                                ByVal iBatchSubmissionID As Integer)
        Try
            Dim drFoundRow As DataRow
            Dim drFoundRows As DataRow()
            Dim sFilter As String

            'Delete the related tissues
            sFilter = "BatchSubmissionID=" & Convert.ToString(iBatchSubmissionID)
            drFoundRows = dtTissues.Select(sFilter)
            For Each drFoundRow In drFoundRows
                drFoundRow.Delete()
            Next

            'Delete the record from the BatchSubmission table
            sFilter = "ID=" & Convert.ToString(iBatchSubmissionID)
            drFoundRows = dtBatchSub.Select(sFilter)
            If Not drFoundRows Is Nothing And drFoundRows.Length > 0 Then
                drFoundRows(0).Delete()
            End If

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBatchSubmissionObject)
            Return False
        End Try
    End Function

#End Region

#Region "Update Block Data"

    Public Function UpdateBatchSubmission(ByVal iBatchID As Integer, _
                                        ByRef dtBatchSubmissionData As DataTable, _
                                        ByRef objDBConn As SqlConnection, _
                                        ByRef objDBTran As SqlTransaction, _
                                        ByRef objErrorList As ArrayList, _
                                        ByRef objAnimalID As ArrayList) As ArrayList

        UpdateBatchID(dtBatchSubmissionData, iBatchID)
        UpdateAnimalID(dtBatchSubmissionData, objAnimalID)
        UpdateBatchSubmissionData(dtBatchSubmissionData, objDBConn, objDBTran, objErrorList)
        Return BatchSubmissionIDs
    End Function

    Public Function UpdateBatchSubmissionData(ByRef dtBatchSubmissionData As DataTable, _
                                            ByRef objDBConn As SqlConnection, _
                                            ByRef objDBTran As SqlTransaction, _
                                            ByRef objErrorList As ArrayList)
        Dim objRelationParamList As New libDataAccess.libDataAccess.UpdateParameterList()

        With objRelationParamList
            .AddInsertParam("ID", DbtType.dbtInteger)
            .AddInsertParam("BatchID", DbtType.dbtInteger)
            .AddInsertParam("AnimalID", DbtType.dbtString)
            .AddInsertParam("Order", DbtType.dbtInteger)
            .AddInsertParam("OldID", DbtType.dbtInteger, , ParameterDirection.Output)
            .AddInsertParam("NewID", DbtType.dbtInteger, , ParameterDirection.Output)

            .AddUpdateParam("ID", DbtType.dbtInteger)
            .AddUpdateParam("BatchID", DbtType.dbtInteger)
            .AddUpdateParam("AnimalID", DbtType.dbtString)
            .AddUpdateParam("Order", DbtType.dbtInteger)

            .AddDeleteParam("ID", DbtType.dbtInteger)

        End With
        OptimisticUpdateDataTable(objDBConn, _
                        objDBTran, _
                        AddressOf OnBatchSubmissionRowUpdated, _
                        "", _
                        "AddBatchSubmission", _
                        "EditBatchSubmission", _
                        "DeleteBatchSubmission", _
                        CommandType.StoredProcedure, _
                        dtBatchSubmissionData, _
                        objRelationParamList)

        AddRowErrorsToList("Batch Data", "ID", dtBatchSubmissionData, objErrorList)
    End Function

#End Region

#Region "Private Functions"

    Private Sub UpdateBatchID(ByRef dtBatchSubmissionData As DataTable, ByVal iBatchID As Integer)
        Dim iRowsCount As Integer
        Dim dr As DataRow
        For Each dr In dtBatchSubmissionData.Rows
            If Not dr.RowState = DataRowState.Deleted Then
                dr("BatchID") = iBatchID
            End If
        Next
    End Sub

    Private Sub AddRowErrorsToList(ByVal sTableName As String, ByVal sReportColumn As String, ByRef dtData As DataTable, ByRef objErrorList As ArrayList)

        Dim drData As DataRow
        For Each drData In dtData.Rows
            If drData.HasErrors Then
                Dim objMessage As New System.Text.StringBuilder()
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

    Private Sub OnBatchSubmissionRowUpdated(ByVal sender As Object, ByVal args As SqlRowUpdatedEventArgs)

        If args.Status = UpdateStatus.ErrorsOccurred Then
            args.Row.RowError = args.Errors.Message
            args.Status = UpdateStatus.SkipCurrentRow
        Else
            If args.RecordsAffected = 0 Then
                args.Row.RowError = "Failed to update the block"
                args.Status = UpdateStatus.SkipCurrentRow
            Else
                If args.StatementType = System.Data.StatementType.Insert Then
                    Dim Ids As New HistopathologyLib.clsIDPairs()
                    Ids.OldID = args.Command.Parameters("@OldID").Value
                    Ids.NewID = args.Command.Parameters("@NewID").Value
                    BatchSubmissionIDs.Add(Ids)
                End If
            End If
        End If
    End Sub

    Private Sub UpdateAnimalID(ByRef dtBlocks As DataTable, ByVal objNewIDsList As ArrayList)
        Dim iListCount As Integer
        Dim objNewIDs As New HistopathologyLib.clsIDPairs()
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

#Region "Copy Data"

    Public Function CopyBatchSubmissionWithAnimalID(ByRef dsBatchDetails As DataSet, ByVal iCopyAnimalID As Integer, ByVal iNewAnimalID As Integer) As Boolean
        Try
            Dim objTissue As New HistopathologyLib.clsTissue()
            Dim dtBatchSubmission As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_SUBMISSION_TABLE)
            Dim dtBatch As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)
            Dim dtTissues As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TISSUES_TABLE)
            Dim iBatchID As Integer = dtBatch.Rows(0)("ID")
            Dim iNewBatchSubmissionID As Integer
            Dim sFilter As String
            Dim foundRows As DataRow()
            Dim dr As DataRow

            sFilter = "AnimalID=" & iCopyAnimalID
            foundRows = dtBatchSubmission.Select(sFilter)

            If Not NewRecord(dtBatchSubmission, iNewBatchSubmissionID, iBatchID, iNewAnimalID) Then
                Throw New Exception("BatchSubmission.NewRecord returned false.")
            End If

            If Not foundRows Is Nothing Then
                For Each dr In foundRows(0).GetChildRows("BATCHSUBMISSION_BATCHTISSUES")
                    If Not objTissue.NewBatchSubmissionTissue(dtTissues, iNewBatchSubmissionID, dr) Then
                        Throw New Exception("Tissue.NewBatchSubmissionTissue returned false.")
                    End If
                Next
            End If

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBatchSubmissionObject)
            Return False
        End Try
    End Function

    Public Function CopyDataToNewBatch(ByRef dtOriginal As DataTable, _
                                       ByRef dtNew As DataTable, _
                                       ByVal iBatchID As Integer, _
                                       ByVal objAnimalIDs As ArrayList, _
                                       ByRef objBatchSubmissionIDs As ArrayList) As Boolean
        Try
            Dim dr As DataRow
            Dim drNewRow As DataRow
            Dim iBatchSubmissionID As Integer
            Dim objIDs As New HistopathologyLib.clsIDPairs()
            Dim iCount As Integer = 0

            'Copy the data 
            For Each dr In dtOriginal.Rows
                For iCount = 0 To objAnimalIDs.Count - 1
                    objIDs = objAnimalIDs(iCount)
                    If objIDs.OldID = dr("AnimalID") Then
                        NewRecord(dtNew, iBatchSubmissionID, iBatchID, objIDs.NewID)

                        'Need to keep a match of the old BatchSubmissionIDs and the new one
                        'created so that its possible to match the tissues correctly
                        Dim objBatchSubmissionID As New HistopathologyLib.clsIDPairs()
                        objBatchSubmissionID.OldID = dr("ID")
                        objBatchSubmissionID.NewID = iBatchSubmissionID
                        objBatchSubmissionIDs.Add(objBatchSubmissionID)
                    End If
                Next
            Next

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsCheckBoxObject)
            Return False
        End Try
    End Function

#End Region
End Class
