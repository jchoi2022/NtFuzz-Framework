module FuzzingFramework.Signal

open FuzzingFramework.Paths
open System.IO

// Note that 'startFile' should be synched with 'start_check_vm' script.
let private startFile = @"start.txt"
let private finishFile = @"finish.txt"
let private bootFile = @"boot.txt"
let private heartbeatFile = @"heartbeat.txt"
let private crashedFile = @"crashed.txt"

(*************************** Functions for VM guest ***************************)

/// Notify that the work of guest is finished.
let sendFinished () =
  let finishPath = Path.Combine(GuestPath.taskDir, finishFile)
  File.WriteAllText (finishPath, "")

/// Notify that the instance successfully booted and starts working.
let sendBootSuccess () =
  let bootPath = Path.Combine(GuestPath.taskDir, bootFile)
  if File.Exists bootPath then Utils.deleteFileWithRetry bootPath 3

let getHeartBeatPath () =
  Path.Combine(GuestPath.taskDir, heartbeatFile)

/// Notify that the guest code is normally executing.
let sendHeartBeat () =
  let heartbeatPath = Path.Combine(GuestPath.taskDir, heartbeatFile)
  if not (File.Exists(heartbeatPath)) then File.WriteAllText (heartbeatPath, "")

/// Notify that the guest code is going to die (when rebooted from crash).
let sendCrashed () =
  let deadPath = Path.Combine(GuestPath.taskDir, crashedFile)
  File.WriteAllText (deadPath, "")

(*************************** Functions for VM host ***************************)

/// Check if VBoxManage restart command was successful. Note that this signal is
/// sent by 'start_check_vm' script.
let checkStarted workerID =
  let startPath = Path.Combine(HostPath.taskDir workerID, startFile)
  let exists = File.Exists startPath
  if exists then Utils.deleteFileWithRetry startPath 3
  exists

/// Check if the work of VM guest is finished.
let checkFinished workerID =
  let finishPath = Path.Combine(HostPath.taskDir workerID, finishFile)
  let exists = File.Exists(finishPath)
  if exists then Utils.deleteFileWithRetry finishPath 3
  exists

/// Check if the VM guest is normally running.
let checkHeartBeat workerID =
  let hearbeatPath = Path.Combine(HostPath.taskDir workerID, heartbeatFile)
  let exists = File.Exists(hearbeatPath)
  if exists then Utils.deleteFileWithRetry hearbeatPath 3
  exists

/// Check if the VM guest has rebooted from crash.
let checkCrashed workerID =
  let crashedPath = Path.Combine(HostPath.taskDir workerID, crashedFile)
  let exists = File.Exists(crashedPath)
  if exists then Utils.deleteFileWithRetry crashedPath 3
  exists

/// Self-notify that this instance is rebooting.
let sendBootTry workerID =
  let bootPath = Path.Combine(HostPath.taskDir workerID, bootFile)
  File.WriteAllText (bootPath, "")

/// Check if the booting failed (i.e. signal file not deleted by guest).
let checkBootFail workerID =
  // Caution. If file exists, it indicates failure.
  let rebootPath = Path.Combine(HostPath.taskDir workerID, bootFile)
  let exists = File.Exists(rebootPath)
  if exists then Utils.deleteFileWithRetry rebootPath 3
  exists
