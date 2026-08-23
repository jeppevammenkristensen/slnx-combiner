using System;
using System.IO;
using TruePath;

namespace Slnx_Combiner.Tests;

internal static class TestPaths
{
    public static AbsolutePath CreateTemporaryFile(string fileName, string contents)
    {
        var directory = Path.Combine(Path.GetTempPath(), "Slnx-Combiner.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, fileName);
        File.WriteAllText(path, contents);
        return AbsolutePath.Create(path);
    }

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
