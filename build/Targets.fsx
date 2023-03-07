// load dependencies from source folder to allow bootstrapping
#r "/bin/Plainion.CI/Fake.Core.Target.dll"
#r "/bin/Plainion.CI/Fake.IO.FileSystem.dll"
#r "/bin/Plainion.CI/Fake.IO.Zip.dll"
#r "/bin/Plainion.CI/Plainion.CI.Tasks.dll"

open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Plainion.CI

Target.create "CreatePackage" (fun _ ->
    !! ( outputPath </> "*.*Tests.*" )
    ++ ( outputPath </> "*nunit*" )
    ++ ( outputPath </> "TestResult.xml" )
    ++ ( outputPath </> "**/*.pdb" )
    |> File.deleteAll

    [
        ( "TickSpec.Build.targets", Some "build", None)
        ( "TickSpec.CodeGen", Some "lib/netstandard2.0", None)
        ( "TickSpec.Build", Some "tasks/net6.0", None)
    ]
    |> PNuGet.Pack (projectRoot </> "build" </> "TickSpec.Build.nuspec") (projectRoot </> "pkg")
)

Target.create "Deploy" (fun _ ->
    ()
)

Target.create "Publish" (fun _ ->
    ()
    // PNuGet.PublishPackage (projectName + ".Core") (projectRoot </> "pkg")
)

Target.runOrDefault ""
