module IdentifyCrossTradedPairsService

open IdentifyCrossTradedPairsCore

let IdentifyCrossTradedPairsService (exchange1, exchange2, exchange3) =
    let crossTradedPairs = exchange1 |> Set.intersect exchange2 |> Set.intersect exchange3
    crossTradedPairs
