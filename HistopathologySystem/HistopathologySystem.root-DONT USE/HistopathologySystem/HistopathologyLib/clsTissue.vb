Imports libDataAccess.libDataAccess
Imports libDataAccess.libDataAccess.TBCultureDA

Public Class TissueUpdateException : Inherits ApplicationException

    Public Sub New(ByVal message As String)
        MyBase.New(message)
    End Sub

    Public Sub New(ByVal message As String, ByVal inner As Exception)
        MyBase.New(message, inner)
    End Sub

End Class

Public Class clsTissue

#Region "Update Tissue Data"


    Private Sub UpdateTissueDetails(ByRef dtTissues As DataTable, _
                                ByRef dbConn As Object, _
                                ByRef dbTran As Object, _
                                ByRef objErrorList As ArrayList, _
                                ByVal sKeyField As String, _
                                ByVal iUserID As Integer)

        Dim objTissueParamList As New libDataAccess.libDataAccess.UpdateParameterList()
        Dim dr As DataRow

        AddUserID(dtTissues, iUserID)

        With objTissueParamList
            .AddInsertParam(sKeyField, DbtType.dbtInteger)
            .AddInsertParam("TissueCode", DbtType.dbtString)
            .AddInsertParam("NoPieces", DbtType.dbtSmallInt)
            .AddInsertParam("Comment", DbtType.dbtString)

            .AddUpdateParam("ID", DbtType.dbtInteger)
            .AddUpdateParam(sKeyField, DbtType.dbtInteger)
            .AddUpdateParam("TissueCode", DbtType.dbtString)
            .AddUpdateParam("NoPieces", DbtType.dbtSmallInt)
            .AddUpdateParam("Comment", DbtType.dbtString)
            .AddUpdateParam("UserID", DbtType.dbtInteger)
            .AddUpdateParam("RowStamp", DbtType.dbtBinary)

            'If we are updating the Batch tissues then add the archive parameters
            If sKeyField = "BatchSubmissionID" Then
                .AddUpdateParam("ArchiveLocation", DbtType.dbtString)
                .AddUpdateParam("ArchivedDate", DbtType.dbtDateTime)
                .AddUpdateParam("ArchiveComment", DbtType.dbtString)
            End If

            .AddDeleteParam("ID", DbtType.dbtInteger)
        End With

        If sKeyField = "BatchSubmissionID" Then
            UpdateDataTable(dbConn, dbTran, _
                            "", _
                            "AddTissue", _
                            "EditTissue", _
                            "DeleteTissue", _
                            CommandType.StoredProcedure, _
                            dtTissues, _
                            objTissueParamList)
        Else
            UpdateDataTable(dbConn, dbTran, _
                                "", _
                                "AddBlockTissue", _
                                "EditBlockTissue", _
                                "DeleteBlockTissue", _
                                CommandType.StoredProcedure, _
                                dtTissues, _
                                objTissueParamList)
        End If
    End Sub

    Public Function UpdateTissues(ByRef dtTissues As DataTable, _
                                    ByRef dbConn As Object, _
                                    ByRef dbTran As Object, _
                                    ByRef objErrorList As ArrayList, _
                                    ByVal objNewIDsList As ArrayList, _
                                    ByVal sKeyField As String, _
                                    ByVal iUserID As Integer)
        Try
            UpdateIDs(dtTissues, objNewIDsList, sKeyField)
            UpdateTissueDetails(dtTissues, dbConn, dbTran, objErrorList, sKeyField, iUserID)
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsTissueObject)
            objErrorList.Add("An application error caused the tissue update to fail")
            Throw New Exception()
        End Try
    End Function

#End Region

#Region "Handle Data Table"

    Public Function NewBlockTissue(ByRef dtTissues As DataTable, ByVal iBlockID As Integer, ByVal dr As DataRow) As Boolean
        Try
            Dim drNew As DataRow
            drNew = dtTissues.NewRow()
            drNew("BlockID") = iBlockID
            drNew("TissueCode") = dr("TissueCode")
            drNew("NoPieces") = dr("NoPieces")
            dtTissues.Rows.Add(drNew)

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsTissueObject)
            Return False
        End Try
    End Function

    Public Function NewBatchSubmissionTissue(ByRef dtTissues As DataTable, ByVal iBatchSubmission As Integer, ByVal dr As DataRow) As Boolean
        Try
            Dim drNew As DataRow
            drNew = dtTissues.NewRow()
            drNew("BatchSubmissionID") = iBatchSubmission
            drNew("TissueCode") = dr("TissueCode")
            drNew("NoPieces") = dr("NoPieces")
            drNew("Comment") = dr("Comment")
            dtTissues.Rows.Add(drNew)

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsTissueObject)
            Return False
        End Try
    End Function

    Public Function CopyDataToNewBatch(ByRef dtOriginal As DataTable, _
                                       ByRef dtNew As DataTable, _
                                       ByVal objIDPairs As ArrayList, _
                                       ByVal bBlocked As Boolean) As Boolean
        Try
            Dim dr As DataRow
            Dim drNewRow As DataRow
            Dim objIDs As New HistopathologyLib.clsIDPairs()
            Dim iCount As Integer = 0

            'Copy the data 
            For Each dr In dtOriginal.Rows
                For iCount = 0 To objIDPairs.Count - 1
                    objIDs = objIDPairs(iCount)
                    If Not bBlocked Then
                        If objIDs.OldID = dr("BatchSubmissionID") Then
                            NewBatchSubmissionTissue(dtNew, objIDs.NewID, dr)
                        End If
                    Else
                        If objIDs.OldID = dr("BlockID") Then
                            NewBlockTissue(dtNew, objIDs.NewID, dr)
                        End If
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

#Region "Private Functions"

    Private Sub UpdateIDs(ByRef dtTissues As DataTable, ByVal objNewIDsList As ArrayList, ByVal sKeyField As String)
        Dim iListCount As Integer
        Dim objNewIDs As New HistopathologyLib.clsIDPairs()
        Dim dr As DataRow

        For iListCount = 0 To objNewIDsList.Count - 1
            objNewIDs = objNewIDsList(iListCount)
            For Each dr In dtTissues.Rows
                If Not dr.RowState = DataRowState.Deleted Then
                    If dr(sKeyField) = objNewIDs.OldID Then
                        dr(sKeyField) = objNewIDs.NewID
                    End If
                End If
            Next
        Next
    End Sub

    Private Sub AddUserID(ByRef dtData As DataTable, ByVal iUserID As Integer)
        Dim dr As DataRow
        Dim dcCol As DataColumn
        Dim bFound As Boolean = False

        For Each dcCol In dtData.Columns
            If dcCol.ColumnName = "UserID" Then
                bFound = True
            End If
        Next

        If Not bFound Then
            dtData.Columns.Add("UserID")
            For Each dr In dtData.Rows
                If dr.RowState = DataRowState.Modified Then
                    dr("UserID") = iUserID
                End If
            Next
        Else
            For Each dr In dtData.Rows
                If dr.RowState = DataRowState.Modified Then
                    dr("UserID") = iUserID
                End If
            Next
        End If
    End Sub

#End Region

#Region "Get Tissue Data"

    Public Function GetBatchAnimalTissues(ByVal iBatchID As Integer, ByVal iAnimalID As Integer) As DataTable
        Try
            Dim dtData As New DataTable()
            Dim objInParamList As New ParameterList()

            objInParamList.QuickAddInputParam("BatchID", DbtType.dbtInteger, iBatchID)
            objInParamList.QuickAddInputParam("AnimalID", DbtType.dbtInteger, iAnimalID)

            FillDataTable("GetBatchSampleTissues", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            SetPrimaryKey(dtData, "ID", True)
            dtData.Columns.Add("BeenAdded", System.Type.GetType("System.Boolean"))

            Return dtData
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsTissueObject)
            Return Nothing
        End Try
    End Function

#End Region
End Class
