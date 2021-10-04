using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Xml;
using System.Xml.Linq;

namespace Pixsper.DisguiseDmxTableGen.Resolume
{
    record ResolumePixelMap
    {
        public static ResolumePixelMap FromXml(XDocument doc)
        {
            var elRoot = doc.Root;
            if (elRoot is null)
                throw new XmlException();

            var name = elRoot.AttributeAsString("name");

            var elScreenSetup = elRoot.RequiredElement("ScreenSetup");
            var elScreenSetupCurrentCompositionTextureSize = elScreenSetup.RequiredElement("CurrentCompositionTextureSize");

            var width = elScreenSetupCurrentCompositionTextureSize.AttributeAsInt("width");
            var height = elScreenSetupCurrentCompositionTextureSize.AttributeAsInt("height");

            var elScreens = elScreenSetup.RequiredElement("screens");

            var elsDmxScreens = elScreens.RequiredElements("DmxScreen");

            var fixtures = elsDmxScreens.SelectMany(ResolumeFixture.FromScreenXml).ToImmutableList();

            return new ResolumePixelMap
            {
                Name = name,
                Size = new Size(width, height),
                Fixtures = fixtures
            };
        }

        public string Name { get; init; } = string.Empty;

        public Size Size { get; init; } = Size.Empty;

        public ImmutableList<ResolumeFixture> Fixtures { get; init; } = ImmutableList<ResolumeFixture>.Empty;

        public DisguiseDmxTable ComputeDmxTable()
        {
            return new DisguiseDmxTable
            {
                Entries = Fixtures.SelectMany(f => f.ComputeDmxTableEntries()).ToImmutableList()
            };
        }
    }

    record ResolumeFixture
    {
        public static IEnumerable<ResolumeFixture> FromScreenXml(XElement el)
        {
            var elsParamRange = el.RequiredElement("OutputDevice")
                .RequiredElement("OutputDeviceDmx")
                .RequiredElement("DmxOutputParams")
                .RequiredElements("ParamRange")
                .ToImmutableList();

            var subnet = elsParamRange.FirstElementWithAttributeValue("name", "Subnet").AttributeAsDouble("value");
            var universe = elsParamRange.FirstElementWithAttributeValue("name", "Universe").AttributeAsDouble("value");

            var combinedUniverse = (int)Math.Floor(universe) + ((int)Math.Floor(subnet) << 4);

            var elsDmxSlices = el.RequiredElement("layers").Elements("DmxSlice");

            return elsDmxSlices.Select(elDmxSlice => FromXml(elDmxSlice, combinedUniverse));
        }

        public static ResolumeFixture FromXml(XElement el, int lumiverseId)
        {
            var elParamsCommon = el.RequiredElements("Params").FirstElementWithAttributeValue("name", "Common");

            var name = elParamsCommon.RequiredElements("Param")
                .FirstElementWithAttributeValue("name", "Name")
                .AttributeAsString("value");

            var isEnabled = elParamsCommon.RequiredElements("Param")
                .FirstElementWithAttributeValue("name", "Enabled")
                .AttributeAsBool("value");

            var elParamsInput = el.RequiredElements("Params").FirstElementWithAttributeValue("name", "Input");

            var elStartChannel = elParamsInput.RequiredElements("ParamRange")
                .FirstElementWithAttributeValue("name", "Start Channel")
                .AttributeAsDouble("value");

            var elsV = el.RequiredElement("InputRect").RequiredElements("v").ToImmutableList();
            if (elsV.Count != 4)
                throw new XmlException("Incorrect number of coordinates in input rect");

            var topLeftX = elsV[0].AttributeAsFloat("x");
            var topLeftY = elsV[0].AttributeAsFloat("y");
            var topRightX = elsV[1].AttributeAsFloat("x");
            var topRightY = elsV[1].AttributeAsFloat("y");
            var bottomRightX = elsV[2].AttributeAsFloat("x");
            var bottomRightY = elsV[2].AttributeAsFloat("y");
            var bottomLeftX = elsV[3].AttributeAsFloat("x");
            var bottomLeftY = elsV[3].AttributeAsFloat("y");


            var elParamFixturePixels = el.RequiredElement("FixtureInstance").RequiredElement("Fixture")
                .RequiredElement("Params")
                .RequiredElement("ParamFixturePixels");

            var elParamRanges = elParamFixturePixels.RequiredElements("ParamRange").ToImmutableList();

            var width = elParamRanges.FirstElementWithAttributeValue("name", "Width").AttributeAsDouble("value");
            var height = elParamRanges.FirstElementWithAttributeValue("name", "Height").AttributeAsDouble("value");

            var elParamChoices = elParamFixturePixels.RequiredElements("ParamChoice").ToImmutableList();

            var colorFormatString = elParamChoices.FirstElementWithAttributeValue("name", "Color Format")
                .AttributeAsString("value");

            if (!Enum.TryParse<ColorFormat>(colorFormatString, true, out var colorFormat))
                throw new XmlException($"'{colorFormatString}' is not a valid color format");

            var distribution = elParamChoices.FirstElementWithAttributeValue("name", "Distribution")
                .AttributeAsEnum<PixelDistribution>("value");


            return new ResolumeFixture
            {
                Name = name,
                IsEnabled = isEnabled,
                LumiverseId = lumiverseId,
                StartChannel = (int)Math.Floor(elStartChannel),
                TopLeft = new Vector2(topLeftX, topLeftY),
                TopRight = new Vector2(topRightX, topRightY),
                BottomRight = new Vector2(bottomRightX, bottomRightY),
                BottomLeft = new Vector2(bottomLeftX, bottomLeftY),
                Size = new Size((int)Math.Floor(width), (int)Math.Floor(height)),
                ColorFormat = colorFormat,
                Distribution = distribution
            };
        }

        public string Name { get; init; } = string.Empty;

        public bool IsEnabled { get; init; }

        public int LumiverseId { get; init; }
        public int StartChannel { get; init; }

        public Vector2 TopLeft { get; init; }
        public Vector2 TopRight { get; init; }
        public Vector2 BottomRight { get; init; }
        public Vector2 BottomLeft { get; init; }

        public Size Size { get; init; }
        public ColorFormat ColorFormat { get; init; }
        public PixelDistribution Distribution { get; init; }

        public IEnumerable<DisguiseDmxTable.Entry> ComputeDmxTableEntries()
        {
            int channel = StartChannel;

            for (int x = 0; x < Size.Width; ++x)
            {
                var normX = x / (float)Size.Width;

                var top = Vector2.Lerp(TopLeft, TopRight, normX);
                var bottom = Vector2.Lerp(BottomLeft, BottomRight, normX);

                for (int y = 0; y < Size.Height; ++y)
                {
                    var normY = y / (float)Size.Height;
                    var point = Vector2.Lerp(top, bottom, normY);

                    yield return new DisguiseDmxTable.Entry
                    {
                        X = (int)Math.Round(point.X, MidpointRounding.AwayFromZero),
                        Y = (int)Math.Round(point.Y, MidpointRounding.AwayFromZero),
                        UniverseIndex = LumiverseId,
                        StartChannel = channel
                    };

                    channel += ColorFormat.GetChannelWidth();
                }
            }
        }
    }
}
