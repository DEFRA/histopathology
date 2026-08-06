Imports libDataAccess.libDataAccess
Imports libDataAccess.libDataAccess.TBCultureDA

Public Class clsUser

    Public Function GetUsers() As DataTable

        Dim dtData As New DataTable()

        Try
            FillDataTable("GetUsers", _
                          CommandType.StoredProcedure, _
                          dtData)

            SetPrimaryKey(dtData, "ID", True)

            Return dtData
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Return Nothing
        End Try

    End Function

    Public Function GetUsersByArea(ByVal sUserArea As String) As DataTable

        Try
            Dim dtData As New DataTable()
            Dim ParamsIn As New ParameterList()

            ParamsIn.QuickAddInputParam("UserArea", DbtType.dbtString, sUserArea)

            FillDataTable("GetUsersByUserArea", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          ParamsIn)

            SetPrimaryKey(dtData, "ID", True)

            Return dtData
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Return Nothing
        End Try
    End Function
    Public Function GetGroupForUser(ByVal sNTUserID As String, ByRef GroupCode As Long, ByRef GroupName As String) As Boolean
        Try
            Dim ParamsIn As New ParameterList()
            Dim ParamsOut As New ParameterList()

            ParamsIn.QuickAddInputParam("NTUserID", DbtType.dbtString, sNTUserID)

            ParamsOut.QuickAddResultsetParam("UserGroup", DbtType.dbtInteger)
            ParamsOut.QuickAddResultsetParam("Name", DbtType.dbtString)

            ExecuteQuery("GetGroupForUser", _
                         CommandType.StoredProcedure, _
                         ParamsOut, _
                         ParamsIn)

            GroupCode = Convert.ToInt32(ParamsOut.Item("UserGroup").Value)
            GroupName = Convert.ToString(ParamsOut.Item("Name").Value)

            Return True

        Catch ex As Exception
            'clsLog.LogException(ex, clsLog.LogSource.lsUserObject)
            Return False
        End Try
    End Function

    Public Function GetUserByNTLogin(ByVal NTLogin As String, _
                                     ByRef UserID As Integer, _
                                     ByRef Name As String, _
                                     ByRef GroupCode As Integer, _
                                     ByRef GroupName As String, _
                                     ByRef Email As String, _
                                     ByRef AreaCode As Integer, _
                                     ByRef AreaName As String, _
                                     ByRef Active As Boolean) As Boolean
        Try
            Dim ParamsIn As New ParameterList()
            Dim ParamsOut As New ParameterList()

            ParamsIn.QuickAddInputParam("NTLogin", DbtType.dbtString, NTLogin)

            ParamsOut.QuickAddResultsetParam("ID", DbtType.dbtInteger)
            ParamsOut.QuickAddResultsetParam("Name", DbtType.dbtString)
            ParamsOut.QuickAddResultsetParam("UserGroup", DbtType.dbtString)
            ParamsOut.QuickAddResultsetParam("GroupName", DbtType.dbtString)
            ParamsOut.QuickAddResultsetParam("Email", DbtType.dbtString)
            ParamsOut.QuickAddResultsetParam("UserArea", DbtType.dbtString)
            ParamsOut.QuickAddResultsetParam("AreaName", DbtType.dbtString)
            ParamsOut.QuickAddResultsetParam("Active", DbtType.dbtBoolean)


            ExecuteQuery("GetUserByNTLogin", _
                         CommandType.StoredProcedure, _
                         ParamsOut, _
                         ParamsIn)

            UserID = CInt(ParamsOut.Item("ID").Value)
            Name = Convert.ToString(ParamsOut.Item("Name").Value)
            GroupCode = Convert.ToInt32(ParamsOut.Item("UserGroup").Value)
            GroupName = Convert.ToString(ParamsOut.Item("GroupName").Value)
            Email = Convert.ToString(ParamsOut.Item("Email").Value)
            AreaCode = Convert.ToInt32(ParamsOut.Item("UserArea").Value)
            AreaName = Convert.ToString(ParamsOut.Item("AreaName").Value)
            Active = Convert.ToString(ParamsOut.Item("Active").Value)

            If Not Active Then
                Return False
            End If

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsUserObject)
            Return False
        End Try
    End Function

    Public Function SaveUserData(ByRef dt As DataTable, ByVal iUserID As Integer) As Boolean
        Try
            Dim sUpdate As String = "EditUser"
            Dim sInsert As String = "AddUser"
            Dim sDelete As String = "DeleteUser"

            Dim params As New UpdateParameterList()

            AddUserID(dt, iUserID)

            'update params
            params.AddUpdateParam("ID", DbtType.dbtInteger)
            params.AddUpdateParam("NTLogin", DbtType.dbtString)
            params.AddUpdateParam("Name", DbtType.dbtString)
            params.AddUpdateParam("Email", DbtType.dbtString)
            params.AddUpdateParam("UserGroup", DbtType.dbtString)
            params.AddUpdateParam("UserArea", DbtType.dbtString)
            params.AddUpdateParam("Active", DbtType.dbtBoolean)
            params.AddUpdateParam("UserID", DbtType.dbtInteger)

            'insert params
            params.AddInsertParam("NTLogin", DbtType.dbtString)
            params.AddInsertParam("Name", DbtType.dbtString)
            params.AddInsertParam("Email", DbtType.dbtString)
            params.AddInsertParam("UserGroup", DbtType.dbtString)
            params.AddInsertParam("UserArea", DbtType.dbtString)
            params.AddInsertParam("Active", DbtType.dbtBoolean)

            'delete params
            params.AddDeleteParam("ID", DbtType.dbtInteger)

            UpdateDataTable("", _
                            sInsert, _
                            sUpdate, _
                            sDelete, _
                            CommandType.StoredProcedure, _
                            dt, _
                            params)
            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsUserObject)
            Return False
        End Try
    End Function

    Public Function GetUserByID(ByVal iID As Integer, ByRef sUserName As String, ByRef sUserArea As String, ByRef sUserGroup As String, ByRef sUserAreaID As String) As Boolean
        Try
            Dim ParamsIn As New ParameterList()
            Dim ParamsOut As New ParameterList()

            ParamsIn.QuickAddInputParam("ID", DbtType.dbtInteger, iID)

            ParamsOut.QuickAddResultsetParam("ID", DbtType.dbtInteger)
            ParamsOut.QuickAddResultsetParam("Name", DbtType.dbtString)
            ParamsOut.QuickAddResultsetParam("UserGroup", DbtType.dbtString)
            ParamsOut.QuickAddResultsetParam("GroupName", DbtType.dbtString)
            ParamsOut.QuickAddResultsetParam("Email", DbtType.dbtString)
            ParamsOut.QuickAddResultsetParam("UserArea", DbtType.dbtString)
            ParamsOut.QuickAddResultsetParam("AreaName", DbtType.dbtString)

            ExecuteQuery("GetUserByID", _
                         CommandType.StoredProcedure, _
                         ParamsOut, _
                         ParamsIn)

            sUserName = Convert.ToString(ParamsOut.Item("Name").Value)
            sUserGroup = Convert.ToString(ParamsOut.Item("GroupName").Value)
            sUserArea = Convert.ToString(ParamsOut.Item("AreaName").Value)
            sUserAreaID = Convert.ToString(ParamsOut.Item("UserArea").Value)
            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsUserObject)
            Return False
        End Try
    End Function

#Region "Private Functions"

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
End Class
