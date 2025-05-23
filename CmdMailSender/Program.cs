using System;
using System.IO;    // Required for FileNotFoundException, Path
using System.Xml;   // Required for XmlException
using System.Collections.Generic; // Required for Dictionary
using System.Linq; // Required for Linq methods (string.Join, Select)
using System.Net; // Required for NetworkCredential
using System.Net.Mail; // Required for MailMessage, SmtpClient

namespace CmdMailSender
{
    class Program
    {
        static void PrintUsageAndExit(string? errorMessage = null)
        {
            if (!string.IsNullOrEmpty(errorMessage))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {errorMessage}");
                Console.ResetColor();
            }
            Console.WriteLine("\nUsage: CmdMailSender konu=\"<subject>\" msj=\"<message>\" alici=\"<recipient1@example.com[;recipient2@example.com]>\" [config=\"<path/to/ayar.xml>\"]");
            Console.WriteLine("\nArguments:");
            Console.WriteLine("  konu=\"<subject>\"                : Subject of the email (required).");
            Console.WriteLine("  msj=\"<message>\"                 : Body content of the email (required).");
            Console.WriteLine("  alici=\"<recipients>\"            : Recipient email address(es) (required).");
            Console.WriteLine("                                     Multiple emails can be separated by semicolons (e.g., \"one@example.com;two@example.com\").");
            Console.WriteLine("  config=\"<path/to/ayar.xml>\"   : Optional. Path to the XML configuration file.");
            Console.WriteLine("                                     If not provided, defaults to \"ayar.xml\" in the current directory.");
            Environment.Exit(1);
        }

        static bool SendEmail(SmtpConfig config, string recipients, string subject, string messageBody)
        {
            MailMessage? mailMessage = null;
            try
            {
                if (config == null) // Should be caught before calling, but as a safeguard
                {
                     Console.ForegroundColor = ConsoleColor.Red;
                     Console.WriteLine("Critical Error: SMTP configuration object is null.");
                     Console.ResetColor();
                     return false;
                }
                if (string.IsNullOrWhiteSpace(config.Sender) || 
                    string.IsNullOrWhiteSpace(config.ServerAddress) || 
                    string.IsNullOrWhiteSpace(config.Username) ||
                    config.Password == null) // Password can be empty, but not null if tag exists.
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: SMTP configuration is incomplete. Please check sender, server address, username, and password in the configuration file.");
                    Console.ResetColor();
                    return false;
                }
                
                mailMessage = new MailMessage
                {
                    From = new MailAddress(config.Sender),
                    Subject = subject,
                    Body = messageBody,
                    IsBodyHtml = false
                };

                string[] recipientList = recipients.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (recipientList.Length == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: No valid recipient email addresses were provided or parsed from the 'alici' argument.");
                    Console.ResetColor();
                    return false;
                }
                foreach (string recipient in recipientList)
                {
                    mailMessage.To.Add(recipient);
                }

                SmtpClient smtpClient = new SmtpClient();
                
                string host = config.ServerAddress;
                int port = 587; 

                if (config.ServerAddress.Contains(':'))
                {
                    string[] parts = config.ServerAddress.Split(':');
                    host = parts[0];
                    if (parts.Length > 1 && int.TryParse(parts[1], out int parsedPort) && parsedPort > 0 && parsedPort <= 65535)
                    {
                        port = parsedPort;
                    }
                    else if (parts.Length > 1) // Port was specified but not a valid number
                    {
                         Console.ForegroundColor = ConsoleColor.Yellow;
                         Console.WriteLine($"Warning: Invalid port '{parts[1]}' in server address '{config.ServerAddress}'. Defaulting to port {port}.");
                         Console.ResetColor();
                    }
                }
                smtpClient.Host = host;
                smtpClient.Port = port;
                
                smtpClient.Credentials = new NetworkCredential(config.Username, config.Password);
                smtpClient.EnableSsl = true; 

                Console.WriteLine($"\nAttempting to send email via {smtpClient.Host}:{smtpClient.Port} as {config.Sender}...");
                smtpClient.Send(mailMessage);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Email sent successfully to: {string.Join(", ", mailMessage.To.Select(m => m.Address))}");
                Console.ResetColor();
                return true;
            }
            catch (SmtpException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nSMTP Error: {ex.Message} (Status Code: {ex.StatusCode})");
                Console.WriteLine("  Troubleshooting: Check your SMTP server address ({config?.ServerAddress}), port, credentials in the config file, and network connectivity. Ensure SSL/TLS is correctly configured for your SMTP provider (usually port 587 with STARTTLS/EnableSsl=true, or port 465 for direct SSL).");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"  Inner Exception: {ex.InnerException.Message}");
                }
                Console.ResetColor();
            }
            catch (FormatException ex) 
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nError in email address format: {ex.Message}");
                Console.WriteLine("  Troubleshooting: Please check the format of the sender email in the config file ('gonderen' tag) and the recipient email(s) provided in the 'alici' argument.");
                Console.ResetColor();
            }
            catch (InvalidOperationException ex) // Can occur if SmtpClient is misconfigured
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nInvalid Operation Error during email sending: {ex.Message}");
                Console.WriteLine("  Troubleshooting: This might indicate an issue with how the SmtpClient is configured or used. Review SMTP settings.");
                Console.ResetColor();
            }
            catch (Exception ex) 
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nAn unexpected error occurred while sending email: {ex.Message}");
                Console.ResetColor();
            }
            finally
            {
                mailMessage?.Dispose();
            }
            return false;
        }

        static void Main(string[] args)
        {
            string? subjectArg = null;
            string? messageArg = null;
            string? recipientsArg = null;
            string configFile = "ayar.xml"; 

            if (args.Length == 0)
            {
                PrintUsageAndExit("No arguments provided.");
            }

            foreach (string arg in args)
            {
                string[] parts = arg.Split(new[] { '=' }, 2);
                if (parts.Length != 2)
                {
                    PrintUsageAndExit($"Malformed argument: '{arg}'. Arguments must be in key=\"value\" format.");
                    return; 
                }
                string key = parts[0].ToLowerInvariant();
                string value = parts[1];
                if ((value.StartsWith("\"") && value.EndsWith("\"")) || (value.StartsWith("'") && value.EndsWith("'")))
                {
                    value = value.Substring(1, value.Length - 2);
                }
                switch (key)
                {
                    case "konu": subjectArg = value; break;
                    case "msj": messageArg = value; break;
                    case "alici": recipientsArg = value; break;
                    case "config": configFile = value; break;
                    default: PrintUsageAndExit($"Unknown argument key: '{key}'."); return;
                }
            }

            if (string.IsNullOrWhiteSpace(subjectArg)) PrintUsageAndExit("Subject (konu) is required.");
            if (string.IsNullOrWhiteSpace(messageArg)) PrintUsageAndExit("Message (msj) is required.");
            if (string.IsNullOrWhiteSpace(recipientsArg)) PrintUsageAndExit("Recipients (alici) are required.");

            Console.WriteLine("--- Parsed Arguments ---");
            Console.WriteLine($"Subject: {subjectArg}");
            Console.WriteLine($"Message: {messageArg}");
            Console.WriteLine($"Recipients: {recipientsArg}");
            Console.WriteLine($"Config File: {Path.GetFullPath(configFile)}"); // Show full path for clarity
            Console.WriteLine("------------------------\n");
            
            SmtpConfig? smtpConfig = null;
            try
            {
                if (!File.Exists(configFile))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Warning: Configuration file '{Path.GetFullPath(configFile)}' not found.");
                    Console.WriteLine("  To proceed with a dummy configuration for testing, create a file named 'ayar.xml' (or your specified config file)");
                    Console.WriteLine("  with the following structure in the application's directory:");
                    Console.WriteLine(@"
<Ayarlar>
    <gonderen>your_sender_email@example.com</gonderen>
    <smtpAdres>smtp.example.com:587</smtpAdres>
    <kullanici>your_smtp_username</kullanici>
    <sifre>your_smtp_password</sifre>
</Ayarlar>");
                    Console.ResetColor();
                    // Removed automatic dummy file creation to make behavior more predictable for users.
                    // User must provide a valid config file.
                    PrintUsageAndExit($"Configuration file '{Path.GetFullPath(configFile)}' is missing.");
                }

                Console.WriteLine($"Loading configuration from: {Path.GetFullPath(configFile)}");
                smtpConfig = ConfigLoader.LoadConfig(configFile); // ConfigLoader already has good error messages for missing tags
                Console.WriteLine("\nSMTP Configuration loaded successfully:");
                Console.WriteLine($"  Sender: {smtpConfig.Sender}");
                Console.WriteLine($"  Server Address: {smtpConfig.ServerAddress}");
                Console.WriteLine($"  Username: {smtpConfig.Username}");
                Console.WriteLine($"  Password: {(string.IsNullOrWhiteSpace(smtpConfig.Password) ? "[empty]" : "[set/not empty]")}");
            }
            catch (FileNotFoundException ex) 
            { 
                Console.ForegroundColor = ConsoleColor.Red; 
                Console.WriteLine($"\nConfiguration File Error: {ex.Message}"); 
                Console.WriteLine("  Troubleshooting: Please ensure the file exists at the specified path and the application has read permissions.");
                Console.ResetColor(); 
                Environment.Exit(1); 
            }
            catch (XmlException ex) 
            { 
                Console.ForegroundColor = ConsoleColor.Red; 
                Console.WriteLine($"\nConfiguration XML Error: {ex.Message}"); 
                Console.WriteLine("  Troubleshooting: Please ensure the XML is well-formed and all required tags (gonderen, smtpAdres, kullanici, sifre) are present and contain valid values.");
                if (ex.InnerException != null) Console.WriteLine($"  Inner XML Exception: {ex.InnerException.Message}");
                Console.ResetColor(); 
                Environment.Exit(1); 
            }
            catch (Exception ex) // Catch-all for other ConfigLoader exceptions
            { 
                Console.ForegroundColor = ConsoleColor.Red; 
                Console.WriteLine($"\nUnexpected error loading configuration: {ex.Message}"); 
                Console.ResetColor(); 
                Environment.Exit(1); 
            }

            if (subjectArg == null || messageArg == null || recipientsArg == null || smtpConfig == null)
            {
                 Console.ForegroundColor = ConsoleColor.Red;
                 Console.WriteLine("\nCritical error: Arguments or configuration were null before sending email. This indicates an issue in the program logic.");
                 Console.ResetColor();
                 Environment.Exit(1); 
            }
            
            if (!SendEmail(smtpConfig, recipientsArg, subjectArg, messageArg))
            {
                // SendEmail method already prints detailed errors. This is a final generic failure message.
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nEmail sending failed. Please review previous error messages for details.");
                Console.ResetColor();
                Environment.Exit(1); 
            }
            // Implicit exit with 0 if email sent successfully
        }
    }
}
