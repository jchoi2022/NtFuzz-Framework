module FuzzingFramework.Fuzzing.Host

open System
open System.IO
open System.Collections.Generic
open System.Threading
open FuzzingFramework
open FuzzingFramework.Paths

type State = {
  LastRestore : DateTime
  HeartBeatLife : int
  RunTime : int
}

let private stateMap = Dictionary<VirtualMachine.WorkerID, State>()

let private rand = Random()

let private initState workerID =
  let now = DateTime.Now
  let life = Literal.heartBeatLifeForBooting
  let state = { LastRestore = now; HeartBeatLife = life; RunTime = 0 }
  stateMap.[workerID] <- state

let private resetRestoreTime workerID =
  let state = stateMap.[workerID]
  stateMap.[workerID] <- { state with LastRestore = DateTime.Now }

let private resetHeartBeatLife workerID heartBeatLife =
  let state = stateMap.[workerID]
  stateMap.[workerID] <- { state with HeartBeatLife = heartBeatLife }

let private incrRunTime workerID =
  let state = stateMap.[workerID]
  let newRunTime = state.RunTime + Literal.checkIntervalInSeconds
  stateMap.[workerID] <- { state with RunTime = newRunTime }

let private printRunTime workerID =
  let runTime = stateMap.[workerID].RunTime
  let hour = runTime / 3600
  let min = (runTime % 3600) / 60
  let sec = runTime % 60
  printfn "Total runtime of %s = %02d:%02d:%02d" workerID hour min sec

let private discountHeartBeatLife workerID =
  printfn "[*] Heartbeat not heard from %s" workerID
  let state = stateMap.[workerID]
  stateMap.[workerID] <- { state with HeartBeatLife = state.HeartBeatLife - 1 }

let private checkHeartBeatDown workerID =
  stateMap.[workerID].HeartBeatLife <= 0

let private checkRestoreTime workerID =
  let state = stateMap.[workerID]
  let lastRestore = state.LastRestore
  lastRestore.AddMinutes(float Literal.restoreIntervalInMinutes) < DateTime.Now

let private saveSeed workerID =
  Seed.save workerID (rand.Next(0x7fffffff))

let private overwriteTypeSpec instancesPerApp workerNum workerID =
  let typeFiles = Directory.GetFiles(HostPath.typeVariationDir)
  if Array.length typeFiles <> instancesPerApp then
    failwithf "[*] InstancesPerApp does not match the # of type spec files"
  let iterNth = workerNum % instancesPerApp
  let typeFileSrc = Path.Combine(HostPath.typeVariationDir, typeFiles.[iterNth])
  let hookerDirDst = HostPath.hookerDir workerID
  let typeFileDst = Path.Combine(hookerDirDst, "Types.json")
  File.Copy(typeFileSrc, typeFileDst, true)

let private initTaskDir mutRatio instancesPerApp workerNum app =
  let workerID = VirtualMachine.decideWorkerID workerNum
  // First, create task directory and crash directory.
  let taskDir = HostPath.taskDir workerID
  Directory.CreateDirectory(taskDir) |> ignore
  let crashDir = HostPath.crashDir workerID
  Directory.CreateDirectory(crashDir) |> ignore
  // Next, copy FuzzingFramework, hooker, and launcher directories.
  let codeDirDst = HostPath.codeDir workerID
  Utils.copyDir HostPath.codeDirSrc codeDirDst
  let hookerDirDst = HostPath.hookerDir workerID
  Utils.copyDir HostPath.hookerDirSrc hookerDirDst
  let launcherDirDst = HostPath.launcherDir workerID
  Utils.copyDir HostPath.launcherDirSrc launcherDirDst
  // Save configuration file and action file.
  Config.save workerID app mutRatio
  Action.save workerID Fuzzing
  // Save the first seed file.
  saveSeed workerID
  // Finally, prepare symbol directory and known bug list file.
  let symbolDirDst = HostPath.symbolDir workerID
  Utils.copyDir HostPath.symbolDirSrc symbolDirDst
  let knownBugFileDst = HostPath.knownBugFile workerID
  File.Copy(HostPath.knownBugFileSrc, knownBugFileDst)
  // Ad-hoc logic for type accuracy experiment
  if Directory.Exists(HostPath.typeVariationDir) then
    overwriteTypeSpec instancesPerApp workerNum workerID

let private initTaskDirs mutRatio apps instancesPerApp =
  let workList = List.collect (List.replicate instancesPerApp) apps
  List.iteri (initTaskDir mutRatio instancesPerApp) workList

let private writeTimeInfo timelimit =
  let currentTime = DateTime.Now
  let finishTime = DateTime.Now.AddMinutes (float timelimit)
  let curStr = currentTime.ToString "yyyy-MM-dd-HH-mm"
  let finStr = finishTime.ToString "yyyy-MM-dd-HH-mm"
  let appendStr = sprintf "%s %s\n" curStr finStr
  File.AppendAllText(HostPath.timeInfoFile, appendStr)

let private startInstances workerIDs =
  for workerID in workerIDs do
    VirtualMachine.restoreSnapshot workerID
    VirtualMachine.start workerID
    // To mitigate sudden burst of resource use.
    Thread.Sleep (5 * Literal.secondInMilliseconds)
    initState workerID

let private printRunTimes workerIDs =
  for workerID in workerIDs do
    printRunTime workerID

let private haltInstances workerIDs =
  for workerID in workerIDs do
    VirtualMachine.stop workerID

let private restoreInstance workerID =
  VirtualMachine.stop workerID
  printfn "[*] Now save a fresh seed to %s" workerID
  saveSeed workerID
  VirtualMachine.restoreSnapshot workerID
  let started = VirtualMachine.startAndCheck workerID
  // Since we restore snapshot, we expect the start command to always success.
  if not started then
    failwithf "[*] Rebooting VBoxManage command failed on %s" workerID
  printfn "[*] Now reset last restoration time & down count of %s" workerID
  resetRestoreTime workerID
  resetHeartBeatLife workerID Literal.heartBeatLifeForBooting

let private rebootInstance workerID =
  VirtualMachine.stop workerID
  printfn "[*] Save a fresh configuration to %s" workerID
  saveSeed workerID
  VirtualMachine.start workerID
  printfn "[*] Mark %s as trying to boot" workerID
  Signal.sendBootTry workerID
  printfn "[*] Now reset last restoration time & down count of %s" workerID
  resetRestoreTime workerID
  resetHeartBeatLife workerID Literal.heartBeatLifeForBooting

let private updateInstanceState workerID =
  printfn "[*] Checking the heartbeat of %s" workerID
  if not (Signal.checkHeartBeat workerID)
  then discountHeartBeatLife workerID
  else resetHeartBeatLife workerID Literal.heartBeatLifeForFuzzing
       incrRunTime workerID

let handleInstanceErrors workerID =
  if VirtualMachine.checkAborted workerID then
    printfn "[*] %s has aborted, restore" workerID
    restoreInstance workerID
  elif Signal.checkCrashed workerID then
    printfn "[*] %s rebooted from crash, restore" workerID
    restoreInstance workerID
  elif Signal.checkFinished workerID then
    printfn "[*] %s stopped running due to an exception" workerID
    restoreInstance workerID
  elif checkRestoreTime workerID then
    printfn "[*] Enough time elapsed for %s, restore" workerID
    restoreInstance workerID
  elif checkHeartBeatDown workerID then
    if Signal.checkBootFail workerID then // Failed to reboot.
      printfn "[*] %s failed to reboot and launch guest code, restore" workerID
      restoreInstance workerID
    else // (4-1) and (4-2) case. Reboot w/o restoring snapshot, for (4-1).
      printfn "[*] %s seems to be down, first try rebooting" workerID
      rebootInstance workerID

let private monitorInstances workerIDs =
  for workerID in workerIDs do
    updateInstanceState workerID
    handleInstanceErrors workerID

let rec private runLoop finishTime checkTime workerIDs =
  let now = DateTime.Now
  if now >= finishTime then
    printRunTimes workerIDs
    haltInstances workerIDs
  elif now >= checkTime then
    // Caution: next check time must be calculated immediately.
    let nextCheckTime = now.AddSeconds(float Literal.checkIntervalInSeconds)
    monitorInstances workerIDs
    printfn "[*] Next check time: %s" (nextCheckTime.ToString())
    printfn "[*] Finish time: %s" (finishTime.ToString())
    runLoop finishTime nextCheckTime workerIDs
  else
    Thread.Sleep (Literal.secondInMilliseconds)
    runLoop finishTime checkTime workerIDs

let run mutRatio timelimit instancesPerApp =
  let finishTime = DateTime.Now.AddMinutes(float timelimit)
  if not (Directory.Exists(HostPath.tasksRootDir)) then
    Directory.CreateDirectory(HostPath.tasksRootDir) |> ignore
  Utils.assertDirPrepared(HostPath.hookerDirSrc)
  Utils.assertDirPrepared(HostPath.launcherDirSrc)
  Utils.assertDirPrepared(HostPath.codeDirSrc)
  Utils.assertDirPrepared(HostPath.symbolDirSrc)
  Utils.assertFilePrepared(HostPath.knownBugFileSrc)
  let apps = File.ReadAllText(HostPath.appList).Split("\n")
             |> Array.toList
             |> List.filter ((<>) "")
             |> List.sort
  let totalInstances = List.length apps * instancesPerApp
  let workerNumbers = List.init totalInstances id
  let workerIDs = List.map VirtualMachine.decideWorkerID workerNumbers
  if not (Utils.checkTasksDirClear()) then
    printfn "[*] Tasks directory not clear, want to continue? (y/n)"
    if Console.ReadLine().Trim().ToLower() <> "y" then exit(1)
  else
    initTaskDirs mutRatio apps instancesPerApp
  writeTimeInfo timelimit
  startInstances workerIDs
  printfn "[*] Wait for a while until the VM guests start working"
  Thread.Sleep(Literal.vmBootWaitInMilliseconds)
  runLoop finishTime DateTime.Now workerIDs
