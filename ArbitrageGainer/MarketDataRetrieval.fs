module MarketDataRetrieval

// Define the Quote type parsed from the market data
type Quote = {
    CryptoPair: string
    BidPrice: decimal
    AskPrice: decimal
    Timestamp: int64
    ExchangeId: int
}

// List of mock historical crypto pairs
let historicalCryptoPairs = [
    ("BTC-USD", 30); ("CHZ-USD", 22); ("KNC-USD", 15);
    ("YFI-USD", 12); ("CRV-USD", 10); ("FTM-USD", 8);
    ("ZRX-USD", 5); ("BAT-USD", 4); ("ADA-USD", 2); ("SOL-USD", 1)
]

// List of mock crypto pairs that are cross-traded
let crossTradedCryptoPairs = [
    "BTC-USD"; "KNC-USD"; "YFI-USD"; "ZRX-USD"; "DOG-USD"; "SOL-USD"; "ADA-USD"
]

// mock user input
let numOfCryptoToSub = 5

// Select the top n crypto pairs to subscribe to
let selectCryptoPairsToSub (n: int): (string * int) list =
    let rec select pairs selected =
        match pairs, selected with
        | _, selected when List.length selected >= n -> List.take n selected |> List.rev
        | pair :: rest, selected when List.contains (fst pair) crossTradedCryptoPairs ->
            select rest (pair :: selected)
        | _ :: rest, selected -> select rest selected
        | [], _ -> List.rev selected
    select historicalCryptoPairs []

// Subscribe to the selected crypto pairs
let subToCryptoPairs (cryptoPairs: (string * int) list): unit = 
    printfn "Subscribed to: %A" cryptoPairs

// Check if the exchange is one of the relevant exchanges: Bitfinex, Bitstamp, Kraken
let isRelevantExchange (quote: Quote): bool =
    match quote.ExchangeId with
    | 2 | 3 | 4 -> true
    | _ -> false

// Process the incoming quote
let emitQuote (quote: Quote): unit =
    match isRelevantExchange quote with
    | true -> printfn "Emitting quote for %s from exchange ID %d" quote.CryptoPair quote.ExchangeId
    | false -> ()

// Example call to subscribe to the top n crypto pairs
let topNCryptoPairs = selectCryptoPairsToSub numOfCryptoToSub
subToCryptoPairs topNCryptoPairs

// mock quote from a relevant exchange
let mockRelevantQuote = {
    CryptoPair = "BTC-USD"
    BidPrice = 33052.79M
    AskPrice = 33073.19M
    Timestamp = 1610462411115L
    ExchangeId = 2
}

// mock quote from an irrelevant exchange
let mockIrrelevantQuote = {
    CryptoPair = "BTC-USD"
    BidPrice = 33052.79M
    AskPrice = 33073.19M
    Timestamp = 1610462411115L
    ExchangeId = 1
}

// example of processing an incoming quote
emitQuote mockRelevantQuote
emitQuote mockIrrelevantQuote