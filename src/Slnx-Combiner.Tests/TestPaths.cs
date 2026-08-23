using System.IO;
using TruePath;

namespace Slnx_Combiner.Tests;

internal static class TestPaths
{
    public static AbsolutePath Absolute(params string[] segments)
    {
        var path = Path.Combine(Path.GetTempPath(), "Slnx-Combiner.Tests");
        foreach (var segment in segments)
        {
            path = Path.Combine(path, segment);
        }

        return AbsolutePath.Create(path);
    }
}