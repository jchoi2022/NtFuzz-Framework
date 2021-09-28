module FuzzingFramework.Fuzzing.Guest

open System.Diagnostics
open System.IO
open FuzzingFramework
open FuzzingFramework.Utils
open FuzzingFramework.Paths

let private pythonExe = @"C:\Python27\python.exe"
let private hookerDir = @"C:\Hooker"
let private launcherDir = @"C:\Launcher"
let private setupScript = @"C:\Launcher\hooker32.py"
let private fuzzScript = @"C:\Launcher\run32.py"

let private setupDir () =
  if Directory.Exists(hookerDir) then Directory.Delete(hookerDir, true)
  copyDir GuestPath.hookerDir hookerDir
  if Directory.Exists(launcherDir) then Directory.Delete(launcherDir, true)
  copyDir GuestPath.launcherDir launcherDir
  let workDir = GuestPath.fuzzWorkDir
  if Directory.Exists(workDir) then Directory.Delete(workDir, true)
  Directory.CreateDirectory(workDir) |> ignore
  Directory.SetCurrentDirectory(workDir)

let private isUnknownBug (stackStr: string) =
  let buf = File.ReadAllText(GuestPath.knownBugFile).Trim()
  let knownBugs = buf.Split("\n") |> Array.filter ((<>) "")
  // String "*' is wildcard to filter out all the crashes (to save disk space).
  Array.forall (fun s -> s <> "*" && not (stackStr.Contains(s))) knownBugs

let private handleCrash () =
  let cdbOutput = CDB.run()
  let stack = CDB.parseOutput cdbOutput
  let stackStr = String.concat "\n" stack
  printfn "[*] Crash stack: %s" stackStr
  let notEmpty = not (Array.isEmpty stack)
  let isNew = notEmpty && isUnknownBug stackStr && CDB.updateCrashSet stackStr
  if isNew then printfn "[*] Unique crash found (save dump, too)"
  Crash.saveFromGuest isNew cdbOutput

let private runSetup () =
  let args = setupScript
  printfn "[*] Run %s %s" pythonExe args
  use proc = new Process ()
  proc.StartInfo.FileName <- pythonExe
  proc.StartInfo.Arguments <- args
  proc.StartInfo.CreateNoWindow <- false
  if proc.Start () then (proc.WaitForExit(); true)
  else false

let private runFuzz target mutRatio seed =
  let heartbeat = Signal.getHeartBeatPath()
  // Use negative mutation ratio to indicate start fuzzing from the beginning.
  let mutRatio = if mutRatio < 0 then (- mutRatio) else mutRatio
  let scriptArgs = sprintf "%s %s %d %d" heartbeat target mutRatio seed
  let pythonArgs = sprintf "%s %s" fuzzScript scriptArgs
  printfn "[*] Run %s %s" pythonExe pythonArgs
  use proc = new Process ()
  proc.StartInfo.FileName <- pythonExe
  proc.StartInfo.Arguments <- pythonArgs
  proc.StartInfo.CreateNoWindow <- false
  if proc.Start () then proc.WaitForExit()

let run () =
  printfn "[*] Send reboot success signal"
  Signal.sendBootSuccess()
  if Crash.checkFromGuest() then
    printfn "[*] Crash found, check and wait for the host to restore snapshot"
    CDB.setup()
    handleCrash()
    Signal.sendCrashed()
  else
    printfn "[*] Start fuzzing process"
    setupDir()
    Signal.sendHeartBeat ()
    let config = Config.load()
    let seed = Seed.load()
    let setupSuccess = runSetup()
    if setupSuccess then runFuzz config.Target config.MutationRatio seed
    Signal.sendFinished()
