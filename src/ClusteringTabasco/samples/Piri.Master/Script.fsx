open System
open System.IO
open System.Diagnostics

let masterPath =  __SOURCE_DIRECTORY__ + @"\bin\Debug\Piri.Master.exe"
let workerPath =  __SOURCE_DIRECTORY__ + @"\..\Piri.Worker\bin\Debug\Piri.Worker.exe"
let start (exe:string) = System.Diagnostics.Process.Start(exe)

start masterPath
start workerPath
