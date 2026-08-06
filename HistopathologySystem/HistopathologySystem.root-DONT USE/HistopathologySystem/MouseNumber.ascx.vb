Imports System.Text.RegularExpressions

Partial  Class MouseNumber
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
        SetClientValidation()
    End Sub

    Public Sub DisplayError(ByVal bVisible As Boolean)

        lblError.Visible = bVisible

    End Sub

    Public Function SetEnterKeyPress(ByVal ctlButton As Button)
        Common.SetTextboxDefaultButton(txtMouseNumber, ctlButton)
    End Function

    Public Function SetFocus()
        Common.SetFocus(txtMouseNumber)
    End Function

    Public Function SetTextboxOnEnter(ByVal sControlClientID As String)
        Common.SetTextboxControlOnEnter(txtMouseNumber, sControlClientID)
    End Function

    Public Function SetTextboxOnEnter(ByVal ctlMouseControl As MouseNumber)
        Common.SetTextboxControlOnEnter(txtMouseNumber, ctlMouseControl.GetTextBoxClientID())
    End Function

    Public Function GetTextBoxClientID() As String
        Return txtMouseNumber.ClientID
    End Function

    Public Sub SetErrorToolTip(ByVal strValue As String)

        lblError.ToolTip = strValue

    End Sub

    Public Function IsValid() As Boolean
        valMouseNumber.Validate()
        Return valMouseNumber.IsValid
    End Function

    Public Function CheckMouseNumber() As Boolean
        If txtMouseNumber.Text = "" Then
            SetErrorToolTip("Required Field")
            lblError.Visible = True
            Return False
        Else
            lblError.Visible = False
            Return IsValid()
        End If
    End Function

    Public Sub SetMandatory(ByVal bMandatory As Boolean)

        rfvMouseNumber.Enabled = bMandatory

    End Sub

    Public Property Text() As String
        Get
            Return txtMouseNumber.Text
        End Get
        Set(ByVal sValue As String)
            txtMouseNumber.Text = sValue
        End Set
    End Property

    Public Sub SetEnabled(ByVal bEnabled As Boolean)

        txtMouseNumber.Enabled = bEnabled

    End Sub

    Public Sub SetREVTooltip(ByVal strValue As String)

        valMouseNumber.ToolTip = strValue

    End Sub

#Region "Validation"

    Private Function SetClientValidation() As Boolean
        If HttpContext.Current.Request.Browser.JavaScript Then
            Dim scr As New System.Text.StringBuilder()

            scr.Append("<SCRIPT language=""Javascript"">" + vbNewLine)
            scr.Append("function ClientValidateMouseNumber(sender, args)" + vbNewLine)
            scr.Append("{" + vbNewLine)
            scr.Append("    var sMouseNumber = args.Value;" + vbNewLine)
            scr.Append("    var expMouseNumber = /MC[0-9][0-9][0-9][0-9][0-9][0-9]/;" + vbNewLine)
            scr.Append("    sMouseNumber = sMouseNumber.toUpperCase();" + vbNewLine)
            scr.Append("    if (expMouseNumber.test(sMouseNumber))" + vbNewLine)
            scr.Append("    {" + vbNewLine)
            scr.Append("        args.IsValid = true;" + vbNewLine)
            scr.Append("        return;" + vbNewLine)
            scr.Append("    }" + vbNewLine)
            scr.Append("    else" + vbNewLine)
            scr.Append("    {" + vbNewLine)
            scr.Append("        args.IsValid = false;" + vbNewLine)
            scr.Append("        return;" + vbNewLine)
            scr.Append("    }" + vbNewLine)
            scr.Append("}" + vbNewLine)
            scr.Append("</SCRIPT>" + vbNewLine)

            Me.Page.RegisterClientScriptBlock("SetMouseClientValidation", scr.ToString())
            Return True
        Else
            Return False
        End If
    End Function

    Public Sub ValidateMouseNumber(ByVal sender As Object, ByVal args As ServerValidateEventArgs)
        Dim sMouseNumber As String = CStr(args.Value)
        Dim revMouseNumber As Regex = New Regex("MC[0-9][0-9][0-9][0-9][0-9][0-9]")
        Dim match As Match

        sMouseNumber = sMouseNumber.ToUpper()
        match = revMouseNumber.Match(sMouseNumber)

        txtMouseNumber.Text = sMouseNumber
        If match.Success Then
            args.IsValid = True
            Exit Sub
        Else
            args.IsValid = False
            Exit Sub
        End If
    End Sub

#End Region
End Class
