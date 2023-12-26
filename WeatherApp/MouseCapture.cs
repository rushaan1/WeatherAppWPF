using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;

namespace WeatherApp
{
    class MouseCapture : Application 
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            EventManager.RegisterClassHandler(typeof(Window), Window.PreviewMouseDownEvent, new MouseButtonEventHandler(OnPreviewMouseDown));

            base.OnStartup(e);
        }

        static void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            MainWindow mw = new MainWindow();
        }
    }

}
