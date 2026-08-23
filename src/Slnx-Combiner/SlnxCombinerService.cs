using System.Collections.Immutable;
using System.IO.Abstractions;
using System.Xml.Linq;
using FileBasedApp.Toolkit;
using Spectre.Console;
using TruePath;

public class SlnxCombinerService
{
    private readonly IFileSystem _fileSystem;
    private readonly IAnsiConsole _console;

    public SlnxCombinerService(IFileSystem fileSystem, IAnsiConsole console)
    {
        _fileSystem = fileSystem;
        _console = console;
    }

    public async Task Combine(RunCommand.Settings settings, CancellationToken cancellationToken)
    {
        var destination = settings.OutputFilePath;

        if (destination.GetExtensionWithoutDot() != "slnx")
        {
            _console.MarkupLineInterpolated(
                $"File extension must be .slnx. Found {destination.GetExtensionWithoutDot()}. Changed to .slnx");
            destination = destination / ".." /
                          $"{destination.GetFilenameWithoutExtension()}.slnx";
        }

        var solutionFiles = FindSolutionFiles(settings.TraversePath, destination);
        if (solutionFiles.Length == 0)
        {
            throw new InvalidOperationException("No solution files found in this direcotry or it's subdirectories");
        }

        var buildSlnx = await BuildSlnx(solutionFiles, destination / "..");
        await BuildFileFromBuilder(buildSlnx, destination, cancellationToken);
    }

    private async Task BuildFileFromBuilder(SlnxBuilder buildSlnx, AbsolutePath destination,
        CancellationToken cancellationToken)
    {
        using var fileSystemStream = _fileSystem.File.Create(destination.Value);
        var buildResult = buildSlnx.Build();
        await buildResult.SaveAsync(fileSystemStream, SaveOptions.None, cancellationToken);
    }


    private async Task<SlnxBuilder> BuildSlnx(AbsolutePath[] solutionFiles, AbsolutePath root)
    {
        var builder = new SlnxBuilder(root);

        HashSet<string> names = new HashSet<string>();
        var processed = FindDuplicates(await ProcessSolution(solutionFiles, root, names).ToListAsync());

        await ProcessSolutionIt(builder, processed);

        return builder;
    }

    private ImmutableArray<SolutionWrapper> FindDuplicates(List<SolutionWrapper> solutionWrappers)
    {
        var solutionWrapper = new SolutionWrapper("", null, []);

        var duplicates = solutionWrappers.SelectMany(x => x.Projects)
            .GroupBy(x => x.ProjectPath)
            .Where(x => x.Count() > 1)
            .Select(x => x.First())
            .ToList();

        solutionWrapper.AddRange(duplicates);

        foreach (var wrapper in solutionWrappers)
        {
            wrapper.Remove(duplicates);
        }

        return solutionWrapper.Empty ? [.. solutionWrappers] : [solutionWrapper, .. solutionWrappers];
    }

    private async IAsyncEnumerable<SolutionWrapper> ProcessSolution(AbsolutePath[] solutionFile,
        AbsolutePath destination, HashSet<string> names)
    {
        foreach (var s in solutionFile)
        {
            if (s.Equals(destination)) continue;

            var result = await SolutionParser.ReadSolutionAsync(s, CancellationToken.None);

            var name = NameUtil.GetName(names, s);


            yield return new SolutionWrapper(name, s, result.SolutionProjects.Select(x =>
            {
                var filePath = LocalPath.Create(x.FilePath);
                if (!filePath.IsAbsolute)
                {
                    filePath = s / ".." / filePath;
                }

                return new ProjectReference(AbsolutePath.Create(filePath.Value), x.DisplayName, x.Type);
            }));
        }
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


    private AbsolutePath[] FindSolutionFiles(AbsolutePath destinationDirectory, AbsolutePath existingFile)
    {
        return
        [
            .. destinationDirectory.GetAllFiles("*.slnx", _fileSystem)
                .Concat(destinationDirectory.GetAllFiles("*.sln", _fileSystem))
                .Where(x => !x.Equals(existingFile))
        ];
    }
}

public record ProjectReference(AbsolutePath ProjectPath, string? DisplayName, string? Type);

public class SolutionWrapper
{
    public AbsolutePath? SolutionFile { get; }

    private ImmutableDictionary<AbsolutePath, ProjectReference> ProjectPaths { get; set; }

    public IEnumerable<ProjectReference> Projects => ProjectPaths.Values;

    public bool Empty => ProjectPaths.Count == 0;

    public SolutionWrapper(string name, AbsolutePath? solutionFile, IEnumerable<ProjectReference> projectFiles)
    {
        Name = name;
        SolutionFile = solutionFile;
        ProjectPaths = projectFiles.ToImmutableDictionary(x => x.ProjectPath);
    }

    public string Name { get; private set; }

    public bool Contains(AbsolutePath projectFile) => ProjectPaths.ContainsKey(projectFile);

    public void Remove(AbsolutePath projectFile) => ProjectPaths = ProjectPaths.Remove(projectFile);

    public void Remove(IEnumerable<ProjectReference> duplicates) =>
        ProjectPaths = ProjectPaths.RemoveRange(duplicates.Select(x => x.ProjectPath));

    public void AddRange(IEnumerable<ProjectReference> duplicates)
    {
        ProjectPaths = ProjectPaths.AddRange(duplicates.ToImmutableDictionary(x => x.ProjectPath));
    }
}
