module FuzzingFramework.Crash

open System.IO
open System.Diagnostics
open Printf
open FuzzingFramework.Paths

let private vmDumpPath = @"C:\Windows\MEMORY.DMP"
let private dumpFormat: StringFormat<uint32 -> string> = @"%u.dmp"
let private cdbLogFormat: StringFormat<uint32 -> string> = @"%u.cdb"

let private getCrashNumsFromDir dir =
  Directory.GetFiles dir
  |> Array.map (Path.GetFileNameWithoutExtension >> uint32)
  |> Array.distinct
  |> Array.sort

let private getCrashMaxNumFromDir dir =
  getCrashNumsFromDir dir
  |> Array.tryLast
  |> Option.defaultValue 0u

/// Save the VM's memory dump to crash directory.
let private saveDump crashNo =
  if File.Exists(vmDumpPath) then
    // Note that we should run script, since this operation requires admin priv.
    let script = Path.Combine(GuestPath.scriptDir, "move_dump.bat")
    use proc = new Process ()
    proc.StartInfo.FileName <- script
    proc.StartInfo.Arguments <- sprintf "%d" crashNo
    proc.StartInfo.CreateNoWindow <- true
    if proc.Start ()
    then proc.WaitForExit ()
    else failwith "Error while running memory dump move script"

/// Check if this VM has rebooted from crash.
let checkFromGuest () =
  File.Exists(vmDumpPath)

/// Save the crash (seed, blacklist, dump, cdb log) from within the VM guest.
let saveFromGuest isNew cdbOutput =
  // Caution: +1 to avoid duplicate.
  let crashNo = getCrashMaxNumFromDir GuestPath.crashDir + 1u
  if isNew then saveDump crashNo // To save space, save dump only for new crash.
  let cdbLogFile = sprintf cdbLogFormat crashNo
  let cdbLogPath = Path.Combine(GuestPath.crashDir, cdbLogFile)
  System.IO.File.WriteAllText(cdbLogPath, cdbOutput)
