namespace FuzzingFramework.Fuzzing

open System
open System.IO
open FuzzingFramework.Paths

type Seed = int32

module Seed =

  let save workerID seed =
    let buf = sprintf "%u" seed
    File.WriteAllText(HostPath.seedFile workerID, buf)

  let load () =
    Int32.Parse (File.ReadAllText(GuestPath.seedFile).Trim())
