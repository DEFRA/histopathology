Imports libDataAccess.libDataAccess
Imports libDataAccess.libDataAccess.TBCultureDA
Imports System.Data.SqlClient


Public Class BatchUpdateException : Inherits ApplicationException

    Public Sub New(ByVal message As String)
        MyBase.New(message)
    End Sub

    Public Sub New(ByVal message As String, ByVal inner As Exception)
        MyBase.New(message, inner)
    End Sub

End Class


Public Class clsBatch

    'DataSet Batch tables 
    Public Const BATCH_TABLE As Integer = 0
    Public Const BATCH_HISTOLOGY_TABLE As Integer = 1
    Public Const BATCH_ANTIBODIES_TABLE As Integer = 2
    Public Const BATCH_STAIN_TABLE As Integer = 3
    Public Const BATCH_POSTFIXATION_TABLE As Integer = 4
    Public Const BATCH_SUBMITTEDAS_TABLE As Integer = 5

    'Dataset Batch Sample tables
    Public Const BATCH_SUBMISSION_TABLE As Integer = 6
    Public Const BATCH_TISSUES_TABLE As Integer = 7
    Public Const BATCH_ANIMAL_TABLE As Integer = 8

    'DataSet Block Tables
    Public Const BATCH_BLOCK_TABLE As Integer = 6
    Public Const BATCH_BLOCK_TISSUES As Integer = 7
    Public Const BATCH_BLOCK_HISTOLOGY As Integer = 8
    Public Const BATCH_BLOCK_ANTIBODIES As Integer = 9
    Public Const BATCH_BLOCK_STAIN As Integer = 10
    Public Const BATCH_BLOCK_ANIMAL As Integer = 11
    Public Const HISTOLOGY_REFS As Integer = 12
    Public Const BLOCK_ANTIBODIES_TCCODES As Integer = 14
    Public Const BLOCK_HISTOLOGY_TCCODES As Integer = 15
    Public Const BLOCK_SPECIALSTAIN_TCCODES As Integer = 13
    Public Const ANIMAL_PREBOOKED_BLOCKS As Integer = 16

    Public Const STATUS_SUBMITTED As String = "1"
    Public Const STATUS_RECEIVED As String = "2"
    Public Const STATUS_REJECTED As String = "3"
    Public Const STATUS_COMPLETED As String = "4"
    Public Const STATUS_ONHOLD As String = "5"
    Public Const STATUS_INPROGRESS As String = "6"

#Region "Get Batch Details"

    Public Function GetBatchComments(ByVal iBatchID As Integer, ByRef dsData As DataSet) As Boolean
        Dim objInParamList As New ParameterList
        Try
            objInParamList.QuickAddInputParam("ID", DbtType.dbtInteger, iBatchID)

            FillDataSet("GetAllBatchComments", _
                        CommandType.StoredProcedure, _
                        dsData, _
                        objInParamList)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBatchObject)
            Return False
        End Try
    End Function

    Public Function GetCommonBatchDetails(ByVal iBatchID As Integer, ByRef dsData As DataSet) As Boolean

        Dim objInParamList As New ParameterList

        Try
            objInParamList.QuickAddInputParam("ID", DbtType.dbtInteger, iBatchID)

            FillDataSet("GetCommonBatchTablesByID", _
                        CommandType.StoredProcedure, _
                        dsData, _
                        objInParamList)

            With dsData
                SetPrimaryKey(.Tables(BATCH_TABLE), "ID", True)
                SetPrimaryKey(.Tables(BATCH_HISTOLOGY_TABLE), "ID", True)
                SetPrimaryKey(.Tables(BATCH_ANTIBODIES_TABLE), "ID", True)
                SetPrimaryKey(.Tables(BATCH_STAIN_TABLE), "ID", True)
                SetPrimaryKey(.Tables(BATCH_POSTFIXATION_TABLE), "ID", True)
                SetPrimaryKey(.Tables(BATCH_SUBMITTEDAS_TABLE), "ID", True)
            End With

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBatchObject)
            Return False
        End Try
    End Function

    Public Function GetBatchBlockDetails(ByVal iBatchID As Integer, ByRef dsData As DataSet) As Boolean

        Dim objInParamList As New ParameterList

        Try
            objInParamList.QuickAddInputParam("ID", DbtType.dbtInteger, iBatchID)
            Dim dsNewDataSet As DataSet

            FillDataSet("GetBatchBlocksByID", _
                        CommandType.StoredProcedure, _
                        dsNewDataSet, _
                        objInParamList)

            'Rather than a complete redesign just import the new populated tables into
            'the batch details dataset
            If Not dsNewDataSet Is Nothing Then
                Dim iCount As Integer = 0
                For iCount = 0 To dsNewDataSet.Tables.Count - 1
                    Dim dtDataTable As New DataTable
                    dtDataTable = dsNewDataSet.Tables(iCount).Copy()
                    dtDataTable.TableName = Convert.ToString(iCount)
                    dsData.Tables.Add(dtDataTable)
                Next
            End If

            With dsData
                SetPrimaryKey(.Tables(BATCH_BLOCK_TABLE), "ID", True)
                SetPrimaryKey(.Tables(BATCH_BLOCK_HISTOLOGY), "ID", True)
                SetPrimaryKey(.Tables(BATCH_BLOCK_ANTIBODIES), "ID", True)
                SetPrimaryKey(.Tables(BATCH_BLOCK_STAIN), "ID", True)
                SetPrimaryKey(.Tables(BATCH_BLOCK_TISSUES), "ID", True)
                SetPrimaryKey(.Tables(BATCH_BLOCK_ANIMAL), "ID", True)
                SetPrimaryKey(.Tables(BLOCK_ANTIBODIES_TCCODES), "ID", True)
                SetPrimaryKey(.Tables(BLOCK_HISTOLOGY_TCCODES), "ID", True)
                SetPrimaryKey(.Tables(BLOCK_SPECIALSTAIN_TCCODES), "ID", True)
            End With

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBatchObject)
            Return False
        End Try
    End Function

    Public Function GetBatchSubmissionDetails(ByVal iBatchID As Integer, ByRef dsData As DataSet) As Boolean

        Dim objInParamList As New ParameterList
        Dim dsNewDataSet As DataSet
        Try
            objInParamList.QuickAddInputParam("ID", DbtType.dbtInteger, iBatchID)

            FillDataSet("GetBatchSubmissionDetailsByBatchID", _
                        CommandType.StoredProcedure, _
                        dsNewDataSet, _
                        objInParamList)

            'Rather than a complete redesign just import the new populated tables into
            'the batch details dataset
            If Not dsNewDataSet Is Nothing Then
                Dim iCount As Integer = 0
                For iCount = 0 To dsNewDataSet.Tables.Count - 1
                    Dim dtDataTable As New DataTable
                    dtDataTable = dsNewDataSet.Tables(iCount).Copy()
                    dtDataTable.TableName = Convert.ToString(iCount)
                    dsData.Tables.Add(dtDataTable)
                Next
            End If

            With dsData
                SetPrimaryKey(.Tables(BATCH_SUBMISSION_TABLE), "ID", True)
                SetPrimaryKey(.Tables(BATCH_TISSUES_TABLE), "ID", True)
                SetPrimaryKey(.Tables(BATCH_ANIMAL_TABLE), "ID", True)
            End With

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBatchObject)
            Return False
        End Try
    End Function

    Public Function GetReceivedBatches(ByRef dtData As DataTable) As Boolean
        Dim objInParamList As New ParameterList

        Try
            FillDataTable("GetReceivedBatches", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)
            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBatchObject)
            Return False
        End Try
    End Function

    Public Function GetBatchesToBeBlocked(ByRef dtData As DataTable) As Boolean
        Dim objInParamList As New ParameterList

        Try
            FillDataTable("GetBatchesToBeBlocked", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)
            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBatchObject)
            Return False
        End Try
    End Function

    'Public Function GetNotReceivedBatches(ByRef dtData As DataTable) As Boolean
    '    Dim objInParamList As New ParameterList()

    '    Try
    '        FillDataTable("GetNotReceivedBatches", _
    '                      CommandType.StoredProcedure, _
    '                      dtData, _
    '                      objInParamList)
    '        Return True

    '    Catch ex As Exception
    '        clsLog.LogException(ex, clsLog.LogSource.lsBatchObject)
    '        Return False
    '    End Try
    'End Function

    Public Function GetBatchesWithStatus(ByRef dtData As DataTable, ByVal iStatus As Integer) As Boolean
        Dim objInParamList As New ParameterList

        objInParamList.QuickAddInputParam("BatchStatus", DbtType.dbtInteger, iStatus)

        Try
            FillDataTable("GetBatchesWithStatus", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)
            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBatchObject)
            Return False
        End Try
    End Function

    Public Function GetBatchesForDispatch(ByRef dtData As DataTable) As Boolean
        Try
            FillDataTable("GetBatchesForDispatch", _
                          CommandType.StoredProcedure, _
                          dtData)
            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBatchObject)
            Return False
        End Try
    End Function

    Public Function SearchBatchDetails(ByVal SubmittedBy As String, _
                                        ByVal ProjectContractCode As String, _
                                        ByVal ContactName As String, _
                                        ByVal Species As String, _
                                        ByVal SubmittedArea As String, _
                                        ByVal SubmittedDateFrom As String, _
                                        ByVal SubmittedDateTo As String, _
                                        ByVal ReceivedDateFrom As String, _
                                        ByVal ReceivedDateTo As String, _
                                        ByVal Fixation As String, _
                                        ByVal HistologyRef As String, _
                                        ByVal SenderRef As String, _
                                        ByVal SubmissionNumber As String, _
                                        ByVal Status As String, _
                                        ByVal EnteredBy As String, _
                                        ByVal AllRecords As Integer, _
                                        ByRef dsData As DataTable) As Boolean

        Dim objInParamList As New ParameterList

        Try
            objInParamList.QuickAddInputParam("SubmittedBy", DbtType.dbtInteger, FormatEmptyString(SubmittedBy))
            objInParamList.QuickAddInputParam("ProjectContract", DbtType.dbtString, FormatEmptyString(ProjectContractCode))
            objInParamList.QuickAddInputParam("ContactName", DbtType.dbtString, FormatEmptyString(ContactName))
            objInParamList.QuickAddInputParam("Species", DbtType.dbtString, FormatEmptyString(Species))
            objInParamList.QuickAddInputParam("SubmittedArea", DbtType.dbtString, FormatEmptyString(SubmittedArea))
            objInParamList.QuickAddInputParam("SubmittedDateFrom", DbtType.dbtDateTime, FormatEmptyString(SubmittedDateFrom))
            objInParamList.QuickAddInputParam("SubmittedDateTo", DbtType.dbtDateTime, FormatEmptyString(SubmittedDateTo))
            objInParamList.QuickAddInputParam("ReceivedDateFrom", DbtType.dbtDateTime, FormatEmptyString(ReceivedDateFrom))
            objInParamList.QuickAddInputParam("ReceivedDateTo", DbtType.dbtDateTime, FormatEmptyString(ReceivedDateTo))
            objInParamList.QuickAddInputParam("Fixation", DbtType.dbtString, FormatEmptyString(Fixation))
            objInParamList.QuickAddInputParam("HistologyRef", DbtType.dbtString, FormatEmptyString(HistologyRef))
            objInParamList.QuickAddInputParam("SenderRef", DbtType.dbtString, FormatEmptyString(SenderRef))
            objInParamList.QuickAddInputParam("Number", DbtType.dbtInteger, FormatEmptyString(SubmissionNumber))
            objInParamList.QuickAddInputParam("Status", DbtType.dbtInteger, FormatEmptyString(Status))
            objInParamList.QuickAddInputParam("EnteredBy", DbtType.dbtInteger, FormatEmptyString(EnteredBy))
            objInParamList.QuickAddInputParam("All", DbtType.dbtInteger, FormatEmptyString(AllRecords))

            FillDataTable("GetSearchBatchDetails", _
                        CommandType.StoredProcedure, _
                        dsData, _
                        objInParamList)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBatchObject)
            Return False
        End Try
    End Function

    Public Function CheckQCNoteExists(ByVal iQCNoteRef As Integer, _
                                      ByRef iCount As Integer)

        Try
            Dim objInParamList As New ParameterList

            objInParamList.AddParameter("RETURN_VALUE", DbtType.dbtInteger, "RETURN_VALUE", daDirection:=ParameterDirection.ReturnValue)
            objInParamList.QuickAddInputParam("QCNoteRef", DbtType.dbtInteger, iQCNoteRef)

            ExecuteNonQuery("GetQCNoteByID", _
                            CommandType.StoredProcedure, _
                            objInParamList)

            iCount = CInt(objInParamList("RETURN_VALUE").Value)

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBatchObject)
            Return False
        End Try
    End Function
    Public Function CheckBatchExists(ByVal iBatchID As Integer, _
                                     ByVal iBatchStatus As Integer, _
                                     ByRef iCount As Integer, _
                                     ByRef iBatchType As Integer, _
                                     ByRef iIsCassetted As Integer, _
                                     Optional ByVal iCassetted As Integer = -1) As Boolean
        Try
            'Passing 0 to this function will check against all status
            Dim objInParamList As New ParameterList

            objInParamList.AddParameter("RETURN_VALUE", DbtType.dbtInteger, "RETURN_VALUE", daDirection:=ParameterDirection.ReturnValue)
            objInParamList.QuickAddInputParam("ID", DbtType.dbtInteger, iBatchID)
            objInParamList.QuickAddInputParam("BatchStatus", DbtType.dbtInteger, iBatchStatus)
            objInParamList.QuickAddInputParam("Cassetted", DbtType.dbtInteger, iCassetted)
            objInParamList.AddParameter("BatchType", DbtType.dbtInteger, "@BatchType", "BatchType", , daDirection:=ParameterDirection.Output)
            objInParamList.AddParameter("IsCassetted", DbtType.dbtInteger, "@IsCassetted", "Cassetted", , daDirection:=ParameterDirection.Output)

            ExecuteNonQuery("GetBatchWithStatus", _
                            CommandType.StoredProcedure, _
                            objInParamList)

            iCount = CInt(objInParamList("RETURN_VALUE").Value)
            If iCount > 0 Then
                iBatchType = CInt(objInParamList("BatchType").Value)
                iIsCassetted = CInt(objInParamList("IsCassetted").Value)
            End If

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBatchObject)
            Return False
        End Try
    End Function

    Public Function GetTestItemRows(ByVal ProjectDesc As String, _
                                    ByVal BatchType As Integer, _
                                    ByRef dsData As DataTable) As Boolean
        Try
            Dim objInParamList As New ParameterList
            Dim iCount As Integer = 0

            Dim dr As DataRow

            objInParamList.QuickAddInputParam("ProjectContractDesc", DbtType.dbtString, FormatEmptyString(ProjectDesc))
            objInParamList.QuickAddInputParam("BatchType", DbtType.dbtInteger, BatchType)

            FillDataTable("GetTestRows", _
                            CommandType.StoredProcedure, _
                            dsData, _
                            objInParamList)

            objInParamList.Clear()
            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBatchObject)
            Return False
        End Try
    End Function

    Public Function CountStainTestItems(ByVal ProjectContractCode As String, _
                                        ByVal SubmittedDateFrom As String, _
                                        ByVal SubmittedDateTo As String, _
                                        ByVal BatchType As Integer, _
                                        ByVal Tests As String, _
                                        ByVal TestCode As String, _
                                        ByRef dsData As DataTable) As Boolean
        Try
            Dim objInParamList As New ParameterList

            With objInParamList
                .QuickAddInputParam("ProjectContractCode", DbtType.dbtString, ProjectContractCode)
                .QuickAddInputParam("SubmittedDateFrom", DbtType.dbtDateTime, FormatEmptyString(SubmittedDateFrom))
                .QuickAddInputParam("SubmittedDateTo", DbtType.dbtDateTime, FormatEmptyString(SubmittedDateTo))
                .QuickAddInputParam("BatchType", DbtType.dbtInteger, BatchType)
                .QuickAddInputParam("Tests", DbtType.dbtString, Tests)
                .QuickAddInputParam("TestCode", DbtType.dbtString, TestCode)
            End With

            FillDataTable("GetTestStainsCounts", _
                          CommandType.StoredProcedure, _
                          dsData, _
                          objInParamList)

            objInParamList.Clear()
            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBatchObject)
            Return False
        End Try
    End Function

    Public Function CountStainTestBatch(ByVal ProjectContractCode As String, _
                                        ByVal SubmittedDateFrom As String, _
                                        ByVal SubmittedDateTo As String, _
                                        ByVal BatchType As Integer, _
                                        ByVal Tests As String, _
                                        ByVal TestCode As String, _
                                        ByRef dsData As DataTable) As Boolean
        Try
            Dim objInParamList As New ParameterList

            With objInParamList
                .QuickAddInputParam("ProjectContractCode", DbtType.dbtString, ProjectContractCode)
                .QuickAddInputParam("SubmittedDateFrom", DbtType.dbtDateTime, FormatEmptyString(SubmittedDateFrom))
                .QuickAddInputParam("SubmittedDateTo", DbtType.dbtDateTime, FormatEmptyString(SubmittedDateTo))
                .QuickAddInputParam("BatchType", DbtType.dbtInteger, BatchType)
                .QuickAddInputParam("Tests", DbtType.dbtString, Tests)
                .QuickAddInputParam("TestCode", DbtType.dbtString, TestCode)
            End With

            FillDataTable("GetTestStainsBatch", _
                          CommandType.StoredProcedure, _
                          dsData, _
                          objInParamList)

            objInParamList.Clear()
            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBatchObject)
            Return False
        End Try
    End Function

    Public Function CountAntibodesTestItems(ByVal ProjectContractCode As String, _
                                    ByVal SubmittedDateFrom As String, _
                                    ByVal SubmittedDateTo As String, _
                                    ByVal BatchType As Integer, _
                                    ByVal Tests As String, _
                                    ByVal TestCode As String, _
                                    ByRef dsData As DataTable) As Boolean
        Try
            Dim objInParamList As New ParameterList

            With objInParamList
                .QuickAddInputParam("ProjectContractCode", DbtType.dbtString, ProjectContractCode)
                .QuickAddInputParam("SubmittedDateFrom", DbtType.dbtDateTime, FormatEmptyString(SubmittedDateFrom))
                .QuickAddInputParam("SubmittedDateTo", DbtType.dbtDateTime, FormatEmptyString(SubmittedDateTo))
                .QuickAddInputParam("BatchType", DbtType.dbtInteger, BatchType)
                .QuickAddInputParam("Tests", DbtType.dbtString, Tests)
                .QuickAddInputParam("TestCode", DbtType.dbtString, TestCode)
            End With

            FillDataTable("GetTestAntibodiesCounts", _
                          CommandType.StoredProcedure, _
                          dsData, _
                          objInParamList)

            objInParamList.Clear()
            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBatchObject)
            Return False
        End Try
    End Function

    Public Function CountAntibodesTestBatch(ByVal ProjectContractCode As String, _
                                    ByVal SubmittedDateFrom As String, _
                                    ByVal SubmittedDateTo As String, _
                                    ByVal BatchType As Integer, _
                                    ByVal Tests As String, _
                                    ByVal TestCode As String, _
                                    ByRef dsData As DataTable) As Boolean
        Try
            Dim objInParamList As New ParameterList

            With objInParamList
                .QuickAddInputParam("ProjectContractCode", DbtType.dbtString, ProjectContractCode)
                .QuickAddInputParam("SubmittedDateFrom", DbtType.dbtDateTime, FormatEmptyString(SubmittedDateFrom))
                .QuickAddInputParam("SubmittedDateTo", DbtType.dbtDateTime, FormatEmptyString(SubmittedDateTo))
                .QuickAddInputParam("BatchType", DbtType.dbtInteger, BatchType)
                .QuickAddInputParam("Tests", DbtType.dbtString, Tests)
                .QuickAddInputParam("TestCode", DbtType.dbtString, TestCode)
            End With

            FillDataTable("GetTestAntibodiesBatch", _
                          CommandType.StoredProcedure, _
                          dsData, _
                          objInParamList)

            objInParamList.Clear()
            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBatchObject)
            Return False
        End Try
    End Function

    Public Function CountHistologysTestItems(ByVal ProjectContractCode As String, _
                                    ByVal SubmittedDateFrom As String, _
                                    ByVal SubmittedDateTo As String, _
                                    ByVal BatchType As Integer, _
                                    ByVal Tests As String, _
                                    ByVal TestCode As String, _
                                    ByRef dsData As DataTable) As Boolean
        Try
            Dim objInParamList As New ParameterList

            With objInParamList
                .QuickAddInputParam("ProjectContractCode", DbtType.dbtString, ProjectContractCode)
                .QuickAddInputParam("SubmittedDateFrom", DbtType.dbtDateTime, FormatEmptyString(SubmittedDateFrom))
                .QuickAddInputParam("SubmittedDateTo", DbtType.dbtDateTime, FormatEmptyString(SubmittedDateTo))
                .QuickAddInputParam("BatchType", DbtType.dbtInteger, BatchType)
                .QuickAddInputParam("Tests", DbtType.dbtString, Tests)
                .QuickAddInputParam("TestCode", DbtType.dbtString, TestCode)
            End With

            FillDataTable("GetTestHistologyCounts", _
                          CommandType.StoredProcedure, _
                          dsData, _
                          objInParamList)

            objInParamList.Clear()
            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBatchObject)
            Return False
        End Try
    End Function

    Public Function CountHistologysTestBatch(ByVal ProjectContractCode As String, _
                                    ByVal SubmittedDateFrom As String, _
                                    ByVal SubmittedDateTo As String, _
                                    ByVal BatchType As Integer, _
                                    ByVal Tests As String, _
                                    ByVal TestCode As String, _
                                    ByRef dsData As DataTable) As Boolean
        Try
            Dim objInParamList As New ParameterList

            With objInParamList
                .QuickAddInputParam("ProjectContractCode", DbtType.dbtString, ProjectContractCode)
                .QuickAddInputParam("SubmittedDateFrom", DbtType.dbtDateTime, FormatEmptyString(SubmittedDateFrom))
                .QuickAddInputParam("SubmittedDateTo", DbtType.dbtDateTime, FormatEmptyString(SubmittedDateTo))
                .QuickAddInputParam("BatchType", DbtType.dbtInteger, BatchType)
                .QuickAddInputParam("Tests", DbtType.dbtString, Tests)
                .QuickAddInputParam("TestCode", DbtType.dbtString, TestCode)
            End With

            FillDataTable("GetTestHistologyBatch", _
                          CommandType.StoredProcedure, _
                          dsData, _
                          objInParamList)

            objInParamList.Clear()
            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBatchObject)
            Return False
        End Try
    End Function

#End Region

#Region "Update Batch Details"

    Public Function UpdateBatchDetails(ByVal iUserID As Integer, _
                                           ByRef dsBatchDetails As DataSet, _
                                           ByRef objErrorList As ArrayList, _
                                           ByVal bBlocks As Boolean, _
                                           ByRef iBatchID As Integer, _
                                           Optional ByVal dtQCNotesID As DataTable = Nothing, _
                                           Optional ByVal bIsPreCassetted As Boolean = False, _
                                           Optional ByVal dtUnusedHistologyRefs As DataTable = Nothing) As Boolean
        If Not dsBatchDetails.HasChanges Then
            Return True
        End If

        Dim objDBConn As SqlConnection = Nothing
        Dim objDBTran As SqlTransaction = Nothing
        Dim dsDataSetBackup As New DataSet

        Try
            dsDataSetBackup = dsBatchDetails.Copy()

            'open a database connection and begin a transaction
            objDBConn = TBCultureDA.OpenConnection()
            objDBTran = TBCultureDA.BeginTransaction(objDBConn)

            'update submission data
            iBatchID = UpdateBatchData(iUserID, dsBatchDetails.Tables(BATCH_TABLE).Rows(0), objDBConn, objDBTran, objErrorList)

            Dim chkbl As New clsCheckBoxData
            chkbl.UpdateTable(dsBatchDetails.Tables(BATCH_HISTOLOGY_TABLE), _
                              objDBConn, _
                              objDBTran, _
                              objErrorList, _
                              iBatchID, _
                              HistopathologyLib.clsBatch.BATCH_HISTOLOGY_TABLE, _
                              iUserID)

            chkbl.UpdateTable(dsBatchDetails.Tables(BATCH_ANTIBODIES_TABLE), _
                              objDBConn, _
                              objDBTran, _
                              objErrorList, _
                              iBatchID, _
                              HistopathologyLib.clsBatch.BATCH_ANTIBODIES_TABLE, _
                              iUserID)

            chkbl.UpdateTable(dsBatchDetails.Tables(BATCH_STAIN_TABLE), _
                             objDBConn, _
                             objDBTran, _
                             objErrorList, _
                             iBatchID, _
                             HistopathologyLib.clsBatch.BATCH_STAIN_TABLE, _
                             iUserID)

            chkbl.UpdateTable(dsBatchDetails.Tables(BATCH_POSTFIXATION_TABLE), _
                             objDBConn, _
                             objDBTran, _
                             objErrorList, _
                             iBatchID, _
                             HistopathologyLib.clsBatch.BATCH_POSTFIXATION_TABLE, _
                             iUserID)

            chkbl.UpdateTable(dsBatchDetails.Tables(BATCH_SUBMITTEDAS_TABLE), _
                             objDBConn, _
                             objDBTran, _
                             objErrorList, _
                             iBatchID, _
                             HistopathologyLib.clsBatch.BATCH_SUBMITTEDAS_TABLE, _
                             iUserID)

            'if the table count is greater than 7 we also need to save either the block, or submission details
            If dsBatchDetails.Tables.Count > 7 Then

                Dim objTissue As New HistopathologyLib.clsTissue
                Dim objQCNotes As New HistopathologyLib.clsQCNote
                Dim objectNewIDs As New ArrayList
                Dim objNewAnimalIDs As New ArrayList
                'if its cassetted then save block details, otherwise save the submission details

                If bBlocks Then
                    Dim objHistologyRefs As New HistopathologyLib.clsHistology
                    Dim objAnimal As New HistopathologyLib.clsAnimal

                    objAnimal.UpdateAnimalDetails(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL), _
                                                  objDBConn, _
                                                  objDBTran, _
                                                  objErrorList, _
                                                  objNewAnimalIDs, _
                                                  iUserID)

                    Dim objBlock As New HistopathologyLib.clsBlock
                    objectNewIDs = objBlock.UpdateBlocks(iBatchID, dsBatchDetails.Tables(BATCH_BLOCK_TABLE), objDBConn, objDBTran, objErrorList, objNewAnimalIDs, iUserID)

                    objTissue.UpdateTissues(dsBatchDetails.Tables(BATCH_BLOCK_TISSUES), objDBConn, objDBTran, objErrorList, objectNewIDs, "BlockID", iUserID)

                    If Not dtQCNotesID Is Nothing Then
                        objQCNotes.UpdateQCNOtes(dtQCNotesID, _
                                            objDBConn, _
                                            objDBTran)

                    End If

                    chkbl.UpdateBlockTables(dsBatchDetails.Tables(BATCH_BLOCK_HISTOLOGY), _
                                                                  objDBConn, _
                                                                  objDBTran, _
                                                                  objErrorList, _
                                                                  objectNewIDs, _
                                                                  BATCH_BLOCK_HISTOLOGY, _
                                                                  iUserID, _
                                                                  iBatchID, _
                                                                  dtQCNotesID)

                    chkbl.UpdateBlockTables(dsBatchDetails.Tables(BATCH_BLOCK_ANTIBODIES), _
                                                                  objDBConn, _
                                                                  objDBTran, _
                                                                  objErrorList, _
                                                                  objectNewIDs, _
                                                                  BATCH_BLOCK_ANTIBODIES, _
                                                                  iUserID, _
                                                                  iBatchID, _
                                                                  dtQCNotesID)

                    chkbl.UpdateBlockTables(dsBatchDetails.Tables(BATCH_BLOCK_STAIN), _
                                                                  objDBConn, _
                                                                  objDBTran, _
                                                                  objErrorList, _
                                                                  objectNewIDs, _
                                                                  BATCH_BLOCK_STAIN, _
                                                                  iUserID, _
                                                                  iBatchID, _
                                                                  dtQCNotesID)

                    chkbl.UpdateTCCodeTable(dsBatchDetails.Tables(BLOCK_ANTIBODIES_TCCODES), objDBConn, objDBTran, objErrorList, BLOCK_ANTIBODIES_TCCODES, iUserID, iBatchID)

                    chkbl.UpdateTCCodeTable(dsBatchDetails.Tables(BLOCK_SPECIALSTAIN_TCCODES), objDBConn, objDBTran, objErrorList, BLOCK_SPECIALSTAIN_TCCODES, iUserID, iBatchID)

                    chkbl.UpdateTCCodeTable(dsBatchDetails.Tables(BLOCK_HISTOLOGY_TCCODES), objDBConn, objDBTran, objErrorList, BLOCK_HISTOLOGY_TCCODES, iUserID, iBatchID)

                    objHistologyRefs.UpdateHistologyRefs(dsBatchDetails.Tables(HISTOLOGY_REFS), objDBConn, objDBTran, objErrorList)

                    If bIsPreCassetted Then
                        If Not dsBatchDetails.Tables.IndexOf("ANIMAL_PREBOOKED_BLOCKS") = -1 Then
                            objBlock.UpdatePreBookedBlocks(dsBatchDetails.Tables(ANIMAL_PREBOOKED_BLOCKS), objDBConn, objDBTran, objErrorList, iBatchID)
                        End If
                    End If

                    If Not dtUnusedHistologyRefs Is Nothing Then
                        objHistologyRefs.SaveUnusedHistologyRef(dtUnusedHistologyRefs, objErrorList, objDBConn, objDBTran)
                    End If
                Else
                    Dim objAnimal As New HistopathologyLib.clsAnimal
                    objAnimal.UpdateAnimalDetails(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE), _
                                                                        objDBConn, _
                                                                        objDBTran, _
                                                                        objErrorList, _
                                                                        objNewAnimalIDs, _
                                                                        iUserID)

                    Dim objBatchSubmission As New HistopathologyLib.clsBatchSubmission

                    objectNewIDs = objBatchSubmission.UpdateBatchSubmission(iBatchID, dsBatchDetails.Tables(BATCH_SUBMISSION_TABLE), objDBConn, objDBTran, objErrorList, objNewAnimalIDs)

                    objTissue.UpdateTissues(dsBatchDetails.Tables(BATCH_TISSUES_TABLE), _
                                            objDBConn, _
                                            objDBTran, _
                                            objErrorList, _
                                            objectNewIDs, _
                                            "BatchSubmissionID", _
                                            iUserID)
                End If
            End If

            'commit the database transaction
            TBCultureDA.CommitTransaction(objDBTran)

        Catch exBatchUpdate As BatchUpdateException
            objErrorList.Add(exBatchUpdate.Message)
            If Not objDBTran Is Nothing Then
                TBCultureDA.RollbackTransaction(objDBTran)
            End If
            clsLog.LogException(exBatchUpdate, clsLog.LogSource.lsStoredProcedure)

            dsBatchDetails.Clear()
            dsBatchDetails = dsDataSetBackup.Copy()
            iBatchID = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("ID")

            Return False
        Catch exAnimalUpdate As AnimalUpdateException
            objErrorList.Add(exAnimalUpdate.Message)
            If Not objDBTran Is Nothing Then
                TBCultureDA.RollbackTransaction(objDBTran)
            End If
            clsLog.LogException(exAnimalUpdate, clsLog.LogSource.lsStoredProcedure)

            dsBatchDetails.Clear()
            dsBatchDetails = dsDataSetBackup.Copy()
            iBatchID = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("ID")

            Return False
        Catch exHistologyRef As HistologyRefUpdateException
            objErrorList.Add(exHistologyRef.Message)
            If Not objDBTran Is Nothing Then
                TBCultureDA.RollbackTransaction(objDBTran)
            End If

            dsBatchDetails.Clear()
            dsBatchDetails = dsDataSetBackup.Copy()

            iBatchID = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("ID")

            Return False
        Catch exTissueUpdate As TissueUpdateException
            objErrorList.Add(exTissueUpdate.Message)
            If Not objDBTran Is Nothing Then
                TBCultureDA.RollbackTransaction(objDBTran)
            End If

            dsBatchDetails.Clear()
            dsBatchDetails = dsDataSetBackup.Copy()
            iBatchID = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("ID")

            Return False
        Catch exQCNoteUpdateException As QCNoteUpdateException
            objErrorList.Add(exQCNoteUpdateException.Message)
            If Not objDBTran Is Nothing Then
                TBCultureDA.RollbackTransaction(objDBTran)
            End If

            dsBatchDetails.Clear()
            dsBatchDetails = dsDataSetBackup.Copy()
            iBatchID = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("ID")

            Return False
        Catch ex As Exception
            If Not objDBTran Is Nothing Then
                TBCultureDA.RollbackTransaction(objDBTran)
            End If
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)

            dsBatchDetails.Clear()
            dsBatchDetails = dsDataSetBackup.Copy()
            iBatchID = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("ID")

            Return False
        Finally

            If Not objDBConn Is Nothing Then
                TBCultureDA.CloseConnection(objDBConn)
            End If

        End Try

        Return True
    End Function

    Private Function UpdateBatchData(ByVal iUserID As Integer, ByRef drBatchRow As DataRow, ByRef objDBConn As SqlConnection, ByRef objDBTran As SqlTransaction, ByRef objErrorList As ArrayList) As Integer
        If drBatchRow.RowState <> DataRowState.Added And drBatchRow.RowState <> DataRowState.Modified Then
            Return Convert.ToInt32(drBatchRow("ID"))
        End If

        Dim objSubmissionParamList As New libDataAccess.libDataAccess.ParameterList

        With objSubmissionParamList
            .AddParameter("RETURN_VALUE", DbtType.dbtInteger, "RETURN_VALUE", daDirection:=ParameterDirection.ReturnValue)
            If drBatchRow.RowState = DataRowState.Modified Then
                .QuickAddInputParam("ID", DbtType.dbtInteger, drBatchRow.Item("ID"))
                .QuickAddInputParam("DateReceived", DbtType.dbtDate, drBatchRow.Item("DateReceived"))
                .QuickAddInputParam("TimeReceived", DbtType.dbtString, drBatchRow.Item("TimeReceived"))
                .QuickAddInputParam("ReceivedBy", DbtType.dbtInteger, drBatchRow.Item("ReceivedBy"))
                .QuickAddInputParam("StatusComments", DbtType.dbtString, drBatchRow.Item("StatusComments"))
                .QuickAddInputParam("PostFixationOther", DbtType.dbtString, drBatchRow.Item("PostFixationOther"))
                .QuickAddInputParam("UserID", DbtType.dbtInteger, iUserID)
                .QuickAddInputParam("DateCompleted", DbtType.dbtDateTime, drBatchRow.Item("DateCompleted"))
                .QuickAddInputParam("AllTissuesAssigned", DbtType.dbtBoolean, drBatchRow.Item("AllTissuesAssigned"))
                .QuickAddInputParam("RowStamp", DbtType.dbtBinary, drBatchRow.Item("RowStamp"))
            End If
            .QuickAddInputParam("ProjectContractCode", DbtType.dbtInteger, drBatchRow.Item("ProjectContractCode"))
            .QuickAddInputParam("ContactName", DbtType.dbtInteger, drBatchRow.Item("ContactName"))
            .QuickAddInputParam("Species", DbtType.dbtString, drBatchRow.Item("Species"))
            .QuickAddInputParam("BatchDate", DbtType.dbtDateTime, drBatchRow.Item("BatchDate"))
            .QuickAddInputParam("BatchType", DbtType.dbtInteger, drBatchRow.Item("BatchType"))
            .QuickAddInputParam("SubmittedBy", DbtType.dbtInteger, drBatchRow.Item("SubmittedBy"))
            .QuickAddInputParam("SafeToHandle", DbtType.dbtBoolean, drBatchRow.Item("SafeToHandle"))
            .QuickAddInputParam("BatchStatus", DbtType.dbtInteger, drBatchRow.Item("BatchStatus"))
            .QuickAddInputParam("OtherSubmittedArea", DbtType.dbtString, drBatchRow.Item("OtherSubmittedArea"))
            .QuickAddInputParam("OtherSubmittedBy", DbtType.dbtInteger, drBatchRow.Item("OtherSubmittedBy"))
            .QuickAddInputParam("Fixation", DbtType.dbtString, drBatchRow.Item("Fixation"))
            .QuickAddInputParam("Cassetted", DbtType.dbtBoolean, drBatchRow.Item("Cassetted"))
            .QuickAddInputParam("Comments", DbtType.dbtString, drBatchRow.Item("Comments"))
            .QuickAddInputParam("IsBlocked", DbtType.dbtBoolean, drBatchRow.Item("IsBlocked"))
            .QuickAddInputParam("CustomerReceivedDate", DbtType.dbtDateTime, drBatchRow.Item("CustomerReceivedDate"))
            .QuickAddInputParam("SubmittedArea", DbtType.dbtString, drBatchRow.Item("SubmittedArea"))
            .QuickAddInputParam("SampleSameProjects", DbtType.dbtBoolean, drBatchRow.Item("SampleSameProjects"))
            If drBatchRow.RowState = DataRowState.Added Then
                .AddParameter("BatchID", DbtType.dbtInteger, "@BatchID", "ID", , , ParameterDirection.Output)
            End If
            .QuickAddInputParam("ByPassSort", DbtType.dbtBoolean, drBatchRow.Item("ByPassSort"))
        End With

        Select Case drBatchRow.RowState
            Case DataRowState.Added
                Try
                    TBCultureDA.ExecuteNonQuery(objDBConn, objDBTran, "AddBatch", CommandType.StoredProcedure, objSubmissionParamList)
                    Dim iBatchID As Integer
                    iBatchID = objSubmissionParamList("BatchID").Value()

                    Dim iReturnValue As Integer = CInt(objSubmissionParamList("RETURN_VALUE").Value)
                    Select Case iReturnValue
                        Case 1
                            Throw New BatchUpdateException("Another user has altered the Batch Record.")
                    End Select

                    Return iBatchID
                Catch ex As Exception
                    clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
                    Throw New BatchUpdateException(ex.Message, ex.InnerException)
                End Try


            Case DataRowState.Modified
                Try
                    TBCultureDA.ExecuteNonQuery(objDBConn, objDBTran, "EditBatch", CommandType.StoredProcedure, objSubmissionParamList)

                    Dim iReturnValue As Integer = CInt(objSubmissionParamList("RETURN_VALUE").Value)
                    Select Case iReturnValue
                        Case 1
                            Throw New BatchUpdateException("Another user has altered the Batch Record.")
                    End Select

                    Return drBatchRow("ID")
                Catch ex As Exception
                    clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
                    Throw New BatchUpdateException(ex.Message, ex.InnerException)
                End Try
        End Select

    End Function

    'Public Function ChangeBatchStatus(ByVal iBatchID As Integer, _
    '                                  ByRef dsBatchDetails As DataSet, _
    '                                  ByRef objErrorlist As ArrayList, _
    '                                  ByVal iUserID As Integer) As Boolean

    '    Dim objDBConn As SqlConnection = Nothing
    '    Dim objDBTran As SqlTransaction = Nothing

    '    Try
    '        'open a database connection and begin a transaction
    '        objDBConn = TBCultureDA.OpenConnection()
    '        objDBTran = TBCultureDA.BeginTransaction(objDBConn)

    '        Dim objChkBoxList As New HistopathologyLib.clsCheckBoxData()

    '        'open a database connection and begin a transaction
    '        UpdateBatchData(iBatchID, _
    '                        dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0), _
    '                        objDBConn, _
    '                        objDBTran, _
    '                        objErrorlist)

    '        objChkBoxList.UpdateTable(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_POSTFIXATION_TABLE), _
    '                                  objDBConn, _
    '                                  objDBTran, _
    '                                  objErrorlist, _
    '                                  iBatchID, _
    '                                  HistopathologyLib.clsBatch.BATCH_POSTFIXATION_TABLE, _
    '                                  iUserID)

    '        'commit the database transaction
    '        TBCultureDA.CommitTransaction(objDBTran)

    '    Catch exBatchUpdate As BatchUpdateException
    '        objErrorlist.Add(exBatchUpdate.Message)
    '        If Not objDBTran Is Nothing Then
    '            TBCultureDA.RollbackTransaction(objDBTran)
    '        End If
    '        clsLog.LogException(exBatchUpdate, clsLog.LogSource.lsStoredProcedure)
    '        Return False
    '    Catch ex As Exception
    '        If Not objDBTran Is Nothing Then
    '            TBCultureDA.RollbackTransaction(objDBTran)
    '        End If
    '        clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
    '        Return False
    '    Finally
    '        If Not objDBConn Is Nothing Then
    '            TBCultureDA.CloseConnection(objDBConn)
    '        End If
    '    End Try
    '    Return True
    'End Function

#End Region

#Region "DataTable Handling"

    Public Function NewBatch(ByRef dtBatch As DataTable, ByRef iBatchID As Integer) As Boolean
        Try
            Dim dr As DataRow
            dr = dtBatch.NewRow()
            iBatchID = dr("ID")
            dtBatch.Rows.InsertAt(dr, 0)

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBatchObject)
            Return False
        End Try
    End Function

    Public Function CopyDataToNewBatch(ByVal dtOldBatch As DataTable, _
                                       ByRef dtNewBatch As DataTable, _
                                       ByRef iBatchID As Integer, _
                                       ByVal iUserArea As Integer, _
                                       ByVal iUserID As Integer) As Boolean
        Try
            Dim newDate As System.DateTime
            Dim dr As DataRow
            dr = dtNewBatch.NewRow()
            iBatchID = dr("ID")

            dr("ProjectContractCode") = dtOldBatch.Rows(0)("ProjectContractCode")
            dr("ContactName") = dtOldBatch.Rows(0)("ContactName")
            dr("Species") = dtOldBatch.Rows(0)("Species")
            dr("BatchDate") = newDate.Now.ToShortDateString
            dr("BatchType") = dtOldBatch.Rows(0)("BatchType")
            dr("SubmittedBy") = iUserID
            dr("SafeToHandle") = dtOldBatch.Rows(0)("SafeToHandle")
            dr("BatchStatus") = STATUS_SUBMITTED
            dr("DateReceived") = DBNull.Value
            dr("TimeReceived") = DBNull.Value
            dr("ReceivedBy") = DBNull.Value
            dr("OtherSubmittedBy") = dtOldBatch.Rows(0)("OtherSubmittedBy")
            dr("OtherSubmittedArea") = dtOldBatch.Rows(0)("OtherSubmittedArea")
            dr("Cassetted") = dtOldBatch.Rows(0)("Cassetted")
            dr("Fixation") = dtOldBatch.Rows(0)("Fixation")
            dr("IsBlocked") = False
            dr("PostFixationOther") = dtOldBatch.Rows(0)("PostFixationOther")
            dr("Comments") = dtOldBatch.Rows(0)("Comments")
            dr("StatusComments") = dtOldBatch.Rows(0)("StatusComments")
            dr("SubmittedArea") = iUserArea
            dr("SampleSameProjects") = dtOldBatch.Rows(0)("SampleSameProjects")
            dr("ByPassSort") = dtOldBatch.Rows(0)("ByPassSort")
            dtNewBatch.Rows.InsertAt(dr, 0)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBatchObject)
            Return False
        End Try
    End Function
#End Region

#Region "Copy Batch"

    Public Function CopyBatch(ByVal dsOldBatch As DataSet, _
                              ByRef dsNewBatch As DataSet, _
                              ByVal bBlocked As Boolean, _
                              ByVal objAnimalIDs As ArrayList, _
                              ByVal iUserArea As Integer, _
                              ByVal iUserID As Integer, _
                              ByVal bPreCassetted As Boolean) As Boolean
        Try
            Dim objCheckBoxListData As New HistopathologyLib.clsCheckBoxData
            Dim objBatchSubmission As New HistopathologyLib.clsBatchSubmission
            Dim objTissues As New HistopathologyLib.clsTissue
            Dim objBlocks As New HistopathologyLib.clsBlock
            Dim iBatchID As Integer = 0
            Dim objBatchSubmissionIDs As New ArrayList
            Dim objBlockIDs As New ArrayList

            'Copy the batch
            If Not CopyDataToNewBatch(dsOldBatch.Tables(HistopathologyLib.clsBatch.BATCH_TABLE), _
                                               dsNewBatch.Tables(HistopathologyLib.clsBatch.BATCH_TABLE), _
                                               iBatchID, iUserArea, iUserID) Then
                Throw New Exception("Batch.CopyDataToNewBatch returned false.")
            End If

            'Copy the Batch Histology data
            If Not objCheckBoxListData.CopyDataToNewBatch(dsOldBatch.Tables(HistopathologyLib.clsBatch.BATCH_HISTOLOGY_TABLE), _
                                                   dsNewBatch.Tables(HistopathologyLib.clsBatch.BATCH_HISTOLOGY_TABLE), _
                                                   iBatchID) Then
                Throw New Exception("CheckBoxData.CopyDataToNewBatch returned false.")
            End If

            'Copy the Batch Antibodies
            If Not objCheckBoxListData.CopyDataToNewBatch(dsOldBatch.Tables(HistopathologyLib.clsBatch.BATCH_ANTIBODIES_TABLE), _
                                                  dsNewBatch.Tables(HistopathologyLib.clsBatch.BATCH_ANTIBODIES_TABLE), _
                                                  iBatchID) Then
                Throw New Exception("CheckBoxData.CopyDataToNewBatch returned false.")
            End If

            'Copy the Batch special stain
            If Not objCheckBoxListData.CopyDataToNewBatch(dsOldBatch.Tables(HistopathologyLib.clsBatch.BATCH_STAIN_TABLE), _
                                                  dsNewBatch.Tables(HistopathologyLib.clsBatch.BATCH_STAIN_TABLE), _
                                                  iBatchID) Then
                Throw New Exception("CheckBoxData.CopyDataToNewBatch returned false.")
            End If

            'Copy the Batch post fixation 
            If Not objCheckBoxListData.CopyDataToNewBatch(dsOldBatch.Tables(HistopathologyLib.clsBatch.BATCH_POSTFIXATION_TABLE), _
                                                  dsNewBatch.Tables(HistopathologyLib.clsBatch.BATCH_POSTFIXATION_TABLE), _
                                                  iBatchID) Then
                Throw New Exception("CheckBoxData.CopyDataToNewBatch returned false.")
            End If

            'Copy the Batch submitted as
            If Not objCheckBoxListData.CopyDataToNewBatch(dsOldBatch.Tables(HistopathologyLib.clsBatch.BATCH_SUBMITTEDAS_TABLE), _
                                                  dsNewBatch.Tables(HistopathologyLib.clsBatch.BATCH_SUBMITTEDAS_TABLE), _
                                                  iBatchID) Then
                Throw New Exception("CheckBoxData.CopyDataToNewBatch returned false.")
            End If


            'If its not been blocked save the tissue details etc
            If Not bBlocked Then
                If Not objBatchSubmission.CopyDataToNewBatch(dsOldBatch.Tables(HistopathologyLib.clsBatch.BATCH_SUBMISSION_TABLE), _
                                                            dsNewBatch.Tables(HistopathologyLib.clsBatch.BATCH_SUBMISSION_TABLE), _
                                                            iBatchID, _
                                                            objAnimalIDs, _
                                                            objBatchSubmissionIDs) Then
                    Throw New Exception("BatchSubmission.CopyDataToNewBatch returned false.")
                End If

                If Not objTissues.CopyDataToNewBatch(dsOldBatch.Tables(HistopathologyLib.clsBatch.BATCH_TISSUES_TABLE), _
                                                    dsNewBatch.Tables(HistopathologyLib.clsBatch.BATCH_TISSUES_TABLE), _
                                                    objBatchSubmissionIDs, _
                                                    False) Then
                    Throw New Exception("Tissues.CopyDataToNewBatch returned false.")
                End If
                'if it has been blocked save the block details
            Else
                If bPreCassetted Then
                    If Not objBlocks.CopyDataToNewBatchBookedBlocks(dsOldBatch.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE), _
                                                                    dsNewBatch.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE), _
                                                                    iBatchID, _
                                                                    objAnimalIDs, _
                                                                    objBlockIDs, _
                                                                    dsNewBatch.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL), _
                                                                    True, _
                                                                    dsOldBatch) Then
                        Throw New Exception("Block.CopyDataToNewBatch returned false.")
                    End If
                Else
                    If Not objBlocks.CopyDataToNewBatch(dsOldBatch.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE), _
                                                    dsNewBatch.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE), _
                                                    iBatchID, _
                                                    objAnimalIDs, _
                                                    objBlockIDs, _
                                                    dsNewBatch.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL)) Then
                        Throw New Exception("Block.CopyDataToNewBatch returned false.")
                    End If
                End If
                If Not objTissues.CopyDataToNewBatch(dsOldBatch.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TISSUES), _
                                                    dsNewBatch.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TISSUES), _
                                                    objBlockIDs, _
                                                    True) Then
                    Throw New Exception("Tissues.CopyDataToNewBatch returned false.")
                End If

                'Copy the Block Histology data
                If Not objCheckBoxListData.CopyDataToNewBatch(dsOldBatch.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_HISTOLOGY), _
                                                    dsNewBatch.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_HISTOLOGY), _
                                                    iBatchID, _
                                                    objBlockIDs) Then
                    Throw New Exception("CheckBoxData.CopyDataToNewBatch returned false.")
                End If

                'Copy the Block Antibodies data
                If Not objCheckBoxListData.CopyDataToNewBatch(dsOldBatch.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANTIBODIES), _
                                                    dsNewBatch.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANTIBODIES), _
                                                    iBatchID, _
                                                    objBlockIDs) Then
                    Throw New Exception("CheckBoxData.CopyDataToNewBatch returned false.")
                End If

                'Copy the Block Antibodies data
                If Not objCheckBoxListData.CopyDataToNewBatch(dsOldBatch.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_STAIN), _
                                                    dsNewBatch.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_STAIN), _
                                                    iBatchID, _
                                                    objBlockIDs) Then
                    Throw New Exception("CheckBoxData.CopyDataToNewBatch returned false.")
                End If
            End If

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBatchObject)
            Return False
        End Try

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
#End Region

End Class
