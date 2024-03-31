module PnLCalculationTests

open NUnit.Framework
open PnLCalculation

[<TestFixture>]
type PnLCalculationTests() =

    [<Test>]
    member this.``Profit Calculation for Valid Transaction`` () =
        let transaction = { TransactionType = Buy; PurchaseCost = 100M; SaleRevenue = 150M; Amount = 1M }
        let expected = Profit 50M
        match calculateProfitLoss transaction with
        | Ok result -> Assert.AreEqual(expected, result)
        | Error _ -> Assert.Fail("Expected a valid Profit calculation.")

    [<Test>]
    member this.``Transaction with Zero Amount`` () =
        let transaction = { TransactionType = Sell; PurchaseCost = 100M; SaleRevenue = 150M; Amount = 0M }
        match calculateProfitLoss transaction with
        | Ok _ -> Assert.Fail("Expected an error for transaction with zero amount.")
        | Error (InvalidTransaction msg) -> Assert.IsTrue(msg.Contains("invalid values"))
        | Error _ -> Assert.Fail("Unexpected error type.")

    [<Test>]
    member this.``Transaction with Extremely High Values`` () =
        let transaction = { TransactionType = Buy; PurchaseCost = 1e+10M; SaleRevenue = 1e+10M + 100M; Amount = 1M }
        let expected = Profit 100M
        match calculateProfitLoss transaction with
        | Ok result -> Assert.AreEqual(expected, result)
        | Error _ -> Assert.Fail("Expected a valid Profit calculation for high values.")

    [<Test>]
    member this.``Aggregate Multiple Transactions`` () =
        let transactions = [
            { TransactionType = Buy; PurchaseCost = 100M; SaleRevenue = 150M; Amount = 1M };
            { TransactionType = Sell; PurchaseCost = 50M; SaleRevenue = 100M; Amount = 2M }
        ]
        match calculateTotalPnL transactions with
        | Ok (Profit totalProfit) -> Assert.AreEqual(150M, totalProfit)
        | Error _ -> Assert.Fail("Expected valid aggregation of transactions.")

    [<Test>]
    member this.``Handle Invalid Transaction in Aggregation`` () =
        let transactions = [
            { TransactionType = Buy; PurchaseCost = 100M; SaleRevenue = 150M; Amount = 1M };
            { TransactionType = Sell; PurchaseCost = -50M; SaleRevenue = 100M; Amount = 1M } // Invalid transaction
        ]
        match calculateTotalPnL transactions with
        | Ok _ -> Assert.Fail("Expected an error due to invalid transaction in the list.")
        | Error (InvalidTransaction msg) -> Assert.IsTrue(msg.Contains("invalid values"))
        | Error _ -> Assert.Fail("Unexpected error type.")
