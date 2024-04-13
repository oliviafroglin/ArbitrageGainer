module PnLThresholdManagement

type PnLThreshold = {
    Value: decimal
}

type ThresholdUpdateError = 
    | InvalidThresholdValue of string

let validateThreshold (newThreshold: decimal) =
    match newThreshold >= 0M with
    | true -> Ok newThreshold
    | false -> Error (InvalidThresholdValue "Threshold must be greater than or equal to 0.")

let updateThreshold (currentThreshold: PnLThreshold) (newThreshold: decimal) =
    match validateThreshold newThreshold with
    | Ok validThreshold -> 
        let updatedThreshold = { Value = validThreshold }
        match validThreshold with
        | _ when validThreshold > 0M -> printfn "New P&L Threshold set to: %M" validThreshold
        | _ -> printfn "Threshold canceled. The system will continuously perform transactions."
        Ok updatedThreshold
    | Error err -> 
        printfn "Error: %A" err
        Error err

