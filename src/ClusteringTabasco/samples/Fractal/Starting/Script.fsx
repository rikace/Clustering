open System
open System.IO
open System.Diagnostics

let workerFolderSource = __SOURCE_DIRECTORY__ + @"\..\Fractal.Worker\bin\Debug"


let rec getFiles path = seq { for file in Directory.GetFiles(path, "*.*") do yield file
                              for dir in Directory.GetDirectories(path, "*.*") do yield! getFiles dir}

let rec copyWorkerFiles source destination =
    if not <| System.IO.Directory.Exists(destination) then
        System.IO.Directory.CreateDirectory(destination) |> ignore
    let srcDir = new System.IO.DirectoryInfo(source)
    for file in srcDir.GetFiles() do
        let temppath = System.IO.Path.Combine(destination, file.Name)
        file.CopyTo(temppath, true) |> ignore

    for subdir in srcDir.GetDirectories() do
            let dstSubDir = System.IO.Path.Combine(destination, subdir.Name)
            copyWorkerFiles subdir.FullName dstSubDir

let masterPath =  __SOURCE_DIRECTORY__ + @"\..\Fractal.Master\bin\Debug\Fractal.Master.exe"
let start (exe:string) = System.Diagnostics.Process.Start(exe)

let createWorkers ``n workers`` =
    [1..``n workers``]
    |> List.map(fun i ->
        let workerFolderPath = sprintf @"%s\..\worker%d" __SOURCE_DIRECTORY__ i
        if System.IO.Directory.Exists(workerFolderPath) then
            System.IO.Directory.Delete(workerFolderPath, true) |> ignore
        copyWorkerFiles workerFolderSource workerFolderPath
        File.Copy(masterPath, Path.Combine(workerFolderPath, Path.GetFileName(masterPath)), true)
        System.IO.Path.Combine(workerFolderPath, "Fractal.Worker.exe"))


let workers = createWorkers 4


start masterPath

workers |> List.iteri(fun i w -> start w |> ignore)


