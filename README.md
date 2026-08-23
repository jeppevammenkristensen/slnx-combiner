# slnx-combiner

[![Slnx.Combine](https://img.shields.io/nuget/v/Slnx.Combine.svg?style=flat-square&label=Slnx.Combine)](https://www.nuget.org/packages/Slnx.Combine)

`slnx-combine` is a .NET tool that searches a directory and its subdirectories for Visual Studio solution files (`.sln` and `.slnx`) and combines their projects into one XML solution (`.slnx`).

## Install

```powershell
dotnet tool install --global Slnx.Combine --version 0.0.2
```

## Usage

```text
slnx-combine <Output> [TraverseDirectory]
```

- `Output` is the generated solution path. If its extension is not `.slnx`, the tool changes it to `.slnx`.
- `TraverseDirectory` is the directory searched recursively for `.sln` and `.slnx` files. When omitted, the output file's directory is searched.

When installed as a global .NET tool from NuGet:

```powershell
slnx-combine combined.slnx C:\code\my-repository
```

When running the project directly:

```powershell
dotnet run --project src/SlnxCombiner/SlnxCombiner.csproj -- combined.slnx C:\code\my-repository
```

The output file is excluded from the discovered input solutions, so an existing combined solution can be regenerated in place.

## How the combined solution is generated

For every discovered solution, the tool:

1. Reads all of its projects.
2. Resolves project paths relative to that input solution.
3. Rewrites the paths relative to the generated output file.
4. Places the projects in a solution folder named after the input solution file.

If multiple input solutions have the same filename, their generated folder names receive suffixes such as `_1` and `_2`. A project referenced by more than one solution is emitted only once, directly under the root of the combined solution. Project display names and project type metadata are preserved when present.

For example, combining `Api.slnx` and `Worker.sln`, which both reference `Shared.csproj`, produces a structure similar to:

```xml
<Solution>
  <!--Projects that where duplicates-->
  <Project Path="src/Shared/Shared.csproj" />

  <!--Folder: solutions/Api.slnx-->
  <Folder Name="/Api/">
    <Project Path="src/Api/Api.csproj" />
  </Folder>

  <!--Folder: solutions/Worker.sln-->
  <Folder Name="/Worker/">
    <Project Path="src/Worker/Worker.csproj" />
  </Folder>
</Solution>
```

The exact relative paths depend on the locations of the input solutions, projects, and output file.

## Build and test

```powershell
dotnet test slnx-combiner.slnx
```
