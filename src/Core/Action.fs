namespace FuzzingFramework

open System.IO
open FuzzingFramework.Paths

type Action = Fuzzing | PostProcess | Minimize

module Action =

  let toString = function
    | Fuzzing -> "fuzzing"
    | PostProcess -> "postprocess"
    | Minimize -> "minimize"

  let ofString (s: string) =
    match s.ToLower() with
    | "fuzzing" -> Fuzzing
    | "postprocess" -> PostProcess
    | "minimize" -> Minimize
    | _ -> failwithf "Invalid string: %s" s

  let save workerID action =
    let buf = toString action
    File.WriteAllText(HostPath.actionFile workerID, buf)

  let load () =
    File.ReadAllText(GuestPath.actionFile).Trim() |> ofString
