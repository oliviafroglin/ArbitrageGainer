module HistoricalDataAnalysisCore

open Newtonsoft.Json

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

let mapPhaseWithoutGroupByPair (quotes: MarketData list) =
    let earliestTimestamp = quotes |> List.minBy (fun q -> q.Timestamp) |> fun q -> q.Timestamp
    quotes
    |> List.groupBy (fun d -> (d.Timestamp - earliestTimestamp) / 5L)
    |> List.collect (fun (_, bucketQuotes) ->
        let exchangeGroups = bucketQuotes |> List.groupBy (fun q -> q.Exchange)
        exchangeGroups |> List.collect (fun (exchange1, quotes1) ->
            exchangeGroups |> List.collect (fun (exchange2, quotes2) ->
                if exchange1 <> exchange2 then
                    let highestBid1 = quotes1 |> List.maxBy (fun q -> q.BidPrice)
                    let lowestAsk1 = quotes1 |> List.minBy (fun q -> q.AskPrice)
                    let highestBid2 = quotes2 |> List.maxBy (fun q -> q.BidPrice)
                    let lowestAsk2 = quotes2 |> List.minBy (fun q -> q.AskPrice)
                    let opportunities = []
                    let bid1ToAsk2 = if highestBid1.BidPrice > lowestAsk2.AskPrice + 0.01m then [(highestBid1.Pair, 1)] else []
                    let bid2ToAsk1 = if highestBid2.BidPrice > lowestAsk1.AskPrice + 0.01m then [(highestBid2.Pair, 1)] else []
                    opportunities @ bid1ToAsk2 @ bid2ToAsk1
                else []
                )))

let reducePhaseWithoutGroupByPair (mappedData: (string * int) list) =
    mappedData
    |> List.groupBy fst
    |> List.map (fun (pair, instances) -> (pair, List.sumBy snd instances))