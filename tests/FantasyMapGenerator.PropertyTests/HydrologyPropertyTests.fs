module HydrologyPropertyTests

open Expecto
open FsCheck
open FantasyMapGenerator.Core.Generators
open FantasyMapGenerator.Core.Models

/// Property: Rivers must always flow downhill (or stay level)
let riversFlowDownhill (seed: int) =
    let settings = MapGenerationSettings(Seed = seed, Width = 800, Height = 600, NumPoints = 1000)
    let generator = MapGenerator()
    let mapData = generator.Generate(settings)
    
    mapData.Rivers
    |> Seq.forall (fun river ->
        river.Cells
        |> Seq.pairwise
        |> Seq.forall (fun (cellIdA, cellIdB) ->
            let cellA = mapData.Cells.[cellIdA]
            let cellB = mapData.Cells.[cellIdB]
            cellA.Height >= cellB.Height))

/// Property: All river cells must have valid indices
let riverCellsAreValid (seed: int) =
    let settings = MapGenerationSettings(Seed = seed, Width = 800, Height = 600, NumPoints = 1000)
    let generator = MapGenerator()
    let mapData = generator.Generate(settings)
    
    let maxIndex = mapData.Cells.Count - 1
    mapData.Rivers
    |> Seq.forall (fun river ->
        river.Cells
        |> Seq.forall (fun cellId -> cellId >= 0 && cellId <= maxIndex))

/// Property: River sources must be at higher elevations than mouths
let riverSourcesHigherThanMouths (seed: int) =
    let settings = MapGenerationSettings(Seed = seed, Width = 800, Height = 600, NumPoints = 1000)
    let generator = MapGenerator()
    let mapData = generator.Generate(settings)
    
    mapData.Rivers
    |> Seq.filter (fun r -> r.Cells.Count > 1)
    |> Seq.forall (fun river ->
        let sourceCell = mapData.Cells.[river.Cells.[0]]
        let mouthCell = mapData.Cells.[river.Cells.[river.Cells.Count - 1]]
        sourceCell.Height >= mouthCell.Height)

/// Property: Rivers must have at least 2 cells
let riversHaveMinimumLength (seed: int) =
    let settings = MapGenerationSettings(Seed = seed, Width = 800, Height = 600, NumPoints = 1000)
    let generator = MapGenerator()
    let mapData = generator.Generate(settings)
    
    mapData.Rivers
    |> Seq.forall (fun river -> river.Cells.Count >= 2)

[<Tests>]
let hydrologyTests =
    testList "Hydrology Property Tests" [
        testProperty "rivers always flow downhill" riversFlowDownhill
        testProperty "river cells have valid indices" riverCellsAreValid
        testProperty "river sources higher than mouths" riverSourcesHigherThanMouths
        testProperty "rivers have minimum length" riversHaveMinimumLength
    ]
