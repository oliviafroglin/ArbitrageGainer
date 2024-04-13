module ManagePnLThresholdCore

type ThresholdMessage =
    | SetThreshold of decimal
    | GetThreshold

type ThresholdResult =
    | Ok of decimal
    | Error of string

type PnLThreshold = {
    Value: decimal
}

type ThresholdValidation =
    | Valid of decimal
    | Invalid


