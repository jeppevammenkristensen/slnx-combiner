using Microsoft.VisualStudio.SolutionPersistence;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using TruePath;

public class SolutionParser
{

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