import argparse
import xml.etree.ElementTree as ET
import smtplib
from email.mime.text import MIMEText
import socket # For socket.error
import sys # For sys.exit()

def parse_config(config_path):
    """Parses the XML configuration file and extracts SMTP details."""
    try:
        tree = ET.parse(config_path)
        root = tree.getroot()

        required_tags = ["gonderen", "smtpAdres", "kullanici", "sifre"]
        details = {}
        for tag_name in required_tags:
            element = root.find(tag_name)
            if element is None or element.text is None or not element.text.strip():
                print(f"Error: '{tag_name}' tag missing, empty, or invalid in configuration file '{config_path}'.")
                return None, None, None, None
            details[tag_name] = element.text.strip()

        return details["gonderen"], details["smtpAdres"], details["kullanici"], details["sifre"]

    except FileNotFoundError:
        print(f"Error: Configuration file '{config_path}' not found.")
        return None, None, None, None
    except ET.ParseError:
        print(f"Error: Could not parse XML configuration file '{config_path}'. Make sure it is well-formed.")
        return None, None, None, None
    except Exception as e: # Catch any other potential parsing errors
        print(f"An unexpected error occurred while parsing the configuration file: {e}")
        return None, None, None, None

def send_email(smtp_details, to_email, subject, body):
    """Sends an email using the provided SMTP details and email content."""
    sender_email, smtp_server_address, username, password = smtp_details
    
    msg = MIMEText(body)
    msg['From'] = sender_email
    msg['To'] = to_email
    msg['Subject'] = subject

    try:
        smtp_port = 587
        server = smtplib.SMTP(smtp_server_address, smtp_port)
        server.ehlo()
        server.starttls()
        server.ehlo()
        server.login(username, password)
        server.sendmail(sender_email, to_email, msg.as_string())
        server.quit()
        print("Email sent successfully!")
        return True
    except smtplib.SMTPAuthenticationError:
        print("Error: SMTP Authentication failed. Check username/password in config.")
    except smtplib.SMTPConnectError:
        print(f"Error: Could not connect to SMTP server at {smtp_server_address}:{smtp_port}.")
    except smtplib.SMTPServerDisconnected:
        print("Error: SMTP server disconnected unexpectedly.")
    except (socket.error, smtplib.SMTPException) as e: # Broader catch for SMTP related errors
        print(f"SMTP or socket error occurred: {e}")
    except Exception as e:
        print(f"An unexpected error occurred while sending email: {e}")
    return False

def main():
    parser = argparse.ArgumentParser(
        description="Command-line tool to send an email using SMTP details from an XML configuration file.",
        formatter_class=argparse.RawTextHelpFormatter # To allow for multiline help messages if needed later
    )
    # Argparse with required=True automatically handles missing arguments and exits with status 2
    parser.add_argument("--to", required=True, help="Recipient's email address.")
    parser.add_argument("--subject", required=True, help="Subject of the email.")
    parser.add_argument("--body", required=True, help="Body content of the email.")
    parser.add_argument("--config", default="ayar.xml", help="Path to the XML configuration file (default: ayar.xml).")

    args = parser.parse_args()

    # These print statements are for debugging/confirmation, can be removed in production
    # print("Parsed arguments:")
    # print(f"To: {args.to}")
    # print(f"Subject: {args.subject}")
    # print(f"Body: {args.body}")
    # print(f"Config file: {args.config}")

    smtp_details = parse_config(args.config)

    # Check if all components of smtp_details are valid
    if not smtp_details or not all(smtp_details):
        print("\nSMTP configuration is incomplete, missing, or could not be loaded. Email not sent.")
        sys.exit(1)
    
    sender_email, smtp_server, username, password = smtp_details

    # print("\nSMTP Configuration loaded:") # Debugging
    # print(f"Sender Email: {sender_email}") # Debugging
    # print(f"SMTP Server: {smtp_server}") # Debugging
        
    print("\nAttempting to send email...")
    if not send_email(smtp_details, args.to, args.subject, args.body):
        print("Email sending failed.")
        sys.exit(1)
    
    # If script reaches here, email was sent successfully, and it will exit with 0 by default.

if __name__ == "__main__":
    main()
