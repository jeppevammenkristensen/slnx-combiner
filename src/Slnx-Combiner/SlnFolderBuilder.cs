using System.Collections.Immutable;
using System.Xml.Linq;
using TruePath;

public class SlnFolderBuilder
{
    private readonly string _name;
    private readonly AbsolutePath _rootFolder;

    public SlnFolderBuilder(string name, AbsolutePath rootFolder, AbsolutePath? solutionFile)
    {
        _name = name;
        _rootFolder = rootFolder;
        SolutionFile = solutionFile;
    }

    private ImmutableArray<SlnProject> _projects = ImmutableArray<SlnProject>.Empty;
    public AbsolutePath? SolutionFile { get; }

    public SlnFolderBuilder AddProject(AbsolutePath projectPath, string? displayName, string? type)
    {
        var relativeTo = projectPath.RelativeTo(_rootFolder);
        _projects = _projects.Add(new SlnProject(relativeTo.Value.Replace("\\", "/"), displayName, type));
        return this;
    }

    public void AddTo(SlnxBuilder builder)
    {
        builder.AddFolder(this);
    }

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