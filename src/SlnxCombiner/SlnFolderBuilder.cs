using System.Collections.Immutable;
using System.Xml.Linq;
using TruePath;

/// <summary>
/// Builds one solution folder and its project entries in an SLNX document.
/// </summary>
public class SlnFolderBuilder
{
    private readonly string _name;
    private readonly AbsolutePath _rootFolder;

    /// <summary>
    /// Initializes a new instance of the <see cref="SlnFolderBuilder"/> class.
    /// </summary>
    /// <param name="name">The name assigned to the solution folder.</param>
    /// <param name="rootFolder">The root path used to make project paths relative.</param>
    /// <param name="solutionFile">The source solution path associated with this folder.</param>
    public SlnFolderBuilder(string name, AbsolutePath rootFolder, AbsolutePath? solutionFile)
    {
        _name = name;
        _rootFolder = rootFolder;
        SolutionFile = solutionFile;
    }

    private ImmutableArray<SlnProject> _projects = ImmutableArray<SlnProject>.Empty;
    /// <summary>
    /// Gets the source solution path associated with this folder.
    /// </summary>
    public AbsolutePath? SolutionFile { get; }

    /// <summary>
    /// Adds a project entry to the solution folder.
    /// </summary>
    /// <param name="projectPath">The absolute path to the project file.</param>
    /// <param name="displayName">The optional display name written for the project.</param>
    /// <param name="type">The optional project type written for the project.</param>
    /// <returns>This builder so additional projects can be added.</returns>
    public SlnFolderBuilder AddProject(AbsolutePath projectPath, string? displayName, string? type)
    {
        var relativeTo = projectPath.RelativeTo(_rootFolder);
        _projects = _projects.Add(new SlnProject(relativeTo.Value.Replace("\\", "/"), displayName, type));
        return this;
    }

    /// <summary>
    /// Adds this solution folder to the specified SLNX builder.
    /// </summary>
    /// <param name="builder">The builder that receives this folder.</param>
    public void AddTo(SlnxBuilder builder)
    {
        builder.AddFolder(this);
    }

    /// <summary>
    /// Appends this folder and its project entries to a solution XML element.
    /// </summary>
    /// <param name="element">The root solution element to update.</param>
    public void Build(XElement element)
    {
        XElement folderElement = element;

        var nameEmpty = string.IsNullOrWhiteSpace(_name);
        if (!nameEmpty)
        {
            element.Add(new XComment($"Folder: {SolutionFile?.RelativeTo(_rootFolder).Value}"));
            folderElement = new XElement("Folder");
            folderElement.SetAttributeValue("Name", "/" + _name.Trim('/') + "/");
        }
        else
        {
            element.Add(new XComment($"Projects that where duplicates"));            
        }


        foreach (var project in _projects)
        {
            var projectElement = new XElement("Project");
            projectElement.SetAttributeValue("Path", project.Path);
            if (!string.IsNullOrWhiteSpace(project.DisplayName))
            {
                projectElement.SetAttributeValue("DisplayName", project.DisplayName);
            }

            if (!string.IsNullOrWhiteSpace(project.Type))
            {
                projectElement.SetAttributeValue("Type", project.Type);
            }

            folderElement.Add(projectElement);
        }

        if (!nameEmpty)
        {
            element.Add(folderElement);
        }
    }
}
