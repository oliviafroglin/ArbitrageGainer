module HistoricalDataAnalysisCore

open Newtonsoft.Json

// Represents market data.
[<CLIMutable>]
type MarketData = {
    [<JsonProperty("pair")>]
    Pair: string
    [<JsonProperty("bp")>]
    BidPrice: decimal
    [<JsonProperty("bs")>]
    BidSize: decimal
    [<JsonProperty("ap")>]
    AskPrice: decimal
    [<JsonProperty("as")>]
    AskSize: decimal
    [<JsonProperty("t")>]
    Timestamp: int64
    [<JsonProperty("x")>]
    Exchange: int
}

// Represents an arbitrage opportunity.
type ArbitrageOpportunity = {
    Pair: string
    NumberOfOpportunities: int
}

// Maps market data to arbitrage opportunities.
let mapPhase (quotes: MarketData list) =
    quotes |> List.groupBy (fun d -> d.Timestamp / 5L)
           |> List.collect (fun (_, bucketQuotes) ->
                let exchangeGroups = bucketQuotes |> List.groupBy (fun q -> q.Exchange)
                match List.length exchangeGroups with
                | count when count > 1 ->
                    let highestBid = bucketQuotes |> List.maxBy (fun q -> q.BidPrice)
                    let lowestAsk = bucketQuotes |> List.minBy (fun q -> q.AskPrice)
                    match highestBid.BidPrice, lowestAsk.AskPrice with
                    | bid, ask when bid > ask + 0.01m -> [(highestBid, lowestAsk)]
                    | _ -> []
                | _ -> [])

// Reduces arbitrage opportunities to a list of pairs and the number of opportunities.
let reducePhase (mappedData: (MarketData * MarketData) list) =
    mappedData
    |> List.map (fun (bid, ask) -> bid.Pair)
    |> List.groupBy id
    |> List.map (fun (pair, instances) -> { Pair = pair; NumberOfOpportunities = List.length instances })