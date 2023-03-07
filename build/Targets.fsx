// load dependencies from source folder to allow bootstrapping
#r "/bin/Plainion.CI/FAKE/FakeLib.dll"
#load "/bin/Plainion.CI/bits/PlainionCI.fsx"

open Fake
open PlainionCI

Target "CreatePackage" (fun _ ->
    !! ( outputPath </> "*.*Tests.*" )
    ++ ( outputPath </> "*nunit*" )
    ++ ( outputPath </> "TestResult.xml" )
    ++ ( outputPath </> "**/*.pdb" )
    |> DeleteFiles

    [
        ( "TickSpec.Build.targets", Some "build", None)
        ( "TickSpec.CodeGen", Some "lib/netstandard2.0", None)
        ( "TickSpec.Build", Some "tasks/net6.0", None)
    ]
    |> PNuGet.Pack (projectRoot </> "build" </> "TickSpec.Build.nuspec") (projectRoot </> "pkg")
)

Target "Deploy" (fun _ ->
    trace "Nothing to deploy"
)

Target "Publish" (fun _ ->
    // PNuGet.PublishPackage (projectName + ".Core") (projectRoot </> "pkg")
)

RunTarget()
