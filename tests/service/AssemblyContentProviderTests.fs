#if INTERACTIVE
#r "../../debug/fcs/net45/FSharp.Compiler.Service.dll" // note, run 'build fcs debug' to generate this, this DLL has a public API so can be used from F# Interactive
#r "../../packages/NUnit.3.5.0/lib/net45/nunit.framework.dll"
#load "FsUnit.fs"
#load "Common.fs"
#else
module Tests.Service.AssemblyContentProviderTests
#endif

open System
open System.IO
open System.Text
open NUnit.Framework
open Microsoft.FSharp.Compiler.SourceCodeServices

let private filePath = "C:\\test.fs"

let private projectOptions : FSharpProjectOptions = 
    { ProjectFileName = "C:\\test.fsproj"
      ProjectId = None
      SourceFiles =  [| filePath |]
      ReferencedProjects = [| |]
      OtherOptions = [| |]
      IsIncompleteTypeCheckEnvironment = true
      UseScriptResolutionRules = false
      LoadTime = DateTime.MaxValue
      OriginalLoadReferences = []
      UnresolvedReferences = None
      ExtraProjectInfo = None
      Stamp = None }

let private checker = FSharpChecker.Create()

let (=>) (source: string) (expected: string list) =
    let lines =
        use reader = new StringReader(source)
        [| let line = ref (reader.ReadLine())
           while not (isNull !line) do
               yield !line
               line := reader.ReadLine()
           if source.EndsWith "\n" then
               // last trailing space not returned
               // http://stackoverflow.com/questions/19365404/stringreader-omits-trailing-linebreak
               yield "" |]

    let _, checkFileAnswer = checker.ParseAndCheckFileInProject(filePath, 0, source, projectOptions) |> Async.RunSynchronously
    
    let checkFileResults =
        match checkFileAnswer with
        | FSharpCheckFileAnswer.Aborted -> failwithf "ParseAndCheckFileInProject aborted"
        | FSharpCheckFileAnswer.Succeeded(checkFileResults) -> checkFileResults

    let actual = 
        AssemblyContentProvider.getAssemblySignatureContent AssemblyContentType.Full checkFileResults.PartialAssemblySignature
        |> List.map (fun x -> x.CleanedIdents |> String.concat ".") 
        |> List.sort

    let expected = List.sort expected

    if actual <> expected then failwithf "\n\nExpected\n\n%A\n\nbut was\n\n%A" expected actual

[<Test>]
let ``implicitly added Module suffix is removed``() =
    """
type MyType = { F: int }

module MyType =
    let func123 x = x
"""
    => ["Test"
        "Test.MyType"
        "Test.MyType"
        "Test.MyType.func123"]
        
[<Test>]
let ``Module suffix added by an xplicitly applied MuduleSuffix attribute is removed``() =
    """
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module MyType =
    let func123 x = x
"""
    => [ "Test"
         "Test.MyType"
         "Test.MyType.func123" ]


