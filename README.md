# slnx-combiner

`slnx-combiner` is a small .NET CLI that combines multiple XML solution (`.slnx`) files into one.

It rebases each project path relative to the output file, merges matching solution folders, and includes duplicate project paths once.

## Usage

```powershell
dotnet run --project src/SlnxCombiner -- --output combined.slnx team-a.slnx team-b.slnx
```

Use `--force` to overwrite an existing output file:

```powershell
dotnet run --project src/SlnxCombiner -- --output combined.slnx --force team-a.slnx team-b.slnx
```

Only `.slnx` files are accepted as inputs and output.

## Build and test

```powershell
dotnet test
```
