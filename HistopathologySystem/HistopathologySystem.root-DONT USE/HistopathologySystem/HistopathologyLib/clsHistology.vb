Imports libDataAccess.libDataAccess
Imports libDataAccess.libDataAccess.TBCultureDA
Imports System.Data.SqlClient

Public Class HistologyRefUpdateException : Inherits ApplicationException

    Public Sub New(ByVal message As String)
        MyBase.New(message)
    End Sub

    Public Sub New(ByVal message As String, ByVal inner As Exception)
        MyBase.New(message, inner)
    End Sub

End Class

Public Class clsHistology

#Region "Update Histology Data"

    Public Function CreateUsedHistologyRefs() As DataTable
        Dim dtUsedHistologyRefs As DataTable
        dtUsedHistologyRefs = New DataTable

        dtUsedHistologyRefs.Columns.Add("HistologyRef", System.Type.GetType("System.String"))
        dtUsedHistologyRefs.Columns.Add("HistologyType", System.Type.GetType("System.Int32"))

        SetPrimaryKey(dtUsedHistologyRefs, "HistologyRef", False)

        Return dtUsedHistologyRefs
    End Function

    Public Function CreateUnusedHistologyRefs() As DataTable
        Dim dtUnUsedHistologyRefs As DataTable
        dtUnUsedHistologyRefs = New DataTable

        dtUnUsedHistologyRefs.Columns.Add("HistologyRef", System.Type.GetType("System.String"))
        dtUnUsedHistologyRefs.Columns.Add("SenderRef", System.Type.GetType("System.String"))
        dtUnUsedHistologyRefs.Columns.Add("HistologyType", System.Type.GetType("System.Int32"))

        SetPrimaryKey(dtUnUsedHistologyRefs, "HistologyRef", False)

        Return dtUnUsedHistologyRefs
    End Function

    Public Sub AddUsedHistologyRef(ByRef dtData As DataTable, ByVal sHistologyRef As String, ByVal iHistologyType As Integer)

        Dim drHistologyRefs As DataRow()

        drHistologyRefs = dtData.Select("HistologyRef='" & sHistologyRef & "'")

        If Not drHistologyRefs Is Nothing Then
            If drHistologyRefs.Length = 0 Then
                Dim drNewRow As DataRow = dtData.NewRow
                drNewRow("HistologyRef") = sHistologyRef
                drNewRow("HistologyType") = iHistologyType
                dtData.Rows.Add(drNewRow)
            End If
        End If
    End Sub

    Public Sub AddUnusedHistologyRef(ByRef dtData As DataTable, ByVal sSenderRef As String, ByVal sHistologyRef As String, ByVal iHistologyType As Integer)

        Dim drHistologyRefs As DataRow()

        drHistologyRefs = dtData.Select("HistologyType=" & iHistologyType & " and SenderRef='" & sSenderRef & "'")

        If Not drHistologyRefs Is Nothing Then
            If drHistologyRefs.Length = 0 Then
                Dim drNewRow As DataRow = dtData.NewRow
                drNewRow("HistologyRef") = sHistologyRef
                drNewRow("SenderRef") = sSenderRef
                drNewRow("HistologyType") = iHistologyType
                dtData.Rows.Add(drNewRow)
            End If
        End If
    End Sub

    Public Function FindUnusedHistologyRef(ByRef dtData As DataTable, ByVal sSenderRef As String, ByVal iHistologyType As Integer) As String

        Dim drHistologyRefs As DataRow()

        drHistologyRefs = dtData.Select("HistologyType=" & iHistologyType & " and SenderRef='" & sSenderRef & "'")

        If Not drHistologyRefs Is Nothing Then
            If drHistologyRefs.Length = 1 Then
                Return drHistologyRefs(0)("HistologyRef")
            End If
        End If

        Return ""
    End Function

    Public Function FindUsedHistologyRef(ByRef dtData As DataTable, ByVal sHistologyRef As String) As String

        Dim drHistologyRefs As DataRow()

        If Not dtData Is Nothing Then
            drHistologyRefs = dtData.Select("HistologyRef='" & sHistologyRef & "'")

            If Not drHistologyRefs Is Nothing Then
                If drHistologyRefs.Length = 1 Then
                    Return drHistologyRefs(0)("HistologyRef")
                End If
            End If
        End If

        Return ""
    End Function

    Public Sub RemoveUnusedHistologyRef(ByRef dtData As DataTable, ByVal sHistologyRef As String)
        Dim drHistology As DataRow

        drHistology = dtData.Rows.Find(sHistologyRef)

        If drHistology Is Nothing Then
            drHistology.Delete()
        End If
    End Sub


    Public Sub SaveUnusedHistologyRef(ByRef dtData As DataTable, _
                                        ByRef objErrorList As ArrayList, _
                                        ByRef objDBConn As SqlConnection, _
                                        ByRef objDBTran As SqlTransaction)
        Dim objParamList As New libDataAccess.libDataAccess.UpdateParameterList

        With objParamList
            .AddInsertParam("HistologyRef", DbtType.dbtString)
            .AddInsertParam("SenderRef", DbtType.dbtString)
        End With
        OptimisticUpdateDataTable(objDBConn, _
                        objDBTran, _
                        AddressOf OnUnusedHistologyRowUpdated, _
                        "", _
                        "AddUnusedHistologyRef", _
                        "", _
                        "", _
                        CommandType.StoredProcedure, _
                        dtData, _
                        objParamList)

        AddRowErrorsToList("Unused HistologyRef", "HistologyRef", dtData, objErrorList)

        If objErrorList.Count > 0 Then
            Throw New Exception
        End If

    End Sub

    Public Function UpdateHistologyRefs(ByRef dtHistologyRefs As DataTable, _
                                        ByRef dbConn As Object, _
                                        ByRef dbTran As Object, _
                                        ByRef objErrorList As ArrayList)
        Dim dtChanges As DataTable = Nothing
        Dim iRowCount As Integer = 0

        dtChanges = dtHistologyRefs.GetChanges()

        If Not dtChanges Is Nothing Then
            For iRowCount = 0 To dtHistologyRefs.Rows.Count - 1
                If dtHistologyRefs.Rows(iRowCount).RowState = DataRowState.Modified Then
                    If dtHistologyRefs.Rows(iRowCount)("NextHistologyRef", DataRowVersion.Current).ToString() <> dtHistologyRefs.Rows(iRowCount)("NextHistologyRef", DataRowVersion.Original).ToString() Then
                        UpdateHistologyRefRow(dtHistologyRefs.Rows(iRowCount), dbConn, dbTran, objErrorList)
                    End If
                End If
            Next
        End If
    End Function

    Public Function UpdateHistologyRefs(ByRef drRow As DataRow, _
                                        ByRef objErrorList As ArrayList) As Boolean

        Dim objDBConn As SqlConnection = Nothing
        Dim objDBTran As SqlTransaction = Nothing

        Try
            'open a database connection and begin a transaction
            objDBConn = TBCultureDA.OpenConnection()
            objDBTran = TBCultureDA.BeginTransaction(objDBConn)

            UpdateHistologyRefRow(drRow, objDBConn, objDBTran, objErrorList)

            'commit the database transaction
            TBCultureDA.CommitTransaction(objDBTran)

        Catch exHistologyRef As HistologyRefUpdateException
            objErrorList.Add(exHistologyRef.Message)
            If Not objDBTran Is Nothing Then
                TBCultureDA.RollbackTransaction(objDBTran)
            End If
            Return False
        Catch ex As Exception
            If Not objDBTran Is Nothing Then
                TBCultureDA.RollbackTransaction(objDBTran)
            End If
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Return False
        Finally
            If Not objDBConn Is Nothing Then
                TBCultureDA.CloseConnection(objDBConn)
            End If
        End Try

        Return True
    End Function

    Private Sub UpdateHistologyRefRow(ByRef drHistologyRefRow As DataRow, _
                               ByRef objDBConn As SqlConnection, _
                               ByRef objDBTran As SqlTransaction, _
                               ByRef objErrorList As ArrayList)

        If drHistologyRefRow.RowState <> DataRowState.Modified Then
            Exit Sub
        End If

        Dim objHistologyParamList As New libDataAccess.libDataAccess.ParameterList

        With objHistologyParamList
            .AddParameter("RETURN_VALUE", DbtType.dbtInteger, "RETURN_VALUE", daDirection:=ParameterDirection.ReturnValue)
            .QuickAddInputParam("Type", DbtType.dbtInteger, drHistologyRefRow.Item("Type"))
            .QuickAddInputParam("NextHistologyRef", DbtType.dbtString, drHistologyRefRow.Item("NextHistologyRef"))
            .QuickAddInputParam("RowStamp", DbtType.dbtBinary, drHistologyRefRow.Item("RowStamp"))
        End With

        Select Case drHistologyRefRow.RowState
            Case DataRowState.Modified
                Try
                    TBCultureDA.ExecuteNonQuery(objDBConn, objDBTran, "EditHistologyRef", CommandType.StoredProcedure, objHistologyParamList)
                Catch ex As Exception
                    clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
                    Throw New HistologyRefUpdateException(ex.Message, ex.InnerException)
                End Try
                Dim iReturnValue As Integer = CInt(objHistologyParamList("RETURN_VALUE").Value)
                Select Case iReturnValue
                    Case 1
                        Throw New HistologyRefUpdateException("Another User has altered the HistologyRef Record.")
                End Select
        End Select

    End Sub

#End Region

#Region "Private Functions"

    Private Sub UpdateBlockIDs(ByRef dtHistology As DataTable, ByVal objNewIDsList As ArrayList)
        Dim iListCount As Integer
        Dim objNewIDs As New HistopathologyLib.clsIDPairs()
        Dim dr As DataRow
        Dim iRowsCount As Integer
        Dim iBlockID As Integer

        For iListCount = 0 To objNewIDsList.Count - 1
            objNewIDs = objNewIDsList(iListCount)
            For Each dr In dtHistology.Rows
                If dr("BlockID") = objNewIDs.OldID Then
                    dr("BlockID") = objNewIDs.NewID
                End If
            Next
        Next
    End Sub

    Private Sub UpdateBatchID(ByRef dtHistology As DataTable, ByVal iBatchID As Integer)
        Dim iRowsCount As Integer
        Dim dr As DataRow
        For Each dr In dtHistology.Rows
            dr("BatchID") = iBatchID
        Next
    End Sub

        Private Sub OnUnusedHistologyRowUpdated(ByVal sender As Object, ByVal args As SqlRowUpdatedEventArgs)

        If args.Status = UpdateStatus.ErrorsOccurred Then
            args.Row.RowError = args.Errors.Message
            args.Status = UpdateStatus.SkipCurrentRow
        Else
            If args.RecordsAffected = 0 Then
                args.Row.RowError = "Failed to update the block"
                args.Status = UpdateStatus.SkipCurrentRow
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

#End Region

#Region "Get Histology Data"

    Public Function GetNextAvailableHistologyRef(ByVal iHistoType As Integer, ByRef sNextHistologyRef As String) As Boolean
        Try
            Dim objInParamList As New ParameterList

            objInParamList.AddParameter("RETURN_VALUE", DbtType.dbtInteger, "RETURN_VALUE", daDirection:=ParameterDirection.ReturnValue)
            objInParamList.QuickAddInputParam("Type", DbtType.dbtInteger, iHistoType)
            objInParamList.AddParameter("NextHistologyRef", DbtType.dbtString, "@NextHistologyRef", "NextHistologyRef", 5, , ParameterDirection.Output)
            objInParamList.AddParameter("RowStamp", DbtType.dbtBinary, "@RowStamp", "RowStamp", 8, , ParameterDirection.Output)

            ExecuteNonQuery("GetNextHistologyRef", _
                            CommandType.StoredProcedure, _
                            objInParamList)

            sNextHistologyRef = CStr(objInParamList("NextHistologyRef").Value)

            If sNextHistologyRef = "" Then
                Return False
            End If

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsHistologyObject)
            Return False
        End Try
    End Function

    Public Function GetUnUsedHistologyRefsTable(ByRef dtData As DataTable) As Boolean
        Try
            FillDataTable("GetUnUsedHistologyRefs", _
                          CommandType.StoredProcedure, _
                          dtData)

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsHistologyObject)
            Return False
        End Try
    End Function

    Public Function GetHistologyRefsTable(ByRef dtData As DataTable) As Boolean
        Try
            FillDataTable("GetHistologyRefs", _
                          CommandType.StoredProcedure, _
                          dtData)

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsHistologyObject)
            Return False
        End Try
    End Function

#End Region

End Class
