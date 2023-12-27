using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.IO;
using System.Device.Location;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Threading;
using XamlAnimatedGif;
using System.Collections;

namespace WeatherApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GeoCoordinateWatcher watcher;
        private char tempUnit = 'C';
        private int minTemp;
        private int maxTemp;
        private int currentTemp;
        private int feelsLike;
        private bool refreshing = false;
        private int searchItems = 0;
        private List<string> cities = new List<string>();
        private List<string> countries = new List<string>();
        private List<Button> searchButtons = new List<Button>();
        private string city = "";
        private string textBoxContentLeft = "";
        private bool sc = true;
        private static ManualResetEvent mre = new ManualResetEvent(false);


        public MainWindow()
        {
            InitializeComponent();
            SetCueBannerBackground(searchBar, "Search City");
            watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.Default);
            watcher.PositionChanged += SettingWeatherData;
            watcher.TryStart(false, TimeSpan.FromSeconds(3));

            using (var reader = new StreamReader(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files", "cities_list.csv")))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');
                    cities.Add(values[0]);
                    countries.Add(values[3]);
                }
            }

        }

        string GetOpenWeatherUrl(double lat, double lon, string unit)
        {
            string api_key = "bc0dc3b2b3b5fca1427a621c5cd66b44";
            string url = $"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&appid={api_key}&units={unit}";
            return url;                 
        }

        string Get(string uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader Reader = new StreamReader(stream))
            {
                return Reader.ReadToEnd();
            }
        }

        int convert(int celsius)
        {
            int value = celsius;
            if (tempUnit == 'F')
            {
                value = ToF(celsius);
            }
            return value;
        }

        void AddButton(string text)
        {

            Button btn = new Button() { Content = text };
            btn.Click += SearchButtonClick;
            btn.Height = 37;
            btn.Width = 274;
            btn.Background = Brushes.Snow;
            btn.FontSize = 26;
            if (text.Length > 18)
            {
                btn.FontSize = 16;
            }
            if (text.Length > 28)
            {
                btn.FontSize = 13;
            }
            mainCanvas.Children.Add(btn);
            searchButtons.Add(btn);
            Canvas.SetTop(btn, 66 + (37 * searchItems));
            Canvas.SetLeft(btn, 559);
            searchItems++;
        }

        void RemoveAllSearchButtons()
        {
            foreach (Button btn in searchButtons.ToList())
            {
                mainCanvas.Children.Remove(btn);
                searchButtons.Remove(btn);
                searchItems--;
            }
        }

        private void SearchButtonClick(object sender, RoutedEventArgs e)
        {
            SetWeatherDataCity((e.Source as Button).Content.ToString());
            location.Content = (e.Source as Button).Content.ToString();
            searchBar.Text = "";
            SetCueBannerBackground(searchBar, "Search City");
        }

        private void md(object sender, MouseButtonEventArgs e)
        {
            int x = (int)Mouse.GetPosition(mainCanvas as IInputElement).X;
            int y = (int)Mouse.GetPosition(mainCanvas as IInputElement).Y;
            Trace.WriteLine("X: " + x + "\nY:" + y);

            if (x >= 597 && y <= 67 + (37 * (searchItems)) && y>=21 && x <=835)
            {
                Trace.WriteLine("In the zone!");

                SearchGotFocus();
            }
            else
            {
                SearchLostFocus();
            }
        }

        private void SearchLostFocus()
        {
            SetCueBannerBackground(searchBar, "Search City");
            searchBar.CaretBrush = Brushes.Transparent;
            textBoxContentLeft = searchBar.Text;
            searchBar.Text = "";
        }

        private void SearchGotFocusE(object sender, RoutedEventArgs e)
        {
            SearchGotFocus();
        }

        private void SearchGotFocus()
        {
            SetCueBannerBackground(searchBar, "");
            searchBar.CaretBrush = Brushes.Black;
            if (textBoxContentLeft == "") { return; }
            searchBar.Text = textBoxContentLeft;
            textBoxContentLeft = "";
        }

        private void SetCueBannerBackground(TextBox textBox, string content)
        {
            if (content == "")
            {
                sc = false;
            }
            else
            {
                sc = true;
            }
            // Create the VisualBrush with a Label containing the cue banner
            VisualBrush cueBannerBrush = new VisualBrush
            {
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Center,
                Stretch = Stretch.None,
                Visual = new Label
                {
                    Background = Brushes.Transparent,
                    FontSize = 30,
                    Content = content,
                    Foreground = Brushes.LightGray
                }
            };

            // Set the VisualBrush as the background for the TextBox
            textBox.Background = cueBannerBrush;
        }


        private void Search(object sender, RoutedEventArgs e)
        {
            if (sc)
            {
                searchBar.Text = "";
            }
            RemoveAllSearchButtons();

            string searchText = (sender as TextBox).Text;
            List<string> matchingKeywords = new List<string>();

            if (searchText == "")
            {
                //((UIElement)sender).RaiseEvent(new RoutedEventArgs(LostFocusEvent));
                //sc.Visibility = Visibility.Visible;
                return;
            }
            else
            {
                //sc.Visibility = Visibility.Hidden;
            }

            for (int i = 0; i < cities.Count; i++)
            {
                if (cities[i].ToLower().Contains(searchText.ToLower()))
                {
                    matchingKeywords.Add(cities[i]);
                    //Trace.WriteLine(cities[i]);
                }
                else if (countries[i].ToLower().Contains(searchText.ToLower()))
                {
                    matchingKeywords.Add(countries[i]);
                    //Trace.WriteLine(countries[i]);
                }
            }

            int mkCount = 5;
            if (matchingKeywords.Count < 5)
            {
                mkCount = matchingKeywords.Count;
            }

            for (int i = 0; i < mkCount; i++)
            {
                for (int j = 0; j < cities.Count; j++)
                {
                    if (cities[j].Contains(matchingKeywords[i]))
                    {
                        AddButton($"{cities[j]}, {countries[j]}");
                        break;
                    }
                    else if (countries[j].Contains(matchingKeywords[i]))
                    {
                        foreach (string city in cities)
                        {
                            if (countries[cities.IndexOf(city)] == countries[j])
                            {
                                matchingKeywords.RemoveAll((string item) => { return item == countries[j]; });
                                if (matchingKeywords.Contains(city) == false)
                                {
                                    matchingKeywords.Add(city);
                                }
                            }
                        }
                        AddButton($"{cities[j]}, {countries[j]}");
                        break;
                    }
                }
            }
        }

        void SetBackground(string weather)
        {
            switch (weather)
            {
                case "Clear":
                    Uri uri = new Uri("/Images/clearskylooop.gif", UriKind.Relative);
                    AnimationBehavior.SetSourceUri(bg, uri);
                    break;
                case "Snow":
                    Uri uri1 = new Uri("/Images/snow.gif", UriKind.Relative);
                    AnimationBehavior.SetSourceUri(bg, uri1);
                    break;
                case "Rain":
                    Uri uri2 = new Uri("/Images/rain.gif", UriKind.Relative);
                    AnimationBehavior.SetSourceUri(bg, uri2);
                    break;
                case "Drizzle":
                    Uri uri3 = new Uri("/Images/rain.gif", UriKind.Relative);
                    AnimationBehavior.SetSourceUri(bg, uri3);
                    break;
                case "Thunderstorm":
                    Uri uri4 = new Uri("/Images/thunderstorm.gif", UriKind.Relative);
                    AnimationBehavior.SetSourceUri(bg, uri4);
                    break;
            }
        }

        int ToC(int f)
        {
            double c = (f - 32) * 5 / 9;
            return Convert.ToInt32(c);
        }

        int ToF(int c)
        {
            double f = (c * 9.0) / 5.0 + 32.0;
            return Convert.ToInt32(f);
        }

        private void Refresh(object sender, RoutedEventArgs e)
        {
            if (refreshing)
            {
                return;
            }
            refreshing = true;
            mre = new ManualResetEvent(false);

            Thread thread;

            if (city != "")
            {
                SetWeatherDataCity(this.city);
                thread = new Thread(() => { RefreshThread(false); });
            }
            else
            {
                watcher.PositionChanged += SettingWeatherData;
                thread = new Thread(() => { RefreshThread(true); });
            }
            thread.Start();
        }

        private void RefreshThread(bool manualWait)
        {
            this.Dispatcher.Invoke(() =>
            {
                refreshIcon.Visibility = Visibility.Hidden;
                refreshGif.Visibility = Visibility.Visible;
            });
            if (!manualWait)
            {
                Thread.Sleep(5000);
            }
            else
            {
                Trace.WriteLine("WAITING FOR MRE");
                mre.WaitOne();
            }
            this.Dispatcher.Invoke(() =>
            {
                refreshIcon.Visibility = Visibility.Visible;
                refreshGif.Visibility = Visibility.Hidden;
            });
            refreshing = false;
        }

        private void Celsius(object sender, RoutedEventArgs e)
        {
            if (tempUnit == 'F')
            {
                currentTemp = ToC(currentTemp);
                feelsLike = ToC(feelsLike);
                minTemp = ToC(minTemp);
                maxTemp = ToC(maxTemp);

                temperature.Content = currentTemp + "°C";
                feelslike.Content = "Feels Like " + feelsLike + "°C";
                min.Content = "Min " + minTemp + "°C";
                max.Content = "Max " + maxTemp + "°C";
            }

            tempUnit = 'C';
            Canvas.SetLeft(snowOverlay, 358);
        }


        private void Faren(object sender, RoutedEventArgs e)
        {
            if (tempUnit == 'C')
            {
                temperature.Content = ToF(currentTemp) + "°F";
                currentTemp = ToF(currentTemp);
                feelslike.Content = "Feels Like " + ToF(feelsLike) + "°F";
                feelsLike = ToF(feelsLike);
                min.Content = "Min " + ToF(minTemp) + "°F";
                minTemp = ToF(minTemp);
                max.Content = "Max " + ToF(maxTemp) + "°F";
                maxTemp = ToF(maxTemp);
            }
            tempUnit = 'F';
            Canvas.SetLeft(snowOverlay, 411);
        }

        private double[] latlong(string city)
        {
            double[] latlongs = { 0.00, 0.00 };
            string Response = Get($"http://api.openweathermap.org/geo/1.0/direct?q={city}&limit=5&appid=bc0dc3b2b3b5fca1427a621c5cd66b44");
            JsonTextReader reader = new JsonTextReader(new StringReader(Response));
            bool latlongset = false;
            while (reader.Read())
            {
                if (reader.Value != null)
                {
                    if (reader.Value.Equals("lat") && !latlongset)
                    {
                        reader.Read();
                        latlongs[0] = (double)reader.Value;
                    }
                    if (reader.Value.Equals("lon") && !latlongset)
                    {
                        reader.Read();
                        latlongs[1] = (double)reader.Value;
                        latlongset = true;
                    }
                }
            }
            return latlongs;
        }

        private void SetWeatherDataCity(string cityName)
        {
            double[] latlongss = latlong(cityName);
            string Response = Get(GetOpenWeatherUrl(latlongss[0], latlongss[1], "metric"));
            Trace.WriteLine(Response);
            JsonTextReader reader = new JsonTextReader(new StringReader(Response));
            bool mainSet = false;
            while (reader.Read())
            {
                if (reader.Value != null)
                {
                    if (reader.Value.Equals("temp"))
                    {
                        reader.Read();
                        double temp = (double)reader.Value;
                        int tempInt = Convert.ToInt32(temp);
                        currentTemp = convert(tempInt);
                        this.Dispatcher.Invoke(() => { temperature.Content = convert(tempInt) + "°C"; });
                    }
                    if (reader.Value.Equals("main") && !mainSet)
                    {
                        reader.Read();
                        weather.Content = (string)reader.Value;
                        mainSet = true;
                        SetBackground((string)reader.Value);
                    }
                    if (reader.Value.Equals("feels_like"))
                    {
                        reader.Read();
                        double fl = (double)reader.Value;
                        this.feelsLike = convert(Convert.ToInt32(fl));
                        feelslike.Content = "Feels Like " + convert(Convert.ToInt32(fl)) + "°C";
                    }
                    if (reader.Value.Equals("temp_min"))
                    {
                        reader.Read();
                        double mint = (double)reader.Value;
                        this.minTemp = convert(Convert.ToInt32(mint));
                        min.Content = "Min " + convert(Convert.ToInt32(mint)) + "°C";
                    }
                    if (reader.Value.Equals("temp_max"))
                    {
                        reader.Read();
                        double maxt = (double)reader.Value;
                        this.maxTemp = convert(Convert.ToInt32(maxt));
                        max.Content = "Max " + convert(Convert.ToInt32(maxt)) + "°C";
                    }


                    if (reader.Value.Equals("pressure"))
                    {
                        reader.Read();
                        Trace.WriteLine(reader.Value);
                        //int v = (int)reader.Value;
                        presssure.Content = "Air Pressure: " + reader.Value;
                    }
                    if (reader.Value.Equals("humidity"))
                    {
                        reader.Read();
                        //int v = (int)reader.Value;
                        humidity.Content = "Humidity: " + reader.Value + "%";
                    }
                    if (reader.Value.Equals("visibility"))
                    {
                        reader.Read();
                        //int v = (int)reader.Value;
                        visibility.Content = "Visibility: " + reader.Value + "m";
                    }
                    if (reader.Value.Equals("speed"))
                    {
                        reader.Read();
                        //double v = (double)reader.Value;
                        wspeed.Content = "Wind Speed: " + reader.Value + "m/s";
                    }
                }
            }
            this.city = cityName;
        }

        private void SettingWeatherData(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            // This event is triggered when the location is changed
            GeoCoordinate coord = e.Position.Location;

            double latitude = coord.Latitude;
            double longitude = coord.Longitude;

            string Response = Get(GetOpenWeatherUrl(latitude, longitude, "metric"));
            Trace.WriteLine(Response);
            JsonTextReader reader = new JsonTextReader(new StringReader(Response));
            bool mainSet = false;
            string countrycode = "";
            string cityy = "";
            while (reader.Read())
            {
                if (reader.Value != null)
                {
                    if (reader.Value.Equals("temp"))
                    {
                        reader.Read();
                        double temp = (double)reader.Value;
                        int tempInt = Convert.ToInt32(temp);
                        currentTemp = convert(tempInt);
                        this.Dispatcher.Invoke(() => { temperature.Content = convert(tempInt) + "°C"; });
                    }
                    if (reader.Value.Equals("main") && !mainSet)
                    {
                        reader.Read();
                        weather.Content = (string)reader.Value;
                        mainSet = true;
                        SetBackground((string)reader.Value);
                    }
                    if (reader.Value.Equals("feels_like"))
                    {
                        reader.Read();
                        double fl = (double)reader.Value;
                        this.feelsLike = convert(Convert.ToInt32(fl));
                        feelslike.Content = "Feels Like " + convert(Convert.ToInt32(fl)) + "°C";
                    }
                    if (reader.Value.Equals("temp_min"))
                    {
                        reader.Read();
                        double mint = (double)reader.Value;
                        this.minTemp = convert(Convert.ToInt32(mint));
                        min.Content = "Min " + convert(Convert.ToInt32(mint)) + "°C";
                    }
                    if (reader.Value.Equals("temp_max"))
                    {
                        reader.Read();
                        double maxt = (double)reader.Value;
                        this.maxTemp = convert(Convert.ToInt32(maxt));
                        max.Content = "Max " + convert(Convert.ToInt32(maxt)) + "°C";
                    }


                    if (reader.Value.Equals("pressure"))
                    {
                        reader.Read();
                        Trace.WriteLine(reader.Value);
                        //int v = (int)reader.Value;
                        presssure.Content = "Air Pressure: " + reader.Value;
                    }
                    if (reader.Value.Equals("humidity"))
                    {
                        reader.Read();
                        //int v = (int)reader.Value;
                        humidity.Content = "Humidity: " + reader.Value + "%";
                    }
                    if (reader.Value.Equals("visibility"))
                    {
                        reader.Read();
                        //int v = (int)reader.Value;
                        visibility.Content = "Visibility: " + reader.Value + "m";
                    }
                    if (reader.Value.Equals("speed"))
                    {
                        reader.Read();
                        //double v = (double)reader.Value;
                        wspeed.Content = "Wind Speed: " + reader.Value + "m/s";
                    }
                    if (reader.Value.Equals("name"))
                    {
                        reader.Read();
                        cityy = "" + reader.Value;
                    }
                    if (reader.Value.Equals("country"))
                    {
                        reader.Read();
                        countrycode = "" + reader.Value;
                    }
                }
            }
            this.city = "";
            location.Content = cityy + ", " + countrycode;
            watcher.PositionChanged -= SettingWeatherData;
            searchBar.IsReadOnly = false;
            mre.Set();
            overlay1.Visibility = Visibility.Hidden;
            overlay2.Visibility = Visibility.Hidden;
            overlay1.IsEnabled = false;
            overlay2.IsEnabled = false;
        }

        private void mc(object sender, RoutedEventArgs e)
        {
            //if (searchBar.Text == "" )
            //{
            //    return;
            //}
            searchBar.Text = "";
            textBoxContentLeft = "";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.city == "")
            {
                return;
            }
            watcher.PositionChanged += SettingWeatherData;
            mre = new ManualResetEvent(false);
            Thread thread = new Thread(() => { RefreshThread(true); });
            thread.Start();
            searchBar.IsReadOnly = true;
        }
    }
}
