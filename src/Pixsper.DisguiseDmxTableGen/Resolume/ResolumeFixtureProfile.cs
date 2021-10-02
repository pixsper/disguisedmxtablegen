using System;
using System.Collections.Immutable;
using System.Drawing;
using System.Xml;
using System.Xml.Linq;

namespace Pixsper.DisguiseDmxTableGen.Resolume
{
    record ResolumeFixtureProfile
    {
        public static ResolumeFixtureProfile FromXml(XDocument doc)
        {
            var elRoot = doc.Root;
            if (elRoot is null)
                throw new XmlException();

            var id = elRoot.AttributeAsGuid("uuid");
            var name = elRoot.AttributeAsString("fixtureName");

            var elParamFixturePixels = elRoot.RequiredElement("Params").RequiredElement("ParamFixturePixels");

            var elParamRanges = elParamFixturePixels.RequiredElements("ParamRange").ToImmutableList();

            var width = elParamRanges.FirstElementWithAttributeValue("name", "Width").AttributeAsDouble("value");
            var height = elParamRanges.FirstElementWithAttributeValue("name", "Height").AttributeAsDouble("value");

            var elParamChoices = elParamFixturePixels.RequiredElements("ParamChoice").ToImmutableList();

            var distribution = elParamChoices.FirstElementWithAttributeValue("name", "Distribution")
                .AttributeAsEnum<PixelDistribution>("value");

            var colorFormatString = elParamChoices.FirstElementWithAttributeValue("name", "Color Format")
                .AttributeAsString("value");

            if (!Enum.TryParse<ColorFormat>(colorFormatString, true, out var colorFormat))
                throw new XmlException($"'{colorFormatString}' is not a valid color format");

            return new ResolumeFixtureProfile
            {
                Id = id,
                Name = name,
                Size = new Size((int)Math.Floor(width), (int)Math.Floor(height)),
                Distribution = distribution,
                ColorFormat = colorFormat
            };
        }

        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public Size Size { get; init; } = Size.Empty;

        public PixelDistribution Distribution { get; init; }

        public ColorFormat ColorFormat { get; init; } 
    }

    enum ColorFormat : int
    {
        Rgb,
        Rbg,
        Grb,
        Gbr,
        Brg,
        Bgr,
        L,
        Rgba,
        Rgbw,
        Rgbwa,
        Rgbaw,
        Grbw,
        Wrgb,
        Wargb,
        Cmy
    }

    static class ColorFormatExtensions
    {
        public static int GetChannelWidth(this ColorFormat format)
        {
            switch (format)
            {
                case ColorFormat.L:
                    return 1;

                case ColorFormat.Rgb:
                case ColorFormat.Rbg:
                case ColorFormat.Grb:
                case ColorFormat.Gbr:
                case ColorFormat.Brg:
                case ColorFormat.Bgr:
                case ColorFormat.Cmy:
                    return 3;
                
                case ColorFormat.Rgba:
                case ColorFormat.Rgbw:
                case ColorFormat.Grbw:
                case ColorFormat.Wrgb:
                    return 4;
                case ColorFormat.Rgbwa:
                case ColorFormat.Rgbaw:
                case ColorFormat.Wargb:
                    return 5;

                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }
    }

    enum PixelDistribution : int
    {
        LeftToRight = 170,
        RightToLeft = 102,
        BottomToTop = 154,
        TopToBottom = 166
    }
}