module HistoricalDataAnalysisTests

open NUnit.Framework
open HistoricalDataAnalysisCore
open HistoricalDataAnalysisService

[<TestFixture>]
type HistoricalDataAnalysisTests() =

    // Prepare sample market data mimicking real input where opportunities exist
    let sampleMarketData = [
        { Pair = "BTC-USD"; BidPrice = 10000M; BidSize = 1M; AskPrice = 10010M; AskSize = 1M; Timestamp = 100L; Exchange = 1 };
        { Pair = "BTC-USD"; BidPrice = 10020M; BidSize = 1M; AskPrice = 10030M; AskSize = 1M; Timestamp = 105L; Exchange = 2 };
    ]

    [<Test>]
    member this.``Arbitrage Opportunity Found for BTC-USD`` () =
        let opportunities = identifyArbitrageOpportunities sampleMarketData
        let btcOpportunity = opportunities |> List.tryFind (fun (pair, _) -> pair = "BTC-USD")
        match btcOpportunity with
        | Some (_, count) -> Assert.AreEqual(1, count) // Expecting 1 opportunity for this simple case
        | None -> Assert.Fail("Expected to find an arbitrage opportunity for BTC-USD.")

    [<Test>]
    member this.``No Arbitrage Opportunity for Stable Pair`` () =
        let stablePairData = [
            { Pair = "STABLE-USD"; BidPrice = 1M; BidSize = 100M; AskPrice = 1.01M; AskSize = 100M; Timestamp = 100L; Exchange = 1 };
            { Pair = "STABLE-USD"; BidPrice = 1M; BidSize = 100M; AskPrice = 1.01M; AskSize = 100M; Timestamp = 105L; Exchange = 2 };
        ]
        let opportunities = identifyArbitrageOpportunities stablePairData
        let stableOpportunity = opportunities |> List.tryFind (fun (pair, _) -> pair = "STABLE-USD")
        Assert.IsTrue(stableOpportunity.IsNone, "Expected no arbitrage opportunities for STABLE-USD due to stable prices.")

    // Adding more complex scenarios involving multiple exchanges and time stamps
    [<Test>]
    member this.``Multiple Exchanges With Valid Opportunities`` () =
        let complexMarketData = [
            { Pair = "ETH-USD"; BidPrice = 500M; BidSize = 50M; AskPrice = 505M; AskSize = 50M; Timestamp = 100L; Exchange = 1 };
            { Pair = "ETH-USD"; BidPrice = 510M; BidSize = 50M; AskPrice = 515M; AskSize = 50M; Timestamp = 105L; Exchange = 2 };
            { Pair = "ETH-USD"; BidPrice = 498M; BidSize = 50M; AskPrice = 502M; AskSize = 50M; Timestamp = 101L; Exchange = 3 }
        ]
        let opportunities = identifyArbitrageOpportunities complexMarketData
        printfn "opportunities: %A" opportunities
        let ethOpportunity = opportunities |> List.tryFind (fun (pair, _) -> pair = "ETH-USD")
        match ethOpportunity with
        | Some (_, count) -> Assert.IsTrue(count > 1, "Expected multiple arbitrage opportunities for ETH-USD.")
        | None -> Assert.Fail("Expected to find multiple arbitrage opportunities for ETH-USD.")