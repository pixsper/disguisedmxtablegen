using System.Reflection;
using Pixsper.DisguiseDmxTableGen.Resolume;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Pixsper.DisguiseDmxTableGen
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var app = new CommandApp<SelectCommand>();

            app.Configure(config =>
            {
                config.AddCommand<ResolumeCommand>("resolume");
            });

            AnsiConsole.Markup(@"[bold black on white]
                                               
     ____ _____  ______  ____  _____ ____      
    |  _ \_ _\ \/ / ___||  _ \| ____|  _ \     
    | |_) | | \  /\___ \| |_) |  _| | |_) |    
    |  __/| | /  \ ___) |  __/| |___|  _ <     
    |_|  |___/_/\_\____/|_|   |_____|_| \_\    
                                               
                                               
[/]");

            AnsiConsole.WriteLine();

            var version = Assembly.GetCallingAssembly().GetName().Version;
            if (version is not null)
                AnsiConsole.WriteLine($"Pixsper.DisguiseDmxTableGen v{version.Major}.{version.Minor}.{version.Revision}");

            AnsiConsole.WriteLine();

            return app.Run(args);
        }
    }
}