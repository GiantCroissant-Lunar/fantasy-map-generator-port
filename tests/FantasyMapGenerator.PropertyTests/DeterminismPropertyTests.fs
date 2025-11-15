module DeterminismPropertyTests

open Expecto
open FsCheck
open FantasyMapGenerator.Core.Generators
open FantasyMapGenerator.Core.Models

/// Helper to create a deterministic hash of map state
let mapToHash (mapData: MapData) =
    let cellHash = 
        mapData.Cells
        |> Seq.take (min 100 mapData.Cells.Count)
        |> Seq.map (fun c -> (c.Id, c.Height, c.Biome))
        |> Seq.toArray
        |> fun arr -> arr.GetHashCode()
    
    let riverHash =
        mapData.Rivers
        |> Seq.take (min 10 mapData.Rivers.Count)
        |> Seq.map (fun r -> (r.Id, r.Cells.Count, r.Type))
        |> Seq.toArray
        |> fun arr -> arr.GetHashCode()
    
    (cellHash, riverHash)

/// Property: Same seed produces identical maps
let mapGenerationIsDeterministic (seed: int) =
    let settings1 = MapGenerationSettings(Seed = seed, Width = 800, Height = 600, NumPoints = 1000)
    let settings2 = MapGenerationSettings(Seed = seed, Width = 800, Height = 600, NumPoints = 1000)
    
    let generator = MapGenerator()
    
    let map1 = generator.Generate(settings1)
    let map2 = generator.Generate(settings2)
    
    let hash1 = mapToHash map1
    let hash2 = mapToHash map2
    
    hash1 = hash2

/// Property: Different seeds produce different maps
let differentSeedsProduceDifferentMaps (seed1: int, seed2: int) =
    if seed1 = seed2 then true
    else
        let settings1 = MapGenerationSettings(Seed = seed1, Width = 800, Height = 600, NumPoints = 1000)
        let settings2 = MapGenerationSettings(Seed = seed2, Width = 800, Height = 600, NumPoints = 1000)
        
        let generator = MapGenerator()
        
        let map1 = generator.Generate(settings1)
        let map2 = generator.Generate(settings2)
        
        let hash1 = mapToHash map1
        let hash2 = mapToHash map2
        
        hash1 <> hash2

/// Property: Cell count remains constant
let cellCountIsConstant (seed: int) =
    let settings = MapGenerationSettings(Seed = seed, Width = 800, Height = 600, NumPoints = 1000)
    let generator = MapGenerator()
    let mapData = generator.Generate(settings)
    
    mapData.Cells.Count > 0 && mapData.Cells.Count < 100000

[<Tests>]
let determinismTests =
    testList "Determinism Property Tests" [
        testProperty "same seed produces identical maps" mapGenerationIsDeterministic
        testProperty "different seeds produce different maps" differentSeedsProduceDifferentMaps
        testProperty "cell count is within expected range" cellCountIsConstant
    ]
