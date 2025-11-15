module DeterminismPropertyTests

open Expecto
open FsCheck
open FantasyMapGenerator.Core
open FantasyMapGenerator.Core.Generators
open System.Linq

/// Helper to create a deterministic hash of map state
let mapToHash (map: Map) =
    let cellHash = 
        map.Cells
        |> Seq.take (min 100 map.Cells.Count)
        |> Seq.map (fun c -> (c.Index, c.Height, c.Water, c.Biome))
        |> Seq.toArray
        |> fun arr -> arr.GetHashCode()
    
    let riverHash =
        map.Rivers
        |> Seq.take (min 10 map.Rivers.Count)
        |> Seq.map (fun r -> (r.Id, r.Cells.Count, r.Type))
        |> Seq.toArray
        |> fun arr -> arr.GetHashCode()
    
    (cellHash, riverHash)

/// Property: Same seed produces identical maps
let mapGenerationIsDeterministic (seed: int) =
    let config1 = MapGeneratorConfig(Seed = seed, Width = 800, Height = 600)
    let config2 = MapGeneratorConfig(Seed = seed, Width = 800, Height = 600)
    
    let generator1 = MapGenerator(config1)
    let generator2 = MapGenerator(config2)
    
    let map1 = generator1.Generate()
    let map2 = generator2.Generate()
    
    let hash1 = mapToHash map1
    let hash2 = mapToHash map2
    
    hash1 = hash2

/// Property: Different seeds produce different maps
let differentSeedsProduceDifferentMaps (seed1: int, seed2: int) =
    if seed1 = seed2 then true
    else
        let config1 = MapGeneratorConfig(Seed = seed1, Width = 800, Height = 600)
        let config2 = MapGeneratorConfig(Seed = seed2, Width = 800, Height = 600)
        
        let generator1 = MapGenerator(config1)
        let generator2 = MapGenerator(config2)
        
        let map1 = generator1.Generate()
        let map2 = generator2.Generate()
        
        let hash1 = mapToHash map1
        let hash2 = mapToHash map2
        
        hash1 <> hash2

/// Property: Cell count remains constant
let cellCountIsConstant (seed: int) =
    let config = MapGeneratorConfig(Seed = seed, Width = 800, Height = 600)
    let generator = MapGenerator(config)
    let map = generator.Generate()
    
    map.Cells.Count > 0 && map.Cells.Count < 100000

[<Tests>]
let determinismTests =
    testList "Determinism Property Tests" [
        testProperty "same seed produces identical maps" mapGenerationIsDeterministic
        testProperty "different seeds produce different maps" differentSeedsProduceDifferentMaps
        testProperty "cell count is within expected range" cellCountIsConstant
    ]
