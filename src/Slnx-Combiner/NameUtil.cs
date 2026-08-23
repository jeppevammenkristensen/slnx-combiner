using System.Text.RegularExpressions;
using TruePath;

public static partial class NameUtil
{
    public static string GetName(HashSet<string> names, AbsolutePath solutionFile)
    {
        var candidate = solutionFile.GetFilenameWithoutExtension();
        if (!names.Add(candidate))
        {
            int i = 1;
            var modifiedCandidate = $"{candidate}_{i}";

            while (names.Contains(modifiedCandidate))
            {
                i++;
                modifiedCandidate = $"{candidate}_{i}";
            }

            names.Add(modifiedCandidate);
            return modifiedCandidate;
        }

        return candidate;
    }
}