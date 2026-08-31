using System.Reflection;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Help;
using Spectre.Console.Rendering;

namespace SlnxCombiner;

/// <summary>
/// Extends the standard command help with values discovered from enum-typed command options.
/// </summary>
/// <remarks>
/// This proof of concept receives the settings types explicitly because Spectre.Console.Cli's
/// public help model does not expose the settings type or the property behind an option.
/// </remarks>
public sealed class EnumValueHelpProvider : HelpProvider
{
    private readonly IReadOnlyList<EnumOption> _enumOptions;

    /// <summary>
    /// Initializes an enum-aware help provider for the supplied command settings types.
    /// </summary>
    /// <param name="settings">The command application settings used by the standard help provider.</param>
    /// <param name="settingsTypes">Settings types whose enum options should be documented.</param>
    public EnumValueHelpProvider(ICommandAppSettings settings, params Type[] settingsTypes)
        : base(settings)
    {
        _enumOptions = settingsTypes.SelectMany(DiscoverEnumOptions).ToArray();
    }

    /// <summary>
    /// Renders the standard options section followed by valid values for enum options.
    /// </summary>
    public override IEnumerable<IRenderable> GetOptions(ICommandModel model, ICommandInfo? command)
    {
        foreach (var renderable in base.GetOptions(model, command))
        {
            yield return renderable;
        }

        var visibleEnumOptions = _enumOptions
            .Where(enumOption => command?.Parameters
                .OfType<ICommandOption>()
                .Any(enumOption.Matches) == true)
            .ToArray();

        if (visibleEnumOptions.Length == 0)
        {
            yield break;
        }

        var grid = new Grid();
        grid.AddColumn(new GridColumn {Padding = new Padding(4, 4), NoWrap = true});
        grid.AddColumn(new GridColumn {Padding = new Padding(0, 0)});

        foreach (var option in visibleEnumOptions)
        {
            grid.AddRow(
                new Text($"Valid values for {option.DisplayName}"),
                new Text(string.Join(", ", option.Values)));
        }

        yield return grid;
    }

    /// <summary>
    /// Discovers enum-backed options declared by a settings type.
    /// </summary>
    private static IEnumerable<EnumOption> DiscoverEnumOptions(Type settingsType)
    {
        foreach (var property in settingsType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var attribute = property.GetCustomAttribute<CommandOptionAttribute>();
            var optionType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            if (attribute == null || !optionType.IsEnum)
            {
                continue;
            }

            var names = attribute.LongNames.Concat(attribute.ShortNames).ToHashSet(StringComparer.Ordinal);
            var displayName = attribute.LongNames.Count > 0
                ? $"--{attribute.LongNames[0]}"
                : $"-{attribute.ShortNames[0]}";

            yield return new EnumOption(displayName, names, Enum.GetNames(optionType));
        }
    }

    /// <summary>
    /// Describes an enum option and the values shown for it in help output.
    /// </summary>
    private sealed record EnumOption(string DisplayName, IReadOnlySet<string> Names, IReadOnlyList<string> Values)
    {
        /// <summary>
        /// Determines whether this reflected option corresponds to a help-model option.
        /// </summary>
        public bool Matches(ICommandOption option) =>
            option.LongNames.Concat(option.ShortNames).Any(Names.Contains);
    }
}