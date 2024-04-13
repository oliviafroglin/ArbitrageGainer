module HistoricalDataAnalysisService

open HistoricalDataAnalysisCore

let identifyArbitrageOpportunities (data: MarketData list) =
    data
    |> mapPhaseWithoutGroupByPair
    |> reducePhaseWithoutGroupByPair
    |> List.sortByDescending snd