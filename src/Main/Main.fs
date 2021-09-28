module FuzzingFramework.Main

open System
open FuzzingFramework

let runPrepare startNum instanceNumToAdd vmBaseDir =
  let startNum = Int32.Parse startNum
  let instanceNumToAdd = Int32.Parse instanceNumToAdd
  let workerNumbers = Seq.init instanceNumToAdd (fun n -> startNum + n)
  for workerNum in workerNumbers do
    let workerID = VirtualMachine.decideWorkerID workerNum
    VirtualMachine.clone workerID vmBaseDir
    VirtualMachine.takeSnapshot workerID

let runConfigure totalInstances nCore memSize =
  let totalInstances = Int32.Parse totalInstances
  let nCore = Int32.Parse nCore
  let memSize = Int32.Parse memSize
  let workerNumbers = Seq.init totalInstances id
  for workerNum in workerNumbers do
    let workerID = VirtualMachine.decideWorkerID workerNum
    VirtualMachine.configure workerID nCore memSize

let runStop instancesPerApp =
  let totalInstances = Int32.Parse instancesPerApp
  let workerNumbers = Seq.init totalInstances id
  for workerNum in workerNumbers do
    let workerID = VirtualMachine.decideWorkerID workerNum
    VirtualMachine.stop workerID

let runCleanUp instancesPerApp =
  let totalInstances = Int32.Parse instancesPerApp
  let workerNumbers = Seq.init totalInstances id
  for workerNum in workerNumbers do
    let workerID = VirtualMachine.decideWorkerID workerNum
    VirtualMachine.delete workerID

let runFuzzing mutRatio timelimit instancesPerApp =
  let mutRatio = Int32.Parse mutRatio
  let timelimit = Int32.Parse timelimit
  let instancesPerApp = Int32.Parse instancesPerApp
  Fuzzing.Host.run mutRatio timelimit instancesPerApp

let runGuest () =
  match Action.load() with
  | Fuzzing -> Fuzzing.Guest.run()
  | PostProcess | Minimize -> failwith "Unsupported in hooking-based fuzzing"

let usage() =
  printfn "Usage :"
  printfn "  guest"
  printfn "  prepare startNum instanceNum vmBaseDir"
  printfn "  configure totalInstances nCore memorySize"
  printfn "  stop totalInstances"
  printfn "  cleanup totalInstances"
  printfn "  fuzzing ratio timelimit instancesPerApp"

[<EntryPoint>]
let main argv =
  match argv with
  | [| "prepare"; a; b; c|] -> runPrepare a b c; 0
  | [| "configure"; a; b; c |] -> runConfigure a b c; 0
  | [| "stop"; a |] -> runStop a; 0
  | [| "cleanup"; a |] -> runCleanUp a; 0
  | [| "fuzzing"; a; b; c |] -> runFuzzing a b c; 0
  | [| "guest" |] -> runGuest (); 0
  | _ -> usage(); 1
