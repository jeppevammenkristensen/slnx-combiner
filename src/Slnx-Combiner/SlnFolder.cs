using System.Collections.Immutable;

internal record SlnFolder(string Path, ImmutableArray<SlnProject> Projects)
{
    
}