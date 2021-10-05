using System;
using System.Threading;
using System.Threading.Tasks;
using FastEnumUtility;
using Pixsper.DisguiseDmxTableGen.Resolume;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Pixsper.DisguiseDmxTableGen
{
    class SelectCommand : AsyncCommand
    {
        public override async Task<int> ExecuteAsync(CommandContext context)
        {
            var prompt = new SelectionPrompt<InputFileFormatKind>()
                .UseConverter(v => v.GetLabel(throwIfNotFound: true)!)
                .Title("Select an input pixelmap format and press ENTER");

            prompt.AddChoice(InputFileFormatKind.Resolume);

            var selection = await prompt.ShowAsync(AnsiConsole.Console, CancellationToken.None);

            switch (selection)
            {
                case InputFileFormatKind.Resolume:
                    return await new ResolumeCommand().ExecuteAsync(context, new ResolumeCommandSettings());
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public enum InputFileFormatKind
        {
            [Label("Resolume")]
            Resolume
        }
    }
}
