module IdentifyCrossTradedPairsInfra

open IdentifyCrossTradedPairsCore
open IdentifyCrossTradedPairsService

open System
open System.Net.Http
open FSharp.Data
open System.Threading.Tasks
open System.IO

let httpClient = new HttpClient()

let fetchCurrencyPairsFromBitfinex () =
    async {
        let url = "https://api-pub.bitfinex.com/v2/conf/pub:list:pair:exchange"
        let! response = httpClient.GetAsync(url) |> Async.AwaitTask
        printfn "Response: %A" response
        let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        let data = JsonValue.Parse(content)
        return data.[0].AsArray() |> Array.map (fun x -> 
            let pair = x.AsString()
            match pair.Contains(":") with
            | true -> pair.Split(':')
            | false ->
                let middleIndex = pair.Length / 2
                [| pair.[0..middleIndex-1]; pair[middleIndex..] |]
            ) |> Array.choose (fun arr -> 
                match arr.Length = 2 with 
                | true -> Some (arr.[0], arr.[1])
                | false -> None)
    }

let fetchCurrencyPairsFromBitstamp () =
    async {
        let url = "https://www.bitstamp.net/api/v2/ticker/"
        let! response = httpClient.GetAsync(url) |> Async.AwaitTask
        printfn "Response: %A" response
        let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        let data = JsonValue.Parse(content)
        return data.AsArray() |> Array.map (fun x -> x.["pair"].AsString().Replace("/", ":").Split(':')) |> Array.choose (fun arr -> 
                match arr.Length = 2 with 
                | true -> Some (arr.[0], arr.[1])
                | false -> None)
    }

let fetchCurrencyPairsFromKraken () =
    async {
        let url = "https://api.kraken.com/0/public/AssetPairs"
        let! response = httpClient.GetAsync(url) |> Async.AwaitTask
        printfn "Response: %A" response
        let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        let data = JsonValue.Parse(content)
        return data.["result"].Properties() |> Array.map (fun (_, v) -> (v.["base"].AsString(), v.["quote"].AsString()))
    }

let saveSetToFile filePath (currencyPairs : Set<CurrencyPair>) =
    let content = 
        currencyPairs
        |> Set.map (fun (b, quote) -> sprintf "%s, %s" b quote)
        |> Set.fold (fun acc pair -> acc + pair + "\n") ""
    File.WriteAllText(filePath, content)

let identifyCrossTradedPairs () =
    async {
        let! bitfinexPairs = fetchCurrencyPairsFromBitfinex ()
        let! bitstampPairs = fetchCurrencyPairsFromBitstamp ()
        let! krakenPairs = fetchCurrencyPairsFromKraken ()

        let bitfinexSet = Set.ofArray bitfinexPairs
        let bitstampSet = Set.ofArray bitstampPairs
        let krakenSet = Set.ofArray krakenPairs
        
        let bitfinexSet1 = processCurrencyPairs bitfinexSet
        let bitstampSet1 = processCurrencyPairs bitstampSet
        let krakenSet1 = processKrakenCurrencyPairs krakenSet

        let crossTradedPairs = IdentifyCrossTradedPairsService(bitfinexSet1, bitstampSet1, krakenSet1)

        saveSetToFile "./crossTradedPairs.txt" crossTradedPairs

        printfn "crossTradedPairs: %A" crossTradedPairs
        return crossTradedPairs
    }

// let printCrossTradedPairs () =
//     async {
//         let crossTradedPairs = identifyCrossTradedPairs () |> Async.RunSynchronously
//         crossTradedPairs |> Set.iter (fun (pair1, pair2) -> printfn "%s-%s" pair1 pair2)
//     }

// printCrossTradedPairs () |> Async.RunSynchronously
// identifyCrossTradedPairs () |> Async.RunSynchronously