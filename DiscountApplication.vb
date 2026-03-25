Public Class DiscountApplication
    Public Property ItemIndices As List(Of Integer)
    Public Property RuleIndex As Integer
    Public Property RuleName As String
    Public Property DiscountType As DiscountRule.DiscountType
    Public Property OriginalPrice As Decimal
    Public Property DiscountedPrice As Decimal
    Public Property DiscountAmount As Decimal
End Class
