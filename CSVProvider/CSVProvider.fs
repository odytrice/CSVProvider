// Type Provider Skeleton
namespace CSVProvider.Implementation

open FSharp.Core.CompilerServices

open ProviderImplementation
open ProviderImplementation.ProvidedTypes
open System.Reflection

open System.IO
open System.Data
open CSVProvider.Helpers

[<TypeProvider>]
type SimpleTypeProvider(config: TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces(config)

    // Get the assembly and namespace used to house the provided types.
    let asm = Assembly.GetExecutingAssembly()
    let ns = "FSharpConf"

    // Create the main provided type.
    let provider = ProvidedTypeDefinition(asm, ns, "CsvProvider", Some(typeof<obj>))

    let createRowType (datatable:DataTable) =
        let columns = datatable.Columns
        // Define a provided type for each row, This will be erased to a DataRow
        let rowType = ProvidedTypeDefinition("Row", Some(typeof<obj>))

        // Create a field out of each column
        for i in 0 .. columns.Count - 1 do

            let prop =
                ProvidedProperty(columns.[i].ColumnName, columns.[i].DataType,
                    getterCode = fun args -> <@@ ((%%args.[0]:obj) :?> DataRow).[i] |> unbox @@>)

            // Add metadata that defines the property's location in the referenced file.
            rowType.AddMember(prop)
        rowType

    let createType typeName (args: obj[]) =
        let filename = args.[0] :?> string
        // Resolve the filename relative to the resolution folder.
        let resolvedFilename = Path.Combine(config.ResolutionFolder, filename)

        let datatable = CsvFile.Load(resolvedFilename)

        // Define the provided type, erasing to CsvFile.
        let typeName = ProvidedTypeDefinition(asm, ns, typeName, Some(typeof<CsvFile>))

        // Add a parameterless constructor that loads the file that was used to define the schema.
        let ctor0 =
            ProvidedConstructor([],
                invokeCode = fun _ -> <@@ CsvFile(resolvedFilename) @@>)
        typeName.AddMember ctor0

        // Add a constructor that takes the file name to load.
        let ctor1 = ProvidedConstructor([ProvidedParameter("filename", typeof<string>)], invokeCode = fun args -> <@@ CsvFile((%%args.[1]: obj) :?> string) @@>)
        typeName.AddMember ctor1

        let rowType = createRowType datatable

        // Add a more strongly typed Data property, which uses the existing property at runtime.
        let prop =
            ProvidedProperty("Rows", typedefof<seq<_>>.MakeGenericType(rowType),
                getterCode = fun args -> <@@ (%%args.[0]:CsvFile).Rows @@>)
        typeName.AddMember prop

        // Add the row type as a nested type.
        typeName.AddMember rowType
        typeName


    //Declare Static Type Parameters
    let parameters = [ ProvidedStaticParameter("FilePath", typeof<string>) ]

    // Add the type to the namespace.
    do
        provider.DefineStaticParameters(parameters, createType)
        this.AddNamespace(ns, [ provider ])

[<assembly: TypeProviderAssembly>]
do()