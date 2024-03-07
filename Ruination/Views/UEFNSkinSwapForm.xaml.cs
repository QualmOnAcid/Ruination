using Ruination_v2.Models;
using Ruination_v2.Swapper;
using Ruination_v2.Utils;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Ruination_v2.Views
{
    /// <summary>
    /// Interaktionslogik für UEFNSkinSwapForm.xaml
    /// </summary>
    public partial class UEFNSkinSwapForm : Window
    {

        public ApiUEFNSkinObject item;
        public Item option;

        public UEFNSkinSwapForm(ApiUEFNSkinObject item, Item option)
        {
            InitializeComponent();
            this.item = item;
            this.option = option;

            headerLabel.Content = option.name + " To " + item.Name;
            this.Title = headerLabel.Content.ToString();

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(option.icon);
            bitmapImage.EndInit();

            fromItem.skinIcon.ImageSource = bitmapImage;
            fromItem.skinnamelabel.Text = option.name;

            var bitmapImage2 = new BitmapImage();
            bitmapImage2.BeginInit();
            bitmapImage2.UriSource = new Uri(item.Icon);
            bitmapImage2.EndInit();
            toItem.skinIcon.ImageSource = bitmapImage2;
            toItem.skinnamelabel.Text = item.Name;

            Storyboard startup = (Storyboard)FindResource("Startup");
            startup.Begin(this);
        }
        
        public UEFNSkinSwapForm(PluginModel Plugin, Item option)
        {
            InitializeComponent();

            headerLabel.Content = option.name + " To " + Plugin.Name;
            this.Title = headerLabel.Content.ToString();

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(option.icon);
            bitmapImage.EndInit();

            fromItem.skinIcon.ImageSource = bitmapImage;
            fromItem.skinnamelabel.Text = option.name;

            var bitmapImage2 = new BitmapImage();
            bitmapImage2.BeginInit();
            bitmapImage2.UriSource = new Uri(Plugin.Icon);
            bitmapImage2.EndInit();
            toItem.skinIcon.ImageSource = bitmapImage2;
            toItem.skinnamelabel.Text = Plugin.Name;

            Storyboard startup = (Storyboard)FindResource("Startup");
            startup.Begin(this);

            convertBtn.Click += async delegate
            {
                await Swapper.Plugin.ConvertUEFNSkinPlugin(Plugin, option, this);
            };
            
            revertBtn.Click += async delegate
            {
                await Swapper.Plugin.RevertUEFNSkinPlugin(Plugin, option, this);
            };
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (item == null || option == null)
                return;
            
           await UEFN.ConvertUEFN(item, option, SwapUtils.GetProvider(), this);
        }

        public async void updatetext(string t)
        {
           await Dispatcher.InvokeAsync(async () =>
           {
               label.Content = t;
           });
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (item == null || option == null)
                return;
            
           await UEFN.RevertUEFN(item, option, this);
        }

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.Close();
        }

        private void OptionForm_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
    }
}
