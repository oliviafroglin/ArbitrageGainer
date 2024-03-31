open Newtonsoft.Json
open System.IO

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

type ArbitrageOpportunity = {
    Pair: string
    NumberOfOpportunities: int
}

let readMarketDataFromFile (filePath: string): MarketData list =
    let json = File.ReadAllText(filePath)
    JsonConvert.DeserializeObject<MarketData list>(json)

let filePath = "data.txt"
let marketData = readMarketDataFromFile filePath

let identifyArbitrageOpportunities (data: MarketData list): ArbitrageOpportunity list =
    data
    // Group data by pair to ensure we're comparing the same currencies.
    |> List.groupBy (fun d -> d.Pair)
    |> List.collect (fun (pair, quotes) ->
        // Within each currency pair, group quotes into 5-ms buckets.
        quotes |> List.groupBy (fun d -> d.Timestamp / 5L)
               |> List.collect (fun (_, bucketQuotes) ->
                    // For each bucket, separate quotes by exchange.
                    let exchangeGroups = bucketQuotes |> List.groupBy (fun q -> q.Exchange)
                    if List.length exchangeGroups > 1 then
                        // Find highest bid and lowest ask for every exchange in this bucket.
                        let highestBid = bucketQuotes |> List.maxBy (fun q -> q.BidPrice)
                        let lowestAsk = bucketQuotes |> List.minBy (fun q -> q.AskPrice)
                        // Compare bid and ask prices between all exchanges and identify arbitrage opportunity.
                        if highestBid.BidPrice > lowestAsk.AskPrice + 0.01m then
                            // Return an opportunity for this pair and bucket.
                            [{ Pair = pair; NumberOfOpportunities = 1 }]
                        else
                            []
                    else
                        []))
    // Aggregate opportunities by pair and count.
    |> List.groupBy (fun o -> o.Pair)
    |> List.map (fun (pair, opportunities) ->
        { Pair = pair; NumberOfOpportunities = List.length opportunities })
    |> List.sortByDescending (fun o -> o.NumberOfOpportunities)

let printArbitrageOpportunities (opportunities: ArbitrageOpportunity list) =
    printfn "Arbitrage Opportunities:"
    opportunities
    |> List.iter (fun opportunity ->
        printfn "Pair: %s, Number of Opportunities: %d" opportunity.Pair opportunity.NumberOfOpportunities)

let arbitrageOpportunities = identifyArbitrageOpportunities marketData
printArbitrageOpportunities arbitrageOpportunities