open System
open Akka.FSharp
open Akka.Actor
open Akka.Cluster
open Piri
open FSharp.Charting

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
            port = 4500
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

let dim = 2 // point dimensions: we use 2 dimensions so we can chart the results
let numCentroids = 5 // The number of centroids to find
let partitions = 12 // The number of point partitions
let pointsPerPartition = 50000 // The number of points per partition
let epsilon = 0.1

(** Generate some random input data, a deterministic set of points based on the parameters above. *)


/// Represents a multi-dimensional point.
type Point = float[]
type Observation = DateTimeOffset*int*float*Point[]

/// Generates a set of points via a random walk from the origin, using provided seed.
let generatePoints dim numPoints seed : Point[] =
    let rand = Random(seed * 2003 + 22)
    let prev = Array.zeroCreate dim

    let nextPoint () =
        let arr = Array.zeroCreate dim
        for i = 0 to dim - 1 do 
            arr.[i] <- prev.[i] + rand.NextDouble() * 40.0 - 20.0
            prev.[i] <- arr.[i]
        arr

    [| for i in 1 .. numPoints -> nextPoint() |]

let randPoints = Array.init partitions (generatePoints dim pointsPerPartition)

(** Next you display a chart showing the first 500 points from each partition: *)

let point2d (p:Point) = p.[0], p.[1]

let selectionOfPoints = 
    [ for points in randPoints do 
         for i in 0 .. 100 .. points.Length-1 do
             yield point2d points.[i] ]

Chart.Point selectionOfPoints 

(** 
Giving ![Input to KMeans](../img/kmeans-input.png)
Now you define a set of helper functions and types related to points and finding centroids: 
*)
[<AutoOpen>]
module KMeansHelpers =

    /// Calculates the distance between two points.
    let dist (p1 : Point) (p2 : Point) = 
        Array.fold2 (fun acc e1 e2 -> acc + pown (e1 - e2) 2) 0.0 p1 p2

    /// Assigns a point to the correct centroid, and returns the index of that centroid.
    let private findCentroid (p: Point) (centroids: Point[]) : int =
        let mutable mini = 0
        let mutable min = Double.PositiveInfinity
        for i = 0 to centroids.Length - 1 do
            let dist = dist p centroids.[i]
            if dist < min then
                min <- dist
                mini <- i

        mini

    /// Given a set of points, calculates the number of points assigned to each centroid.
    let kmeans (points : Point[]) (centroids : Point[]) : (int * (int * Point))[] =
        let lens = Array.zeroCreate centroids.Length
        let sums = 
            Array.init centroids.Length (fun _ -> Array.zeroCreate centroids.[0].Length)

        for point in points do
            let cent = findCentroid point centroids
            lens.[cent] <- lens.[cent] + 1
            for i = 0 to point.Length - 1 do
                sums.[cent].[i] <- sums.[cent].[i] + point.[i]

        Array.init centroids.Length (fun i -> (i, (lens.[i], sums.[i])))

    /// Sums a collection of points
    let sumPoints (pointArr : Point []) dim : Point =
        let sum = Array.zeroCreate dim
        for p in pointArr do
            for i = 0 to dim - 1 do
                sum.[i] <- sum.[i] + p.[i]
        sum

    /// Scalar division of a point
    let divPoint (point : Point) (x : float) : Point =
        Array.map (fun p -> p / x) point

type KMeanMessage =
    | ComputePartition of partition:Point[] * centroids:Point[]
    | ComputedParts of (int * (int * Point))[] []
    | Iterate of int

let kmeans : Receive<_, _> =
    fun ctx () (ComputePartition(points, centroids)) ->
        let result = KMeansHelpers.kmeans points centroids
        ctx.Context.Sender <! result
 
let kmeansIterator (partitionedPoints, centroids, epsilon, workersRef, emit) (ctx: Actor<_>) =
    let rec iterate (partitionedPoints, centroids: Point []) = 
        actor {
            let! msg = ctx.Receive()
            match msg with
            | Iterate iteration ->
                 // Stage 1: map computations to each worker per point partition
                async {
                    let! grouped =
                        partitionedPoints
                        |> Array.map (fun partition -> async {
                            let! res = workersRef <? ComputePartition(partition, centroids)
                            return downcast res })
                        |> Async.Parallel 
                    return ComputedParts grouped
                } |!> ctx.Self
                return! reduce (partitionedPoints, centroids, iteration+1) }
    and reduce (partitionedPoints, centroids: Point [], iteration) = 
        actor {
            let! msg = ctx.Receive()
            match msg with
            | ComputedParts clusterParts ->                
                // Stage 2: reduce computations to obtain the new centroids
                let dim = centroids.[0].Length
                let newCentroids =
                    clusterParts
                    |> Array.concat
                    |> Array.groupBy fst
                    |> Array.sortBy fst
                    |> Array.map snd
                    |> Array.map (fun clp -> clp |> Seq.map snd |> Seq.toArray |> Array.unzip)
                    |> Array.map (fun (ns,points) -> Array.sum ns, sumPoints points dim)
                    |> Array.map (fun (n, sum) -> divPoint sum (float n))

                // Stage 3: check convergence and decide whether to continue iteration
                let diff = Array.map2 dist newCentroids centroids |> Array.max

                printfn "KMeans: iteration [#%d], diff %A with centroids /n%A" iteration diff centroids

                // emit an observation
                emit(DateTimeOffset.UtcNow,iteration,diff,centroids)

                if diff < epsilon then
                    ctx.Context.Sender <! newCentroids
                // reschedule next iteration
                ctx.Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds 1., ctx.Self, Iterate (iteration + 1))
                return! iterate (partitionedPoints, newCentroids) }
    iterate (partitionedPoints, centroids)

let initCentroids = randPoints |> Seq.concat |> Seq.take numCentroids |> Seq.toArray
let piri = PiriPlugin.Get system
let workersRef = piri.Client("k-means-worker", kmeans, ())
let centroidsSoFar = ResizeArray()
let mutable observed = []
let emit x = 
    let d = [ for centroids in centroidsSoFar do for p in centroids -> point2d p ]
    observed <- d
    
printfn "Enter to start..."
let iterRef = spawn system "main" (kmeansIterator (randPoints, initCentroids, epsilon, workersRef, emit))
iterRef <! Iterate 1

printfn "Enter to show results..."
Console.ReadLine()

Chart.Combine   
    [ Chart.Point(selectionOfPoints)
      Chart.Point(centroidsSoFar.[0] |> Array.map point2d, Color=Drawing.Color.Red) ]
      
printfn "Enter to close..."
Console.ReadLine()