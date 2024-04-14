module MarketDataRetrieval

open ArbitrageModels

// Select the n crypto pairs to subscribe to based on the historical pairs and cross-traded pairs
let selectCryptoPairsToSub (n: int) (historicalPairs: (string * int) list) (crossTradedPairs: string list): (string * int) list =
    let rec select pairs selected =
        match pairs, selected with
        // Return the selected pairs when the number of selected pairs is equal to n
        | _, selected when List.length selected >= n -> List.take n selected |> List.rev
        // Select the next pair when the pair is in the cross-traded pairs list with the pair added to the selected pairs list
        | pair :: rest, selected when List.contains (fst pair) crossTradedPairs ->
            select rest (pair :: selected)
        // Select the next pair when the pair is not in the selected pairs list
        | _ :: rest, selected -> select rest selected
        | [], _ -> List.rev selected
    select historicalPairs []

let isRelevantExchange (quote: Quote): bool =
    match quote.ExchangeId with
    | 2 | 6 | 23 -> true
    | _ -> false

let updateMarketDataCache (cache: (string * int * Quote) list) (quote: Quote): (string * int * Quote) list =
    let cacheWithoutOldQuote = cache |> List.filter (fun (cryptoPair, exchangeId, _) -> not (cryptoPair = quote.CryptoPair && exchangeId = quote.ExchangeId))
    (quote.CryptoPair, quote.ExchangeId, quote) :: cacheWithoutOldQuote
