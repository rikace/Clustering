open System
open System.IO
open Nessos.FsPickler
open FSharp.Quotations.Evaluator

[<EntryPoint>]
let main argv = 
    
    let folderTarget = "../../../Temp"
    //let folderTarget = "C:\Temp\CodeQuotations"
  //  let folderTarget =Environment.CurrentDirectory + "..\\..\\..\\..\\CodeQuotations"
    
   // Environment.GetFolderPath(Environment.f)

    let serializer = FsPickler.CreateBinarySerializer()
    
    let u = serializer.Pickle<@ 1 + 121 @>
  //  let p = serializer.UnPickle<Quotations.Expr<int>> u

    //let r = QuotationEvaluator.Evaluate p

    use fs = new FileStream(folderTarget + "\\CQ.bin", FileMode.Create, FileAccess.Write, FileShare.ReadWrite)
    fs.Write(u, 0, u.Length)
    fs.Dispose()

    Console.ReadLine() |> ignore

    0 // return an integer exit code
