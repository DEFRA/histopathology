Imports libDataAccess.libDataAccess
Imports libDataAccess.libDataAccess.TBCultureDA
Imports System.Data.SqlClient

Public Class QCNoteUpdateException : Inherits ApplicationException

    Public Sub New(ByVal message As String)
        MyBase.New(message)
    End Sub

    Public Sub New(ByVal message As String, ByVal inner As Exception)
        MyBase.New(message, inner)
    End Sub

End Class

Public Class clsQCNote

#Region "Table Handling"

    Public Function CreateQCNoteTable(ByRef dtQCNotes As DataTable) As Boolean
        Try
            dtQCNotes.Columns.Add("ID", System.Type.GetType("System.Int32"))
            dtQCNotes.Columns.Add("CreatedBy", System.Type.GetType("System.Int32"))
            dtQCNotes.Columns.Add("DateCreated", System.Type.GetType("System.String"))
            dtQCNotes.Columns.Add("NewID", System.Type.GetType("System.Int32"))
            SetPrimaryKey(dtQCNotes, "ID", True)

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsQCNoteObject)
            Return False
        End Try
    End Function

    Public Function NewQCNote(ByRef dtQCNotes As DataTable, ByVal sCreatedBy As String, ByRef iNewID As Integer) As Boolean
        Try
            Dim drNewRow As DataRow
            Dim now As Date

            If Not dtQCNotes Is Nothing Then
                drNewRow = dtQCNotes.NewRow()
                drNewRow("CreatedBy") = sCreatedBy
                drNewRow("DateCreated") = now.Now.ToShortDateString
                dtQCNotes.Rows.Add(drNewRow)
                iNewID = drNewRow("ID")
            Else
                iNewID = 0
            End If

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsQCNoteObject)
            Return False
        End Try
    End Function

#End Region

#Region "Update Functions"

    Public Function UpdateQCNote(ByVal iQCNote As Integer, _
                                ByVal sText As String, _
                                ByVal aRowStamp As System.Array, _
                                ByVal iUserID As Integer, _
                                ByRef objErrorList As ArrayList)

        Dim objParamList As New ParameterList

        Try
            objParamList.AddParameter("RETURN_VALUE", DbtType.dbtInteger, "RETURN_VALUE", daDirection:=ParameterDirection.ReturnValue)
            objParamList.QuickAddInputParam("QCNoteRef", DbtType.dbtInteger, iQCNote)
            objParamList.QuickAddInputParam("QCText", DbtType.dbtString, sText)
            objParamList.QuickAddInputParam("RowStamp", DbtType.dbtBinary, aRowStamp)
            objParamList.QuickAddInputParam("UserID", DbtType.dbtInteger, iUserID)

            ExecuteNonQuery("EditQCNote", _
                            CommandType.StoredProcedure, _
                            objParamList)

            Dim iReturnValue As Integer = CInt(objParamList("RETURN_VALUE").Value)
            Select Case iReturnValue
                Case 1
                    Throw New Exception("Another user has altered the QC Note record.")
            End Select

            Return True
        Catch exSP As StoredProcException
            objErrorList.Add(exSP.Message)
            clsLog.LogException(exSP, clsLog.LogSource.lsStoredProcedure)
            Return False
        Catch ex As Exception
            objErrorList.Add(ex.Message)
            clsLog.LogException(ex, clsLog.LogSource.lsQCNoteObject)
            Return False
        End Try
    End Function

    Public Function UpdateQCNOtes(ByRef dtQCNoteIDs As DataTable, _
                                ByRef dbConn As Object, _
                                ByRef dbTran As Object)

        Dim drDataRow As DataRow
        For Each drDataRow In dtQCNoteIDs.Rows
            UpdateQCNoteRow(drDataRow, dbConn, dbTran)
        Next
    End Function

    Private Sub UpdateQCNoteRow(ByRef drQCRow As DataRow, _
                               ByRef objDBConn As SqlConnection, _
                               ByRef objDBTran As SqlTransaction)

        If drQCRow.RowState <> DataRowState.Added Then
            Exit Sub
        End If

        Dim objQCParamList As New libDataAccess.libDataAccess.ParameterList

        With objQCParamList
            If drQCRow.RowState = DataRowState.Added Then
                .AddParameter("NewID", DbtType.dbtInteger, "@NewID", , , , ParameterDirection.Output)
                .QuickAddInputParam("DateCreated", DbtType.dbtDateTime, drQCRow.Item("DateCreated"))
                .QuickAddInputParam("CreatedBy", DbtType.dbtInteger, drQCRow.Item("CreatedBy"))
            End If
        End With

        If drQCRow.RowState = DataRowState.Added Then
            Try
                TBCultureDA.ExecuteNonQuery(objDBConn, objDBTran, "AddQCNote", CommandType.StoredProcedure, objQCParamList)
                drQCRow("NewID") = Convert.ToInt32(objQCParamList("NewID").Value)
            Catch ex As Exception
                clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
                Throw New QCNoteUpdateException(ex.Message, ex.InnerException)
            End Try

            'ElseIf drQCRow.RowState = DataRowState.Modified Then
            '    Try
            '        TBCultureDA.ExecuteNonQuery(objDBConn, objDBTran, "EditQCNote", CommandType.StoredProcedure, objQCParamList)
            '    Catch ex As Exception
            '        clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            '        Throw New QCNoteUpdateException(ex.Message, ex.InnerException)
            '    End Try
        End If

    End Sub

#End Region

#Region "Get Functions"

    Public Function GetAllQCNotes(ByRef dtData As DataTable) As Boolean
        Try
            FillDataTable("GetAllQCNotes", _
                            CommandType.StoredProcedure, _
                            dtData)
            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsQCNoteObject)
            Return False
        End Try
    End Function
    Public Function GetBatchQCNotes(ByRef dtData As DataTable, Optional ByVal QCNoteRef As Integer = Nothing) As Boolean
        Try
            Dim objInParamList As New ParameterList()

            If QCNoteRef = Nothing Then
                objInParamList.QuickAddInputParam("QCNoteRef", DbtType.dbtInteger, DBNull.Value)
            Else
                objInParamList.QuickAddInputParam("QCNoteRef", DbtType.dbtInteger, QCNoteRef)
            End If

            FillDataTable("GetBatchQCNotes", _
                         CommandType.StoredProcedure, _
                         dtData, _
                         objInParamList)
            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsQCNoteObject)
            Return False
        End Try
    End Function

    Public Function GetQCNoteTestInformation(ByRef dtData As DataTable, ByVal QCNoteRef As Integer, ByVal iSubmissionType As Integer) As Boolean
        Try
            Dim objInParamList As New ParameterList()
            Dim dtAntiBodiesQCInfo As New DataTable()
            Dim dr As DataRow

            objInParamList.QuickAddInputParam("QCNoteRef", DbtType.dbtInteger, QCNoteRef)

            FillDataTable("GetQCNoteHistStainTestInformation", _
                         CommandType.StoredProcedure, _
                         dtData, _
                         objInParamList)

            objInParamList.QuickAddInputParam("SubmissionType", DbtType.dbtInteger, iSubmissionType)

            FillDataTable("GetQCNoteAntibodiesInformation", _
                        CommandType.StoredProcedure, _
                        dtAntiBodiesQCInfo, _
                        objInParamList)

            For Each dr In dtAntiBodiesQCInfo.Rows
                dtData.ImportRow(dr)
            Next

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsQCNoteObject)
            Return False
        End Try
    End Function

#End Region

#Region "Generate Repoort"

    Public Function CreateReportDataset(ByVal iQCNoteRef As Integer, ByRef dsDataSet As DataSet) As Boolean
        Try
            Dim dtHeaderData As New DataTable()
            Dim ContentData As New DataTable()
            Dim dtReportTable As New DataTable("Header")
            Dim drNewRow As DataRow
            Dim iSubmissionType As Integer = 0
            Dim sSubmittedArea

            dtReportTable.Columns.Add("QCNoteRef", System.Type.GetType("System.String"))
            dtReportTable.Columns.Add("SubmissionNumber", System.Type.GetType("System.String"))
            dtReportTable.Columns.Add("Project", System.Type.GetType("System.String"))
            dtReportTable.Columns.Add("Species", System.Type.GetType("System.String"))
            dtReportTable.Columns.Add("StainRef", System.Type.GetType("System.String"))
            dtReportTable.Columns.Add("QCText", System.Type.GetType("System.String"))
            dtReportTable.Columns.Add("CreatedBy", System.Type.GetType("System.String"))
            dtReportTable.Columns.Add("DateCreated", System.Type.GetType("System.String"))

            If Not GetBatchQCNotes(dtHeaderData, iQCNoteRef) Then
                Throw New Exception("QCNote.GetBatchQCNotes returned false.")
            End If

            drNewRow = dtReportTable.NewRow()
            If Not dtHeaderData Is Nothing And dtHeaderData.Rows.Count > 0 Then
                drNewRow("QCNoteRef") = dtHeaderData.Rows(0)("QCNoteRef")
                drNewRow("SubmissionNumber") = dtHeaderData.Rows(0)("ID")
                drNewRow("StainRef") = dtHeaderData.Rows(0)("StainRef").ToString()
                drNewRow("Species") = dtHeaderData.Rows(0)("Species").ToString()
                iSubmissionType = dtHeaderData.Rows(0)("BatchType")
                sSubmittedArea = dtHeaderData.Rows(0)("SubmittedArea").ToString()
                drNewRow("Project") = GetListTypeID(dtHeaderData.Rows(0)("ProjectContractCode").ToString(), 19) 'Projects lookup

                If Not GetQCNoteTestInformation(ContentData, iQCNoteRef, iSubmissionType) Then
                    Throw New Exception("QCNote.GetQCNoteTestInformation returned false.")
                End If

                If Not ContentData Is Nothing Then
                    If ContentData.Rows.Count > 0 Then
                        drNewRow("QCText") = ContentData.Rows(0)("QCText").ToString()
                        drNewRow("CreatedBy") = ContentData.Rows(0)("Name").ToString()
                        drNewRow("DateCreated") = Format(CDate(ContentData.Rows(0)("DateCreated").ToString()), "Long Date")
                    End If
                End If
            End If

            dtReportTable.Rows.Add(drNewRow)
            dsDataSet.Tables.Add(dtReportTable)

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsQCNoteObject)
            Return False
        End Try

    End Function

#End Region


#Region "Private Functions"

    Public Function GetListTypeID(ByVal sCode As String, ByVal lookuplist As Integer) As String
        Dim dt As DataTable = GetLookupTypeList(lookuplist)

        If Not dt Is Nothing Then
            Dim dv As New DataView(dt, "", "ID", DataViewRowState.CurrentRows)
            Dim iRow As Integer = dv.Find(sCode)
            If iRow >= 0 Then
                Return dv(iRow).Item("Description").ToString()
            Else
                Return ""
            End If
        Else
            Return ""
        End If
    End Function

    Public Function GetLookupTypeList(ByVal lookuplist As Integer) As DataTable
        Dim objLookup As New HistopathologyLib.LookupData()
        Dim dt As DataTable = objLookup.GetLookupData(lookuplist, True)

        If dt Is Nothing Then
            Throw New Exception("LookupData.GetLookupData returned Nothing")
        End If

        Return dt
    End Function

#End Region



End Class
