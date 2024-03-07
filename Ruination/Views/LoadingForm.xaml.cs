using Newtonsoft.Json;
using Ruination_v2.Models;
using Ruination_v2.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CUE4Parse.UE4.Assets;
using Ruination_v2.Swapper;

namespace Ruination_v2.Views
{
    /// <summary>
    /// Interaktionslogik für LoadingForm.xaml
    /// </summary>
    public partial class LoadingForm : Window
    {
        public LoadingForm()
        {
            InitializeComponent();
            imageAwesome.Spin = true;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Storyboard startAnim = (Storyboard)FindResource("Startup");

            startAnim.Completed += async delegate
            {
                imageAwesome.Spin = true;
                
                System.Net.ServicePointManager.SecurityProtocol =
                    SecurityProtocolType.Tls12 |
                    SecurityProtocolType.Tls11 |
                    SecurityProtocolType.Tls;

                if(!await Epicgames.Load())
                {
                    MessageBox.Show("Fortnite Installation was not found.");
                }

                label.Content = "Loading API";
                await Utils.API.Load();
                Config.Load();

                if (API.GetApi().Other.Version != Utils.Utils.Version)
                {
                    MessageBox.Show("You are running an old version of Ruination. Please download a new one at:\n" +
                                    API.GetApi().Other.Discord, "Ruination", MessageBoxButton.OK, MessageBoxImage.Information);
                    Environment.Exit(0);
                }

                if (API.GetApi().Other.Disabled)
                {
                    MessageBox.Show(API.GetApi().Other.DisabledMessage, "Ruination", MessageBoxButton.OK, MessageBoxImage.Information);
                    Environment.Exit(0);
                }
                
                new Thread(async () =>
                {
                    await Utils.DiscordRPC.Load();
                }).Start();
                
                DateTime dt = DateTime.Now;
                bool ignoreKeyCheck = Config.GetConfig().KeyValidTime > dt;

                if (!ignoreKeyCheck)
                {
                    if (!Utils.Utils.IsPremium && !await BstlarKey.CheckConfigKey())
                    {
                        new KeyForm().Show();
                        this.Close();
                        return;
                    }   
                }
                
                Utils.Utils.CheckForBackupChanges(Epicgames.GetPaksPath());
                
                await DoSlide("Downloading Mappings");
                await Utils.Mappings.DownloadMappings();
                await DoSlide("Loading Provider");
                await Utils.SwapUtils.LoadProvider();
                await DoSlide("Parsing Cosmetics");
                await Utils.Utils.ParseAllCosmetics();
                await DoSlide("Checking Ruination Utils");
                await Utils.Utils.CheckRuinationUtils(label);
                await DoSlide("All done!");

                Storyboard fadeOut = (Storyboard)FindResource("Fadeout");
                fadeOut.Completed += delegate
                {
                    new newui().Show();
                    this.Close();
                };
                fadeOut.Begin(this);

            };

            startAnim.Begin(this);
        }

        private async Task DoSlide(string text)
        {
            bool finished = false;
            Storyboard startAnim = (Storyboard)FindResource("TextSlide");
            startAnim.Completed += delegate
            {
                label.Content = text;
                Storyboard inAnim = (Storyboard)FindResource("TextSlideIn");
                inAnim.Completed += delegate
                {
                    finished = true;
                };
                inAnim.Begin(this);
            };
            startAnim.Begin(this);

            while (!finished)
                await Task.Delay(100);
        }

    }
}
