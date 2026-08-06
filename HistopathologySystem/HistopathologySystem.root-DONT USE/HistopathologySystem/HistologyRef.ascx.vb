Imports System.Text.RegularExpressions

Partial  Class HistologyRef
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
    Private HPNumbersAllowed As Boolean

    Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        SetClientValidation()
    End Sub

    Public Sub SetMandatory(ByVal bMandatory As Boolean)
        rfvHistologyRef.Enabled = bMandatory
    End Sub

    Public Function IsMandatory() As Boolean
        Return rfvHistologyRef.Enabled
    End Function

    Public Function IsComplete() As Boolean
        rfvHistologyRef.Validate()
        Return rfvHistologyRef.IsValid
    End Function

    Public Property Text() As String
        Get
            Return txtHistologyRef.Text
        End Get
        Set(ByVal sValue As String)
            txtHistologyRef.Text = sValue
        End Set
    End Property

    Public Property Tooltip() As String
        Get
            Return valHistologyRef.ToolTip
        End Get
        Set(ByVal sValue As String)
            valHistologyRef.ToolTip = sValue
        End Set
    End Property

    Public Property AllowHPNumbers() As Boolean
        Get
            Return HPNumbersAllowed
        End Get
        Set(ByVal bValue As Boolean)
            HPNumbersAllowed = bValue
            If bValue Then
                Tooltip = "Format: NN/NNNNN (Year part must not be greater than current year) or HPNNNN/NN."
            End If
        End Set
    End Property

    Public Function GetTextBoxClientID() As String
        Return txtHistologyRef.ClientID
    End Function

    Public Function SetEnterKeyPress(ByVal ctlButton As Button)
        Common.SetTextboxDefaultButton(txtHistologyRef, ctlButton)
    End Function

    Public Function SetFocus()
        Common.SetFocus(txtHistologyRef)
    End Function

    Public Sub SetEnabled(ByVal bEnabled As Boolean)

        txtHistologyRef.Enabled = bEnabled

    End Sub

    Public Function IsEnabled() As Boolean
        Return txtHistologyRef.Enabled
    End Function

    Public Sub SetValidate(ByVal bValidate As Boolean)
        valHistologyRef.Enabled = bValidate
    End Sub

    Public Function IsValid() As Boolean
        valHistologyRef.Validate()
        Return valHistologyRef.IsValid
    End Function

#Region "Validation"

     Private Function SetClientValidation() As Boolean
        If HttpContext.Current.Request.Browser.JavaScript Then
            Dim scr As New System.Text.StringBuilder

            scr.Append("<SCRIPT language=""Javascript"">" + vbNewLine)
            scr.Append("function ClientValidateHistologyRef(sender, args)" + vbNewLine)
            scr.Append("{" + vbNewLine)
            scr.Append("    var sHistologyRef = args.Value;" + vbNewLine)
            scr.Append("    var iIndexof = sHistologyRef.indexOf(""-"");" + vbNewLine)
            scr.Append("    if (iIndexof != -1)" + vbNewLine)
            scr.Append("    { " + vbNewLine)
            scr.Append("        args.IsValid = false;" + vbNewLine)
            scr.Append("        return;" + vbNewLine)
            scr.Append("    }" + vbNewLine)
            scr.Append("    else" + vbNewLine)
            scr.Append("    {" + vbNewLine)

            If AllowHPNumbers Then
                scr.Append("    var expHPRef = /[H][P][0-9][0-9][0-9][0-9][/][0-9][0-9]/;" + vbNewLine)
                scr.Append("    if (expHPRef.test(sHistologyRef))" + vbNewLine)
                scr.Append("    {" + vbNewLine)
                scr.Append("        args.IsValid = true;" + vbNewLine)
                scr.Append("        return;" + vbNewLine)
                scr.Append("    }" + vbNewLine)
            End If

            scr.Append("        if (sHistologyRef.length !=8)" + vbNewLine)
            scr.Append("        {" + vbNewLine)
            scr.Append("            args.IsValid = false;" + vbNewLine)
            scr.Append("            return;" + vbNewLine)
            scr.Append("        }" + vbNewLine)
            scr.Append("        else" + vbNewLine)
            scr.Append("        {" + vbNewLine)
            scr.Append("            var expHistologyRef = /[0-9][0-9][/][0-9][0-9][0-9][0-9][0-9]/;" + vbNewLine)
            scr.Append("            if (expHistologyRef.test(sHistologyRef))" + vbNewLine)
            scr.Append("            {" + vbNewLine)
            scr.Append("                var d = new Date();" + vbNewLine)
            scr.Append("                var sYear = sHistologyRef.substring(0,2);" + vbNewLine)
            scr.Append("                var currentDate = d.getFullYear().toString();" + vbNewLine)
            scr.Append("                var sYearPart = currentDate.substring(2, 4);" + vbNewLine)
            scr.Append("                if (sYear > sYearPart && sYear < 70)" + vbNewLine)
            scr.Append("                {" + vbNewLine)
            scr.Append("                    args.IsValid = false;" + vbNewLine)
            scr.Append("                    return;" + vbNewLine)
            scr.Append("                }" + vbNewLine)
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
            scr.Append("}" + vbNewLine)
            scr.Append("</SCRIPT>" + vbNewLine)

            Me.Page.RegisterClientScriptBlock("SetHistologyClientValidation", scr.ToString())
            Return True
        Else
            Return False
        End If
    End Function

    Public Sub ValidateHistologyRef(ByVal sender As Object, ByVal args As ServerValidateEventArgs)
        Dim sHistologyRef As String = CStr(args.Value)
        Dim sHistoYear As String
        Dim dDate As Date
        Dim revHistologyRef As Regex = New Regex("[0-9][0-9](/)[0-9][0-9][0-9][0-9][0-9]")
        Dim revHPRef As Regex = New Regex("[H][P][0-9][0-9][0-9][0-9][/][0-9][0-9]")
        Dim match As Match

        If AllowHPNumbers Then
            match = revHPRef.Match(sHistologyRef)

            If match.Success Then
                args.IsValid = True
                Exit Sub
            End If
        End If

        If sHistologyRef.IndexOf("-") <> -1 Then
            args.IsValid = False
            Exit Sub
        Else
            If sHistologyRef.Length <> 8 Then
                args.IsValid = False
                Exit Sub
            Else
                match = revHistologyRef.Match(sHistologyRef)

                If match.Success Then
                    sHistoYear = Left$(sHistologyRef, 2)
                    Dim sYear As String = Right$(dDate.Now.Year(), 2)

                    If Convert.ToInt32(sHistoYear) > Convert.ToInt32(sYear) And Convert.ToInt32(sHistoYear) < 70 Then
                        args.IsValid = False
                    Else
                        args.IsValid = True
                    End If
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

