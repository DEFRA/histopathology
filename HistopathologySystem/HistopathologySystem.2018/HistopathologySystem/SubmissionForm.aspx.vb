Partial Class SubmissionForm
    Inherits System.Web.UI.Page

#Region " Web Form Designer Generated Code "

    'This call is required by the Web Form Designer.
    <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()

    End Sub

    Private Sub Page_Init(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Init
        'CODEGEN: This method call is required by the Web Form Designer
        'Do not modify it using the code editor.
        InitializeComponent()
    End Sub

#End Region

    Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        'Headers()
        Dim objUsers As New HistopathologyLib.clsUser()
        Dim objLookup As New HistopathologyLib.LookupData()
        Dim dsBatchDetails As DataSet
        Dim rBatchReport As New HistologyReport()
        Dim dsBatchReport As New DataSet("Batch")
        Dim dtBatchReport As New DataTable("Batch")
        Dim dtBatchHistologyReport As New DataTable("BatchHistology")
        Dim dtBatchPostFixationReport As New DataTable("BatchPostFixation")
        Dim dtVersion As New DataTable("Version")
        Dim drVersionRow As DataRow
        Dim dr As DataRow   'Row counter
        Dim drBatch As DataRow
        Dim drBatchHistology As DataRow
        Dim drBatchPostFixation As DataRow
        Dim iBatchType As Integer
        Dim iBatchID As Integer
        Dim iCount As Integer = 0
        Dim iHistologyRowCount As Integer = 0
        Dim iAntibodiesRowCount As Integer = 0
        Dim iSpecialStainRowCount As Integer = 0
        Dim bMaxedRows As Boolean = False
        Dim bCassetted As Boolean = False
        Dim bAssignTissuesBlocks As Boolean = False

        iBatchID = CInt(Session.Item(SessionVars.SV_BatchID))

        GetCommonBatchDetailsFromDatabase(iBatchID, Session)

        'Gets the dataset from the Session object
        dsBatchDetails = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
        If CType(Session.Item(SessionVars.SV_SearchSubmission), Boolean) = True Then
            If CType(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("IsBlocked"), Boolean) = True Then
                GetBatchBlockDetailsFromDatabase(iBatchID, Session)
                'Batch blocks lookup
                dsBatchReport.Tables.Add(CreateBatchBlocksDataTable(dsBatchDetails))
            Else
                GetBatchSubmissionDetailsFromDatabase(iBatchID, Session)
                dsBatchReport.Tables.Add(CreateBatchSubmissionDataTable(dsBatchDetails))
            End If
           
        Else
            bCassetted = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("Cassetted")
            bAssignTissuesBlocks = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("IsBlocked")

            If CBool(Session.Item(SessionVars.SV_AssignBlocks)) = True Then
                GetBatchBlockDetailsFromDatabase(iBatchID, Session)
                'Batch blocks lookup
                dsBatchReport.Tables.Add(CreateBatchBlocksDataTable(dsBatchDetails))
            Else
                If Not bCassetted Then
                    GetBatchSubmissionDetailsFromDatabase(iBatchID, Session)
                    'Batch submission lookup
                    dsBatchReport.Tables.Add(CreateBatchSubmissionDataTable(dsBatchDetails))
                Else
                    GetBatchBlockDetailsFromDatabase(iBatchID, Session)
                    'Batch blocks lookup
                    dsBatchReport.Tables.Add(CreateBatchBlocksDataTable(dsBatchDetails))
                End If
            End If
        End If


        iBatchType = CInt(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("BatchType"))

        dsBatchDetails = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)

        'Post Fixation Lookup
        dtBatchPostFixationReport.Columns.Add("BatchID")
        dtBatchPostFixationReport.Columns.Add("Decal")
        dtBatchPostFixationReport.Columns.Add("Phenol")
        dtBatchPostFixationReport.Columns.Add("Formic")
        dtBatchPostFixationReport.Columns.Add("Other")

        drBatchPostFixation = dtBatchPostFixationReport.NewRow()
        For Each dr In dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_POSTFIXATION_TABLE).Rows
            drBatchPostFixation("BatchID") = dr("ID")
            Select Case dr("Code")
                Case "1"
                    drBatchPostFixation("Formic") = Chr(252)
                Case "2"
                    drBatchPostFixation("Decal") = Chr(252)
                Case "3"
                    drBatchPostFixation("Phenol") = Chr(252)
                Case "Other"
                    drBatchPostFixation("Other") = Chr(252)
            End Select
        Next
        dtBatchPostFixationReport.Rows.Add(drBatchPostFixation)

        dsBatchReport.Tables.Add(dtBatchPostFixationReport)

        Dim bMoreHistology As Boolean = False

        If bCassetted Then
            dsBatchReport.Tables.Add(CreateBlockTestTable(dsBatchDetails, iBatchType, bMoreHistology))
        Else
            dsBatchReport.Tables.Add(CreateBatchTestTable(dsBatchDetails, iBatchType, bMoreHistology))
        End If


        'Batch Report
        dtBatchReport.Columns.Add("ProjectContractCode")
        dtBatchReport.Columns.Add("ContactName")
        dtBatchReport.Columns.Add("BatchDate")
        dtBatchReport.Columns.Add("Species")
        dtBatchReport.Columns.Add("DateReceived")
        dtBatchReport.Columns.Add("TimeReceived")
        dtBatchReport.Columns.Add("SafeToHandle", System.Type.GetType("System.Boolean"))
        dtBatchReport.Columns.Add("Comments")
        dtBatchReport.Columns.Add("Fixation")
        dtBatchReport.Columns.Add("ID")
        dtBatchReport.Columns.Add("PostFixationOther")
        dtBatchReport.Columns.Add("CommentLengthOK")
        dtBatchReport.Columns.Add("OtherSubmittedBy")
        dtBatchReport.Columns.Add("NumberSamples")
        dtBatchReport.Columns.Add("BatchType")
        dtBatchReport.Columns.Add("SubmittedAs")
        dtBatchReport.Columns.Add("MoreHistology")

        drBatch = dtBatchReport.NewRow()

        drBatch("BatchDate") = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("BatchDate")
        drBatch("Species") = objLookup.GetSpeciesDescription(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("Species").ToString())
        drBatch("DateReceived") = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("DateReceived")
        drBatch("TimeReceived") = GetListType(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("TimeReceived").ToString(), LOOKUP_TIME_RECEIVED)
        drBatch("SafeToHandle") = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("SafeToHandle")
        drBatch("Comments") = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("Comments").ToString()
        drBatch("Fixation") = GetListType(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("Fixation").ToString(), LOOKUP_FIXATIVE)
        drBatch("ID") = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("ID")
        drBatch("PostFixationOther") = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("PostFixationOther").ToString()

        Dim sUserName As String = ""
        Dim sUserArea As String = ""
        Dim sUserGroup As String = ""
        Dim sUserAreaID As String = ""

        drBatch("ContactName") = GetListTypeID(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("ContactName").ToString(), LOOKUP_CONTACTS)
        drBatch("ProjectContractCode") = GetListTypeID(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("ProjectContractCode").ToString(), LOOKUP_PROJECTS)


        objUsers.GetUserByID(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("OtherSubmittedBy"), _
                                                           sUserName, _
                                                           sUserArea, _
                                                           sUserGroup, _
                                                           sUserAreaID)
        drBatch("OtherSubmittedBy") = sUserName

        drBatch("NumberSamples") = GetNumberSamplesOnBatch(dsBatchDetails)
        If iBatchType = 0 Then
            drBatch("BatchType") = "TSE"
        Else
            drBatch("BatchType") = "NON TSE"
        End If
        drBatch("SubmittedAs") = CreateSubmittedAsString(dsBatchDetails, iBatchID)

        If bMoreHistology Then
            drBatch("MoreHistology") = "*"
        End If

        If Len(drBatch("Comments")) > 150 Then
            drBatch("CommentLengthOK") = Chr(252)
        End If

        dtBatchReport.Rows.Add(drBatch)

        ' Get the version from the web.config.
        dtVersion.Columns.Add("Version")
        drVersionRow = dtVersion.NewRow()
        If iBatchType = 0 Then
            drVersionRow("Version") = ConfigurationSettings.AppSettings("SubmissionFormVersionTSE").ToString()
        Else
            drVersionRow("Version") = ConfigurationSettings.AppSettings("SubmissionFormVersionNonTSE").ToString()
        End If
        dtVersion.Rows.Add(drVersionRow)
        dsBatchReport.Tables.Add(dtVersion)

        dsBatchReport.Tables.Add(dtBatchReport)

        rBatchReport.SetDataSource(dsBatchReport)

        'Printing code (export to PDF)
        Dim crExportOptions As CrystalDecisions.Shared.ExportOptions
        Dim crDiskFileDestinationOptions As CrystalDecisions.Shared.DiskFileDestinationOptions
        Dim Fname As String

        Fname = System.Configuration.ConfigurationSettings.AppSettings("Exports") & Session.SessionID.ToString & ".pdf"
        crDiskFileDestinationOptions = New CrystalDecisions.Shared.DiskFileDestinationOptions
        crDiskFileDestinationOptions.DiskFileName = Fname
        crExportOptions = rBatchReport.ExportOptions
        With crExportOptions
            .DestinationOptions = crDiskFileDestinationOptions
            .ExportDestinationType = CrystalDecisions.[Shared].ExportDestinationType.DiskFile
            .ExportFormatType = CrystalDecisions.[Shared].ExportFormatType.PortableDocFormat
        End With
        rBatchReport.Export()

        'Display report in browser
        With Response
            .ClearContent()
            .ClearHeaders()
            .ContentType = "application/pdf"
            .WriteFile(Fname)
            .Flush()
            .Close()
        End With

        'Delete the report once it is displayed in the browser
        System.IO.File.Delete(Fname)

    End Sub

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


    Private Function CreateBatchBlocksDataTable(ByVal dsBatchDetails As DataSet) As DataTable
        Dim dtBatchBlocks As New DataTable("BatchSubmission")
        dtBatchBlocks.Columns.Add("SenderRef")
        dtBatchBlocks.Columns.Add("HistologyRef")
        dtBatchBlocks.Columns.Add("BlockRef")
        dtBatchBlocks.Columns.Add("TissueDetails")
        dtBatchBlocks.Columns.Add("RepeatBlock")
        dtBatchBlocks.Columns.Add("CustomerRef")

        Dim drBatchBlocks As DataRow
        Dim drBlocks As DataRow()
        Dim drTissueRow As DataRow()
        Dim drAnimals As DataRow()
        Dim drBlock As DataRow
        Dim drAnimalRow As DataRow
        Dim iTissueCount As Integer = 0
        Dim iTissueMultiplesCount As Integer = 0
        Dim iRowCount As Integer = 0
        'Sort the Blocks
        Dim dvViewRow As DataRowView
        Dim dtTempBlockTable As New DataTable
        Dim drSortedRow As DataRow
        Dim bByPassSort As Boolean = False
        Dim drOrderedRows As DataRow()
        Dim drOrderedRow As DataRow
        Dim drFindRow As DataRow()

        bByPassSort = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("ByPassSort")

        If bByPassSort = True Then
            drOrderedRows = GetUniqueRows(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE))
        Else
            drOrderedRows = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL).Select
            Array.Sort(drOrderedRows, New HistologyRefAscSort)
        End If

        For Each drOrderedRow In drOrderedRows
            drBatchBlocks = dtBatchBlocks.NewRow()

            drAnimalRow = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL).Rows.Find(drOrderedRow("ID"))

            drBlocks = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE).Select()
            Array.Sort(drBlocks, New CustomBlockRefAscSort)

            For Each drBlock In drBlocks
                If drBlock("AnimalID") = drAnimalRow("ID") Then
                    drBatchBlocks = dtBatchBlocks.NewRow()
                    drBatchBlocks("SenderRef") = drAnimalRow("SenderRef")
                    drBatchBlocks("HistologyRef") = drAnimalRow("HistologyRef")
                    drBatchBlocks("BlockRef") = CreateBlockRefString(drBlock)
                    drBatchBlocks("CustomerRef") = drBlock("CustomerRef")
                    'Display tissues
                    drTissueRow = drBlock.GetChildRows("BLOCK_TISSUES")

                    Array.Sort(drTissueRow, New CustomTissuesAscSort)

                    For iTissueCount = 0 To drTissueRow.Length - 1
                        If iTissueCount = 0 Then
                            drBatchBlocks("TissueDetails") = drTissueRow(iTissueCount)("TissueCode")
                            dtBatchBlocks.Rows.Add(drBatchBlocks)

                            If iRowCount Mod 17 = 0 Then
                                drBatchBlocks("SenderRef") = drAnimalRow("SenderRef")
                                drBatchBlocks("HistologyRef") = drAnimalRow("HistologyRef")
                                drBatchBlocks("BlockRef") = CreateBlockRefString(drBlock)
                            End If
                            iRowCount += 1

                            For iTissueMultiplesCount = 1 To drTissueRow(iTissueCount)("NoPieces") - 1
                                drBatchBlocks = dtBatchBlocks.NewRow()
                                drBatchBlocks("TissueDetails") = drTissueRow(iTissueCount)("TissueCode")
                                dtBatchBlocks.Rows.Add(drBatchBlocks)

                                If iRowCount Mod 17 = 0 Then
                                    drBatchBlocks("SenderRef") = drAnimalRow("SenderRef")
                                    drBatchBlocks("HistologyRef") = drAnimalRow("HistologyRef")
                                    drBatchBlocks("BlockRef") = CreateBlockRefString(drBlock)
                                End If

                                iRowCount += 1
                            Next
                        Else
                            drBatchBlocks = dtBatchBlocks.NewRow()
                            drBatchBlocks("TissueDetails") = drTissueRow(iTissueCount)("TissueCode")
                            dtBatchBlocks.Rows.Add(drBatchBlocks)

                            If iRowCount Mod 17 = 0 Then
                                drBatchBlocks("SenderRef") = drAnimalRow("SenderRef")
                                drBatchBlocks("HistologyRef") = drAnimalRow("HistologyRef")
                                drBatchBlocks("BlockRef") = CreateBlockRefString(drBlock)
                            End If

                            iRowCount += 1
                            For iTissueMultiplesCount = 1 To drTissueRow(iTissueCount)("NoPieces") - 1
                                drBatchBlocks = dtBatchBlocks.NewRow()
                                drBatchBlocks("TissueDetails") = drTissueRow(iTissueCount)("TissueCode")
                                dtBatchBlocks.Rows.Add(drBatchBlocks)

                                If iRowCount Mod 17 = 0 Then
                                    drBatchBlocks("SenderRef") = drAnimalRow("SenderRef")
                                    drBatchBlocks("HistologyRef") = drAnimalRow("HistologyRef")
                                    drBatchBlocks("BlockRef") = CreateBlockRefString(drBlock)
                                End If

                                iRowCount += 1
                            Next
                        End If
                    Next
                End If
            Next
        Next

        Return dtBatchBlocks

    End Function

    Private Function CreateBatchSubmissionDataTable(ByVal dsBatchDetails As DataSet) As DataTable
        'Batch Submission Report
        Dim dtBatchSubmissionReport As New DataTable("BatchSubmission")
        Dim drAnimalRows As DataRow()
        Dim drTissueRow As DataRow()
        Dim drBatchSubmissions As DataRow()
        Dim drAnimal As DataRow
        Dim drBatchSubmission As DataRow
        Dim iRowCount As Int32
        Dim iTissueCount As Int32
        Dim iRowsAddedCount As Int32 = 0
        Dim iModdedNumber As Int32 = 0
        Dim bByPassSort As Boolean = False
        Dim drOrderedRows() As DataRow
        Dim sColumnName As String
        Dim drOrderedRow As DataRow

        dtBatchSubmissionReport.Columns.Add("BatchID")
        dtBatchSubmissionReport.Columns.Add("SenderRef")
        dtBatchSubmissionReport.Columns.Add("HistologyRef")
        dtBatchSubmissionReport.Columns.Add("BlockRef")
        dtBatchSubmissionReport.Columns.Add("TissueDetails")
        dtBatchSubmissionReport.Columns.Add("CustomerRef")

        bByPassSort = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("ByPassSort")

        'Go through all the animals and get the related tissues
        If bByPassSort Then
            drOrderedRows = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_SUBMISSION_TABLE).Select("", "Order Asc")
            sColumnName = "AnimalID"
        Else
            drOrderedRows = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE).Select()
            Array.Sort(drOrderedRows, New HistologyRefAscSort)
            sColumnName = "ID"
        End If

        For Each drOrderedRow In drOrderedRows
            drAnimal = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE).Rows.Find(drOrderedRow(sColumnName))

            drBatchSubmission = dtBatchSubmissionReport.NewRow()

            drBatchSubmissions = drAnimal.GetChildRows("ANIMAL_BATCHSUBMISSION")
            drBatchSubmission("BatchID") = drBatchSubmissions(0)("BatchID")
            drBatchSubmission("SenderRef") = drAnimal("SenderRef").ToString()
            drBatchSubmission("HistologyRef") = drAnimal("HistologyRef").ToString()

            drTissueRow = drBatchSubmissions(0).GetChildRows("BATCHSUBMISSION_BATCHTISSUES")

            'Array.Sort(drTissueRow, New CustomTissuesAscSort())

            For iRowCount = 0 To drTissueRow.Length - 1
                If iRowCount = 0 Then
                    drBatchSubmission("TissueDetails") = drTissueRow(iRowCount)("TissueCode")   'LookupDescription(dtTissuesList, drTissueRow(iRowCount)("TissueCode"))

                    If iRowsAddedCount Mod 17 = 0 Then
                        drBatchSubmission("SenderRef") = drAnimal("SenderRef").ToString()
                        drBatchSubmission("HistologyRef") = drAnimal("HistologyRef").ToString()
                    End If
                    iRowsAddedCount = iRowsAddedCount + 1

                    dtBatchSubmissionReport.Rows.Add(drBatchSubmission)
                    For iTissueCount = 1 To drTissueRow(iRowCount)("NoPieces") - 1
                        drBatchSubmission = dtBatchSubmissionReport.NewRow()
                        drBatchSubmission("TissueDetails") = drTissueRow(iRowCount)("TissueCode")   'LookupDescription(dtTissuesList, drTissueRow(iRowCount)("TissueCode"))

                        If iRowsAddedCount Mod 17 = 0 Then
                            drBatchSubmission("SenderRef") = drAnimal("SenderRef").ToString()
                            drBatchSubmission("HistologyRef") = drAnimal("HistologyRef").ToString()
                        End If
                        iRowsAddedCount = iRowsAddedCount + 1

                        dtBatchSubmissionReport.Rows.Add(drBatchSubmission)
                    Next
                Else
                    drBatchSubmission = dtBatchSubmissionReport.NewRow()
                    drBatchSubmission("TissueDetails") = drTissueRow(iRowCount)("TissueCode")   'LookupDescription(dtTissuesList, drTissueRow(iRowCount)("TissueCode"))

                    If iRowsAddedCount Mod 17 = 0 Then
                        drBatchSubmission("SenderRef") = drAnimal("SenderRef").ToString()
                        drBatchSubmission("HistologyRef") = drAnimal("HistologyRef").ToString()
                    End If
                    iRowsAddedCount = iRowsAddedCount + 1

                    dtBatchSubmissionReport.Rows.Add(drBatchSubmission)
                    For iTissueCount = 1 To drTissueRow(iRowCount)("NoPieces") - 1
                        drBatchSubmission = dtBatchSubmissionReport.NewRow()
                        drBatchSubmission("TissueDetails") = drTissueRow(iRowCount)("TissueCode")   'LookupDescription(dtTissuesList, drTissueRow(iRowCount)("TissueCode"))

                        If iRowsAddedCount Mod 17 = 0 Then
                            drBatchSubmission("SenderRef") = drAnimal("SenderRef").ToString()
                            drBatchSubmission("HistologyRef") = drAnimal("HistologyRef").ToString()
                        End If
                        iRowsAddedCount = iRowsAddedCount + 1

                        dtBatchSubmissionReport.Rows.Add(drBatchSubmission)
                    Next
                End If
            Next

        Next

        Return dtBatchSubmissionReport
    End Function

    Private Sub Headers()
        With Response
            .Clear()
            .Charset = ""
            .ContentType = "application/pdf"
            .AddHeader("Content-Disposition", "attachment; filename=""Test.pdf""")
        End With
        Me.EnableViewState = False
    End Sub

    Private Function GetNumberSamplesOnBatch(ByVal dsBatchDetails As DataSet) As String
        If dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("IsBlocked") = 0 Then
            Dim dtBatchSubmission As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_SUBMISSION_TABLE)

            Return dtBatchSubmission.Rows.Count.ToString()
        Else
            Dim dtBatchBlock As DataTable
            Dim aArray As New ArrayList
            Dim drRow As DataRow
            Dim iAnimalID As Integer

            'Find the number of samples that have been added against the submission
            dtBatchBlock = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_SUBMISSION_TABLE)

            For Each drRow In dtBatchBlock.Rows
                iAnimalID = drRow("AnimalID")

                If Not aArray.Contains(iAnimalID) Then
                    aArray.Add(iAnimalID)
                End If
            Next

            Return aArray.Count.ToString()
        End If

    End Function

    Private Function CreateSubmittedAsString(ByVal dsBatchDetails As DataSet, ByVal iBatchID As Integer) As String
        Dim dtSubmittedAs As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_SUBMITTEDAS_TABLE)
        Dim drFoundRows As DataRow()
        Dim iCount As Integer = 0
        Dim sFilter As String
        Dim sSubmittedAs As String

        sFilter = "BatchID=" & iBatchID
        drFoundRows = dtSubmittedAs.Select(sFilter)
        For iCount = 0 To drFoundRows.Length - 1
            If iCount = 0 Then
                sSubmittedAs = GetListType(drFoundRows(iCount)("Code").ToString(), LOOKUP_SUBMITTEDAS)
            Else
                sSubmittedAs = sSubmittedAs & ", " & GetListType(drFoundRows(iCount)("Code").ToString(), LOOKUP_SUBMITTEDAS)
            End If
        Next

        Return sSubmittedAs
    End Function


    Private Function CreateBatchTestTable(ByVal dsBatchDetails As DataSet, ByVal iBatchType As Integer, ByRef bMoreHistology As Boolean) As DataTable
        Dim dtHistologyTable As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_HISTOLOGY_TABLE)
        Dim dtSpecialStain As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_STAIN_TABLE)
        Dim dtAntibodies As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_ANTIBODIES_TABLE)

        Dim drBatchHistology As DataRow
        Dim iHistologyRowCount As Integer = 0
        Dim iAntibodiesRowCount As Integer = 0
        Dim iSpecialStainRowCount As Integer = 0
        Dim iCount As Integer = 0

        Dim dtBatchHistologyReport As New DataTable("BatchHistology")
        'Batch Histology Sub Report
        dtBatchHistologyReport.Columns.Add("BatchID")
        dtBatchHistologyReport.Columns.Add("Code")

        'Display maximum of 8 histologys. If more display * on last one to indicate more details.
        'Uses the bMoreHistology flag to determin whether to display the * or not.
        While iHistologyRowCount <= dtHistologyTable.Rows.Count - 1 And iCount < 8
            'If the histology is not Special stain or antibodies
            If dtHistologyTable.Rows(iHistologyRowCount)("Code").ToString() = "1" Or _
                dtHistologyTable.Rows(iHistologyRowCount)("Code").ToString() = "2" Or _
                dtHistologyTable.Rows(iHistologyRowCount)("Code").ToString() = "5" Or _
                dtHistologyTable.Rows(iHistologyRowCount)("Code").ToString() = "7" Then

                iCount = iCount + 1

                drBatchHistology = dtBatchHistologyReport.NewRow()
                drBatchHistology("Code") = GetHistologyListType(dtHistologyTable.Rows(iHistologyRowCount)("Code").ToString())
                drBatchHistology("BatchID") = dtHistologyTable.Rows(iHistologyRowCount)("BatchID")
                dtBatchHistologyReport.Rows.Add(drBatchHistology)

                If iCount = 8 And (iHistologyRowCount < dtHistologyTable.Rows.Count - 1 Or _
                                   iSpecialStainRowCount < dtSpecialStain.Rows.Count - 1 Or _
                                   iAntibodiesRowCount < dtAntibodies.Rows.Count - 1) Then
                    bMoreHistology = True
                End If

                iHistologyRowCount = iHistologyRowCount + 1

            ElseIf dtHistologyTable.Rows(iHistologyRowCount)("Code").ToString() = "3" Then 'Special Stain
                'Display the required special stains
                While iSpecialStainRowCount <= dtSpecialStain.Rows.Count - 1 And iCount < 8
                    iCount = iCount + 1

                    drBatchHistology = dtBatchHistologyReport.NewRow()

                    If dtSpecialStain.Rows(iSpecialStainRowCount)("Code").ToString() = "Other" Then
                        drBatchHistology("Code") = "Special Other"
                    Else
                        drBatchHistology("Code") = GetListType(dtSpecialStain.Rows(iSpecialStainRowCount)("Code").ToString(), LOOKUP_SPECIAL_STAIN)
                    End If

                    drBatchHistology("BatchID") = dtHistologyTable.Rows(iHistologyRowCount)("BatchID")
                    dtBatchHistologyReport.Rows.Add(drBatchHistology)

                    If iCount = 8 And (iHistologyRowCount < dtHistologyTable.Rows.Count - 1 Or _
                                   iSpecialStainRowCount < dtSpecialStain.Rows.Count - 1 Or _
                                   iAntibodiesRowCount < dtAntibodies.Rows.Count - 1) Then
                        bMoreHistology = True
                    End If

                    iSpecialStainRowCount = iSpecialStainRowCount + 1
                End While

                iHistologyRowCount = iHistologyRowCount + 1
            ElseIf dtHistologyTable.Rows(iHistologyRowCount)("Code").ToString() = "4" Or dtHistologyTable.Rows(iHistologyRowCount)("Code").ToString() = "6" Then 'IHC
                'Display the required TSE or Non TSE antibodies
                If iBatchType = 0 Then 'TSE
                    While iAntibodiesRowCount <= dtAntibodies.Rows.Count - 1 And iCount < 8
                        iCount = iCount + 1

                        drBatchHistology = dtBatchHistologyReport.NewRow()
                        drBatchHistology("BatchID") = dtHistologyTable.Rows(iHistologyRowCount)("BatchID")

                        If dtAntibodies.Rows(iAntibodiesRowCount)("Code").ToString() = "Other" Then
                            drBatchHistology("Code") = "IHC-PrP Other"
                        Else
                            drBatchHistology("Code") = GetListType(dtAntibodies.Rows(iAntibodiesRowCount)("Code").ToString(), LOOKUP_TSE_ANTIBODIES)
                        End If

                        dtBatchHistologyReport.Rows.Add(drBatchHistology)

                        If iCount = 8 And (iHistologyRowCount < dtHistologyTable.Rows.Count - 1 Or _
                                  iSpecialStainRowCount < dtSpecialStain.Rows.Count - 1 Or _
                                  iAntibodiesRowCount < dtAntibodies.Rows.Count - 1) Then
                            bMoreHistology = True
                        End If

                        iAntibodiesRowCount = iAntibodiesRowCount + 1
                    End While

                    iHistologyRowCount = iHistologyRowCount + 1
                Else
                    While iAntibodiesRowCount <= dtAntibodies.Rows.Count - 1 And iCount < 8
                        iCount = iCount + 1

                        drBatchHistology = dtBatchHistologyReport.NewRow()
                        drBatchHistology("BatchID") = dtHistologyTable.Rows(iHistologyRowCount)("BatchID")

                        If dtAntibodies.Rows(iAntibodiesRowCount)("Code").ToString() = "Other" Then
                            drBatchHistology("Code") = "IHC-PrP Other"
                        Else
                            drBatchHistology("Code") = GetListType(dtAntibodies.Rows(iAntibodiesRowCount)("Code").ToString(), LOOKUP_NONTSE_ANTIBODIES)
                        End If

                        dtBatchHistologyReport.Rows.Add(drBatchHistology)

                        If iCount = 8 And (iHistologyRowCount < dtHistologyTable.Rows.Count - 1 Or _
                                   iSpecialStainRowCount < dtSpecialStain.Rows.Count - 1 Or _
                                   iAntibodiesRowCount < dtAntibodies.Rows.Count - 1) Then
                            bMoreHistology = True
                        End If

                        iAntibodiesRowCount = iAntibodiesRowCount + 1

                    End While
                    iHistologyRowCount = iHistologyRowCount + 1
                End If
            End If
        End While

        Return dtBatchHistologyReport
    End Function


    Private Function CreateBlockTestTable(ByVal dsBatchDetails As DataSet, ByVal iBatchType As Integer, ByRef bMoreHistology As Boolean) As DataTable
        Dim dtHistologyTable As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_HISTOLOGY)
        Dim dtSpecialStain As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_STAIN)
        Dim dtAntibodies As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANTIBODIES)
        Dim drBlockAntibodies As DataRow()
        Dim drBlockStain As DataRow()
        Dim drBlockHistology As DataRow()
        Dim drAnimalRow As DataRow
        Dim drAnimals As DataRow()
        Dim drBlocks As DataRow()
        Dim iAnimalCount As Integer = 0
        Dim iCount As Integer = 0
        Dim iBlockCount As Integer = 0
        Dim dtBatchHistologyReport As New DataTable("BatchHistology")
        Dim drBatchHistology As DataRow
        Dim iHistologyRowCount As Integer = 0
        Dim iAntibodiesRowCount As Integer = 0
        Dim iSpecialStainRowCount As Integer = 0
        Dim sFilter As String = ""
        Dim objLookup As New HistopathologyLib.LookupData
        Dim dtLookupHistology As DataTable = objLookup.GetHistologyLookupData()
        Dim dtLookupTSEAntibodies As DataTable = objLookup.GetLookupData(LOOKUP_TSE_ANTIBODIES)
        Dim dtLookupNonTSEAntibodies As DataTable = objLookup.GetLookupData(LOOKUP_NONTSE_ANTIBODIES)
        Dim dtLookupStain As DataTable = objLookup.GetLookupData(LOOKUP_SPECIAL_STAIN)
        Dim sTestString As String = ""

        If dtLookupHistology Is Nothing Or _
           dtLookupTSEAntibodies Is Nothing Or _
           dtLookupNonTSEAntibodies Is Nothing Or _
           dtLookupStain Is Nothing Then
            Throw New Exception("Failed to load lookup data tables")
        End If

        'Batch Histology Sub Report
        dtBatchHistologyReport.Columns.Add("BatchID")
        dtBatchHistologyReport.Columns.Add("Code")

        drAnimals = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL).Select("", "SenderRef ASC")

        While iAnimalCount <= drAnimals.Length - 1 And iCount < 8

            drBlocks = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE).Select("AnimalID=" & drAnimals(iAnimalCount)("ID"))
            Array.Sort(drBlocks, New CustomBlockRefAscSort)

            iBlockCount = 0
            While iBlockCount <= drBlocks.Length - 1 And iCount < 9
                sFilter = "BlockID=" & drBlocks(iBlockCount)("ID")

                drBlockAntibodies = dtAntibodies.Select(sFilter)
                drBlockStain = dtSpecialStain.Select(sFilter)
                drBlockHistology = dtHistologyTable.Select(sFilter)


                iHistologyRowCount = 0
                iSpecialStainRowCount = 0
                iAntibodiesRowCount = 0
                'Display maximum of 8 histologys. If more display * on last one to indicate more details.
                'Uses the bMoreHistology flag to determin whether to display the * or not.
                While iHistologyRowCount <= drBlockHistology.Length - 1 And iCount < 9
                    'If the histology is not Special stain or antibodies
                    If drBlockHistology(iHistologyRowCount)("Code").ToString() = "1" Or _
                       drBlockHistology(iHistologyRowCount)("Code").ToString() = "2" Or _
                        drBlockHistology(iHistologyRowCount)("Code").ToString() = "5" Or _
                        drBlockHistology(iHistologyRowCount)("Code").ToString() = "7" Then

                        sTestString = LookupDescription(dtLookupHistology, drBlockHistology(iHistologyRowCount)("Code").ToString())
                        If Not CheckIfTestExists(dtBatchHistologyReport, sTestString) Then
                            iCount = iCount + 1
                            drBatchHistology = dtBatchHistologyReport.NewRow()
                            drBatchHistology("Code") = sTestString
                            drBatchHistology("BatchID") = drBlocks(iBlockCount)("BatchID")
                            dtBatchHistologyReport.Rows.Add(drBatchHistology)

                        End If
                        iHistologyRowCount = iHistologyRowCount + 1
                    ElseIf drBlockHistology(iHistologyRowCount)("Code").ToString() = "3" Then 'Special Stain
                        'Display the required special stains

                        While iSpecialStainRowCount <= drBlockStain.Length - 1 And iCount < 9

                            If dtSpecialStain.Rows(iSpecialStainRowCount)("Code").ToString() = "Other" Then
                                sTestString = "Special Other"
                            Else
                                sTestString = LookupDescription(dtLookupStain, drBlockStain(iSpecialStainRowCount)("Code").ToString())
                            End If

                            If Not CheckIfTestExists(dtBatchHistologyReport, sTestString) Then
                                iCount = iCount + 1

                                drBatchHistology = dtBatchHistologyReport.NewRow()
                                drBatchHistology("Code") = sTestString
                                drBatchHistology("BatchID") = drBlocks(iBlockCount)("BatchID")
                                dtBatchHistologyReport.Rows.Add(drBatchHistology)
                            End If
                            iSpecialStainRowCount = iSpecialStainRowCount + 1
                        End While

                        iHistologyRowCount = iHistologyRowCount + 1
                    ElseIf drBlockHistology(iHistologyRowCount)("Code").ToString() = "4" Or drBlockHistology(iHistologyRowCount)("Code").ToString() = "6" Then 'IHC
                        'Display the required TSE or Non TSE antibodies
                        If iBatchType = 0 Then 'TSE

                            While iAntibodiesRowCount <= drBlockAntibodies.Length - 1 And iCount < 9

                                If dtAntibodies.Rows(iAntibodiesRowCount)("Code").ToString() = "Other" Then
                                    sTestString = "IHC-PrP Other"
                                Else
                                    sTestString = LookupDescription(dtLookupTSEAntibodies, drBlockAntibodies(iAntibodiesRowCount)("Code").ToString())
                                End If

                                If Not CheckIfTestExists(dtBatchHistologyReport, sTestString) Then
                                    iCount = iCount + 1
                                    drBatchHistology = dtBatchHistologyReport.NewRow()
                                    drBatchHistology("BatchID") = drBlocks(iBlockCount)("BatchID")
                                    drBatchHistology("Code") = sTestString
                                    dtBatchHistologyReport.Rows.Add(drBatchHistology)
                                End If

                                iAntibodiesRowCount = iAntibodiesRowCount + 1
                            End While

                            iHistologyRowCount = iHistologyRowCount + 1
                        Else
                            While iAntibodiesRowCount <= drBlockAntibodies.Length - 1 And iCount < 9

                                If dtAntibodies.Rows(iAntibodiesRowCount)("Code").ToString() = "Other" Then
                                    sTestString = "IHC-PrP Other"
                                Else
                                    sTestString = LookupDescription(dtLookupNonTSEAntibodies, drBlockAntibodies(iAntibodiesRowCount)("Code").ToString())
                                End If

                                If Not CheckIfTestExists(dtBatchHistologyReport, sTestString) Then
                                    iCount = iCount + 1
                                    drBatchHistology = dtBatchHistologyReport.NewRow()
                                    drBatchHistology("BatchID") = drBlocks(iBlockCount)("BatchID")
                                    drBatchHistology("Code") = sTestString
                                    dtBatchHistologyReport.Rows.Add(drBatchHistology)
                                End If
                                iAntibodiesRowCount = iAntibodiesRowCount + 1

                            End While
                            iHistologyRowCount = iHistologyRowCount + 1
                        End If
                    End If
                End While

                iBlockCount = iBlockCount + 1
            End While

            iAnimalCount = iAnimalCount + 1
        End While

        If iCount = 9 Then
            bMoreHistology = True
            dtBatchHistologyReport.Rows.RemoveAt(dtBatchHistologyReport.Rows.Count - 1)
        End If

        Return dtBatchHistologyReport
    End Function

    Private Function CheckIfTestExists(ByVal dtTable As DataTable, ByVal sCode As String)
        Dim bFoundRow As Boolean = False
        Dim drRow As DataRow

        For Each drRow In dtTable.Rows
            If drRow("Code").ToString() = sCode Then
                bFoundRow = True
                Exit For
            End If
        Next

        Return bFoundRow
    End Function

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


    Private Function CreateBlockRefString(ByVal drBlockRow As DataRow) As String
        Dim sBlockRef As String = ""

        If drBlockRow("Comment").ToString.Trim <> "" Then
            sBlockRef = drBlockRow("BlockRef").ToString() & "*"
        ElseIf Not IsDBNull(drBlockRow("RepeatBlock")) Then
            If drBlockRow("RepeatBlock") = True Then
                sBlockRef = drBlockRow("BlockRef").ToString() & "*"
            Else
                sBlockRef = drBlockRow("BlockRef").ToString()
            End If
        Else
            sBlockRef = drBlockRow("BlockRef").ToString()
        End If

        Return sBlockRef
    End Function
End Class
