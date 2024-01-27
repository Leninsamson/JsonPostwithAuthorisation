using System;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Timers;

using Newtonsoft.Json;
using System.Windows.Threading;

namespace HttpTest
{
    public partial class MainWindow : Window
    {
        private string resultend;
        public static string Userstr { get; set; }
        public static string Versnumber { get; set; }
        private Timer Api_Timer;
        private bool isTimerRunning = false;
        private DateTime timerStartTime;
        private DispatcherTimer timer;

        //  private JsonReader responsejson;

        public MainWindow()
        {
            InitializeComponent();
            UserID.Content = Userstr;
            ClientvMain.Content = Versnumber;

            timer = new DispatcherTimer();
            timer.Tick += timer_Tick;
            timer.Interval = TimeSpan.FromMilliseconds(1);

        }

        public class Jsonmodel
        {
            public string state { get; set; }
            public string description { get; set; }
            public object sn { get; set; }
        }

        public async void SendJson(string json, string url)
        {
            try
            {
              
                var data = new StringContent(json, Encoding.UTF8, "application/json");

                HttpClient  client = new HttpClient();

                var response = await client.PostAsync(url, data);

                if (response.IsSuccessStatusCode)
                {
                    string resultend = await response.Content.ReadAsStringAsync();
                    Jsonmodel jsonmodel = JsonConvert.DeserializeObject<Jsonmodel>(resultend);
                    string statestring = jsonmodel.state;
                    Responsecont.Content = resultend;
                    if (jsonmodel != null)
                    {
                        string pattern = @"^1(-1)*$";

                        // Check if the state matches the pattern
                        if (Regex.IsMatch(jsonmodel.state, pattern))
                        {
                          
                              Responsecont.Background = Brushes.Green;
                            // Stop the timer if it's running
                            timer.Stop();
                            TimeSpan elapsedTime = DateTime.Now - timerStartTime;

                            // Show the elapsed time in the Timer_label label
                            double timer1 = Math.Floor(elapsedTime.TotalMilliseconds);
                                Timer_label.Content = timer1 + " ms";


                                if (timer1 < 1000)
                                        {
                                            Timer_label.Background = Brushes.Green;
                                        }
                                        else
                                        {

                                            Timer_label.Background = Brushes.IndianRed;
                                        }

                            
                        }

                        else
                        {

                            Responsecont.Background = Brushes.IndianRed;
                            // Stop the timer if it's running
                            timer.Stop();
                            TimeSpan elapsedTime = DateTime.Now - timerStartTime;

                            // Show the elapsed time in the Timer_label label
                            double timer1 = Math.Floor(elapsedTime.TotalMilliseconds);
                            Timer_label.Content = timer1 + " ms";


                            if (timer1 < 1000)
                            {
                                Timer_label.Background = Brushes.Green;
                            }
                            else
                            {

                                Timer_label.Background = Brushes.IndianRed;
                            }

                        }
                    }
                    

                }
                else
                {
                    timer.Stop();
                    string errormesspost = "Invalid HTTP Post API Request";
                    Responsecont.Content = errormesspost;
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                string errormess = "Unable to send the request to endpoint URL";
                Responsecont.Content = errormess;

            }

        }

        private void Close_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            window.Close();
        }
        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            if (window.WindowState == WindowState.Normal)
            {
                window.WindowState = WindowState.Maximized;
            }
            else
            {
                window.WindowState = WindowState.Normal;
            }
        }
        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            window.WindowState = WindowState.Minimized;
        }

        private void window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                window.DragMove();
            }
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            string url = apitextbox.Text;
            string json = Datatextbox.Text;
            // Start the timer
            var fieldColor = FindResource("Fieldcolor") as Brush;

            // Set the background color of the Timer_label
            Timer_label.Background = fieldColor;
            timerStartTime = DateTime.Now;
            timer.Start();
            SendJson(json, url);
           
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

        private void Datatextbox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                string url = apitextbox.Text;
                string json = Datatextbox.Text;
                // Start the timer
                var fieldColor = FindResource("Fieldcolor") as Brush;

                // Set the background color of the Timer_label
                Timer_label.Background = fieldColor;
                timerStartTime = DateTime.Now;
                timer.Start();
                SendJson(json, url);

            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            TimeSpan elapsedTime = DateTime.Now - timerStartTime;

            // Show the elapsed time in the Timer_label label
            double timer0 = Math.Floor(elapsedTime.TotalMilliseconds);
            Timer_label.Content = timer0 + " ms";
        }



    }

}






