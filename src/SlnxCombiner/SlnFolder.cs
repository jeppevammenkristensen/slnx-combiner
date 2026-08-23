using System.Collections.Immutable;

/// <summary>
/// Represents a solution folder and the projects it contains.
/// </summary>
internal record SlnFolder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SlnFolder"/> record.
    /// </summary>
    /// <param name="path">The solution folder path.</param>
    /// <param name="projects">The projects contained in the folder.</param>
    internal SlnFolder(string path, ImmutableArray<SlnProject> projects)
    {
        Path = path;
        Projects = projects;
    }

    /// <summary>
    /// Gets the solution folder path.
    /// </summary>
    public string Path { get; init; }

    /// <summary>
    /// Gets the projects contained in the folder.
    /// </summary>
    public ImmutableArray<SlnProject> Projects { get; init; }
}
