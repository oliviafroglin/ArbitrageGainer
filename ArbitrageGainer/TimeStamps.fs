module TimeStamps

open System
open ArbitrageModels

let timeAgent = MailboxProcessor.Start(fun inbox ->
    let rec loop startTime endTime logged=
        async {
            let! message = inbox.Receive()
            match message with
            | UpdateStartTime newStartTime ->
                return! loop newStartTime endTime logged
            | UpdateEndTime newEndTime ->
                return! loop startTime newEndTime logged
            | GetStartTime replyChannel ->
                replyChannel.Reply startTime
                return! loop startTime endTime logged
            | GetEndTime replyChannel ->
                replyChannel.Reply endTime
                return! loop startTime endTime logged
            | GetTimeDiff replyChannel ->
                replyChannel.Reply (endTime - startTime)
                return! loop startTime endTime true
            | GetLogged replyChannel ->
                replyChannel.Reply logged
                return! loop startTime endTime logged
        }
    loop DateTime.MinValue DateTime.MinValue false
)
