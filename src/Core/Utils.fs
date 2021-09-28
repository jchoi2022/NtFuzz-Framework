module FuzzingFramework.Utils

open System.IO
open System.Threading
open FuzzingFramework.Paths

/// Copy directory directly, like 'cp -r' command of linux.
let rec copyDir srcDir dstDir =
  if not (Directory.Exists(dstDir)) then
    Directory.CreateDirectory(dstDir) |> ignore
  let srcFiles = Directory.GetFiles(srcDir)
  for srcFilePath in srcFiles do
    let fileName = Path.GetFileName(srcFilePath)
    File.Copy(srcFilePath, Path.Combine(dstDir, fileName))
  let recDirs = Directory.GetDirectories(srcDir)
  for recDirPath in recDirs do
    let recDirName = Path.GetFileName(recDirPath) // Not GetDirectoryName()
    copyDir recDirPath (Path.Combine (dstDir, recDirName))


let rec deleteFileWithRetry filePath n =
  try File.Delete filePath with
  | _ -> if n <= 0 then failwithf "Failed to delete %s, abort." filePath
         else printf "Exception while deleting %s, retry..." filePath
              Thread.Sleep(Literal.secondInMilliseconds)
              deleteFileWithRetry filePath (n - 1)

let assertDirPrepared path =
  if not (Directory.Exists(path)) then
    printfn "[*] %s should be prepared, first" path
    exit(1)

let assertFilePrepared path =
  if not (File.Exists(path)) then
    printfn "[*] %s should be prepared, first" path
    exit(1)

let checkTasksDirClear () =
  Directory.GetDirectories(HostPath.tasksRootDir).Length = 0