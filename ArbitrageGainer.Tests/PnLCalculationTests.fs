module PnLCalculationServiceTests

open NUnit.Framework
open PnLCalculationCore
open PnLCalculationService
open System

[<TestFixture>]
type PnLCalculationServiceTests() =

    [<Test>]
    member this.``Calculate Total PnL with No Transactions``() =
        let transactions = []
        let result = calculateTotalPnL transactions
        Assert.AreEqual(0m, result, "Expected total PnL to be zero with no transactions.")

    [<Test>]
    member this.``Calculate Total PnL with Only Losses``() =
        let transactions = [
            { TransactionType = Sell;  Price = 100m; Amount = 1m; TransactionDate = DateTime(2021, 1, 1) }
            { TransactionType = Buy;  Price = 150m; Amount = 1m; TransactionDate = DateTime(2021, 1, 2) }
        ]
        let result = calculateTotalPnL transactions
        // Since the Sell results in a loss (because BuyPrice > SellPrice), we expect a negative PnL
        Assert.IsTrue(result < 0m, "Expected total PnL to be negative due to the nature of the transactions.")

    [<Test>]
    member this.``Calculate Total PnL with Break-Even Transactions``() =
        let transactions = [
            { TransactionType = Buy; Price = 100m; Amount = 1m; TransactionDate = DateTime(2021, 1, 1) }
            { TransactionType = Sell; Price = 100m; Amount = 1m; TransactionDate = DateTime(2021, 1, 2) }
        ]
        let result = calculateTotalPnL transactions
        Assert.AreEqual(0m, result, "Expected total PnL to be zero for break-even transactions.")

    [<Test>]
    member this.``Calculate Total PnL with High Precision Prices``() =
        let transactions = [
            { TransactionType = Buy; Price = 100.1234m; Amount = 1.2345m; TransactionDate = DateTime(2021, 1, 1) }
            { TransactionType = Sell; Price = 100.5678m; Amount = 1.2345m; TransactionDate = DateTime(2021, 1, 2)}
        ]
        let result = calculateTotalPnL transactions
        Assert.IsTrue(result > 0m, "Expected a positive total PnL with precise decimal operations.")

    [<Test>]
    member this.``Calculate Historical PnL on Boundary Dates``() =
        let transactions = [
            { TransactionType = Buy; Price = 100m; Amount = 1m; TransactionDate = DateTime(2021, 1, 1) };
            { TransactionType = Sell; Price = 150m; Amount = 1m; TransactionDate = DateTime(2021, 12, 31) }
        ]
        let dateRange = { StartDate = DateTime(2021, 1, 1); EndDate = DateTime(2021, 12, 31) }
        let result = calculateHistoricalPnL transactions dateRange
        match result with
        | Ok (Profit p) -> Assert.IsTrue(p >= 0m, "Expected a profit from transactions on the boundary dates.")
        | _ -> Assert.Fail("Unexpected result when calculating PnL on boundary dates.")

    [<Test>]
    member this.``Historical PnL Calculation with Future Transactions``() =
        let transactions = [
            { TransactionType = Buy; Price = 100m; Amount = 1m; TransactionDate = DateTime(2022, 1, 1) }
            { TransactionType = Buy; Price = 200m; Amount = 1m; TransactionDate = DateTime(2022, 1, 2) }
        ]
        let dateRange = { StartDate = DateTime(2021, 1, 1); EndDate = DateTime(2021, 12, 31) }
        let result = calculateHistoricalPnL transactions dateRange
        match result with
        | Error msg when msg = "No transactions found in the given date range." -> 
            Assert.Pass("Correct error message returned for transactions outside the specified date range.")
        | Error _ -> 
            Assert.Fail("Error returned, but the message was not correct.")
        | Ok _ -> 
            Assert.Fail("Expected an error due to transactions being outside the specified date range but got Ok result.")

    [<Test>]
    member this.``Historical PnL for Short Date Range``() =
        let transactions = [
            { TransactionType = Buy; Price = 150m; Amount = 2m; TransactionDate = DateTime(2021, 6, 15) }
            { TransactionType = Sell; Price = 400m; Amount = 1m; TransactionDate = DateTime(2021, 6, 16) }
        ]
        let dateRange = { StartDate = DateTime(2021, 6, 14); EndDate = DateTime(2021, 6, 16) }
        let result = calculateHistoricalPnL transactions dateRange
        match result with
        | Ok (Profit p) -> Assert.IsTrue(p > 0m, "Expected a profit from transactions within a very short date range.")
        | _ -> Assert.Fail("Unexpected result when calculating PnL for a short date range.")
