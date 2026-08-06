Imports System.Text.RegularExpressions

Partial Class BookHistologyRef
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader

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
        VLAHeader1.PageTitle = "Block Book Histology Refs"
        CheckPermissions()
        SetClientValidation()
        VLAHeader1.SubmissioNoVisible() = False

        If Not IsPostBack Then
            PromptBeforeSaveScript("Are you sure you want to book the specified histology numbers.  Continue?", btnOK)
            InitialiseHistologyGrid()
            LoadLookupLists()
        End If

        SetEnterKeyPress()

    End Sub

#Region "Load Lookup Lists"

    Private Sub LoadLookupLists()
        Try
            Dim dtData As DataTable
            Dim li As ListItem

            'This datatable gets populated in InitialiseHistologyGrid
            dtData = CType(Session.Item(SessionVars.SV_HistologyRefTable), DataTable)

            If Not dtData Is Nothing Then
                ddlHistologyType.DataSource = dtData
                ddlHistologyType.DataValueField = "Type"
                ddlHistologyType.DataTextField = "Description"
                ddlHistologyType.DataBind()
                AddItemToDropDownList(ddlHistologyType)
            End If

            RemovePGNumberOption()

        Catch ex As Exception
            clsAppError.DisplayError("Error initialising the Histology dropdown on the Block book Histology ref page.", ex)
        End Try
    End Sub

#End Region

#Region "Grid Handling"

    Private Sub InitialiseHistologyGrid()
        Try
            Dim objHistology As New HistopathologyLib.clsHistology
            Dim dtData As DataTable

            If Not objHistology.GetHistologyRefsTable(dtData) Then
                Throw New Exception("Histology.GetHistologyRefsTable returned false.")
            End If

            Session.Item(SessionVars.SV_HistologyRefTable) = dtData

            grdHistologyRefs.DataSource = dtData
            grdHistologyRefs.DataBind()

        Catch ex As Exception
            clsAppError.DisplayError("Error initialising the Histology grid on the Block book Histology ref page.", ex)
        End Try
    End Sub

#End Region

#Region "Private Functions"

    Public Function CheckRange(ByVal iVal) As Integer
        If iVal >= 10000 And iVal < 20000 Then
            Return HistologyRefType.eNeuropath
        ElseIf iVal >= 20000 And iVal < 30000 Then
            Return HistologyRefType.eAbattoirSurvey
        ElseIf iVal >= 30000 And iVal < 40000 Then
            Return HistologyRefType.eTBDiag
        ElseIf iVal >= 40000 And iVal < 60000 Then
            Return HistologyRefType.eGeneralPool
        ElseIf iVal >= 60000 And iVal < 90000 Then
            Return HistologyRefType.eMouseProjects
        End If
    End Function

    Private Sub SetEnterKeyPress()
        SetFocus(ddlHistologyType)
        SetDropDownControlOnEnter(ddlHistologyType, txtNumberToBook.ClientID)
        SetTextboxDefaultButton(txtNumberToBook, btnOK)
    End Sub

    Private Sub CheckPermissions()
        VLAHeader1.getUserDetails()

        Dim sGroupName As String = Session(SessionVars.SV_HeaderGroupName)

        If sGroupName = "Customer" Then
            Response.Redirect("Home.aspx")
        ElseIf sGroupName = "Histopathology User" Then
            'nothing
        ElseIf sGroupName = "Maintenance" Then
            'Nothing
        Else
            Response.Redirect("Home.aspx")
        End If
    End Sub

    Private Sub RemovePGNumberOption()
        Dim iItemCount As Integer = 0
        For iItemCount = ddlHistologyType.Items.Count - 1 To 0 Step -1
            If ddlHistologyType.Items(iItemCount).Text = "use pg number" Then
                ddlHistologyType.Items.RemoveAt(iItemCount)
            End If
        Next
    End Sub

    Private Sub UpdateHistologyRefs(ByVal iHistoType As Integer)
        Dim dtData As DataTable = CType(Session.Item(SessionVars.SV_HistologyRefTable), DataTable)
        Dim sFilter As String
        Dim drFoundRow As DataRow()
        Dim iNextHistologyRef As Integer
        Dim sNextHistologyRef As String
        Dim sOldHistologyRef As String
        Dim objHistology As New HistopathologyLib.clsHistology
        'Dim iBlockrefFrom As Integer = 0
        'Dim iBlockRefTo As Integer = 0

        'If txtBlockRefFrom.Text <> "" Then
        '    iBlockrefFrom = CInt(txtBlockRefFrom.Text)

        '    If txtBlockRefTo.Text <> "" Then
        '        iBlockRefTo = CInt(txtBlockRefTo.Text)

        '        If iBlockRefTo <= iBlockrefFrom Then
        '            ctlDIV.InnerHtml = "<font color=""Red"">The requested block ref range cannot be created.</font>"
        '            Exit Sub
        '        End If
        '    Else
        '        iBlockRefTo = iBlockrefFrom
        '    End If
        'End If

        'For the selected type in the drop down list increment the related value in the datatable by the
        'required amount and then try to update the database.
        sFilter = "Type=" & iHistoType
        If Not dtData Is Nothing And dtData.Rows.Count > 0 Then
            drFoundRow = dtData.Select(sFilter)
            If Not drFoundRow Is Nothing And drFoundRow.Length > 0 Then
                sOldHistologyRef = drFoundRow(0)("NextHistologyRef").ToString()
                iNextHistologyRef = CInt(sOldHistologyRef)
                iNextHistologyRef = iNextHistologyRef + Convert.ToInt32(txtNumberToBook.Text())
                sNextHistologyRef = CStr(iNextHistologyRef)

                'Check that the new value doesnt break the boundaries
                Select Case iHistoType
                    Case HistologyRefType.eNeuropath
                        If iNextHistologyRef >= 20000 Then
                            ctlDIV.InnerHtml = "<p><font color=""Red"">Cannot book the required histology numbers as the maximum neuropath histology number is 19999.</font></p>"
                            Exit Sub
                        End If
                    Case HistologyRefType.eAbattoirSurvey
                        If iNextHistologyRef >= 30000 Then
                            ctlDIV.InnerHtml = "<p><font color=""Red"">Cannot book the required histology numbers as the maximum abattoir survey histology number is 29999.</font></p>"
                            Exit Sub
                        End If
                    Case HistologyRefType.eTBDiag
                        If iNextHistologyRef >= 40000 Then
                            ctlDIV.InnerHtml = "<p><font color=""Red"">Cannot book the required histology numbers as the maximum TB diagnostic histology number is 39999.</font></p>"
                            Exit Sub
                        End If
                    Case HistologyRefType.eGeneralPool
                        If iNextHistologyRef >= 60000 Then
                            ctlDIV.InnerHtml = "<p><font color=""Red"">Cannot book the required histology numbers as the maximum general pool histology number is 59999.</font></p>"
                            Exit Sub
                        End If

                    Case HistologyRefType.eMouseProjects
                        If iNextHistologyRef >= 90000 Then
                            ctlDIV.InnerHtml = "<p><font color=""Red"">Cannot book the required histology numbers as the maximum mouse project histology number is 89999.</font></p>"
                            Exit Sub
                        End If
                End Select

                drFoundRow(0)("NextHistologyRef") = sNextHistologyRef
                Dim objErrorList As New ArrayList

                Dim bSuccess = objHistology.UpdateHistologyRefs(drFoundRow(0), objErrorList)

                If Not bSuccess Then
                    ctlDIV.InnerHtml = "<p><font color=""Red"">The database has not been updated because the following error(s) occurred:</font></p><p><font color=""Red"">" & Join(objErrorList.ToArray, "</font></p><p><font color=""Red"">") & "</font></p><p><font color=""Red"">Please Try Again.</font></p>"
                Else
                    ctlDIV.InnerHtml = "<p><font color=""Green"">You have successfully booked Histology numbers in the range " & sOldHistologyRef & " - " & CStr(CInt(sNextHistologyRef) - 1) & ", inclusive.</font></p>"

                    'If iBlockrefFrom > 0 Then
                    'ProcessMultipleBookings(iBlockrefFrom, iBlockRefTo, sOldHistologyRef, Convert.ToInt32(txtNumberToBook.Text()))
                    'End If
                End If

                InitialiseHistologyGrid()
            End If
        End If
    End Sub

#End Region

    Private Sub ProcessMultipleBookings(ByVal iBlockRefFrom As Integer, ByVal iBlockRefTo As Integer, ByVal sCurrentHistologyNumber As String, ByVal iNumberToBook As Integer)
        Dim objAnimal As New HistopathologyLib.clsAnimal
        Dim objBlock As New HistopathologyLib.clsBlock
        Dim iAnimalId As Integer
        Dim iAnimalCount As Integer
        Dim iBlockCount As Integer
        Dim dtAnimaldata As DataTable
        Dim dtBlockData As DataTable
        Dim sErrorMessage As String = ""
        Dim drFoundBlocks As DataRow()
        Dim sBlockRef As String = ""
        Dim sHistologyRef As String = ""
        Dim dDate As Date

        For iAnimalCount = CInt(sCurrentHistologyNumber) To (CInt(sCurrentHistologyNumber) + iNumberToBook) - 1

            sHistologyRef = Right$(dDate.Now.Year(), 2).ToString & "/" & iAnimalCount

            If Not objAnimal.GetAnimalByHistologyRef(sHistologyRef, dtAnimaldata) Then
                sErrorMessage = sErrorMessage & "Failed to retrieve sample data for " & sHistologyRef & ".<br>"
            Else
                If dtAnimaldata.Rows.Count = 0 Then
                    If Not objAnimal.AddAnimal("", iAnimalId, sHistologyRef) Then
                        sErrorMessage = sErrorMessage & "<font color=""Red"">Failed to retrieve sample data for " & sHistologyRef & ".</font><br>"
                    Else

                        If Not objAnimal.GetAnimalBlocks(dtBlockData, sHistologyRef) Then
                            sErrorMessage = sErrorMessage & "<font color=""Red"">Blocks not booked for sample " & sHistologyRef & ".</font><br>"
                        Else
                            ' Animal created now create the pre-booked blocks.
                            For iBlockCount = iBlockRefFrom To iBlockRefTo
                                If iBlockCount < 10 Then
                                    sBlockRef = "0" & Convert.ToString(iBlockCount)
                                Else
                                    sBlockRef = Convert.ToString(iBlockCount)
                                End If

                                drFoundBlocks = dtBlockData.Select("BlockRef=" & sBlockRef)

                                If drFoundBlocks.Length > 0 Then
                                    sErrorMessage = sErrorMessage & "<font color=""Red"">Block " & sBlockRef & "  not booked for sample " & sHistologyRef & " as it already exists.</font><br>"
                                Else
                                    If Not objBlock.CreatePreBookedBlock(iAnimalId, sBlockRef) Then
                                        sErrorMessage = sErrorMessage & "<font color=""Red"">Block " & sBlockRef & "  not booked for sample " & sHistologyRef & ".</font><br>"
                                    End If
                                End If
                            Next
                        End If
                    End If
                Else
                    If Not objAnimal.GetAnimalBlocks(dtBlockData, sHistologyRef) Then
                        sErrorMessage = sErrorMessage & "Blocks not booked for sample " & sHistologyRef & ".<br>"
                    Else
                        ' Animal created now create the pre-booked blocks.
                        For iBlockCount = iBlockRefFrom To iBlockRefTo

                            If iBlockCount < 10 Then
                                sBlockRef = "0" & Convert.ToString(iBlockCount)
                            Else
                                sBlockRef = Convert.ToString(iBlockCount)
                            End If

                            drFoundBlocks = dtBlockData.Select("BlockRef=" & sBlockRef)

                            If drFoundBlocks.Length > 0 Then
                                sErrorMessage = sErrorMessage & "<font color=""Red"">Block " & sBlockRef & "  not booked for sample " & sHistologyRef & " as it already exists.</font><br>"
                            Else
                                If Not objBlock.CreatePreBookedBlock(dtAnimaldata.Rows(0)("ID"), sBlockRef) Then
                                    sErrorMessage = sErrorMessage & "<font color=""Red"">Block " & sBlockRef & "  not booked for sample " & sHistologyRef & ".</font><br>"
                                End If
                            End If
                        Next
                    End If
                End If
            End If
        Next

        If sErrorMessage <> "" Then
            ctlBlockBookDiv.InnerHtml = sErrorMessage
        Else
            ctlBlockBookDiv.InnerHtml = "<font color=""Green"">Successfully booked requested blocks.</font>"
        End If
    End Sub

#Region "Event Handlers"

    Private Sub btnBack_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnBack.Click
        Response.Redirect("BookingMenu.aspx")
    End Sub

    Private Sub btnOK_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnOK.Click
        Try
            ctlDIV.InnerHtml = ""

            rfvHistoType.Validate()
            revNoToBook.Validate()
            rfvNoToBook.Validate()

            If rfvHistoType.IsValid And _
               revNoToBook.IsValid And _
               rfvNoToBook.IsValid Then

                Dim iSelectedItem = ddlHistologyType.SelectedItem.Value

                Select Case iSelectedItem
                    Case HistologyRefType.eNeuropath
                        UpdateHistologyRefs(HistologyRefType.eNeuropath)
                    Case HistologyRefType.eAbattoirSurvey
                        UpdateHistologyRefs(HistologyRefType.eAbattoirSurvey)
                    Case HistologyRefType.eTBDiag
                        UpdateHistologyRefs(HistologyRefType.eTBDiag)
                    Case HistologyRefType.eGeneralPool
                        UpdateHistologyRefs(HistologyRefType.eGeneralPool)
                    Case HistologyRefType.eMouseProjects
                        UpdateHistologyRefs(HistologyRefType.eMouseProjects)
                End Select
            Else
                ctlDIV.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to block book required Histology Refs.", ex)
        End Try
    End Sub

    Private Sub lbCheckBlockRefs_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lbCheckBlockRefs.Click
        OpenDownloadPopup("SearchBlockRefs.aspx", Me.Page)
    End Sub
#End Region

#Region "Validation"

    Private Function SetClientValidation() As Boolean
        If HttpContext.Current.Request.Browser.JavaScript Then
            Dim scr As New System.Text.StringBuilder

            scr.Append("<SCRIPT language=""Javascript"">" + vbNewLine)
            scr.Append("function ClientValidateBlockRef(sender, args)" + vbNewLine)
            scr.Append("{" + vbNewLine)
            scr.Append("    var sBlockRef = args.Value;" + vbNewLine)
            scr.Append("    if (sBlockRef == ""00"")" + vbNewLine)
            scr.Append("    {" + vbNewLine)
            scr.Append("        args.IsValid = false;" + vbNewLine)
            scr.Append("        return;" + vbNewLine)
            scr.Append("    }" + vbNewLine)
            scr.Append(vbNewLine)
            scr.Append("    if (sBlockRef == ""000"")" + vbNewLine)
            scr.Append("    {" + vbNewLine)
            scr.Append("        args.IsValid = false;" + vbNewLine)
            scr.Append("        return;" + vbNewLine)
            scr.Append("    }" + vbNewLine)
            scr.Append(vbNewLine)
            scr.Append("    if (sBlockRef.length <=2)" + vbNewLine)
            scr.Append("    {" + vbNewLine)
            scr.Append("        var expBlockRef = /[0-9][0-9]/;" + vbNewLine)
            scr.Append("        if (expBlockRef.test(sBlockRef))" + vbNewLine)
            scr.Append("        {" + vbNewLine)
            scr.Append("            args.IsValid = true;" + vbNewLine)
            scr.Append("            return;" + vbNewLine)
            scr.Append("        }" + vbNewLine)
            scr.Append("        else" + vbNewLine)
            scr.Append("        {" + vbNewLine)
            scr.Append("            args.IsValid = false;" + vbNewLine)
            scr.Append("            return;" + vbNewLine)
            scr.Append("        }" + vbNewLine)
            scr.Append("    }" + vbNewLine)
            scr.Append("    else" + vbNewLine)
            scr.Append("    {" + vbNewLine)
            scr.Append("        var expBlockRef = /[1-9][0-9][0-9]/;" + vbNewLine)
            scr.Append("        if (expBlockRef.test(sBlockRef))" + vbNewLine)
            scr.Append("        {" + vbNewLine)
            scr.Append("            args.IsValid = true;" + vbNewLine)
            scr.Append("            return;" + vbNewLine)
            scr.Append("        }" + vbNewLine)
            scr.Append("        else" + vbNewLine)
            scr.Append("        {" + vbNewLine)
            scr.Append("            args.IsValid = false;" + vbNewLine)
            scr.Append("            return;" + vbNewLine)
            scr.Append("        }" + vbNewLine)
            scr.Append("    }" + vbNewLine)
            scr.Append("}" + vbNewLine)
            scr.Append("</SCRIPT>" + vbNewLine)

            Me.Page.RegisterClientScriptBlock("SetBlockRefClientValidation", scr.ToString())
            Return True
        Else
            Return False
        End If
    End Function

    Public Sub ValidateBlockRefRef(ByVal sender As Object, ByVal args As ServerValidateEventArgs)
        Dim sBlockRef As String = CStr(args.Value)
        Dim match As Match

        If sBlockRef = "00" Then
            args.IsValid = False
            Exit Sub
        ElseIf sBlockRef = "000" Then
            args.IsValid = False
            Exit Sub
        Else
            If sBlockRef.Length <= 2 Then
                Dim revBlockRef As Regex = New Regex("[0-9][0-9]")
                match = revBlockRef.Match(sBlockRef)

                If match.Success Then
                    args.IsValid = True
                    Exit Sub
                Else
                    args.IsValid = False
                    Exit Sub
                End If
            Else
                Dim revBlockRef As Regex = New Regex("[1-9][0-9][0-9]")
                match = revBlockRef.Match(sBlockRef)

                If match.Success Then
                    args.IsValid = True
                    Exit Sub
                Else
                    args.IsValid = False
                    Exit Sub
                End If
            End If
        End If

    End Sub

#End Region


End Class
