Public Class clsIDPairs
    Dim iOldID As Integer
    Dim iNewID As Integer
    Dim sValue As String
    Dim sOtherValue As String

    Public Property OldID() As Integer
        Get
            Return iOldID
        End Get
        Set(ByVal iValue As Integer)
            iOldID = iValue
        End Set
    End Property

    Public Property NewID() As Integer
        Get
            Return iNewID
        End Get
        Set(ByVal iValue As Integer)
            iNewID = iValue
        End Set
    End Property

    Public Property Value() As String
        Get
            Return sValue
        End Get
        Set(ByVal Value As String)
            sValue = Value
        End Set
    End Property

    Public Property OtherValue() As String
        Get
            Return sOtherValue
        End Get
        Set(ByVal Value As String)
            sOtherValue = Value
        End Set
    End Property
End Class


