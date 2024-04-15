## Link to repo: https://github.com/oliviafroglin/FunctionalProgramming/tree/main

## 1. Workflows
#### 1. Historical spread calculation algorithm

Path: [Core](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/HistoricalDataAnalysisCore.fs), [Service](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/HistoricalDataAnalysisService.fs), [Infra](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/HistoricalDataAnalysisInfra.fs) \
Test: [HistoricalDataAnalysisTests.fs](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer.Tests/HistoricalDataAnalysisTests.fs)

#### 2. Identify Cross Traded Pairs

Path: [Core](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/IdentifyCrossTradedPairsCore.fs), [Service](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/IdentifyCrossTradedPairsService.fs), [Infra](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/IdentifyCrossTradedPairsInfra.fs)


#### 3. MarketDataRetrieval

Path: [Core](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/MarketDataRetrieval.fs), [Service](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/ArbitrageService.fs), [Infra](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/ArbitrageInfra.fs)

#### 4. ArbitrageOpportunityIdentifier

Path: [Core](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/ArbitrageOpportunityIdentifier.fs), [Service](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/ArbitrageService.fs), [Infra](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/ArbitrageInfra.fs)

#### 5. Order Execution

Path: [Infra](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/OrderExecutionInfra.fs)

#### 6. P & L Calculation

Path: [Core](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/PnLCalculationCore.fs), [Service](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/PnLCalculationService.fs), [Infra](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/PnLCalculationInfra.fs) \
Test: [PnLCalculationTests.fs](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer.Tests/PnLCalculationTests.fs)

#### 7. P & L Threshold Management

Path: [Core](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/ManagePnLThresholdCore.fs), [Service](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/ManagePnLThresholdService.fs), [Infra](hhttps://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/ManagePnLThresholdInfra.fs) 
Test: [ManagePnLThresholdTests.fs](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer.Tests/ManagePnLThresholdTests.fs)

#### 8. Annualized Return (on demand)

Path: [Core](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/AnnualizedReturnCalculationCore.fs), [Infra](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/AnnualizedReturnCalculationInfra.fs) \
Test: [AnnualizedReturnCalculationTests.fs](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer.Tests/AnnualizedReturnCalculationTests.fs)


## 2. Side Effects

1. **Calculate historical spread (arbitrage opportunities)** \
ArbitrageGainer/HistoricalDataAnalysisCore.fs: defines F# modules and types for historical market data analysis, including functions for mapping and reducing market data to identify arbitrage opportunities.

    ArbitrageGainer/HistoricalDataAnalysisService.fs: defines a function identifyArbitrageOpportunities for analyzing market data and identifying arbitrage opportunities.

    ArbitrageGainer/HistoricalDataAnalysisInfra.fs: contains infrastructure code for historical data analysis, including database initialization, data saving, reading from files, and printing and writing arbitrage opportunities.

    Test file: ArbitrageGainer.Tests/HistoricalDataAnalysisTests.fs

2. **Identify Cross Traded Pairs** \
    ArbitrageGainer/IdentifyCrossTradedPairsCore.fs: contains functions for normalizing and cleaning currency pairs, specifically for identifying cross-traded pairs.

    ArbitrageGainer/IdentifyCrossTradedPairsService.fs: contains a module called IdentifyCrossTradedPairsService that defines a function to identify cross-traded pairs between three exchanges.

    ArbitrageGainer/IdentifyCrossTradedPairsInfra.fs: contains F# code that fetches currency pairs from different exchanges, identifies cross-traded pairs, and saves them to a file and a MySQL database.

3. **Order Execution** \
    File:OrderExecutionInfra.fs: 

    sendEmail: Sends an email via SMTP and logs success or failure.
    mockOrderResponse: Simulates an order response, either as fully or partially filled.
    insertCompletedTransaction: Inserts transaction details into a database and logs the outcome.
    updateEmail: Updates the email address based on HTTP request parameters and provides response via an HTTP context.

4. **Market Data Retrieval** \
    fetchCrossAndHistPairs: Fetch crypto currency information from cloud database
    connectToWebSocket: connect to web socket
    receiveData: receive inputs from web socket
    sendJsonMessage: send messages to Polygon API
    update-config: receive user inputs of trading parameters
    startTrading: receive user input to initiate socket and the trading process
    stopTrading: receive user input to stop the trading process

    [ArbitrageInfra](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/ArbitrageInfra.fs)

5. **P & L Calculation** \
[Infra](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/PnLCalculationInfra.fs) \
Database Access: The function [fetchTransactions](https://github.com/oliviafroglin/FunctionalProgramming/blob/ca4d0ab69357a004a12a4da22510973659dd2ed9/ArbitrageGainer/PnLCalculationInfra.fs#L18) interacts with MySQL database. This interaction includes connecting to the database, executing a query, and reading results. \
HTTP Context Manipulations: The [pnlHandler](https://github.com/oliviafroglin/FunctionalProgramming/blob/ca4d0ab69357a004a12a4da22510973659dd2ed9/ArbitrageGainer/PnLCalculationInfra.fs#L55) function modifies the HTTP context by setting response status codes and bodies based on the outcome of its operations.

6. **P & L Threshold Management** 
* [Service](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/ManagePnLThresholdService.fs) \
State Changes through MailboxProcessor: The PnLThresholdAgent [(here)](https://github.com/oliviafroglin/FunctionalProgramming/blob/ca4d0ab69357a004a12a4da22510973659dd2ed9/ArbitrageGainer/ManagePnLThresholdService.fs#L12) uses a MailboxProcessor to maintain and update the threshold state asynchronously. This component changes state internally and affects the external behavior based on the threshold values it manages. \
Asynchronous Responses: When setting or getting thresholds[(here)](https://github.com/oliviafroglin/FunctionalProgramming/blob/ca4d0ab69357a004a12a4da22510973659dd2ed9/ArbitrageGainer/ManagePnLThresholdService.fs#L32), the agent communicates asynchronously using `AsyncReplyChannel`, impacting the system's state asynchronously.

* [Infra](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/ManagePnLThresholdInfra.fs) \
HTTP Request Handling [(here)](https://github.com/oliviafroglin/FunctionalProgramming/blob/ca4d0ab69357a004a12a4da22510973659dd2ed9/ArbitrageGainer/ManagePnLThresholdInfra.fs#L30): processes HTTP requests and modifies the HTTP response based on the operations performed, such as updating or retrieving the threshold.

7. **Annualized Return Calculation** \
[Infra](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/AnnualizedReturnCalculationInfra.fs) \
Database Access: The [fetchTransactionsForDay](https://github.com/oliviafroglin/FunctionalProgramming/blob/ca4d0ab69357a004a12a4da22510973659dd2ed9/ArbitrageGainer/AnnualizedReturnCalculationInfra.fs#L15) function connects to a database and reads data. \
HTTP Response Handling: The [annualizedReturnHandler](https://github.com/oliviafroglin/FunctionalProgramming/blob/ca4d0ab69357a004a12a4da22510973659dd2ed9/ArbitrageGainer/AnnualizedReturnCalculationInfra.fs#L52) alters HTTP responses based on the computation outcomes and input validation, directly affecting the HTTP state transmitted to the client.

## 3. Error Handling
1. **Market Data Retrieval** \
* [Service](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/ArbitrageService.fs) \
    tryParseQuote \
    processQuotes \
* [ArbitrageInfra](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/ArbitrageInfra.fs) \
    fetchCrossAndHistPairs \
    

3. **Order Execution** \
    File:OrderExecutionInfra.fs: 

    executeTransaction - Manages the transaction execution process, handles responses, and updates profit or notifies of failure, encapsulating outcomes in a Result.

4. **P & L Calculation** 
* [Infra](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/PnLCalculationInfra.fs) \
Date Parsing and Validation [(here)](https://github.com/oliviafroglin/FunctionalProgramming/blob/ca4d0ab69357a004a12a4da22510973659dd2ed9/ArbitrageGainer/PnLCalculationInfra.fs#L80): Errors in date parsing or invalid date ranges result in immediate HTTP 400 responses. These are handled explicitly, ensuring users are informed of input errors.
Generic Exception Handling [(here)](https://github.com/oliviafroglin/FunctionalProgramming/blob/9b658c91e2f830d74ec467011e9adc3e75459838/ArbitrageGainer/PnLCalculationInfra.fs#L51): This approach ensures that no type of exception goes unhandled, providing a catch-all safety net for any runtime issues that might occur during database operations.
* [Service](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/PnLCalculationService.fs) \
Calculate PnL Functions [(here)](https://github.com/oliviafroglin/FunctionalProgramming/blob/ca4d0ab69357a004a12a4da22510973659dd2ed9/ArbitrageGainer/PnLCalculationService.fs#L11): These functions handle errors in transaction data processing by returning structured results (Profit or Loss), encapsulating potential errors in business logic (e.g., incorrect calculations based on transaction types).

4. **P & L Threshold Management** 
* [Service](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/ManagePnLThresholdService.fs) \
Error Reporting [(here)](https://github.com/oliviafroglin/FunctionalProgramming/blob/ca4d0ab69357a004a12a4da22510973659dd2ed9/ArbitrageGainer/ManagePnLThresholdService.fs#L22): When a threshold update fails validation, an error message is passed back through the `MailboxProcessor`, which is then communicated to the caller via the service's public methods.\
Validation of Threshold Values: The [validateThreshold](https://github.com/oliviafroglin/FunctionalProgramming/blob/ca4d0ab69357a004a12a4da22510973659dd2ed9/ArbitrageGainer/ManagePnLThresholdService.fs#L7) function explicitly handles the validation of threshold inputs, returning either Valid or Invalid based on whether the threshold is positive.

* [Infra](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/ManagePnLThresholdInfra.fs) \
HTTP Error Responses [(here)](https://github.com/oliviafroglin/FunctionalProgramming/blob/ca4d0ab69357a004a12a4da22510973659dd2ed9/ArbitrageGainer/ManagePnLThresholdInfra.fs#L34): directly handles parsing errors (e.g., when a query parameter is missing or invalid) and returns appropriate HTTP status codes (400 Bad Request). \
Delegating Business Logic Errors [(here)](https://github.com/oliviafroglin/FunctionalProgramming/blob/ca4d0ab69357a004a12a4da22510973659dd2ed9/ArbitrageGainer/ManagePnLThresholdInfra.fs#L32): It handles errors related to business logic (e.g., setting an invalid threshold) by delegating to the service layer and then converting returned errors into HTTP responses.


5. **Annualized Return Calculation**
* [Core](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/AnnualizedReturnCalculationCore.fs) \
Divide by Zero: The [calculateAnnualizedReturn](https://github.com/oliviafroglin/FunctionalProgramming/blob/ca4d0ab69357a004a12a4da22510973659dd2ed9/ArbitrageGainer/AnnualizedReturnCalculationCore.fs#L23) function raises an exception if DurationYears is zero, which is critical to avoid mathematical errors in calculation.

* [Infra](https://github.com/oliviafroglin/FunctionalProgramming/blob/main/ArbitrageGainer/AnnualizedReturnCalculationInfra.fs) \
Input Validation: The [handler function](https://github.com/oliviafroglin/FunctionalProgramming/blob/ca4d0ab69357a004a12a4da22510973659dd2ed9/ArbitrageGainer/AnnualizedReturnCalculationInfra.fs#L65) validates the input date format and checks if the start date is in the future, responding with an HTTP 400 (Bad Request) if there are any issues.
Generic Exception Handling [(here)](https://github.com/oliviafroglin/FunctionalProgramming/blob/9b658c91e2f830d74ec467011e9adc3e75459838/ArbitrageGainer/AnnualizedReturnCalculationInfra.fs#L53): employ a generic exception handler that catches all exceptions (including database connection error). 

