Partial  Class Batch
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
        If Not IsPostBack Then

        End If
    End Sub

#Region "Batch Details"

    Public Sub DisplayDetails()
        Try
            'Make sure the lookup lists have been loaded 
            LoadLookupLists()

            Dim objLookup As New HistopathologyLib.LookupData()
            Dim iBatchID As Integer = CType(Session.Item(SessionVars.SV_BatchID), Integer)
            Dim dsData As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim objUser As New HistopathologyLib.clsUser()
            Dim sUserArea As String = ""
            Dim sUserName As String = ""
            Dim sUserGroup As String = ""
            Dim sUserAreaID As String = ""

            If Not dsData Is Nothing Then
                Dim dtBatchData As DataTable = dsData.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)
                If Not dtBatchData Is Nothing And dtBatchData.Rows.Count <> 0 Then
                    With dtBatchData.Rows(0)
                        lblSpeciesVal.Text = objLookup.GetSpeciesDescription(.Item("Species").ToString())
                        Dim sString As String = .Item("ProjectContractCode").ToString()
                        lblProjectCodeVal.Text = GetListTypeID(.Item("ProjectContractCode").ToString(), LOOKUP_PROJECTS)
                        lblContactNameVal.Text = GetListTypeID(.Item("ContactName").ToString(), LOOKUP_CONTACTS)
                        lblSubmissionDateVal.Text = .Item("BatchDate").ToString()
                        If Not objUser.GetUserByID(Convert.ToInt32(.Item("SubmittedBy")), sUserName, sUserArea, sUserGroup, sUserAreaID) Then
                            Throw New Exception("User.GetUserByID returned false.")
                        Else
                            lblEnteredByVal.Text = sUserName
                            lblEnteredAreaVal.Text = sUserArea
                        End If

                        If Not objUser.GetUserByID(.Item("OtherSubmittedBy"), sUserName, sUserArea, sUserGroup, sUserAreaID) Then
                            Throw New Exception("User.GetUserByID returned false.")
                        Else
                            lblSubmittedByVal.Text = sUserName
                        End If

                        lblSubmittedAreaVal.Text = GetListType(.Item("OtherSubmittedArea").ToString(), LOOKUP_USER_AREA)
                    End With
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to display the Batch Details.", ex)
        End Try
    End Sub

#End Region

#Region "Lookup List Population"

    Private Sub LoadLookupLists()
        Dim objDataTable As DataTable
        Dim objLookup As New HistopathologyLib.LookupData()

        Try

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve Batch control drop down lists.", ex)
        End Try

    End Sub

#End Region

End Class
