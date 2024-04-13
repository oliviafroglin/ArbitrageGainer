module PnLThresholdManagement

open System

type ThresholdMessage =
    | SetThreshold of decimal * AsyncReplyChannel<Result<decimal, string>>
    | GetThreshold of AsyncReplyChannel<decimal>

type PnLThresholdAgent() =
    let agent = MailboxProcessor.Start(fun inbox ->
        let rec messageLoop currentThreshold = async {
            let! msg = inbox.Receive()
            match msg with
            | SetThreshold(newThreshold, reply) ->
                if newThreshold >= 0M then
                    reply.Reply(Ok newThreshold)
                    return! messageLoop newThreshold
                else
                    reply.Reply(Error "Threshold must be greater than or equal to 0.")
                    return! messageLoop currentThreshold

            | GetThreshold(reply) ->
                reply.Reply(currentThreshold)
                return! messageLoop currentThreshold
        }
        messageLoop 0M) // Initial threshold

    member this.SetThreshold(threshold) =
        agent.PostAndAsyncReply(fun reply -> SetThreshold(threshold, reply))

    member this.GetThreshold() =
        agent.PostAndAsyncReply(fun reply -> GetThreshold(reply))
