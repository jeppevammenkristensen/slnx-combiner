/// <summary>
/// Represents a project included in a solution.
/// </summary>
internal record SlnProject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SlnProject"/> record.
    /// </summary>
    /// <param name="path">The project path relative to the solution file.</param>
    /// <param name="displayName">The optional name displayed for the project.</param>
    /// <param name="type">The optional project type.</param>
    internal SlnProject(string path, string? displayName, string? type)
    {
        Path = path;
        DisplayName = displayName;
        Type = type;
    }

    /// <summary>
    /// Gets the project path relative to the solution file.
    /// </summary>
    public string Path { get; init; }

    /// <summary>
    /// Gets the optional name displayed for the project.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets the optional project type.
    /// </summary>
    public string? Type { get; init; }
}
