Imports System.Data.OleDb
Imports System.IO
Imports System.Text.RegularExpressions

Partial Class AddSubmission
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents SenderRef1 As SenderRef
    Protected WithEvents MouseNumber1 As MouseNumber
    Protected WithEvents btnMouseRange As System.Web.UI.WebControls.Button
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
        SenderRef1.SetMandatory(True)
        SenderRef1.SetEnterKeyPress(btnNext)
        MouseNumber1.SetTextboxOnEnter(MouseNumber2)
        MouseNumber2.SetEnterKeyPress(btnNext)

        If Not IsPostBack Then
            DisplaySenderRef()
            InitialiseUserAreaControls()
        End If
    End Sub


#Region "Event Handlers"

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        Dim sPrevPage As String
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtAnimal As DataTable
            Dim bCassetted As Boolean = CType(Session.Item(SessionVars.SV_Cassetted), Boolean)
            Dim iAnimalID As Integer = CInt(Session.Item(SessionVars.SV_AnimalID))
            Dim dtData As DataTable
            Dim sFilter As String
            Dim foundRows As DataRow()
            sPrevPage = CStr(Session.Item(SessionVars.SV_AddSamplePrevPage))

            If bCassetted Then
                dtData = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE)
                dtAnimal = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL)
            Else
                dtData = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_SUBMISSION_TABLE)
                dtAnimal = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE)
            End If

            sFilter = "AnimalID=" & iAnimalID
            foundRows = dtData.Select(sFilter)

            'if the sample has no attached tissues or blocks remove it from the dataset
            If Not foundRows Is Nothing AndAlso foundRows.Length = 0 Then
                sFilter = "ID=" & iAnimalID
                foundRows = dtAnimal.Select(sFilter)

                If Not foundRows Is Nothing AndAlso foundRows.Length > 0 Then
                    dtAnimal.Rows.Remove(foundRows(0))
                End If
            End If

            Try
                sPrevPage = CStr(Session.Item(SessionVars.SV_AddSamplePrevPage))
                If sPrevPage = "BatchBlockSummary.aspx" Or sPrevPage = "BatchSummary.aspx" Then
                    Dim objCrumbArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
                    If Not objCrumbArrayList Is Nothing Then
                        objCrumbArrayList(1) = "Submission Samples"
                        objCrumbArrayList(2) = "Sample Summary"
                        objCrumbArrayList.RemoveAt(3)
                        Session.Item(SessionVars.SV_BreadCrumbs) = objCrumbArrayList
                    End If
                End If
            Catch ex As Exception
                clsAppError.DisplayError("Bread Crumb Error, AddSubmission.aspx.", ex)
            End Try

        Catch ex As Exception
            clsAppError.DisplayError("Failed to remove animal from session.", ex)
        End Try

        Response.Redirect(sPrevPage)
    End Sub

    Private Sub lbLookup_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lbLookup.Click
        Dim bCassetted As Boolean = False
        Dim sNextPage As String
        Try
            Dim objAnimal As New HistopathologyLib.clsAnimal
            Dim dtData As DataTable = Nothing
            Dim iAnimalID As Integer

            If Not objAnimal.GetAnimalsBySenderRef(SenderRef1.Text(), dtData) Then
                Throw New Exception("Animal.GetAnimalsBySenderRef returned false.")
            End If

            Session.Item(SessionVars.SV_TempPickSenderList) = dtData
            Session.Item(SessionVars.SV_SenderRef) = SenderRef1.Text()

        Catch ex As Exception
            clsAppError.DisplayError("Failed to lookup Sender Ref.", ex)
        End Try

        Try
            Dim objCrumbArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
            If Not objCrumbArrayList Is Nothing Then
                objCrumbArrayList(1) = "Submission Samples"
                objCrumbArrayList(2) = "Add Sample"
                objCrumbArrayList(3) = "Search Sender Ref"
                Session.Item(SessionVars.SV_BreadCrumbs) = objCrumbArrayList
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Bread Crumb Error, AddSubmission.aspx.", ex)
        End Try

        Response.Redirect("SearchSender.aspx")

    End Sub

    Private Sub btnNext_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnNext.Click
        Dim bCassetted As Boolean
        Dim bRedirectToLookup As Boolean = True
        Dim sNextPage As String
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dsOldBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_OldBatchDetails), DataSet)
            Dim dtBatch As DataTable
            Dim objLookup As New HistopathologyLib.LookupData
            Dim dtAnimal As DataTable
            Dim dtSamples As DataTable
            Dim objAnimal As New HistopathologyLib.clsAnimal
            Dim objArrayList As ArrayList
            Dim dtData As DataTable = Nothing
            Dim sFilter As String
            Dim foundAnimal As DataRow()
            Dim foundRows As DataRow()
            Dim iHistoRefSet As Boolean = False
            Dim bPMDateSet As Boolean = False
            Dim sError As String = ""
            Dim iAnimalID As Integer
            Dim sProjects As String
            Dim sSpecies As String
            Dim sSpeciesDescription As String
            Dim sPMDate As String
            Dim bValidate As Boolean = True
            Dim sProjectDescription As String
            Dim bNeuropath As Boolean = False
            Dim iNumberOfPreBooked As Integer = 0
            Dim bCopySamples As Boolean = False

            ctlDiv.InnerHtml = ""

            bCassetted = CType(Session.Item(SessionVars.SV_Cassetted), Boolean)

            If bCassetted Then
                dtAnimal = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL)
                dtSamples = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE)
            Else
                dtAnimal = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE)
                dtSamples = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_SUBMISSION_TABLE)
            End If

            If CStr(Session.Item(SessionVars.SV_AddSampleNextPage)) = "CopyBatch.aspx" Or _
             CStr(Session.Item(SessionVars.SV_AddSampleNextPage)) = "CopyBatchBlocks.aspx" Then
                bCopySamples = True
                dtBatch = dsOldBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)
            Else
                bCopySamples = False
                dtBatch = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)
            End If

            If CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Neuropath" Then
                bNeuropath = True
            Else
                bNeuropath = False
            End If

            'Bit lazy but move the code that was in the 'Assign Range' button into the next button click event
            If Not SenderRef1.Text = "" And (MouseNumber1.Text = "" And MouseNumber2.Text = "") Then
                'Process the sender ref

                If SenderRef1.CheckSenderRef() Then
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

                                    dtBatch.Rows(0)("SampleSameProjects") = cbProjectOverride.Checked
                                    Session.Item(SessionVars.SV_ImportedFromDayBook) = True
                                End If
                            End If
                        End If
                    End If

                    '----------------------------------------------------------------

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

                    If Not foundAnimal Is Nothing Then
                        If foundAnimal.Length >= 1 Then

                            '----- Pre Booked Block Functionality -----
                            If Not objAnimal.GetPreBookedBlocks(foundAnimal(0)("ID"), dsBatchDetails) Then
                                Throw New Exception("Animal.GetPreBookedBlocks returned false.")
                            End If

                            If bCopySamples = True Then
                                Dim sValidationError As String

                                If bCassetted Then
                                    If Not objAnimal.ValidateAnimalBlocks(dsBatchDetails, dsOldBatchDetails, foundAnimal(0)("ID"), _
                                                                                    CInt(Session.Item(SessionVars.SV_SelectedAnimal)), _
                                                                                    CInt(Session.Item(SessionVars.SV_SelectedAnimalNumberBlocks)), _
                                                                                    SenderRef1.Text, _
                                                                                    sValidationError, _
                                                                                    IsBatchPreCassetted(dsOldBatchDetails, CInt(Session.Item(SessionVars.SV_BatchID)))) Then
                                        Throw New Exception("Animal.ValidateAnimalBlocks returned false.")
                                    End If

                                    If sValidationError <> "" Then
                                        If IsBatchPreCassetted(dsOldBatchDetails, CInt(Session.Item(SessionVars.SV_BatchID))) Then
                                            ctlDiv.InnerHtml = "<p><font color=""Red"">" & sValidationError & "<br>Please contact Histopathology to request pre-booking of the required blocks</font></p>"
                                        Else
                                            ctlDiv.InnerHtml = "<p><font color=""Red"">" & sValidationError & "<br>Please contact Histopathology</font></p>"
                                        End If
                                        Exit Sub
                                    End If
                                End If
                            Else
                                If IsBatchPreCassetted(dsBatchDetails, CInt(Session.Item(SessionVars.SV_BatchID))) Then
                                    If Not objAnimal.CheckPreBookedBlocksExist(foundAnimal(0)("ID"), dsBatchDetails, iNumberOfPreBooked) Then
                                        ctlDiv.InnerHtml = "<p><font color=""Red"">Sender Ref: " & Trim(SenderRef1.Text) & " No pre booked blocks exist.<br>Please contact Histopathology to request pre-booking of the required blocks</font></p>"
                                        Exit Sub
                                    End If
                                End If
                            End If

                            '----- End Pre Booked Block Functionality -----

                            bRedirectToLookup = False
                            Session.Item(SessionVars.SV_AnimalID) = foundAnimal(0)("ID")

                            '--------- Code for the Copy Submission

                            Dim objNewIDs As New HistopathologyLib.clsIDPairs

                            objArrayList = CType(Session.Item(SessionVars.SV_AnimalIDs), ArrayList)

                            If Not objArrayList Is Nothing Then
                                objNewIDs.OldID = CInt(Session.Item(SessionVars.SV_SelectedAnimal))
                                objNewIDs.NewID = foundAnimal(0)("ID")
                                objNewIDs.Value = foundAnimal(0)("SenderRef").ToString()
                                objNewIDs.OtherValue = foundAnimal(0)("SenderRef").ToString()
                                objArrayList.Add(objNewIDs)
                                Session.Item(SessionVars.SV_AnimalIDs) = objArrayList
                            End If

                            '-----------

                            foundRows = dtAnimal.Select(sFilter)
                            '  If Not foundRows Is Nothing And foundRows.Length > 0 Then
                            If Not foundRows Is Nothing And foundRows.Length = 0 Then
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

                                Session.Item(SessionVars.SV_AnimalID) = foundAnimal(0)("ID")
                            Else
                                If Not foundRows Is Nothing Then
                                    Dim iNotDeletedAnimal As Integer
                                    'The old animal was deleted so add it back
                                    If CheckIfFoundAnimalsAreDeleted(foundRows, iNotDeletedAnimal) Then
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

                                        Session.Item(SessionVars.SV_AnimalID) = foundAnimal(0)("ID")
                                    Else
                                        Session.Item(SessionVars.SV_AnimalID) = iNotDeletedAnimal
                                    End If
                                End If
                            End If
                        ElseIf foundAnimal.Length = 0 Then

                            '----- Pre Booked Block Functionality -----
                            If Not objAnimal.GetPreBookedBlocks(0, dsBatchDetails) Then
                                Throw New Exception("Animal.GetPreBookedBlocks returned false.")
                            End If

                            If bCopySamples = True Then
                                Dim sValidationError As String
                                sValidationError = ""

                                If bCassetted Then
                                    If Not objAnimal.ValidateAnimalBlocks(dsBatchDetails, dsOldBatchDetails, 0, _
                                                                                    CInt(Session.Item(SessionVars.SV_SelectedAnimal)), _
                                                                                    CInt(Session.Item(SessionVars.SV_SelectedAnimalNumberBlocks)), _
                                                                                    SenderRef1.Text, _
                                                                                    sValidationError, _
                                                                                    IsBatchPreCassetted(dsOldBatchDetails, CInt(Session.Item(SessionVars.SV_BatchID)))) Then
                                        Throw New Exception("Animal.ValidateAnimalBlocks returned false.")
                                    End If

                                    If sValidationError <> "" Then
                                        ctlDiv.InnerHtml = "<p><font color=""Red"">" & sValidationError & "<br>Please contact Histopathology to request pre-booking of the required blocks</font></p>"
                                        Exit Sub
                                    End If
                                End If
                            Else
                                If IsBatchPreCassetted(dsBatchDetails, CInt(Session.Item(SessionVars.SV_BatchID))) Then
                                    ctlDiv.InnerHtml = "<p><font color=""Red"">Sender Ref: " & Trim(SenderRef1.Text) & " No pre booked blocks exist.<br>Please contact Histopathology to request pre-booking of the required blocks</font></p>"
                                    Exit Sub
                                End If
                            End If

                            bRedirectToLookup = False
                            foundRows = dtAnimal.Select(sFilter)

                            If Not foundRows Is Nothing And foundRows.Length = 0 Then
                                '---- Neuropath stuff
                                If CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Neuropath" Then
                                    Dim bSetInDatabase As Boolean = False
                                    If CStr(Session.Item(SessionVars.SV_PMDate)) <> "" Then
                                        bSetInDatabase = True
                                    End If

                                    If Not objAnimal.NewRecord(dtAnimal, SenderRef1.Text(), iAnimalID, CStr(Session.Item(SessionVars.SV_PMDate)), bSetInDatabase, bNeuropath) Then
                                        Throw New Exception("Animal.NewRecord returned false.")
                                    End If
                                Else
                                    If Not objAnimal.NewRecord(dtAnimal, SenderRef1.Text(), iAnimalID, bNeuropath) Then
                                        Throw New Exception("Animal.NewRecord returned false.")
                                    End If
                                End If

                                Session.Item(SessionVars.SV_AnimalID) = iAnimalID
                            Else
                                If Not foundRows Is Nothing Then
                                    'The old animal was deleted so add it back
                                    Dim iNotDeletedAnimal As Integer
                                    If CheckIfFoundAnimalsAreDeleted(foundRows, iNotDeletedAnimal) Then
                                        '---- Neuropath stuff
                                        If CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Neuropath" Then
                                            Dim bSetInDatabase As Boolean = False
                                            If CStr(Session.Item(SessionVars.SV_PMDate)) <> "" Then
                                                bSetInDatabase = True
                                            End If

                                            If Not objAnimal.NewRecord(dtAnimal, SenderRef1.Text(), iAnimalID, CStr(Session.Item(SessionVars.SV_PMDate)), bSetInDatabase, bNeuropath) Then
                                                Throw New Exception("Animal.NewRecord returned false.")
                                            End If
                                            Session.Item(SessionVars.SV_AnimalID) = iAnimalID
                                        Else
                                            If Not objAnimal.NewRecord(dtAnimal, SenderRef1.Text(), iAnimalID, bNeuropath) Then
                                                Throw New Exception("Animal.NewRecord returned false.")
                                            End If
                                            Session.Item(SessionVars.SV_AnimalID) = iAnimalID
                                        End If
                                    Else
                                        Session.Item(SessionVars.SV_AnimalID) = iNotDeletedAnimal
                                    End If
                                End If

                            End If

                            '--------- Code for the Copy Submission
                            Dim objNewIDs As New HistopathologyLib.clsIDPairs

                            objArrayList = CType(Session.Item(SessionVars.SV_AnimalIDs), ArrayList)

                            If Not objArrayList Is Nothing Then
                                objNewIDs.OldID = CInt(Session.Item(SessionVars.SV_SelectedAnimal))
                                objNewIDs.NewID = CInt(Session.Item(SessionVars.SV_AnimalID)) 'Changed this from iAnimalID
                                objNewIDs.Value = SenderRef1.Text()
                                objNewIDs.OtherValue = SenderRef1.Text()
                                objArrayList.Add(objNewIDs)
                                Session.Item(SessionVars.SV_AnimalIDs) = objArrayList
                            End If

                            '-----------

                        End If
                    End If
                    Session.Item(SessionVars.SV_SenderRef) = SenderRef1.Text()
                Else
                    Exit Sub
                End If
            ElseIf ((Not MouseNumber1.Text = "") And (Not MouseNumber2.Text = "")) And SenderRef1.Text = "" Then
                'Process the mouse ranges
                Dim iMouseRangeFrom As Integer = 0
                Dim iMouseRangeTo As Integer = 0
                Dim iCount As Integer = 0
                Dim sMouseNumber As String = ""

                If MouseNumber1.CheckMouseNumber() And MouseNumber2.CheckMouseNumber() Then

                    objArrayList = CType(Session.Item(SessionVars.SV_AnimalIDs), ArrayList)

                    iMouseRangeFrom = Convert.ToInt32(Right$(MouseNumber1.Text(), 6))
                    iMouseRangeTo = Convert.ToInt32(Right$(MouseNumber2.Text(), 6))

                    If iMouseRangeFrom >= iMouseRangeTo Then
                        sError = "The from number cannot be greater than the to number."
                    Else
                        For iCount = iMouseRangeFrom To iMouseRangeTo
                            iHistoRefSet = False
                            bPMDateSet = False

                            sMouseNumber = "MC" & PadWithZeroes(iCount)

                            'Check if the animal is in the database
                            If Not objAnimal.GetAnimalsBySenderRef(sMouseNumber, dtData) Then
                                Throw New Exception("Animal.GetAnimalsBySenderRef returned false.")
                            End If

                            'Check the if an animal was found in the database. 
                            sFilter = "SenderRef=" & "'" & sMouseNumber & "'"
                            foundAnimal = dtData.Select(sFilter)

                            'Its in the database add it to our local dataset
                            If foundAnimal.Length = 1 Then

                                If Not objAnimal.GetPreBookedBlocks(foundAnimal(0)("ID"), dsBatchDetails) Then
                                    Throw New Exception("Animal.GetPreBookedBlocks returned false.")
                                End If

                                Dim sValidationError As String
                                sValidationError = ""

                                If bCopySamples = True Then
                                    If bCassetted Then
                                        If Not objAnimal.ValidateAnimalBlocks(dsBatchDetails, dsOldBatchDetails, foundAnimal(0)("ID"), _
                                                                                    CInt(Session.Item(SessionVars.SV_SelectedAnimal)), _
                                                                                    CInt(Session.Item(SessionVars.SV_SelectedAnimalNumberBlocks)), _
                                                                                    sMouseNumber, _
                                                                                    sValidationError, _
                                                                                    IsBatchPreCassetted(dsOldBatchDetails, CInt(Session.Item(SessionVars.SV_BatchID)))) Then
                                            Throw New Exception("Animal.ValidateAnimalBlocks returned false.")
                                        End If
                                    End If
                                    If sValidationError <> "" Then
                                        sError = sError & sValidationError & "<br>"
                                    End If
                                Else
                                    If IsBatchPreCassetted(dsBatchDetails, CInt(Session.Item(SessionVars.SV_BatchID))) Then
                                        If Not objAnimal.CheckPreBookedBlocksExist(foundAnimal(0)("ID"), dsBatchDetails, iNumberOfPreBooked) Then
                                            sError = sError & "Sender Ref: " & Trim(SenderRef1.Text) & " no pre booked blocks exist.<br>"
                                        End If
                                    End If
                                End If

                                If sValidationError = "" Then
                                    If Not objArrayList Is Nothing Then
                                        Dim objNewIDs As New HistopathologyLib.clsIDPairs
                                        objNewIDs.OldID = CInt(Session.Item(SessionVars.SV_SelectedAnimal))
                                        objNewIDs.NewID = foundAnimal(0)("ID")
                                        objNewIDs.Value = foundAnimal(0)("SenderRef").ToString()
                                        objNewIDs.OtherValue = MouseNumber1.Text() & " - " & MouseNumber2.Text()
                                        objArrayList.Add(objNewIDs)
                                        Session.Item(SessionVars.SV_AnimalIDs) = objArrayList
                                    End If

                                    'Check if the animal is in the local dataset, if not add it
                                    foundRows = dtAnimal.Select(sFilter)

                                    If Not foundRows Is Nothing And foundRows.Length = 0 Then
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
                                                bPMDateSet) Then
                                            Throw New Exception("Animal.NewExistingRecord returned false.")
                                        End If
                                    End If
                                End If
                                'Not in the database
                            ElseIf foundAnimal.Length = 0 Then

                                If Not objAnimal.GetPreBookedBlocks(0, dsBatchDetails) Then
                                    Throw New Exception("Animal.GetPreBookedBlocks returned false.")
                                End If

                                If bCopySamples = True Then

                                    Dim sValidationError As String
                                    sValidationError = ""
                                    If bCassetted Then
                                        If Not objAnimal.ValidateAnimalBlocks(dsBatchDetails, dsOldBatchDetails, 0, _
                                                                                        CInt(Session.Item(SessionVars.SV_SelectedAnimal)), _
                                                                                        CInt(Session.Item(SessionVars.SV_SelectedAnimalNumberBlocks)), _
                                                                                        sMouseNumber, _
                                                                                        sValidationError, _
                                                                                        IsBatchPreCassetted(dsOldBatchDetails, CInt(Session.Item(SessionVars.SV_BatchID)))) Then
                                            Throw New Exception("Animal.ValidateAnimalBlocks returned false.")
                                        End If
                                    End If
                                    If sValidationError <> "" Then
                                        sError = sError & sValidationError & "<br>"
                                    End If

                                Else
                                    If IsBatchPreCassetted(dsBatchDetails, CInt(Session.Item(SessionVars.SV_BatchID))) Then
                                        sError = sError & "Sender Ref: " & Trim(sMouseNumber) & " no pre booked blocks exist.<br>"
                                    End If
                                End If

                                foundRows = dtAnimal.Select(sFilter)

                                'Check that it isnt already in the local dataset
                                If Not foundRows Is Nothing And foundRows.Length = 0 Then
                                    If Not objAnimal.NewRecord(dtAnimal, sMouseNumber, iAnimalID, False) Then
                                        Throw New Exception("Animal.NewRecord returned false.")
                                    End If
                                    Session.Item(SessionVars.SV_AnimalID) = iAnimalID
                                Else
                                    sError = "Mouse number " & sMouseNumber & " already exists on the new submission. Alter the range and try again.</br>"
                                    Exit For
                                End If

                                objArrayList = CType(Session.Item(SessionVars.SV_AnimalIDs), ArrayList)

                                If Not objArrayList Is Nothing Then
                                    Dim objNewIDs As New HistopathologyLib.clsIDPairs
                                    objNewIDs.OldID = CInt(Session.Item(SessionVars.SV_SelectedAnimal))
                                    objNewIDs.NewID = CInt(Session.Item(SessionVars.SV_AnimalID))
                                    objNewIDs.Value = sMouseNumber
                                    objNewIDs.OtherValue = MouseNumber1.Text() & " - " & MouseNumber2.Text()
                                    objArrayList.Add(objNewIDs)
                                    Session.Item(SessionVars.SV_AnimalIDs) = objArrayList
                                End If
                            End If
                        Next
                    End If
                Else
                    Exit Sub
                End If

                If sError <> "" Then
                    If bCopySamples = True Then
                        If IsBatchPreCassetted(dsOldBatchDetails, CInt(Session.Item(SessionVars.SV_BatchID))) Then
                            ctlDiv.InnerHtml = "<p><font color=""Red"">" & sError & "Please contact Histopathology to request pre-booking of the required blocks</font></p>"
                        Else
                            ctlDiv.InnerHtml = "<p><font color=""Red"">" & sError & "Please contact Histopathology</font></p>"
                        End If
                    Else
                        If IsBatchPreCassetted(dsOldBatchDetails, CInt(Session.Item(SessionVars.SV_BatchID))) Then
                            ctlDiv.InnerHtml = "<p><font color=""Red"">" & sError & "Please contact Histopathology to request pre-booking of the required blocks</font></p>"
                        End If
                    End If

                    If sMouseNumber <> "" Then
                        Dim iMouseNumberError As Integer = CInt(Right$(sMouseNumber, 6))
                        Dim iArrayCount = 0
                        Dim objNewIDs As New HistopathologyLib.clsIDPairs

                        'If we get to a mouse number that has already been used remove the added animals from the dataset
                        ' so the user can start again
                        For iCount = iMouseNumberError - 1 To iMouseRangeFrom Step -1
                            sMouseNumber = "MC" & PadWithZeroes(iCount)

                            sFilter = "SenderRef=" & "'" & sMouseNumber & "'"
                            foundAnimal = dtAnimal.Select(sFilter)

                            If Not foundAnimal Is Nothing AndAlso foundAnimal.Length = 1 Then
                                objArrayList = CType(Session.Item(SessionVars.SV_AnimalIDs), ArrayList)

                                dtAnimal.Rows.Remove(foundAnimal(0))

                                'Aswell as removing the animal from the dataset remove any
                                'record of the animal in the ID pairs array
                                For iArrayCount = objArrayList.Count - 1 To 0 Step 1
                                    objNewIDs = objArrayList(iArrayCount)

                                    If objNewIDs.NewID = foundAnimal(0)("ID") Then
                                        objArrayList.RemoveAt(iArrayCount)
                                    End If
                                Next
                            End If
                        Next
                    End If
                    Exit Sub
                Else
                    bRedirectToLookup = False
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


            Session.Item(SessionVars.SV_BatchSubmissionID) = 0
        Catch ex As Exception
            clsAppError.DisplayError("Failed to lookup 'Sender Ref'.", ex)
        End Try

        If Not bRedirectToLookup Then
            Try
                sNextPage = CStr(Session.Item(SessionVars.SV_AddSampleNextPage))
                If sNextPage = "SubmissionDetailsBlock.aspx" Then
                    'Bread crumbs
                    Dim objCrumbArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
                    If Not objCrumbArrayList Is Nothing Then
                        objCrumbArrayList(1) = "Submission Samples"
                        objCrumbArrayList(2) = "Blocking"
                        objCrumbArrayList(3) = "Sample Blocks"
                        Session.Item(SessionVars.SV_BreadCrumbs) = objCrumbArrayList
                    End If
                ElseIf sNextPage = "SubmissionDetails.aspx" Then
                    'Bread crumbs
                    Dim objCrumbArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
                    If Not objCrumbArrayList Is Nothing Then
                        objCrumbArrayList(1) = "Submission Samples"
                        objCrumbArrayList(2) = "Tissuing"
                        objCrumbArrayList(3) = "Sample Details"
                        Session.Item(SessionVars.SV_BreadCrumbs) = objCrumbArrayList
                    End If
                End If
            Catch ex As Exception
                clsAppError.DisplayError("Bread Crumb Error, AddSubmission.aspx.", ex)
            End Try

            Response.Redirect(sNextPage)
        Else
            Try
                'Bread crumbs
                Dim objCrumbArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
                If Not objCrumbArrayList Is Nothing Then
                    objCrumbArrayList(1) = "Submission Samples"
                    objCrumbArrayList(2) = "Add Sample"
                    objCrumbArrayList(3) = "Search Sender Ref"
                    Session.Item(SessionVars.SV_BreadCrumbs) = objCrumbArrayList
                End If
            Catch ex As Exception
                clsAppError.DisplayError("Bread Crumb Error, AddSubmission.aspx.", ex)
            End Try
            Response.Redirect("SearchSender.aspx")
        End If
    End Sub

    Private Sub btnLookup_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Dim bRedirectSubmissionDetails As Boolean = False
        Dim bCassetted As Boolean = False
        Dim sNextPage As String
        Try
            If SenderRef1.CheckSenderRef() Then
                Dim objAnimal As New HistopathologyLib.clsAnimal
                Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
                Dim dtAnimal As DataTable
                Dim dtData As DataTable = Nothing
                Dim iAnimalID As Integer
                Dim foundRows As DataRow()
                Dim sFilter As String
                Dim iNotDeletedAnimal As Integer
                Dim bNeuropath As Boolean = False

                If CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Neuropath" Then
                    bNeuropath = True
                End If

                If Not objAnimal.GetAnimalsBySenderRef(SenderRef1.Text(), dtData) Then
                    Throw New Exception("Animal.GetAnimalsBySenderRef returned false.")
                End If

                bCassetted = CType(Session.Item(SessionVars.SV_Cassetted), Boolean)
                If bCassetted Then
                    dtAnimal = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL)
                Else
                    dtAnimal = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE)
                End If

                sFilter = "SenderRef=" & "'" & SenderRef1.Text() & "'"
                foundRows = dtAnimal.Select(sFilter)

                'if its a new record create and go directly to the submission details page.
                If dtData.Rows.Count = 0 And foundRows.Length = 0 Then
                    If Not objAnimal.NewRecord(dtAnimal, SenderRef1.Text(), iAnimalID, bNeuropath) Then
                        Throw New Exception("Animal.NewRecord returned false.")
                    End If

                    bRedirectSubmissionDetails = True
                    Session.Item(SessionVars.SV_AnimalID) = iAnimalID

                    '--------- Code for the Copy Submission
                    Dim objArrayList As ArrayList
                    Dim objNewIDs As New HistopathologyLib.clsIDPairs

                    objArrayList = CType(Session.Item(SessionVars.SV_AnimalIDs), ArrayList)

                    If Not objArrayList Is Nothing Then
                        objNewIDs.OldID = CInt(Session.Item(SessionVars.SV_SelectedAnimal))
                        objNewIDs.NewID = iAnimalID
                        objNewIDs.Value = SenderRef1.Text()
                        objNewIDs.OtherValue = SenderRef1.Text()
                        objArrayList.Add(objNewIDs)
                        Session.Item(SessionVars.SV_AnimalIDs) = objArrayList
                    End If
                ElseIf dtData.Rows.Count > 0 And CheckIfFoundAnimalsAreDeleted(foundRows, iNotDeletedAnimal) Then
                    If Not objAnimal.NewRecord(dtAnimal, SenderRef1.Text(), iAnimalID, bNeuropath) Then
                        Throw New Exception("Animal.NewRecord returned false.")
                    End If

                    bRedirectSubmissionDetails = True
                    Session.Item(SessionVars.SV_AnimalID) = iAnimalID

                    '--------- Code for the Copy Submission
                    Dim objArrayList As ArrayList
                    Dim objNewIDs As New HistopathologyLib.clsIDPairs

                    objArrayList = CType(Session.Item(SessionVars.SV_AnimalIDs), ArrayList)

                    If Not objArrayList Is Nothing Then
                        objNewIDs.OldID = CInt(Session.Item(SessionVars.SV_SelectedAnimal))
                        objNewIDs.NewID = iAnimalID
                        objNewIDs.Value = SenderRef1.Text()
                        objNewIDs.OtherValue = SenderRef1.Text()
                        objArrayList.Add(objNewIDs)
                        Session.Item(SessionVars.SV_AnimalIDs) = objArrayList
                    End If

                End If

                Session.Item(SessionVars.SV_TempPickSenderList) = dtData
                Session.Item(SessionVars.SV_SenderRef) = SenderRef1.Text()
                Session.Item(SessionVars.SV_BatchSubmissionID) = 0

            Else
                Exit Sub
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to lookup Sender Ref.", ex)
        End Try

        sNextPage = CStr(Session.Item(SessionVars.SV_AddSampleNextPage))

        If Not bRedirectSubmissionDetails Then
            'Bread crumbs

            Try
                Dim objCrumbArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
                If Not objCrumbArrayList Is Nothing Then
                    objCrumbArrayList(1) = "Submission Samples"
                    objCrumbArrayList(2) = "Add Sample"
                    objCrumbArrayList(3) = "Search Sender Ref"
                    Session.Item(SessionVars.SV_BreadCrumbs) = objCrumbArrayList
                End If
            Catch ex As Exception
                clsAppError.DisplayError("Bread Crumb Error, AddSubmission.aspx.", ex)
            End Try
            Response.Redirect("SearchSender.aspx")

        Else
            Response.Redirect(CStr(Session.Item(SessionVars.SV_AddSampleNextPage)))
        End If
    End Sub

    Private Sub VLAHeader1_HomeClick(ByVal sender As Object, ByVal e As HistopathologySystem.HomeLinkEventArgs) Handles VLAHeader1.HomeClick
        Dim sMessage As System.Text.StringBuilder = New System.Text.StringBuilder

        If CType(Session.Item(SessionVars.SV_EditingBatch), Boolean) Then
            sMessage.Append("You are currently editing a submission. Any changes that you have made since you last saved the submission will be lost. Are you sure you wish to exit?")
        ElseIf CType(Session.Item(SessionVars.SV_AssignBlocks), Boolean) Then
            sMessage.Append("You are currently assigning tissues to blocks. Any block assignment that you have completed since you last saved will be lost. Are you sure you wish to exit?")
        ElseIf CType(Session.Item(SessionVars.SV_CreatingNewBatch), Boolean) Then
            sMessage.Append("You are currently creating a new submission. If you exit now all the data you have entered will be lost. Are you sure you wish to exit?")
        Else
            sMessage.Append("Any changes that have been made will be discarded, are you sure you wish to exit without saving?")
        End If

        Page.RegisterStartupScript("navigate", PromptBeforeNavigateScript(sMessage.ToString(), "Home.aspx"))
        e.bNavigateHome = False
    End Sub

    Private Sub ddlSenderRef_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ddlSenderRef.SelectedIndexChanged
        Try
            If ddlSenderRef.SelectedIndex >= 0 Then
                Dim objAnimal As New HistopathologyLib.clsAnimal
                Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
                Dim dtBlockAnimals As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL)
                Dim dtAnimal As DataTable = CType(Session.Item(SessionVars.SV_AnimalTable), DataTable)
                Dim sFilter As String
                Dim foundRows As DataRow()
                Dim foundAnimal As DataRow()
                Dim iAnimalID As Integer

                iAnimalID = CInt(ddlSenderRef.SelectedItem.Value())
                sFilter = "ID=" & iAnimalID
                foundRows = dtAnimal.Select(sFilter)

                If Not foundRows Is Nothing And foundRows.Length > 0 Then
                    'Check this animal is not already in the local dataset

                    foundAnimal = dtBlockAnimals.Select(sFilter)
                    If Not foundAnimal Is Nothing And foundAnimal.Length = 0 Then
                        If Not objAnimal.NewExistingRecord(dtBlockAnimals, _
                                                           foundRows(0)("SenderRef").ToString(), _
                                                           foundRows(0)("HistologyRef").ToString(), _
                                                           foundRows(0)("NextBlockRef").ToString(), _
                                                           foundRows(0)("RowStamp"), _
                                                           foundRows(0)("ID"), _
                                                           foundRows(0)("HistoRefSet"), _
                                                           foundRows(0)("OnHold"), _
                                                           foundRows(0)("PMDate").ToString(), _
                                                           foundRows(0)("PMDateSet")) Then
                            Throw New Exception("Animal.NewExistingRecord returned false.")
                        End If
                    End If

                    '----- Pre Booked Block Functionality -----
                    If Not objAnimal.GetPreBookedBlocks(iAnimalID, dsBatchDetails) Then
                        Throw New Exception("Animal.GetPreBookedBlocks returned false.")
                    End If
                    '----- End Pre Booked Block Functionality -----

                    Session.Item(SessionVars.SV_AnimalID) = foundRows(0)("ID")
                    Session.Item(SessionVars.SV_SenderRef) = foundRows(0)("SenderRef").ToString()
                End If
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to select the choose sender ref.", ex)
        End Try
        Response.Redirect("SubmissionDetailsBlock.aspx")
    End Sub

    Private Sub cbUseValidation_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cbUseValidation.CheckedChanged
        SenderRef1.SetValidate(cbUseValidation.Checked)
        Session.Item(SessionVars.SV_UseValidation) = cbUseValidation.Checked
    End Sub

#End Region

#Region "Private Functions"

    Private Sub InitialiseUserAreaControls()
        Try
            SenderRef1.SetValidate(False)

            'Only display the mouse controls if we are copying a submission and the user is mouse user
            If CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Mouse Bioassay" Or _
             CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Histopath" Then
                SenderRef1.SetValidate(True)
                SenderRef1.SetREVTooltip("Mouse number format: MCNNNNNN")
                If (CStr(Session.Item(SessionVars.SV_AddSampleNextPage)) = "CopyBatch.aspx" Or _
                  CStr(Session.Item(SessionVars.SV_AddSamplePrevPage)) = "CopyBatchBlocks.aspx") Then
                    ctlMouseDiv.Visible = True
                    ctlUploadDiv.Visible = True
                Else
                    ctlUploadDiv.Visible = False
                    ctlMouseDiv.Visible = False
                End If
            Else
                ctlUploadDiv.Visible = False
                ctlMouseDiv.Visible = False
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

    Private Sub SetProjectOverride()
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dsOldBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_OldBatchDetails), DataSet)
            Dim dtBatch As DataTable
            Dim bProjectOverride As Boolean

            '   cbProjectOverride.Enabled = False

            If Not dsBatchDetails Is Nothing Then
                If CStr(Session.Item(SessionVars.SV_AddSampleNextPage)) = "CopyBatch.aspx" Or _
                    CStr(Session.Item(SessionVars.SV_AddSampleNextPage)) = "CopyBatchBlocks.aspx" Then
                    dtBatch = dsOldBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)
                Else
                    dtBatch = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)
                End If

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

    Private Function CheckIfFoundAnimalsAreDeleted(ByVal drFoundRows As DataRow(), ByRef iNotDeletedAnimal As Integer) As Boolean
        Dim dr As DataRow

        For Each dr In drFoundRows
            If Not dr("RowState") = DataRowState.Deleted Then
                iNotDeletedAnimal = dr("ID")
                Return False
            End If
        Next
        Return True
    End Function

    Private Sub DisplaySenderRef()
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtBatch As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)
            Dim dtAnimal As DataTable
            Dim objAnimal As New HistopathologyLib.clsAnimal
            Dim bAssignBlocks As Boolean = CType(Session.Item(SessionVars.SV_AssignBlocks), Boolean)

            If Not objAnimal.GetAnimalsForBatch(CInt(Session.Item(SessionVars.SV_BatchID)), dtAnimal) Then
                Throw New Exception("Animal.GetAnimals for batch returned false.")
            End If

            ddlSenderRef.Visible = False
            SenderRef1.Visible = True
            rfvSenderDropDown.Enabled = False
            SenderRef1.SetEnabled(True)
            ctlDivLookup.Visible = True

            'Display the drop down list of Sender refs if the submission is of wet tissue
            If Not dtBatch Is Nothing And dtBatch.Rows.Count > 0 Then
                If dtBatch.Rows(0)("Cassetted") = 0 And bAssignBlocks = True Then
                    If Not dtAnimal Is Nothing Then
                        ddlSenderRef.DataSource = dtAnimal
                        ddlSenderRef.DataTextField = "SenderRef"
                        ddlSenderRef.DataValueField = "ID"
                        ddlSenderRef.DataBind()
                        AddItemToDropDownList(ddlSenderRef, "")
                        Session.Item(SessionVars.SV_AnimalTable) = dtAnimal
                    End If
                    ddlSenderRef.Visible = True
                    SenderRef1.Visible = False
                    rfvSenderDropDown.Enabled = True
                    SenderRef1.SetEnabled(False)
                    btnNext.Enabled = False
                    ctlDivLookup.Visible = False
                    SetFocus(ddlSenderRef)
                End If
            End If

            If SenderRef1.Visible = True Then
                SenderRef1.Text() = CStr(Session.Item(SessionVars.SV_SenderRef))
                SenderRef1.SetFocus()
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to display the Sender ref input field.", ex)
        End Try
    End Sub

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
        Dim dsOldBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_OldBatchDetails), DataSet)
        Dim objAnimal As New HistopathologyLib.clsAnimal
        Dim dtExistingAnimal As DataTable = Nothing
        Dim sFilter As String
        Dim drFoundMouse As DataRow()
        Dim dtAnimal As DataTable
        Dim dtSamples As DataTable

        If bCassetted Then
            dtAnimal = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL)
            dtSamples = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE)
        Else
            dtAnimal = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE)
            dtSamples = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_SUBMISSION_TABLE)
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
                            If Not objAnimal.ValidateAnimalBlocks(dsBatchDetails, dsOldBatchDetails, drFoundMouse(0)("ID"), _
                                                                        CInt(Session.Item(SessionVars.SV_SelectedAnimal)), _
                                                                        CInt(Session.Item(SessionVars.SV_SelectedAnimalNumberBlocks)), _
                                                                        sMouseNumber, _
                                                                        sValidationError, _
                                                                        IsBatchPreCassetted(dsOldBatchDetails, CInt(Session.Item(SessionVars.SV_BatchID)))) Then
                                Throw New Exception("Animal.ValidateAnimalBlocks returned false.")
                            End If
                        End If
                    ElseIf drFoundMouse.Length = 0 Then
                        If Not objAnimal.GetPreBookedBlocks(0, dsBatchDetails) Then
                            Throw New Exception("Animal.GetPreBookedBlocks returned false.")
                        End If

                        sValidationError = ""
                        If bCassetted Then
                            If Not objAnimal.ValidateAnimalBlocks(dsBatchDetails, dsOldBatchDetails, 0, _
                                                                    CInt(Session.Item(SessionVars.SV_SelectedAnimal)), _
                                                                    CInt(Session.Item(SessionVars.SV_SelectedAnimalNumberBlocks)), _
                                                                    sMouseNumber, _
                                                                    sValidationError, _
                                                                    IsBatchPreCassetted(dsOldBatchDetails, CInt(Session.Item(SessionVars.SV_BatchID)))) Then
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
            Session.Item(SessionVars.SV_AnimalID) = 0
            Return False
        End If

    End Function


    Private Sub ProcessImport(ByRef dtMouseNumbers As DataTable)

        Dim bRedirectToLookup As Boolean
        Dim sNextPage As String

        Try
            Dim dtAnimal As DataTable
            Dim dtSamples As DataTable
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
            Dim iAnimalID As Integer
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dsOldBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_OldBatchDetails), DataSet)
            Dim objArrayList As ArrayList = CType(Session.Item(SessionVars.SV_AnimalIDs), ArrayList)

            Dim dtBatch As DataTable = dsOldBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)

            If bCassetted Then
                dtAnimal = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL)
                dtSamples = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE)
            Else
                dtAnimal = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE)
                dtSamples = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_SUBMISSION_TABLE)
            End If

            If Not ValidateUploadNumbers(dtMouseNumbers, sError) Then
                If IsBatchPreCassetted(dsOldBatchDetails, CInt(Session.Item(SessionVars.SV_BatchID))) Then
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
                ' Validate the mouse number
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

                        If Not objArrayList Is Nothing Then
                            Dim objNewIDs As New HistopathologyLib.clsIDPairs
                            objNewIDs.OldID = CInt(Session.Item(SessionVars.SV_SelectedAnimal))
                            objNewIDs.NewID = drFoundMouse(0)("ID")
                            objNewIDs.Value = drFoundMouse(0)("SenderRef").ToString()
                            objNewIDs.OtherValue = "Uploaded Mouse"
                            objArrayList.Add(objNewIDs)
                            Session.Item(SessionVars.SV_AnimalIDs) = objArrayList
                        End If

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
                        End If
                    ElseIf drFoundMouse.Length = 0 Then
                        'Not in the database

                        drFoundMouseCheck = dtAnimal.Select(sFilter)

                        'Check that it isnt already in the local dataset
                        If Not drFoundMouseCheck Is Nothing And drFoundMouseCheck.Length = 0 Then
                            If Not objAnimal.NewRecord(dtAnimal, sMouseNumber, iAnimalID, False) Then
                                Throw New Exception("Animal.NewRecord returned false.")
                            End If
                            Session.Item(SessionVars.SV_AnimalID) = iAnimalID
                        End If

                        objArrayList = CType(Session.Item(SessionVars.SV_AnimalIDs), ArrayList)

                        If Not objArrayList Is Nothing Then
                            Dim objNewIDs As New HistopathologyLib.clsIDPairs
                            objNewIDs.OldID = CInt(Session.Item(SessionVars.SV_SelectedAnimal))
                            objNewIDs.NewID = CInt(Session.Item(SessionVars.SV_AnimalID))
                            objNewIDs.Value = sMouseNumber
                            objNewIDs.OtherValue = "Uploaded Mouse"
                            objArrayList.Add(objNewIDs)
                            Session.Item(SessionVars.SV_AnimalIDs) = objArrayList
                        End If
                    End If
                End If
            Next
            Session.Item(SessionVars.SV_BatchSubmissionID) = 0
        Catch ex As Exception
            clsAppError.DisplayError("Failed to lookup 'Sender Ref'.", ex)
        End Try


        Try
            sNextPage = CStr(Session.Item(SessionVars.SV_AddSampleNextPage))
            If sNextPage = "SubmissionDetailsBlock.aspx" Then
                'Bread crumbs
                Dim objCrumbArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
                If Not objCrumbArrayList Is Nothing Then
                    objCrumbArrayList(1) = "Submission Samples"
                    objCrumbArrayList(2) = "Blocking"
                    objCrumbArrayList(3) = "Sample Blocks"
                    Session.Item(SessionVars.SV_BreadCrumbs) = objCrumbArrayList
                End If
            ElseIf sNextPage = "SubmissionDetails.aspx" Then
                'Bread crumbs
                Dim objCrumbArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
                If Not objCrumbArrayList Is Nothing Then
                    objCrumbArrayList(1) = "Submission Samples"
                    objCrumbArrayList(2) = "Tissuing"
                    objCrumbArrayList(3) = "Sample Details"
                    Session.Item(SessionVars.SV_BreadCrumbs) = objCrumbArrayList
                End If
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Bread Crumb Error, AddSubmission.aspx.", ex)
        End Try

        Response.Redirect(sNextPage)
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
