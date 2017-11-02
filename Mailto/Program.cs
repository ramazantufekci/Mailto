/*
 * Created by SharpDevelop.
 * User: ramazan
 * Date: 11/1/2017
 * Time: 1:49 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Xml;
using System.Net.Mail;

namespace Mailto
{
	class Program
	{
			static string msj;
			static string konu;
			static string alici;
			static string gonderen;
			static string smtpAdres;
			static string kullanici;
			static string sifre;
			
		public static void Main(string[] args)
		{			
			// TODO: Implement Functionality Here
			foreach (var element in args) {
				if (element.StartsWith("msj=")) {
					msj = element.Replace("msj=","");
				}else if(element.StartsWith("konu=")){
					konu = element.Replace("konu=","");
				}else if(element.StartsWith("alici=")){
					alici = element.Replace("alici=","");
				}
			}
			try {
				if (konu.Length>2 && alici.Length>3) {
					mailGonder(konu,msj,alici);
				}
				
				
			} catch {
				
				Console.WriteLine("Alıcıları ve konuyu boş bırakmayın !!!");
				Console.WriteLine("Örnek Mailto.exe alici=\"example@example.com\" konu=\"example subject\"");
				Console.Read();
			}
			
		}
		
		static void mailGonder(string konu2,string msj2,string alici2){
			if(ayarOku()){
				MailMessage mail = new MailMessage();
				mail.From = new MailAddress(gonderen);
				string[] alc = alici2.Split(';');
				for (int i = 0; i < alc.Length; i++) {
					mail.To.Add(alc[i]);
				}
				
				mail.Subject = konu2;
				mail.Body = msj2;
				SmtpClient smtp = new SmtpClient(smtpAdres);
				smtp.Credentials = new System.Net.NetworkCredential(kullanici,sifre);
				smtp.EnableSsl = true;
				smtp.Send(mail);
			}
			
			
			
			
		}
		
		static bool ayarOku(){
			XmlTextReader xml = new XmlTextReader("ayar.xml");
			try {
					while(xml.Read()){
						if (xml.NodeType==XmlNodeType.Element) {
							switch (xml.Name) {
								case "gonderen":
								gonderen = xml.ReadString();
								break;
							case "smtpAdres":
								smtpAdres = xml.ReadString();
								break;
							case "kullanici":
								kullanici = xml.ReadString();
								break;
							case "sifre":
								sifre = xml.ReadString();
								break;
								
							}
						}
					
				
					}
				return true;
			} catch {
				
				Console.WriteLine("Ayar dosyasından bilgileri okuyamadım !!!");
				return false;
			}
		}
	}
}