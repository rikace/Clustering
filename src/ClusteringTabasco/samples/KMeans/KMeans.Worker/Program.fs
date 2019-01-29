open System
open Akka.FSharp
open Piri

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
        maximum-payload-bytes = 30000000
        helios.tcp {
            maximum-frame-size = 30000000
            hostname = "localhost"
            port = 0
        }
    }
    cluster {
        seed-nodes = [ "akka.tcp://cluster@localhost:4500/" ]
    }
    tabasco {
        assembly-cache-path = "tmp/"
        known-assemblies = [ "Akka.FSharp", "Akka", "Akka.Remote", "Akka.Cluster" ]
    }
}
"""

let piri = PiriPlugin.Get system
let server = piri.Server

Console.ReadLine()