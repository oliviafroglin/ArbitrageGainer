module AnnualizedReturnCalculationTests

open NUnit.Framework
open AnnualizedReturnCalculationCore
open PnLCalculationCore
open System

[<TestFixture>]
type AnnualizedReturnTests() =

    [<Test>]
    member this.``Calculate Initial Investment with No Transactions``() =
        let transactions = []
        let result = calculateInitialInvestment transactions
        Assert.AreEqual(0m, result, "Expected initial investment to be zero with no transactions.")

    [<Test>]
    member this.``Calculate Initial Investment with Only Buys``() =
        let transactions = [
            { TransactionType = Buy; Price = 200m; Amount = 2m; TransactionDate = DateTime(2020, 1, 1) }
        ]
        let result = calculateInitialInvestment transactions
        Assert.AreEqual(-400m, result, "Expected initial investment to be negative due to only buys.")

    [<Test>]
    member this.``Calculate Initial Investment with Only Sells``() =
        let transactions = [
            { TransactionType = Sell; Price = 300m; Amount = 3m; TransactionDate = DateTime(2020, 1, 1) }
        ]
        let result = calculateInitialInvestment transactions
        Assert.AreEqual(900m, result, "Expected initial investment to be positive due to only sells.")

    [<Test>]
    member this.``Annualized Return for Zero Duration``() =
        let investmentDetails = {
            StartDate = DateTime(2020, 1, 1)
            InitialInvestment = 1000m
            TotalReturn = 1200m
            DurationYears = 0.0  // This will cause division by zero in the calculation
        }
        try
            let result = calculateAnnualizedReturn investmentDetails
            Assert.Fail("Expected a division by zero exception, but calculation succeeded with result: " + result.ToString())
        with
        | :? System.DivideByZeroException -> Assert.Pass("Division by zero exception correctly thrown.")
        | ex -> Assert.Fail("Expected a division by zero exception, but got another type: " + ex.GetType().ToString())



    [<Test>]
    member this.``Annualized Return for Negative Initial Investment``() =
        let investmentDetails = {
            StartDate = DateTime(2020, 1, 1)
            InitialInvestment = -1000m
            TotalReturn = 1200m
            DurationYears = 1.0
        }
        let result = calculateAnnualizedReturn investmentDetails
        Assert.IsTrue(result < 0.0, "Expected a negative annualized return due to the nature of negative initial investment resulting in a decremental outcome.")


    [<Test>]
    member this.``Annualized Return with Exponential Growth``() =
        let investmentDetails = {
            StartDate = DateTime(2020, 1, 1)
            InitialInvestment = 1m
            TotalReturn = 1000m
            DurationYears = 1.0
        }
        let result = calculateAnnualizedReturn investmentDetails
        Assert.IsTrue(result > 0.0, "Expected a very high annualized return due to exponential growth relative to the small initial investment.")

    [<Test>]
    member this.``Annualized Return Calculation Precision``() =
        let investmentDetails = {
            StartDate = DateTime(2020, 1, 1)
            InitialInvestment = 1000m
            TotalReturn = 1000.01m
            DurationYears = 1.0
        }
        let result = calculateAnnualizedReturn investmentDetails
        Assert.IsTrue(result > 0.0 && result < 0.0001, "Expected a very small positive annualized return, testing precision of the calculation.")

    [<Test>]
    member this.``Annualized Return for Long Duration``() =
        let investmentDetails = {
            StartDate = DateTime(2000, 1, 1)
            InitialInvestment = 1000m
            TotalReturn = 2000m
            DurationYears = 20.0
        }
        let result = calculateAnnualizedReturn investmentDetails
        Assert.IsTrue(result > 0.0, "Expected a positive annualized return over a long investment period.")

