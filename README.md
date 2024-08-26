# ILRepack.Lib.MSBuild.Task

MSBuild task for [ILRepack](https://github.com/gluck/il-repack) which is an open-source alternative to ILMerge.

## Install via NuGet [![NuGet](https://img.shields.io/nuget/v/ILRepack.Lib.MSBuild.Task.svg)](https://www.nuget.org/packages/ILRepack.Lib.MSBuild.Task/) [![NuGet](https://img.shields.io/nuget/dt/ILRepack.Lib.MSBuild.Task.svg)](https://www.nuget.org/packages/ILRepack.Lib.MSBuild.Task/)

      Install-Package ILRepack.Lib.MSBuild.Task

## Supported build tools

* MSBuild

## Usage

You just need to install NuGet package to merge all your project dependencies. If you want to customize the process then
you can create a file named "ILRepack.targets" in your project folder. You can create it like shown below.

### Example "ILRepack.targets"

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- ILRepack -->
    <Target Name="ILRepacker" AfterTargets="Build" Condition="'$(Configuration)' == 'Release'">
        <ItemGroup>
            <InputAssemblies Include="$(OutputPath)\ExampleAssemblyToMerge1.dll" />
            <InputAssemblies Include="$(OutputPath)\ExampleAssemblyToMerge2.dll" />
            <InputAssemblies Include="$(OutputPath)\ExampleAssemblyToMerge3.dll" />
        </ItemGroup>
    
        <ItemGroup>
            <!-- Must be a fully qualified name -->
            <DoNotInternalizeAssemblies Include="ExampleAssemblyToMerge3" />
        </ItemGroup>
    
        <ILRepack
            Parallel="true"
            Internalize="true"
            InternalizeExclude="@(DoNotInternalizeAssemblies)"
            InputAssemblies="@(InputAssemblies)"
            TargetKind="Dll"
            OutputFile="$(OutputPath)\$(AssemblyName).dll"
        />
    </Target>
    <!-- /ILRepack -->
</Project>
```

## Configuration

You need to create "ILRepack.Config.props" file in your project folder to configure the behavior of
ILRepack.Lib.MSBuild.Task.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
  </PropertyGroup>
</Project>
```

You can specify following options inside the &lt;PropertyGroup&gt; element to configure the behavior of the ILRepack
task.

### Specify your custom Targets file path

If you don't want to add "ILRepack.targets" file in your project folder then you can specify your targets file path as
shown below.

```xml
<ILRepackTargetsFile>$(SolutionDir)ILRepack.targets</ILRepackTargetsFile>
```

### Specify Key File to use for signing

You can specify the path of the SNK file you want to use for signing your assembly as shown below. This configuration
option only applies if you are using default targets file provided in NuGet package.

```xml
<KeyFile>$(ProjectDir)ILRepack.snk</KeyFile>
```

### Disable default ILRepack Target

By default if no "ILRepack.targets" file is found a default ILRepack Target runs after build if the configuration contains "Release".
You can disable this behavior by setting the following property to `false` or `true` to enable it.

```xml
<ILRepackEnabled>false</ILRepackEnabled>
```

### Specify whether to clear directory after merging

If you are using default targets file then you may notice that it clears Output directory after merging dependencies.
You can turn this functionality off by setting ClearOutputDirectory to False as shown below.

```xml
<ClearOutputDirectory>false</ClearOutputDirectory>
```

## Task options

| Option                         | Description                                                                                                                                                |
|--------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------|
| AllowDuplicateResources        | Allows duplicating resources in the output assembly.                                                                                                       |
| AllowMultiple                  | Allows multiple attributes (if type allows).                                                                                                               |
| AllowedDuplicateNamespaces     | Allows the specified namespaces from being duplicated into input assemblies. Multiple namespaces are delimited by ",".                                     |
| AttributeFile                  | Take assembly attributes from the given assembly file.                                                                                                     |
| CopyAttributes                 | Copy assembly attributes.                                                                                                                                  |
| DebugInfo                      | Enable/disable symbol file generation.                                                                                                                     |
| DelaySign                      | Set the key file, but don't sign the assembly.                                                                                                             |
| ExcludeInternalizeSerializable | Do not internalize types marked as Serializable.                                                                                                           |
| InputAssemblies                | List of assemblies that will be merged.                                                                                                                    |
| Internalize                    | Set all types but the ones from the first assembly 'internal'.                                                                                             |
| InternalizeAssembly            | Internalize only specific assemblies (list of assembly names without path or extension).                                                                   |
| InternalizeExclude             | If Internalize is set to true, any which match these regular expressions will not be internalized. If Internalize is false, then this property is ignored. |
| KeyContainer                   | Specifies a key container to use.                                                                                                                          |
| KeyFile                        | Specifies a key file to sign the output assembly.                                                                                                          |
| LibraryPath                    | List of paths to use as "include directories" when attempting to merge assemblies.                                                                         |
| LogFile                        | Specifies a log file to output log information.                                                                                                            |
| LogTaskMessageImportance       | Specifies the log message importance in the task. (`true` by default)                                                                                      | 
| MergeIlLinkerFiles             | Merge IL Linker file XML resources from Microsoft assemblies (optional). Same-named XML resources ('ILLink.*.xml') will be combined during merging.        |
| NoRepackRes                    | Does not add the embedded resource 'ILRepack.List' with all merged assembly names.                                                                         |
| OutputFile                     | Output name for the merged assembly.                                                                                                                       |
| Parallel                       | Use as many CPUs as possible to merge the assemblies.                                                                                                      |
| PauseBeforeExit                | Pause execution once completed (good for debugging).                                                                                                       |
| RenameInternalized             | Rename all internalized types (to be used when Internalize is enabled).                                                                                    |
| RepackDropAttribute            | Name of an attribute (optional). Members in input assemblies marked with this attribute will be dropped during merging.                                    |
| TargetKind                     | Target assembly kind (Exe\|Dll\|WinExe\|SameAsPrimaryAssembly)                                                                                             |
| TargetPlatformDirectory        | Path of Directory where the target platform is located.                                                                                                    |
| TargetPlatformVersion          | Target platform (v1, v1.1, v2, v4 supported).                                                                                                              |
| Union                          | Merges types with identical names into one.                                                                                                                |
| Verbose                        | Additional debug information during the merge that will be outputted to LogFile.                                                                           |
| Wildcards                      | Allows (and resolves) file wildcards (e.g., `*.dll`) in input assemblies.                                                                                  |
| XmlDocumentation               | Merge assembly XML documentation.                                                                                                                          |
| ZeroPeKind                     | Allows assemblies with Zero PeKind (but obviously only IL will get merged).                                                                                |
