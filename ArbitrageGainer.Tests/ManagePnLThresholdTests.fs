module ManagePnLThresholdUnitTests

open NUnit.Framework
open ManagePnLThresholdCore
open ManagePnLThresholdService
open Moq
open System.Threading.Tasks


[<TestFixture>]
type ManagePnLThresholdUnitTests() =

    [<Test>]
    member this.``Validate Positive Threshold``() =
        let result = validateThreshold 500m
        match result with
        | Valid _ -> Assert.Pass("Valid threshold accepted correctly.")
        | Invalid _ -> Assert.Fail("Valid threshold was incorrectly rejected.")

    [<Test>]
    member this.``Validate Negative Threshold``() =
        let result = validateThreshold -500m
        match result with
        | Valid _ -> Assert.Fail("Negative threshold was incorrectly accepted.")
        | Invalid msg -> Assert.AreEqual("Threshold must be positive", msg)


    // [<Test>]
    // member this.``Mock Agent GetThreshold Returns Ok``() =
    //     let mockAgent = new Mock<PnLThresholdAgent>()
    //     let expectedThreshold = 200m
    //
    //     mockAgent.Setup(fun agent -> agent.GetThreshold())
    //              .Returns(Task.FromResult(ThresholdResult.Ok(expectedThreshold)) :> Task<ThresholdResult>)
    //
    //     let actualResult = mockAgent.Object.GetThreshold() |> Async.RunSynchronously
    //
    //     match actualResult with
    //     | Ok value when value = expectedThreshold -> 
    //         Assert.Pass("Threshold returned as expected.")
    //     | _ -> 
    //         Assert.Fail("Threshold not returned as expected.")