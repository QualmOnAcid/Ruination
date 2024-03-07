using Newtonsoft.Json.Linq;
using Ruination_v2.Swapper;
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

namespace Ruination_v2.Views
{
    /// <summary>
    /// Interaktionslogik für HomeControl.xaml
    /// </summary>
    public partial class HomeControl : UserControl
    {
        public HomeControl()
        {
            InitializeComponent();

            string url = "https://fortnite-api.com/v2/news/br";

            JObject j = JObject.Parse(new System.Net.WebClient().DownloadString(url));

            string img = j["data"]["image"].ToString();

            newsImage.Position = new TimeSpan(0, 0, 1);
            newsImage.Source = new Uri(img);
            newsImage.Play();

            newsImage.MediaEnded += delegate
            {
                newsImage.Position = new TimeSpan(0, 0, 1);
                newsImage.Play();
            };
        }
    }
}
