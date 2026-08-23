using System.Text.RegularExpressions;
using TruePath;

/// <summary>
/// Creates unique solution-folder names from solution file paths.
/// </summary>
public static partial class NameUtil
{
    /// <summary>
    /// Gets an unused name derived from a solution file and reserves it in the supplied set.
    /// </summary>
    /// <param name="names">The set of names that have already been reserved.</param>
    /// <param name="solutionFile">The solution file whose filename provides the base name.</param>
    /// <returns>The reserved unique name.</returns>
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
