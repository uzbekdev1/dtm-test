using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace DTM.Test.OMR.Helpers
{
    public static class XMLHelper
    {
        public static string Serialize<T>(T value, XmlWriterSettings xmlWriterSettings = null)
        {
            if (value == null)
                throw new ArgumentException("value");

            var serializer = new XmlSerializer(typeof(T));
            var settings = xmlWriterSettings ?? new XmlWriterSettings
                           {
                               Encoding = new UnicodeEncoding(false, false),
                               Indent = false,
                               OmitXmlDeclaration = false
                           };

            using (var textWriter = new StringWriter())
            {
                using (var xmlWriter = XmlWriter.Create(textWriter, settings))
                {
                    serializer.Serialize(xmlWriter, value);
                }

                return textWriter.ToString();
            }
        }

        public static T Deserialize<T>(string xml, XmlReaderSettings xmlReaderSettings = null)
        {
            if (string.IsNullOrEmpty(xml))
                throw new ArgumentException("xml");

            var serializer = new XmlSerializer(typeof(T));
            var settings = xmlReaderSettings ?? new XmlReaderSettings();

            // No settings need modifying here
            using (var textReader = new StringReader(xml))
            {
                using (var xmlReader = XmlReader.Create(textReader, settings))
                {
                    return (T) serializer.Deserialize(xmlReader);
                }
            }
        }
    }
}