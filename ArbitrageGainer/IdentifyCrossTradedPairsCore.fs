module IdentifyCrossTradedPairsCore

type CurrencyPair = string * string

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