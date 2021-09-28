module FuzzingFramework.Paths

open System.IO

let private coreDir = __SOURCE_DIRECTORY__
let private srcDir = Directory.GetParent(coreDir).FullName
let private prjDir = Directory.GetParent(srcDir).FullName

let private scriptDirName  = @"script"
let private codeDirName = @"codebase"
let private hookerDirName = @"Hooker"
let private launcherDirName = @"Launcher"
let private symbolDirName = @"symbols"
let private knownBugFileName = @"knowns.txt"
let private configFileName = @"config.txt"
let private seedFileName = @"seed.txt"
let private actionFileName = @"action.txt"
let private crashDirName = @"crash"

module GuestPath =
  let taskDir = @"\\vboxsvr\task\"
  let fuzzWorkDir = @".\box"

  let scriptDir = Path.Combine(prjDir, scriptDirName)
  // Note that 'codeDir' should be managed in 'init.bat' script.
  let hookerDir = Path.Combine(taskDir, hookerDirName)
  let launcherDir = Path.Combine(taskDir, launcherDirName)
  let symbolDir = Path.Combine(taskDir, symbolDirName)
  let knownBugFile = Path.Combine(taskDir, knownBugFileName)
  let configFile = Path.Combine(taskDir, configFileName)
  let seedFile = Path.Combine(taskDir, seedFileName)
  let actionFile = Path.Combine(taskDir, actionFileName)
  let crashDir = Path.Combine(taskDir, crashDirName)

module HostPath =
  let tasksRootDir = Path.Combine(prjDir, @"tasks")
  let timeInfoFile = Path.Combine(tasksRootDir, @"time.txt")
  let taskDir workerID = Path.Combine(tasksRootDir, workerID)

  let appList = Path.Combine(prjDir, @"apps.txt")
  let scriptDir = Path.Combine(prjDir, scriptDirName)
  let codeDirSrc = Path.Combine(prjDir, codeDirName)
  let hookerDirSrc = Path.Combine(prjDir, hookerDirName)
  let launcherDirSrc = Path.Combine(prjDir, launcherDirName)
  let symbolDirSrc = Path.Combine(prjDir, symbolDirName)
  let knownBugFileSrc = Path.Combine(prjDir, knownBugFileName)

  // Ad-hoc path for type accuracy experiment
  let typeVariationDir = Path.Combine(prjDir, @"Types")

  let codeDir workerID = Path.Combine(taskDir workerID, codeDirName)
  let hookerDir workerID = Path.Combine(taskDir workerID, hookerDirName)
  let launcherDir workerID = Path.Combine(taskDir workerID, launcherDirName)
  let symbolDir workerID = Path.Combine(taskDir workerID, symbolDirName)
  let knownBugFile workerID = Path.Combine(taskDir workerID, knownBugFileName)
  let configFile workerID = Path.Combine(taskDir workerID, configFileName)
  let seedFile workerID = Path.Combine(taskDir workerID, seedFileName)
  let actionFile workerID = Path.Combine(taskDir workerID, actionFileName)
  let crashDir workerID = Path.Combine(taskDir workerID, crashDirName)
