Public Class BundleDiscountCalculator
    Private _items As List(Of PurchaseItem)
    Private _discounts As List(Of DiscountRule)

    Public Sub New(items As List(Of PurchaseItem), discounts As List(Of DiscountRule))
        _items = items
        _discounts = discounts
    End Sub

    Public Function FindMinimumPrice() As (minPrice As Decimal, minIdx As Integer, optimalCombination As List(Of DiscountApplication))
        Dim minPrice As Decimal = Decimal.MaxValue
        Dim minIdx As Integer = 0
        Dim optimalCombination As List(Of DiscountApplication) = Nothing

        Dim allIndices As List(Of Integer) = Enumerable.Range(0, _items.Count).ToList()
        Dim allCombinations = New List(Of List(Of DiscountApplication))

        ' バックトラックで全パターンを生成
        GenerateAllCombinations(
            New List(Of DiscountApplication),
            allIndices,
            New HashSet(Of Integer),
            New HashSet(Of Integer),
            0,
            allCombinations)

        ' ★ デバッグ：すべてのパターンを表示
        Console.WriteLine($"生成されたパターン数: {allCombinations.Count}")
        Console.WriteLine("")

        ' 各パターンの合計金額を計算
        Dim patternIdx = 0
        For Each combination In allCombinations
            Dim price = CalculateTotalPrice(combination)

            ' ★ デバッグ：各パターンを表示
            If combination.Count > 0 OrElse price < 100000 Then  ' 値引きがあるパターンのみ表示
                'Console.WriteLine($"パターン {patternIdx}: 金額 {price:N0}円")
                'For Each app In combination
                '    Dim itemNames = String.Join(", ", app.ItemIndices.Select(Function(i) _items(i).Name))
                '    Console.WriteLine($"  - {app.RuleName}: [{String.Join(",", app.ItemIndices)}] {itemNames} → {app.DiscountedPrice:N0}円")
                'Next
                'Console.WriteLine("")
            End If

            If price < minPrice Then
                minPrice = price
                optimalCombination = combination
                minIdx = patternIdx
            End If

            patternIdx += 1
        Next

        Return (minPrice, minIdx, optimalCombination)
    End Function

    Private Sub GenerateAllCombinations(
        currentCombination As List(Of DiscountApplication),
        remainingIndices As List(Of Integer),
        usedInBundleDiscount As HashSet(Of Integer),
        usedInCouponDiscount As HashSet(Of Integer),
        startRuleIdx As Integer,
        result As List(Of List(Of DiscountApplication)))

        ' すべてのルール（startRuleIdx以降）を試す
        For ruleIdx = startRuleIdx To _discounts.Count - 1
            Dim rule = _discounts(ruleIdx)

            ' 未適用の明細が無しの場合は終了
            If remainingIndices.Count = 0 Then
                Exit For
            End If

            ' クーポン値引きが既に適用されているかチェック（前提条件4）
            Dim currentIdx = ruleIdx
            Dim couponAlreadyApplied = currentCombination.Any(Function(app) app.DiscountType = DiscountRule.DiscountType.CouponDiscount AndAlso app.RuleIndex = currentIdx)

            ' クーポン値引きが既に適用されている場合、クーポンルールをスキップ（前提条件4）
            If rule.Type = DiscountRule.DiscountType.CouponDiscount AndAlso couponAlreadyApplied Then
                Continue For
            End If

            ' 残りのアイテムで適用可能な組み合わせを探す
            Select Case rule.Type
                Case DiscountRule.DiscountType.BundleDiscount
                    Dim bundleRemaining = remainingIndices.Where(Function(i) Not usedInBundleDiscount.Contains(i) AndAlso Not usedInCouponDiscount.Contains(i)).ToList()
                    Dim itemCombinations = GenerateItemCombinations(bundleRemaining, rule)
                    For Each itemSet In itemCombinations
                        Dim items = itemSet.Select(Function(i) _items(i)).ToList()
                        If rule.Condition(items) Then
                            ApplyBundleDiscount(
                            currentCombination, remainingIndices, usedInBundleDiscount, usedInCouponDiscount,
                            itemSet, ruleIdx, rule, result)
                        End If
                    Next

                Case DiscountRule.DiscountType.SetDiscount
                    Dim items = _items.Where(Function(i) rule.ExtractCondition(i)).ToList()
                    Dim itemSet = remainingIndices.Where(Function(i) rule.ExtractCondition(_items(i))).ToList()
                    If itemSet.Count > 0 Then
                        Dim conditionMet = False
                        If rule.Condition(items) Then
                            ApplySetDiscount(
                                currentCombination, remainingIndices, usedInBundleDiscount, usedInCouponDiscount,
                                itemSet, ruleIdx, rule, result)
                        End If
                    End If

                Case DiscountRule.DiscountType.CouponDiscount
                    Dim couponRemaining = remainingIndices.Where(Function(i) Not usedInBundleDiscount.Contains(i) AndAlso Not usedInCouponDiscount.Contains(i)).ToList()
                    For Each itemIdx In couponRemaining
                        Dim items = New List(Of PurchaseItem) From {_items(itemIdx)}
                        If rule.Condition(items) Then
                            ApplyCouponDiscount(
                            currentCombination, remainingIndices, usedInBundleDiscount, usedInCouponDiscount,
                            itemIdx, ruleIdx, rule, result)
                        End If
                    Next

                Case DiscountRule.DiscountType.DmDiscount
                    Dim items = _items.Where(Function(i) rule.ExtractCondition(i)).ToList()
                    Dim itemSet = remainingIndices.Where(Function(i) rule.ExtractCondition(_items(i))).ToList()
                    If itemSet.Count > 0 Then
                        Dim conditionMet = False
                        If rule.Condition(items) Then
                            ApplyDmDiscount(
                                currentCombination, remainingIndices, usedInBundleDiscount, usedInCouponDiscount,
                                itemSet, ruleIdx, rule, result)
                        End If
                    End If

            End Select
        Next

        ' 現在の状態を結果に追加
        result.Add(New List(Of DiscountApplication)(currentCombination))

    End Sub

    Private Sub ApplyBundleDiscount(
        currentCombination As List(Of DiscountApplication),
        remainingIndices As List(Of Integer),
        usedInBundleDiscount As HashSet(Of Integer),
        usedInCouponDiscount As HashSet(Of Integer),
        itemSet As List(Of Integer),
        ruleIdx As Integer,
        rule As DiscountRule,
        result As List(Of List(Of DiscountApplication)))

        Dim items = itemSet.Select(Function(i) _items(i)).ToList()
        Dim originalPrice = items.Sum(Function(p) p.Price)
        Dim discountedPrice = rule.CalculatePrice(items)
        Dim discountAmount = Math.Max(0, originalPrice - discountedPrice)

        If discountAmount > originalPrice Then
            discountAmount = originalPrice
            discountedPrice = 0
        End If

        Dim discountApp As New DiscountApplication With {
        .ItemIndices = itemSet,
        .RuleIndex = ruleIdx,
        .RuleName = rule.Name,
        .DiscountType = rule.Type,
        .OriginalPrice = originalPrice,
        .DiscountedPrice = discountedPrice,
        .DiscountAmount = discountAmount
    }

        currentCombination.Add(discountApp)
        Dim newRemaining = remainingIndices.Except(itemSet).ToList()
        Dim newUsedInBundle = New HashSet(Of Integer)(usedInBundleDiscount)
        For Each idx In itemSet
            newUsedInBundle.Add(idx)
        Next
        Dim newCombination = DeepCopyDiscountApplicationList(currentCombination)

        ' ruleIdx の代わりに 0 から開始（すべてのルールを再度試す）
        GenerateAllCombinations(newCombination, newRemaining, newUsedInBundle, usedInCouponDiscount, 0, result)
        currentCombination.RemoveAt(currentCombination.Count - 1)
    End Sub

    Private Sub ApplySetDiscount(
        currentCombination As List(Of DiscountApplication),
        remainingIndices As List(Of Integer),
        usedInBundleDiscount As HashSet(Of Integer),
        usedInCouponDiscount As HashSet(Of Integer),
        itemSet As List(Of Integer),
        ruleIdx As Integer,
        rule As DiscountRule,
        result As List(Of List(Of DiscountApplication)))

        Dim items = itemSet.Select(Function(i) _items(i)).ToList()
        Dim originalPrice = items.Sum(Function(p) p.Price)
        Dim discountedPrice = rule.CalculatePrice(items)
        Dim discountAmount = Math.Max(0, originalPrice - discountedPrice)

        If discountAmount > originalPrice Then
            discountAmount = originalPrice
            discountedPrice = 0
        End If

        Dim discountApp As New DiscountApplication With {
            .ItemIndices = itemSet,
            .RuleIndex = ruleIdx,
            .RuleName = rule.Name,
            .DiscountType = rule.Type,
            .OriginalPrice = originalPrice,
            .DiscountedPrice = discountedPrice,
            .DiscountAmount = discountAmount
        }

        currentCombination.Add(discountApp)
        Dim newRemaining = remainingIndices.Except(itemSet).ToList()
        Dim newCombination = DeepCopyDiscountApplicationList(currentCombination)

        ' ruleIdx の代わりに 0 から開始（すべてのルールを再度試す）
        GenerateAllCombinations(newCombination, newRemaining, usedInBundleDiscount, usedInCouponDiscount, 0, result)
        currentCombination.RemoveAt(currentCombination.Count - 1)

    End Sub

    Private Sub ApplyCouponDiscount(
        currentCombination As List(Of DiscountApplication),
        remainingIndices As List(Of Integer),
        usedInBundleDiscount As HashSet(Of Integer),
        usedInCouponDiscount As HashSet(Of Integer),
        itemIdx As Integer,
        ruleIdx As Integer,
        rule As DiscountRule,
        result As List(Of List(Of DiscountApplication)))

        Dim item = _items(itemIdx)
        Dim originalPrice = item.Price
        Dim discountedPrice = rule.CalculatePrice(New List(Of PurchaseItem) From {item})
        Dim discountAmount = Math.Max(0, originalPrice - discountedPrice)

        If discountAmount > originalPrice Then
            discountAmount = originalPrice
            discountedPrice = 0
        End If

        Dim discountApp As New DiscountApplication With {
            .ItemIndices = New List(Of Integer) From {itemIdx},
            .RuleIndex = ruleIdx,
            .RuleName = rule.Name,
            .DiscountType = rule.Type,
            .OriginalPrice = originalPrice,
            .DiscountedPrice = discountedPrice,
            .DiscountAmount = discountAmount
        }

        currentCombination.Add(discountApp)
        Dim newRemaining = remainingIndices.Except(New List(Of Integer) From {itemIdx}).ToList()
        Dim newUsedInCoupon = New HashSet(Of Integer)(usedInCouponDiscount)
        newUsedInCoupon.Add(itemIdx)
        Dim newCombination = DeepCopyDiscountApplicationList(currentCombination)

        ' ruleIdx の代わりに 0 から開始（全てのルールを再度試す）
        GenerateAllCombinations(newCombination, newRemaining, usedInBundleDiscount, newUsedInCoupon, 0, result)
        currentCombination.RemoveAt(currentCombination.Count - 1)
    End Sub

    Private Sub ApplyDmDiscount(
        currentCombination As List(Of DiscountApplication),
        remainingIndices As List(Of Integer),
        usedInBundleDiscount As HashSet(Of Integer),
        usedInCouponDiscount As HashSet(Of Integer),
        itemSet As List(Of Integer),
        ruleIdx As Integer,
        rule As DiscountRule,
        result As List(Of List(Of DiscountApplication)))

        Dim items = itemSet.Select(Function(i) _items(i)).ToList()
        Dim originalPrice = items.Sum(Function(p) p.Price)
        Dim discountedPrice = rule.CalculatePrice(items)
        Dim discountAmount = Math.Max(0, originalPrice - discountedPrice)

        If discountAmount > originalPrice Then
            discountAmount = originalPrice
            discountedPrice = 0
        End If

        Dim discountApp As New DiscountApplication With {
            .ItemIndices = itemSet,
            .RuleIndex = ruleIdx,
            .RuleName = rule.Name,
            .DiscountType = rule.Type,
            .OriginalPrice = originalPrice,
            .DiscountedPrice = discountedPrice,
            .DiscountAmount = discountAmount
        }

        currentCombination.Add(discountApp)
        Dim newRemaining = remainingIndices.Except(itemSet).ToList()
        Dim newCombination = DeepCopyDiscountApplicationList(currentCombination)

        ' ruleIdx の代わりに 0 から開始（すべてのルールを再度試す）
        GenerateAllCombinations(newCombination, newRemaining, usedInBundleDiscount, usedInCouponDiscount, 0, result)
        currentCombination.RemoveAt(currentCombination.Count - 1)

    End Sub

    ' List(Of DiscountApplication) を深くコピーするヘルパーメソッド（LINQ版）
    Private Shared Function DeepCopyDiscountApplicationList(source As List(Of DiscountApplication)) As List(Of DiscountApplication)
        Return source.Select(Function(app) New DiscountApplication With {
        .ItemIndices = New List(Of Integer)(app.ItemIndices),
        .RuleIndex = app.RuleIndex,
        .RuleName = app.RuleName,
        .DiscountType = app.DiscountType,
        .OriginalPrice = app.OriginalPrice,
        .DiscountedPrice = app.DiscountedPrice,
        .DiscountAmount = app.DiscountAmount
    }).ToList()
    End Function

    Private Function GenerateItemCombinations(indices As List(Of Integer), rule As DiscountRule) As List(Of List(Of Integer))
        Dim combinations As New List(Of List(Of Integer))

        Select Case rule.Type
            Case DiscountRule.DiscountType.BundleDiscount
                ' まとめ値引き：2個以上の組み合わせ
                For size = 2 To indices.Count
                    Dim combos = GetCombinations(indices, size)
                    combinations.AddRange(combos)
                Next

            Case DiscountRule.DiscountType.SetDiscount
                ' セット値引き：2個以上の組み合わせ
                For size = 2 To indices.Count
                    Dim combos = GetCombinations(indices, size)
                    combinations.AddRange(combos)
                Next

            Case DiscountRule.DiscountType.CouponDiscount
                ' クーポン値引き：1個のみ
                For Each idx In indices
                    combinations.Add(New List(Of Integer) From {idx})
                Next

            Case DiscountRule.DiscountType.DmDiscount
                ' DM値引き：1個ずつ
                For Each idx In indices
                    combinations.Add(New List(Of Integer) From {idx})
                Next
        End Select

        Return combinations
    End Function

    Private Function GetCombinations(items As List(Of Integer), size As Integer) As List(Of List(Of Integer))
        If size = 0 Then
            Return New List(Of List(Of Integer)) From {New List(Of Integer)()}
        End If
        If items.Count = 0 Then
            Return New List(Of List(Of Integer))()
        End If

        Dim result As New List(Of List(Of Integer))
        Dim head = items(0)
        Dim tail = items.Skip(1).ToList()

        For Each combination In GetCombinations(tail, size - 1)
            combination.Insert(0, head)
            result.Add(combination)
        Next

        For Each combination In GetCombinations(tail, size)
            result.Add(combination)
        Next

        Return result
    End Function

    Private Function CalculateTotalPrice(combination As List(Of DiscountApplication)) As Decimal
        Dim usedItems As New HashSet(Of Integer)
        Dim totalPrice As Decimal = 0

        ' 割引が適用されたアイテムの金額
        For Each app In combination
            totalPrice += app.DiscountedPrice
            For Each idx In app.ItemIndices
                usedItems.Add(idx)
            Next
        Next

        ' 割引が適用されないアイテムの金額
        For i = 0 To _items.Count - 1
            If Not usedItems.Contains(i) Then
                totalPrice += _items(i).Price
            End If
        Next

        Return totalPrice
    End Function
End Class