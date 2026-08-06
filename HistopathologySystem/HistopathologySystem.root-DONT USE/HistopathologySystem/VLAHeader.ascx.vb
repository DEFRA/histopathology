'C****************************************************************************
'  Class:    VLAHeader
'
'  Summary:  Header User Control
'
'****************************************************************************C

'****************************************************************************
'*      Imports
'****************************************************************************

Public Class HomeLinkEventArgs
    Inherits System.EventArgs

    Public bNavigateHome As Boolean = True

End Class

Partial  Class VLAHeader
    Inherits System.Web.UI.UserControl

    Private Const USER_CAPTION As String = "User: "
    Private Const LAB_CAPTION As String = "Group: "
    Private Const BATCH_CAPTION As String = "Batch No: "
    Private Const AREA_CAPTION As String = "Area: "
    Private Const SUBMISSION_CAPTION As String = "Submission No: "

    '****************************************************************************
    '*      Private class memebers
    '****************************************************************************

    '****************************************************************************
    '*      Protected class memebers
    '****************************************************************************

    '****************************************************************************
    '*      Public class memebers
    '****************************************************************************

    Public Event HomeClick(ByVal sender As System.Object, ByVal e As HomeLinkEventArgs)

    Public Property PageTitle() As String
        Get
            Return lblPageTitle.Text
        End Get
        Set(ByVal Value As String)
            lblPageTitle.Text = Value
        End Set
    End Property

    Public Property SubmissioNoVisible() As Boolean
        Get
            Return lblSubmission.Visible
        End Get
        Set(ByVal Value As Boolean)
            lblSubmission.Visible = Value
        End Set
    End Property
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
        If Not IsPostBack Then
            Dim bValidUser As Boolean = False
            Try
                bValidUser = getUserDetails()
                lblUser.Text = USER_CAPTION & Session(SessionVars.SV_HeaderUserName)
                lblLab.Text = LAB_CAPTION & Session(SessionVars.SV_HeaderGroupName)
                lblArea.Text = AREA_CAPTION & Session(SessionVars.SV_HeaderUserArea)

                Dim sVersion As String = System.Configuration.ConfigurationSettings.AppSettings("SystemVersion")
                If sVersion <> "" Then
                    sVersion = sVersion & " (" & System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() & ")"
                    lblVersion.Text = sVersion
                End If

                Dim iBatchID As Integer = CType(Session.Item(SessionVars.SV_BatchID), Integer)

                If iBatchID > 0 Then
                    lblSubmission.Text = SUBMISSION_CAPTION & CStr(Session.Item(SessionVars.SV_BatchID))
                    lblSubmission.CssClass = "topnavtextBigger"
                Else
                    lblSubmission.Text = SUBMISSION_CAPTION & "Not saved"
                    lblSubmission.CssClass = "topnavtext"
                End If

                Dim objArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
                If Not objArrayList Is Nothing Then
                    Dim iCount As Integer = 0
                    Dim sBreadCrumb As String
                    For iCount = 0 To objArrayList.Count - 1
                        If Not iCount = objArrayList.Count - 1 Then
                            sBreadCrumb = sBreadCrumb & CStr(objArrayList.Item(iCount)) & "\"
                        Else
                            sBreadCrumb = sBreadCrumb & CStr(objArrayList.Item(iCount))
                        End If
                    Next
                    lblBreadCrumb.Text = sBreadCrumb
                End If

            Catch ex As Exception
                clsAppError.DisplayError("Failed to set retrieve user details.", ex)
            End Try

            If Not bValidUser Then
                Response.Redirect("unauthorized.htm")
            End If

            Dim sPageName As String
            Try
                ' set the help link to point to the section in Help.aspx with
                ' the same name as this page (if a section hasn't already
                ' been set)
                If lnkHelp.NavigateUrl.IndexOf("#") < 0 Then
                    sPageName = Request.CurrentExecutionFilePath()
                    Dim sPathParts() As String = sPageName.Split(New Char() {"/", "\"})
                    Dim sNameParts() As String = sPathParts(sPathParts.GetUpperBound(0)).Split(".")

                    Dim sUserGroup As String = CStr(Session.Item(SessionVars.SV_HeaderGroupName))
                    If sUserGroup = "Customer" Then
                        lnkHelp.NavigateUrl = "HistoHelp_CustomerGroup.htm#" & sNameParts(0)
                    Else
                        lnkHelp.NavigateUrl = "HistoHelp_HistoGroup.htm#" & sNameParts(0)
                    End If

                End If
            Catch ex As Exception
                clsAppError.DisplayError("Failed to set Help chapter for page " & sPageName, ex)
            End Try
        End If
    End Sub

    Public Function GetUserName() As String
        Return lblUser.Text.Substring(USER_CAPTION.Length)
    End Function

    Public Function getUserDetails() As Boolean

        Dim sName As String = Session(SessionVars.SV_HeaderUserName)
        Dim sGroupName As String = Session(SessionVars.SV_HeaderGroupName)
        Dim sAreaName As String = Session(SessionVars.SV_HeaderUserArea)

        If sName Is Nothing OrElse sName = "" OrElse sGroupName Is Nothing OrElse sGroupName = "" Then
            Dim sNTLogin As String = GetLoggedOnUser()
            Dim iUserID As Integer
            Dim sEmail As String
            Dim objUser As New HistopathologyLib.clsUser
            Dim sGroupCode As Long
            Dim iUserArea As Integer
            Dim bActive As Boolean
            If Not objUser.GetUserByNTLogin(sNTLogin, iUserID, sName, sGroupCode, sGroupName, sEmail, iUserArea, sAreaName, bActive) Then
                Return False
            End If

            Session(SessionVars.SV_HeaderUserName) = sName
            Session(SessionVars.SV_HeaderGroupID) = sGroupCode
            Session(SessionVars.SV_HeaderGroupName) = sGroupName
            Session(SessionVars.SV_HeaderUserID) = iUserID
            Session(SessionVars.SV_HeaderUserEmail) = sEmail
            Session(SessionVars.SV_HeaderUserArea) = sAreaName
            Session(SessionVars.SV_HeaderUserAreaID) = iUserArea
        End If
        Return True

    End Function


    Private Sub lnkHome_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lnkHome.Click
        Page.SmartNavigation = False
        Dim arg As New HomeLinkEventArgs
        RaiseEvent HomeClick(sender, arg)
        If arg.bNavigateHome Then
            RemoveSessionVars(Session)
            Response.Redirect("home.aspx")
        End If

    End Sub

    'Private Sub lbHelp_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lbHelp.Click
    '    Dim sUserGroup As String = CStr(Session.Item(SessionVars.SV_HeaderGroupName))
    '    If sUserGroup = "Customer" Then
    '        OpenDownloadPopup("helpCustomer.htm", Me.Page)
    '    Else
    '        OpenDownloadPopup("help.htm", Me.Page)
    '    End If
    'End Sub

    Protected WithEvents lbHelp As System.Web.UI.WebControls.LinkButton
End Class
