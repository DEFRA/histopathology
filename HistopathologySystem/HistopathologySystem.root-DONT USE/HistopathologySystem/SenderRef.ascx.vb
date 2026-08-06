Imports System.Text.RegularExpressions

Partial  Class SenderRef
    Inherits System.Web.UI.UserControl

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
      

        If CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "TB Diagnostics" Then
            SetTBValidation()
            valSenderRef.ClientValidationFunction = "ValidateTBSenderRef"
        ElseIf CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Mouse Bioassay" Then
            SetMouseValidation()
            valSenderRef.ClientValidationFunction = "ValidateMouseSenderRef"
        ElseIf CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Histopath" Then
          
        ElseIf CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Neuropath" Then
            SetPGValidation()
            valSenderRef.ClientValidationFunction = "ValidatePGSenderRef"
        Else

        End If
    End Sub

    Public Function SetEnterKeyPress(ByVal ctlButton As Button)
        Common.SetTextboxDefaultButton(txtSenderRef, ctlButton)
    End Function

    Public Function SetFocus()
        Common.SetFocus(txtSenderRef)
    End Function

    Public Function IsPGNumber() As Boolean
        If txtSenderRef.Text.Length > 2 Then
            Dim sPGPart As String
            sPGPart = Left(txtSenderRef.Text, 2)
            If sPGPart = "PG" Or sPGPart = "Pg" Or sPGPart = "pG" Or sPGPart = "pg" Then
                Return True
            End If
        Else
            Return False
        End If
    End Function

    Public Function IsMouseNumber() As Boolean
        If txtSenderRef.Text.Length > 2 Then
            Dim sPGPart As String
            sPGPart = Left(txtSenderRef.Text, 2)
            If sPGPart = "MC" Or sPGPart = "mc" Or sPGPart = "Mc" Or sPGPart = "Mc" Then
                Return True
            End If
        Else
            Return False
        End If
    End Function

    Public Function CheckPGNumber(ByVal bDisplayError As Boolean) As Boolean
        Dim strSender As String = txtSenderRef.Text
        Dim strYear As String
        Dim strID As String

        'check that the sender ref entered is actually a PG number
        If strSender.Length > 2 Then

            strSender = Left$(strSender, 2)
            If strSender = "PG" Or strSender = "pg" Or strSender = "pG" Or strSender = "Pg" Then
                'check that its valid
                If IsValid() Then
                    Return True
                Else
                    Return False
                End If
            Else
                SetErrorToolTip("PG Number Format: PGNNNN/NN")
                DisplayError(bDisplayError)
                Return False
            End If
        Else
            SetErrorToolTip("PG Number Format: PGNNNN/NN")
            DisplayError(bDisplayError)
            Return False
        End If
    End Function

    Public Function CheckMouseNumber(ByVal bDisplayError As Boolean) As Boolean
        Dim strSender As String = txtSenderRef.Text
        Dim strYear As String
        Dim strID As String

        'check that the sender ref entered is actually a mouse number
        If strSender.Length > 2 Then

            strSender = Left$(strSender, 2)
            If strSender = "MC" Or strSender = "mc" Or strSender = "mC" Or strSender = "Mc" Then
                'check that its valid
                If IsValid() Then
                    Return True
                Else
                    Return False
                End If
            Else
                SetErrorToolTip("Mouse number format MCNNNNNN")
                DisplayError(bDisplayError)
                Return False
            End If
        Else
            SetErrorToolTip("Mouse number format MCNNNNNN")
            DisplayError(bDisplayError)
            Return False
        End If
    End Function

    Public Sub SetErrorToolTip(ByVal strValue As String)

        lblError.ToolTip = strValue

    End Sub

    Public Sub DisplayError(ByVal bVisible As Boolean)

        lblError.Visible = bVisible

    End Sub

    Public Sub SetMandatory(ByVal bMandatory As Boolean)

        rfvSenderRef.Enabled = bMandatory

    End Sub

    Public Sub SetEnabled(ByVal bEnabled As Boolean)

        txtSenderRef.Enabled = bEnabled

    End Sub

    Public Sub SetREVTooltip(ByVal strValue As String)

        valSenderRef.ToolTip = strValue

    End Sub

    Public Sub SetValidate(ByVal bValidate As Boolean)
        valSenderRef.Enabled = bValidate
    End Sub

    Public Function IsValid() As Boolean
        valSenderRef.Validate()
        Return valSenderRef.IsValid
    End Function

    Public Function IsComplete() As Boolean
        rfvSenderRef.Validate()
        Return rfvSenderRef.IsValid
    End Function

    Public Function CheckSenderRef() As Boolean
        If txtSenderRef.Text = "" Then
            SetErrorToolTip("Required Field")
            lblError.Visible = True
            Return False
        Else
            lblError.Visible = False
            Return IsValid()
        End If
    End Function

    Public Property Text() As String
        Get
            Return txtSenderRef.Text
        End Get
        Set(ByVal sValue As String)
            txtSenderRef.Text = sValue
        End Set
    End Property

#Region "Validation"

    Private Function SetPGValidation() As Boolean
        If HttpContext.Current.Request.Browser.JavaScript Then
            Dim scr As New System.Text.StringBuilder

            scr.Append("<SCRIPT language=""Javascript"">" + vbNewLine)
            scr.Append("function ValidatePGSenderRef(sender, args)" + vbNewLine)
            scr.Append("{" + vbNewLine)
            scr.Append("    var sSenderRef = args.Value;" + vbNewLine)
            scr.Append("    if (sSenderRef.length >= 2)" + vbNewLine)
            scr.Append("    {" + vbNewLine)
            scr.Append("    var sPGCode = sSenderRef.substring(0,2);" + vbNewLine)
            scr.Append("    sPGCode = sPGCode.toUpperCase();" + vbNewLine)
            scr.Append("    var sPGNumber = sSenderRef.substring(2, sSenderRef.length);" + vbNewLine)
            scr.Append("    if (sPGCode == ""PG"")" + vbNewLine)
            scr.Append("    {" + vbNewLine)
            scr.Append("        var expSenderRef = /PG[0-9][0-9][0-9][0-9][/][0-9][0-9]/;" + vbNewLine)
            scr.Append("        sSenderRef = sPGCode + sPGNumber;" + vbNewLine)
            '    scr.Append("        var count = sSenderRef.length;" + vbNewLine)
            scr.Append("        if (sSenderRef.length !=9)" + vbNewLine)
            scr.Append("        {" + vbNewLine)
            scr.Append("            args.IsValid = false;" + vbNewLine)
            scr.Append("        }" + vbNewLine)
            scr.Append("        else" + vbNewLine)
            scr.Append("        {" + vbNewLine)
            scr.Append("            if (expSenderRef.test(sSenderRef))" + vbNewLine)
            scr.Append("            {" + vbNewLine)
            scr.Append("                args.IsValid = true;" + vbNewLine)
            scr.Append("            }" + vbNewLine)
            scr.Append("            else" + vbNewLine)
            scr.Append("            {" + vbNewLine)
            scr.Append("                args.IsValid = false;" + vbNewLine)
            scr.Append("            }" + vbNewLine)
            scr.Append("        }" + vbNewLine)
            scr.Append("    }" + vbNewLine)
            scr.Append("    else" + vbNewLine)
            scr.Append("    {" + vbNewLine)
            scr.Append("    args.IsValid = true;" + vbNewLine)
            scr.Append("    }" + vbNewLine)
            scr.Append("}" + vbNewLine)
            scr.Append("}" + vbNewLine)
            scr.Append("</SCRIPT>" + vbNewLine)

            Me.Page.RegisterClientScriptBlock("SetSenderClientValidation", scr.ToString())
            Return True
        Else
            Return False
        End If
    End Function

    Private Function SetMouseValidation() As Boolean
        If HttpContext.Current.Request.Browser.JavaScript Then
            Dim scr As New System.Text.StringBuilder

            scr.Append("<SCRIPT language=""Javascript"">" + vbNewLine)
            scr.Append("function ValidateMouseSenderRef(sender, args)" + vbNewLine)
            scr.Append("{" + vbNewLine)
            scr.Append("    var sSenderRef = args.Value;" + vbNewLine)
            scr.Append("    if (sSenderRef.length >= 2)" + vbNewLine)
            scr.Append("    {" + vbNewLine)
            scr.Append("    var sCode = sSenderRef.substring(0,2);" + vbNewLine)
            scr.Append("    sCode = sCode.toUpperCase();" + vbNewLine)
            scr.Append("    var sPGNumber = sSenderRef.substring(2, sSenderRef.length);" + vbNewLine)
            scr.Append("    if (sCode == ""MC"")" + vbNewLine)
            scr.Append("    {" + vbNewLine)
            scr.Append("        sSenderRef = sSenderRef.toUpperCase();" + vbNewLine)
            scr.Append("        var expMouseNumber = /MC[0-9][0-9][0-9][0-9][0-9][0-9]/;" + vbNewLine)
            scr.Append("        if (sSenderRef.length !=8)" + vbNewLine)
            scr.Append("        {" + vbNewLine)
            scr.Append("            args.IsValid = false;" + vbNewLine)
            scr.Append("        }" + vbNewLine)
            scr.Append("        else" + vbNewLine)
            scr.Append("        {" + vbNewLine)
            scr.Append("            if (expMouseNumber.test(sSenderRef))" + vbNewLine)
            scr.Append("            {" + vbNewLine)
            scr.Append("                args.IsValid = true;" + vbNewLine)
            scr.Append("                return;" + vbNewLine)
            scr.Append("            }" + vbNewLine)
            scr.Append("            else" + vbNewLine)
            scr.Append("            {" + vbNewLine)
            scr.Append("                args.IsValid = false;" + vbNewLine)
            scr.Append("                return;" + vbNewLine)
            scr.Append("            }" + vbNewLine)
            scr.Append("        }" + vbNewLine)
            scr.Append("    }" + vbNewLine)
            scr.Append("    else" + vbNewLine)
            scr.Append("    {" + vbNewLine)
            scr.Append("        args.IsValid = true;" + vbNewLine)
            scr.Append("    }" + vbNewLine)
            scr.Append(" }" + vbNewLine)
            scr.Append("}" + vbNewLine)
            scr.Append("</SCRIPT>" + vbNewLine)

            Me.Page.RegisterClientScriptBlock("SetMouseValidation", scr.ToString())
            Return True
        Else
            Return False
        End If
    End Function

    Private Function SetTBValidation() As Boolean
        If HttpContext.Current.Request.Browser.JavaScript Then
            Dim scr As New System.Text.StringBuilder

            scr.Append("<SCRIPT language=""Javascript"">" + vbNewLine)
            scr.Append("function ValidateTBSenderRef(sender, args)" + vbNewLine)
            scr.Append("{" + vbNewLine)
            scr.Append("    var sSenderRef = args.Value;" + vbNewLine)
            scr.Append("    if (sSenderRef.length != 11)" + vbNewLine)
            scr.Append("    {" + vbNewLine)
            scr.Append("        args.IsValid = false;" + vbNewLine)
            scr.Append("    }" + vbNewLine)
            scr.Append("    else" + vbNewLine)
            scr.Append("    {" + vbNewLine)
            scr.Append("        var expSenderRef = /[0-9][0-9][/][0-9][0-9][0-9][0-9][0-9][/][0-9][0-9]/;" + vbNewLine)
            scr.Append("        if (expSenderRef.test(sSenderRef))" + vbNewLine)
            scr.Append("        {" + vbNewLine)
            scr.Append("            args.IsValid = true;" + vbNewLine)
            scr.Append("        }" + vbNewLine)
            scr.Append("        else" + vbNewLine)
            scr.Append("        {" + vbNewLine)
            scr.Append("            args.IsValid = false;" + vbNewLine)
            scr.Append("        }" + vbNewLine)
            scr.Append("    }" + vbNewLine)
            scr.Append("}" + vbNewLine)
            scr.Append("</SCRIPT>" + vbNewLine)

            Me.Page.RegisterClientScriptBlock("SetSenderTBValidation", scr.ToString())
            Return True
        Else
            Return False
        End If
    End Function

    Public Sub ValidateSenderRef(ByVal sender As Object, ByVal args As ServerValidateEventArgs)
        Dim sSenderRef As String = CStr(args.Value)
        Dim sSenderNumber As String = ""
        Dim sSenderCode As String = ""

        If CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "TB Diagnostics" Then
            If sSenderRef.Length = 11 Then
                Dim revTBSenderRef As Regex = New Regex("[0-9][0-9](/)[0-9][0-9][0-9][0-9][0-9](/)[0-9][0-9]")
                Dim match As Match

                match = revTBSenderRef.Match(sSenderRef)

                If match.Success Then
                    args.IsValid = True
                    txtSenderRef.Text = sSenderRef
                    Exit Sub
                Else
                    args.IsValid = False
                    Exit Sub
                End If
            Else
                args.IsValid = False
                Exit Sub
            End If
        ElseIf CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Histopath" Then
            '
        ElseIf CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Neuropath" Then

            If sSenderRef.Length >= 2 Then
                sSenderCode = Left$(sSenderRef, 2).ToUpper
                sSenderNumber = Right$(sSenderRef, Len(sSenderRef) - 2)
            Else
                args.IsValid = True
                Exit Sub
            End If

            sSenderCode = sSenderCode.ToUpper()
            If sSenderCode = "PG" Then
                Dim revSenderRef As Regex = New Regex("PG[0-9][0-9][0-9][0-9](/)[0-9][0-9]")
                Dim match As Match

                sSenderRef = sSenderCode + sSenderNumber
                match = revSenderRef.Match(sSenderRef)

                If sSenderRef.Length <> 9 Then
                    args.IsValid = False
                Else
                    If match.Success Then
                        args.IsValid = True
                        txtSenderRef.Text = sSenderRef
                        Exit Sub
                    Else
                        args.IsValid = False
                        Exit Sub
                    End If
                End If
            End If
        ElseIf CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Mouse Bioassay" Then

            If sSenderRef.Length >= 2 Then
                sSenderCode = Left$(sSenderRef, 2).ToUpper
                sSenderNumber = Right$(sSenderRef, Len(sSenderRef) - 2)
            Else
                args.IsValid = True
                Exit Sub
            End If

            sSenderCode = sSenderCode.ToUpper()
            If sSenderCode = "MC" Then
                Dim revMouseNumber As Regex = New Regex("MC[0-9][0-9][0-9][0-9][0-9][0-9]")
                Dim match As Match

                sSenderRef = sSenderRef.ToUpper()
                match = revMouseNumber.Match(sSenderRef)

                If sSenderRef.Length <> 8 Then
                    args.IsValid = False
                    Exit Sub
                Else
                    If match.Success Then
                        txtSenderRef.Text = sSenderRef
                        args.IsValid = True
                        Exit Sub
                    Else
                        args.IsValid = False
                        Exit Sub
                    End If
                End If
            Else
                args.IsValid = True
            End If
        Else
            '
        End If

    End Sub


#End Region

End Class
