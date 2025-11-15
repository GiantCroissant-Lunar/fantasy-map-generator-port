module HydrologyPropertyTests

open Expecto
open FsCheck
open FantasyMapGenerator.Core
open FantasyMapGenerator.Core.Generators

/// Property: Rivers must always flow downhill (or stay level)
let riversFlowDownhill (seed: int) =
    let config = MapGeneratorConfig(Seed = seed, Width = 800, Height = 600)
    let generator = MapGenerator(config)
    let map = generator.Generate()
    
    map.Rivers
    |> Seq.forall (fun river ->
        river.Cells
        |> Seq.pairwise
        |> Seq.forall (fun (a, b) -> a.Height >= b.Height))

/// Property: All river cells must have valid indices
let riverCellsAreValid (seed: int) =
    let config = MapGeneratorConfig(Seed = seed, Width = 800, Height = 600)
    let generator = MapGenerator(config)
    let map = generator.Generate()
    
    let maxIndex = map.Cells.Count - 1
    map.Rivers
    |> Seq.forall (fun river ->
        river.Cells
        |> Seq.forall (fun cell -> cell.Index >= 0 && cell.Index <= maxIndex))

/// Property: Lakes must be correctly classified
let lakesAreValid (seed: int) =
    let config = MapGeneratorConfig(Seed = seed, Width = 800, Height = 600)
    let generator = MapGenerator(config)
    let map = generator.Generate()
    
    // All lake cells should have water > 0
    map.Cells
    |> Seq.filter (fun c -> c.Lake > 0)
    |> Seq.forall (fun c -> c.Water > 0.0)

/// Property: River sources must be at higher elevations than mouths
let riverSourcesHigherThanMouths (seed: int) =
    let config = MapGeneratorConfig(Seed = seed, Width = 800, Height = 600)
    let generator = MapGenerator(config)
    let map = generator.Generate()
    
    map.Rivers
    |> Seq.filter (fun r -> r.Cells.Count > 1)
    |> Seq.forall (fun river ->
        let source = river.Cells.[0]
        let mouth = river.Cells.[river.Cells.Count - 1]
        source.Height >= mouth.Height)

[<Tests>]
let hydrologyTests =
    testList "Hydrology Property Tests" [
        testProperty "rivers always flow downhill" riversFlowDownhill
        testProperty "river cells have valid indices" riverCellsAreValid
        testProperty "lakes are correctly classified" lakesAreValid
        testProperty "river sources higher than mouths" riverSourcesHigherThanMouths
    ]
