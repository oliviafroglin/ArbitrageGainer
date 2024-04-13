module ManagePnLThresholdService

open System
open FSharp.Control
open ManagePnLThresholdCore

let validateThreshold threshold =
    match threshold>0M with
    | true -> Valid threshold
    | false -> Invalid "Threshold must be positive"

type PnLThresholdAgent() =
    let agent = MailboxProcessor.Start(fun inbox ->
        let rec messageLoop currentThreshold = async {
            let! msg = inbox.Receive()
            match msg with
            | SetThreshold (newThreshold, replyChannel) ->
                match validateThreshold newThreshold with
                | Valid validThreshold ->
                    replyChannel.Reply(ThresholdResult.Ok validThreshold)
                    return! messageLoop validThreshold
                | Invalid ->
                    replyChannel.Reply(ThresholdResult.Error "Threshold must be greater than or equal to 0.")
                    return! messageLoop currentThreshold

            | GetThreshold replyChannel ->
                replyChannel.Reply(ThresholdResult.Ok currentThreshold)
                return! messageLoop currentThreshold
        }
        messageLoop 0M)  // Initial threshold

    member this.SetThreshold(threshold: decimal) =
        agent.PostAndAsyncReply(fun replyChannel -> SetThreshold(threshold, replyChannel))

    member this.GetThreshold() =
        agent.PostAndAsyncReply(fun replyChannel -> GetThreshold(replyChannel))