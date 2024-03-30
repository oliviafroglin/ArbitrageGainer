module PnLThresholdManagement

type PnLThreshold = {
    mutable Value:  decimal
}

let pnlThreshold = { Value = 0 }

type ThresholdUpdateError = 
    | InvalidThresholdValue of string

// Validate the threshold value
let validateThreshold (newThreshold: decimal) =
    if newThreshold >= 0M then 
        Ok newThreshold
    else 
        Error (InvalidThresholdValue "Threshold must be greater than or equal to 0.")

// Function to update the threshold with error handling
let updateThreshold (newThreshold: decimal) =
    match validateThreshold newThreshold with
    | Ok validThreshold -> 
        pnlThreshold.Value <- Some validThreshold
        if validThreshold > 0M then
            printfn "New P&L Threshold set to: %M" validThreshold
        else
            printfn "Threshold canceled. The system will continuously perform transactions."
        Ok ()
    | Error err -> 
        printfn "Error: %A" err
        Error err


// result is now of type Result<unit, ThresholdUpdateError>
