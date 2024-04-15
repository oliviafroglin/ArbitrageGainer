module IdentifyCrossTradedPairsService

open IdentifyCrossTradedPairsCore

// Identifies the currency pairs that are traded on all three exchanges.
let IdentifyCrossTradedPairsService (exchange1, exchange2, exchange3) =
    let crossTradedPairs = exchange1 |> Set.intersect exchange2 |> Set.intersect exchange3
    crossTradedPairs
