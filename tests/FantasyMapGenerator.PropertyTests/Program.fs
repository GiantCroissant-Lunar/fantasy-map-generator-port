module Program

open Expecto

[<EntryPoint>]
let main argv =
    // Combine all test suites
    let allTests =
        testList "Fantasy Map Generator Property Tests" [
            HydrologyPropertyTests.hydrologyTests
            DeterminismPropertyTests.determinismTests
            GeometryPropertyTests.geometryTests
        ]
    
    // Run tests
    runTestsInAssemblyWithCLIArgs [] argv
