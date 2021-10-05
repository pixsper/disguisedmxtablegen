using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Pixsper.DisguiseDmxTableGen.Resolume
{
    class ResolumeCommandSettings : CommandSettings
    {
        [CommandOption("-i|--input")]
        [Description("Optional path to input file, if not set the user will be invited to select from the Resolume presets available on the local filesystem")]
        public string? InputFilePath { get; set; }

        [CommandOption("-o|--output")]
        [Description("Optional path to output file, if not set the output file will be written to the same directory as the input file")]
        public string? OutputFilePath { get; set; }
    }

    class ResolumeCommand : AsyncCommand<ResolumeCommandSettings>
    {
        private readonly string _resolumeDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Resolume Arena");

        public override async Task<int> ExecuteAsync(CommandContext context, ResolumeCommandSettings settings)
        {
            string resolvedInputFilepath;

            if (settings.InputFilePath is null)
            {
                if (!Directory.Exists(_resolumeDirectoryPath))
                {
                    AnsiConsole.MarkupLine("[bold red]Couldn't find Resolume directory in documents[/]");
                    return -1;
                }

                var resolumeAdvancedOutputPresetsPath = Path.Combine(_resolumeDirectoryPath, "Presets", "Advanced Output");

                if (!Directory.Exists(resolumeAdvancedOutputPresetsPath))
                {
                    AnsiConsole.MarkupLine("[bold red]Couldn't find advanced output presets directory in Resolume directory[/]");
                    return -1;
                }

                var outputPresetsXmlFilePaths = Directory.GetFiles(resolumeAdvancedOutputPresetsPath, "*.xml").ToImmutableList();
                if (outputPresetsXmlFilePaths.IsEmpty)
                {
                    AnsiConsole.MarkupLine("[bold red]Couldn't find any advanced output preset files in the Resolume directory[/]");
                    return -1;
                }

                var choices = outputPresetsXmlFilePaths.ToDictionary(
                    s => Path.GetFileNameWithoutExtension(s)!);

                var choiceKey = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select a Resolume advanced output preset to convert and press ENTER")
                        .AddChoices(choices.Keys));

                resolvedInputFilepath = choices[choiceKey];
            }
            else
            {
                resolvedInputFilepath = Path.GetFullPath(settings.InputFilePath);
            }
            

            AnsiConsole.MarkupLine($"Converting Resolume advanced output preset '{Path.GetFileNameWithoutExtension(resolvedInputFilepath)}'..");

            ResolumePixelMap pixelmap;

            try
            {
                var fileStream = File.OpenRead(resolvedInputFilepath);
                var doc = await XDocument.LoadAsync(fileStream, LoadOptions.None, default);
                pixelmap = ResolumePixelMap.FromXml(doc);
            }
            // ReSharper disable once CatchAllClause
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[bold red]Failed to read preset file from path '{resolvedInputFilepath}' - {ex.Message}[/]");
                return -1;
            }

            if (pixelmap.Fixtures.Select(f => f.ColorFormat).Distinct().Count() > 1)
            {
                AnsiConsole.MarkupLine($"[bold red]Can't convert this preset as it contains fixtures with different color formats. " +
                                       $"Disguise cannot support a single DMX table with multiple color formats[/]");
                return -1;
            }

            var dmxTable = pixelmap.ComputeDmxTable();

            AnsiConsole.MarkupLine($"[bold green]Conversion finished, created DMX table with {dmxTable.Entries.Count} entries[/]");

            string resolvedOutputFilePath;

            if (settings.OutputFilePath is null)
            {
                resolvedOutputFilePath = Path.Combine(Path.GetDirectoryName(resolvedInputFilepath)!,
                    $"{Path.GetFileNameWithoutExtension(resolvedInputFilepath)}.csv");
            }
            else
            {
                resolvedOutputFilePath = Path.GetFullPath(settings.OutputFilePath);

                var outputFileDirectory = Path.HasExtension(resolvedOutputFilePath)
                    ? Path.GetDirectoryName(resolvedOutputFilePath)
                    : resolvedOutputFilePath;

                if (outputFileDirectory is not null)
                {
                    if (!Directory.Exists(outputFileDirectory))
                        Directory.CreateDirectory(outputFileDirectory);
                }

                if (outputFileDirectory is null || outputFileDirectory == resolvedOutputFilePath)
                {
                    resolvedOutputFilePath = Path.Combine(resolvedOutputFilePath,
                        $"{Path.GetFileNameWithoutExtension(resolvedInputFilepath)}.csv");
                }
            }

            dmxTable.WriteToFile(resolvedOutputFilePath);

            AnsiConsole.MarkupLine($"CSV file written to {resolvedOutputFilePath}");

            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine("[bold]Press any key to exit[/]");
            Console.ReadKey(true);

            return 0;
        }
    }
}
