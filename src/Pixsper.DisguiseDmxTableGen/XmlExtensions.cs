using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Pixsper.DisguiseDmxTableGen
{
    static class XmlExtensions
    {
        public static XElement RequiredElement(this XElement el, XName name)
        {
            var target = el.Element(name);
            if (target is null)
                throw new XmlException($"Couldn't find element named '{name}' on element '{el.Name}'");

            return target;
        }

        public static IEnumerable<XElement> RequiredElements(this XElement el, XName name)
        {
            var targets = el.Elements(name);
            if (targets is null)
                throw new XmlException($"Couldn't find elements named '{name}' on element '{el.Name}'");

            return targets;
        }

        public static XElement FirstElementWithAttributeValue(this IEnumerable<XElement> els, XName name, string value)
        {
            var target = els.FirstOrDefault(e => e.AttributeAsString(name) == value);
            if (target is null)
                throw new XmlException($"Couldn't find element with attribute '{name}' value of '{value}'");

            return target;
        }

        public static string AttributeAsString(this XElement el, XName name)
        {
            var attr = el.Attribute(name);
            if (attr is null)
                throw new XmlException($"Couldn't find attribute named '{name}' on element '{el.Name}'");

            return attr.Value;
        }

        public static Guid AttributeAsGuid(this XElement el, XName name)
        {
            var valueString = el.AttributeAsString(name);

            if (!Guid.TryParse(valueString, out var value))
                throw new XmlException($"Couldn't parse attribute named '{name}' on element '{el.Name}' as Guid");

            return value;
        }

        public static bool AttributeAsBool(this XElement el, XName name)
        {
            var valueString = el.AttributeAsString(name);

            if (!int.TryParse(valueString, out var value))
                throw new XmlException($"Couldn't parse attribute named '{name}' on element '{el.Name}' as int");

            return value == 1;
        }

        public static int AttributeAsInt(this XElement el, XName name)
        {
            var valueString = el.AttributeAsString(name);

            if (!int.TryParse(valueString, out var value))
                throw new XmlException($"Couldn't parse attribute named '{name}' on element '{el.Name}' as int");

            return value;
        }

        public static float AttributeAsFloat(this XElement el, XName name)
        {
            var valueString = el.AttributeAsString(name);

            if (!float.TryParse(valueString, out var value))
                throw new XmlException($"Couldn't parse attribute named '{name}' on element '{el.Name}' as float");

            return value;
        }

        public static double AttributeAsDouble(this XElement el, XName name)
        {
            var valueString = el.AttributeAsString(name);

            if (!double.TryParse(valueString, out var value))
                throw new XmlException($"Couldn't parse attribute named '{name}' on element '{el.Name}' as double");

            return value;
        }

        public static TEnum AttributeAsEnum<TEnum>(this XElement el, XName name) where TEnum : Enum
        {
            var intValue = el.AttributeAsInt(name);
            var value = (TEnum)(object)intValue;

            if (!Enum.IsDefined(typeof(TEnum), value)) 
                throw new XmlException($"Attribute value '{value}' is not a valid value for attribute '{name}'");

            return value;
        }
    }
}
