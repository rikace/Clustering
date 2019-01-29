open System
open Akka.FSharp
open Akka.Actor
open Akka.Cluster
open Piri
open MandelbrotSet

let system = System.create "cluster" <| Configuration.parse """
akka {
    #loglevel = "DEBUG"
    actor {
        provider = "Akka.Cluster.ClusterActorRefProvider, Akka.Cluster"
        serializers {
          wire = "Akka.Serialization.WireSerializer, Akka.Serialization.Wire"
        }
        serialization-bindings {
          "System.Object" = wire
        }
    }
    remote {
        helios.tcp {
            hostname = "localhost"
            port = 8091
        }
    }
    cluster {
        seed-nodes = [ "akka.tcp://cluster@localhost:8091/" ]
    }
    tabasco {
        assembly-cache-path = "tmp/"
        known-assemblies = [ "Akka.FSharp", "Akka", "Akka.Remote", "Akka.Cluster" ]
    }
}
"""

type State =
    { Counter: int }
    static member Zero = { Counter = 0 }

// code to be loaded as version 1.0
let increment : Receive<State, int> =
    fun ctx state msg ->
        let newState = { state with Counter = state.Counter + msg }
        Console.ForegroundColor <-ConsoleColor.Yellow
        printfn "New state of actor %s incremented to: %A" ctx.Context.Self.Path.Name newState
        newState

// code to be reloaded as version 2.0
let decrement : Receive<State, int> =
    fun ctx state msg ->
        let newState = { state with Counter = state.Counter - msg }
        Console.ForegroundColor <-ConsoleColor.Magenta
        printfn "New state of actor %s decremented to: %A" ctx.Context.Self.Path.Name newState
        let sender = ctx.Context.Sender
        newState

// all of this functionality is solved as a plugin for akka
let plugin = PiriPlugin.Get system

// activate a new actor on every cluster node (with tolerance to nodes churn)
// load code - v1.0
let ref = plugin.Client("stateful-actor", increment, State.Zero, Akka.Routing.BroadcastRoutingLogic(), 4)
printfn "Press Enter to start..."
Console.ReadLine()


[1..10] |> List.iter (fun i -> ref <! 1 )
                               //ref.Ask


Console.ReadLine()
// reload code - v2.0
ref <! Client.reload decrement
[1..10] |> List.iter (fun i -> ref <! 1)

Console.ReadLine()