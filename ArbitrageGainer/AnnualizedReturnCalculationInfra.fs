module AnnualizedReturnCalculationInfra

open PnLCalculationCore
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.RequestErrors
open AnnualizedReturnCalculationCore
open System.Globalization
open System
open PnLCalculationService
open PnLCalculationCore

let connectionString = "Host=34.42.239.81;Database=orders;Username=sqlserver;Password=-*lUp54$JMRku5Ay;Trusted_Connection=True;"

let fetchTransactionsForDay (connectionString: string) (date: DateTime) : list<CompletedTransaction>=
    // Placeholder: implement actual database fetching logic here
    // This should return a list of CompletedTransaction
    // Example SQL: SELECT * FROM Transactions WHERE TransactionDate BETWEEN @startOfDay AND @endOfDay
    [
        { TransactionType = TransactionType.Buy; BuyPrice = 100.0M; SellPrice = 0.0M; Amount = 100.0M; TransactionDate = DateTime.Parse("2021-01-01") }
        { TransactionType = TransactionType.Sell; BuyPrice = 0.0M; SellPrice = 200.0M; Amount = 200.0M; TransactionDate = DateTime.Parse("2021-01-02") }
    ]


let annualizedReturnHandler (ctx: HttpContext) : Async<HttpContext option> =
    async {
        match ctx.request.queryParam "startDate" with
        | Choice1Of2 startDateStr ->
            let parseDate (dateStr: string) =
                match DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None) with
                | true, date -> Some date
                | _ -> None

            match parseDate startDateStr with
            | Some startDate ->
                let transactions = fetchTransactionsForDay connectionString startDate  // Correctly uses DateTime
                let initialInvestment = calculateInitialInvestment transactions
                let endDate = DateTime.Now
                let dateRange = { StartDate = startDate; EndDate = endDate }
                let totalReturn = calculateHistoricalPnL transactions dateRange
                let durationYears = (endDate - startDate).TotalDays / 365.25

                match totalReturn with
                | Ok profitOrLoss ->
                    let totalReturnAmount = 
                        match profitOrLoss with
                        | Profit p -> p
                        | Loss l -> -l
                    let investmentDetails = {
                        StartDate = startDate; 
                        InitialInvestment = initialInvestment; 
                        TotalReturn = totalReturnAmount; 
                        DurationYears = durationYears 
                    }
                    let annualizedReturn = calculateAnnualizedReturn investmentDetails
                    return! OK (sprintf "{\"Annualized Return\": %f}" annualizedReturn) ctx
                | Error msg -> return! BAD_REQUEST (sprintf "Error calculating PnL: %s" msg) ctx
            | None -> return! BAD_REQUEST "Invalid startDate format. Please use YYYY-MM-DD." ctx
        | Choice2Of2 errMsg -> return! BAD_REQUEST (sprintf "Error retrieving 'startDate' parameter: %s" errMsg) ctx
    }



