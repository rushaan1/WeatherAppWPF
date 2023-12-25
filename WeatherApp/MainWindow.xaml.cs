﻿using System;
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

        public MainWindow()
        {
            InitializeComponent();
            watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.Default);
            watcher.PositionChanged += SettingWeatherData;
            watcher.TryStart(false, TimeSpan.FromSeconds(3));

            using (var reader = new StreamReader(@"C:\Users\rusha\source\repos\WeatherApp\WeatherApp\Files\cities_list.csv"))
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
            btn.Height = 21;
            btn.Width = 153;
            btn.Background = Brushes.Snow;
            mainCanvas.Children.Add(btn);
            searchButtons.Add(btn);
            Canvas.SetTop(btn, 69 + (21*searchItems));
            Canvas.SetLeft(btn, 643);
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
            Trace.WriteLine("Working");
        }

        private void Search(object sender, RoutedEventArgs e) 
        {
            RemoveAllSearchButtons();

            string searchText = (sender as TextBox).Text;
            List<string> matchingKeywords = new List<string>();

            if (searchText == "") 
            {
                return;
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
                        Trace.WriteLine(countries[j]);
                        AddButton($"{cities[j]}, {countries[j]}");
                        break; 
                    }
                    else if (countries[j].Contains(matchingKeywords[i]))
                    {
                        Trace.WriteLine(countries[j]);
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
                    Uri uri = new Uri("/Images/clearsky.gif", UriKind.Relative);
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
            watcher.PositionChanged += SettingWeatherData;

            Thread thread = new Thread(RefreshThread);
            thread.Start();
        }

        private void RefreshThread() 
        {
            this.Dispatcher.Invoke(() =>
            {
                refreshIcon.Visibility = Visibility.Hidden;
                refreshGif.Visibility = Visibility.Visible;
            });
            Thread.Sleep(5000);
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
                        feelslike.Content = "Feels Like "+convert(Convert.ToInt32(fl)) + "°C";
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
                        humidity.Content = "Humidity: " + reader.Value +"%";
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
                        wspeed.Content = "Wind Speed: " + reader.Value +"m/s";
                    }
                }
            }
            watcher.PositionChanged -= SettingWeatherData;
        }
    }
}
