module FShadeSample

open Aardvark.Base
open FShade
open FShade.Imperative

type Vertex =
    {
        // [<Position>] is a shorthand for [<Semantic("Positions")>]
        [<Semantic("inPosition")>] pos : V4f
        [<Semantic("inUV")>] uv : V2f
        [<Semantic("inColor")>] col : V4f
    }

type Fragment =
    {
        [<Color>] c : V4d
    }

let fraggy (v : Vertex) =
    fragment {
        let uv = v.uv
        let col = v.col
        let f a = a * 0.5f + 2.0f
        let time : int = uniform?time
        let mutable a = 1.0f + uv.X + col.Y
        for i in 1..5 do
            a <- a + float32 i
        return {
            c = V4d(float32 time, f a, float32 v.pos.Z, 1.0f)
        }
    }

open System
open FShade.GLSL
open System.Text.RegularExpressions

let regexReplace (a : string) (b : string) x = Regex(a, RegexOptions.Singleline).Replace(x, b)

[<EntryPoint>]
let main argv =
    //Examples.UtiliyFunctions.run()
    let shader =
        fraggy
        |> Effect.ofFunction
        |> Effect.toModule {
            EffectConfig.empty with
                lastStage = ShaderStage.Fragment
                outputs = Map.ofList [
                    "Colors", (typeof<V4d>, 0)
                ]
           }
    
    shader
    |> ModuleCompiler.compileGLSL
        (Backend.Create {
            version                 = Version(3, 3)
            enabledExtensions       = Set.empty
            createUniformBuffers    = false
            bindingMode             = BindingMode.PerKind
            createDescriptorSets    = false
            stepDescriptorSets      = false
            createInputLocations    = false
            createPerStageUniforms  = true
            reverseMatrixLogic      = true
        })
    |> fun x -> x.code
    |> regexReplace "fs_([a-zA-Z][0-9a-zA-Z]+)" @"$1"
    |> regexReplace "#version.*#ifdef Vertex.*#endif.*\n#ifdef Fragment(.*)#endif" "$1"
    |> fun x -> x.Trim()
    |> printfn "//--- Generated with FShade ---\n\n%s\n\n//--- Generated with FShade ---"

    0 // return an integer exit code
