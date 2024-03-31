module HistoricalDataAnalysisTests

open NUnit.Framework
open HistoricalDataAnalysis

[<TestFixture>]
type HistoricalDataAnalysisTests() =

    // Sample market data for testing
    let sampleMarketData = [
        { Pair = "BTC-USD"; BidPrice = 10000M; BidSize = 1M; AskPrice = 10001M; AskSize = 1M; Timestamp = 1L; Exchange = 1 };
        { Pair = "BTC-USD"; BidPrice = 10002M; BidSize = 1M; AskPrice = 10003M; AskSize = 1M; Timestamp = 2L; Exchange = 2 };
    ]

    [<Test>]
    member this.``Arbitrage Opportunity Found for BTC-USD`` () =
        let opportunities = identifyArbitrageOpportunities sampleMarketData
        let btcOpportunity = opportunities |> List.tryFind (fun o -> o.Pair = "BTC-USD")
        match btcOpportunity with
        | Some opp -> NUnit.Framework.Assert.AreEqual(1, opp.NumberOfOpportunities) // Expecting 1 opportunity for this simple case
        | None -> NUnit.Framework.Assert.Fail("Expected to find an arbitrage opportunity for BTC-USD.")

    [<Test>]
    member this.``No Arbitrage Opportunity for Stable Pair`` () =
        let stablePairData = [
            { Pair = "STABLE-USD"; BidPrice = 1M; BidSize = 100M; AskPrice = 1M; AskSize = 100M; Timestamp = 1L; Exchange = 1 };
            { Pair = "STABLE-USD"; BidPrice = 1M; BidSize = 100M; AskPrice = 1M; AskSize = 100M; Timestamp = 2L; Exchange = 2 };
        ]
        let opportunities = identifyArbitrageOpportunities stablePairData
        let stableOpportunity = opportunities |> List.tryFind (fun o -> o.Pair = "STABLE-USD")
        NUnit.Framework.Assert.IsTrue(stableOpportunity.IsNone, "Expected no arbitrage opportunities for STABLE-USD due to stable prices.")
