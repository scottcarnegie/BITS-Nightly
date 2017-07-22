using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;
using System.Net.Mail;
using System.Net;

namespace BITS_Nightly
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Better IT Systems");
            Console.WriteLine("Nightly");
            Console.WriteLine("v1.1");
            Console.WriteLine("Scott Carnegie");
            Console.WriteLine("September 17, 2016\n");

            string fileLocation = ConfigurationManager.AppSettings["FolderLocation"];
            string backupLocation = ConfigurationManager.AppSettings["BackupLocation"];
            bool emailErrorEnabled = Convert.ToBoolean(ConfigurationManager.AppSettings["EnableErrorEmail"]);
            bool folderLevelCopy = Convert.ToBoolean(ConfigurationManager.AppSettings["FolderLevelCopy"]);
            string filename = ConfigurationManager.AppSettings["Filename"];
            
            string logfolderlocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BITS Nightly");
            string filename_short;
            if (!folderLevelCopy)
            {
                filename_short = filename.Replace(Path.GetExtension(filename), "");
                string logfilelocation = Path.Combine(logfolderlocation, "log_" + filename_short + ".csv");

                Console.WriteLine("File Location: \t\t{0}\nFile Name: \t\t{1}\nBackup Location: \t{2}\nLog Location: \t\t{3}\n", fileLocation, filename, backupLocation, logfilelocation);
                Console.WriteLine("Copying file...");
                CreateFileCopy(fileLocation, filename, backupLocation, emailErrorEnabled, logfilelocation);

                Console.WriteLine("Checking log delivery schedule...");
                LogIssuer(fileLocation, filename, backupLocation, logfilelocation);

            }
            else
            {
                filename_short = Path.GetFileName(fileLocation);
                string logfilelocation = Path.Combine(logfolderlocation, "log_" + filename_short + ".csv");

                Console.WriteLine("Folder Location: \t\t{0}\nBackup Location: \t{1}\nLog Location: \t\t{2}\n", fileLocation, backupLocation, logfilelocation);
                Console.WriteLine("Copying folder...\n");
                CreateFolderCopy(fileLocation, backupLocation, emailErrorEnabled, logfilelocation);

                Console.WriteLine("Checking log delivery schedule...");
                LogIssuer(fileLocation, "", backupLocation, logfilelocation);
            }

            

        }

        static void EmailError(string errorMessage, string filepath, string filename, string destpath, string logfilelocation)
        {
            string emailTemplateError = ConfigurationManager.AppSettings["EmailTemplate_Error"];

            string smtpServerUrl = ConfigurationManager.AppSettings["SMTPServerUrl"];
            int smtpServerPort = Convert.ToInt32(ConfigurationManager.AppSettings["SMTPServerPort"]);
            string smtpServerUsername = ConfigurationManager.AppSettings["SMTPServerUsername"];
            string smtpServerPassword = ConfigurationManager.AppSettings["SMTPServerPassword"];

            string emailTo = ConfigurationManager.AppSettings["EmailTo"];
            string emailFrom = ConfigurationManager.AppSettings["EmailFrom"];
            string emailSubject_Error = ConfigurationManager.AppSettings["EmailSubject_Error"];

            SmtpClient client = new SmtpClient(smtpServerUrl, smtpServerPort)
            {
                Credentials = new NetworkCredential(smtpServerUsername, smtpServerPassword),
                EnableSsl = true
            };

            MailMessage mail = new MailMessage(emailFrom, emailTo);
            mail.Subject = emailSubject_Error;
            mail.Body = File.ReadAllText(emailTemplateError).Replace("{ERROR MESSAGE}",errorMessage).Replace("{FILEPATH}",filepath).Replace("{FILENAME}",filename).Replace("{COPYPATH}",destpath);
            mail.IsBodyHtml = true;
            try
            {
                Console.WriteLine("Sending Email...");
                client.Send(mail);
                Console.WriteLine("Error email sent successfully to {0}.\n",emailTo);
                WriteToLog(logfilelocation, "", filename, "", "", "Error email sent successfully to " + emailTo + ".");
            }
            catch (SmtpException)
            {
                Console.WriteLine("An error occurred: The SMTP profile was unable to authenticate.Please verify SMTP server and credentials are correct. \n");
                WriteToLog(logfilelocation, "", filename, "", "", "An error occurred: The SMTP profile was unable to authenticate. Please verify SMTP server, port and credentials are correct.");
            }
            catch (Exception e)
            {
                Console.WriteLine("\n" + e.ToString());
                WriteToLog(logfilelocation, "", "", "", "", e.ToString());
            }
        }

        static void EmailLog(string filepath, string filename, string destpath, string logfilelocation)
        {
            string emailTemplateLog = ConfigurationManager.AppSettings["EmailTemplate_Log"];

            string smtpServerUrl = ConfigurationManager.AppSettings["SMTPServerUrl"];
            int smtpServerPort = Convert.ToInt32(ConfigurationManager.AppSettings["SMTPServerPort"]);
            string smtpServerUsername = ConfigurationManager.AppSettings["SMTPServerUsername"];
            string smtpServerPassword = ConfigurationManager.AppSettings["SMTPServerPassword"];

            string emailTo = ConfigurationManager.AppSettings["EmailTo"];
            string emailFrom = ConfigurationManager.AppSettings["EmailFrom"];
            string emailSubject_Log = ConfigurationManager.AppSettings["EmailSubject_Log"];
            string emailSubject_Error = ConfigurationManager.AppSettings["EmailSubject_Log"];

            SmtpClient client = new SmtpClient(smtpServerUrl, smtpServerPort)
            {
                Credentials = new NetworkCredential(smtpServerUsername, smtpServerPassword),
                EnableSsl = true
            };

            MailMessage mail = new MailMessage(emailFrom, emailTo);
            mail.Subject = emailSubject_Log;
            mail.Body = File.ReadAllText(emailTemplateLog).Replace("{FILEPATH}", filepath).Replace("{FILENAME}", filename).Replace("{COPYPATH}", destpath);
            mail.IsBodyHtml = true;
            mail.Attachments.Add(new Attachment(logfilelocation));
            try
            {
                Console.WriteLine("Sending Email...");
                client.Send(mail);
                Console.WriteLine("Log email sent successfully to {0}.\n", emailTo);
            }
            catch (SmtpException)
            {
                Console.WriteLine("An error occurred: The SMTP profile was unable to authenticate.Please verify SMTP server and credentials are correct. \n");
            }
            catch (Exception e)
            {
                Console.WriteLine("\n" + e.ToString());
            }

        }

        static string WriteToLog(string logfilelocation, string filepath, string filename, string backuplocation, string copyname, string details)
        {
            string logfolderlocation = Directory.GetParent(logfilelocation).FullName;

            Console.WriteLine("Writing to log...");

            try{
                if (!Directory.Exists(logfolderlocation))
                {
                    Directory.CreateDirectory(logfolderlocation);
                }

                var csvEntry = new StringBuilder();
                if (!File.Exists(logfilelocation))
                {
                    File.Create(logfilelocation).Close();
                    csvEntry.AppendLine("Date and Time,Source File Path,Source File Name,Destination Path,Destination File Name,Details");
                }

                string entry = string.Format("{0},{1},{2},{3},{4},{5}", DateTime.Now.ToString(), filepath, filename, backuplocation, copyname, details);
                csvEntry.AppendLine(entry);

                File.AppendAllText(logfilelocation, csvEntry.ToString());
                Console.WriteLine("Log updated successfully.\n");
            }
            catch(Exception e)
            {
                Console.WriteLine("Error with log file: {0}", e.ToString());
            }

            return logfilelocation;
        }

        static void LogIssuer(string filepath, string filename, string destpath, string logfilelocation)
        {
            int period = Convert.ToInt32(ConfigurationManager.AppSettings["SendLogEmails"]);

            switch (period)
            {
                case 0:
                    Console.WriteLine("Logs are configured to never send.\n");
                    break;
                case 1:
                    Console.WriteLine("Logs are scheduled to be sent after each backup.\n");
                    EmailLog(filepath, filename, destpath, logfilelocation);
                    break;
                case 2:
                    Console.WriteLine("Logs are scheduled to be sent at the beginning of each week.\n");
                    if (DateTime.Now.DayOfWeek == (DayOfWeek)0)
                    {
                        EmailLog(filepath, filename, destpath, logfilelocation);
                    }
                    break;
                case 3:
                    Console.WriteLine("Logs are scheduled to be sent at the beginning of each month.\n");
                    if(DateTime.Now.Day == 1)
                    {
                        EmailLog(filepath, filename, destpath, logfilelocation);
                    }
                    break;
                default:
                    Console.WriteLine("Incorrect value entered in App.Config file. Logs will not be sent.\n");
                    break;
            }


        }

        static void CreateFileCopy(string fileLocation, string filename,string backupLocation,bool emailErrorEnabled, string logfilelocation)
        {
            try
            {
                string filepath = Path.Combine(fileLocation, filename);
                string fileext = Path.GetExtension(filepath);
                string copyname = filename.Replace(fileext, "_") + (DateTime.Now).ToString("yyyyMMdd_hhmmss") + fileext;
                string copypath = Path.Combine(backupLocation, copyname);
                File.Copy(filepath, copypath);

                string successMessage = "Copy completed: " + copyname;
                Console.WriteLine(successMessage + "\n");
                WriteToLog(logfilelocation, fileLocation, filename, backupLocation, copyname, successMessage);
            }
            catch (FileNotFoundException)
            {
                string errorMessage = "An error occurred: the source file was not found. Please validate the filename and file location.\n";
                Console.WriteLine(errorMessage);
                WriteToLog(logfilelocation, fileLocation, filename, backupLocation, "", errorMessage);
                if (emailErrorEnabled)
                {
                    EmailError(errorMessage, fileLocation, filename, backupLocation,logfilelocation);
                }

            }
            catch (DirectoryNotFoundException)
            {
                string errorMessage = "An error occurred: a directory was not found. Please validate the source and destination file location.";
                Console.WriteLine(errorMessage);
                WriteToLog(logfilelocation, fileLocation, filename, backupLocation, "", errorMessage);
                if (emailErrorEnabled)
                {
                    EmailError(errorMessage, fileLocation, filename, backupLocation,logfilelocation);
                }
            }
            catch (Exception e)
            {
                string errorMessage = e.ToString();
                Console.WriteLine(errorMessage);
                WriteToLog(logfilelocation, fileLocation, filename, backupLocation, "", errorMessage);
                if (emailErrorEnabled)
                {
                    EmailError(errorMessage, fileLocation, filename, backupLocation,logfilelocation);
                }
            }
        }

        static void CreateFolderCopy(string folderLocation, string backupLocation, bool emailErrorEnabled, string logfilelocation)
        {

            string folderName = Path.GetFileName(folderLocation);
            string destName = Path.Combine(backupLocation, folderName + "_" + (DateTime.Now).ToString("yyyyMMdd_hhmmss"));

            try
            {
                if (!Directory.Exists(backupLocation)||!Directory.Exists(folderLocation))
                {
                    throw new DirectoryNotFoundException();
                }
                
                if (!Directory.Exists(destName)) {
                    Console.WriteLine("Creating folder: {0}...",destName);
                    Directory.CreateDirectory(destName);
                    WriteToLog(logfilelocation, folderLocation, "", destName, "", "Root backup directory created successfully.");
                }
                foreach (string srcFilePath in Directory.GetFiles(folderLocation)) {
                    string copypath = Path.Combine(destName, Path.GetFileName(srcFilePath));
                    Console.WriteLine("Copying file: {0}...", srcFilePath);
                    File.Copy(srcFilePath, copypath);
                    WriteToLog(logfilelocation, folderLocation, Path.GetFileName(srcFilePath), destName, copypath, "File copied successfully.");
                }

                foreach (string srcPath in Directory.GetDirectories(folderLocation, "*", SearchOption.AllDirectories))
                {
                    string newDir = Path.Combine(destName,srcPath.Replace(folderLocation+"\\", ""));
                    if (!Directory.Exists(newDir))
                    {
                        Console.WriteLine("Creating folder: {0}...", newDir);
                        Directory.CreateDirectory(newDir);
                        WriteToLog(logfilelocation, folderLocation, "", newDir, "", "Backup sub-directory created successfully.");
                    }
                    foreach (string srcFilePath in Directory.GetFiles(srcPath))
                    {
                        Console.WriteLine("Copying file: {0}...", srcFilePath);
                        string copypath = Path.Combine(newDir, Path.GetFileName(srcFilePath));
                        File.Copy(srcFilePath, copypath);
                        WriteToLog(logfilelocation, folderLocation, Path.GetFileName(srcFilePath), newDir, copypath, "File copied successfully.");
                    }
                }

                string successMessage = "Copy of folder completed: " + destName;
                Console.WriteLine(successMessage + "\n");
                WriteToLog(logfilelocation, folderLocation, "", destName, "", successMessage);
            }
            catch (DirectoryNotFoundException)
            {
                string errorMessage = "An error occurred: a directory was not found. Please validate the source and destination file location.";
                Console.WriteLine(errorMessage);
                WriteToLog(logfilelocation, folderLocation, "", backupLocation, "", errorMessage);
                if (emailErrorEnabled)
                {
                    EmailError(errorMessage, folderLocation, "", backupLocation, logfilelocation);
                }
            }
            catch (Exception e)
            {
                string errorMessage = e.ToString();
                Console.WriteLine(errorMessage);
                WriteToLog(logfilelocation, folderLocation, "", backupLocation, "", errorMessage);
                if (emailErrorEnabled)
                {
                    EmailError(errorMessage, folderLocation, "", backupLocation, logfilelocation);
                }
            }
        }

    }
}
