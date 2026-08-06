Imports libDataAccess.libDataAccess
Imports libDataAccess.libDataAccess.TBCultureDA

Public Class LookupData

    Public Function ListEditableLookups() As DataTable

        Try
            Dim dtData As New DataTable()
            TBCultureDA.FillDataTable("GetEditableLookups", _
                                      CommandType.StoredProcedure, _
                                      dtData)
            Return dtData
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Return Nothing
        End Try

    End Function

    Public Function GetContactsByArea(ByVal sSubmittedArea As String) As DataTable
        Dim dtData As New DataTable()
        Try

            Dim objInParamList As New ParameterList()

            objInParamList.QuickAddInputParam("Area", DbtType.dbtString, sSubmittedArea)

            FillDataTable("GetContactsArea", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)
            Return dtData

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Return Nothing
        End Try

    End Function

    Public Function GetProjectsByArea(ByVal sSubmittedArea As String) As DataTable
        Dim dtData As New DataTable()
        Try

            Dim objInParamList As New ParameterList()

            objInParamList.QuickAddInputParam("Area", DbtType.dbtString, sSubmittedArea)

            FillDataTable("GetProjectsArea", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)
            Return dtData

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Return Nothing
        End Try

    End Function



    Public Function GetUserAreaData(ByVal TableID As Integer, ByVal sUserArea As String)
        Dim dtData As New DataTable()
        Dim objInParamList As New ParameterList()

        Try
            Dim sProcName As String = GetSelectProc(TableID)
            If sProcName = "" Then
                Throw New Exception("The look-up table Select procedure could not " _
                                    & "be found for table ID " & TableID.ToString)
            End If

            objInParamList.QuickAddInputParam("UserArea", DbtType.dbtString, sUserArea)

            FillDataTable(sProcName, _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            SetPrimaryKey(dtData, "ID", True)

            Return dtData

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Return Nothing
        End Try
    End Function

    Public Function GetImportedtables() As DataTable
        Dim dtData As DataTable

        Try
            FillDataTable("GetluImportedTables", _
                          CommandType.StoredProcedure, _
                          dtData)

            SetPrimaryKey(dtData, "ID", True)

            Return dtData
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Return Nothing
        End Try

    End Function
    Public Function GetLookupData(ByVal TableID As Integer, Optional ByVal bIncludeInactive As Boolean = False) As DataTable

        Dim dtData As New DataTable

        Try
            Dim sProcName As String = GetSelectProc(TableID)
            If sProcName = "" Then
                Throw New Exception("The look-up table Select procedure could not " _
                                    & "be found for table ID " & TableID.ToString)
            End If

            If bIncludeInactive Then
                sProcName = sProcName & "All"
            End If

            FillDataTable(sProcName, _
                          CommandType.StoredProcedure, _
                          dtData)

            ' set the ID field as the primary key - this should be the same for
            ' all lookup tables
            SetPrimaryKey(dtData, "ID", True)

            Return dtData
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Return Nothing
        End Try

    End Function

    Private Function GetLookupDataTable(ByVal strStoredProc As String) As DataTable

        Dim dtData As New DataTable

        Try
            FillDataTable(strStoredProc, _
                          CommandType.StoredProcedure, _
                          dtData)

            Return dtData
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Return Nothing
        End Try

    End Function

    Private Function GetSelectProc(ByVal ID As Integer) As String
        Dim sSelect As String
        Dim sUpdate As String
        Dim sInsert As String
        Dim sDelete As String
        GetStoredProcedures(ID, sSelect, sUpdate, sInsert, sDelete)
        Return sSelect
    End Function

    Private Sub GetStoredProcedures(ByVal ID As Integer, _
                                    ByRef sSelectProc As String, _
                                    ByRef sUpdateProc As String, _
                                    ByRef sInsertProc As String, _
                                    ByRef sDeleteProc As String)

        Dim ParamsIn As New ParameterList
        Dim ParamsOut As New ParameterList

        ParamsIn.QuickAddInputParam("ID", DbtType.dbtInteger, ID)

        ParamsOut.AddParameter("SelectStoredProcedure", DbtType.dbtString, "", "SelectStoredProcedure", , "", ParameterDirection.Output)
        ParamsOut.AddParameter("UpdateStoredProcedure", DbtType.dbtString, "", "UpdateStoredProcedure", , "", ParameterDirection.Output)
        ParamsOut.AddParameter("InsertStoredProcedure", DbtType.dbtString, "", "InsertStoredProcedure", , "", ParameterDirection.Output)
        ParamsOut.AddParameter("DeleteStoredProcedure", DbtType.dbtString, "", "DeleteStoredProcedure", , "", ParameterDirection.Output)

        ExecuteQuery("GetEditableLookupProcs", _
                     CommandType.StoredProcedure, _
                     ParamsOut, _
                     ParamsIn)

        sSelectProc = CStr(ParamsOut.Item("SelectStoredProcedure").Value)
        sUpdateProc = CStr(ParamsOut.Item("UpdateStoredProcedure").Value)
        sInsertProc = CStr(ParamsOut.Item("InsertStoredProcedure").Value)
        sDeleteProc = CStr(ParamsOut.Item("DeleteStoredProcedure").Value)

    End Sub

    Public Function SaveLookupData(ByVal TableID As Integer, _
                                    ByRef dt As DataTable, _
                                    ByVal iUserID As Integer) As Boolean
        Try
            Dim sSelect As String
            Dim sUpdate As String
            Dim sInsert As String
            Dim sDelete As String
            GetStoredProcedures(TableID, sSelect, sUpdate, sInsert, sDelete)

            Dim params As New libDataAccess.libDataAccess.UpdateParameterList

            Select Case TableID
                Case 18 'luContacts
                    BuildParamListID(params, dt, iUserID)
                Case 19 'luProjects
                    BuildParamListID(params, dt, iUserID)
                Case Else
                    BuildParamListCommon(params, dt, iUserID)
            End Select

            TBCultureDA.UpdateDataTable("", sInsert, sUpdate, sDelete, _
                                        CommandType.StoredProcedure, _
                                        dt, params)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Return False
        End Try
    End Function

    Public Function GetUserGroups() As DataTable
        Dim dtData As New DataTable

        Try
            FillDataTable("GetluUserGroup", CommandType.StoredProcedure, dtData)
            Return dtData
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Return Nothing
        End Try
    End Function

    Public Function GetUserAreas() As DataTable
        Dim dtData As New DataTable

        Try
            FillDataTable("GetluUserArea", CommandType.StoredProcedure, dtData)
            Return dtData
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Return Nothing
        End Try
    End Function

    Public Function GetSpeciesLookup() As DataTable
        Dim dtSpecies As New DataTable
        Dim drRow As DataRow

        Try
            FillDataTable("GetluSpecies", CommandType.StoredProcedure, dtSpecies)

            Return dtSpecies

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Return Nothing
        End Try
    End Function

    Public Function GetSpeciesDescription(ByVal sCode As String) As String
        Try
            Dim dtSpecies As New DataTable
            Dim drRow As DataRow

            FillDataTable("GetluSpecies", CommandType.StoredProcedure, dtSpecies)

            If Not dtSpecies Is Nothing Then
                Dim dv As New DataView(dtSpecies, "", "SpeciesID", DataViewRowState.CurrentRows)
                Dim iRow As Integer = dv.Find(sCode)
                If iRow >= 0 Then
                    Return dv(iRow).Item("Species").ToString()
                Else
                    Return ""
                End If
            Else
                Return ""
            End If

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Return Nothing
        End Try
    End Function


#Region "Histology Functions"

    Private Sub BuildParamListCommon(ByRef params As libDataAccess.libDataAccess.UpdateParameterList, ByRef dt As DataTable, ByVal iUserID As Integer)
        ' update parameters
        Dim dr As DataRow
        Dim dcCol As DataColumn
        Dim bFound As Boolean = False

        For Each dcCol In dt.Columns
            If dcCol.ColumnName = "UserID" Then
                bFound = True
            End If
        Next

        If Not bFound Then
            dt.Columns.Add("UserID")
            For Each dr In dt.Rows
                If dr.RowState = DataRowState.Modified Then
                    dr("UserID") = iUserID
                End If
            Next
        Else
            For Each dr In dt.Rows
                If dr.RowState = DataRowState.Modified Then
                    dr("UserID") = iUserID
                End If
            Next
        End If

        params.AddUpdateParam("Original_Code", Parameter.ToDbtType(dt.Columns("Code").DataType), , drvSourceVersion:=DataRowVersion.Original)
        params.AddUpdateParam("Code", Parameter.ToDbtType(dt.Columns("Code").DataType))
        params.AddUpdateParam("Description", Parameter.ToDbtType(dt.Columns("Description").DataType))
        params.AddUpdateParam("IsActive", Parameter.ToDbtType(dt.Columns("IsActive").DataType))
        params.AddUpdateParam("UserID", DbtType.dbtInteger, iUserID)

        ' insert parameters
        params.AddInsertParam("Code", Parameter.ToDbtType(dt.Columns("Code").DataType))
        params.AddInsertParam("Description", Parameter.ToDbtType(dt.Columns("Description").DataType))
        params.AddInsertParam("IsActive", Parameter.ToDbtType(dt.Columns("IsActive").DataType))

        ' delete parameters
        params.AddDeleteParam("Code", Parameter.ToDbtType(dt.Columns("Code").DataType))
    End Sub

    Private Sub BuildParamListID(ByRef params As libDataAccess.libDataAccess.UpdateParameterList, ByRef dt As DataTable, ByVal iUserID As Integer)
        ' update parameters
        Dim dr As DataRow
        Dim dcCol As DataColumn
        Dim bFound As Boolean = False

        For Each dcCol In dt.Columns
            If dcCol.ColumnName = "UserID" Then
                bFound = True
            End If
        Next

        If Not bFound Then
            dt.Columns.Add("UserID")
            For Each dr In dt.Rows
                If dr.RowState = DataRowState.Modified Then
                    dr("UserID") = iUserID
                End If
            Next
        Else
            For Each dr In dt.Rows
                If dr.RowState = DataRowState.Modified Then
                    dr("UserID") = iUserID
                End If
            Next
        End If

        ' update parameters
        params.AddUpdateParam("ID", DbtType.dbtInteger)
        params.AddUpdateParam("Description", DbtType.dbtString)
        params.AddUpdateParam("IsActive", DbtType.dbtBoolean)
        params.AddUpdateParam("UserID", DbtType.dbtInteger, iUserID)

        ' insert parameters
        'params.AddInsertParam("ID", DbtType.dbtInteger)
        params.AddInsertParam("IsActive", DbtType.dbtBoolean)
        params.AddInsertParam("Description", DbtType.dbtString)
        params.AddInsertParam("ID", DbtType.dbtInteger, , ParameterDirection.Output)
        params.AddInsertParam("Area", DbtType.dbtString)

        ' delete parameters
        params.AddDeleteParam("ID", DbtType.dbtInteger)

    End Sub

    Public Function GetHistologyLookupData() As DataTable
        Dim dtData As New DataTable
        Try
            dtData = GetLookupDataTable("GetluHistology")
            Return dtData
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Return Nothing
        End Try
    End Function

    Public Function GetStatusLookupData() As DataTable
        Dim dtData As New DataTable
        Try
            dtData = GetLookupDataTable("GetluStatus")
            Return dtData
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Return Nothing
        End Try
    End Function

    Public Function GetTestResultLookupData() As DataTable
        Dim dtData As New DataTable
        Try
            dtData = GetLookupDataTable("GetluTestResult")
            Return dtData
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Return Nothing
        End Try
    End Function

    Public Function GetHistologyRefLookupData() As DataTable
        Dim dtData As New DataTable
        Try
            dtData = GetLookupDataTable("GetluHistologyRefType")
            Return dtData
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Return Nothing
        End Try
    End Function

    Private Sub BuildParamListUserAreas(ByRef params As libDataAccess.libDataAccess.UpdateParameterList, ByRef dt As DataTable)
        ' update parameters
        params.AddUpdateParam("Original_Code", Parameter.ToDbtType(dt.Columns("Code").DataType), drvSourceVersion:=DataRowVersion.Original)
        params.AddUpdateParam("Code", Parameter.ToDbtType(dt.Columns("Code").DataType))
        params.AddUpdateParam("Description", DbtType.dbtString)
        params.AddUpdateParam("IsActive", DbtType.dbtBoolean)
        params.AddUpdateParam("UserArea", DbtType.dbtString)

        ' insert parameters
        params.AddInsertParam("Code", Parameter.ToDbtType(dt.Columns("Code").DataType))
        params.AddInsertParam("Description", DbtType.dbtString)
        params.AddInsertParam("IsActive", DbtType.dbtBoolean)
        params.AddInsertParam("UserArea", DbtType.dbtString)

        ' delete parameters
        params.AddDeleteParam("Code", Parameter.ToDbtType(dt.Columns("Code").DataType))
    End Sub

#End Region



#Region "BSE Functions"
    'Public Function GetNonGBCounty() As DataTable

    '    Dim dtData As New DataTable()

    '    Try
    '        FillDataTable("GetNonGBCounty", _
    '                      CommandType.StoredProcedure, _
    '                      dtData)

    '        Return dtData
    '    Catch ex As Exception
    '        clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
    '        Return Nothing
    '    End Try

    'End Function

    'Public Function GetBSERegionID() As DataTable

    '    Dim dtData As New DataTable()

    '    Try
    '        FillDataTable("GetluBSERegion", _
    '                      CommandType.StoredProcedure, _
    '                      dtData)

    '        Return dtData
    '    Catch ex As Exception
    '        clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
    '        Return Nothing
    '    End Try

    'End Function

    Private Sub BuildParamListAHO(ByRef params As libDataAccess.libDataAccess.UpdateParameterList, ByRef dt As DataTable)
        ' update parameters
        params.AddUpdateParam("Original_Code", DbtType.dbtString, drvSourceVersion:=DataRowVersion.Original)
        params.AddUpdateParam("Code", DbtType.dbtString)
        params.AddUpdateParam("Name", DbtType.dbtString)
        params.AddUpdateParam("BSERegionID", DbtType.dbtInteger)

        ' insert parameters
        params.AddInsertParam("Code", DbtType.dbtString)
        params.AddInsertParam("Name", DbtType.dbtString)
        params.AddInsertParam("BSERegionID", DbtType.dbtInteger)

        ' delete parameters
        params.AddDeleteParam("Code", DbtType.dbtString)
    End Sub

    Private Sub BuildParamListBreed(ByRef params As libDataAccess.libDataAccess.UpdateParameterList, ByRef dt As DataTable)
        ' update parameters
        params.AddUpdateParam("Original_Code", DbtType.dbtString, drvSourceVersion:=DataRowVersion.Original)
        params.AddUpdateParam("Code", DbtType.dbtString)
        params.AddUpdateParam("FullName", DbtType.dbtString)
        params.AddUpdateParam("AmalgamatedName", DbtType.dbtString)

        ' insert parameters
        params.AddInsertParam("Code", Parameter.ToDbtType(dt.Columns("Code").DataType))
        params.AddInsertParam("FullName", DbtType.dbtString)
        params.AddInsertParam("AmalgamatedName", DbtType.dbtString)

        ' delete parameters
        params.AddDeleteParam("Code", Parameter.ToDbtType(dt.Columns("Code").DataType))
    End Sub

    Private Sub BuildParamListBSECounty(ByRef params As libDataAccess.libDataAccess.UpdateParameterList, ByRef dt As DataTable)
        ' update parameters
        params.AddUpdateParam("IDColumn", DbtType.dbtString)
        params.AddUpdateParam("Original_Code", DbtType.dbtString, drvSourceVersion:=DataRowVersion.Original)
        params.AddUpdateParam("Code", DbtType.dbtString)
        params.AddUpdateParam("Description", DbtType.dbtString)
        params.AddUpdateParam("BSERegionID", DbtType.dbtInteger)

        ' insert parameters
        params.AddInsertParam("IDColumn", DbtType.dbtString)
        params.AddInsertParam("Code", Parameter.ToDbtType(dt.Columns("Code").DataType))
        params.AddInsertParam("Description", DbtType.dbtString)
        params.AddInsertParam("BSERegionID", DbtType.dbtInteger)

        ' delete parameters
        params.AddDeleteParam("Code", Parameter.ToDbtType(dt.Columns("Code").DataType))
    End Sub

    Private Sub BuildParamListRelationFate(ByRef params As libDataAccess.libDataAccess.UpdateParameterList, ByRef dt As DataTable)
        ' update parameters
        params.AddUpdateParam("Original_Code", Parameter.ToDbtType(dt.Columns("Code").DataType), drvSourceVersion:=DataRowVersion.Original)
        params.AddUpdateParam("Code", Parameter.ToDbtType(dt.Columns("Code").DataType))
        params.AddUpdateParam("Description", DbtType.dbtString)
        params.AddUpdateParam("IsActive", DbtType.dbtBoolean)

        ' insert parameters
        params.AddInsertParam("Code", Parameter.ToDbtType(dt.Columns("Code").DataType))
        params.AddInsertParam("Description", DbtType.dbtString)
        params.AddInsertParam("IsActive", DbtType.dbtBoolean)

        ' delete parameters
        params.AddDeleteParam("Code", Parameter.ToDbtType(dt.Columns("Code").DataType))
    End Sub

    Private Sub BuildParamListSupplier(ByRef params As libDataAccess.libDataAccess.UpdateParameterList, ByRef dt As DataTable)
        ' update parameters
        params.AddUpdateParam("ID", DbtType.dbtInteger)
        params.AddUpdateParam("Name", DbtType.dbtString)
        params.AddUpdateParam("Details", DbtType.dbtString)

        ' insert parameters
        params.AddInsertParam("Name", DbtType.dbtString)
        params.AddInsertParam("Details", DbtType.dbtString)
        params.AddInsertParam("ID", DbtType.dbtInteger, , ParameterDirection.Output)

        ' delete parameters
        params.AddDeleteParam("ID", DbtType.dbtInteger)
    End Sub

    Public Function SaveSupplier(ByVal TableID As Integer, _
                                    ByRef dt As DataTable) As Boolean
        Try
            Dim sSelect As String
            Dim sUpdate As String
            Dim sInsert As String
            Dim sDelete As String
            GetStoredProcedures(TableID, sSelect, sUpdate, sInsert, sDelete)

            Dim params As New libDataAccess.libDataAccess.UpdateParameterList

            ' update parameters
            params.AddUpdateParam("ID", DbtType.dbtInteger)
            params.AddUpdateParam("Name", DbtType.dbtString)
            params.AddUpdateParam("Details", DbtType.dbtString)

            ' insert parameters
            params.AddInsertParam("Name", DbtType.dbtString)
            params.AddInsertParam("Details", DbtType.dbtString)
            params.AddInsertParam("ID", DbtType.dbtInteger, , ParameterDirection.Output)

            ' delete parameters
            params.AddDeleteParam("ID", DbtType.dbtInteger)

            TBCultureDA.UpdateDataTable("", sInsert, sUpdate, sDelete, _
                                        CommandType.StoredProcedure, _
                                        dt, params)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Return False
        End Try
    End Function

#End Region


End Class

