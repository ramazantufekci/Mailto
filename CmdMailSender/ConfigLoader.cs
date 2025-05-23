using System;
using System.IO;
using System.Xml.Linq;

namespace CmdMailSender
{
    public class ConfigLoader
    {
        public static SmtpConfig LoadConfig(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Configuration file not found at: {filePath}");
            }

            try
            {
                XDocument doc = XDocument.Load(filePath);
                XElement? root = doc.Root;

                if (root == null)
                {
                    throw new System.Xml.XmlException("XML root element not found in the configuration file.");
                }

                SmtpConfig config = new SmtpConfig
                {
                    Sender = GetElementValue(root, "gonderen", filePath),
                    ServerAddress = GetElementValue(root, "smtpAdres", filePath),
                    Username = GetElementValue(root, "kullanici", filePath),
                    Password = GetElementValue(root, "sifre", filePath)
                };

                return config;
            }
            catch (System.Xml.XmlException ex)
            {
                // Re-throw with a more specific message or just let it propagate
                throw new System.Xml.XmlException($"Error parsing XML configuration file '{filePath}': {ex.Message}", ex);
            }
            catch (Exception ex) // Catch any other unexpected errors during loading
            {
                throw new Exception($"An unexpected error occurred while loading configuration from '{filePath}': {ex.Message}", ex);
            }
        }

        private static string GetElementValue(XElement parentElement, string elementName, string filePathForErrorMessage)
        {
            XElement? element = parentElement.Element(elementName);
            if (element == null || string.IsNullOrWhiteSpace(element.Value))
            {
                throw new System.Xml.XmlException($"Required element '{elementName}' is missing or empty in configuration file '{filePathForErrorMessage}'.");
            }
            return element.Value;
        }
    }
}
