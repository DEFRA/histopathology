Imports libDataAccess.libDataAccess
Imports libDataAccess.libDataAccess.TBCultureDA
Imports System.Data.SqlClient


Public Class clsCheckBoxData

#Region "Private Functions"

    Private Sub UpdateBlockIDs(ByRef dtHistology As DataTable, ByVal objNewIDsList As ArrayList, ByVal userID As Integer, ByVal iBatchID As Integer)
        Dim iListCount As Integer
        Dim objNewIDs As New HistopathologyLib.clsIDPairs()
        Dim dr As DataRow
        Dim iRowsCount As Integer
        Dim iBlockID As Integer

        dtHistology.Columns.Add("UserID")
        dtHistology.Columns.Add("BatchID")

        For Each dr In dtHistology.Rows
            If Not dr.RowState = DataRowState.Deleted Then
                For iListCount = 0 To objNewIDsList.Count - 1
                    objNewIDs = objNewIDsList(iListCount)
                    If dr("BlockID") = objNewIDs.OldID Then
                        dr("BlockID") = objNewIDs.NewID
                    End If
                Next
                If dr.RowState = DataRowState.Modified Or dr.RowState = DataRowState.Added Then
                    dr("UserID") = userID
                    dr("BatchID") = iBatchID
                End If
            End If
        Next
    End Sub

    Private Sub UpdateBatchID(ByRef dtHistology As DataTable, ByVal iBatchID As Integer, ByVal userID As Integer)
        Dim iRowsCount As Integer
        Dim dr As DataRow

        dtHistology.Columns.Add("UserID")

        For Each dr In dtHistology.Rows
            If Not dr.RowState = DataRowState.Deleted Then
                dr("BatchID") = iBatchID
                dr("UserID") = userID
            End If
            'If dr.RowState = DataRowState.Modified Then
            '    dr("UserID") = userID
            'End If
        Next
    End Sub

    Private Sub AddUserBatch(ByRef dtData As DataTable, ByVal iBatchID As Integer, ByVal iUserID As Integer)
        Dim iRowsCount As Integer
        Dim dr As DataRow

        dtData.Columns.Add("UserID")
        dtData.Columns.Add("BatchID")

        For Each dr In dtData.Rows
            If Not dr.RowState = DataRowState.Deleted Then
                dr("BatchID") = iBatchID
                dr("UserID") = iUserID
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

#End Region

#Region "Update Data"

    Public Function UpdateTable(ByRef dtData As DataTable, _
                                ByRef dbConn As Object, _
                                ByRef dbTran As Object, _
                                ByRef objErrorList As ArrayList, _
                                ByVal iBatchID As Integer, _
                                ByVal iTableID As Integer, _
                                ByVal iUserID As Integer)

        UpdateBatchID(dtData, iBatchID, iUserID)
        UpdateTableDetails(dtData, dbConn, dbTran, objErrorList, iTableID, iUserID)

        If objErrorList.Count > 0 Then
            Throw New Exception()
        End If

    End Function

    Private Sub UpdateTableDetails(ByRef dtData As DataTable, _
                                    ByRef dbConn As Object, _
                                    ByRef dbTran As Object, _
                                    ByRef objErrorList As ArrayList, _
                                    ByVal iTableID As Integer, _
                                    ByVal iUserID As Integer)

        Dim objHistologyParamList As New libDataAccess.libDataAccess.UpdateParameterList()

        Dim sKeyField As String
        Dim sInsertSP As String
        Dim sUpdateSP As String
        Dim sDeleteSP As String

        Select Case iTableID
            Case 1 'BATCH_HISTOLOGY_TABLE
                sInsertSP = "AddHistology"
                sUpdateSP = "EditHistology"
                sDeleteSP = "DeleteHistology"

            Case 2 'BATCH_ANTIBODIES_TABLE
                sInsertSP = "AddAntibodies"
                sUpdateSP = "EditAntibodies"
                sDeleteSP = "DeleteAntibodies"

            Case 3 'BATCH_STAIN_TABLE
                sInsertSP = "AddSpecialStain"
                sUpdateSP = "EditSpecialStain"
                sDeleteSP = "DeleteSpecialStain"

            Case 4 'BATCH_POSTFIXATION_TABLE
                sInsertSP = "AddPostFixation"
                sUpdateSP = "EditPostFixation"
                sDeleteSP = "DeletePostFixation"

            Case 5 'BATCH_SUBMITTEDAS_TABLE
                sInsertSP = "AddSubmittedAs"
                sUpdateSP = "EditSubmittedAs"
                sDeleteSP = "DeleteSubmittedAs"
        End Select

        ''Do this so we can pass the User ID to the stored procedure for auditing purposes
        'dtData.Columns.Add("UserID")

        With objHistologyParamList
            .AddInsertParam("BatchID", DbtType.dbtInteger)
            .AddInsertParam("Code", DbtType.dbtString)
            .AddInsertParam("UserID", DbtType.dbtInteger)

            .AddUpdateParam("ID", DbtType.dbtInteger)
            .AddUpdateParam("BatchID", DbtType.dbtInteger)
            .AddUpdateParam("Code", DbtType.dbtString)
            '  .AddUpdateParam("UserID", DbtType.dbtInteger, iUserID, ParameterDirection.Input, , DataRowVersion.Current)

            .AddDeleteParam("ID", DbtType.dbtInteger)
            ' .AddDeleteParam("UserID", DbtType.dbtInteger, iUserID, ParameterDirection.Input, DataRowVersion.Current)
        End With

        OptimisticUpdateDataTable(dbConn, _
                                  dbTran, _
                                  AddressOf OnTestsUpdated, _
                                  "", _
                                  sInsertSP, _
                                  sUpdateSP, _
                                  sDeleteSP, _
                                  CommandType.StoredProcedure, _
                                  dtData, _
                                  objHistologyParamList)

        Select Case iTableID
            Case 1 'BATCH_HISTOLOGY_TABLE
                AddRowErrorsToList("batch histology", "code", dtData, objErrorList)
            Case 2 'BATCH_ANTIBODIES_TABLE
                AddRowErrorsToList("batch antibodies", "code", dtData, objErrorList)
            Case 3 'BATCH_STAIN_TABLE
                AddRowErrorsToList("batch stain", "code", dtData, objErrorList)
            Case 4 'BATCH_POSTFIXATION_TABLE
                AddRowErrorsToList("batch  postfixation", "code", dtData, objErrorList)
        End Select

    End Sub

    Private Function UpdateQCNoteIDs(ByRef dtData As DataTable, ByVal dtQCNoteIDs As DataTable)
        Dim drQCNote As DataRow
        Dim dr As DataRow

        For Each drQCNote In dtQCNoteIDs.Rows
            For Each dr In dtData.Rows
                If Not dr.RowState = DataRowState.Deleted Then
                    If Not IsDBNull(dr("QCNoteRef")) Then
                        If drQCNote("ID") = dr("QCNoteRef") Then
                            dr("QCNoteRef") = drQCNote("NewID")
                        End If
                    End If
                End If
            Next
        Next
    End Function

    Public Function UpdateBlockTables(ByRef dtData As DataTable, _
                                ByRef dbConn As Object, _
                                ByRef dbTran As Object, _
                                ByRef objErrorList As ArrayList, _
                                ByVal objBlockIDs As ArrayList, _
                                ByVal iTableID As Integer, _
                                ByVal iUserID As Integer, _
                                ByVal iBatchID As Integer, _
                                Optional ByVal dtQCNoteIDs As DataTable = Nothing)
        If Not dtQCNoteIDs Is Nothing Then
            UpdateQCNoteIDs(dtData, dtQCNoteIDs)
        End If
        UpdateBlockIDs(dtData, objBlockIDs, iUserID, iBatchID)
        UpdateBlockTablesDetails(dtData, dbConn, dbTran, objErrorList, iTableID, iUserID, dtQCNoteIDs)

        If objErrorList.Count > 0 Then
            Throw New Exception()
        End If
    End Function

    Private Sub UpdateBlockTablesDetails(ByRef dtData As DataTable, _
                                         ByRef dbConn As Object, _
                                         ByRef dbTran As Object, _
                                         ByRef objErrorList As ArrayList, _
                                         ByVal iTableID As Integer, _
                                         ByVal iUserID As Integer, _
                                         Optional ByVal dtQCNotesIDs As DataTable = Nothing)

        Dim objParamList As New libDataAccess.libDataAccess.UpdateParameterList()

        Dim sKeyField As String
        Dim sInsertSP As String
        Dim sUpdateSP As String
        Dim sDeleteSP As String

        Select Case iTableID
            Case 8 'BATCH_BLOCK_HISTOLOGY
                sInsertSP = "AddBlockHistology"
                sUpdateSP = "EditBlockHistology"
                sDeleteSP = "DeleteBlockHistology"
            Case 9 'BATCH_BLOCK_ANTIBODIES
                sInsertSP = "AddBlockAntibodies"
                sUpdateSP = "EditBlockAntibodies"
                sDeleteSP = "DeleteBlockAntibodies"
            Case 10 'BATCH_BLOCK_STAIN
                sInsertSP = "AddBlockStain"
                sUpdateSP = "EditBlockStain"
                sDeleteSP = "DeleteBlockStain"
        End Select

        With objParamList
            .AddInsertParam("BlockID", DbtType.dbtInteger)
            .AddInsertParam("Code", DbtType.dbtString)
            .AddInsertParam("Comment", DbtType.dbtString)
            .AddInsertParam("UserID", DbtType.dbtInteger)
            .AddInsertParam("BatchID", DbtType.dbtInteger)

            .AddUpdateParam("ID", DbtType.dbtInteger)
            .AddUpdateParam("BlockID", DbtType.dbtInteger)
            .AddUpdateParam("Code", DbtType.dbtString)
            .AddUpdateParam("Result", DbtType.dbtString)
            .AddUpdateParam("QCCode", DbtType.dbtString)
            .AddUpdateParam("QCNoteRef", DbtType.dbtInteger)
            .AddUpdateParam("QCNote", DbtType.dbtBoolean)
            .AddUpdateParam("StainRef", DbtType.dbtString)
            .AddUpdateParam("DispatchedDate", DbtType.dbtDateTime)
            .AddUpdateParam("DispatchedBy", DbtType.dbtString)
            .AddUpdateParam("EnteredBy", DbtType.dbtString)
            .AddUpdateParam("PremiumCharge", DbtType.dbtString)
            .AddUpdateParam("Dispatched", DbtType.dbtBoolean)
            .AddUpdateParam("DispatchedTo", DbtType.dbtString)
            .AddUpdateParam("Comment", DbtType.dbtString)
            .AddUpdateParam("RemedialAction", DbtType.dbtString)
            .AddUpdateParam("ArchiveLocation", DbtType.dbtString)
            .AddUpdateParam("ArchivedDate", DbtType.dbtDateTime)
            .AddUpdateParam("ArchiveComment", DbtType.dbtString)
            .AddUpdateParam("NumberOfSlides", DbtType.dbtTinyInt)
            .AddUpdateParam("UserID", DbtType.dbtInteger)
            .AddUpdateParam("RowStamp", DbtType.dbtBinary)

            .AddDeleteParam("ID", DbtType.dbtInteger)
        End With

        OptimisticUpdateDataTable(dbConn, _
                                dbTran, _
                                AddressOf OnTestsUpdated, _
                                "", _
                                sInsertSP, _
                                sUpdateSP, _
                                sDeleteSP, _
                                CommandType.StoredProcedure, _
                                dtData, _
                                objParamList)

        Select Case iTableID
            Case 8 'BATCH_BLOCK_HISTOLOGY
                AddRowErrorsToList("block histology", "code", dtData, objErrorList)
            Case 9 'BATCH_BLOCK_ANTIBODIES
                AddRowErrorsToList("block antibodies", "code", dtData, objErrorList)
            Case 10 'BATCH_BLOCK_STAIN
                AddRowErrorsToList("block stain", "code", dtData, objErrorList)
        End Select
    End Sub




    '----------------------------------------

    Public Function UpdateTCCodeTable(ByRef dtData As DataTable, _
                                ByRef dbConn As Object, _
                                ByRef dbTran As Object, _
                                ByRef objErrorList As ArrayList, _
                                ByVal iTableID As Integer, _
                                ByVal iUserID As Integer, _
                                ByVal iBatchID As Integer)
        AddUserBatch(dtData, iBatchID, iUserID)
        UpdateTCCodeTableDetails(dtData, dbConn, dbTran, objErrorList, iTableID, iUserID)

        If objErrorList.Count > 0 Then
            Throw New Exception()
        End If

    End Function

    Private Sub UpdateTCCodeTableDetails(ByRef dtData As DataTable, _
                                         ByRef dbConn As Object, _
                                         ByRef dbTran As Object, _
                                         ByRef objErrorList As ArrayList, _
                                         ByVal iTableID As Integer, _
                                         ByVal iUserID As Integer)

        Dim objHistologyParamList As New libDataAccess.libDataAccess.UpdateParameterList()

        Dim sKeyField As String
        Dim sInsertSP As String
        Dim sUpdateSP As String
        Dim sDeleteSP As String

        Select Case iTableID
            Case 13 'BLOCK_SPECIALSTAIN_TCCODES
                sInsertSP = "AddSpecialStainTCCode"
                sUpdateSP = "EditSpecialStainTCCode"
                sDeleteSP = "DeleteSpecialStainTCCode"
            Case 14 'BLOCK_ANTIBODIES_TCCODES
                sInsertSP = "AddAntibodiesTCCode"
                sUpdateSP = "EditAntibodiesTCCode"
                sDeleteSP = "DeleteAntibodiesTCCode"
            Case 15 'BLOCK_HISTOLOGY_TCCODES
                sInsertSP = "AddHistologyTCCode"
                sUpdateSP = "EditHistologyTCCode"
                sDeleteSP = "DeleteHistologyTCCode"
        End Select

        With objHistologyParamList
            .AddInsertParam("TestID", DbtType.dbtInteger)
            .AddInsertParam("Code", DbtType.dbtString)
            .AddInsertParam("UserID", DbtType.dbtInteger)
            .AddInsertParam("BatchID", DbtType.dbtInteger)

            .AddUpdateParam("ID", DbtType.dbtInteger)
            .AddUpdateParam("TestID", DbtType.dbtInteger)
            .AddUpdateParam("Code", DbtType.dbtString)
            .AddUpdateParam("UserID", DbtType.dbtInteger)

            .AddDeleteParam("ID", DbtType.dbtInteger)

        End With

        OptimisticUpdateDataTable(dbConn, _
                                  dbTran, _
                                  AddressOf OnTCCodesUpdated, _
                                  "", _
                                  sInsertSP, _
                                  sUpdateSP, _
                                  sDeleteSP, _
                                  CommandType.StoredProcedure, _
                                  dtData, _
                                  objHistologyParamList)

        Select Case iTableID
            Case 13 'BLOCK_SPECIALSTAIN_TCCODES
                AddRowErrorsToList("stain tccodes", "code", dtData, objErrorList)
            Case 14 'BLOCK_ANTIBODIES_TCCODES
                AddRowErrorsToList("antibodies tccodes", "code", dtData, objErrorList)
            Case 15 'BLOCK_HISTOLOGY_TCCODES
                AddRowErrorsToList("histology tccodes", "code", dtData, objErrorList)
        End Select

    End Sub

    '------------------------------------------
#End Region

#Region "Data Table"

    Public Function NewItem(ByRef dtData As DataTable, _
                        ByVal sCode As String, _
                        ByVal iLinkID As Integer, _
                        ByVal sLinkField As String) As Boolean
        Try
            Dim dr As DataRow
            dr = dtData.NewRow()
            dr(sLinkField) = iLinkID
            dr("Code") = sCode
            dtData.Rows.Add(dr)
            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsCheckBoxObject)
            Return False
        End Try
    End Function

    Public Function NewBlockItem(ByRef dtData As DataTable, _
                                ByVal sCode As String, _
                                ByVal iLinkID As Integer, _
                                ByVal sLinkField As String, _
                                ByVal dataRow As DataRow) As Boolean
        Try
            Dim dr As DataRow
            dr = dtData.NewRow()
            dr(sLinkField) = iLinkID
            dr("Code") = sCode
            dtData.Rows.Add(dr)
            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsCheckBoxObject)
            Return False
        End Try
    End Function

    Public Function DeleteRows(ByRef dtData As DataTable, ByVal iKeyID As Integer, ByVal sKeyField As String) As Boolean
        Try
            Dim foundRows As DataRow()
            Dim sFilter As String = sKeyField & "=" & Convert.ToString(iKeyID)
            Dim dr As DataRow

            foundRows = dtData.Select(sFilter)
            For Each dr In foundRows
                dtData.Rows.Remove(dr)
            Next

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsCheckBoxObject)
            Return False
        End Try
    End Function

    Public Function CopyDataToNewBatch(ByRef dtOriginal As DataTable, ByRef dtNew As DataTable, ByVal iBatchID As Integer, Optional ByVal objBlockIDs As ArrayList = Nothing) As Boolean
        Try
            Dim dr As DataRow
            Dim drNewRow As DataRow
            Dim iIDs As New HistopathologyLib.clsIDPairs()
            Dim iCount As Integer = 0

            If Not objBlockIDs Is Nothing Then
                For Each dr In dtOriginal.Rows
                    For iCount = 0 To objBlockIDs.Count - 1
                        iIDs = objBlockIDs(iCount)
                        If iIDs.OldID = dr("BlockID") Then
                            NewItem(dtNew, dr("Code"), iIDs.NewID, "BlockID")
                        End If
                    Next
                Next
            Else
                For Each dr In dtOriginal.Rows
                    NewItem(dtNew, dr("Code"), iBatchID, "BatchID")
                Next
            End If

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsCheckBoxObject)
            Return False
        End Try
    End Function

#End Region

#Region "Event Handlers"

    Private Sub OnTestsUpdated(ByVal sender As Object, ByVal args As SqlRowUpdatedEventArgs)

        If args.Status = UpdateStatus.ErrorsOccurred Then
            args.Row.RowError = args.Errors.Message
            args.Status = UpdateStatus.SkipCurrentRow
        Else
            If args.RecordsAffected = 0 Then
                args.Row.RowError = "Data was changed by another user"
                args.Status = UpdateStatus.SkipCurrentRow
            End If
        End If

    End Sub

    Private Sub OnTCCodesUpdated(ByVal sender As Object, ByVal args As SqlRowUpdatedEventArgs)

        If args.Status = UpdateStatus.ErrorsOccurred Then
            args.Row.RowError = args.Errors.Message
            args.Status = UpdateStatus.SkipCurrentRow
        Else
            If args.RecordsAffected = 0 Then
                args.Row.RowError = "Data was changed by another user"
                args.Status = UpdateStatus.SkipCurrentRow
            End If
        End If

    End Sub

#End Region

End Class
