module FuzzingFramework.VirtualMachine

type WorkerID = string

open FuzzingFramework.Paths
open System.Diagnostics
open System.IO
open System.Threading
open System.Runtime.InteropServices

let decideWorkerID workerNum : WorkerID =
  sprintf "worker-%d" workerNum

let private runScriptWithWorkerID script args =
  use proc = new Process ()
  proc.StartInfo.FileName <- script
  proc.StartInfo.Arguments <- args
  proc.StartInfo.CreateNoWindow <- true
  if proc.Start ()
  then proc.WaitForExit ()
  else printf "[FATAL] Error while running VM script (%s, %s)" script args
  proc.ExitCode

let private getScriptPath scriptName =
  let scriptExtension =
    if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then ".bat"
    elif RuntimeInformation.IsOSPlatform(OSPlatform.Linux) then ".sh"
    else failwith "Unsupported platform"
  let scriptFile = scriptName + scriptExtension
  Path.Combine (HostPath.scriptDir, scriptFile)

let clone workerID vmBaseDir =
  printfn "[*] Clone VM for %s and wait for a while" workerID
  let script = getScriptPath @"clone_vm"
  let args = sprintf "%s %s" workerID vmBaseDir
  runScriptWithWorkerID script args |> ignore
  Thread.Sleep(Literal.vmSyncWaitInMilliseconds) // To avoid sync issue.

let configure workerID nCore mem =
  printfn "[*] Configure VM for %s with %d cores & %d MB RAM" workerID nCore mem
  let script = getScriptPath @"configure_vm"
  let args = sprintf "%s %d %d" workerID nCore mem
  runScriptWithWorkerID script args |> ignore
  Thread.Sleep(Literal.vmSyncWaitInMilliseconds) // To avoid sync issue.

let delete workerID =
  printfn "[*] Delete VM of %s" workerID
  let script = getScriptPath @"delete_vm"
  runScriptWithWorkerID script workerID |> ignore

let takeSnapshot workerID =
  printfn "[*] Take snapshot of %s and wait for a while" workerID
  let script = getScriptPath @"take_snapshot"
  runScriptWithWorkerID script workerID |> ignore
  Thread.Sleep(Literal.vmSyncWaitInMilliseconds) // To avoid sync issue.

let restoreSnapshot workerID =
  printfn "[*] Restore snapshot of %s and wait for a while" workerID
  let script = getScriptPath @"restore_snapshot"
  runScriptWithWorkerID script workerID |> ignore
  Thread.Sleep(Literal.vmSyncWaitInMilliseconds) // To avoid sync issue.

let deleteSnapshot workerID =
  printfn "[*] Delete snapshot of %s and wait for a while" workerID
  let script = getScriptPath @"delete_snapshot"
  runScriptWithWorkerID script workerID |> ignore
  Thread.Sleep(Literal.vmSyncWaitInMilliseconds) // To avoid sync issue.

let start workerID =
  printfn "[*] Start %s and wait for a while" workerID
  let script = getScriptPath @"start_vm"
  runScriptWithWorkerID script workerID |> ignore
  Thread.Sleep(Literal.vmSyncWaitInMilliseconds) // To avoid sync issue.

let startAndCheck workerID =
  printfn "[*] Start %s and wait for a while" workerID
  let script = getScriptPath @"start_check_vm"
  runScriptWithWorkerID script workerID |> ignore
  Thread.Sleep(Literal.vmSyncWaitInMilliseconds) // To avoid sync issue.
  printfn "[*] Check if start command succeeded"
  Signal.checkStarted workerID

let stop workerID =
  printfn "[*] Stop %s and wait for a while" workerID
  let script = getScriptPath @"stop_vm"
  runScriptWithWorkerID script workerID |> ignore
  Thread.Sleep(Literal.vmSyncWaitInMilliseconds) // To avoid sync issue.

let checkAborted workerID =
  let script = getScriptPath @"check_aborted"
  runScriptWithWorkerID script workerID = 0
