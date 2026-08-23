using System.Collections.Immutable;
using System.Xml.Linq;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using TruePath;

/// <summary>
/// Builds the XML representation of a combined SLNX solution.
/// </summary>
public class SlnxBuilder
{
    private readonly AbsolutePath _rootFolder;
    private ImmutableArray<SlnFolderBuilder> _folders = ImmutableArray<SlnFolderBuilder>.Empty;
    /// <summary>
    /// Contains the solution-folder names reserved by this builder.
    /// </summary>
    public ImmutableHashSet<string> SolutionFolders = ImmutableHashSet.Create<string>();
    
    /// <summary>
    /// Initializes a new instance of the <see cref="SlnxBuilder"/> class.
    /// </summary>
    /// <param name="rootFolder">The root path used to make project paths relative.</param>
    public SlnxBuilder(AbsolutePath rootFolder)
    {
        _rootFolder = rootFolder;
    }

    /// <summary>
    /// Adds a configured solution folder to the output.
    /// </summary>
    /// <param name="folder">The solution folder to add.</param>
    public void AddFolder(SlnFolderBuilder folder)
    {
        _folders = _folders.Add(folder);
    }
    
    /// <summary>
    /// Builds the complete SLNX XML document element.
    /// </summary>
    /// <returns>The root solution element containing all configured folders and projects.</returns>
    public XElement Build()
    {
        XElement element = new XElement("Solution");

        foreach (var folder in _folders)
        {
            folder.Build(element);
        }
        
        return element;
    }

    /// <summary>
    /// Creates a solution-folder builder rooted at this builder's output directory.
    /// </summary>
    /// <param name="name">The name of the solution folder.</param>
    /// <param name="solutionFile">The source solution path associated with the folder.</param>
    /// <returns>A new solution-folder builder.</returns>
    public SlnFolderBuilder NewSolutionFolder(string name, AbsolutePath? solutionFile)
    {
        return new SlnFolderBuilder(name, _rootFolder, solutionFile);
    }
}

