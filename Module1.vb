Module Module1

    Sub Main()
        ' 購入明細
        Dim items As New List(Of PurchaseItem) From {
        New PurchaseItem With {.Index = 0, .Category = "ゲーム機本体", .Name = "SWITCH", .Price = 40000},
        New PurchaseItem With {.Index = 1, .Category = "ゲーム機本体", .Name = "SWITCH2", .Price = 42000},
        New PurchaseItem With {.Index = 2, .Category = "ゲームソフト", .Name = "スーパーマリオ", .Price = 5000},
        New PurchaseItem With {.Index = 3, .Category = "ゲームソフト", .Name = "マリオカート", .Price = 6000},
        New PurchaseItem With {.Index = 4, .Category = "ゲームソフト", .Name = "ドンキーコング", .Price = 3000},
        New PurchaseItem With {.Index = 5, .Category = "ゲームソフト", .Name = "ベースボール", .Price = 3000},
        New PurchaseItem With {.Index = 6, .Category = "ゲームソフト", .Name = "サッカー", .Price = 8000},
        New PurchaseItem With {.Index = 7, .Category = "ゲームソフト", .Name = "バスケットボール", .Price = 3000},
        New PurchaseItem With {.Index = 8, .Category = "オンライン用カード", .Name = "nintendo online カード", .Price = 1000},
        New PurchaseItem With {.Index = 9, .Category = "オンライン用カード", .Name = "nintendo online カード", .Price = 1000},
        New PurchaseItem With {.Index = 10, .Category = "雑貨", .Name = "紙袋", .Price = 200}
}

        ' 値引きルール設定
        ' まとめ値引き1: ゲーム機本体 + (スーパーマリオ OR マリオカート) → 37000円
        ' まとめ値引き2: ゲームフト + オンライン用カード → ゲーム機ソフト550円引き
        ' セット値引き1: ゲームソフト 2点以上 → 各500円引き
        ' セット値引き2: ゲームソフト 3点以上 → 各600円引き
        ' クーポン値引き: ゲームソフト1点のみ → 5000円引き
        Dim discounts As New List(Of DiscountRule) From {
        New DiscountRule With {
            .Name = "ゲーム機本体セット",
            .Type = DiscountRule.DiscountType.BundleDiscount,
            .Condition = Function(purchaseItems) _
                purchaseItems.Count = 2 AndAlso
                purchaseItems.Where(Function(p) p.Category = "ゲーム機本体").Count() = 1 AndAlso
                purchaseItems.Where(Function(p) p.Category = "ゲームソフト" AndAlso (p.Name = "スーパーマリオ" OrElse p.Name = "マリオカート")).Count() = 1,
            .ExtractCondition = Function(item) _
                item.Category = "ゲーム機本体" OrElse
                (item.Category = "ゲームソフト" AndAlso (item.Name = "スーパーマリオ" OrElse item.Name = "マリオカート")),
            .CalculatePrice = Function(purchaseItems) 37000D
        },
        New DiscountRule With {
            .Name = "ソフト+カードセット",
            .Type = DiscountRule.DiscountType.BundleDiscount,
            .Condition = Function(purchaseItems) _
                purchaseItems.Count = 2 AndAlso
                purchaseItems.Where(Function(p) p.Category = "ゲームソフト").Count() = 1 AndAlso
                purchaseItems.Where(Function(p) p.Category = "オンライン用カード").Count() = 1,
            .ExtractCondition = Function(item) _
                item.Category = "ゲームソフト" OrElse item.Category = "オンライン用カード",
            .CalculatePrice = Function(purchaseItems) _
                purchaseItems.Sum(Function(p) If(p.Category = "ゲームソフト", Math.Max(0, p.Price - 550), p.Price))
        },
        New DiscountRule With {
            .Name = "ゲームソフト2点以上購入",
            .Type = DiscountRule.DiscountType.SetDiscount,
            .Condition = Function(purchaseItems) _
                purchaseItems.Count >= 2 AndAlso
                purchaseItems.All(Function(p) p.Category = "ゲームソフト"),
            .ExtractCondition = Function(item) _
                item.Category = "ゲームソフト",
            .CalculatePrice = Function(purchaseItems) _
                purchaseItems.Sum(Function(p) Math.Max(0, p.Price - 300))
        },
        New DiscountRule With {
            .Name = "ゲームソフト5点以上購入",
            .Type = DiscountRule.DiscountType.SetDiscount,
            .Condition = Function(purchaseItems) _
                purchaseItems.Count >= 5 AndAlso
                purchaseItems.All(Function(p) p.Category = "ゲームソフト"),
            .ExtractCondition = Function(item) _
                item.Category = "ゲームソフト",
            .CalculatePrice = Function(purchaseItems) _
                purchaseItems.Sum(Function(p) Math.Max(0, p.Price - 400))
        },
        New DiscountRule With {
            .Name = "ゲームソフトクーポン",
            .Type = DiscountRule.DiscountType.CouponDiscount,
            .Condition = Function(purchaseItems) _
                purchaseItems.Count = 1 AndAlso
                (purchaseItems(0).Category = "ゲームソフト" OrElse purchaseItems(0).Category = "ゲームソフト"),
            .ExtractCondition = Function(item) _
                item.Category = "ゲームソフト",
            .CalculatePrice = Function(purchaseItems) Math.Max(0, purchaseItems(0).Price - 5000)
        },
        New DiscountRule With {
            .Name = "全商品50%割引",
            .Type = DiscountRule.DiscountType.DmDiscount,
            .Condition = Function(purchaseItems) _
                purchaseItems.Count = 1 AndAlso
                purchaseItems(0).Category = "雑貨",
            .ExtractCondition = Function(item) _
                item.Category = "雑貨",
            .CalculatePrice = Function(purchaseItems) Math.Max(0, purchaseItems(0).Price * 0.5)
        }
        }

        ' 計算実行
        Dim calculator As New BundleDiscountCalculator(items, discounts)
        Dim result = calculator.FindMinimumPrice()

        Console.WriteLine($"最安結果: {result.minIdx:N0}番目")
        Console.WriteLine($"最安価格: {result.minPrice:N0}円")
        Console.WriteLine("適用値引き内容:")
        For Each app In result.optimalCombination
            Dim itemNames = String.Join(", ", app.ItemIndices.Select(Function(i) items(i).Name))
            Console.WriteLine($"  - アイテム[{String.Join(",", app.ItemIndices)}]({itemNames})")
            Console.WriteLine($"    → {app.RuleName}")
            Console.WriteLine($"    → 元値: {app.OriginalPrice:N0}円 → {app.DiscountedPrice:N0}円（{app.DiscountAmount:N0}円引き）")
        Next

        ' 使用されなかったアイテムを表示
        Dim usedIndices = New HashSet(Of Integer)(result.optimalCombination.SelectMany(Function(a) a.ItemIndices))
        For i = 0 To items.Count - 1
            If Not usedIndices.Contains(i) Then
                Console.WriteLine($"  - アイテム[{i}]({items(i).Name}) → 値引きなし → {items(i).Price:N0}円")
            End If
        Next
    End Sub

End Module
