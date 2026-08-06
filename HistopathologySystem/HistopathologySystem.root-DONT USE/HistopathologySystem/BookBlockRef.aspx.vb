Imports HistopathologyLib
Imports System.Text.RegularExpressions

Partial Class BookBlockRef
    Inherits System.Web.UI.Page

#Region " Web Form Designer Generated Code "

    'This call is required by the Web Form Designer.
    <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()

    End Sub

    'NOTE: The following placeholder declaration is required by the Web Form Designer.
    'Do not delete or move it.
    Private designerPlaceholderDeclaration As System.Object

    Private Sub Page_Init(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Init
        'CODEGEN: This method call is required by the Web Form Designer
        'Do not modify it using the code editor.
        InitializeComponent()
    End Sub

#End Region

    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents SenderRefFrom As SenderRef
    Protected WithEvents SenderRefTo As SenderRef

    Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        VLAHeader1.PageTitle = "Book Blocks"
        SetClientValidation()
        CheckPermissions()
        SenderRefFrom.SetMandatory(True)
        SenderRefTo.SetMandatory(False)
        PromptBeforeSaveScript("Are you sure you want to pre-book the specified blocks.  Continue?", btnOk)
        VLAHeader1.SubmissioNoVisible() = False

        If Not IsPostBack Then
            SenderRefFrom.SetFocus()
        End If

    End Sub

#Region "Private Functions"

    Private Sub CheckPermissions()
        VLAHeader1.getUserDetails()
        Dim sGroupName As String = Session(SessionVars.SV_HeaderGroupName)
        If sGroupName = "Customer" Then
            Response.Redirect("Home.aspx")
        ElseIf sGroupName = "Histopathology User" Then
            'Nothing
        ElseIf sGroupName = "Maintenance" Then
            'Nothing
        Else
            Response.Redirect("Home.aspx")
        End If
    End Sub

    Private Sub SetEnterKeyPress()
        SetTextboxDefaultButton(txtBlockRefFrom, btnOk)
        SetTextboxDefaultButton(txtBlockRefTo, btnOk)
    End Sub

    Private Sub InitialiseControls()
        SenderRefFrom.SetMandatory(True)
        SenderRefTo.SetMandatory(False)
    End Sub

    Private Function PadPGNumberZeroes(ByVal iNumber As Integer) As String
        Dim sNumber As String = CStr(iNumber)

        Select Case sNumber.Length
            Case 1
                Return "000" & sNumber
            Case 2
                Return "00" & sNumber
            Case 3
                Return "0" & sNumber
            Case Else
                Return sNumber
        End Select
    End Function

    Private Function PadMouseNumberZeroes(ByVal iNumber As Integer) As String
        Dim sNumber As String = CStr(iNumber)

        Select Case sNumber.Length
            Case 1
                Return "00000" & sNumber
            Case 2
                Return "0000" & sNumber
            Case 3
                Return "000" & sNumber
            Case 4
                Return "00" & sNumber
            Case 5
                Return "0" & sNumber
            Case Else
                Return sNumber
        End Select
    End Function


    Private Sub ProcessMultipleBookings(ByVal bIsPGNumber As Boolean, ByVal sPGNumberYear As String, _
                                        ByVal iSenderRefFrom As Integer, ByVal iSenderRefTo As Integer, _
                                        ByVal iBlockRefFrom As Integer, ByVal iBlockrefTo As Integer, _
                                        ByVal bIsMouseNumber As Boolean)

        Dim objAnimal As New clsAnimal
        Dim objBlock As New clsBlock
        Dim iAnimalId As Integer
        Dim iAnimalCount As Integer
        Dim iBlockCount As Integer
        Dim dtAnimaldata As DataTable
        Dim dtBlockData As DataTable
        Dim sErrorMessage As String = ""
        Dim drFoundBlocks As DataRow()
        Dim sBlockRef As String = ""
        Dim sSenderRef As String = ""
        Dim iNumberFails As Integer = 0
        Dim iNumberSuccess As Integer = 0
        Dim bError As Boolean = False

        For iAnimalCount = iSenderRefFrom To iSenderRefTo
            iNumberFails = 0
            iNumberSuccess = 0

            If bIsPGNumber Then
                sSenderRef = "PG" & PadPGNumberZeroes(iAnimalCount) & "/" & sPGNumberYear
            ElseIf bIsMouseNumber Then
                sSenderRef = "MC" & PadMouseNumberZeroes(iAnimalCount)
            Else
                sSenderRef = SenderRefFrom.Text
            End If

            If Not objAnimal.GetAnimalBySender(sSenderRef, dtAnimaldata) Then
                sErrorMessage = sErrorMessage & "Failed to retrieve sample data for " & sSenderRef & ".<br>"
            Else
                If dtAnimaldata.Rows.Count = 0 Then
                    If Not objAnimal.AddAnimal(sSenderRef, iAnimalId, "") Then
                        sErrorMessage = sErrorMessage & "<font color=""Red"">Failed to retrieve sample data for " & sSenderRef & ".</font><br>"
                    Else
                        If Not objAnimal.GetAnimalBlocksBySenderRef(dtBlockData, sSenderRef) Then
                            sErrorMessage = sErrorMessage & "<font color=""Red"">Sample :" & sSenderRef & " No blocks booked.</font><br>"
                        Else
                            ' Animal created now create the pre-booked blocks.
                            For iBlockCount = iBlockRefFrom To iBlockrefTo

                                sBlockRef = ConvertBlockRefToString(iBlockCount)

                                drFoundBlocks = dtBlockData.Select("BlockRef=" & sBlockRef)

                                If drFoundBlocks.Length > 0 Then
                                    sErrorMessage = sErrorMessage & "<font color=""Red"">Sample: " & sSenderRef & " Block " & sBlockRef & "  not booked as it already exists.</font><br>"
                                    bError = True
                                Else
                                    If Not objBlock.CreatePreBookedBlock(iAnimalId, sBlockRef) Then
                                        sErrorMessage = sErrorMessage & "<font color=""Red"">Sample: " & sSenderRef & " Block " & sBlockRef & "  not booked.</font><br>"
                                        bError = True
                                    End If
                                End If

                                If bError Then
                                    iNumberFails = iNumberFails + 1
                                Else
                                    iNumberSuccess = iNumberSuccess + 1
                                End If

                                bError = False
                            Next
                        End If
                    End If

                Else
                    If Not objAnimal.GetAnimalBlocksBySenderRef(dtBlockData, sSenderRef) Then
                        sErrorMessage = sErrorMessage & "<font color=""Red"">Sample :" & sSenderRef & " No blocks booked.</font><br>"
                    Else
                        ' Animal created now create the pre-booked blocks.
                        For iBlockCount = iBlockRefFrom To iBlockrefTo

                            sBlockRef = ConvertBlockRefToString(iBlockCount)

                            drFoundBlocks = dtBlockData.Select("BlockRef=" & sBlockRef)

                            If drFoundBlocks.Length > 0 Then
                                sErrorMessage = sErrorMessage & "<font color=""Red"">Sample: " & sSenderRef & " Block " & sBlockRef & "  not booked as it already exists.</font><br>"
                                bError = True
                            Else
                                If Not objBlock.CreatePreBookedBlock(dtAnimaldata.Rows(0)("ID"), sBlockRef) Then
                                    sErrorMessage = sErrorMessage & "<font color=""Red"">Sample: " & sSenderRef & " Block " & sBlockRef & "  not booked.</font><br>"
                                    bError = True
                                End If
                            End If

                            If bError Then
                                iNumberFails = iNumberFails + 1
                            Else
                                iNumberSuccess = iNumberSuccess + 1
                            End If

                            bError = False
                        Next
                    End If
                End If
            End If

            sErrorMessage = sErrorMessage & "<font color=""Green"">Sample: " & sSenderRef & " " & CStr(iNumberSuccess) & " blocks booked, " & CStr(iNumberFails) & " blocks not booked.</font><br>"
        Next

        If sErrorMessage <> "" Then
            ctlDiv.InnerHtml = sErrorMessage
            'lblError.Text = "<font color=""Green"">" & CStr(iNumberSuccess) & " blocks sucessfully booked and " & CStr(iNumberFails) & " blocks not booked.</font>"
        Else

            If txtBlockRefTo.Text <> "" Then
                If SenderRefTo.Text <> "" Then
                    lblError.Text = "<font color=""Green"">Successfully booked blocks " & ConvertBlockRefToString(iBlockRefFrom) _
                           & " to " & ConvertBlockRefToString(iBlockrefTo) & " for range " & SenderRefFrom.Text & " to " & SenderRefTo.Text & ".</font>"
                Else
                    lblError.Text = "<font color=""Green"">Successfully booked blocks " & ConvertBlockRefToString(iBlockRefFrom) _
                           & " to " & ConvertBlockRefToString(iBlockrefTo) & " for " & SenderRefFrom.Text & ".</font>"
                End If
            Else
                If SenderRefTo.Text <> "" Then
                    lblError.Text = "<font color=""Green"">Successfully booked block " & ConvertBlockRefToString(iBlockRefFrom) _
                                    & " for range " & SenderRefFrom.Text & " to " & SenderRefTo.Text & ".</font>"
                Else
                    lblError.Text = "<font color=""Green"">Successfully booked block " & ConvertBlockRefToString(iBlockRefFrom) _
                                    & " for " & SenderRefFrom.Text & ".</font>"
                End If
            End If

        End If
    End Sub

    Private Function ValidatePGNumberRange(ByRef sPGNumberYear As String, ByRef iSenderRefFrom As Integer, ByRef iSenderRefTo As Integer) As Boolean

        Session.Item(SessionVars.SV_HeaderUserArea) = "Neuropath"
        SenderRefFrom.CheckPGNumber(True)
        sPGNumberYear = Right$(SenderRefFrom.Text, 2)

        If SenderRefTo.Text <> "" Then
            If Not SenderRefTo.IsPGNumber Then
                lblError.Visible = True
                lblError.Text = "The range requested cannot be created."
                Return False
            Else
                If Not SenderRefTo.CheckPGNumber(True) Then
                    lblError.Visible = True
                    lblError.Text = "The range requested cannot be created."
                    Return False
                End If
            End If
        End If

        If SenderRefTo.Text <> "" Then

            If sPGNumberYear <> Right$(SenderRefTo.Text, 2) Then
                lblError.Visible = True
                lblError.Text = "PG Number years must be the same."
                Return False
            End If
            iSenderRefTo = SenderRefTo.Text.Substring(2, 4)
            iSenderRefFrom = SenderRefFrom.Text.Substring(2, 4)

            If iSenderRefTo <= iSenderRefFrom Then
                lblError.Visible = True
                lblError.Text = "The requested sender ref range cannot be created."
                Return False
            End If
        Else
            iSenderRefFrom = SenderRefFrom.Text.Substring(2, 4)
            iSenderRefTo = iSenderRefFrom
        End If

        Return True
    End Function

    Private Function ValidateMouseNumberRange(ByRef iSenderRefFrom As Integer, ByRef iSenderRefTo As Integer) As Boolean

        Session.Item(SessionVars.SV_HeaderUserArea) = "Mouse Bioassay"

        SenderRefFrom.CheckMouseNumber(True)
        If SenderRefTo.Text <> "" Then
            If Not SenderRefTo.IsMouseNumber Then
                lblError.Visible = True
                lblError.Text = "The range requested cannot be created."
                Return False
            Else
                If Not SenderRefTo.CheckMouseNumber(True) Then
                    lblError.Visible = True
                    lblError.Text = "The range requested cannot be created."
                    Return False
                End If
            End If
        End If

        If SenderRefTo.Text <> "" Then
            iSenderRefTo = Right(SenderRefTo.Text, 6)
            iSenderRefFrom = Right(SenderRefFrom.Text, 6)

            If iSenderRefTo <= iSenderRefFrom Then
                lblError.Visible = True
                lblError.Text = "The requested sender ref range cannot be created."
                Return False
            End If
        Else
            iSenderRefFrom = Right(SenderRefFrom.Text, 6)
            iSenderRefTo = iSenderRefFrom
        End If

        Return True
    End Function

#End Region

#Region "Event Handlers"

    Private Sub btnBack_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnBack.Click
        Session.Item(SessionVars.SV_HeaderUserArea) = "Histopath"
        Response.Redirect("BookingMenu.aspx")
    End Sub

    Private Sub btnOk_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnOk.Click
        Dim bIsPGNumber As Boolean = False
        Dim sPGNumberYear As String = ""
        Dim bIsMouseNumber As Boolean = False
        Dim iSenderRefFrom As Integer = 0
        Dim iSenderRefTo As Integer = 0
        Dim iBlockRefFrom As Integer
        Dim iBlockRefTo As Integer

        lblError.Text = ""
        ctlDiv.InnerHtml = ""

        bIsPGNumber = SenderRefFrom.IsPGNumber()
        bIsMouseNumber = SenderRefFrom.IsMouseNumber

        If bIsPGNumber Then
            If Not ValidatePGNumberRange(sPGNumberYear, iSenderRefFrom, iSenderRefTo) Then
                Exit Sub
            End If
        ElseIf bIsMouseNumber Then
            If Not ValidateMouseNumberRange(iSenderRefFrom, iSenderRefTo) Then
                Exit Sub
            End If
        End If

        If txtBlockRefFrom.Text <> "" Then
            revBlockRefFrom.Validate()
            iBlockRefFrom = CInt(txtBlockRefFrom.Text)

            If Not revBlockRefFrom.IsValid Then
                lblError.Visible = True
                lblError.Text = "The requested block ref range cannot be created."
                Exit Sub
            End If
        Else
            Exit Sub
        End If

        If txtBlockRefTo.Text <> "" Then
            iBlockRefTo = CInt(txtBlockRefTo.Text)

            revBlockRefTo.Validate()

            If (iBlockRefTo <= iBlockRefFrom) Or Not revBlockRefTo.IsValid Then
                lblError.Visible = True
                lblError.Text = "The requested block ref range cannot be created."
                Exit Sub
            End If
        Else
            iBlockRefTo = iBlockRefFrom
        End If

        Try
            If SenderRefTo.Text = "" Then
                iSenderRefTo = iSenderRefFrom
            End If
            ProcessMultipleBookings(bIsPGNumber, sPGNumberYear, iSenderRefFrom, iSenderRefTo, iBlockRefFrom, iBlockRefTo, bIsMouseNumber)

        Catch ex As Exception
            clsAppError.DisplayError("Failed to create prebooked blocks.", ex)
        End Try
    End Sub

    Private Sub lbSearchBlockRefs_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lbSearchBlockRefs.Click
        OpenDownloadPopup("SearchBlockRefs.aspx?SenderRef=" & SenderRefFrom.Text, Me.Page)
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
