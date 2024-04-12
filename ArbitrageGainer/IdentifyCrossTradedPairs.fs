open System
open System.Net.Http
open FSharp.Data
open System.Threading.Tasks
open System.IO

type CurrencyPair = string * string

let httpClient = new HttpClient()

// Asynchronous function to fetch data from Bitfinex
let fetchCurrencyPairsFromBitfinex () =
    async {
        let url = "https://api-pub.bitfinex.com/v2/conf/pub:list:pair:exchange"
        let! response = httpClient.GetAsync(url) |> Async.AwaitTask
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

let normalizeCurrency currency =
    match currency with
    | "UST" | "USDT" | "USDC" -> "USD"
    | "EURT" -> "EUR"
    | "CNHT" | "MXNT" | "TESTUSD" | "TESTUSDT" -> ""
    | curr when curr.StartsWith("TEST") -> ""
    | _ -> currency

let cleanPair (base_, quote) =
    let normalizedBase = normalizeCurrency base_
    let normalizedQuote = normalizeCurrency quote
    match normalizedBase, normalizedQuote with
    | "", _ -> None
    | _, "" -> None
    | _ -> Some (normalizedBase, normalizedQuote)

let processCurrencyPairs pairs =
    pairs
    |> Seq.map cleanPair
    |> Seq.choose id
    |> Set.ofSeq

let normalizeKrakenCurrency currency =
    match currency with
    | "UST" | "USDT" | "USDC" -> "USD"
    | "EURT" -> "EUR"
    | curr when (curr.StartsWith("X") || curr.StartsWith("Z") || curr.StartsWith("A")) && curr.Length > 3 -> curr.[1..]
    | _ -> currency

let cleanKrakenPair (base_, quote) =
    let normalizedBase = normalizeKrakenCurrency base_
    let normalizedQuote = normalizeKrakenCurrency quote
    match normalizedBase, normalizedQuote with
    | "", _ -> None
    | _, "" -> None
    | _ -> Some (normalizedBase, normalizedQuote)

let processKrakenCurrencyPairs pairs =
    pairs
    |> Seq.map cleanKrakenPair
    |> Seq.choose id 
    |> Set.ofSeq

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

        let crossTradedPairs = bitfinexSet1 |> Set.intersect bitstampSet1 |> Set.intersect krakenSet1

        printfn "bitfinexSet1: %A" bitfinexSet1
        printfn "bitstampSet1: %A" bitstampSet1
        printfn "krakenSet1: %A" krakenSet1

        saveSetToFile "./bitfinexPairs.txt" bitfinexSet1
        saveSetToFile "./bitstampPairs.txt" bitstampSet1
        saveSetToFile "./krakenPairs.txt" krakenSet1

        return crossTradedPairs
    }

// Execute the async function and print the results
[<EntryPoint>]
let main argv =
    let task = identifyCrossTradedPairs () |> Async.StartAsTask
    task.Result |> Set.iter (fun (pair1, pair2) -> printfn "%s-%s" pair1 pair2)
    0 // return an integer exit code
