Imports libDataAccess.libDataAccess
Imports libDataAccess.libDataAccess.TBCultureDA

Public Class clsAuditLog

#Region "Private Functions"

    Private Function FormatEmptyString(ByVal sValue As String) As Object
        If sValue = "" Then
            Return DBNull.Value
        Else
            Return sValue
        End If
    End Function

#End Region

    Public Function GetSubmissionAuditLogReport(ByVal sSubmissionID As String, _
                                                      ByVal dStartDate As String, _
                                                      ByVal dEndDate As String, _
                                                      ByRef dtData As DataTable) As Boolean

        Dim objInParamList As New ParameterList
        Dim objIDParamList As New ParameterList

        Dim objTempParamList As New ParameterList

        Dim dtIDData As DataTable
        Dim dtTempResults As DataTable

        Dim dr As DataRow
        Dim drAuditRow As DataRow
        Dim sFilter As String
        Dim drFoundRows As DataRow()

        Try
            With objInParamList
                .QuickAddInputParam("StartDate", DbtType.dbtDateTime, FormatEmptyString(dStartDate))
                .QuickAddInputParam("EndDate", DbtType.dbtDateTime, FormatEmptyString(dEndDate))
                .QuickAddInputParam("SubmissionID", DbtType.dbtString, sSubmissionID)
            End With

            With objIDParamList
                .QuickAddInputParam("ID", DbtType.dbtInteger, CInt(sSubmissionID))
            End With


            FillDataTable("GetAuditLogBySubmission", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            FillDataTable("GetBatchTissuesIDs", _
                          CommandType.StoredProcedure, _
                          dtIDData, _
                          objIDParamList)

            For Each dr In dtIDData.Rows
                Dim objAuditParamList As New ParameterList

                With objAuditParamList
                    .QuickAddInputParam("ID", DbtType.dbtInteger, dr("ID"))
                    .QuickAddInputParam("Blocked", DbtType.dbtInteger, 0)
                    .QuickAddInputParam("StartDate", DbtType.dbtDateTime, FormatEmptyString(dStartDate))
                    .QuickAddInputParam("EndDate", DbtType.dbtDateTime, FormatEmptyString(dEndDate))
                End With

                FillDataTable("GetAuditLogTissue", _
                              CommandType.StoredProcedure, _
                              dtTempResults, _
                              objAuditParamList)

                For Each drAuditRow In dtTempResults.Rows
                    dtData.ImportRow(drAuditRow)
                Next
            Next

            If Not dtTempResults Is Nothing Then
                dtTempResults.Clear()
            End If
            If Not dtIDData Is Nothing Then
                dtIDData.Clear()
            End If


            FillDataTable("GetBatchBlockTissuesIDs", _
                          CommandType.StoredProcedure, _
                          dtIDData, _
                          objIDParamList)

            For Each dr In dtIDData.Rows
                Dim objAuditParamList As New ParameterList

                With objAuditParamList
                    .QuickAddInputParam("ID", DbtType.dbtInteger, dr("ID"))
                    .QuickAddInputParam("Blocked", DbtType.dbtInteger, 1)
                    .QuickAddInputParam("StartDate", DbtType.dbtDateTime, FormatEmptyString(dStartDate))
                    .QuickAddInputParam("EndDate", DbtType.dbtDateTime, FormatEmptyString(dEndDate))
                End With

                FillDataTable("GetAuditLogTissue", _
                              CommandType.StoredProcedure, _
                              dtTempResults, _
                              objAuditParamList)

                For Each drAuditRow In dtTempResults.Rows
                    dtData.ImportRow(drAuditRow)
                Next
            Next

            If Not dtTempResults Is Nothing Then
                dtTempResults.Clear()
            End If
            If Not dtIDData Is Nothing Then
                dtIDData.Clear()
            End If

            FillDataTable("GetBatchBlockAnimalIDs", _
                          CommandType.StoredProcedure, _
                          dtIDData, _
                          objIDParamList)

            For Each dr In dtIDData.Rows
                Dim objAuditParamList As New ParameterList

                With objAuditParamList
                    .QuickAddInputParam("ID", DbtType.dbtInteger, dr("ID"))
                    .QuickAddInputParam("StartDate", DbtType.dbtDateTime, FormatEmptyString(dStartDate))
                    .QuickAddInputParam("EndDate", DbtType.dbtDateTime, FormatEmptyString(dEndDate))
                End With

                FillDataTable("GetAuditLogAnimal", _
                              CommandType.StoredProcedure, _
                              dtTempResults, _
                              objAuditParamList)

                For Each drAuditRow In dtTempResults.Rows
                    sFilter = "ID" = "'" & drAuditRow("ID") & "'" & "AND TableName=" & "'" & "Animal" & "'"
                    drFoundRows = dtData.Select(sFilter)

                    If Not drFoundRows Is Nothing And drFoundRows.Length = 0 Then
                        dtData.ImportRow(drAuditRow)
                    End If
                Next
            Next

            If Not dtTempResults Is Nothing Then
                dtTempResults.Clear()
            End If
            If Not dtIDData Is Nothing Then
                dtIDData.Clear()
            End If

            FillDataTable("GetBatchBlockAntibodiesIDs", _
                          CommandType.StoredProcedure, _
                          dtIDData, _
                          objIDParamList)

            For Each dr In dtIDData.Rows
                Dim objAuditParamList As New ParameterList

                With objAuditParamList
                    .QuickAddInputParam("ID", DbtType.dbtInteger, dr("ID"))
                    .QuickAddInputParam("StartDate", DbtType.dbtDateTime, FormatEmptyString(dStartDate))
                    .QuickAddInputParam("EndDate", DbtType.dbtDateTime, FormatEmptyString(dEndDate))
                End With

                FillDataTable("GetAuditLogAntibodies", _
                              CommandType.StoredProcedure, _
                              dtTempResults, _
                              objAuditParamList)

                For Each drAuditRow In dtTempResults.Rows
                    dtData.ImportRow(drAuditRow)
                Next
            Next

            If Not dtTempResults Is Nothing Then
                dtTempResults.Clear()
            End If
            If Not dtIDData Is Nothing Then
                dtIDData.Clear()
            End If

            FillDataTable("GetBatchBlockHistologyIDs", _
                          CommandType.StoredProcedure, _
                          dtIDData, _
                          objIDParamList)

            For Each dr In dtIDData.Rows
                Dim objAuditParamList As New ParameterList

                With objAuditParamList
                    .QuickAddInputParam("ID", DbtType.dbtInteger, dr("ID"))
                    .QuickAddInputParam("StartDate", DbtType.dbtDateTime, FormatEmptyString(dStartDate))
                    .QuickAddInputParam("EndDate", DbtType.dbtDateTime, FormatEmptyString(dEndDate))
                End With

                FillDataTable("GetAuditLogHistology", _
                              CommandType.StoredProcedure, _
                              dtTempResults, _
                              objAuditParamList)

                For Each drAuditRow In dtTempResults.Rows
                    dtData.ImportRow(drAuditRow)
                Next
            Next


            If Not dtTempResults Is Nothing Then
                dtTempResults.Clear()
            End If
            If Not dtIDData Is Nothing Then
                dtIDData.Clear()
            End If

            FillDataTable("GetBatchBlockStainIDs", _
                          CommandType.StoredProcedure, _
                          dtIDData, _
                          objIDParamList)

            For Each dr In dtIDData.Rows
                Dim objAuditParamList As New ParameterList

                With objAuditParamList
                    .QuickAddInputParam("ID", DbtType.dbtInteger, dr("ID"))
                    .QuickAddInputParam("StartDate", DbtType.dbtDateTime, FormatEmptyString(dStartDate))
                    .QuickAddInputParam("EndDate", DbtType.dbtDateTime, FormatEmptyString(dEndDate))
                End With

                FillDataTable("GetAuditLogStains", _
                              CommandType.StoredProcedure, _
                              dtTempResults, _
                              objAuditParamList)

                For Each drAuditRow In dtTempResults.Rows
                    dtData.ImportRow(drAuditRow)
                Next
            Next

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Return False
        End Try

    End Function

    Public Function GetDailyAuditLogReport(ByVal dDate As Date, ByRef dtData As DataTable) As Boolean

        Dim objInParamList As New ParameterList

        Try
            With objInParamList
                .QuickAddInputParam("LogDate", DbtType.dbtDateTime, dDate)
            End With

            FillDataTable("GetAuditLogByDate", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Return False
        End Try

    End Function

    Public Function GetUserAuditLogReport(ByVal iUserID As Integer, ByVal dStartDate As Date, ByVal dEndDate As Date, ByRef dtData As DataTable) As Boolean

        Dim objInParamList As New ParameterList

        Try
            With objInParamList
                .QuickAddInputParam("StartDate", DbtType.dbtDateTime, dStartDate)
                .QuickAddInputParam("EndDate", DbtType.dbtDateTime, dEndDate)
                .QuickAddInputParam("UserID", DbtType.dbtInteger, iUserID)
            End With

            FillDataTable("GetAuditLogByUser", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Return False
        End Try

    End Function

End Class
