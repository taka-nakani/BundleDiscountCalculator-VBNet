Public Class DiscountRule
    Public Enum DiscountType
        BundleDiscount      ' まとめ値引き（セットマッチ）
        SetDiscount         ' セット値引き（バンドル、Mixマッチ）
        CouponDiscount      ' クーポン値引き
        DmDiscount          ' DM値引き
    End Enum

    Public Property Name As String
    Public Property Type As DiscountType
    ' 実際に適用するアイテムでの条件判定
    Public Property Condition As Func(Of List(Of PurchaseItem), Boolean)
    ' 抽出条件（どのアイテムを対象にするか）
    Public Property ExtractCondition As Func(Of PurchaseItem, Boolean)
    ' 値引後価格
    Public Property CalculatePrice As Func(Of List(Of PurchaseItem), Decimal)
End Class