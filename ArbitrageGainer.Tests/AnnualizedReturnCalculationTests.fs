module AnnualizedReturnCalculationTests

open NUnit.Framework
open AnnualizedReturnCalculation
open System

[<TestFixture>]
type AnnualizedReturnCalculationTests() =

    [<Test>]
    member this.``Annualized Return for Valid Input`` () =
        let initialInvestment = 1000
        let totalReturn = 1200
        let startTime = DateTime(2020, 1, 1)
        match calculateAnnualizedReturn initialInvestment totalReturn startTime with
        | Ok annualizedReturn -> Assert.IsTrue(annualizedReturn > 0)
        | Error _ -> Assert.Fail("Expected a successful calculation of annualized return.")
    
    [<Test>]
    member this.``Annualized Return for Invalid Duration, start time is in future`` () =
        let initialInvestment = 1000
        let totalReturn = 1200000
        let startTime = DateTime.UtcNow.AddMinutes(1)
        match calculateAnnualizedReturn initialInvestment totalReturn startTime with
        | Ok _ -> Assert.Fail("Expected failure due to invalid duration.")
        | Error (InvalidDuration _) -> Assert.Pass("Correctly identified invalid duration.")
        | Error _ -> Assert.Fail("Unexpected error type.")

    [<Test>]
    member this.``Annualized Return for Negative Total Return`` () =
        let initialInvestment = 1000
        let totalReturn = -200 // Scenario with a loss
        let startTime = DateTime(2020, 1, 1)
        match calculateAnnualizedReturn initialInvestment totalReturn startTime with
        | Ok annualizedReturn -> Assert.IsTrue(annualizedReturn < 0, "Expected a negative annualized return.")
        | Error _ -> Assert.Fail("Expected a successful calculation, even with a negative return.")

    [<Test>]
    member this.``Annualized Return for Very Short Investment Period`` () =
        let initialInvestment = 1000
        let totalReturn = 1010
        let startTime = DateTime.UtcNow.AddDays(-15) // 15 days ago
        match calculateAnnualizedReturn initialInvestment totalReturn startTime with
        | Ok annualizedReturn -> Assert.IsTrue(annualizedReturn > 0, "Expected a positive annualized return for a short period.")
        | Error _ -> Assert.Fail("Expected a successful calculation for a short investment period.")

    [<Test>]
    member this.``Annualized Return for Zero Initial Investment`` () =
        let initialInvestment = 0 // Scenario where there was no initial investment
        let totalReturn = 1000
        let startTime = DateTime(2020, 1, 1)
        match calculateAnnualizedReturn initialInvestment totalReturn startTime with
        | Ok _ -> Assert.Fail("Expected failure due to zero initial investment.")
        | Error (InvalidInitialInvestment msg) -> Assert.IsTrue(msg.Contains("Initial investment must be greater than zero"), "Expected validation failure for zero initial investment.")
        | Error _ -> Assert.Fail("Unexpected error type.")

