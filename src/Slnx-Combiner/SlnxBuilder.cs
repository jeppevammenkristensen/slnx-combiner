using System.Collections.Immutable;
using System.Xml.Linq;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using TruePath;

public class SlnxBuilder
{
    private readonly AbsolutePath _rootFolder;
    private ImmutableArray<SlnFolderBuilder> _folders = ImmutableArray<SlnFolderBuilder>.Empty;
    public ImmutableHashSet<string> SolutionFolders = ImmutableHashSet.Create<string>();
    
    
    public SlnxBuilder(AbsolutePath rootFolder)
    {
        _rootFolder = rootFolder;
    }

    public void AddFolder(SlnFolderBuilder folder)
    {
        _folders = _folders.Add(folder);
    }
    
    public XElement Build()
    {
        XElement element = new XElement("Solution");

        foreach (var folder in _folders)
        {
            folder.Build(element);
        }
        
        return element;
    }

    public SlnFolderBuilder NewSolutionFolder(string name, AbsolutePath? solutionFile)
    {
        return new SlnFolderBuilder(name, _rootFolder, solutionFile);
    }
}

