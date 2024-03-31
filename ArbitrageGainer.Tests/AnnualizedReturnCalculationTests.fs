module AnnualizedReturnCalculationTests

open NUnit.Framework
open AnnualizedReturnCalculation
open System

[<TestFixture>]
type AnnualizedReturnCalculationTests() =

    [<Test>]
    member this.``Annualized Return for Valid Input`` () =
        let initialInvestment = 1000M
        let totalReturn = 1200M
        let startTime = DateTime(2020, 1, 1)
        match calculateAnnualizedReturn initialInvestment totalReturn startTime with
        | Ok annualizedReturn -> Assert.IsTrue(annualizedReturn > 0M)
        | Error _ -> Assert.Fail("Expected a successful calculation of annualized return.")
    
    [<Test>]
    member this.``Annualized Return for Invalid Duration`` () =
        let initialInvestment = 1000M
        let totalReturn = 1200M
        let startTime = DateTime.UtcNow.AddYears(1) // Future date, invalid scenario
        match calculateAnnualizedReturn initialInvestment totalReturn startTime with
        | Ok _ -> Assert.Fail("Expected failure due to invalid duration.")
        | Error (InvalidDuration msg) -> Assert.IsTrue(msg.Contains("Duration must be greater than zero"))
        | Error _ -> Assert.Fail("Unexpected error type.")

    [<Test>]
    member this.``Annualized Return for Negative Total Return`` () =
        let initialInvestment = 1000M
        let totalReturn = -200M // Scenario with a loss
        let startTime = DateTime(2020, 1, 1)
        match calculateAnnualizedReturn initialInvestment totalReturn startTime with
        | Ok annualizedReturn -> Assert.IsTrue(annualizedReturn < 0M, "Expected a negative annualized return.")
        | Error _ -> Assert.Fail("Expected a successful calculation, even with a negative return.")

    [<Test>]
    member this.``Annualized Return for Very Short Investment Period`` () =
        let initialInvestment = 1000M
        let totalReturn = 1010M
        let startTime = DateTime.UtcNow.AddDays(-15) // 15 days ago
        match calculateAnnualizedReturn initialInvestment totalReturn startTime with
        | Ok annualizedReturn -> Assert.IsTrue(annualizedReturn > 0M, "Expected a positive annualized return for a short period.")
        | Error _ -> Assert.Fail("Expected a successful calculation for a short investment period.")

    [<Test>]
    member this.``Annualized Return for Zero Initial Investment`` () =
        let initialInvestment = 0M // Scenario where there was no initial investment
        let totalReturn = 1000M
        let startTime = DateTime(2020, 1, 1)
        match calculateAnnualizedReturn initialInvestment totalReturn startTime with
        | Ok _ -> Assert.Fail("Expected failure due to zero initial investment.")
        | Error (InvalidInitialInvestment msg) -> Assert.IsTrue(msg.Contains("Initial investment must be greater than zero"), "Expected validation failure for zero initial investment.")
        | Error _ -> Assert.Fail("Unexpected error type.")

