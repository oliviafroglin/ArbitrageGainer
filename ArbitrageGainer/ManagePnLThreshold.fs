module PnLThresholdManagement

type PnLThreshold = {
    mutable Value:  decimal
}

let pnlThreshold = { Value = 0 }

type ThresholdUpdateError = 
    | InvalidThresholdValue of string

// Validate the threshold value
let validateThreshold (newThreshold: decimal) =
    match newThreshold >= 0M with
    | true -> Ok newThreshold
    | false -> Error (InvalidThresholdValue "Threshold must be greater than or equal to 0.")

// Function to update the threshold with error handling
let updateThreshold (newThreshold: decimal) =
    match validateThreshold newThreshold with
    | Ok validThreshold -> 
        pnlThreshold.Value <- validThreshold
        match validThreshold with
        | _ when validThreshold > 0M -> printfn "New P&L Threshold set to: %M" validThreshold
        | _ -> printfn "Threshold canceled. The system will continuously perform transactions."
        Ok ()
    | Error err -> 
        printfn "Error: %A" err
        Error err

// result is now of type Result<unit, ThresholdUpdateError>
