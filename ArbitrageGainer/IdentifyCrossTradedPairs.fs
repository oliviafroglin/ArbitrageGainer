module IdentifyCrossTradedPairs

open System
open System.Collections.Generic

type CurrencyPair = string * string

let getCurrencyPairs () =
    // Fake API call to Bitfinex
    let bitfinexPairs = [("BTC", "USD"); ("ETH", "USD"); ("BTC", "ETH")]

    // Fake API call to Bitstamp
    let bitstampPairs = [("BTC", "USD"); ("ETH", "USD"); ("LTC", "USD")]

    // Fake API call to Kraken
    let krakenPairs = [("BTC", "USD"); ("ETH", "USD"); ("XRP", "USD")]

    // Create a set of currency pairs for each exchange
    let bitfinexSet = Set.ofList bitfinexPairs
    let bitstampSet = Set.ofList bitstampPairs
    let krakenSet = Set.ofList krakenPairs

    // Find the intersection of all sets to get the cross-traded currency pairs
    let crossTradedPairs = bitfinexSet |> Set.intersect bitstampSet |> Set.intersect krakenSet

    // Filter out pairs with more than 3 letters in either currency
    let filteredPairs = crossTradedPairs |> Set.filter (fun (pair1, pair2) -> pair1.Length <= 3 && pair2.Length <= 3)

    // Return the filtered pairs as a list
    filteredPairs |> Set.toList

let main () =
    let crossTradedPairs = getCurrencyPairs ()
    crossTradedPairs |> List.iter (fun (pair1, pair2) -> printfn "%s-%s" pair1 pair2)

main ()