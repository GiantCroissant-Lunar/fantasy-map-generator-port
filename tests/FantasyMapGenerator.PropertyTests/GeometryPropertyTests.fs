module GeometryPropertyTests

open Expecto
open FsCheck
open FantasyMapGenerator.Core.Generators
open FantasyMapGenerator.Core.Models

/// Property: All cells must have valid neighbor references
let cellNeighborsAreValid (seed: int) =
    let settings = MapGenerationSettings(Seed = seed, Width = 800, Height = 600, NumPoints = 1000)
    let generator = MapGenerator()
    let mapData = generator.Generate(settings)
    
    let maxIndex = mapData.Cells.Count - 1
    mapData.Cells
    |> Seq.forall (fun cell ->
        cell.Neighbors
        |> Seq.forall (fun n -> n >= 0 && n <= maxIndex))

/// Property: Voronoi cells must not be degenerate
let voronoiCellsAreValid (seed: int) =
    let settings = MapGenerationSettings(Seed = seed, Width = 800, Height = 600, NumPoints = 1000)
    let generator = MapGenerator()
    let mapData = generator.Generate(settings)
    
    mapData.Cells
    |> Seq.forall (fun cell ->
        // Each cell should have at least 3 neighbors (triangulation property)
        cell.Neighbors.Count >= 3 && cell.Neighbors.Count <= 10)

/// Property: Heights must be within valid range (0-100 as byte)
let heightsAreInRange (seed: int) =
    let settings = MapGenerationSettings(Seed = seed, Width = 800, Height = 600, NumPoints = 1000)
    let generator = MapGenerator()
    let mapData = generator.Generate(settings)
    
    mapData.Cells
    |> Seq.forall (fun cell ->
        cell.Height >= 0uy && cell.Height <= 100uy)

/// Property: Biomes must be assigned (non-negative)
let biomesAreAssigned (seed: int) =
    let settings = MapGenerationSettings(Seed = seed, Width = 800, Height = 600, NumPoints = 1000)
    let generator = MapGenerator()
    let mapData = generator.Generate(settings)
    
    mapData.Cells
    |> Seq.forall (fun cell -> cell.Biome >= 0)

/// Property: No self-referencing neighbors
let noSelfReferencingNeighbors (seed: int) =
    let settings = MapGenerationSettings(Seed = seed, Width = 800, Height = 600, NumPoints = 1000)
    let generator = MapGenerator()
    let mapData = generator.Generate(settings)
    
    mapData.Cells
    |> Seq.mapi (fun idx cell ->
        cell.Neighbors
        |> Seq.forall (fun n -> n <> idx))
    |> Seq.forall id

[<Tests>]
let geometryTests =
    testList "Geometry Property Tests" [
        testProperty "cell neighbors are valid indices" cellNeighborsAreValid
        testProperty "voronoi cells are not degenerate" voronoiCellsAreValid
        testProperty "heights are in valid range" heightsAreInRange
        testProperty "biomes are assigned to all cells" biomesAreAssigned
        testProperty "no self-referencing neighbors" noSelfReferencingNeighbors
    ]
