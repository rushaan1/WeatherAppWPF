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
        public MainWindow()
        {
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

        private void SettingWeatherData(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            // This event is triggered when the location is changed
            GeoCoordinate coord = e.Position.Location;

            double latitude = coord.Latitude;
            double longitude = coord.Longitude;

            string Response = Get(GetOpenWeatherUrl(latitude, longitude, "metric"));
            Trace.WriteLine(Response);
            JsonTextReader reader = new JsonTextReader(new StringReader(Response));
            while (reader.Read())
            {
                if (reader.Value != null)
                {
                    if (reader.Value.Equals("temp"))
                    {
                        reader.Read();
                        double temp = (double)reader.Value;
                        int tempInt = Convert.ToInt32(temp);
                        this.Dispatcher.Invoke(() => { temperature.Content = tempInt + "°C"; });
                        setWeatherData = true;
                    }
                }
            }
        }
    }
}
