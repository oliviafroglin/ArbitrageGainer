module IdentifyCrossTradedPairsCore

// Represents a currency pair.
type CurrencyPair = string * string

// Normalizes a currency by mapping it to a standard representation.
// Returns the normalized currency.
let normalizeCurrency currency =
    match currency with
    | "UST" | "USDT" | "USDC" -> "USD"
    | "EURT" -> "EUR"
    | "CNHT" | "MXNT" | "TESTUSD" | "TESTUSDT" -> ""
    | curr when curr.StartsWith("TEST") -> ""
    | _ -> currency

// Cleans a currency pair by normalizing the base and quote currencies.
// Returns Some (normalizedBase, normalizedQuote) if both currencies are non-empty, otherwise None.
let cleanPair (base_, quote) =
    let normalizedBase = normalizeCurrency base_
    let normalizedQuote = normalizeCurrency quote
    match normalizedBase, normalizedQuote with
    | "", _ -> None
    | _, "" -> None
    | _ -> Some (normalizedBase, normalizedQuote)

// Processes a sequence of currency pairs by cleaning each pair and returning a set of unique pairs.
let processCurrencyPairs pairs =
    pairs
    |> Seq.map cleanPair
    |> Seq.choose id
    |> Set.ofSeq

// Normalizes a Kraken currency by mapping it to a standard representation.
// Returns the normalized currency.
let normalizeKrakenCurrency currency =
    match currency with
    | "UST" | "USDT" | "USDC" -> "USD"
    | "EURT" -> "EUR"
    | curr when (curr.StartsWith("X") || curr.StartsWith("Z") || curr.StartsWith("A")) && curr.Length > 3 -> curr.[1..]
    | _ -> currency

// Cleans a Kraken currency pair by normalizing the base and quote currencies.
// Returns Some (normalizedBase, normalizedQuote) if both currencies are non-empty, otherwise None.
let cleanKrakenPair (base_, quote) =
    let normalizedBase = normalizeKrakenCurrency base_
    let normalizedQuote = normalizeKrakenCurrency quote
    match normalizedBase, normalizedQuote with
    | "", _ -> None
    | _, "" -> None
    | _ -> Some (normalizedBase, normalizedQuote)

// Processes a sequence of Kraken currency pairs by cleaning each pair and returning a set of unique pairs.
let processKrakenCurrencyPairs pairs =
    pairs
    |> Seq.map cleanKrakenPair
    |> Seq.choose id 
    |> Set.ofSeq