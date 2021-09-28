module FuzzingFramework.CDB

open System.IO
open System.Diagnostics
open System.Threading
open FuzzingFramework
open FuzzingFramework.Paths

let private STACK_START_INDICATOR = "STACK_TEXT:"
let private STACK_END_INDICATOR = "\r\n\r\n"
let private KERNEL_ENTRY_SIG = "Hooker!HookNt"

// Following two file names should be synched with "run_cdb.bat".
let private redirectPath = @".\cdb.txt"
let private localSymDir = @"C:\symbols"

let private errorLogPath = Path.Combine(GuestPath.taskDir, "cdb_error.txt")
let private crashLogPath = Path.Combine(GuestPath.taskDir, "cdb_crash.txt")
let private CRASH_DELIM = "=============\n"

exception ParseErrorException of string

let private logError error =
  let errMsg = sprintf "[ERROR] %s\n" error
  File.AppendAllText(errorLogPath, errMsg)

let private copyNtosKrnl () =
  // This operation requires admin priv, so run it with a script.
  let script = Path.Combine(GuestPath.scriptDir, "copy_ntoskrnl.bat")
  use proc = new Process ()
  proc.StartInfo.FileName <- script
  proc.StartInfo.Arguments <- ""
  proc.StartInfo.CreateNoWindow <- true
  if proc.Start ()
  then proc.WaitForExit ()
  else failwith "Error while running NTOSKRNL copy script"

let setup () =
  // Copy symbol directory.
  if Directory.Exists(localSymDir) then Directory.Delete(localSymDir, true)
  Utils.copyDir GuestPath.symbolDir localSymDir
  // Copy ntoskrnl.exe to current direcotry (if not, cdb raises error).
  copyNtosKrnl()

let run () =
  // This operation requires admin priv, so run it with a script.
  let script = Path.Combine(GuestPath.scriptDir, "run_cdb.bat")
  use proc = new Process ()
  proc.StartInfo.FileName <- script
  proc.StartInfo.Arguments <- ""
  proc.StartInfo.CreateNoWindow <- true
  printfn "[*] Running cdb..."
  if proc.Start () then
    printfn "[*] CDB script started, now wait for exit..."
    proc.WaitForExit ()
    Thread.Sleep(Literal.secondInMilliseconds) // Wait for file system sync.
    try File.ReadAllText(redirectPath) with _ -> ""
  else ""

let private tryParseCall (s: string) =
  let tokens = s.Split(" ")
  let lastToken = tokens.[tokens.Length - 1].Trim()
  if lastToken = "wrong." || lastToken = "details" then None
  else Some lastToken

let private parseOutputAux (buf: string) =
  printfn "[*] Parsing CDB output"
  if not (buf.Contains(STACK_START_INDICATOR)) then
    let msg = sprintf "(No '%s' in log)\n%s" STACK_START_INDICATOR buf
    raise (ParseErrorException msg)
  let buf = buf.Split(STACK_START_INDICATOR).[1]
  if not (buf.Contains(STACK_END_INDICATOR)) then
    let msg = sprintf "(No '\\n\\n' in log)\n%s" buf
    raise (ParseErrorException msg)
  let buf = buf.Split(STACK_END_INDICATOR).[0].Trim()
  let lines = buf.Split("\n")
  let calls = Array.choose tryParseCall lines
  let isKernelEntry (call: string) = call.Contains(KERNEL_ENTRY_SIG)
  match Array.tryFindIndex isKernelEntry calls with
  | Some idx when idx >= 1 -> calls.[.. idx]
  | Some _ | None -> calls

let parseOutput cdbOutput =
  try parseOutputAux cdbOutput with
  | ParseErrorException msg -> logError msg; [| |]

let private loadCrashStacks () =
  let buf = try File.ReadAllText(crashLogPath).Trim() with _ -> ""
  buf.Split(CRASH_DELIM) |> Array.filter ((<>) "")

let private saveCrashStacks crashes =
  let buf = String.concat CRASH_DELIM crashes
  File.WriteAllText(crashLogPath, buf)

let updateCrashSet crashStack =
  let crashStacks = loadCrashStacks()
  if Array.contains crashStack crashStacks then false
  else (saveCrashStacks (Array.append crashStacks [| crashStack |]); true)
