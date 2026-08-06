Imports libDataAccess.libDataAccess
Imports libDataAccess.libDataAccess.TBCultureDA
Imports System.Text.RegularExpressions

Public Class HistologyRefAscSort : Implements IComparer
    Function Compare(ByVal Obj1 As Object, ByVal Obj2 As Object) As Integer Implements IComparer.Compare
        Dim drRow1 As DataRow = CType(Obj1, DataRow)
        Dim drRow2 As DataRow = CType(Obj2, DataRow)

        Dim sHistologyRef1 As String = drRow1("HistologyRef").ToString()
        Dim sHistologyRef2 As String = drRow2("HistologyRef").ToString()
        Dim sSenderRef1 As String = drRow1("SenderRef").ToString()
        Dim sSenderRef2 As String = drRow2("SenderRef").ToString()
        Dim iCompareResult As Integer
        Dim bBlankvalue As Boolean = False
        iCompareResult = String.Compare(sHistologyRef1, sHistologyRef2)

        If sHistologyRef1 = "" Or sHistologyRef2 = "" Then
            bBlankvalue = True
        End If

        If iCompareResult = -1 Then
            If bBlankvalue Then
                Return 1
            Else
                Return -1
            End If
        ElseIf iCompareResult = 1 Then
            If bBlankvalue Then
                Return -1
            Else
                Return 1
            End If
        Else
            If sHistologyRef1 = "" And sHistologyRef2 = "" Then
                iCompareResult = String.Compare(sSenderRef1, sSenderRef2)

                If iCompareResult = -1 Then
                    Return -1
                ElseIf iCompareResult = 1 Then
                    Return 1
                Else
                    Return 0
                End If
            Else
                Return 0
            End If
        End If
    End Function
End Class

Public Class CustomBlockRefAscSort : Implements IComparer
    Function Compare(ByVal Obj1 As Object, ByVal Obj2 As Object) As Integer Implements IComparer.Compare
        ' Take the two objects and convert into strings 
        Dim drRow1 As DataRow = CType(Obj1, DataRow)
        Dim drRow2 As DataRow = CType(Obj2, DataRow)

        Dim iBlockRef1 As Integer = 0
        Dim iBlockRef2 As Integer = 0

        If Not IsDBNull(drRow1("BlockRef")) Then
            iBlockRef1 = CInt(drRow1("BlockRef"))
        End If

        If Not IsDBNull(drRow2("BlockRef")) Then
            iBlockRef2 = CInt(drRow2("BlockRef"))
        End If

        If (iBlockRef1 > iBlockRef2) Then
            Return 1
        End If

        If (iBlockRef1 < iBlockRef2) Then
            Return -1
        Else
            Return 0
        End If
    End Function
End Class

Public Class CustomTissuesAscSort : Implements IComparer

    Public Function GetListType(ByVal sCode As String, ByVal lookuplist As Integer) As String

        Dim dt As DataTable = GetLookupTypeList(lookuplist)

        If Not dt Is Nothing Then
            Dim dv As New DataView(dt, "", "Code", DataViewRowState.CurrentRows)
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
        Dim objLookup As New HistopathologyLib.LookupData
        Dim dt As DataTable = objLookup.GetLookupData(lookuplist)

        If dt Is Nothing Then
            Throw New Exception("LookupData.GetLookupData returned Nothing")
        End If

        Return dt
    End Function

    Function Compare(ByVal Obj1 As Object, ByVal Obj2 As Object) As Integer Implements IComparer.Compare
        ' Take the two objects and convert into strings 
        Dim drRow1 As DataRow = CType(Obj1, DataRow)
        Dim drRow2 As DataRow = CType(Obj2, DataRow)

        Try
            Dim sTissueCode1 As String = GetListType(drRow1("TissueCode"), 9) 'Tissue
            Dim sTissueCode2 As String = GetListType(drRow2("TissueCode"), 9) 'Tissue

            Return String.Compare(sTissueCode1, sTissueCode2)
        Catch ex As Exception
            Return 0
        End Try

    End Function
End Class

Public Class clsBatchSummary

    Public Function CreateBatchSummaryData(ByRef dsDataSet As DataSet, ByRef dtSummary As DataTable, ByRef dtTissuesList As DataTable) As Boolean
        Try
            Dim drTissueRow As DataRow
            Dim drNewSummaryRow As DataRow
            Dim drAnimalRow As DataRow
            Dim drOrderedRow As DataRow
            Dim drBatchSubmissions As DataRow()
            Dim drBatchSubmission As DataRow
            Dim drOrderedRows As DataRow()
            Dim drAnimalTissues As DataRow()
            Dim bByPassSort As Boolean = False
            Dim sColumnName As String

            With dtSummary
                .Columns.Add("ID", System.Type.GetType("System.Int32"))
                .Columns.Add("SenderRef", System.Type.GetType("System.String"))
                .Columns.Add("HistologyRef", System.Type.GetType("System.String"))
                .Columns.Add("AnimalID", System.Type.GetType("System.Int32"))
                .Columns.Add("TissueDetails", System.Type.GetType("System.String"))
            End With

            'CreateSortedTissueAnimalTable(dsDataSet)
            bByPassSort = dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("ByPassSort")

            'Go through all the animals and get the related tissues
            If bByPassSort Then
                drOrderedRows = dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_SUBMISSION_TABLE).Select("", "Order ASC")
                sColumnName = "AnimalID"
            Else
                drOrderedRows = dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE).Select("", "SenderRef ASC")
                Array.Sort(drOrderedRows, New HistologyRefAscSort)
                sColumnName = "ID"
            End If

            For Each drOrderedRow In drOrderedRows
                drAnimalRow = dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE).Rows.Find(drOrderedRow(sColumnName))

                drNewSummaryRow = dtSummary.NewRow()
                drNewSummaryRow("SenderRef") = drAnimalRow("SenderRef").ToString()
                drNewSummaryRow("HistologyRef") = drAnimalRow("HistologyRef").ToString()
                drNewSummaryRow("AnimalID") = drAnimalRow("ID")

                drBatchSubmissions = drAnimalRow.GetChildRows("ANIMAL_BATCHSUBMISSION")
                For Each drBatchSubmission In drBatchSubmissions
                    drNewSummaryRow("ID") = drBatchSubmission("ID")
                    dtSummary.Rows.Add(drNewSummaryRow)

                    drAnimalTissues = drBatchSubmission.GetChildRows("BATCHSUBMISSION_BATCHTISSUES")
                    'Array.Sort(drAnimalTissues, New CustomTissuesAscSort())

                    For Each drTissueRow In drAnimalTissues
                        drNewSummaryRow = dtSummary.NewRow()
                        drNewSummaryRow("TissueDetails") = drTissueRow("NoPieces") & " x " & LookupDescription(dtTissuesList, drTissueRow("TissueCode"))
                        dtSummary.Rows.Add(drNewSummaryRow)
                    Next
                Next
            Next

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBatchSummaryObject)
            Return False
        End Try
    End Function

    Public Function CreateBlockSummaryData(ByRef dsDataSet As DataSet, ByRef dtSummary As DataTable, ByRef dtTissuesList As DataTable, Optional ByVal sFilter As String = "") As Boolean
        Try
            Dim drTissueRow As DataRow
            Dim drBlockRows As DataRow()
            Dim drTissuesRows As DataRow()
            Dim drNewSummaryRow As DataRow
            Dim drRow As DataRow
            Dim drAnimalRow As DataRow
            Dim drBlock As DataRow
            Dim dtBatchBlock As DataTable = dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE)
            Dim drAnimalRows As DataRow()

            With dtSummary
                .Columns.Add("ID", System.Type.GetType("System.Int32"))
                .Columns.Add("SenderRef", System.Type.GetType("System.String"))
                .Columns.Add("HistologyRef", System.Type.GetType("System.String"))
                .Columns.Add("AnimalID", System.Type.GetType("System.Int32"))
                .Columns.Add("NextBlockRef", System.Type.GetType("System.String"))
                .Columns.Add("TissueDetails", System.Type.GetType("System.String"))
                .Columns.Add("BlockRef", System.Type.GetType("System.String"))
                .Columns.Add("EO", System.Type.GetType("System.Boolean"))
                .Columns.Add("HAndE", System.Type.GetType("System.Boolean"))
                .Columns.Add("HAndEBSE", System.Type.GetType("System.Boolean"))
                .Columns.Add("SpecialStain", System.Type.GetType("System.Boolean"))
                .Columns.Add("IHCPrp", System.Type.GetType("System.Boolean"))
                .Columns.Add("IHCOther", System.Type.GetType("System.Boolean"))
                .Columns.Add("RepeatBlock", System.Type.GetType("System.Boolean"))
                .Columns.Add("NewID", System.Type.GetType("System.Int32"))
                .Columns.Add("Archive", System.Type.GetType("System.Boolean"))
            End With

            SetPrimaryKey(dtSummary, "NewID", True)

            If sFilter <> "" Then
                drBlockRows = dtBatchBlock.Select(sFilter)
                Array.Sort(drBlockRows, New CustomBlockRefAscSort)
            Else
                drBlockRows = dtBatchBlock.Select("", "BlockRef ASC")
                Array.Sort(drBlockRows, New CustomBlockRefAscSort)
            End If

            drAnimalRows = dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL).Select("", "SenderRef ASC")
            Array.Sort(drAnimalRows, New HistologyRefAscSort)

            If Not drBlockRows Is Nothing Then
                For Each drAnimalRow In drAnimalRows
                    For Each drBlock In drBlockRows
                        If drBlock("AnimalID") = drAnimalRow("ID") Then
                            drNewSummaryRow = dtSummary.NewRow()
                            drNewSummaryRow("SenderRef") = drAnimalRow("SenderRef")
                            drNewSummaryRow("AnimalID") = drAnimalRow("ID")
                            drNewSummaryRow("HistologyRef") = drAnimalRow("HistologyRef")
                            drNewSummaryRow("NextBlockRef") = drAnimalRow("NextBlockRef")
                            drNewSummaryRow("ID") = drBlock("ID")
                            drNewSummaryRow("BlockRef") = drBlock("BlockRef")
                            drNewSummaryRow("RepeatBlock") = drBlock("RepeatBlock")

                            For Each drRow In drBlock.GetChildRows("BLOCK_HISTOLOGY")
                                Select Case drRow("Code")
                                    Case 1 'EO
                                        drNewSummaryRow("EO") = True
                                    Case 2 'H&E
                                        drNewSummaryRow("HAndE") = True
                                    Case 3 'Special Stain
                                        drNewSummaryRow("SpecialStain") = True
                                    Case 4 ' IHC - Prp
                                        drNewSummaryRow("IHCPrp") = True
                                    Case 5 'H&E (Bse)
                                        drNewSummaryRow("HAndEBSE") = True
                                    Case 6 'IHC Other
                                        drNewSummaryRow("IHCOther") = True
                                    Case 7 'Archive
                                        drNewSummaryRow("Archive") = True
                                    Case Else
                                        'Do nothing
                                End Select
                            Next
                            dtSummary.Rows.Add(drNewSummaryRow)

                            drTissuesRows = drBlock.GetChildRows("BLOCK_TISSUES")
                            Array.Sort(drTissuesRows, New CustomTissuesAscSort)
                            For Each drTissueRow In drTissuesRows
                                drNewSummaryRow = dtSummary.NewRow()
                                drNewSummaryRow("TissueDetails") = drTissueRow("NoPieces") & " x " & LookupDescription(dtTissuesList, drTissueRow("TissueCode"))
                                dtSummary.Rows.Add(drNewSummaryRow)
                            Next
                        End If
                    Next
                Next
            End If

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBatchSummaryObject)
            Return False
        End Try
    End Function

    Public Function CreateAnimalSummaryData(ByRef dsDataSet As DataSet, ByRef dtSummary As DataTable, ByRef dtTissuesList As DataTable, Optional ByVal sFilter As String = "") As Boolean
        Try
            Dim drBlockSubRow As DataRow
            Dim drTissueRow As DataRow
            Dim drAnimalRow As DataRow()
            Dim drBlockRows As DataRow()
            Dim drTissuesRows As DataRow()
            Dim drNewSummaryRow As DataRow
            Dim drRow As DataRow
            Dim dtBatchBlock As DataTable = dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE)

            Dim dcBlockRef As New DataColumn
            dcBlockRef.DataType = System.Type.GetType("System.String")
            dcBlockRef.ColumnName = "BlockRef"
            dcBlockRef.Expression = "Convert(BlockRef, 'System.Int32')"

            With dtSummary
                .Columns.Add("ID", System.Type.GetType("System.Int32"))
                .Columns.Add("TissueDetails", System.Type.GetType("System.String"))
                .Columns.Add("BlockRef", System.Type.GetType("System.String"))
                .Columns.Add("EO", System.Type.GetType("System.Boolean"))
                .Columns.Add("HAndE", System.Type.GetType("System.Boolean"))
                .Columns.Add("HAndEBSE", System.Type.GetType("System.Boolean"))
                .Columns.Add("SpecialStain", System.Type.GetType("System.Boolean"))
                .Columns.Add("IHCPrp", System.Type.GetType("System.Boolean"))
                .Columns.Add("IHCOther", System.Type.GetType("System.Boolean"))
                .Columns.Add("Archive", System.Type.GetType("System.Boolean"))
                .Columns.Add("Selected", System.Type.GetType("System.Boolean"))
            End With

            If sFilter <> "" Then
                drBlockRows = dtBatchBlock.Select(sFilter, "BlockRef")
                Array.Sort(drBlockRows, New CustomBlockRefAscSort)
            Else
                drBlockRows = dtBatchBlock.Select("", "BlockRef ASC")
            End If

            'Create the batch summary grid datatable from the batchsubmission and batchtissues tables
            If Not drBlockRows Is Nothing Then
                For Each drBlockSubRow In drBlockRows
                    drNewSummaryRow = dtSummary.NewRow()
                    drNewSummaryRow("ID") = drBlockSubRow("ID")
                    drNewSummaryRow("BlockRef") = drBlockSubRow("BlockRef")

                    For Each drRow In drBlockSubRow.GetChildRows("BLOCK_HISTOLOGY")
                        Select Case drRow("Code")
                            Case 1 'EO
                                drNewSummaryRow("EO") = True
                            Case 2 'H&E
                                drNewSummaryRow("HAndE") = True
                            Case 3 'Special Stain
                                drNewSummaryRow("SpecialStain") = True
                            Case 4 ' IHC - Prp
                                drNewSummaryRow("IHCPrp") = True
                            Case 5 'H&E (Bse)
                                drNewSummaryRow("HAndEBSE") = True
                            Case 6 'IHC Other
                                drNewSummaryRow("IHCOther") = True
                            Case 7 ' Archive
                                drNewSummaryRow("Archive") = True
                            Case Else
                                'Do nothing
                        End Select
                    Next

                    dtSummary.Rows.Add(drNewSummaryRow)
                    drTissuesRows = drBlockSubRow.GetChildRows("BLOCK_TISSUES")
                    Array.Sort(drTissuesRows, New CustomTissuesAscSort)
                    For Each drTissueRow In drTissuesRows
                        drNewSummaryRow = dtSummary.NewRow()
                        drNewSummaryRow("TissueDetails") = drTissueRow("NoPieces") & " x " & LookupDescription(dtTissuesList, drTissueRow("TissueCode"))
                        dtSummary.Rows.Add(drNewSummaryRow)
                    Next
                Next
            End If

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBatchSummaryObject)
            Return False
        End Try
    End Function

    Public Function CreateTestSummaryData(ByRef dsDataSet As DataSet, ByRef dtTest As DataTable, ByRef dtTestList As DataTable, ByRef dtHistologyRefList As DataTable) As Boolean
        Try
            Dim drBlockRow As DataRow
            Dim drAnimalRow As DataRow
            Dim drTestRows As DataRow()
            Dim dtluStain As DataTable
            Dim dtluAntibodies As DataTable
            Dim drNewSummaryRow As DataRow
            Dim dr As DataRow
            Dim objDataTable As DataTable
            Dim objLookup As New HistopathologyLib.LookupData
            Dim bOnHold As Boolean = False
            Dim drAnimalRows As DataRow()
            Dim drBlocks As DataRow()
            Dim bArchived As Boolean = False
            Dim bBypassSort As Boolean = False
            Dim sHistologyRef As String
            Dim drOrderedRows As DataRow()
            Dim drOrderedRow As DataRow

            bBypassSort = dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("ByPassSort")

            dtHistologyRefList = New DataTable
            dtTestList = New DataTable

            With dtHistologyRefList
                .Columns.Add("ID", System.Type.GetType("System.Int32"))
                .Columns.Add("Description", System.Type.GetType("System.String"))
            End With

            With dtTestList
                .Columns.Add("ID", System.Type.GetType("System.Int32"))
                .Columns.Add("Description", System.Type.GetType("System.String"))
            End With

            SetPrimaryKey(dtHistologyRefList, "ID", True)
            SetPrimaryKey(dtTestList, "ID", True)

            'Check if this is a TSE or Non-TSE Batch
            Dim iBatchType As Integer = dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("BatchType")
            If iBatchType = 0 Then
                dtluAntibodies = objLookup.GetLookupData(4)
            Else
                dtluAntibodies = objLookup.GetLookupData(5)
            End If

            If dtluAntibodies Is Nothing Then
                Throw New Exception("Unable to retreive Antibodies lookup data")
            End If

            dtluStain = objLookup.GetLookupData(6) 'Special Stain
            If dtluStain Is Nothing Then
                Throw New Exception("Unable to retreive Stain lookup data")
            End If

            With dtTest
                .Columns.Add("ID", System.Type.GetType("System.Int32"))
                .Columns.Add("BlockRef", System.Type.GetType("System.String"))
                .Columns.Add("HistologyRef", System.Type.GetType("System.String"))
                .Columns.Add("TestID", System.Type.GetType("System.Int32"))
                .Columns.Add("TestType", System.Type.GetType("System.String"))
                .Columns.Add("TestDetails", System.Type.GetType("System.String"))
                .Columns.Add("Result", System.Type.GetType("System.String"))
                .Columns.Add("Passed", System.Type.GetType("System.Boolean"))
                .Columns.Add("Failed", System.Type.GetType("System.Boolean"))
                .Columns.Add("QCCode", System.Type.GetType("System.String"))
                .Columns.Add("QCNote", System.Type.GetType("System.Boolean"))
                .Columns.Add("QCNoteRef", System.Type.GetType("System.Int32"))
                .Columns.Add("StainRef", System.Type.GetType("System.String"))
                .Columns.Add("Dispatched", System.Type.GetType("System.Boolean"))
                .Columns.Add("DispatchedDate", System.Type.GetType("System.DateTime"))
                .Columns.Add("DispatchedBy", System.Type.GetType("System.String"))
                .Columns.Add("PremiumCharge", System.Type.GetType("System.String"))
                .Columns.Add("DispatchedTo", System.Type.GetType("System.String"))
                .Columns.Add("EnteredBy", System.Type.GetType("System.Int32"))
                .Columns.Add("Comment", System.Type.GetType("System.String"))
                .Columns.Add("RemedialAction", System.Type.GetType("System.String"))
                .Columns.Add("OnHold", System.Type.GetType("System.Boolean"))
                .Columns.Add("ArchiveLocation", System.Type.GetType("System.String"))
                .Columns.Add("ArchivedDate", System.Type.GetType("System.DateTime"))
                .Columns.Add("ArchiveComment", System.Type.GetType("System.String"))
                .Columns.Add("NumberOfSlides", System.Type.GetType("System.Int32"))
                .Columns.Add("Selected", System.Type.GetType("System.Boolean"))
                .Columns.Add("Archived", System.Type.GetType("System.Boolean"))
            End With

            SetPrimaryKey(dtTest, "ID", True)

            If bBypassSort = True Then
                drOrderedRows = GetUniqueRows(dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE))
            Else
                drOrderedRows = dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL).Select("", "SenderRef ASC")
            End If

            drBlocks = dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE).Select()
            Array.Sort(drBlocks, New CustomBlockRefAscSort)

            For Each drOrderedRow In drOrderedRows
                drAnimalRow = dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL).Rows.Find(drOrderedRow("ID"))
                For Each drBlockRow In drBlocks
                    If drBlockRow("AnimalID") = drAnimalRow("ID") Then
                        sHistologyRef = drAnimalRow("HistologyRef").ToString()

                        If Not IsDBNull(drAnimalRow("OnHold")) Then
                            bOnHold = drAnimalRow("OnHold")
                        Else
                            bOnHold = False
                        End If

                        'Get the Histology for this block
                        drTestRows = drBlockRow.GetChildRows("BLOCK_HISTOLOGY")
                        For Each dr In drTestRows
                            drNewSummaryRow = dtTest.NewRow()
                            drNewSummaryRow("BlockRef") = drBlockRow("BlockRef").ToString()
                            drNewSummaryRow("HistologyRef") = sHistologyRef

                            'This datatable will be used for the dropdown list filter
                            AddToListDataTable(dtHistologyRefList, sHistologyRef)

                            Select Case dr("Code")
                                Case 1 ' EO
                                    drNewSummaryRow("TestID") = dr("ID")
                                    drNewSummaryRow("TestDetails") = "EO"
                                    drNewSummaryRow("TestType") = "Histology"
                                    drNewSummaryRow("Result") = dr("Result")
                                    If dr("Result").ToString <> "" Then
                                        If dr("Result").ToString = "1" Then
                                            drNewSummaryRow("Passed") = True
                                            drNewSummaryRow("Failed") = False
                                        ElseIf dr("Result").ToString = "2" Then
                                            drNewSummaryRow("Passed") = False
                                            drNewSummaryRow("Failed") = True
                                        Else
                                            drNewSummaryRow("Passed") = False
                                            drNewSummaryRow("Failed") = False
                                        End If
                                    End If
                                    drNewSummaryRow("QCCode") = dr("QCCode").ToString()
                                    drNewSummaryRow("QCNote") = dr("QCNote")
                                    drNewSummaryRow("QCNoteRef") = dr("QCNoteRef")
                                    drNewSummaryRow("StainRef") = dr("StainRef").ToString()
                                    drNewSummaryRow("DispatchedDate") = dr("DispatchedDate")
                                    drNewSummaryRow("DispatchedBy") = dr("DispatchedBy")
                                    drNewSummaryRow("PremiumCharge") = dr("PremiumCharge")
                                    drNewSummaryRow("Dispatched") = dr("Dispatched")
                                    drNewSummaryRow("DispatchedTo") = dr("DispatchedTo").ToString()
                                    drNewSummaryRow("Comment") = dr("Comment").ToString()
                                    drNewSummaryRow("RemedialAction") = dr("RemedialAction").ToString()
                                    drNewSummaryRow("OnHold") = bOnHold
                                    drNewSummaryRow("ArchiveLocation") = dr("ArchiveLocation").ToString()
                                    drNewSummaryRow("ArchivedDate") = dr("ArchivedDate")
                                    drNewSummaryRow("ArchiveComment") = dr("ArchiveComment")
                                    drNewSummaryRow("NumberOfSlides") = dr("NumberOfSlides")
                                    drNewSummaryRow("EnteredBy") = dr("EnteredBy")
                                    drNewSummaryRow("Selected") = False
                                    drNewSummaryRow("Archived") = Not IsDBNull(dr("ArchivedDate")) And Not IsDBNull(dr("ArchiveLocation"))
                                    dtTest.Rows.Add(drNewSummaryRow)
                                    AddToListDataTable(dtTestList, "EO")
                                Case 2  'H&E
                                    drNewSummaryRow("TestID") = dr("ID")
                                    drNewSummaryRow("TestDetails") = "H&E"
                                    drNewSummaryRow("TestType") = "Histology"
                                    drNewSummaryRow("Result") = dr("Result")
                                    If dr("Result").ToString <> "" Then
                                        If dr("Result").ToString = "1" Then
                                            drNewSummaryRow("Passed") = True
                                            drNewSummaryRow("Failed") = False
                                        ElseIf dr("Result").ToString = "2" Then
                                            drNewSummaryRow("Passed") = False
                                            drNewSummaryRow("Failed") = True
                                        Else
                                            drNewSummaryRow("Passed") = False
                                            drNewSummaryRow("Failed") = False
                                        End If
                                    End If
                                    drNewSummaryRow("QCCode") = dr("QCCode").ToString()
                                    drNewSummaryRow("QCNote") = dr("QCNote")
                                    drNewSummaryRow("QCNoteRef") = dr("QCNoteRef")
                                    drNewSummaryRow("StainRef") = dr("StainRef").ToString()
                                    drNewSummaryRow("DispatchedDate") = dr("DispatchedDate")
                                    drNewSummaryRow("DispatchedBy") = dr("DispatchedBy")
                                    drNewSummaryRow("PremiumCharge") = dr("PremiumCharge")
                                    drNewSummaryRow("Dispatched") = dr("Dispatched")
                                    drNewSummaryRow("DispatchedTo") = dr("DispatchedTo").ToString()
                                    drNewSummaryRow("Comment") = dr("Comment").ToString()
                                    drNewSummaryRow("RemedialAction") = dr("RemedialAction").ToString()
                                    drNewSummaryRow("OnHold") = bOnHold
                                    drNewSummaryRow("ArchiveLocation") = dr("ArchiveLocation").ToString()
                                    drNewSummaryRow("ArchivedDate") = dr("ArchivedDate")
                                    drNewSummaryRow("ArchiveComment") = dr("ArchiveComment")
                                    drNewSummaryRow("NumberOfSlides") = dr("NumberOfSlides")
                                    drNewSummaryRow("EnteredBy") = dr("EnteredBy")
                                    drNewSummaryRow("Selected") = False
                                    drNewSummaryRow("Archived") = Not IsDBNull(dr("ArchivedDate")) And Not IsDBNull(dr("ArchiveLocation"))
                                    dtTest.Rows.Add(drNewSummaryRow)
                                    AddToListDataTable(dtTestList, "H&E")
                                Case 5 'H&E(Bse)
                                    drNewSummaryRow("TestID") = dr("ID")
                                    drNewSummaryRow("TestDetails") = "H&E (BSE)"
                                    drNewSummaryRow("TestType") = "Histology"
                                    drNewSummaryRow("Result") = dr("Result")
                                    If dr("Result").ToString <> "" Then
                                        If dr("Result").ToString = "1" Then
                                            drNewSummaryRow("Passed") = True
                                            drNewSummaryRow("Failed") = False
                                        ElseIf dr("Result").ToString = "2" Then
                                            drNewSummaryRow("Passed") = False
                                            drNewSummaryRow("Failed") = True
                                        Else
                                            drNewSummaryRow("Passed") = False
                                            drNewSummaryRow("Failed") = False
                                        End If
                                    End If
                                    drNewSummaryRow("QCCode") = dr("QCCode").ToString()
                                    drNewSummaryRow("QCNote") = dr("QCNote")
                                    drNewSummaryRow("QCNoteRef") = dr("QCNoteRef")
                                    drNewSummaryRow("StainRef") = dr("StainRef").ToString()
                                    drNewSummaryRow("DispatchedDate") = dr("DispatchedDate")
                                    drNewSummaryRow("DispatchedBy") = dr("DispatchedBy")
                                    drNewSummaryRow("PremiumCharge") = dr("PremiumCharge")
                                    drNewSummaryRow("Dispatched") = dr("Dispatched")
                                    drNewSummaryRow("DispatchedTo") = dr("DispatchedTo").ToString()
                                    drNewSummaryRow("Comment") = dr("Comment").ToString()
                                    drNewSummaryRow("RemedialAction") = dr("RemedialAction").ToString()
                                    drNewSummaryRow("OnHold") = bOnHold
                                    drNewSummaryRow("ArchiveLocation") = dr("ArchiveLocation").ToString()
                                    drNewSummaryRow("ArchivedDate") = dr("ArchivedDate")
                                    drNewSummaryRow("ArchiveComment") = dr("ArchiveComment")
                                    drNewSummaryRow("NumberOfSlides") = dr("NumberOfSlides")
                                    drNewSummaryRow("EnteredBy") = dr("EnteredBy")
                                    drNewSummaryRow("Selected") = False
                                    drNewSummaryRow("Archived") = Not IsDBNull(dr("ArchivedDate")) And Not IsDBNull(dr("ArchiveLocation"))
                                    dtTest.Rows.Add(drNewSummaryRow)
                                    AddToListDataTable(dtTestList, "H&E (BSE)")
                                Case 7 ' Archive
                                    drNewSummaryRow("TestID") = dr("ID")
                                    drNewSummaryRow("TestDetails") = "Archive"
                                    drNewSummaryRow("TestType") = "Archive"
                                    drNewSummaryRow("Result") = dr("Result")
                                    If dr("Result").ToString <> "" Then
                                        If dr("Result").ToString = "1" Then
                                            drNewSummaryRow("Passed") = True
                                            drNewSummaryRow("Failed") = False
                                        ElseIf dr("Result").ToString = "2" Then
                                            drNewSummaryRow("Passed") = False
                                            drNewSummaryRow("Failed") = True
                                        Else
                                            drNewSummaryRow("Passed") = False
                                            drNewSummaryRow("Failed") = False
                                        End If
                                    End If
                                    drNewSummaryRow("QCCode") = dr("QCCode").ToString()
                                    drNewSummaryRow("QCNote") = dr("QCNote")
                                    drNewSummaryRow("QCNoteRef") = dr("QCNoteRef")
                                    drNewSummaryRow("StainRef") = dr("StainRef").ToString()
                                    drNewSummaryRow("DispatchedDate") = dr("DispatchedDate")
                                    drNewSummaryRow("DispatchedBy") = dr("DispatchedBy")
                                    drNewSummaryRow("PremiumCharge") = dr("PremiumCharge")
                                    drNewSummaryRow("Dispatched") = dr("Dispatched")
                                    drNewSummaryRow("DispatchedTo") = dr("DispatchedTo").ToString()
                                    drNewSummaryRow("Comment") = dr("Comment").ToString()
                                    drNewSummaryRow("RemedialAction") = dr("RemedialAction").ToString()
                                    drNewSummaryRow("OnHold") = bOnHold
                                    drNewSummaryRow("ArchiveLocation") = dr("ArchiveLocation").ToString()
                                    drNewSummaryRow("ArchivedDate") = dr("ArchivedDate")
                                    drNewSummaryRow("ArchiveComment") = dr("ArchiveComment")
                                    drNewSummaryRow("NumberOfSlides") = dr("NumberOfSlides")
                                    drNewSummaryRow("EnteredBy") = dr("EnteredBy")
                                    drNewSummaryRow("Selected") = False
                                    drNewSummaryRow("Archived") = Not IsDBNull(dr("ArchivedDate")) And Not IsDBNull(dr("ArchiveLocation"))
                                    dtTest.Rows.Add(drNewSummaryRow)
                                    AddToListDataTable(dtTestList, "Archive")
                                Case Else
                                    'Do nothing
                            End Select

                        Next

                        drTestRows = drBlockRow.GetChildRows("BLOCK_ANTIBODIES")
                        For Each dr In drTestRows
                            drNewSummaryRow = dtTest.NewRow()
                            drNewSummaryRow("BlockRef") = drBlockRow("BlockRef")
                            drNewSummaryRow("HistologyRef") = sHistologyRef
                            drNewSummaryRow("TestID") = dr("ID")
                            If dr("Code").ToString() = "Other" Then
                                drNewSummaryRow("TestDetails") = "Other Antibodies"
                            Else
                                drNewSummaryRow("TestDetails") = LookupDescription(dtluAntibodies, dr("Code"))
                            End If
                            drNewSummaryRow("TestType") = "Antibodies"
                            drNewSummaryRow("Result") = dr("Result")
                            If dr("Result").ToString <> "" Then
                                If dr("Result").ToString = "1" Then
                                    drNewSummaryRow("Passed") = True
                                    drNewSummaryRow("Failed") = False
                                ElseIf dr("Result").ToString = "2" Then
                                    drNewSummaryRow("Passed") = False
                                    drNewSummaryRow("Failed") = True
                                Else
                                    drNewSummaryRow("Passed") = False
                                    drNewSummaryRow("Failed") = False
                                End If
                            End If
                            drNewSummaryRow("QCCode") = dr("QCCode").ToString()
                            drNewSummaryRow("QCNote") = dr("QCNote")
                            drNewSummaryRow("QCNoteRef") = dr("QCNoteRef")
                            drNewSummaryRow("StainRef") = dr("StainRef").ToString()
                            drNewSummaryRow("DispatchedDate") = dr("DispatchedDate")
                            drNewSummaryRow("DispatchedBy") = dr("DispatchedBy")
                            drNewSummaryRow("PremiumCharge") = dr("PremiumCharge")
                            drNewSummaryRow("Dispatched") = dr("Dispatched")
                            drNewSummaryRow("DispatchedTo") = dr("DispatchedTo").ToString()
                            drNewSummaryRow("Comment") = dr("Comment").ToString()
                            drNewSummaryRow("RemedialAction") = dr("RemedialAction").ToString()
                            drNewSummaryRow("OnHold") = bOnHold
                            drNewSummaryRow("ArchiveLocation") = dr("ArchiveLocation").ToString()
                            drNewSummaryRow("ArchivedDate") = dr("ArchivedDate")
                            drNewSummaryRow("ArchiveComment") = dr("ArchiveComment")
                            drNewSummaryRow("NumberOfSlides") = dr("NumberOfSlides")
                            drNewSummaryRow("EnteredBy") = dr("EnteredBy")
                            drNewSummaryRow("Archived") = Not IsDBNull(dr("ArchivedDate")) And Not IsDBNull(dr("ArchiveLocation"))
                            drNewSummaryRow("Selected") = False
                            dtTest.Rows.Add(drNewSummaryRow)
                            AddToListDataTable(dtTestList, drNewSummaryRow("TestDetails").ToString())
                        Next

                        drTestRows = drBlockRow.GetChildRows("BLOCK_STAIN")
                        For Each dr In drTestRows
                            drNewSummaryRow = dtTest.NewRow()
                            drNewSummaryRow("BlockRef") = drBlockRow("BlockRef")
                            drNewSummaryRow("HistologyRef") = sHistologyRef
                            drNewSummaryRow("TestID") = dr("ID")
                            If dr("Code").ToString() = "Other" Then
                                drNewSummaryRow("TestDetails") = "Other Stain"
                            Else
                                drNewSummaryRow("TestDetails") = LookupDescription(dtluStain, dr("Code"))
                            End If
                            drNewSummaryRow("TestType") = "Stain"
                            drNewSummaryRow("Result") = dr("Result")
                            If dr("Result").ToString <> "" Then
                                If dr("Result").ToString = "1" Then
                                    drNewSummaryRow("Passed") = True
                                    drNewSummaryRow("Failed") = False
                                ElseIf dr("Result").ToString = "2" Then
                                    drNewSummaryRow("Passed") = False
                                    drNewSummaryRow("Failed") = True
                                Else
                                    drNewSummaryRow("Passed") = False
                                    drNewSummaryRow("Failed") = False
                                End If
                            End If
                            drNewSummaryRow("QCCode") = dr("QCCode").ToString()
                            drNewSummaryRow("QCNote") = dr("QCNote")
                            drNewSummaryRow("QCNoteRef") = dr("QCNoteRef")
                            drNewSummaryRow("StainRef") = dr("StainRef").ToString()
                            drNewSummaryRow("DispatchedDate") = dr("DispatchedDate")
                            drNewSummaryRow("DispatchedBy") = dr("DispatchedBy")
                            drNewSummaryRow("PremiumCharge") = dr("PremiumCharge")
                            drNewSummaryRow("Dispatched") = dr("Dispatched")
                            drNewSummaryRow("DispatchedTo") = dr("DispatchedTo").ToString()
                            drNewSummaryRow("Comment") = dr("Comment").ToString()
                            drNewSummaryRow("RemedialAction") = dr("RemedialAction").ToString()
                            drNewSummaryRow("OnHold") = bOnHold
                            drNewSummaryRow("ArchiveLocation") = dr("ArchiveLocation").ToString()
                            drNewSummaryRow("ArchivedDate") = dr("ArchivedDate")
                            drNewSummaryRow("ArchiveComment") = dr("ArchiveComment")
                            drNewSummaryRow("NumberOfSlides") = dr("NumberOfSlides")
                            drNewSummaryRow("EnteredBy") = dr("EnteredBy")
                            drNewSummaryRow("Archived") = Not IsDBNull(dr("ArchivedDate")) And Not IsDBNull(dr("ArchiveLocation"))
                            drNewSummaryRow("Selected") = False
                            dtTest.Rows.Add(drNewSummaryRow)
                            AddToListDataTable(dtTestList, drNewSummaryRow("TestDetails").ToString())
                        Next
                    End If
                Next
            Next
            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBatchSummaryObject)
            Return False
        End Try
    End Function

    Public Function CreateArchiveBlockSummaryData(ByRef dsDataSet As DataSet, ByRef dtSummary As DataTable, ByRef dtBlockRefList As DataTable, ByRef dtHistologyRefList As DataTable) As Boolean
        Try
            Dim drBlockRow As DataRow
            Dim drAnimalRows As DataRow()
            Dim drNewSummaryRow As DataRow
            Dim drAnimalRow As DataRow
            Dim drBlockRows As DataRow()
            Dim bByPassSort As Boolean = False
            Dim drOrderedRows As DataRow()
            Dim drOrderedRow As DataRow

            With dtSummary
                .Columns.Add("ID", System.Type.GetType("System.Int32"))
                .Columns.Add("HistologyRef", System.Type.GetType("System.String"))
                .Columns.Add("BlockRef", System.Type.GetType("System.String"))
                .Columns.Add("AnimalID", System.Type.GetType("System.Int32"))
                .Columns.Add("ArchiveLocation", System.Type.GetType("System.String"))
                .Columns.Add("ArchivedDate", System.Type.GetType("System.DateTime"))
                .Columns.Add("BlockID", System.Type.GetType("System.Int32"))
                .Columns.Add("ArchiveComment", System.Type.GetType("System.String"))
                .Columns.Add("SenderRef", System.Type.GetType("System.String"))
                .Columns.Add("Selected", System.Type.GetType("System.Boolean"))
            End With

            SetPrimaryKey(dtSummary, "ID", True)

            dtHistologyRefList = New DataTable
            dtBlockRefList = New DataTable

            With dtHistologyRefList
                .Columns.Add("ID", System.Type.GetType("System.Int32"))
                .Columns.Add("Description", System.Type.GetType("System.String"))
            End With

            With dtBlockRefList
                .Columns.Add("ID", System.Type.GetType("System.Int32"))
                .Columns.Add("Description", System.Type.GetType("System.String"))
            End With

            SetPrimaryKey(dtHistologyRefList, "ID", True)
            SetPrimaryKey(dtBlockRefList, "ID", True)

            bByPassSort = dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("ByPassSort")
            If bByPassSort = True Then
                drOrderedRows = GetUniqueRows(dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE))
            Else
                drOrderedRows = dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL).Select("", "SenderRef ASC")
            End If

            drBlockRows = dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE).Select()
            Array.Sort(drBlockRows, New CustomBlockRefAscSort)

            'Create the batch summary grid datatable from the batch blocks and animal data tables

            For Each drOrderedRow In drOrderedRows
                drAnimalRow = dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL).Rows.Find(drOrderedRow("ID"))
                For Each drBlockRow In drBlockRows
                    If drAnimalRow("ID") = drBlockRow("AnimalID") Then
                        drNewSummaryRow = dtSummary.NewRow()
                        drNewSummaryRow("BlockRef") = drBlockRow("BlockRef").ToString()
                        drNewSummaryRow("ArchiveLocation") = drBlockRow("ArchiveLocation").ToString()
                        drNewSummaryRow("ArchivedDate") = drBlockRow("ArchivedDate")
                        drNewSummaryRow("ArchiveComment") = drBlockRow("ArchiveComment").ToString()
                        drNewSummaryRow("BlockID") = drBlockRow("ID")
                        drNewSummaryRow("HistologyRef") = drAnimalRow("HistologyRef").ToString()
                        drNewSummaryRow("AnimalID") = drAnimalRow("ID")
                        drNewSummaryRow("SenderRef") = drAnimalRow("SenderRef").ToString()
                        drNewSummaryRow("Selected") = False

                        AddToListDataTable(dtHistologyRefList, drNewSummaryRow("HistologyRef").ToString())
                        AddToListDataTable(dtBlockRefList, drNewSummaryRow("BlockRef").ToString())

                        dtSummary.Rows.Add(drNewSummaryRow)
                    End If
                Next
            Next

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBatchSummaryObject)
            Return False
        End Try
    End Function

    Public Function CreateArchiveTissueSummaryData(ByRef dsDataSet As DataSet, ByRef dtSummary As DataTable, ByRef dtTissuesList As DataTable, ByRef dtArchiveTissuesList As DataTable, ByRef dtHistologyRefList As DataTable) As Boolean
        Try
            Dim drBatchSubmissions As DataRow()
            Dim drBatchSubmission As DataRow
            Dim drAnimalRows As DataRow()
            Dim drAnimalRow As DataRow
            Dim drNewSummaryRow As DataRow
            Dim drTissue As DataRow
            Dim iTissueCount As Integer = 0
            Dim drAnimalTissues As DataRow()
            Dim bByPassSort As Boolean = False
            Dim drOrderedRows() As DataRow
            Dim sColumnName As String
            Dim drOrderedRow As DataRow

            dtHistologyRefList = New DataTable
            dtArchiveTissuesList = New DataTable

            bByPassSort = dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("ByPassSort")

            With dtHistologyRefList
                .Columns.Add("ID", System.Type.GetType("System.Int32"))
                .Columns.Add("Description", System.Type.GetType("System.String"))
            End With

            With dtArchiveTissuesList
                .Columns.Add("ID", System.Type.GetType("System.Int32"))
                .Columns.Add("Description", System.Type.GetType("System.String"))
            End With

            SetPrimaryKey(dtHistologyRefList, "ID", True)
            SetPrimaryKey(dtArchiveTissuesList, "ID", True)

            With dtSummary
                .Columns.Add("ID", System.Type.GetType("System.Int32"))
                .Columns.Add("HistologyRef", System.Type.GetType("System.String"))
                .Columns.Add("SenderRef", System.Type.GetType("System.String"))
                .Columns.Add("AnimalID", System.Type.GetType("System.Int32"))
                .Columns.Add("ArchiveLocation", System.Type.GetType("System.String"))
                .Columns.Add("ArchivedDate", System.Type.GetType("System.DateTime"))
                .Columns.Add("ArchiveComment", System.Type.GetType("System.String"))
                .Columns.Add("TissueCode", System.Type.GetType("System.String"))
                .Columns.Add("TissueID", System.Type.GetType("System.Int32"))
                .Columns.Add("Selected", System.Type.GetType("System.Boolean"))
            End With

            SetPrimaryKey(dtSummary, "ID", True)

            If bByPassSort Then
                drOrderedRows = dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_SUBMISSION_TABLE).Select("", "Order Asc")
                sColumnName = "AnimalID"
            Else
                drOrderedRows = dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE).Select("", "SenderRef ASC")
                sColumnName = "ID"
            End If

            For Each drOrderedRow In drOrderedRows
                drAnimalRow = dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE).Rows.Find(drOrderedRow(sColumnName))

                drBatchSubmissions = drAnimalRow.GetChildRows("ANIMAL_BATCHSUBMISSION")
                For Each drBatchSubmission In drBatchSubmissions
                    drAnimalTissues = drBatchSubmission.GetChildRows("BATCHSUBMISSION_BATCHTISSUES")
                    Array.Sort(drAnimalTissues, New CustomTissuesAscSort)
                    For Each drTissue In drAnimalTissues
                        For iTissueCount = 0 To CInt(drTissue("NoPieces")) - 1
                            drNewSummaryRow = dtSummary.NewRow()
                            drNewSummaryRow("HistologyRef") = drAnimalRow("HistologyRef").ToString()
                            drNewSummaryRow("SenderRef") = drAnimalRow("SenderRef").ToString()
                            drNewSummaryRow("AnimalID") = drAnimalRow("ID")

                            drNewSummaryRow("TissueID") = drTissue("ID")
                            drNewSummaryRow("TissueCode") = LookupDescription(dtTissuesList, drTissue("TissueCode"))
                            drNewSummaryRow("ArchivedDate") = drTissue("ArchivedDate")
                            drNewSummaryRow("ArchiveComment") = drTissue("ArchiveComment").ToString()
                            drNewSummaryRow("ArchiveLocation") = drTissue("ArchiveLocation").ToString()
                            drNewSummaryRow("Selected") = False

                            AddToListDataTable(dtHistologyRefList, drAnimalRow("HistologyRef").ToString())
                            AddToListDataTable(dtArchiveTissuesList, drNewSummaryRow("TissueCode").ToString())
                            dtSummary.Rows.Add(drNewSummaryRow)
                        Next
                    Next
                Next
            Next

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBatchSummaryObject)
            Return False
        End Try
    End Function

    Public Function CreateSenderHistoRefData(ByRef dsDataSet As DataSet, ByRef dtSummary As DataTable) As Boolean
        Try
            Dim drNewSummaryRow As DataRow
            Dim drRow As DataRow
            Dim drAnimalRow As DataRow
            Dim drBlock As DataRow
            Dim dtAnimals As DataTable = dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL)
            Dim dtBlocks As DataTable = dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE)
            Dim drAnimalRows As DataRow()
            Dim bByPassSort As Boolean = False
            Dim drOrderedRows As DataRow()
            Dim drOrderedRow As DataRow
            Dim sColumnName As String
            Dim drFindRow As DataRow()

            With dtSummary
                .Columns.Add("SenderRef", System.Type.GetType("System.String"))
                .Columns.Add("HistologyRef", System.Type.GetType("System.String"))
                .Columns.Add("AnimalID", System.Type.GetType("System.Int32"))
                .Columns.Add("NextBlockRef", System.Type.GetType("System.String"))
                .Columns.Add("NewID", System.Type.GetType("System.Int32"))
                .Columns.Add("HistoRefSet", System.Type.GetType("System.Boolean"))
                .Columns.Add("BookedHistologyRef", System.Type.GetType("System.Boolean"))
            End With

            SetPrimaryKey(dtSummary, "NewID", True)

            bByPassSort = dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("ByPassSort")

            If bByPassSort = True Then
                drOrderedRows = dtBlocks.Select("", "Order ASC")
                sColumnName = "AnimalID"
            Else
                drOrderedRows = dtAnimals.Select("", "SenderRef ASC")
                Array.Sort(drOrderedRows, New HistologyRefAscSort)
                sColumnName = "ID"
            End If

            If Not drOrderedRows Is Nothing Then
                For Each drOrderedRow In drOrderedRows
                    drAnimalRow = dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL).Rows.Find(drOrderedRow(sColumnName))

                    drFindRow = dtSummary.Select("SenderRef='" & drAnimalRow("SenderRef") & "'")

                    If drFindRow.Length = 0 Then
                        drNewSummaryRow = dtSummary.NewRow()
                        drNewSummaryRow("SenderRef") = drAnimalRow("SenderRef")
                        drNewSummaryRow("AnimalID") = drAnimalRow("ID")
                        drNewSummaryRow("HistologyRef") = drAnimalRow("HistologyRef")
                        drNewSummaryRow("NextBlockRef") = drAnimalRow("NextBlockRef")
                        drNewSummaryRow("HistoRefSet") = drAnimalRow("HistoRefSet")
                        drNewSummaryRow("BookedHistologyRef") = drAnimalRow("BookedHistologyRef")
                        dtSummary.Rows.Add(drNewSummaryRow)
                    End If
                Next
            End If

            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsBatchSummaryObject)
            Return False
        End Try
    End Function

#Region "Private Functions"

    Private Function LookupDescription(ByVal dtData As DataTable, ByVal sCode As String) As String
        Dim sFilter As String
        Dim foundRow As DataRow()
        sFilter = "Code=" & "'" & sCode & "'"
        foundRow = dtData.Select(sFilter)
        If foundRow Is Nothing Then
            Throw New Exception("LookupDescription, returned no descripition")
        End If

        If foundRow.Length > 0 Then
            Return foundRow(0)("Description").ToString()
        Else
            Return ""
        End If
    End Function

    Private Function AddToListDataTable(ByRef dtDataTable As DataTable, ByVal sItem As String)
        Dim drNewRow As DataRow
        Dim sFilter As String
        Dim drFoundRows As DataRow()

        sItem = Regex.Replace(sItem, "[.']", "''")
        sFilter = "Description=" & "'" & sItem & "'"
        drFoundRows = dtDataTable.Select(sFilter)

        If Not drFoundRows Is Nothing And drFoundRows.Length = 0 Then
            drNewRow = dtDataTable.NewRow()
            drNewRow("Description") = sItem
            dtDataTable.Rows.Add(drNewRow)
        End If
    End Function

    Private Function GetUniqueRows(ByVal dtData As DataTable) As DataRow()
        Dim dtNewTable As DataTable = New DataTable
        Dim drRow As DataRow
        Dim drOrderedRows As DataRow()
        Dim drFindRow As DataRow()
        Dim drNewRow As DataRow

        dtNewTable.Columns.Add("ID", System.Type.GetType("System.Int32"))

        drOrderedRows = dtData.Select("", "Order ASC")

        For Each drRow In drOrderedRows
            drFindRow = dtNewTable.Select("ID=" & drRow("AnimalID"))

            If drFindRow.Length = 0 Then
                drNewRow = dtNewTable.NewRow()
                drNewRow("ID") = drRow("AnimalID")
                dtNewTable.Rows.Add(drNewRow)
            End If
        Next

        Return dtNewTable.Select

    End Function

    'Private Sub CreateSortedTissueAnimalTable(ByRef dsDataSet As DataSet)
    '    Dim dvViewRow As DataRowView
    '    Dim dtTempAnimalTable As New DataTable()
    '    Dim drSortedRow As DataRow

    '    dtTempAnimalTable = dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE).Copy()

    '    Dim dv As DataView = dtTempAnimalTable.DefaultView
    '    dv.Sort = "SenderRef ASC"

    '    dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE).Clear()
    '    For Each dvViewRow In dv
    '        drSortedRow = dvViewRow.Row
    '        dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE).ImportRow(drSortedRow)
    '    Next
    'End Sub

    'Private Sub CreateSortedBlockAnimalTable(ByRef dsDataSet As DataSet)
    '    Dim dvViewRow As DataRowView
    '    Dim dtTempAnimalTable As New DataTable()
    '    Dim drSortedRow As DataRow

    '    dtTempAnimalTable = dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL).Copy()

    '    Dim dv As DataView = dtTempAnimalTable.DefaultView
    '    dv.Sort = "SenderRef ASC"

    '    dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL).Clear()
    '    For Each dvViewRow In dv
    '        drSortedRow = dvViewRow.Row
    '        dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL).ImportRow(drSortedRow)
    '    Next
    'End Sub

    'Private Sub CreateSortedBlockTable(ByRef dsDataSet As DataSet)
    '    Dim dvViewRow As DataRowView
    '    Dim dtTempBlockTable As New DataTable()
    '    Dim drSortedRow As DataRow

    '    dtTempBlockTable = dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE).Copy()

    '    Dim dv As DataView = dtTempBlockTable.DefaultView
    '    dv.Sort = "BlockRef ASC"

    '    dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE).Clear()
    '    For Each dvViewRow In dv
    '        drSortedRow = dvViewRow.Row
    '        dsDataSet.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE).ImportRow(drSortedRow)
    '    Next
    'End Sub

#End Region

End Class
