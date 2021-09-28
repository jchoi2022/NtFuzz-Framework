namespace FuzzingFramework.Fuzzing

open System
open System.IO
open FuzzingFramework.Paths

type Config = {
  Target : string
  MutationRatio : int32
}

module Config =

  let save workerID target mutRatio =
    let buf = sprintf "%s %d" target mutRatio
    File.WriteAllText(HostPath.configFile workerID, buf)

  let load () =
    let tokens = File.ReadAllText(GuestPath.configFile).Split()
    let target = tokens.[0]
    let mutRatio = Int32.Parse tokens.[1]
    { Target = target; MutationRatio = mutRatio }
