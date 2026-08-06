
Imports System.Data.SqlClient

Partial Class EditHistologyRef
    Inherits System.Web.UI.Page


#Region " Web Form Designer Generated Code "

    'This call is required by the Web Form Designer.
    <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()

    End Sub

    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents cmdCone As System.Web.UI.WebControls.Button
    Protected WithEvents rfvSampleSenderReference As System.Web.UI.WebControls.RequiredFieldValidator
    Protected WithEvents txtNewSenderRef As SenderRef
    Protected WithEvents txtOriginalSenderRef As SenderRef
    Protected WithEvents txtNewHistologyRef As HistologyRef

    'NOTE: The following placeholder declaration is required by the Web Form Designer.
    'Do not delete or move it.
    Private designerPlaceholderDeclaration As System.Object

    Private Sub Page_Init(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Init
        'CODEGEN: This method call is required by the Web Form Designer
        'Do not modify it using the code editor.
        InitializeComponent()
    End Sub

#End Region

    Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        VLAHeader1.PageTitle = "Edit Sender/Histology Ref"
        CheckPermissions()
        lblMessage.Visible = False
        VLAHeader1.SubmissioNoVisible() = False
        txtOriginalSenderRef.SetFocus()
        lblMessage.ForeColor = System.Drawing.Color.Red
        txtNewHistologyRef.AllowHPNumbers = True

        If Not IsPostBack Then
            txtNewHistologyRef.SetMandatory(False)
            cmdSaveHistologyRef.Attributes.Add("OnClick", "javascript:if(document.getElementById('txtNewHistologyRef_txtHistologyRef').value == '' && document.getElementById('txtOriginalSenderRef_txtSenderRef').value != '' ){return confirm('You have not entered a Histology ref. This will delete any existing Histology Ref. Do you wish to continue?')}else {return true;}")
        End If
    End Sub

    Private Sub CheckPermissions()
        VLAHeader1.getUserDetails()
        Dim sGroupName As String = Session(SessionVars.SV_HeaderGroupName)
        If sGroupName = "Customer" Then
            Response.Redirect("Home.aspx")
        ElseIf sGroupName = "Histopathology User" Then
            Response.Redirect("Home.aspx")
        ElseIf sGroupName = "Maintenance" Then
            'Nothing
        Else
            Response.Redirect("Home.aspx")
        End If
    End Sub

    Private Sub cmdEditSenderRef_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdEditSenderRef.Click
        Dim objAnimal As New HistopathologyLib.clsAnimal
        Dim bSuccess As Boolean

        txtOriginalSenderRef.Text = txtOriginalSenderRef.Text.Trim()
        txtNewSenderRef.Text = txtNewSenderRef.Text.Trim()

        If txtOriginalSenderRef.Text = "" Then
            lblMessage.Text = " You must enter a Sample Ref."
        ElseIf Not txtOriginalSenderRef.IsValid() Then
            lblMessage.Text = " You must enter a valid Sample Ref."
        ElseIf txtNewSenderRef.Text = "" Then
            lblMessage.Text = " You must enter a New Sample Ref."
        ElseIf Not txtNewSenderRef.IsValid() Then
            lblMessage.Text = " You must enter a valid New Sample Ref."
        Else
            Try
                bSuccess = objAnimal.UpdateAnimalSenderRef(txtOriginalSenderRef.Text, txtNewSenderRef.Text, CType(Session(SessionVars.SV_HeaderUserID), Integer))
                If bSuccess Then
                    lblMessage.ForeColor = System.Drawing.Color.Green
                    lblMessage.Text = "The new Sample Ref has been saved"
                End If
            Catch objAUEx As HistopathologyLib.AnimalUpdateException
                lblMessage.Text = "The Sample Ref was not updated because: " & objAUEx.Message
            Catch objEx As Exception
                lblMessage.Text = "ERROR: " & objEx.Message
            End Try
        End If
        lblMessage.Visible = True
    End Sub

    Private Sub cmdSaveHistologyRef_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdSaveHistologyRef.Click


        Dim objHistology As New HistopathologyLib.clsHistology
        Dim dtData As New DataTable
        Dim bValidateHistologyRef As Boolean = True
        Dim objAnimal As New HistopathologyLib.clsAnimal
        Dim bSuccess As Boolean
        Dim bValidate As Boolean = True

        If ConfigurationSettings.AppSettings("EditHistologyRefValidation").ToString() <> "True" Then
            bValidate = False
        End If

        txtOriginalSenderRef.Text = txtOriginalSenderRef.Text.Trim()
        txtNewHistologyRef.Text = txtNewHistologyRef.Text.Trim()

        Dim sHistRef = getHistologyRef(txtOriginalSenderRef.Text.Trim())

        If Not objHistology.GetHistologyRefsTable(dtData) Then
            Throw New Exception("Histology.GetHistologyRefsTable returned false.")
        End If

        'Check if the HistologyRef is a pre 01 ref. We don't need to validate these.
        If txtOriginalSenderRef.CheckPGNumber(False) Then
            Dim sYear As String
            Dim sID As String
            Dim sSenderRef As String
            sSenderRef = txtOriginalSenderRef.Text().Substring(2)

            sID = Left$(sSenderRef, 4)
            sYear = Right$(sSenderRef, 2)

            'If year is less than or equal to one dont limit the format of the histo ref
            If IsPreEqual01(sYear) Then
                bValidateHistologyRef = False
            End If
        End If

        If txtOriginalSenderRef.Text = "" Then
            lblMessage.Text = " You must enter a Sample Ref."
            lblMessage.Visible = True
            Exit Sub
        ElseIf Not txtOriginalSenderRef.IsValid() Then
            lblMessage.Text = " You must enter a valid Sample Ref."
            lblMessage.Visible = True
            Exit Sub
        ElseIf txtOriginalSenderRef.IsPGNumber Then

            'If the SenderRef is a PG number, Check that the histology ref is of correct format.
            Session.Item(SessionVars.SV_HeaderUserArea) = "Neuropath"

            If bValidate Then
                If bValidateHistologyRef = True Then
                    If txtOriginalSenderRef.CheckPGNumber(True) Then
                        ' The histology ref format is valid so need to check that it is a reverse of the PG.
                        Dim sCorrectHistologyRef As String = ""
                        sCorrectHistologyRef = Right(txtOriginalSenderRef.Text, 2) & "/0" & txtOriginalSenderRef.Text.Substring(2, 4)
                        If txtNewHistologyRef.Text <> sCorrectHistologyRef Then
                            lblMessage.Text = " The Histology Ref is not correct for the PG Number entered."
                            lblMessage.Visible = True
                            Exit Sub
                        End If
                    End If
                End If
            End If

        ElseIf txtNewHistologyRef.Text <> "" And Not txtNewHistologyRef.IsValid Then
            If bValidate Then
                lblMessage.Text = " You must enter a valid Histology Ref."
                lblMessage.Visible = True
                Exit Sub
            End If
        Else

            'Check that the number entered is not higher than the next available
            Dim iHistoNumber As Integer = 0
            Dim iHistoType As Integer = 0
            Dim dDate As Date
            Dim foundRows As DataRow()


            If txtNewHistologyRef.Text <> "" Then
                If txtNewHistologyRef.Text.IndexOf("HP") = -1 Then


                    If Not IsPreviousYearHistoRef(txtNewHistologyRef.Text) Then
                        iHistoNumber = Integer.Parse(Right$(txtNewHistologyRef.Text, 5))
                        iHistoType = CheckRange(iHistoNumber)

                        foundRows = dtData.Select("Type=" & iHistoType)
                        If iHistoNumber >= CInt(foundRows(0)("NextHistologyRef")) Then
                            lblMessage.Text = " The Histology Ref entered is higher than or equal to the the current next Histology Ref (" & Right$(dDate.Now.Year.ToString, 2) & "/" & foundRows(0)("NextHistologyRef").ToString & ") for the selected area."
                            lblMessage.Visible = True
                            Exit Sub
                        End If
                    End If
                End If
            End If
        End If

        lblMessage.Text = ""

        Try
            bSuccess = objAnimal.UpdateAnimalHistologyRef(txtOriginalSenderRef.Text, txtNewHistologyRef.Text, CType(Session(SessionVars.SV_HeaderUserID), Integer))

            If bSuccess Then
                lblMessage.ForeColor = System.Drawing.Color.Green
                If txtNewHistologyRef.Text <> "" Then
                    lblMessage.Text = "The new Histology Ref has been saved"
                Else
                    lblMessage.Text = "The old Histology Ref has been removed. You may now enter a new Histology Ref."
                End If
            End If

        Catch objAUEx As HistopathologyLib.AnimalUpdateException
            lblMessage.Text = "The Histology Reference was not updated because: " & objAUEx.Message
        Catch objEx As Exception
            lblMessage.Text = "ERROR: " & objEx.Message
        End Try

        lblMessage.Visible = True

    End Sub

    Private Function getHistologyRef(ByVal sSenderRef As String) As String
        Dim dtAnimalData As DataTable = New DataTable
        Dim bSuccess As Boolean
        Dim objAnimal As New HistopathologyLib.clsAnimal

        bSuccess = objAnimal.GetAnimalBySender(sSenderRef, dtAnimalData)

        If (bSuccess) Then
            Dim sHistoRef As String
            If dtAnimalData.Rows().Count = 0 Then
                lblMessage.Text = "Sample Ref. not found."
                lblMessage.Visible = True
                Return ""
            ElseIf IsDBNull(dtAnimalData.Rows(0)("HistologyRef")) Then
                Return "<null>"
            Else
                Return dtAnimalData.Rows(0)("HistologyRef")
            End If
        Else
            lblMessage.Text = "Histology Ref. lookup failed."
            lblMessage.Visible = True
            Return ""
        End If

    End Function

    Private Sub cmdDone_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdCone.Click, cmdDone.Click
        Response.Redirect("Home.aspx")
    End Sub

    Private Sub cmdGetOldHistologyRef_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdGetOldHistologyRef.Click

        If txtOriginalSenderRef.Text.Trim() <> "" Then
            txtOldHistologyRef.Text = getHistologyRef(txtOriginalSenderRef.Text.Trim())
        Else
            lblMessage.Text = " You must enter a Sample Ref."
            lblMessage.Visible = True
        End If

    End Sub
End Class
