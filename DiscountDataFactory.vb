
Public Class DiscountRuleMaster
    Public Property RuleId As String
    Public Property RuleName As String
    Public Property DiscountType As String      ' "BundleDiscount", "SetDiscount", "CouponDiscount", "DmDiscount"
    Public Property TargetCategories As String  ' カンマ区切り（例："ゲームソフト,オンライン用カード"）
    Public Property TargetNames As String       ' カンマ区切り（例："スーパーマリオ,マリオカート"）
    Public Property MinCount As Integer         ' 最小個数
    Public Property MaxCount As Integer         ' 最大個数（-1で無制限）
    Public Property FixedPrice As Decimal       ' 固定価格（BundleDiscountの場合）
    Public Property DiscountAmount As Decimal   ' 割引額（SetDiscount/CouponDiscount/DmDiscountの場合）
    Public Property DiscountRate As Decimal  ' 割引率（例：10.5 = 10.5%）
End Class

' ファクトリークラス
Public Class DiscountDataFactory

    ' DiscountRule を CSV から作成
    Public Shared Function LoadDiscountRulesFromCSV(csvFilePath As String) As List(Of DiscountRule)
        Dim rules As New List(Of DiscountRule)

        Try
            Using reader As New System.IO.StreamReader(csvFilePath)
                Dim headerLine = reader.ReadLine()  ' ヘッダーをスキップ

                Dim line As String
                While (InlineAssignHelper(line, reader.ReadLine())) IsNot Nothing
                    Dim parts = line.Split(","c)
                    If parts.Length >= 7 Then
                        Dim master As New DiscountRuleMaster With {
                            .RuleId = parts(0).Trim(),
                            .RuleName = parts(1).Trim(),
                            .DiscountType = parts(2).Trim(),
                            .TargetCategories = parts(3).Trim(),
                            .TargetNames = parts(4).Trim(),
                            .MinCount = Integer.Parse(parts(5).Trim()),
                            .MaxCount = Integer.Parse(parts(6).Trim()),
                            .FixedPrice = If(parts.Length > 7 AndAlso Decimal.TryParse(parts(7).Trim(), New Decimal), Decimal.Parse(parts(7).Trim()), 0),
                            .DiscountAmount = If(parts.Length > 8 AndAlso Decimal.TryParse(parts(8).Trim(), New Decimal), Decimal.Parse(parts(8).Trim()), 0),
                            .DiscountRate = If(parts.Length > 9 AndAlso Decimal.TryParse(parts(9).Trim(), New Decimal), Decimal.Parse(parts(9).Trim()), 0)
                        }

                        Dim rule = CreateDiscountRule(master)
                        If rule IsNot Nothing Then
                            rules.Add(rule)
                        End If
                    End If
                End While
            End Using
        Catch ex As Exception
            Throw New Exception($"DiscountRule CSV 読み込みエラー: {ex.Message}")
        End Try

        Return rules
    End Function

    ' DiscountRule を List(Of Dictionary) から作成
    Public Shared Function LoadDiscountRulesFromList(dataList As List(Of Dictionary(Of String, Object))) As List(Of DiscountRule)
        Dim rules As New List(Of DiscountRule)

        For Each _data In dataList
            Dim master As New DiscountRuleMaster With {
                .RuleId = If(_data.ContainsKey("RuleId"), CStr(_data("RuleId")), ""),
                .RuleName = If(_data.ContainsKey("RuleName"), CStr(_data("RuleName")), ""),
                .DiscountType = If(_data.ContainsKey("DiscountType"), CStr(_data("DiscountType")), ""),
                .TargetCategories = If(_data.ContainsKey("TargetCategories"), CStr(_data("TargetCategories")), ""),
                .TargetNames = If(_data.ContainsKey("TargetNames"), CStr(_data("TargetNames")), ""),
                .MinCount = If(_data.ContainsKey("MinCount"), CInt(_data("MinCount")), 0),
                .MaxCount = If(_data.ContainsKey("MaxCount"), CInt(_data("MaxCount")), -1),
                .FixedPrice = If(_data.ContainsKey("FixedPrice"), CDec(_data("FixedPrice")), 0),
                .DiscountAmount = If(_data.ContainsKey("DiscountAmount"), CDec(_data("DiscountAmount")), 0),
                .DiscountRate = If(_data.ContainsKey("DiscountRate"), CDec(_data("DiscountRate")), 0)
            }

            Dim rule = CreateDiscountRule(master)
            If rule IsNot Nothing Then
                rules.Add(rule)
            End If
        Next

        Return rules
    End Function

    ' DiscountRuleMaster から DiscountRule を生成
    Private Shared Function CreateDiscountRule(master As DiscountRuleMaster) As DiscountRule
        Dim targetCategories = master.TargetCategories.Split(","c).Select(Function(s) s.Trim()).ToList()
        Dim targetNames = master.TargetNames.Split(","c).Select(Function(s) s.Trim()).Where(Function(s) s <> "").ToList()

        ' ★ 追加：割引額と割引率のどちらを使用するかを判定
        Dim useDiscountRate = master.DiscountRate > 0

        Select Case master.DiscountType
            Case "BundleDiscount"
                Return New DiscountRule With {
                    .Name = master.RuleName,
                    .Type = DiscountRule.DiscountType.BundleDiscount,
                    .Condition = Function(items) _
                        items.Count >= master.MinCount AndAlso
                        (master.MaxCount = -1 OrElse items.Count <= master.MaxCount) AndAlso
                        items.All(Function(p) targetCategories.Contains(p.Category) OrElse
                            (targetNames.Count > 0 AndAlso targetNames.Contains(p.Name))),
                    .ExtractCondition = Function(item) _
                        targetCategories.Contains(item.Category) OrElse
                        (targetNames.Count > 0 AndAlso targetNames.Contains(item.Name)),
                    .CalculatePrice = Function(items) _
                        If(master.FixedPrice > 0,
                            master.FixedPrice,
                            Math.Max(0, items.Sum(Function(p) p.Price) * (1 - master.DiscountRate / 100)))
                }

            Case "SetDiscount"
                Return New DiscountRule With {
                    .Name = master.RuleName,
                    .Type = DiscountRule.DiscountType.SetDiscount,
                    .Condition = Function(items) _
                        items.Count >= master.MinCount AndAlso
                        (master.MaxCount = -1 OrElse items.Count <= master.MaxCount) AndAlso
                        items.All(Function(p) targetCategories.Contains(p.Category)),
                    .ExtractCondition = Function(item) targetCategories.Contains(item.Category),
                    .CalculatePrice = Function(items) _
                        If(useDiscountRate,
                            items.Sum(Function(p) Math.Max(0, p.Price * (1 - master.DiscountRate / 100))),
                            items.Sum(Function(p) Math.Max(0, p.Price - master.DiscountAmount)))
                }

            Case "CouponDiscount"
                Return New DiscountRule With {
                    .Name = master.RuleName,
                    .Type = DiscountRule.DiscountType.CouponDiscount,
                    .Condition = Function(items) _
                        items.Count = 1 AndAlso
                        (targetCategories.Contains(items(0).Category) OrElse
                            (targetNames.Count > 0 AndAlso targetNames.Contains(items(0).Name))),
                    .ExtractCondition = Function(item) _
                        targetCategories.Contains(item.Category) OrElse
                        (targetNames.Count > 0 AndAlso targetNames.Contains(item.Name)),
                    .CalculatePrice = Function(items) _
                        If(useDiscountRate,
                            Math.Max(0, items(0).Price * (1 - master.DiscountRate / 100)),
                            Math.Max(0, items(0).Price - master.DiscountAmount))
                }

            Case Else
                Return Nothing
        End Select
    End Function

    ' ヘルパーメソッド（VB.NETの inline assignment用）
    Private Shared Function InlineAssignHelper(Of T)(ByRef target As T, value As T) As T
        target = value
        Return value
    End Function
End Class