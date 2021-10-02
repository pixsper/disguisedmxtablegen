using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

    }

    class ResolumeCommand : AsyncCommand<ResolumeCommandSettings>
    {
        private readonly string _resolumeDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Resolume Arena");

        public override async Task<int> ExecuteAsync(CommandContext context, ResolumeCommandSettings settings)
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

            var outputPresetXmlFilePath = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a Resolume advanced output preset to convert and press ENTER")
                    .AddChoices(outputPresetsXmlFilePaths));

            AnsiConsole.MarkupLine($"Converting advanced output preset '{Path.GetFileNameWithoutExtension(outputPresetXmlFilePath)}'..");

            ResolumePixelMap pixelmap;

            try
            {
                var fileStream = File.OpenRead(outputPresetXmlFilePath);
                var doc = await XDocument.LoadAsync(fileStream, LoadOptions.None, default);
                pixelmap = ResolumePixelMap.FromXml(doc);
            }
            // ReSharper disable once CatchAllClause
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[bold red]Failed to read preset file from path '{outputPresetXmlFilePath}' - {ex.Message}[/]");
                return -1;
            }

            if (pixelmap.Fixtures.Select(f => f.ColorFormat).Distinct().Count() > 1)
            {
                AnsiConsole.MarkupLine($"[bold red]Can't convert this preset as it contains fixtures with different color formats. " +
                                       $"Disguise cannot support a single DMX table with multiple color formats[/]");
                return -1;
            }

            var dmxTable = pixelmap.ComputeDmxTable();

            AnsiConsole.MarkupLine($"Conversion finished, created DMX table with {dmxTable.Entries.Count} entries");

            var outputFilePath = Path.Combine(resolumeAdvancedOutputPresetsPath, 
                $"{Path.GetFileNameWithoutExtension(outputPresetXmlFilePath)}.csv");

            dmxTable.WriteToFile(outputFilePath);

            AnsiConsole.MarkupLine($"CSV file written to {outputFilePath}");

            return 0;
        }

        private async Task<ImmutableDictionary<Guid, ResolumeFixtureProfile>?> readFixtureProfileLibraryAsync()
        {
            AnsiConsole.Markup("Reading Resolume fixture profiles library...");

            var fixtureProfilesDirectoryPath = Path.Combine(_resolumeDirectoryPath, "Fixture Library");
            if (!Directory.Exists(fixtureProfilesDirectoryPath))
            {
                AnsiConsole.MarkupLine("[bold red]Couldn't find fixture library directory in Resolume directory[/]");
                return null;
            }

            var fixtureProfileXmlFilePaths = Directory.GetFiles(fixtureProfilesDirectoryPath, "*.xml").ToImmutableList();
            if (fixtureProfileXmlFilePaths.IsEmpty)
            {
                AnsiConsole.MarkupLine("[bold red]Couldn't find any fixture definition files in the Resolume fixture library directory[/]");
                return null;
            }

            var fixtureProfiles = new Dictionary<Guid, ResolumeFixtureProfile>();

            foreach (var filePath in fixtureProfileXmlFilePaths)
            {
                try
                {
                    var fileStream = File.OpenRead(filePath);
                    var doc = await XDocument.LoadAsync(fileStream, LoadOptions.None, default);
                    var fixtureProfile = ResolumeFixtureProfile.FromXml(doc);
                    fixtureProfiles.Add(fixtureProfile.Id, fixtureProfile);
                }
                // ReSharper disable once CatchAllClause
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[bold red]Failed to read fixture definition file from path '{filePath}' - {ex.Message}[/]");
                    return null;
                }
            }

            AnsiConsole.Markup($"{fixtureProfiles.Count} fixture profile(s) found");

            return fixtureProfiles.ToImmutableDictionary();
        }
    }
}
