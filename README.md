# ILRepack.Lib.MSBuild.Task

MSBuild task for [ILRepack](https://github.com/gluck/il-repack) which is an open-source alternative to ILMerge.

## Install via NuGet [![NuGet](https://img.shields.io/nuget/v/ILRepack.Lib.MSBuild.Task.svg)](https://www.nuget.org/packages/ILRepack.Lib.MSBuild.Task/) [![NuGet](https://img.shields.io/nuget/dt/ILRepack.Lib.MSBuild.Task.svg)](https://www.nuget.org/packages/ILRepack.Lib.MSBuild.Task/)

      Install-Package ILRepack.Lib.MSBuild.Task

## Supported build tools

* MSBuild

## Usage

You just need to install NuGet package to merge all your project dependencies. If you want to customize the process then you can create a file named "ILRepack.targets" in your project folder. You can create it like shown below.

### Example "ILRepack.targets"

```xml
<!-- ILRepack -->
<?xml version="1.0" encoding="utf-8" ?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
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
</Project>
<!-- /ILRepack -->
```

## Configuration

You need to create "ILRepack.Config.props" file in your project folder to configure the behavior of ILRepack.Lib.MSBuild.Task.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
  </PropertyGroup>
</Project>
```

You can specify following options inside the &lt;PropertyGroup&gt; element to configure the behavior of the ILRepack task.

### Specify your custom Targets file path

If you don't want to add "ILRepack.targets" file in your project folder then you can specify your targets file path as shown below.

```xml
<ILRepackTargetsFile>$(SolutionDir)ILRepack.targets</ILRepackTargetsFile>
```

### Specify Key File to use for signing

You can specify the path of the SNK file you want to use for signing your assembly as shown below. This configuration option only applies if you are using default targets file provided in NuGet package.

```xml
<KeyFile>$(ProjectDir)ILRepack.snk</KeyFile>
```

### Specify whether to clear directory after merging

If you are using default targets file then you may notice that it clears Output directory after merging dependencies. You can turn this functionality off by setting ClearOutputDirectory to False as shown below.

```xml
<ClearOutputDirectory>False</ClearOutputDirectory>
```

## Task options

<table border="0" cellpadding="3" cellspacing="0" width="90%" id="tasksTable">
    <tr>
        <th align="left" width="190">
            Option
        </th>
        <th align="left">
            Description
        </th>
    </tr>
	<tr>
        <td>
           KeyFile
        </td>
        <td>
            Specifies a KeyFile to sign the output assembly.
        </td>
    </tr>
	<tr>
        <td>
           KeyContainer
        </td>
        <td>
            Specifies a KeyContainer to use.
        </td>
    </tr>
	<tr>
        <td>
           LogFile
        </td>
        <td>
           Specifies a logfile to output log information.
        </td>
    </tr>
	<tr>
        <td>
           Union
        </td>
        <td>
           Merges types with identical names into one.
        </td>
    </tr>
	<tr>
        <td>
            DebugInfo
        </td>
        <td>
            Enable/disable symbol file generation.
        </td>
    </tr>
	<tr>
        <td>
            AttributeFile
        </td>
        <td>
            Take assembly attributes from the given assembly file.
        </td>
    </tr>
	<tr>
        <td>
            CopyAttributes
        </td>
        <td>
            Copy assembly attributes.
        </td>
    </tr>
	<tr>
        <td>
            AllowMultiple
        </td>
        <td>
            Allows multiple attributes (if type allows).
        </td>
    </tr>
	<tr>
        <td>
            TargetKind
        </td>
        <td>
            Target assembly kind (Exe|Dll|WinExe|SameAsPrimaryAssembly).
        </td>
    </tr>
	<tr>
        <td>
            TargetPlatformVersion
        </td>
        <td>
            Target platform (v1, v1.1, v2, v4 supported).
        </td>
    </tr>
	<tr>
        <td>
            TargetPlatformDirectory
        </td>
        <td>
            Path of Directory where target platform is located.
        </td>
    </tr>
	<tr>
        <td>
            XmlDocumentation
        </td>
        <td>
            Merge assembly xml documentation.
        </td>
    </tr>
	<tr>
        <td>
            LibraryPath
        </td>
        <td>
            List of paths to use as "include directories" when attempting to merge assemblies.
        </td>
    </tr>
	<tr>
        <td>
            Internalize
        </td>
        <td>
            Set all types but the ones from the first assembly 'internal'.
        </td>
    </tr>
	<tr>
        <td>
            RenameInternalized
        </td>
        <td>
            Rename all internalized types (to be used when Internalize is enabled).
        </td>
    </tr>
	<tr>
        <td>
            InternalizeExclude
        </td>
        <td>
            Assemblies that will not be internalized.
        </td>
    </tr>
	<tr>
        <td>
            OutputFile
        </td>
        <td>
            Output name for merged assembly.
        </td>
    </tr>
	<tr>
        <td>
            InputAssemblies
        </td>
        <td>
            List of assemblies that will be merged.
        </td>
    </tr>
	<tr>
        <td>
            DelaySign
        </td>
        <td>
            Set the keyfile, but don't sign the assembly.
        </td>
    </tr>
	<tr>
        <td>
            AllowDuplicateResources
        </td>
        <td>
            Allows to duplicate resources in output assembly.
        </td>
    </tr>
	<tr>
        <td>
            ZeroPeKind
        </td>
        <td>
            Allows assemblies with Zero PeKind (but obviously only IL will get merged).
        </td>
    </tr>
	<tr>
        <td>
            Parallel
        </td>
        <td>
            Use as many CPUs as possible to merge the assemblies.
        </td>
    </tr>
	<tr>
        <td>
            Verbose
        </td>
        <td>
            Additional debug information during merge that will be outputted to LogFile.
        </td>
    </tr>
	<tr>
        <td>
            Wildcards
        </td>
        <td>
            Allows (and resolves) file wildcards (e.g. `*`.dll) in input assemblies.
        </td>
    </tr>
</table>