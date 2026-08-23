using Microsoft.VisualStudio.SolutionPersistence;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using TruePath;

/// <summary>
/// Reads supported Visual Studio solution formats into solution models.
/// </summary>
public class SolutionParser
{

    /// <summary>
    /// Reads a solution or SLNX file using the serializer selected from its extension.
    /// </summary>
    /// <param name="path">The absolute path of the solution file to read.</param>
    /// <param name="cancellationToken">A token that can cancel the read operation.</param>
    /// <returns>The parsed solution model.</returns>
    /// <exception cref="ArgumentException">The file extension does not identify a supported solution format.</exception>
    /// <exception cref="InvalidOperationException">The solution contents are invalid.</exception>
    public static async Task<SolutionModel> ReadSolutionAsync(AbsolutePath path, CancellationToken cancellationToken)
    {
        ISolutionSerializer? serializer =
            SolutionSerializers.GetSerializerByMoniker(path.Value);

        if (serializer is null)
            throw new ArgumentException("Expected a .sln or .slnx file.", nameof(path));

        try
        {
            SolutionModel solution =
                await serializer.OpenAsync(path.Value, cancellationToken);

            return solution;
        }
        catch (SolutionException ex)
        {
            throw new InvalidOperationException(
                $"Invalid solution at {ex.Line}:{ex.Column}", ex);
        }
    }
    
}
