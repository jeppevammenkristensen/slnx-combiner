# slnx-combiner

[![Slnx.Combine](https://img.shields.io/nuget/v/Slnx.Combine.svg?style=flat-square&label=Slnx.Combine)](https://www.nuget.org/packages/Slnx.Combine)

`slnx-combine` is a .NET tool that searches one or more directories and their subdirectories for Visual Studio solution files (`.sln` and `.slnx`) and combines their projects into one XML solution (`.slnx`).

## Install

```powershell
dotnet tool install --global Slnx.Combine
```

## Usage

```text
slnx-combine <Output> [TraverseDirectory ...] [OPTIONS]
slnx-combine combine <Output> [TraverseDirectory ...] [OPTIONS]
```

- `Output` is the generated solution path. If its extension is not `.slnx`, the tool changes it to `.slnx`.
- `TraverseDirectory` is a directory searched recursively for `.sln` and `.slnx` files. Supply multiple directories to search all of them. When omitted, the output file's directory is searched.

### Options

| Option | Description |
| --- | --- |
| `--overwrite` | Overwrite the output file if it already exists. |
| `--include <REGEX>` | Include only solution filenames that match the regular expression. |
| `--exclude <REGEX>` | Exclude solution filenames that match the regular expression. |

Include and exclude expressions are case-insensitive and match the filename without its extension. Invalid expressions are reported before file discovery starts. The filters can be combined: inclusion is applied first, followed by exclusion.

### Examples

When installed as a global .NET tool from NuGet:

```powershell
slnx-combine combined.slnx C:\code\my-repository
```

Search multiple directories:

```powershell
slnx-combine combined.slnx C:\code\services C:\code\libraries
```

Include solution names beginning with `Team-`, but exclude names ending in `-Tests`:

```powershell
slnx-combine combined.slnx C:\code\my-repository --include '^Team-' --exclude '-Tests$'
```

The combine workflow can also be invoked through the explicit `combine` command:

```powershell
slnx-combine combine combined.slnx C:\code\my-repository
```

When running the project directly:

```powershell
dotnet run --project src/SlnxCombiner/SlnxCombiner.csproj -- combined.slnx C:\code\my-repository
```

The output file is excluded from the discovered input solutions. Files found through overlapping traversal directories are de-duplicated before they are combined. To regenerate an existing combined solution in place, pass `--overwrite`.

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
  <!--Projects that were duplicates-->
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

## Publish

Releases are published manually through the **Publish NuGet package** GitHub Actions workflow using NuGet trusted publishing. Before the first release:

1. Add a nuget.org trusted publishing policy with repository owner `jeppevammenkristensen`, repository `slnx-combiner`, workflow file `publish-nuget.yml`, and no environment.
2. Ensure the GitHub Actions repository variable `NUGET_USER` is set to the package owner's nuget.org profile username (`jeppev`).
3. Open **Actions**, select **Publish NuGet package**, choose **Run workflow**, and enter the package version to publish.

The workflow tests the solution, packs the requested version, exchanges GitHub's OIDC token for a short-lived NuGet API key, and publishes the package without a stored API-key secret.
