module HistoricalDataAnalysisService

open HistoricalDataAnalysisCore

let identifyArbitrageOpportunities (data: MarketData list): ArbitrageOpportunity list =
    data
    |> List.groupBy (fun d -> d.Pair)
    |> List.collect (fun (pair, quotes) ->
        // printfn "Processing pair: %s" pair
        let result = mapPhase quotes
        // printfn "Mapped quotes: %A" result
        result)
    |> reducePhase
    |> (fun result ->
        // printfn "Reduced result: %A" result
        result)
    |> List.sortByDescending (fun o ->
        // printfn "Sorting opportunity: %A" o
        o.NumberOfOpportunities)