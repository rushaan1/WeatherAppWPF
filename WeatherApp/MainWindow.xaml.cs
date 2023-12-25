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

namespace WeatherApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GeoCoordinateWatcher watcher;
        private bool setWeatherData = false;
        private char tempUnit = 'C';
        private int minTemp;
        private int maxTemp;
        private int currentTemp;
        private int feelsLike;
        public MainWindow()
        {
            Trace.WriteLine("wus good");
            InitializeComponent();
            watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.Default);
            if (!setWeatherData)
            {
                watcher.PositionChanged += SettingWeatherData;
                watcher.TryStart(false, TimeSpan.FromSeconds(20));
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
            this.setWeatherData = true;
        }
    }
}
