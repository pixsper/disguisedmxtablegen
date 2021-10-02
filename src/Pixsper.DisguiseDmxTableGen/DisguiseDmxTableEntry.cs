using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration.Attributes;

namespace Pixsper.DisguiseDmxTableGen
{
    record DisguiseDmxTable
    {
        public void WriteToFile(string path)
        {
            using var writer = new StreamWriter(path);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            csv.WriteRecords(Entries);
        }

        public ImmutableList<Entry> Entries {get; init; } = ImmutableList<Entry>.Empty;

        public record Entry
        {
            [Name("x"), Index(0)]
            public int X { get; init; }

            [Name("y"), Index(1)]
            public int Y { get; init; }

            [Name("universe"), Index(2)]
            public int UniverseIndex { get; init; } = 1;

            [Name("channel"), Index(3)]
            public int StartChannel { get; init; } = 1;
        }
    }
}
