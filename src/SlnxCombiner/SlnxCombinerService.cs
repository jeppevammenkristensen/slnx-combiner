using System.Collections.Immutable;
using System.IO.Abstractions;
using System.Xml.Linq;
using FileBasedApp.Toolkit;
using SlnxCombiner.Commands;
using Spectre.Console;
using TruePath;
using PathMatch = (TruePath.AbsolutePath Path, SlnxCombiner.Commands.TypeToCombine Type);

/// <summary>
/// Discovers solution files and combines their projects into a generated SLNX document.
/// </summary>
public class SlnxCombinerService
{
    private readonly IFileSystem _fileSystem;
    private readonly IAnsiConsole _console;

    /// <summary>
    /// Initializes a new instance of the <see cref="SlnxCombinerService"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system used to discover and write solution files.</param>
    /// <param name="console">The console used to report changes to the requested output path.</param>
    public SlnxCombinerService(IFileSystem fileSystem, IAnsiConsole console)
    {
        _fileSystem = fileSystem;
        _console = console;
    }

    /// <summary>
    /// Combines projects from the configured directory into the requested SLNX output file.
    /// </summary>
    /// <param name="settings">The validated command settings that define the input directory and output file.</param>
    /// <param name="cancellationToken">A token that can cancel writing the generated file.</param>
    public async Task Combine(CombineCommand.Settings settings, CancellationToken cancellationToken)
    {
        var destination = settings.OutputFilePath;

        if (destination.GetExtensionWithoutDot() != "slnx")
        {
            _console.MarkupLineInterpolated(
                $"File extension must be .slnx. Found {destination.GetExtensionWithoutDot()}. Changed to .slnx");
            destination = destination / ".." / $"{destination.GetFilenameWithoutExtension()}.slnx";
        }

        if (!settings.Overwrite && _fileSystem.File.Exists(destination.Value))
        {
            throw new InvalidOperationException($"Output file already exists: {destination}. Set --overwrite to overwrite the file.");
        }

        var solutionFiles = settings.TraversePath.SelectMany(path => FindSolutionFiles(path, destination, settings)).Distinct().ToArray();
        if (solutionFiles.Length == 0)
        {
            throw new InvalidOperationException("No solution files found in this directory or it's subdirectories");
        }

        OutputMatchedFiles(solutionFiles);

        var buildSlnx = await BuildSlnx(solutionFiles, destination / "..");
        await BuildFileFromBuilder(buildSlnx, destination, cancellationToken);
    }

    private void OutputMatchedFiles(PathMatch[] solutionFiles)
    {
        _console.MarkupLineInterpolated($"[green]Found {solutionFiles.Length} files[/]");
        foreach (var solutionFile in solutionFiles)
        {
            _console.MarkupLineInterpolated($"[dim]{solutionFile.Path} ({solutionFile.Type})[/]");
        }
    }

    private async Task BuildFileFromBuilder(SlnxBuilder buildSlnx, AbsolutePath destination,
        CancellationToken cancellationToken)
    {
        using var fileSystemStream = _fileSystem.File.Create(destination.Value);
        var buildResult = buildSlnx.Build();
        await buildResult.SaveAsync(fileSystemStream, SaveOptions.None, cancellationToken);
    }


    private async Task<SlnxBuilder> BuildSlnx(PathMatch[] solutionFiles, AbsolutePath root)
    {
        var builder = new SlnxBuilder(root);

        HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var processed = ConsolidateDuplicateProjects(await ProcessItems(solutionFiles, root, names));

        await ProcessSolutionIt(builder, processed);

        return builder;
    }

    /// <summary>
    /// Moves projects included by multiple source solutions into a shared synthetic group.
    /// </summary>
    private ImmutableArray<SolutionWrapper> ConsolidateDuplicateProjects(IReadOnlyList<SolutionWrapper> solutionWrappers)
    {
        var solutionWrapper = new SolutionWrapper("", null, []);

        var duplicates = solutionWrappers.SelectMany(x => x.Projects)
            .GroupBy(x => x.ProjectPath)
            .Where(x => x.Count() > 1)
            .Select(x => x.First())
            .ToList();
        
        _console.MarkupLineInterpolated($"[green]Found the following projects referenced in multiple solutions: [/]");

        foreach (var duplicate in duplicates)
        {
            _console.MarkupLineInterpolated($"[dim]{duplicate.ProjectPath.Value}[/]");
        }

        solutionWrapper.AddRange(duplicates);

        foreach (var wrapper in solutionWrappers)
        {
            wrapper.Remove(duplicates);
        }

        return solutionWrapper.Empty ? [.. solutionWrappers] : [solutionWrapper, .. solutionWrappers];
    }

    private async Task<SolutionWrapper> ProcessSolution(AbsolutePath destination, HashSet<string> names)
    {
        var result = await SolutionParser.ReadSolutionAsync(destination, CancellationToken.None);

        var name = NameUtil.GetName(names, destination);


        return new SolutionWrapper(name, destination, result.SolutionProjects.Select(x =>
        {
            var filePath = LocalPath.Create(x.FilePath);
            if (!filePath.IsAbsolute)
            {
                filePath = destination / ".." / filePath;
            }

            return new ProjectReference(AbsolutePath.Create(filePath.Value), x.DisplayName, x.Type);
        }));
    }
    
    private async Task<IReadOnlyList<SolutionWrapper>> ProcessItems(PathMatch[] pathMatches,
        AbsolutePath destination, HashSet<string> names)
    {
        List<ProjectReference> projectReferences = [];
        List<SolutionWrapper> solutionWrappers = [];
        
        foreach (var b in pathMatches)
        {
            var s = b.Path;

            if (s.Equals(destination)) continue;

            switch (b.Type)
            {
                case TypeToCombine.Solution:
                    solutionWrappers.Add(await ProcessSolution(s, names));
                    break;
                case TypeToCombine.Project:
                    projectReferences.Add(new ProjectReference(s, null, null));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        var processProjectReferences = new List<ProjectReference>();
        
        foreach (var references in projectReferences.OrderBy(x => x.ProjectPath.Value)
                     .GroupBy(x => x.ProjectPath.FileName, StringComparer.OrdinalIgnoreCase))
        {
            if (references.Count() > 1)
            {
                foreach (var projectReference in references)
                {
                    solutionWrappers.Add(new SolutionWrapper(NameUtil.GetName(names,projectReference.ProjectPath), null, [projectReference]));
                }
            }
            else
            {
                processProjectReferences.Add(references.First());
            }
        }

        
        

        return [new SolutionWrapper("", null, processProjectReferences), .. solutionWrappers];
    }

    private async Task ProcessSolutionIt(SlnxBuilder builder, ImmutableArray<SolutionWrapper> wrappers)
    {
        foreach (var solutionWrapper in wrappers)
        {
            var folderBuilder = builder.NewSolutionFolder(solutionWrapper.Name, solutionWrapper.SolutionFile);

            foreach (var resultSolutionProject in solutionWrapper.Projects)
            {
                folderBuilder.AddProject(resultSolutionProject.ProjectPath, resultSolutionProject.DisplayName,
                    resultSolutionProject.Type);
            }

            folderBuilder.AddTo(builder);
        }
    }


    private PathMatch[] FindSolutionFiles(AbsolutePath destinationDirectory, AbsolutePath existingFile,
        CombineCommand.Settings settings)
    {
        var includeFilter = GenerateIncludeFilter(settings);
        var excludeFilter = GenerateExcludeFilter(settings);

        
        switch (settings.Type)
        {
            case TypeToCombine.Solution:
                return
                [
                    .. destinationDirectory.GetAllFiles("*.slnx", _fileSystem)
                        .Concat(destinationDirectory.GetAllFiles("*.sln", _fileSystem))
                        .Where(includeFilter)
                        .Where(excludeFilter)
                        .Where(x => !x.Equals(existingFile))
                        .Select(x => (x, TypeToCombine.Solution))
                ];
            case TypeToCombine.Project:
                return
                [
                    .. destinationDirectory.GetAllFiles("*.csproj", _fileSystem)
                        .Where(includeFilter)
                        .Where(excludeFilter)
                        .Where(x => !x.Equals(existingFile))
                        .Select(x => (x, TypeToCombine.Project))
                ];
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        
        
    }

    internal static Func<AbsolutePath, bool> GenerateExcludeFilter(CombineCommand.Settings settings)
    {
        if (settings.ExcludeRegex is not {} regex)
            return _ => true;

        return x => !regex.IsMatch(x.GetFilenameWithoutExtension());
    }

    internal static Func<AbsolutePath, bool> GenerateIncludeFilter(CombineCommand.Settings settings)
    {
        if (settings.IncludeRegex is not {} regex)
            return _ => true;

        return x => regex.IsMatch(x.GetFilenameWithoutExtension());
    }
}

/// <summary>
/// Describes a project entry read from a solution file.
/// </summary>
/// <param name="ProjectPath">The absolute path to the project file.</param>
/// <param name="DisplayName">The optional display name stored in the solution.</param>
/// <param name="Type">The optional project type stored in the solution.</param>
public record ProjectReference(AbsolutePath ProjectPath, string? DisplayName, string? Type);

/// <summary>
/// Groups the projects contributed by one source solution.
/// </summary>
public class SolutionWrapper
{
    /// <summary>
    /// Gets the path of the source solution, or <see langword="null"/> for a synthetic group.
    /// </summary>
    public AbsolutePath? SolutionFile { get; }

    private ImmutableDictionary<AbsolutePath, ProjectReference> ProjectPaths { get; set; }

    /// <summary>
    /// Gets the projects currently assigned to the group.
    /// </summary>
    public IEnumerable<ProjectReference> Projects => ProjectPaths.Values;

    /// <summary>
    /// Gets a value indicating whether the group contains no projects.
    /// </summary>
    public bool Empty => ProjectPaths.Count == 0;

    /// <summary>
    /// Initializes a new project group for a source solution.
    /// </summary>
    /// <param name="name">The solution-folder name used in the combined output.</param>
    /// <param name="solutionFile">The source solution path, or <see langword="null"/> for a synthetic group.</param>
    /// <param name="projectFiles">The projects initially assigned to the group.</param>
    public SolutionWrapper(string name, AbsolutePath? solutionFile, IEnumerable<ProjectReference> projectFiles)
    {
        Name = name;
        SolutionFile = solutionFile;
        ProjectPaths = projectFiles.ToImmutableDictionary(x => x.ProjectPath);
    }

    /// <summary>
    /// Gets the solution-folder name used in the combined output.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Determines whether the group contains the specified project path.
    /// </summary>
    /// <param name="projectFile">The absolute project path to locate.</param>
    /// <returns><see langword="true"/> when the project belongs to the group; otherwise, <see langword="false"/>.</returns>
    public bool Contains(AbsolutePath projectFile) => ProjectPaths.ContainsKey(projectFile);

    /// <summary>
    /// Removes the project with the specified path from the group.
    /// </summary>
    /// <param name="projectFile">The absolute path of the project to remove.</param>
    public void Remove(AbsolutePath projectFile) => ProjectPaths = ProjectPaths.Remove(projectFile);

    /// <summary>
    /// Removes the specified project references from the group.
    /// </summary>
    /// <param name="duplicates">The project references to remove.</param>
    public void Remove(IEnumerable<ProjectReference> duplicates) =>
        ProjectPaths = ProjectPaths.RemoveRange(duplicates.Select(x => x.ProjectPath));

    /// <summary>
    /// Adds the specified project references to the group.
    /// </summary>
    /// <param name="duplicates">The project references to add.</param>
    public void AddRange(IEnumerable<ProjectReference> duplicates)
    {
        ProjectPaths = ProjectPaths.AddRange(duplicates.ToImmutableDictionary(x => x.ProjectPath));
    }
}
