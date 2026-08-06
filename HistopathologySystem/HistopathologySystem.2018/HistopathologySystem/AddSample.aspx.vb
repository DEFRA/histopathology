Imports System.Data.OleDb
Imports System.IO

Partial Class AddSample
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents SenderRef1 As SenderRef
    Protected WithEvents MouseNumber1 As MouseNumber
    Protected WithEvents MouseNumber2 As MouseNumber

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
        VLAHeader1.PageTitle = "Add Sample"
        SenderRef1.SetFocus()
        SenderRef1.SetEnterKeyPress(btnNext)
        MouseNumber1.SetTextboxOnEnter(MouseNumber2)
        MouseNumber2.SetEnterKeyPress(btnNext)

        If Not IsPostBack Then
            SenderRef1.Text() = CStr(Session.Item(SessionVars.SV_SenderRef))

            InitialiseUserAreaControls()
        End If
    End Sub

    Private Sub SetProjectOverride()
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtBatch As DataTable
            Dim bProjectOverride As Boolean

            ' cbProjectOverride.Enabled = False

            If Not dsBatchDetails Is Nothing Then
                dtBatch = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)

                If Not dtBatch Is Nothing And dtBatch.Rows.Count > 0 Then
                    'Get the project override flag
                    If Not IsDBNull(dtBatch.Rows(0)("SampleSameProjects")) Then
                        bProjectOverride = dtBatch.Rows(0)("SampleSameProjects")
                    Else
                        bProjectOverride = False
                    End If

                    cbProjectOverride.Checked = bProjectOverride
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to set project override flag.", ex)
        End Try
    End Sub

    Private Sub InitialiseUserAreaControls()
        Try
            SenderRef1.SetValidate(False)

            'Only display the mouse controls for mouse user
            If CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Mouse Bioassay" Or _
               CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Histopath" Then
                ctlMouseDiv.Visible = True
                ctlUploadDiv.Visible = True
                SenderRef1.SetValidate(True)
                SenderRef1.SetREVTooltip("Mouse number format: MCNNNNNN")
            Else
                ctlMouseDiv.Visible = False
                ctlUploadDiv.Visible = False
            End If

            'Only display the sample override controls if not assigning blocks and user area is neuropath
            If CType(Session.Item(SessionVars.SV_AssignBlocks), Boolean) = False And _
               CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Neuropath" Then
                ctlDivSampleOverride.Visible = True
                SetProjectOverride()
                SenderRef1.SetValidate(True)
                SenderRef1.SetREVTooltip("PG Number Format: PGNNNN/NN")
            Else
                ctlDivSampleOverride.Visible = False
            End If

            'Only display the validation override for TB Diag
            If CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "TB Diagnostics" Then
                Dim bUseValidation As Object = CType(Session.Item(SessionVars.SV_UseValidation), Object)
                If bUseValidation Is Nothing Then
                    cbUseValidation.Checked = True
                    Session.Item(SessionVars.SV_UseValidation) = True
                    SenderRef1.SetValidate(True)
                    SenderRef1.SetREVTooltip("Format: NN/NNNNN/YY")
                Else
                    cbUseValidation.Checked = bUseValidation
                    SenderRef1.SetValidate(bUseValidation)
                    SenderRef1.SetREVTooltip("Format: NN/NNNNN/YY")
                End If

                ctlDivValidationOverride.Visible = True
            Else
                ctlDivValidationOverride.Visible = False
            End If


        Catch ex As Exception
            clsAppError.DisplayError("Failed to initialise user area controls.", ex)
        End Try
    End Sub
#Region "Event Handlers"

    Private Sub cbUseValidation_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cbUseValidation.CheckedChanged
        SenderRef1.SetValidate(cbUseValidation.Checked)
        Session.Item(SessionVars.SV_UseValidation) = cbUseValidation.Checked
    End Sub

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        Dim sPrevPage As String = CStr(Session.Item(SessionVars.SV_AddSamplePrevPage))
        Try
            If sPrevPage = "BatchBlockSummary.aspx" Or sPrevPage = "BatchSummary.aspx" Then
                'Bread crumbs
                Dim objArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
                If Not objArrayList Is Nothing Then
                    objArrayList(1) = "Submission Samples"
                    objArrayList(2) = "Sample Summary"
                    Session.Item(SessionVars.SV_BreadCrumbs) = objArrayList
                End If
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Bread Crumb Error, AddSample.aspx.", ex)
        End Try

        Response.Redirect(sPrevPage)
    End Sub

    Private Sub btnNext_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnNext.Click
        Dim bCassetted As Boolean
        Dim bRedirectToLookup As Boolean = True
        Dim sPrevPage As String
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim objLookup As New HistopathologyLib.LookupData
            Dim dtBatch As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)
            Dim objBatchSubmission As New HistopathologyLib.clsBatchSubmission
            Dim objBlocks As New HistopathologyLib.clsBlock
            Dim dtAnimal As DataTable
            Dim objAnimal As New HistopathologyLib.clsAnimal
            Dim dtData As DataTable = Nothing
            Dim sFilter As String
            Dim foundAnimal As DataRow()
            Dim foundRows As DataRow()
            Dim iAnimalID As Integer = CInt(Session.Item(SessionVars.SV_AnimalID))
            Dim iNewAnimalID As Integer
            Dim iHistoRefSet As Boolean = False
            Dim bPMDateSet As Boolean = False
            Dim sError As String = ""
            Dim sProjects As String
            Dim sSpecies As String
            Dim sSpeciesDescription As String
            Dim sPMDate As String
            Dim sProjectDescription As String
            Dim bNeuropath As Boolean = False
            Dim bIsPreCassetted As Boolean = IsBatchPreCassetted(dsBatchDetails, CInt(Session.Item(SessionVars.SV_BatchID)))
            Dim iNumberOfPreBooked As Integer = 0

            ctlDiv.InnerHtml = ""

            If CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Neuropath" Then
                bNeuropath = True
            Else
                bNeuropath = False
            End If

            bCassetted = CType(Session.Item(SessionVars.SV_Cassetted), Boolean)

            If bCassetted Then
                dtAnimal = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL)
            Else
                dtAnimal = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE)
            End If

            'Bit lazy but move the code that was in the 'Assign Range' button into the next button click event
            If Not SenderRef1.Text = "" And (MouseNumber1.Text = "" And MouseNumber2.Text = "") Then
                'Process the senderRef field
                If SenderRef1.CheckSenderRef() Then

                    '---- Neuropath
                    '-------------------------------------
                    'Neuropath, if its a PG number lookit up in the daybook system

                    If CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Neuropath" Then
                        If SenderRef1.IsPGNumber Then
                            ' If SenderRef1.IsValidDayBook Then
                            'Check if the animal is in the daybook
                            If CStr(Session.Item(SessionVars.SV_OldPGNumber)) <> SenderRef1.Text Then
                                If Not objAnimal.GetAnimalFromDayBook(SenderRef1.Text, sSpecies, sProjects, sPMDate, sSpeciesDescription) Then
                                    Throw New Exception("Animal.GetAnimalFromDayBook returned false.")
                                End If
                                ctlDiv.InnerHtml = ""
                                lblError.Visible = False

                                If sSpecies = "" Then
                                    Dim sYearPart As String
                                    sYearPart = Right$(SenderRef1.Text, 2)

                                    If Not IsPre02(sYearPart) Then
                                        ctlDiv.InnerHtml = "<p><font color=""Red"">The PG Number entered is not present in the TSE Daybook system.</font></p>"
                                        Exit Sub
                                    Else
                                        ctlDiv.InnerHtml = "<p><font color=""Red"">Pre 2002 PG Number not found in TSE Daybook system.</font></p>"
                                        ddlProjectsList.Visible = False
                                        txtSpecies.Visible = False
                                        lblSpecies.Visible = False
                                        lblProject.Visible = False
                                        Exit Sub
                                    End If
                                Else
                                    txtSpecies.Visible = True
                                    lblSpecies.Visible = True
                                    txtSpecies.Text = sSpeciesDescription

                                    If cbProjectOverride.Checked = True Then
                                        'Only do the projects code if project override is false
                                        If Not sProjects = "" Then
                                            Dim strArray() As String = sProjects.Split(",")

                                            'Select the project associated with the PG Number
                                            'If ddlProjectsList.Visible = False Then
                                            lblProject.Visible = True
                                            ddlProjectsList.Visible = True

                                            Dim iCount As Integer = 0
                                            Dim sString As String = 0
                                            Dim sBatchProject As String = ""

                                            'Add the project codes to the drop down list
                                            For iCount = ddlProjectsList.Items.Count - 1 To 0 Step -1
                                                ddlProjectsList.Items.RemoveAt(iCount)
                                            Next

                                            iCount = 0

                                            'If more than one item in the list add a blank item
                                            If strArray.Length > 1 Then
                                                AddItemToDropDownList(ddlProjectsList)
                                            End If

                                            'Add the project codes retrieved from the daybook
                                            For iCount = 0 To strArray.Length - 1
                                                sString = strArray(iCount).ToString().Trim()
                                                AddItemToEndOfDropDownList(ddlProjectsList, sString, sString)
                                            Next

                                            sProjectDescription = ""
                                            If Not IsDBNull(dtBatch.Rows(0)("ProjectContractCode")) Then
                                                sBatchProject = dtBatch.Rows(0)("ProjectContractCode").ToString()

                                                sProjectDescription = GetListTypeID(sBatchProject, LOOKUP_PROJECTS)
                                            End If

                                            'default the project to the one selected at batch level if it is present
                                            If strArray.Length = 1 Then
                                                SelectItemInDropDownList(ddlProjectsList, strArray(0).ToString())
                                            Else
                                                SelectItemInDropDownList(ddlProjectsList, sProjectDescription)
                                                ctlDiv.InnerHtml = "<p><font color=""Green"">The PG Number entered has more than one associated project code, please select the required code.</font></p>"
                                            End If
                                        End If
                                    Else
                                        lblProject.Visible = False
                                        ddlProjectsList.Visible = False
                                    End If

                                    Session.Item(SessionVars.SV_PMDate) = sPMDate
                                    Session.Item(SessionVars.SV_Species) = sSpecies
                                    Session.Item(SessionVars.SV_OldPGNumber) = SenderRef1.Text
                                    Exit Sub
                                End If
                            Else
                                Dim sYearPart As String
                                Dim iProjectContractCode As Integer

                                sYearPart = Right$(SenderRef1.Text, 2)

                                If txtSpecies.Visible = True Then
                                    'Only Set the project if project override is true
                                    If cbProjectOverride.Checked = True Then
                                        If ddlProjectsList.Visible = True Then
                                            If ddlProjectsList.SelectedItem.Value = "" Then
                                                If ddlProjectsList.Items.Count > 0 Then
                                                    lblError.Visible = True
                                                    Exit Sub
                                                Else
                                                    lblError.Visible = False
                                                End If
                                            Else
                                                sProjects = ddlProjectsList.SelectedItem.Value
                                            End If
                                        Else
                                            sProjects = ""
                                        End If

                                        If IsDBNull(dtBatch.Rows(0)("ProjectContractCode")) Or dtBatch.Rows(0)("ProjectContractCode").ToString() = "" Then
                                            If sProjects <> CStr(Session.Item(SessionVars.SV_ProjectCode)) And Not CStr(Session.Item(SessionVars.SV_ProjectCode)) = "" Then
                                                'If sProjects <> sProjectDescription Then
                                                ctlDiv.InnerHtml = "<p><font color=""Red"">The project entered at submission level is not the same as the project associated with the PG number entered.</font></p>"
                                                Exit Sub
                                                'End If
                                            End If
                                        Else
                                            'Get the description for the project from lookup
                                            iProjectContractCode = dtBatch.Rows(0)("ProjectContractCode")

                                            sProjectDescription = GetListTypeID(CStr(iProjectContractCode), LOOKUP_PROJECTS)

                                            'If the selected project and batch level project are not the same dont continune
                                            If sProjects <> sProjectDescription Then
                                                ctlDiv.InnerHtml = "<p><font color=""Red"">The project entered at submission level is not the same as the project associated with the PG number entered.</font></p>"
                                                Exit Sub
                                            End If
                                        End If

                                        Session.Item(SessionVars.SV_ProjectCode) = sProjects
                                    End If

                                    If Not dtBatch Is Nothing And dtBatch.Rows.Count > 0 Then
                                        If IsDBNull(dtBatch.Rows(0)("Species")) Or dtBatch.Rows(0)("Species").ToString() = "" Then
                                            dtBatch.Rows(0)("Species") = CStr(Session.Item(SessionVars.SV_Species))
                                        Else
                                            If CStr(Session.Item(SessionVars.SV_Species)) <> dtBatch.Rows(0)("Species").ToString() Then
                                                ctlDiv.InnerHtml = "<p><font color=""Red"">The species " & objLookup.GetSpeciesDescription(dtBatch.Rows(0)("Species").ToString()) & " entered at batch level is not the same as the species associated with the PG number entered.</font></p>"
                                                Exit Sub
                                            End If
                                        End If
                                    End If

                                    Session.Item(SessionVars.SV_ImportedFromDayBook) = True
                                End If
                            End If
                        End If
                    End If

                    '----------
                    If Not objAnimal.GetAnimalsBySenderRef(SenderRef1.Text(), dtData) Then
                        Throw New Exception("Animal.GetAnimalsBySenderRef returned false.")
                    End If

                    'A couple of rules...
                    'If an exact match is returned from the database then use this Animal. But check if there is
                    ' an updated version in the local datatable, if there isnt add a local version to the datatable.
                    'If no records are returned, check the local datatable. If this SenderRef is not present in the 
                    ' local datatable then it is new, so create the new record.
                    sFilter = "SenderRef=" & "'" & SenderRef1.Text.Replace("'", "") & "'"
                    foundAnimal = dtData.Select(sFilter)
                    foundRows = dtAnimal.Select(sFilter)

                    If foundRows.Length > 0 And Not CheckIfFoundAnimalsAreDeleted(foundRows) Then
                        ctlDiv.InnerHtml = "<p><font color=""Red"">The specified Sender Reference is already present on the Submission </font></p>"
                        Exit Sub
                    End If

                    If Not foundAnimal Is Nothing And Not foundRows Is Nothing Then
                        'if its been found in the database
                        bRedirectToLookup = False
                        If foundAnimal.Length >= 1 Then

                            '----- Pre Booked Block Functionality -----
                            If Not objAnimal.GetPreBookedBlocks(CInt(foundAnimal(0)("ID")), dsBatchDetails) Then
                                Throw New Exception("Animal.GetPreBookedBlocks returned false.")
                            End If

                            If bCassetted Then
                                Dim sValidationError As String
                                If Not objAnimal.ValidateAnimalBlocks(dsBatchDetails, dsBatchDetails, foundAnimal(0)("ID"), _
                                                                                iAnimalID, _
                                                                                CInt(Session.Item(SessionVars.SV_SelectedAnimalNumberBlocks)), _
                                                                                SenderRef1.Text, _
                                                                                sValidationError, _
                                                                                bIsPreCassetted) Then
                                    Throw New Exception("Animal.ValidateAnimalBlocks returned false.")
                                End If

                                If sValidationError <> "" Then
                                    If bIsPreCassetted Then
                                        ctlDiv.InnerHtml = "<p><font color=""Red"">" & sValidationError & "<br>Please contact Histopathology to request pre-booking of the required blocks</font></p>"
                                    Else
                                        ctlDiv.InnerHtml = "<p><font color=""Red"">" & sValidationError & "<br>Please contact Histopathology</font></p>"
                                    End If

                                    Exit Sub
                                End If
                            End If
                            '----- End Pre Booked Block Functionality -----

                            'check if its in the local dataset, if not add it
                            If foundRows.Length = 0 Then
                                If foundAnimal(0)("HistologyRef").ToString <> "" Then
                                    iHistoRefSet = True
                                End If
                                If foundAnimal(0)("PMDate").ToString <> "" Then
                                    bPMDateSet = True
                                End If

                                If Not objAnimal.NewExistingRecord(dtAnimal, _
                                                                foundAnimal(0)("SenderRef").ToString(), _
                                                                foundAnimal(0)("HistologyRef").ToString(), _
                                                                foundAnimal(0)("NextBlockRef").ToString(), _
                                                                foundAnimal(0)("RowStamp"), _
                                                                foundAnimal(0)("ID"), _
                                                                iHistoRefSet, _
                                                                foundAnimal(0)("OnHold"), _
                                                                foundAnimal(0)("PMDate").ToString(), _
                                                                bPMDateSet, _
                                                                bNeuropath) Then
                                    Throw New Exception("Animal.NewExistingRecord returned false.")
                                End If
                                iNewAnimalID = foundAnimal(0)("ID")
                            End If
                            'not found in the database
                        ElseIf foundAnimal.Length = 0 Then
                            'check it doesnt exists in the local datset
                            '----- Pre Booked Block Functionality -----
                            If Not objAnimal.GetPreBookedBlocks(0, dsBatchDetails) Then
                                Throw New Exception("Animal.GetPreBookedBlocks returned false.")
                            End If

                            Dim sValidationError As String

                            If bCassetted Then
                                If Not objAnimal.ValidateAnimalBlocks(dsBatchDetails, dsBatchDetails, 0, _
                                                                                iAnimalID, _
                                                                                CInt(Session.Item(SessionVars.SV_SelectedAnimalNumberBlocks)), _
                                                                                SenderRef1.Text, _
                                                                                sValidationError, _
                                                                                bIsPreCassetted) Then
                                    Throw New Exception("Animal.ValidateAnimalBlocks returned false.")
                                End If

                                If sValidationError <> "" Then
                                    ctlDiv.InnerHtml = "<p><font color=""Red"">" & sValidationError & "<br>Please contact Histopathology to request pre-booking of the required blocks</font></p>"
                                    Exit Sub
                                End If
                            End If
                            If foundRows.Length = 0 Then
                                If Not objAnimal.NewRecord(dtAnimal, SenderRef1.Text(), iNewAnimalID, bNeuropath) Then
                                    Throw New Exception("Animal.NewRecord returned false.")
                                End If
                            Else
                                iNewAnimalID = foundRows(0)("ID")
                            End If
                        End If
                        If Not bCassetted Then
                            'If its not a cassetted submission 
                            If Not objBatchSubmission.CopyBatchSubmissionWithAnimalID(dsBatchDetails, iAnimalID, iNewAnimalID) Then
                                Throw New Exception("BatchSubmission.CopyBatchSubmissionWithAnimalID returned false.")
                            End If
                        Else
                            If Not bIsPreCassetted Then
                                'if its a cassetted submission copy the blocks that have the animal ID specified
                                If Not objBlocks.CopySampleBlocks(dsBatchDetails, iAnimalID, iNewAnimalID) Then
                                    Throw New Exception("Block.CopySampleBlocks returned false")
                                End If
                            Else
                                'if its a cassetted submission copy the blocks that have the animal ID specified
                                If Not objBlocks.CopySamplePreBookedBlocks(dsBatchDetails, iAnimalID, iNewAnimalID) Then
                                    Throw New Exception("Block.CopySampleBlocks returned false")
                                End If
                            End If
                        End If
                    End If

                    Session.Item(SessionVars.SV_SenderRef) = SenderRef1.Text()
                Else
                    'If sender ref validation isnt correct
                    Exit Sub
                End If
            ElseIf ((Not MouseNumber1.Text = "") And (Not MouseNumber2.Text = "")) And SenderRef1.Text = "" Then
                'Process the mouse ranges
                Dim sMouseNumber As String = ""
                Dim iMouseRangeFrom As Integer = 0
                Dim iMouseRangeTo As Integer = 0
                Dim iCount As Integer = 0

                If MouseNumber1.CheckMouseNumber() And MouseNumber2.CheckMouseNumber() Then

                    bCassetted = CType(Session.Item(SessionVars.SV_Cassetted), Boolean)

                    iMouseRangeFrom = Convert.ToInt32(Right$(MouseNumber1.Text(), 6))
                    iMouseRangeTo = Convert.ToInt32(Right$(MouseNumber2.Text(), 6))

                    If iMouseRangeFrom >= iMouseRangeTo Then
                        sError = "The from number cannot be greater than the to number."
                    Else
                        For iCount = iMouseRangeFrom To iMouseRangeTo
                            iHistoRefSet = False
                            bPMDateSet = False

                            sMouseNumber = "MC" & PadWithZeroes(iCount)

                            'Check the database for the animal
                            If Not objAnimal.GetAnimalsBySenderRef(sMouseNumber, dtData) Then
                                Throw New Exception("Animal.GetAnimalsBySenderRef returned false.")
                            End If

                            'Check the if an animal was found in the database. 
                            sFilter = "SenderRef=" & "'" & sMouseNumber & "'"
                            foundAnimal = dtData.Select(sFilter)

                            If Not foundAnimal Is Nothing Then
                                If foundAnimal.Length = 1 Then
                                    foundRows = dtAnimal.Select(sFilter)

                                    If Not objAnimal.GetPreBookedBlocks(foundAnimal(0)("ID"), dsBatchDetails) Then
                                        Throw New Exception("Animal.GetPreBookedBlocks returned false.")
                                    End If


                                    Dim sValidationError As String
                                    sValidationError = ""

                                    If bCassetted Then
                                        If Not objAnimal.ValidateAnimalBlocks(dsBatchDetails, dsBatchDetails, foundAnimal(0)("ID"), _
                                                                            iAnimalID, _
                                                                            CInt(Session.Item(SessionVars.SV_SelectedAnimalNumberBlocks)), _
                                                                            sMouseNumber, _
                                                                            sValidationError, _
                                                                            IsBatchPreCassetted(dsBatchDetails, CInt(Session.Item(SessionVars.SV_BatchID)))) Then
                                            Throw New Exception("Animal.ValidateAnimalBlocks returned false.")
                                        End If
                                    End If

                                    If sValidationError <> "" Then
                                        sError = sError & sValidationError & "<br>"
                                    End If

                                    If sValidationError = "" Then
                                        If Not foundRows Is Nothing Then
                                            If foundRows.Length = 0 Then
                                                'Add the animal to the local dataset
                                                If foundAnimal(0)("HistologyRef").ToString <> "" Then
                                                    iHistoRefSet = True
                                                End If
                                                If foundAnimal(0)("PMDate").ToString <> "" Then
                                                    bPMDateSet = True
                                                End If

                                                If Not objAnimal.NewExistingRecord(dtAnimal, _
                                                                                   foundAnimal(0)("SenderRef").ToString(), _
                                                                                   foundAnimal(0)("HistologyRef").ToString(), _
                                                                                   foundAnimal(0)("NextBlockRef").ToString(), _
                                                                                   foundAnimal(0)("RowStamp"), _
                                                                                   foundAnimal(0)("ID"), _
                                                                                   iHistoRefSet, _
                                                                                   foundAnimal(0)("OnHold"), _
                                                                                   foundAnimal(0)("PMDate").ToString(), _
                                                                                   bPMDateSet, _
                                                                                   bNeuropath) Then
                                                    Throw New Exception("Animal.NewExistingRecord returned false.")
                                                End If
                                                iNewAnimalID = foundAnimal(0)("ID")
                                            Else
                                                sError = "Mouse number " & sMouseNumber & " already exists on the new submission. Alter the range and try again.</br>"
                                                Exit For
                                            End If
                                        End If
                                    End If
                                ElseIf foundAnimal.Length = 0 Then

                                    If Not objAnimal.GetPreBookedBlocks(0, dsBatchDetails) Then
                                        Throw New Exception("Animal.GetPreBookedBlocks returned false.")
                                    End If

                                    Dim sValidationError As String
                                    sValidationError = ""

                                    If bCassetted Then
                                        If Not objAnimal.ValidateAnimalBlocks(dsBatchDetails, dsBatchDetails, 0, _
                                                                                iAnimalID, _
                                                                                CInt(Session.Item(SessionVars.SV_SelectedAnimalNumberBlocks)), _
                                                                                sMouseNumber, _
                                                                                sValidationError, _
                                                                                IsBatchPreCassetted(dsBatchDetails, CInt(Session.Item(SessionVars.SV_BatchID)))) Then
                                            Throw New Exception("Animal.ValidateAnimalBlocks returned false.")
                                        End If
                                    End If

                                    If sValidationError <> "" Then
                                        sError = sError & sValidationError & "<br>"
                                    End If

                                    If sError = "" Then
                                        foundRows = dtAnimal.Select(sFilter)
                                        If Not foundRows Is Nothing Then
                                            If foundRows.Length = 0 Then
                                                If Not objAnimal.NewRecord(dtAnimal, sMouseNumber, iNewAnimalID, bNeuropath) Then
                                                    Throw New Exception("Animal.NewRecord returned false.")
                                                End If
                                            Else
                                                sError = sError & "Mouse number " & sMouseNumber & " already exists on the new submission. Alter the range and try again.</br>"
                                                Exit For
                                            End If
                                        End If
                                    End If
                                End If

                                If sError = "" Then
                                    If Not bCassetted Then
                                        'If its not a cassetted submission 
                                        If Not objBatchSubmission.CopyBatchSubmissionWithAnimalID(dsBatchDetails, iAnimalID, iNewAnimalID) Then
                                            Throw New Exception("BatchSubmission.CopyBatchSubmissionWithAnimalID returned false.")
                                        End If
                                    Else
                                        If Not bIsPreCassetted Then
                                            'if its a cassetted submission copy the blocks that have the animal ID specified
                                            If Not objBlocks.CopySampleBlocks(dsBatchDetails, iAnimalID, iNewAnimalID) Then
                                                Throw New Exception("Block.CopySampleBlocks returned false")
                                            End If
                                        Else
                                            'if its a cassetted submission copy the blocks that have the animal ID specified
                                            If Not objBlocks.CopySamplePreBookedBlocks(dsBatchDetails, iAnimalID, iNewAnimalID) Then
                                                Throw New Exception("Block.CopySampleBlocks returned false")
                                            End If
                                        End If
                                    End If
                                End If
                            End If
                        Next
                    End If
                Else
                    If ctlMouseDiv.Visible = False Then
                        If Not SenderRef1.IsComplete() Then
                            ctlDiv.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
                        End If
                    Else
                        ctlDiv.InnerHtml = "<p><font color=""Red"">Enter either the Sender ref or the mouse number ranges.</font></p>"
                    End If
                    Exit Sub
                End If

                If sError <> "" Then
                    If bIsPreCassetted Then
                        ctlDiv.InnerHtml = "<p><font color=""Red"">" & sError & "Please contact Histopathology to request pre-booking of the required blocks</font></p>"
                    Else
                        ctlDiv.InnerHtml = "<p><font color=""Red"">" & sError & "Please contact Histopathology</font></p>"
                    End If


                    If sMouseNumber <> "" Then
                        Dim iMouseNumberError As Integer = CInt(Right$(sMouseNumber, 6))

                        'If we get to a mouse number that has already been used remove the added animals from the dataset
                        ' so the user can start again
                        For iCount = iMouseNumberError - 1 To iMouseRangeFrom Step -1
                            sMouseNumber = "MC" & PadWithZeroes(iCount)

                            sFilter = "SenderRef=" & "'" & sMouseNumber & "'"
                            foundAnimal = dtAnimal.Select(sFilter)

                            If Not foundAnimal Is Nothing And foundAnimal.Length = 1 Then
                                dtAnimal.Rows.Remove(foundAnimal(0))
                            End If
                        Next
                    End If
                    Exit Sub
                Else
                    bRedirectToLookup = False
                End If
            Else
                ctlDiv.InnerHtml = "<p><font color=""Red"">Either use the SenderRef field or the Mouse range fields.</font></p>"
                Exit Sub
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to lookup 'Sender Ref'.", ex)
        End Try

        Try
            sPrevPage = CType(Session.Item(SessionVars.SV_AddSamplePrevPage), String)
            If sPrevPage = "BatchBlockSummary.aspx" Or sPrevPage = "BatchSummary.aspx" Then
                'Bread crumbs
                Dim objArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
                If Not objArrayList Is Nothing Then
                    objArrayList(1) = "Submission Samples"
                    objArrayList(2) = "Sample Summary"
                    Session.Item(SessionVars.SV_BreadCrumbs) = objArrayList
                End If
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Bread Crumb Error, AddSample.aspx.", ex)
        End Try

        If Not bRedirectToLookup Then
            Response.Redirect(sPrevPage)
        Else
            Response.Redirect("SearchSample.aspx")
        End If
    End Sub

    Private Function CheckIfFoundAnimalsAreDeleted(ByVal drFoundRows As DataRow()) As Boolean
        Dim dr As DataRow

        For Each dr In drFoundRows
            If Not dr("RowState") = DataRowState.Deleted Then
                Return False
            End If
        Next
        Return True
    End Function

    Private Sub VLAHeader1_HomeClick(ByVal sender As Object, ByVal e As HistopathologySystem.HomeLinkEventArgs) Handles VLAHeader1.HomeClick
        Dim sMessage As System.Text.StringBuilder = New System.Text.StringBuilder

        If CType(Session.Item(SessionVars.SV_EditingBatch), Boolean) Then
            sMessage.Append("You are currently editing a submission. Any changes that you have made since you last saved the submission will be lost. Are you sure you wish to exit?")
        ElseIf CType(Session.Item(SessionVars.SV_CreatingNewBatch), Boolean) Then
            sMessage.Append("You are currently creating a new submission. If you exit now all the data you have entered will be lost. Are you sure you wish to exit?")
        Else
            sMessage.Append("Any changes that have been made will be discarded, are you sure you wish to exit without saving?")
        End If

        Page.RegisterStartupScript("navigate", PromptBeforeNavigateScript(sMessage.ToString(), "Home.aspx"))
        e.bNavigateHome = False
    End Sub

    Private Sub lbLookup_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lbLookup.Click
        Try
            Dim objAnimal As New HistopathologyLib.clsAnimal
            Dim dtData As DataTable = Nothing

            If Not objAnimal.GetAnimalsBySenderRef(SenderRef1.Text(), dtData) Then
                Throw New Exception("Animal.GetAnimalsBySenderRef returned false.")
            End If

            Session.Item(SessionVars.SV_TempPickSenderList) = dtData
            Session.Item(SessionVars.SV_SenderRef) = SenderRef1.Text()

        Catch ex As Exception
            clsAppError.DisplayError("Failed to lookup Sender Ref.", ex)
        End Try

        Response.Redirect("SearchSample.aspx")
    End Sub

#End Region

#Region "Private Functions"

    Private Function PadWithZeroes(ByVal iCount As Integer) As String
        If iCount < 10 Then
            Return "00000" & iCount
        ElseIf iCount < 100 Then
            Return "0000" & iCount
        ElseIf iCount < 1000 Then
            Return "000" & iCount
        ElseIf iCount < 10000 Then
            Return "00" & iCount
        ElseIf iCount < 100000 Then
            Return "0" & iCount
        Else
            Return CStr(iCount)
        End If
    End Function

#End Region

   
    Private Sub ImportMouseNumbers(ByRef dtData As DataTable, ByVal sFile As String)
        Dim strConn As String = "Provider=Microsoft.Jet.OleDb.4.0;" _
                           & "data source=" & sFile & ";" _
                           & "Extended Properties=Excel 8.0;"

        Dim objConn As New OleDbConnection(strConn)
        Dim strSql As String = "Select distinct NUMBERS From MOUSE_NUMBERS Where NUMBERS>0"

        Dim objCmd As New OleDbCommand(strSql, objConn)
        Try
            objConn.Open()

            Dim objAdapter As New OleDbDataAdapter(objCmd)

            objAdapter.Fill(dtData)
        Catch exc As Exception
            Throw exc
        Finally
            objConn.Dispose()
        End Try
    End Sub

    Private Function ValidateUploadNumbers(ByVal dtMouseNumbers As DataTable, ByRef sError As String)

        Dim iCount As Integer
        Dim sMouseNumber As String
        Dim bCassetted As Boolean = CType(Session.Item(SessionVars.SV_Cassetted), Boolean)
        Dim sValidationError As String
        Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
        Dim objAnimal As New HistopathologyLib.clsAnimal
        Dim dtExistingAnimal As DataTable = Nothing
        Dim sFilter As String
        Dim drFoundMouse As DataRow()
        Dim dtAnimal As DataTable
        Dim dtSamples As DataTable
        Dim iAnimalID As Integer = CInt(Session.Item(SessionVars.SV_AnimalID))

        If bCassetted Then
            dtAnimal = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL)
        Else
            dtAnimal = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE)
        End If

        For iCount = 0 To dtMouseNumbers.Rows.Count - 1
            If Not IsNumeric(dtMouseNumbers.Rows(iCount)("Numbers")) Then
                sError = sError & "Mouse Number: " & dtMouseNumbers.Rows(iCount)("Numbers").ToString() & " -  Incorrect format" & "<br>"
            Else
                sMouseNumber = "MC" & PadWithZeroes(CInt(dtMouseNumbers.Rows(iCount)("Numbers")))

                ' Validate the mouse number
                If Not ValidateMouseNumber(sMouseNumber) Then
                    sError = sError & "Mouse Number: " & sMouseNumber & " -  Incorrect format" & "<br>"
                End If

                'Check the database for the mouse number
                If Not objAnimal.GetAnimalsBySenderRef(sMouseNumber, dtExistingAnimal) Then
                    Throw New Exception("Animal.GetAnimalsBySenderRef returned false.")
                End If

                'Check the if an animal was found in the database. 
                sFilter = "SenderRef=" & "'" & sMouseNumber & "'"
                drFoundMouse = dtExistingAnimal.Select(sFilter)

                sValidationError = ""
                If Not drFoundMouse Is Nothing Then
                    If drFoundMouse.Length = 1 Then
                        If Not objAnimal.GetPreBookedBlocks(drFoundMouse(0)("ID"), dsBatchDetails) Then
                            Throw New Exception("Animal.GetPreBookedBlocks returned false.")
                        End If

                        If bCassetted Then
                            If Not objAnimal.ValidateAnimalBlocks(dsBatchDetails, dsBatchDetails, drFoundMouse(0)("ID"), _
                                                                        iAnimalID, _
                                                                        CInt(Session.Item(SessionVars.SV_SelectedAnimalNumberBlocks)), _
                                                                        sMouseNumber, _
                                                                        sValidationError, _
                                                                        IsBatchPreCassetted(dsBatchDetails, CInt(Session.Item(SessionVars.SV_BatchID)))) Then
                                Throw New Exception("Animal.ValidateAnimalBlocks returned false.")
                            End If
                        End If
                    ElseIf drFoundMouse.Length = 0 Then
                        If Not objAnimal.GetPreBookedBlocks(0, dsBatchDetails) Then
                            Throw New Exception("Animal.GetPreBookedBlocks returned false.")
                        End If

                        sValidationError = ""
                        If bCassetted Then
                            If Not objAnimal.ValidateAnimalBlocks(dsBatchDetails, dsBatchDetails, 0, _
                                                                    iAnimalID, _
                                                                    CInt(Session.Item(SessionVars.SV_SelectedAnimalNumberBlocks)), _
                                                                    sMouseNumber, _
                                                                    sValidationError, _
                                                                    IsBatchPreCassetted(dsBatchDetails, CInt(Session.Item(SessionVars.SV_BatchID)))) Then
                                Throw New Exception("Animal.ValidateAnimalBlocks returned false.")
                            End If
                        End If
                    End If
                End If

                If sValidationError <> "" Then
                    sError = sError & sValidationError & "<br>"
                End If

                drFoundMouse = dtAnimal.Select(sFilter)

                'Check that it isnt already in the local dataset
                If Not drFoundMouse Is Nothing And drFoundMouse.Length > 0 Then
                    sError = sError & "Mouse number " & sMouseNumber & " already exists on the new submission. Alter the range and try again.</br>"
                End If
            End If
        Next

        If sError = "" Then
            Return True
        Else
            Return False
        End If

    End Function


    Private Sub ProcessImport(ByRef dtMouseNumbers As DataTable)

        Dim bRedirectToLookup As Boolean
        Dim sPrevPage As String

        Try
            Dim dtAnimal As DataTable
            Dim iCount As Integer
            Dim bHistoRefSaved As Boolean
            Dim bPMDateSaved As Boolean
            Dim sMouseNumber As String
            Dim bCassetted As Boolean = CType(Session.Item(SessionVars.SV_Cassetted), Boolean)
            Dim objAnimal As New HistopathologyLib.clsAnimal
            Dim dtExistingAnimal As DataTable = Nothing
            Dim drFoundMouse As DataRow()
            Dim drFoundMouseCheck As DataRow()
            Dim sFilter As String
            Dim sValidationError As String
            Dim sError As String
            Dim iNewAnimalID As Integer
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim iAnimalID As Integer = CInt(Session.Item(SessionVars.SV_AnimalID))
            Dim dtBatch As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)
            Dim objBatchSubmission As New HistopathologyLib.clsBatchSubmission
            Dim objBlocks As New HistopathologyLib.clsBlock
            Dim bIsPreCassetted As Boolean = IsBatchPreCassetted(dsBatchDetails, CInt(Session.Item(SessionVars.SV_BatchID)))

            If bCassetted Then
                dtAnimal = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL)
            Else
                dtAnimal = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE)
            End If

            If Not ValidateUploadNumbers(dtMouseNumbers, sError) Then
                If IsBatchPreCassetted(dsBatchDetails, CInt(Session.Item(SessionVars.SV_BatchID))) Then
                    ctlDiv.InnerHtml = "<p><font color=""Red"">" & sError & "If necessary please contact Histopathology to request pre-booking of the required blocks</font></p>"
                Else
                    ctlDiv.InnerHtml = "<p><font color=""Red"">" & sError & "</font></p>"
                End If
                Exit Sub
            End If

            For iCount = 0 To dtMouseNumbers.Rows.Count - 1
                bHistoRefSaved = False
                bPMDateSaved = False

                sMouseNumber = "MC" & PadWithZeroes(CInt(dtMouseNumbers.Rows(iCount)("Numbers")))

                'Check the database for the mouse number
                If Not objAnimal.GetAnimalsBySenderRef(sMouseNumber, dtExistingAnimal) Then
                    Throw New Exception("Animal.GetAnimalsBySenderRef returned false.")
                End If

                'Check the if an animal was found in the database. 
                sFilter = "SenderRef=" & "'" & sMouseNumber & "'"
                drFoundMouse = dtExistingAnimal.Select(sFilter)

                If Not drFoundMouse Is Nothing Then
                    If drFoundMouse.Length = 1 Then
                        'Check if the animal is in the local dataset, if not add it
                        drFoundMouseCheck = dtAnimal.Select(sFilter)

                        If Not drFoundMouseCheck Is Nothing And drFoundMouseCheck.Length = 0 Then

                            If drFoundMouse(0)("HistologyRef").ToString <> "" Then
                                bHistoRefSaved = True
                            End If
                            If drFoundMouse(0)("PMDate").ToString <> "" Then
                                bPMDateSaved = True
                            End If

                            If Not objAnimal.NewExistingRecord(dtAnimal, _
                                                                drFoundMouse(0)("SenderRef").ToString(), _
                                                                drFoundMouse(0)("HistologyRef").ToString(), _
                                                                drFoundMouse(0)("NextBlockRef").ToString(), _
                                                                drFoundMouse(0)("RowStamp"), _
                                                                drFoundMouse(0)("ID"), _
                                                                bHistoRefSaved, _
                                                                drFoundMouse(0)("OnHold"), _
                                                                drFoundMouse(0)("PMDate").ToString(), _
                                                                bPMDateSaved) Then
                                Throw New Exception("Animal.NewExistingRecord returned false.")
                            End If
                            iNewAnimalID = drFoundMouse(0)("ID")
                        End If
                        'Not in the database
                    ElseIf drFoundMouse.Length = 0 Then
                        drFoundMouseCheck = dtAnimal.Select(sFilter)

                        If Not drFoundMouseCheck Is Nothing And drFoundMouseCheck.Length = 0 Then
                            If Not objAnimal.NewRecord(dtAnimal, sMouseNumber, iNewAnimalID, False) Then
                                Throw New Exception("Animal.NewRecord returned false.")
                            End If
                            Session.Item(SessionVars.SV_AnimalID) = iAnimalID
                        End If
                    End If

                    If Not bCassetted Then
                        'If its not a cassetted submission 
                        If Not objBatchSubmission.CopyBatchSubmissionWithAnimalID(dsBatchDetails, iAnimalID, iNewAnimalID) Then
                            Throw New Exception("BatchSubmission.CopyBatchSubmissionWithAnimalID returned false.")
                        End If
                    Else
                        If Not bIsPreCassetted Then
                            'if its a cassetted submission copy the blocks that have the animal ID specified
                            If Not objBlocks.CopySampleBlocks(dsBatchDetails, iAnimalID, iNewAnimalID) Then
                                Throw New Exception("Block.CopySampleBlocks returned false")
                            End If
                        Else
                            'if its a cassetted submission copy the blocks that have the animal ID specified
                            If Not objBlocks.CopySamplePreBookedBlocks(dsBatchDetails, iAnimalID, iNewAnimalID) Then
                                Throw New Exception("Block.CopySampleBlocks returned false")
                            End If
                        End If
                    End If
                End If
            Next

            Session.Item(SessionVars.SV_BatchSubmissionID) = 0
        Catch ex As Exception
            clsAppError.DisplayError("Failed to lookup 'Sender Ref'.", ex)
        End Try

        Try
            sPrevPage = CType(Session.Item(SessionVars.SV_AddSamplePrevPage), String)
            If sPrevPage = "BatchBlockSummary.aspx" Or sPrevPage = "BatchSummary.aspx" Then
                'Bread crumbs
                Dim objArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
                If Not objArrayList Is Nothing Then
                    objArrayList(1) = "Submission Samples"
                    objArrayList(2) = "Sample Summary"
                    Session.Item(SessionVars.SV_BreadCrumbs) = objArrayList
                End If
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Bread Crumb Error, AddSample.aspx.", ex)
        End Try

        Response.Redirect(sPrevPage)
    End Sub

    Private Sub btnUpload_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnUpload.Click

        Dim sFile As String
        Dim dtMouseNumbers As New DataTable

        sFile = GetFile()
        If sFile <> "" Then
            Try
                ImportMouseNumbers(dtMouseNumbers, sFile)

            Catch ex As Exception
                uploadMsg.Text = ex.Message.ToString()
                uploadMsg.Visible = True
                Exit Sub
            Finally
                File.Delete(sFile)
            End Try

            ProcessImport(dtMouseNumbers)
        End If

    End Sub

    Private Function GetFile() As String

        uploadMsg.Text = ""

        Dim sfilename As String = Path.GetFileName(corrFile.PostedFile.FileName)
        Dim sExportsDirectory As String = System.Configuration.ConfigurationSettings.AppSettings("Exports").ToString()

        If corrFile.PostedFile.ContentLength = 0 Then
            uploadMsg.Text = "The file could not be uploaded. Please check that the file exists and contains some content."
            uploadMsg.Visible = True
            Exit Function
        End If

        'Save the uploaded file to the imports directory
        If Not Directory.Exists(sExportsDirectory) Then
            Directory.CreateDirectory(sExportsDirectory)
        End If

        '	// save the file to temp directory on web server
        Dim sRemoteFile As String = sExportsDirectory + sfilename

        Try
            corrFile.PostedFile.SaveAs(sRemoteFile)
        Catch ex As Exception
            uploadMsg.Text = "The file could not be uploaded."
            uploadMsg.Visible = True
            Exit Function
        End Try

        Return sRemoteFile

    End Function

End Class
