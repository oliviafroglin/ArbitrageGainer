module ManagePnLThresholdCore

type ThresholdResult =
    | Ok of decimal
    | Error of string

type ThresholdMessage =
    | SetThreshold of decimal * AsyncReplyChannel<ThresholdResult>
    | GetThreshold of AsyncReplyChannel<ThresholdResult>

type PnLThreshold = {
    Value: decimal
}

type ThresholdValidation =
    | Valid of decimal
    | Invalid of string  // Also added 'of string' to carry specific error messages
