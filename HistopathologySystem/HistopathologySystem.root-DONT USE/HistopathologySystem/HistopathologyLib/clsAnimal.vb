Imports libDataAccess.libDataAccess
Imports libDataAccess.libDataAccess.TBCultureDA
Imports System.Data.SqlClient

Public Class AnimalUpdateException : Inherits ApplicationException

    Public Sub New(ByVal message As String)
        MyBase.New(message)
    End Sub

    Public Sub New(ByVal message As String, ByVal inner As Exception)
        MyBase.New(message, inner)
    End Sub

End Class

Public Class clsAnimal

#Region "Handle Data Table"

    Public Function NewRecord(ByRef dtAnimal As DataTable, _
                              ByVal sSenderRef As String, _
                              ByRef iAnimalID As Integer, _
                              ByVal sPMDate As String, _
                              ByVal bSetInDatabase As Boolean, _
                              ByVal bNeuropath As Boolean) As Boolean
        Try
            Dim drNewRow As DataRow
            drNewRow = dtAnimal.NewRow()
            iAnimalID = drNewRow("ID")
            drNewRow("SenderRef") = FormatEmptyString(sSenderRef)
            drNewRow("NextBlockRef") = "01"
            drNewRow("RowStamp") = System.DBNull.Value
            drNewRow("RowState") = DataRowState.Added
            drNewRow("HistoRefSet") = False
            drNewRow("HistologyRef") = System.DBNull.Value
            drNewRow("OnHold") = False
            drNewRow("PMDate") = FormatEmptyString(sPMDate)
            drNewRow("PMDateSet") = bSetInDatabase
            drNewRow("IsPGNumber") = False
            drNewRow("BookedHistologyRef") = False

            'Auto reverse if the sender ref is PG Number and user area is neuropath
            If bNeuropath Then
                If sSenderRef.Length > 2 Then
                    Dim sPGPart As String
                    sPGPart = Left(sSenderRef, 2)
                    If sPGPart = "PG" Or sPGPart = "Pg" Or sPGPart = "pG" Or sPGPart = "pg" Then

                        Dim strYear As String
                        Dim strID As String
                        Dim strSender As String = sSenderRef

                        strSender = strSender.Substring(2)
                        strID = Left$(strSender, 4)
                        strYear = Right$(strSender, 2)

                        'only reverse the PG number automatically if the year > 01
                        If IsAfter01(strYear) Then
                            drNewRow("HistologyRef") = strYear + "/" + "0" + strID
                        End If
                    End If
                End If
            End If
            dtAnimal.Rows.Add(drNewRow)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function

    Public Function NewRecord(ByRef dtAnimal As DataTable, _
                              ByVal sSenderRef As String, _
                              ByRef iAnimalID As Integer, _
                              ByVal bNeuropath As Boolean) As Boolean
        Try
            Dim drNewRow As DataRow
            drNewRow = dtAnimal.NewRow()
            iAnimalID = drNewRow("ID")
            drNewRow("SenderRef") = FormatEmptyString(sSenderRef)
            drNewRow("NextBlockRef") = "01"
            drNewRow("RowStamp") = System.DBNull.Value
            drNewRow("RowState") = DataRowState.Added
            drNewRow("HistoRefSet") = False
            drNewRow("OnHold") = False
            drNewRow("PMDateSet") = False
            drNewRow("HistologyRef") = System.DBNull.Value
            drNewRow("IsPGNumber") = False
            drNewRow("BookedHistologyRef") = False

            'Auto reverse if the sender ref is PG Number and user area is neuropath
            If bNeuropath Then
                If sSenderRef.Length > 2 Then
                    Dim sPGPart As String
                    sPGPart = Left(sSenderRef, 2)
                    If sPGPart = "PG" Or sPGPart = "Pg" Or sPGPart = "pG" Or sPGPart = "pg" Then

                        Dim strYear As String
                        Dim strID As String
                        Dim strSender As String = sSenderRef

                        strSender = strSender.Substring(2)
                        strID = Left$(strSender, 4)
                        strYear = Right$(strSender, 2)

                        'only reverse the PG number automatically if the year > 01
                        If IsAfter01(strYear) Then
                            drNewRow("HistologyRef") = strYear + "/" + "0" + strID
                        End If
                    End If
                End If
            End If
            dtAnimal.Rows.Add(drNewRow)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function

    Public Function NewExistingRecord(ByRef dtAnimal As DataTable, _
                                      ByVal sSenderRef As String, _
                                      ByVal sHistologyRef As String, _
                                      ByVal sNextBlockRef As String, _
                                      ByVal aRowStamp As System.Array, _
                                      ByRef iAnimalID As Integer, _
                                      ByVal bHistoSetInDatabase As Boolean, _
                                      ByVal bOnHold As String, _
                                      ByVal sPMDate As String, _
                                      ByVal bPMDateSetInDatabase As Boolean, _
                                      Optional ByVal bNeuropath As Boolean = False) As Boolean
        Try
            Dim drNewRow As DataRow
            drNewRow = dtAnimal.NewRow()
            drNewRow("ID") = iAnimalID
            drNewRow("SenderRef") = FormatEmptyString(sSenderRef)
            drNewRow("HistologyRef") = FormatEmptyString(sHistologyRef)
            drNewRow("NextBlockRef") = FormatEmptyString(sNextBlockRef)
            drNewRow("RowStamp") = aRowStamp
            drNewRow("RowState") = DataRowState.Modified
            drNewRow("HistoRefSet") = bHistoSetInDatabase
            drNewRow("OnHold") = bOnHold
            drNewRow("PMDate") = FormatEmptyString(sPMDate)
            drNewRow("PMDateSet") = bPMDateSetInDatabase
            drNewRow("IsPGNumber") = False
            drNewRow("BookedHistologyRef") = False

            'Auto reverse if the sender ref is PG Number and user area is neuropath
            If bNeuropath And sHistologyRef.Trim = "" Then
                If sSenderRef.Length > 2 Then
                    Dim sPGPart As String
                    sPGPart = Left(sSenderRef, 2)
                    If sPGPart = "PG" Or sPGPart = "Pg" Or sPGPart = "pG" Or sPGPart = "pg" Then

                        Dim strYear As String
                        Dim strID As String
                        Dim strSender As String = sSenderRef

                        strSender = strSender.Substring(2)
                        strID = Left$(strSender, 4)
                        strYear = Right$(strSender, 2)

                        'only reverse the PG number automatically if the year > 01
                        If IsAfter01(strYear) Then
                            drNewRow("HistologyRef") = strYear + "/" + "0" + strID
                        End If
                    End If
                End If
            End If

            dtAnimal.Rows.Add(drNewRow)
            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function

#End Region

#Region "Get Animal Data"

    Public Function GetAnimalBySender(ByVal sSenderRef As String, ByRef dtData As DataTable) As Boolean

        Dim objInParamList As New ParameterList()

        sSenderRef.Replace("'", "")

        Try
            objInParamList.QuickAddInputParam("SenderRef", DbtType.dbtString, sSenderRef)

            FillDataTable("GetAnimalBySender", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try

    End Function

    Public Function GetAnimalByHistologyRef(ByVal sHistologyRef As String, ByRef dtData As DataTable) As Boolean

        Dim objInParamList As New ParameterList

        Try
            objInParamList.QuickAddInputParam("HistologyRef", DbtType.dbtString, sHistologyRef)

            FillDataTable("GetAnimalByHistologyRef", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try

    End Function

    Public Function GetAnimalsBySenderRef(ByVal sSenderRef As String, ByRef dtData As DataTable) As Boolean

        Dim objInParamList As New ParameterList

        sSenderRef.Replace("'", "")

        Try
            objInParamList.QuickAddInputParam("SenderRef", DbtType.dbtString, sSenderRef)

            FillDataTable("GetAnimalsBySenderRef", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)
            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function

    Public Function GetAnimalData(ByVal dtAnimal As DataTable, _
                                  ByVal iAnimalID As Integer, _
                                  ByRef sHistologyRef As String, _
                                  ByRef sSenderRef As String, _
                                  ByRef sNextBlockRef As String, _
                                  ByRef aRowStamp As System.Array, _
                                  ByRef bHistologyInDatabase As Boolean, _
                                  ByRef sPMDate As String, _
                                  ByRef bPMDateInDataBase As Boolean, _
                                  ByRef bHistologyRefLinked As Boolean, _
                                  Optional ByVal dtPreBooked As DataTable = Nothing)
        Try
            Dim foundRows As DataRow()
            Dim sFilter As String
            Dim bPreBooked As Boolean = False
            Dim iBlock As Integer

            sFilter = "ID=" & iAnimalID

            foundRows = dtAnimal.Select(sFilter)
            sHistologyRef = foundRows(0)("HistologyRef").ToString()
            sSenderRef = foundRows(0)("SenderRef").ToString()
            sPMDate = foundRows(0)("PMDate").ToString()
            If Not foundRows(0)("RowStamp") Is System.DBNull.Value Then
                aRowStamp = foundRows(0)("RowStamp")
            End If
            bHistologyInDatabase = foundRows(0)("HistoRefSet")
            bPMDateInDataBase = foundRows(0)("PMDateSet")
            sNextBlockRef = foundRows(0)("NextBlockRef").ToString()
            bHistologyRefLinked = foundRows(0)("BookedHistologyRef")
            If Not dtPreBooked Is Nothing Then
                bPreBooked = CheckBlockIsPreBooked(iAnimalID, sNextBlockRef, dtPreBooked)
                iBlock = CInt(foundRows(0)("NextBlockRef"))

                While bPreBooked
                    iBlock += 1
                    If iBlock < 10 Then
                        sNextBlockRef = "0" & Convert.ToString(iBlock)
                    Else
                        sNextBlockRef = Convert.ToString(iBlock)
                    End If
                    bPreBooked = CheckBlockIsPreBooked(iAnimalID, sNextBlockRef, dtPreBooked)
                End While
                foundRows(0)("NextBlockRef") = sNextBlockRef
            End If

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function

    Public Function GetAnimalsForBatch(ByVal iBatchID As Integer, ByRef dtData As DataTable)
        Dim objInParamList As New ParameterList

        Try
            Dim dr As DataRow

            objInParamList.QuickAddInputParam("ID", DbtType.dbtInteger, iBatchID)

            FillDataTable("GetBatchAnimal", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            dtData.Columns.Add("HistoRefSet", System.Type.GetType("System.Boolean"))
            dtData.Columns.Add("PMDateSet", System.Type.GetType("System.Boolean"))

            For Each dr In dtData.Rows
                If Not IsDBNull(dr("HistologyRef")) And Not dr("HistologyRef").ToString() = "" Then
                    dr("HistoRefSet") = True
                Else
                    dr("HistoRefSet") = False
                End If

                If Not IsDBNull(dr("PMDate")) And Not dr("PMDate").ToString() = "" Then
                    dr("PMDateSet") = True
                Else
                    dr("PMDateSet") = False
                End If
            Next
            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function

    Public Function GetSearchPMDates(ByRef dtData As DataTable, ByVal dFromDate As String, ByVal dToDate As String) As Boolean
        Dim objInParamList As New ParameterList

        Try
            objInParamList.QuickAddInputParam("FromDate", DbtType.dbtDateTime, FormatEmptyString(dFromDate))
            objInParamList.QuickAddInputParam("ToDate", DbtType.dbtDateTime, FormatEmptyString(dToDate))

            FillDataTable("GetSearchPMDates", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)
            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function

    Public Function GetAnimalNextBlock(ByRef dtAnimal As DataTable, ByVal iAnimalID As Integer, ByRef sNextBlockRef As String, Optional ByVal dtPreBooked As DataTable = Nothing) As Boolean
        Try
            Dim drFoundRow As DataRow()
            Dim sFilter As String

            sFilter = "ID=" & iAnimalID
            drFoundRow = dtAnimal.Select(sFilter)

            sNextBlockRef = drFoundRow(0)("NextBlockRef").ToString()

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function

    Public Function GetAnimalFromDayBook(ByVal sSenderRef As String, ByRef sSpecies As String, ByRef sProjects As String, ByRef sPMDate As String, ByRef sSpeciesDescription As String) As Boolean
        Dim objInParamList As New ParameterList
        Dim objOutParamsList As New ParameterList
        Dim dtData As New DataTable

        sSenderRef.Replace("'", "")

        Try
            objInParamList.QuickAddInputParam("SenderRef", DbtType.dbtAnsiString, sSenderRef)

            objOutParamsList.QuickAddResultsetParam("SpeciesID", DbtType.dbtInteger)
            objOutParamsList.QuickAddResultsetParam("ProjectCodesDescription", DbtType.dbtString)
            objOutParamsList.QuickAddResultsetParam("PMDate", DbtType.dbtString)
            objOutParamsList.QuickAddResultsetParam("Species", DbtType.dbtString)

            ExecuteQuery("GetAnimalFromDayBook", _
                         CommandType.StoredProcedure, _
                         objOutParamsList, _
                         objInParamList)

            sSpecies = Convert.ToString(objOutParamsList.Item("SpeciesID").Value)
            sProjects = Convert.ToString(objOutParamsList.Item("ProjectCodesDescription").Value)
            sPMDate = Convert.ToString(objOutParamsList.Item("PMDate").Value)
            sSpeciesDescription = Convert.ToString(objOutParamsList.Item("Species").Value)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function

    Public Function GetAnimalTissues(ByRef dtData As DataTable, ByRef sSenderRef As String, ByRef sHistologyRef As String, ByVal sTissueCode As String, ByVal sProjectDesc As String) As Boolean
        Dim objInParamList As New ParameterList
        Dim dr As DataRow

        Try
            objInParamList.QuickAddInputParam("SenderRef", DbtType.dbtString, FormatEmptyString(sSenderRef))
            objInParamList.QuickAddInputParam("HistologyRef", DbtType.dbtString, FormatEmptyString(sHistologyRef))
            objInParamList.QuickAddInputParam("TissueCode", DbtType.dbtString, FormatEmptyString(sTissueCode))
            objInParamList.QuickAddInputParam("ProjectDesc", DbtType.dbtString, FormatEmptyString(sProjectDesc))

            FillDataTable("GetAnimalBatchTissues", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            If dtData.Rows.Count > 0 Then
                sSenderRef = dtData.Rows(0)("SenderRef").ToString
                sHistologyRef = dtData.Rows(0)("HistologyRef").ToString
            End If

            dtData.DefaultView.Sort = "ID"

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function

    Public Function GetAnimalBlockTissues(ByRef dtData As DataTable, ByRef sSenderRef As String, ByRef sHistologyRef As String, ByVal sTissueCode As String, ByVal sProjectDesc As String) As Boolean
        Dim objInParamList As New ParameterList
        Dim dr As DataRow

        Try
            objInParamList.QuickAddInputParam("SenderRef", DbtType.dbtString, FormatEmptyString(sSenderRef))
            objInParamList.QuickAddInputParam("HistologyRef", DbtType.dbtString, FormatEmptyString(sHistologyRef))
            objInParamList.QuickAddInputParam("TissueCode", DbtType.dbtString, FormatEmptyString(sTissueCode))
            objInParamList.QuickAddInputParam("ProjectDesc", DbtType.dbtString, FormatEmptyString(sProjectDesc))

            FillDataTable("GetAnimalBlockTissues", _
                           CommandType.StoredProcedure, _
                           dtData, _
                           objInParamList)

            If dtData.Rows.Count > 0 Then
                sSenderRef = dtData.Rows(0)("SenderRef").ToString
                sHistologyRef = dtData.Rows(0)("HistologyRef").ToString
            End If

            dtData.DefaultView.Sort = "ID"

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function

    Public Function GetAnimalBlocksForBlockRefSearch(ByRef dtData As DataTable, ByVal sHistologyRef As String)

        Dim objInParamList As New ParameterList

        Try
            objInParamList.QuickAddInputParam("HistologyRef", DbtType.dbtString, sHistologyRef)

            FillDataTable("GetBlocksForHistoRef", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            ' If there are no rows that data may have been imported from access database but has not been linked
            ' to any blocks. Check this.
            If dtData.Rows.Count = 0 Then
                Dim dtAnimal As DataTable
                Dim drNewRow As DataRow
                Dim iBlockRef As Integer

                FillDataTable("GetAnimalByHistologyRef", _
                              CommandType.StoredProcedure, _
                              dtAnimal, _
                              objInParamList)

                If dtAnimal.Rows.Count > 0 Then
                    drNewRow = dtData.NewRow
                    drNewRow("Status") = 0
                    iBlockRef = dtAnimal.Rows(0)("NextBlockRef")
                    iBlockRef = iBlockRef - 1
                    If iBlockRef < 10 Then
                        drNewRow("BlockRef") = "0" & Convert.ToString(iBlockRef)
                    Else
                        drNewRow("BlockRef") = Convert.ToString(iBlockRef)
                    End If
                    dtData.Rows.Add(drNewRow)
                End If
            End If

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try

    End Function

    Public Function GetAnimalBlocks(ByRef dtData As DataTable, ByVal sHistologyRef As String)

        Dim objInParamList As New ParameterList

        Try
            objInParamList.QuickAddInputParam("HistologyRef", DbtType.dbtString, sHistologyRef)

            FillDataTable("GetBlocksForHistoRef", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try

    End Function

    Public Function GetAnimalBlocksBySenderRef(ByRef dtData As DataTable, ByVal sSenderRef As String) As Boolean

        Dim objInParamList As New ParameterList

        Try
            objInParamList.QuickAddInputParam("SenderRef", DbtType.dbtString, sSenderRef)

            FillDataTable("GetBlocksForSenderRef", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)
            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try

    End Function

    Public Function GetAnimalBlocksBySenderRefForBlockRefSearch(ByRef dtData As DataTable, ByVal sSenderRef As String) As Boolean

        Dim objInParamList As New ParameterList

        Try
            objInParamList.QuickAddInputParam("SenderRef", DbtType.dbtString, sSenderRef)

            FillDataTable("GetBlocksForSenderRef", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            ' If there are no rows that data may have been imported from access database but has not been linked
            ' to any blocks. Check this.
            If dtData.Rows.Count = 0 Then
                Dim dtAnimal As DataTable
                Dim drNewRow As DataRow
                Dim iBlockRef As Integer

                FillDataTable("GetAnimalBySender", _
                              CommandType.StoredProcedure, _
                              dtAnimal, _
                              objInParamList)

                If dtAnimal.Rows.Count > 0 Then
                    drNewRow = dtData.NewRow
                    drNewRow("Status") = 0
                    iBlockRef = dtAnimal.Rows(0)("NextBlockRef")
                    iBlockRef = iBlockRef - 1
                    If iBlockRef < 10 Then
                        drNewRow("BlockRef") = "0" & Convert.ToString(iBlockRef)
                    Else
                        drNewRow("BlockRef") = Convert.ToString(iBlockRef)
                    End If
                    dtData.Rows.Add(drNewRow)
                End If
            End If

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try

    End Function

    Public Function GetAnimalBlockArchiveInformation(ByRef dtData As DataTable, _
                                                     ByVal sSenderRef As String, _
                                                     ByVal sHistologyRef As String, _
                                                     ByVal sBlockRef As String, _
                                                     ByVal sArchiveLocation As String) As Boolean
        Dim objInParamList As New ParameterList

        Try
            objInParamList.QuickAddInputParam("SenderRef", DbtType.dbtString, FormatEmptyString(sSenderRef))
            objInParamList.QuickAddInputParam("HistologyRef", DbtType.dbtString, FormatEmptyString(sHistologyRef))
            objInParamList.QuickAddInputParam("BlockRef", DbtType.dbtString, FormatEmptyString(sBlockRef))
            objInParamList.QuickAddInputParam("ArchiveLocation", DbtType.dbtString, FormatEmptyString(sArchiveLocation))

            FillDataTable("GetAnimalBlockArchiveInformation", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function



    Public Function GetAnimalTissuesArchiveInformation(ByRef dtData As DataTable, _
                                                       ByVal sSenderRef As String, _
                                                       ByVal sHistologyRef As String, _
                                                       ByVal sArchiveLocation As String, _
                                                       ByVal sTissueCode As String)
        Dim objInParamList As New ParameterList

        Try
            objInParamList.QuickAddInputParam("SenderRef", DbtType.dbtString, FormatEmptyString(sSenderRef))
            objInParamList.QuickAddInputParam("HistologyRef", DbtType.dbtString, FormatEmptyString(sHistologyRef))
            objInParamList.QuickAddInputParam("ArchiveLocation", DbtType.dbtString, FormatEmptyString(sArchiveLocation))
            objInParamList.QuickAddInputParam("TissueCode", DbtType.dbtString, FormatEmptyString(sTissueCode))

            FillDataTable("GetAnimalTissuesArchiveInformation", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function



    Public Function GetAnimalSlideArchiveInformation(ByRef dtSortedData As DataTable, _
                                                     ByVal sSenderRef As String, _
                                                    ByVal sHistologyRef As String, _
                                                    ByVal sArchiveLocation As String)
        Dim objInParamList As New ParameterList
        Dim dtStainArchiveData As New DataTable
        Dim dtBatchTypes As New DataTable
        Dim dtAntibodiesArchiveData As New DataTable
        Dim dtHistologyArchiveData As New DataTable
        Dim dtData As New DataTable
        Dim drRow As DataRow
        Dim drTestRow As DataRow
        Dim drNewRow As DataRow

        Try
            objInParamList.QuickAddInputParam("SenderRef", DbtType.dbtString, FormatEmptyString(sSenderRef))
            objInParamList.QuickAddInputParam("HistologyRef", DbtType.dbtString, FormatEmptyString(sHistologyRef))
            objInParamList.QuickAddInputParam("ArchiveLocation", DbtType.dbtString, FormatEmptyString(sArchiveLocation))

            FillDataTable("GetAnimalStainArchiveInformation", _
                          CommandType.StoredProcedure, _
                          dtStainArchiveData, _
                          objInParamList)

            dtData = dtStainArchiveData.Copy()

            'Get the batch types associated with the animal
            objInParamList.Clear()

            objInParamList.QuickAddInputParam("SenderRef", DbtType.dbtString, FormatEmptyString(sSenderRef))
            objInParamList.QuickAddInputParam("HistologyRef", DbtType.dbtString, FormatEmptyString(sHistologyRef))

            FillDataTable("GetAnimalBatches", _
                          CommandType.StoredProcedure, _
                          dtBatchTypes, _
                          objInParamList)

            objInParamList.Clear()

            For Each drRow In dtBatchTypes.Rows
                objInParamList.QuickAddInputParam("SenderRef", DbtType.dbtString, FormatEmptyString(sSenderRef))
                objInParamList.QuickAddInputParam("HistologyRef", DbtType.dbtString, FormatEmptyString(sHistologyRef))
                objInParamList.QuickAddInputParam("ArchiveLocation", DbtType.dbtString, FormatEmptyString(sArchiveLocation))
                objInParamList.QuickAddInputParam("BatchID", DbtType.dbtInteger, drRow("BatchID"))
                objInParamList.QuickAddInputParam("SubmissionType", DbtType.dbtInteger, drRow("SubmissionType"))

                FillDataTable("GetAnimalAntibodiesArchiveInformation", _
                              CommandType.StoredProcedure, _
                              dtAntibodiesArchiveData, _
                              objInParamList)

                For Each drTestRow In dtAntibodiesArchiveData.Rows
                    dtData.ImportRow(drTestRow)
                Next

                objInParamList.Clear()
            Next

            objInParamList.QuickAddInputParam("SenderRef", DbtType.dbtString, FormatEmptyString(sSenderRef))
            objInParamList.QuickAddInputParam("HistologyRef", DbtType.dbtString, FormatEmptyString(sHistologyRef))
            objInParamList.QuickAddInputParam("ArchiveLocation", DbtType.dbtString, FormatEmptyString(sArchiveLocation))

            FillDataTable("GetAnimalHistologyArchiveInformation", _
                              CommandType.StoredProcedure, _
                              dtHistologyArchiveData, _
                              objInParamList)

            For Each drTestRow In dtHistologyArchiveData.Rows
                If Not drTestRow("Description").ToString = "Special Stain" And _
                   Not drTestRow("Description").ToString = "IHC - PrP" And _
                   Not drTestRow("Description").ToString = "IHC - Other" Then
                    dtData.ImportRow(drTestRow)
                End If
            Next

            'Order rows by block ref
            Dim dv As New DataView
            dv = dtData.DefaultView
            Dim iCount As Integer
            dv.Sort = "BlockRef ASC"

            dtSortedData = dtData.Clone()

            For iCount = 0 To dv.Count - 1
                drNewRow = dtSortedData.NewRow()
                drNewRow("BatchID") = dv(iCount)("BatchID")
                drNewRow("BlockRef") = dv(iCount)("BlockRef")
                drNewRow("ArchivedDate") = dv(iCount)("ArchivedDate")
                drNewRow("ArchiveLocation") = dv(iCount)("ArchiveLocation")
                drNewRow("Description") = dv(iCount)("Description")
                drNewRow("TissueDescription") = dv(iCount)("TissueDescription")
                drNewRow("NoPieces") = dv(iCount)("NoPieces")
                dtSortedData.Rows.Add(drNewRow)
            Next

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function

    Public Function CheckIfPGAnimal(ByVal dsBatchDetails As DataSet, ByVal iAnimalID As Integer) As Boolean
        Try
            Dim dtAnimal As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL)
            Dim sFilter As String
            Dim drFoundRows As DataRow()
            Dim bPGAnimal As Boolean = False

            sFilter = "ID=" & iAnimalID

            drFoundRows = dtAnimal.Select(sFilter)

            If Not drFoundRows Is Nothing AndAlso drFoundRows.Length > 0 Then
                bPGAnimal = drFoundRows(0)("IsPGNumber")
            Else
                Throw New Exception("CheckIfPGAnimal could not find session Animal")
            End If

            Return bPGAnimal

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function

#End Region

#Region "Update Animal Data"

    Public Function AddAnimal(ByVal sSenderRef As String, ByRef iAnimalID As Integer, ByVal sHistologyRef As String) As Boolean

        Dim objParamList As New ParameterList

        Try
            objParamList.AddParameter("ReturnValue", DbtType.dbtInteger, "@Error", , , -1, ParameterDirection.ReturnValue)
            If sSenderRef = "" Then
                objParamList.AddParameter("SenderRef", DbtType.dbtString, "@SenderRef", "SenderRef", 20, DBNull.Value)
            Else
                objParamList.AddParameter("SenderRef", DbtType.dbtString, "@SenderRef", "SenderRef", 20, sSenderRef)
            End If
            If sHistologyRef = "" Then
                objParamList.AddParameter("HistologyRef", DbtType.dbtString, "@HistologyRef", "HistologyRef", 20, DBNull.Value)
            Else
                objParamList.AddParameter("HistologyRef", DbtType.dbtString, "@HistologyRef", "HistologyRef", 20, sHistologyRef)
            End If

            objParamList.AddParameter("NextBlockRef", DbtType.dbtString, "@NextBlockRef", "NextBlockRef", 4, "01")
            objParamList.AddParameter("PMDate", DbtType.dbtDateTime, "@PMDate", "PMDate", , DBNull.Value)
            objParamList.AddParameter("OnHold", DbtType.dbtBoolean, "@OnHold", "OnHold", , False)
            objParamList.AddParameter("NewID", DbtType.dbtInteger, "@NewID", "ID", , , ParameterDirection.Output)

            ExecuteNonQuery("AddAnimal", _
                            CommandType.StoredProcedure, _
                            objParamList)

            ' Pass back SubmissionID if added OK
            iAnimalID = objParamList("NewID").Value()

            If (iAnimalID > 0) Then
                Return True
            End If

        Catch exSP As StoredProcException
            clsLog.LogException(exSP, clsLog.LogSource.lsStoredProcedure)
            Return False
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function

    Public Sub UpdateAnimalDetails(ByRef dtData As DataTable, _
                                        ByRef dbConn As Object, _
                                        ByRef dbTran As Object, _
                                        ByRef objErrorList As ArrayList, _
                                        ByRef AnimalIDs As ArrayList, _
                                        ByVal iUserID As Integer)
        Dim drDataRow As DataRow
        For Each drDataRow In dtData.Rows
            UpdateAnimalRow(drDataRow, dbConn, dbTran, objErrorList, AnimalIDs, iUserID)
        Next
    End Sub

    Public Sub UpdateAnimalRow(ByRef drAnimalRow As DataRow, _
                               ByRef objDBConn As SqlConnection, _
                               ByRef objDBTran As SqlTransaction, _
                               ByRef objErrorList As ArrayList, _
                               ByRef AnimalIDs As ArrayList, _
                               ByVal iUserID As Integer)

        If drAnimalRow.RowState <> DataRowState.Added And drAnimalRow.RowState <> DataRowState.Modified Then
            Exit Sub
        End If

        Dim objAnimalParamList As New libDataAccess.libDataAccess.ParameterList

        With objAnimalParamList
            .AddParameter("RETURN_VALUE", DbtType.dbtInteger, "RETURN_VALUE", daDirection:=ParameterDirection.ReturnValue)

            If drAnimalRow("RowState") = DataRowState.Modified Or drAnimalRow.RowState = DataRowState.Modified Then
                .QuickAddInputParam("ID", DbtType.dbtInteger, drAnimalRow.Item("ID"))
                .QuickAddInputParam("RowStamp", DbtType.dbtBinary, drAnimalRow.Item("RowStamp"))
                .QuickAddInputParam("UserID", DbtType.dbtInteger, iUserID)
            End If

            .QuickAddInputParam("SenderRef", DbtType.dbtString, drAnimalRow.Item("SenderRef"))
            .QuickAddInputParam("HistologyRef", DbtType.dbtString, drAnimalRow.Item("HistologyRef"))
            .QuickAddInputParam("NextBlockRef", DbtType.dbtString, drAnimalRow.Item("NextBlockRef"))
            .QuickAddInputParam("OnHold", DbtType.dbtBoolean, drAnimalRow.Item("OnHold"))
            .QuickAddInputParam("PMDate", DbtType.dbtDateTime, drAnimalRow.Item("PMDate"))
            If drAnimalRow("RowState") = DataRowState.Added Then
                .AddParameter("NewID", DbtType.dbtInteger, "@NewID", , , , ParameterDirection.Output)
            End If
        End With

        If drAnimalRow("RowState") = DataRowState.Added Then
            Try
                TBCultureDA.ExecuteNonQuery(objDBConn, objDBTran, "AddAnimal", CommandType.StoredProcedure, objAnimalParamList)
            Catch ex As Exception
                clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
                Throw New AnimalUpdateException(ex.Message, ex.InnerException)
            End Try

            Dim Ids As New HistopathologyLib.clsIDPairs
            Ids.OldID = drAnimalRow.Item("ID")
            Ids.NewID = CInt(objAnimalParamList("NewID").Value)
            AnimalIDs.Add(Ids)

            Dim iReturnValue As Integer = CInt(objAnimalParamList("RETURN_VALUE").Value)
            Select Case iReturnValue
                Case 1
                    Throw New AnimalUpdateException("Add Animal: Another user has altered the Animal Record.")
                Case 2
                    Throw New AnimalUpdateException("Add Animal: Another Animal is already assigned the Histology Ref: " & drAnimalRow.Item("HistologyRef").ToString & ".")
            End Select

        ElseIf drAnimalRow("RowState") = DataRowState.Modified Or drAnimalRow.RowState = DataRowState.Modified Then
            Try
                TBCultureDA.ExecuteNonQuery(objDBConn, objDBTran, "EditAnimal", CommandType.StoredProcedure, objAnimalParamList)
            Catch ex As Exception
                clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
                Throw New AnimalUpdateException(ex.Message, ex.InnerException)
            End Try
            Dim iReturnValue As Integer = CInt(objAnimalParamList("RETURN_VALUE").Value)
            Select Case iReturnValue
                Case 1
                    Throw New AnimalUpdateException("Edit Animal: Another User has altered the Animal Record.")
                Case 2
                    Throw New AnimalUpdateException("Edit Animal: Another Animal is already assigned the Histology Ref: " & drAnimalRow.Item("HistologyRef").ToString & ".")
            End Select
        End If

    End Sub

    Public Function UpdateAnimalNextBlock(ByRef dtAnimal As DataTable, ByVal iAnimalID As Integer, ByVal sBlockRef As String)
        Try
            Dim drFoundRows As DataRow()
            Dim sFilter As String

            sFilter = "ID=" & iAnimalID
            drFoundRows = dtAnimal.Select(sFilter)
            If Not drFoundRows Is Nothing Then
                If Not Convert.ToInt32(sBlockRef) Then
                    drFoundRows(0)("NextBlockRef") = sBlockRef
                End If
            Else
                Throw New Exception("Animal.UpdateAnimalNextBlock, couldnt find animal with ID " & Convert.ToString(iAnimalID) & ".")
            End If
            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function

    Public Function UpdateAnimalSenderRef(ByVal sSenderRef As String, ByVal sNewSenderRef As String, ByVal iUserID As Integer) As Boolean

        Dim objAnimalParamList As New libDataAccess.libDataAccess.ParameterList

        Try

            With objAnimalParamList
                .AddParameter("RETURN_VALUE", DbtType.dbtInteger, "RETURN_VALUE", daDirection:=ParameterDirection.ReturnValue)
                .QuickAddInputParam("SenderRef", DbtType.dbtString, sSenderRef)
                .QuickAddInputParam("NewSenderRef", DbtType.dbtString, sNewSenderRef)
                .QuickAddInputParam("UserID", DbtType.dbtInteger, iUserID)
            End With

            ExecuteNonQuery("EditAnimalSenderRef", CommandType.StoredProcedure, objAnimalParamList)

        Catch exSP As StoredProcException
            clsLog.LogException(exSP, clsLog.LogSource.lsStoredProcedure)
            Return False
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try

        Dim iReturnValue As Integer = CInt(objAnimalParamList("RETURN_VALUE").Value)
        Select Case iReturnValue
            Case 1
                Throw New AnimalUpdateException("The Sample Sender Reference was not found.")
            Case 2
                Throw New Exception("There are duplicate entries with the same Sender Reference in the database.")
            Case 3
                Throw New AnimalUpdateException("The New Sender Reference has already been used for another sample.")
        End Select

        Return True

    End Function

    Public Function UpdateAnimalHistologyRef(ByVal sSenderRef As String, ByVal sNewHistologyRef As String, ByVal iUserID As Integer) As Boolean

        Dim objAnimalParamList As New libDataAccess.libDataAccess.ParameterList

        Try

            With objAnimalParamList
                .AddParameter("RETURN_VALUE", DbtType.dbtInteger, "RETURN_VALUE", daDirection:=ParameterDirection.ReturnValue)
                .QuickAddInputParam("SenderRef", DbtType.dbtString, sSenderRef)
                .QuickAddInputParam("NewHistologyRef", DbtType.dbtString, sNewHistologyRef)
                .QuickAddInputParam("UserID", DbtType.dbtInteger, iUserID)
            End With

            ExecuteNonQuery("EditAnimalHistologyRef", CommandType.StoredProcedure, objAnimalParamList)

        Catch exSP As StoredProcException
            clsLog.LogException(exSP, clsLog.LogSource.lsStoredProcedure)
            Return False
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try

        Dim iReturnValue As Integer = CInt(objAnimalParamList("RETURN_VALUE").Value)
        Select Case iReturnValue
            Case 1
                Throw New AnimalUpdateException("The Sample Sender Reference was not found.")
            Case 2
                Throw New Exception("There are duplicate entries with the same Sender Reference in the database.")
            Case 3
                Throw New AnimalUpdateException("The new Histology Reference has already been used for another sample.")
        End Select

        Return True

    End Function


#End Region

#Region "Private Functions"

    Private Function FormatEmptyString(ByVal sValue As String) As Object
        If sValue = "" Then
            Return DBNull.Value
        Else
            Return sValue
        End If
    End Function

    Public Function IsAfter01(ByVal sYearPart As String) As Boolean
        Dim iYear As Integer = CInt(sYearPart)

        If iYear >= 1 And iYear < 70 Then
            Return True
        Else
            Return False
        End If
    End Function

#End Region

#Region "Delete Animal Data"

    Public Function RemoveSubmission(ByRef dsBatchDetails As DataSet, ByVal iAnimalID As Integer, ByVal sTable As String, Optional ByVal bIsPreCassetted As Boolean = False)
        Try
            If sTable = "BATCH_BLOCK_TABLE" Then
                Dim objBatchBlock As New HistopathologyLib.clsBlock
                Dim dtBatchBlock As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE)
                Dim dtAnimal As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL)
                Dim dtPreBooked As DataTable
                Dim foundRows As DataRow()
                Dim drPreBookedRows As DataRow()
                Dim dr As DataRow
                Dim sFilter As String
                Dim objAnimal As New clsAnimal

                sFilter = "AnimalID=" & iAnimalID
                foundRows = dtBatchBlock.Select(sFilter)

                objAnimal.GetPreBookedBlocks(iAnimalID, dsBatchDetails)
                If bIsPreCassetted Then
                    dtPreBooked = dsBatchDetails.Tables(HistopathologyLib.clsBatch.ANIMAL_PREBOOKED_BLOCKS)
                End If

                If Not foundRows Is Nothing Then
                    For Each dr In foundRows
                        ' Check if the block was a pre booked block
                        If bIsPreCassetted Then
                            drPreBookedRows = dtPreBooked.Select("AnimalID=" & iAnimalID & " AND BlockRef='" & dr("BlockRef") & "'")

                            If Not drPreBookedRows Is Nothing Then
                                If drPreBookedRows.Length > 0 Then
                                    objBatchBlock.EditPreBookedBlockStatus(dtPreBooked, drPreBookedRows(0)("ID"), HistopathologyLib.clsBlock.STATUS_PREBOOKED)
                                    objBatchBlock.ClearBatchID(dtPreBooked, drPreBookedRows(0)("ID"))
                                End If
                            End If
                        End If

                        If Not objBatchBlock.DeleteBlockData(dsBatchDetails, dr("ID"), bIsPreCassetted) Then
                            Throw New Exception("Block.DeleteBlockData returned false.")
                        End If
                    Next
                End If

                'delete the animal if it wasnt retrieved from the database
                sFilter = "ID=" & iAnimalID

                foundRows = dtAnimal.Select(sFilter)
                If Not foundRows Is Nothing And foundRows.Length > 0 Then
                    foundRows(0)("RowState") = DataRowState.Deleted
                    dtAnimal.Rows.Remove(foundRows(0))
                End If

                Return True
            Else
                Dim objBatchSubmission As New HistopathologyLib.clsBatchSubmission
                Dim dtAnimal As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE)
                Dim dtBatchSubmission As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_SUBMISSION_TABLE)
                Dim dtTissues As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TISSUES_TABLE)
                Dim foundRows As DataRow()
                Dim dr As DataRow
                Dim sFilter As String

                sFilter = "AnimalID=" & iAnimalID
                foundRows = dtBatchSubmission.Select(sFilter)

                If Not foundRows Is Nothing Then
                    For Each dr In foundRows
                        If Not objBatchSubmission.DeleteRecord(dtTissues, _
                                                        dtBatchSubmission, _
                                                        dr("ID")) Then
                            Throw New Exception("BatchSubmission.DeleteSubmission returned false.")
                        End If
                    Next
                End If

                'delete the animal if it wasnt retrieved from the database
                sFilter = "ID=" & iAnimalID

                foundRows = dtAnimal.Select(sFilter)

                If Not foundRows Is Nothing And foundRows.Length > 0 Then
                    foundRows(0)("RowState") = DataRowState.Deleted
                    dtAnimal.Rows.Remove(foundRows(0))
                End If

                Return True
            End If
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function
#End Region

    Public Function GetPreBookedBlock(ByRef dtPreBooked As DataTable, _
                                  ByVal iAnimalID As Integer, _
                                  ByRef iBlockID As Integer, _
                                  ByRef sBlockref As String) As Boolean

        Try
            Dim drFoundAnimalBlocks As DataRow()

            drFoundAnimalBlocks = dtPreBooked.Select("AnimalID=" & iAnimalID & " AND Status=" & HistopathologyLib.clsBlock.STATUS_PREBOOKED)

            If Not drFoundAnimalBlocks Is Nothing Then
                If drFoundAnimalBlocks.Length > 0 Then
                    sBlockref = drFoundAnimalBlocks(0)("BlockRef").ToString
                    iBlockID = drFoundAnimalBlocks(0)("ID")
                    Return True
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

    Public Function CheckPreBookedBlocksAvailable(ByVal iAnimalID As Integer, ByRef dsPreBooked As DataTable) As Boolean
        Try
            Dim drFoundRows As DataRow()
            drFoundRows = dsPreBooked.Select("AnimalID=" & iAnimalID & " AND Status=" & HistopathologyLib.clsBlock.STATUS_PREBOOKED)

            If Not drFoundRows Is Nothing Then
                If drFoundRows.Length > 0 Then
                    Return True
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

    Public Function CheckBlockIsPreBooked(ByVal iAnimalID As Integer, ByVal sBlockRef As String, ByRef dtPreBooked As DataTable) As Boolean
        Try
            Dim drFoundRows As DataRow()
            drFoundRows = dtPreBooked.Select("AnimalID=" & iAnimalID & " AND BlockRef='" & sBlockRef & "'")

            If Not drFoundRows Is Nothing Then
                If drFoundRows.Length > 0 Then
                    Return True
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

    Public Function GetPreBookedBlocks(ByVal iAnimalID As Integer, ByRef dsBatchDetails As DataSet) As Boolean
        Dim objInParamList As New ParameterList
        Dim dtData As DataTable
        Dim drRow As DataRow
        Dim drNewRow As DataRow
        Dim drFoundRows As DataRow()
        Dim drFoundBlock As DataRow()

        Try
            objInParamList.QuickAddInputParam("AnimalID", DbtType.dbtString, iAnimalID)

            FillDataTable("GetAnimalPreBookedBlocks", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            dtData.TableName = "ANIMAL_PREBOOKED_BLOCKS"

            If dsBatchDetails.Tables.IndexOf("ANIMAL_PREBOOKED_BLOCKS") = -1 Then
                dsBatchDetails.Tables.Add(dtData)
            Else
                For Each drRow In dtData.Rows
                    drFoundBlock = dsBatchDetails.Tables("ANIMAL_PREBOOKED_BLOCKS").Select("ID=" & drRow("ID"))

                    If drFoundBlock.Length = 0 Then
                        dsBatchDetails.Tables("ANIMAL_PREBOOKED_BLOCKS").ImportRow(drRow)
                    End If
                Next
            End If
            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function

    Public Function CheckPreBookedBlocksExist(ByVal iAnimalID As Integer, ByRef dsBatchDetails As DataSet, ByRef iNumberOfPreBooked As Integer) As Boolean
        Dim objInParamList As New ParameterList
        Dim dtData As DataTable
        Dim drFoundRows As DataRow()

        Try
            dtData = dsBatchDetails.Tables(clsBatch.ANIMAL_PREBOOKED_BLOCKS)
            drFoundRows = dtData.Select("AnimalID=" & iAnimalID & " AND Status=" & HistopathologyLib.clsBlock.STATUS_PREBOOKED)

            If drFoundRows.Length > 0 Then
                iNumberOfPreBooked = drFoundRows.Length
                Return True
            Else
                Return False
            End If

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function

    Public Function test(ByRef dtBlocks As DataTable, ByRef dtPreBooked As DataTable, ByVal iBlockID As Integer, ByVal iAnimalID As Integer, ByVal sBlockRef As String, ByVal iBatchID As Integer)
        Try
            Dim foundOrginalRows As DataRow()
            Dim foundNewRows As DataRow()
            Dim foundBlock As DataRow()
            Dim sNewBlockRef As String

            foundOrginalRows = dtPreBooked.Select("ID=" & iBlockID & " AND BlockRef='" & sBlockRef & "'")

            ' If not found this row the block ref for the block has changed.
            If foundOrginalRows.Length = 0 Then
                foundNewRows = dtPreBooked.Select("AnimalID=" & iAnimalID & " AND BlockRef='" & sBlockRef & "'")

                If foundNewRows.Length = 0 Then
                    'This is not a prebooked block
                Else
                    sNewBlockRef = foundNewRows(0)("BlockRef").ToString
                    foundOrginalRows = dtPreBooked.Select("ID=" & iBlockID)

                    If Not foundOrginalRows.Length = 0 Then
                        foundNewRows(0)("BlockRef") = foundOrginalRows(0)("BlockRef")
                        foundOrginalRows(0)("BlockRef") = sNewBlockRef
                        foundOrginalRows(0)("Status") = clsBlock.STATUS_PREBOOKED_USED
                        foundOrginalRows(0)("BatchID") = iBatchID
                    End If

                    foundBlock = dtBlocks.Select("ID=" & iBlockID)
                    If Not foundBlock.Length = 0 Then
                        foundBlock(0)("Status") = clsBlock.STATUS_PREBOOKED_USED
                    End If
                End If
            Else
                foundOrginalRows(0)("BatchID") = iBatchID
                foundOrginalRows(0)("Status") = clsBlock.STATUS_PREBOOKED_USED

                foundBlock = dtBlocks.Select("ID=" & iBlockID)
                If Not foundBlock.Length = 0 Then
                    foundBlock(0)("Status") = clsBlock.STATUS_PREBOOKED_USED
                End If
            End If

        Catch ex As Exception

        End Try

    End Function

    Public Function GetNumberOfBlocks(ByVal dsBatchDetails As DataSet, ByVal iAnimalID As Integer, ByRef iNumberOfSamples As Integer) As Boolean
        Try
            Dim dtBatchBlocks As DataTable
            Dim drRow As DataRow
            Dim iFoundAnimalId As Integer

            iNumberOfSamples = 0
            If Not dsBatchDetails Is Nothing Then
                'Find the number of samples that have been added against the submission
                dtBatchBlocks = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE)

                For Each drRow In dtBatchBlocks.Rows
                    If Not drRow.RowState = DataRowState.Deleted Then
                        iFoundAnimalId = drRow("AnimalID")

                        If iFoundAnimalId = iAnimalID Then
                            iNumberOfSamples += 1
                        End If
                    End If
                Next
            End If

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try

    End Function

    Public Function GetNextFreeBlockRef(ByVal dsBatchDetails As DataSet, ByVal iAnimalID As Integer, ByRef sBlockRef As String) As Boolean
        Try
            Dim dtPreBooked As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.ANIMAL_PREBOOKED_BLOCKS)
            Dim dtAnimal As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL)
            Dim drFoundAnimal As DataRow
            Dim dtBatchBlocks As DataTable
            Dim drRow As DataRow
            Dim iFoundAnimalId As Integer
            Dim bFoundFreeRef As Boolean = False
            Dim iBlockref As Integer = 0

            drFoundAnimal = dtAnimal.Rows.Find(iAnimalID)
            sBlockRef = drFoundAnimal("NextBlockRef").ToString

            ' May need to check that block has not been used before on submission
            While bFoundFreeRef = False
                iBlockref = CInt(sBlockRef)
                If Not CheckBlockIsPreBooked(iAnimalID, sBlockRef, dtPreBooked) Then
                    bFoundFreeRef = True
                Else
                    iBlockref += 1
                    If iBlockref > CInt(sBlockRef) Then
                        If iBlockref < 10 Then
                            sBlockRef = "0" & Convert.ToString(iBlockref)
                        Else
                            sBlockRef = Convert.ToString(iBlockref)
                        End If
                    End If
                End If
            End While

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try

    End Function

    Public Function GetImportedData(ByRef dtData As DataTable, ByVal sSelectedTable As String) As Boolean
        Dim objInParamList As New ParameterList
        Dim sStoredProcedure As String = ""
        Try
            Select Case sSelectedTable
                Case ""
                    'Do nothing
                Case "1"
                    sStoredProcedure = "Get2001EXTSUB"
                Case "2"
                    sStoredProcedure = "Get2001NEUROSUB"
                Case "3"
                    sStoredProcedure = "Get2002EXTSUB"
                Case "4"
                    sStoredProcedure = "Get2002EXTSUBNOCPU"
                Case "5"
                    sStoredProcedure = "Get2002MOUSESUB"
                Case "6"
                    sStoredProcedure = "Get2002NEUROSUB"
                Case "7"
                    sStoredProcedure = "Get2003EXTSUB"
                Case "8"
                    sStoredProcedure = "Get2003MOUSESUB"
                Case "9"
                    sStoredProcedure = "Get2003NEUROSUB"
                Case "10"
                    sStoredProcedure = "Get2004EXTSUB"
                Case "11"
                    sStoredProcedure = "Get2004MOUSESUB"
                Case "12"
                    sStoredProcedure = "Get2004NEUROSUB"
                Case "13"
                    sStoredProcedure = "Get2005TBDIAGSUB"
                Case "14"
                    sStoredProcedure = "GetICCSUBMI11999TO12JAN2001"
                Case "15"
                    sStoredProcedure = "GetICCSUBMI1TISSUEONLYTO12THJAN2001"
                Case Else
                    sStoredProcedure = "GetAllImportedData"
            End Select

            If sStoredProcedure <> "" Then
                FillDataTable(sStoredProcedure, _
                           CommandType.StoredProcedure, _
                           dtData, _
                           objInParamList)
            End If

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function

    Public Function CopyAnimalTissuesFromPreviousSubmission(ByVal dsPreviousBatchDetails As DataSet, _
                                                            ByRef dsCurrentBatchDetails As DataSet, _
                                                            ByVal iPreviousAnimalID As Integer, _
                                                            ByVal iCurrentAnimalID As Integer, _
                                                            ByVal iBatchID As Integer, _
                                                            ByVal iOldBlockID As Integer) As Boolean
        Try
            Dim dtPreviousBatchSubmission As DataTable = dsPreviousBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_SUBMISSION_TABLE)
            Dim dtPreviousAnimalTissues As DataTable = dsPreviousBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TISSUES_TABLE)
            Dim dtCurrentBlockTissues As DataTable = dsCurrentBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TISSUES)
            Dim objTissues As New HistopathologyLib.clsTissue
            Dim drSampleIDs As DataRow()
            Dim sFilter As String
            Dim drFoundRows As DataRow()
            Dim drRow As DataRow
            Dim drIDRow As DataRow

            sFilter = "AnimalID=" & iPreviousAnimalID

            drSampleIDs = dtPreviousBatchSubmission.Select(sFilter)

            For Each drIDRow In drSampleIDs
                sFilter = "BatchSubmissionID=" & drIDRow("ID")

                drFoundRows = dtPreviousAnimalTissues.Select(sFilter)

                For Each drRow In drFoundRows
                    If Not objTissues.NewBlockTissue(dtCurrentBlockTissues, iOldBlockID, drRow) Then
                        Throw New Exception("Tissues.NewBlockTissue returned false.")
                    End If
                Next
            Next

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try
    End Function

    Public Function ValidateAnimalBlocks(ByRef dsBatchDetails As DataSet, _
                                         ByRef dsOldBatchDetails As DataSet, _
                                         ByVal iNewAnimalID As Integer, _
                                         ByVal iOldAnimalID As Integer, _
                                         ByVal iMinPreBooked As Integer, _
                                         ByVal sSenderRef As String, _
                                         ByRef sValidationError As String, _
                                         ByVal bValidateRequiredNumber As Boolean) As Boolean

        Try

            Dim iNumberPreBookedAvailable As Integer

            'If bValidateRequiredNumber Then
            'If Not CheckPreBookedBlocksExist(iNewAnimalID, dsBatchDetails, iNumberPreBookedAvailable) Then
            'sValidationError = "Sender Ref: " & Trim(sSenderRef) & " cannot be copied to as no pre booked blocks exist.<br>"
            'Return True
            'End If

            'If iNumberPreBookedAvailable < iMinPreBooked Then
            'sValidationError = "Sender Ref: " & Trim(sSenderRef) & " cannot be copied to as it does not have enough pre booked blocks.<br>"
            'Return True
            'End If
            'End If

            ' First get a list of all pre booked blocks for the new animal

            Dim dtNewData As DataTable
            Dim dtOldData As DataTable
            Dim drNewAnimalBlocks As DataRow()
            Dim drOldAnimalBlocks As DataRow()
            Dim drNewBlock As DataRow
            Dim drOldBlock As DataRow
            Dim bFoundBlock As Boolean

            dtNewData = dsBatchDetails.Tables(clsBatch.ANIMAL_PREBOOKED_BLOCKS)
            drNewAnimalBlocks = dtNewData.Select("AnimalID=" & iNewAnimalID & " AND Status=" & HistopathologyLib.clsBlock.STATUS_PREBOOKED)

            dtOldData = dsOldBatchDetails.Tables(clsBatch.BATCH_BLOCK_TABLE)
            drOldAnimalBlocks = dtOldData.Select("AnimalID=" & iOldAnimalID)

            For Each drOldBlock In drOldAnimalBlocks
                bFoundBlock = False

                For Each drNewBlock In drNewAnimalBlocks
                    If drOldBlock("Blockref") = drNewBlock("BlockRef") Then
                        bFoundBlock = True
                        Exit For
                    End If
                Next

                If Not bFoundBlock Then
                    If bValidateRequiredNumber Then
                        If sValidationError = "" Then
                            sValidationError = drOldBlock("Blockref")
                        Else
                            sValidationError = sValidationError & ", " & drOldBlock("Blockref")
                        End If
                    End If
                Else
                    If bValidateRequiredNumber = False Then
                        If sValidationError = "" Then
                            sValidationError = drOldBlock("Blockref")
                        Else
                            sValidationError = sValidationError & ", " & drOldBlock("Blockref")
                        End If
                    End If
                End If
            Next

            If sValidationError <> "" Then
                If bValidateRequiredNumber = True Then
                    sValidationError = "Sender Ref: " & Trim(sSenderRef) & " cannot be copied to as the following blocks are either not pre-booked or available: " & sValidationError

                Else
                    sValidationError = "Sender Ref: " & Trim(sSenderRef) & " cannot be copied to as the following blocks have been pre-booked: " & sValidationError
                End If
            End If

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsAnimalObject)
            Return False
        End Try

    End Function
End Class
