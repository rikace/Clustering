

open System
open System.IO
open Nessos.FsPickler
open FSharp.Quotations.Evaluator

[<EntryPoint>]
let main argv = 
    
    let folderTarget = "../../../Temp"
    let fileName = "CQ.bin"
    let fileTarget = Path.Combine(folderTarget, fileName)

    if  Directory.Exists folderTarget |> not then 
        Directory.CreateDirectory folderTarget |> ignore
    if File.Exists fileTarget then File.Delete fileTarget
    
    let fw = new FileSystemWatcher(folderTarget)
    let serializer = FsPickler.CreateBinarySerializer()
    
    let u = serializer.Pickle<@ 1 + 43 @>
    let p = serializer.UnPickle<Quotations.Expr<int>> u

    let r = QuotationEvaluator.Evaluate p
    let evt = 
        fw.Changed 
        |> Observable.add(fun f ->
            let bytes = File.ReadAllBytes f.FullPath
            let op = serializer.UnPickle<Quotations.Expr<int>> bytes
            let r = QuotationEvaluator.Evaluate op
            printfn "kk %d" r
        )

    fw.EnableRaisingEvents <- true

    printfn "Listening..." 

    Console.ReadLine() |> ignore

    0 // return an integer exit code
