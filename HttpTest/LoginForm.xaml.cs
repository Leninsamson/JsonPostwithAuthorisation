using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace HttpTest
{
    public partial class LoginForm : Window
    {
        public string tableappversion;
        public string UpdateFolder = "Updates"; // Folder to store the downloaded update package
        public string currentVersion;
        public string ClUpdateAPI = ConfigurationManager.AppSettings["ClientUpdate_APIServername"];
        public string ClUpdatePortNo = ConfigurationManager.AppSettings["ClientUpdate_Port"];
        public string ClUpdateLoc = ConfigurationManager.AppSettings["ClientUpdatePackage_Location"];
        private string pwdbox = string.Empty;

        public LoginForm()
        {
            InitializeComponent();
            currentVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();
            string Version_Number  = "v" + currentVersion;
            Clientv.Content = Version_Number;
            MainWindow.Versnumber = Version_Number;
            // clearoldfiles();
        }



        public async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {

         
                string LatVersionUrl = string.Format(@"http://{0}:{1}/api/Values/Version", ClUpdateAPI, ClUpdatePortNo);
                string latestVersion = await GetLatestVersion(LatVersionUrl);
               

                string EnforceStatusUrl = string.Format(@"http://{0}:{1}/api/Values/UpdateEnforceStatus", ClUpdateAPI, ClUpdatePortNo);
                string EnforceStatus = await GetEnforceStatus(EnforceStatusUrl);

                Version CV = new Version(currentVersion);
                Version LV = new Version(latestVersion);

                if (LV > CV)
                {

                 
                   if (EnforceStatus == "1")
                     {
                        MessageBoxResult Eresult = MessageBox.Show($"New Version JsonPost - {latestVersion} is available. Do you want to update now?", "Enforced New Updates Available", MessageBoxButton.YesNo);
                        if (Eresult == MessageBoxResult.Yes)
                        {
                            EnforcedPerformUpdate();
                        }
                    
                     }
                   else if (EnforceStatus == "0")
                    {
                        MessageBoxResult result = MessageBox.Show($"New Version JsonPost - {latestVersion} is available. Do you want to update now?", "New Updates Available", MessageBoxButton.YesNo);
                        if (result == MessageBoxResult.Yes)
                            PerformUpdate();
                    }
                }
            }
            catch
            {
                MessageBoxResult resultex =  MessageBox.Show("Unable to Update Client!!! ", "System Message", MessageBoxButton.OK, MessageBoxImage.Information);
                //if(resultex == MessageBoxResult.OK)
                //{
                //    Logwindow.Close();
                //}
            }

        }

        public async Task<string> GetLatestVersion(string LatVersionUrl, int Timeout = 15000)
        {

            try
            {
                HttpClient client = new HttpClient();
                
                HttpResponseMessage response = await client.GetAsync(LatVersionUrl);
                string result = await response.Content.ReadAsStringAsync();
                string fresult = result.Replace(@"""[", "").Replace(@"]""", "");
                DataTable DT = new DataTable();
                DT = JsonConvert.DeserializeObject<DataTable>(fresult);
                return Convert.ToString(DT.Rows[0]["Version"]);
            }

            catch (Exception e)
            {
                return e.Message;
            }
        }



        public async Task<string> GetEnforceStatus(string EnforceStatusUrl, int Timeout = 15000)
        {

            try
            {
                HttpClient client = new HttpClient();

                HttpResponseMessage response = await client.GetAsync(EnforceStatusUrl);
                string Enfresult = await response.Content.ReadAsStringAsync();
                // string fresult = result.Replace(@"""[", "").Replace(@"]""", "");
                //  DataTable DT = new DataTable();
                //  DT = JsonConvert.DeserializeObject<DataTable>(fresult);
                // return Convert.ToString(DT.Rows[0]["Version"]);
                return Enfresult;
            }

            catch (Exception e)
            {
                return e.Message;
            }
        }


        public async Task EnforcedPerformUpdate()
        {
            bool extractionSuccessful = false;
            try
            {
                // Create the update folder if it doesn't exist
                if (!Directory.Exists(UpdateFolder))
                {
                    Directory.CreateDirectory(UpdateFolder);
                }

                // Download the new version app package via URL   
                string UpdateFileNameUrl = string.Format(@"http://{0}:{1}/api/Values/UpdateFileName?directory={2}", ClUpdateAPI, ClUpdatePortNo, ClUpdateLoc);
                HttpClient httpclient = new HttpClient();
                string UpdateFileName = await httpclient.GetStringAsync(UpdateFileNameUrl);
                string updatePackageUrl = string.Format(@"http://{0}:{1}/api/Values/DownloadUpdate?fileName={2}&directory={3}", ClUpdateAPI, ClUpdatePortNo, UpdateFileName, ClUpdateLoc);
                string updatePackagePath = System.IO.Path.Combine(UpdateFolder, UpdateFileName);
                using (var client = new WebClient())
                {
                    await client.DownloadFileTaskAsync(updatePackageUrl, updatePackagePath);
                }


                ///Locate the filepaths
                string foldername = System.IO.Path.GetFileNameWithoutExtension(updatePackagePath);
                string updatedexename = foldername + @".exe";
                string newVersionExePath = System.IO.Path.Combine(UpdateFolder, foldername, updatedexename);
                string ExistingVersionPath = System.IO.Path.Combine(UpdateFolder, foldername);

                // Extract the downloaded Zippackage to a temp dir
                string tempExtractionPath = System.IO.Path.Combine(UpdateFolder, "Temp");
                if (Directory.Exists(tempExtractionPath))
                {
                    Directory.Delete(tempExtractionPath, true);
                }
                ZipFile.ExtractToDirectory(updatePackagePath, tempExtractionPath);

                // Delete the existing extracted dir if it exists
                if (Directory.Exists(ExistingVersionPath))
                {
                    Directory.Delete(ExistingVersionPath, true);
                }

                // Move the extracted Package from the temp dir to the existing extraction dir
                string tempExtrPackagePath = System.IO.Path.Combine(UpdateFolder, "Temp", foldername);
                Directory.Move(tempExtrPackagePath, ExistingVersionPath);

                extractionSuccessful = true;

                // Start the new version app
                if (extractionSuccessful)
                {
                    await StartNewVersionApp(newVersionExePath);
                    // Close the old version app
                    Process.GetCurrentProcess().Kill();
                }
            
            }
            catch (Exception e)
            {
                MessageBoxResult resultex = MessageBox.Show("Unable to Update Client!!! UpdateEnforceStatus is Enabled So Old Version client cannot be used", "System Message", MessageBoxButton.OK, MessageBoxImage.Information);
                if (resultex == MessageBoxResult.OK)
                {
                    Logwindow.Close();
                }
            }
        }


        public async Task PerformUpdate()
        {
            bool extractionSuccessful = false;
            try
            {
                // Create the update folder if it doesn't exist
                if (!Directory.Exists(UpdateFolder))
                {
                    Directory.CreateDirectory(UpdateFolder);
                }

                // Download the new version app package via URL   
                string UpdateFileNameUrl = string.Format(@"http://{0}:{1}/api/Values/UpdateFileName?directory={2}", ClUpdateAPI, ClUpdatePortNo, ClUpdateLoc);
                HttpClient httpclient = new HttpClient();
                string UpdateFileName = await httpclient.GetStringAsync(UpdateFileNameUrl);
                string updatePackageUrl = string.Format(@"http://{0}:{1}/api/Values/DownloadUpdate?fileName={2}&directory={3}", ClUpdateAPI, ClUpdatePortNo, UpdateFileName, ClUpdateLoc);
                string updatePackagePath = System.IO.Path.Combine(UpdateFolder, UpdateFileName);
                using (var client = new WebClient())
                {
                    await client.DownloadFileTaskAsync(updatePackageUrl, updatePackagePath);
                }

            
                ///Locate the filepaths
                string foldername = System.IO.Path.GetFileNameWithoutExtension(updatePackagePath);
                string updatedexename = foldername + @".exe";
                string newVersionExePath = System.IO.Path.Combine(UpdateFolder, foldername, updatedexename);
                string ExistingVersionPath = System.IO.Path.Combine(UpdateFolder, foldername);

                // Extract the downloaded Zippackage to a temp dir
                string tempExtractionPath = System.IO.Path.Combine(UpdateFolder, "Temp");
                if (Directory.Exists(tempExtractionPath))
                {
                    Directory.Delete(tempExtractionPath, true);
                }
                ZipFile.ExtractToDirectory(updatePackagePath, tempExtractionPath);

                // Delete the existing extracted dir if it exists
                if (Directory.Exists(ExistingVersionPath))
                {
                    Directory.Delete(ExistingVersionPath, true);
                }

                // Move the extracted Package from the temp dir to the existing extraction dir
                string tempExtrPackagePath = System.IO.Path.Combine(UpdateFolder, "Temp", foldername);
                Directory.Move(tempExtrPackagePath, ExistingVersionPath);

                extractionSuccessful = true;

                // Start the new version app
                if (extractionSuccessful)
                {
                    await StartNewVersionApp(newVersionExePath);
                    // Close the old version app
                    Process.GetCurrentProcess().Kill();
                }
            }
            catch (Exception e)
            {
                MessageBoxResult resultex = MessageBox.Show("Unable to Update Client!!!", "System Message", MessageBoxButton.OK, MessageBoxImage.Information);
                if (resultex == MessageBoxResult.OK)
                {
                    return;
                }
            }
        }

        public async Task StartNewVersionApp(string newVersionExePath)
        {
            Process.Start(newVersionExePath);
        }

        //public void clearoldfiles()
        //{
        //    string currentDirectory = Directory.GetCurrentDirectory();
        //    string parentDirectory = Directory.GetParent(currentDirectory)?.Parent?.FullName;

        //    if (!string.IsNullOrEmpty(parentDirectory))
        //    {
        //        string targetDirectory = System.IO.Path.Combine(parentDirectory, "Updates");

        //        if (Directory.Exists(targetDirectory))
        //        {
        //            // Delete all files except the "Updates" folder
        //            DirectoryInfo directoryInfo = new DirectoryInfo(parentDirectory);
        //            DeleteFilesAndFolders(directoryInfo, targetDirectory);
        //        }
        //    }

        //   
        //}

        //private void DeleteFilesAndFolders(DirectoryInfo directory, string targetDirectory)
        //{
        //    foreach (FileInfo file in directory.GetFiles())
        //    {
        //        if (file.Name != "Updates")
        //        {
        //            file.Delete();
        //        }
        //    }

        //    foreach (DirectoryInfo subDirectory in directory.GetDirectories())
        //    {
        //        if (subDirectory.Name != "Updates")
        //        {
        //            DeleteFilesAndFolders(subDirectory, targetDirectory);
        //            subDirectory.Delete();
        //        }
        //    }
        //}

        public async void Login()
        {
            string Username = UsernameTextBox.Text;
            MainWindow.Userstr = Username;
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var client = new HttpClient();
                    string LoginAPI = ConfigurationManager.AppSettings["APIServerName"];
                    string LoginPortNo = ConfigurationManager.AppSettings["Port"];
                    string LoginApiUrl = string.Format(@"http://{0}:{1}/api/userAuthenticationCheck?Username={2}&Password={3}", LoginAPI, LoginPortNo, UsernameTextBox.Text, LoginPasswordBox.Password);
                    string AuthenResponse = await client.GetStringAsync(LoginApiUrl);
                    string[] AuthenResponse1 = AuthenResponse.Split('_');

                    try
                    
                    {
                        string Value = AuthenResponse1[0];
                        string Value1 = Value.Remove(0,1);

                        bool Authenvalue;
                        bool.TryParse(Value1, out Authenvalue);
                        if (Authenvalue)
                        {
                            var client1 = new HttpClient();
                            var url2 = string.Format(@"http://{0}:{1}/api/userAuthorizationCheck?Username={2}&RoleCode=JSON_TOOL", LoginAPI, LoginPortNo, UsernameTextBox.Text);
                            string AuthoriseResponse = await client.GetStringAsync(url2);
                            string[] AuthoriseResponse1 = AuthoriseResponse.Split('_');
                            string value2 = AuthoriseResponse1[0];
                            string Value3 = value2.Remove(0, 1);

                            bool Authorvalue;
                            bool.TryParse(Value3, out Authorvalue);
                            if (Authorvalue)
                            {
                                var mainForm = new MainWindow();
                                mainForm.Show();
                                Close();
                            }
                            else
                            {
                                string Authorerr = "Json_Post Tool not authorised for your ID : " + UsernameTextBox.Text;
                                UILabel.Content = Authorerr;
                            }
                        }
                        else
                        {
                            string Autherr = "Invalid Authorisation Check";
                            UILabel.Content = Autherr;
                        }


                    }
                    catch (Exception ex)
                    {
                        string Autherr = "Invalid Authentication Check";
                        UILabel.Content = Autherr;
                      
                    }

                }
            }

            catch
            {
                string Autherr = "Invalid Authentication Check";
                UILabel.Content = Autherr;
            }
        }
 

        private void Close_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Logwindow.Close();
        }
        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            if (Logwindow.WindowState == WindowState.Normal)
            {
                Logwindow.WindowState = WindowState.Maximized;
            }
            else
            {
                Logwindow.WindowState = WindowState.Normal;
            }
        }
        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            Logwindow.WindowState = WindowState.Minimized;
        }

        private void window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Logwindow.DragMove();
            }
        }

        private void themeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (themeToggleButton.IsChecked == true)
            {
                AppTheme.ChangeTheme(new Uri("Themes/Dark.xaml", UriKind.Relative));
            }
            else
            {
                AppTheme.ChangeTheme(new Uri("Themes/Light.xaml", UriKind.Relative));
            }
        }


        public void LoginPasswordBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Login();
            }
        }


        public async void OnLoginClicked(object sender, RoutedEventArgs e)
        {
            Login();
        }
         










    }
}
