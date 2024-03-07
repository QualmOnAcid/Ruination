using Ruination_v2.Models;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Ruination_v2.Views
{
    /// <summary>
    /// Interaktionslogik für SwapForm.xaml
    /// </summary>
    public partial class SwapForm : Window
    {

        public Item item;
        public Item option;

        public SwapForm(Item item, Item option)
        {
            InitializeComponent();

            this.item = item;
            this.option = option;

            headerLabel.Content = option.name + " To " + item.name;
            this.Title = headerLabel.Content.ToString();

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(option.icon);
            bitmapImage.EndInit();
            fromItem.skinIcon.ImageSource = bitmapImage;
            fromItem.skinnamelabel.Text = option.name;

            var bitmapImage2 = new BitmapImage();
            bitmapImage2.BeginInit();
            bitmapImage2.UriSource = new Uri(item.icon);
            bitmapImage2.EndInit();

            toItem.skinnamelabel.Text = item.name;
            toItem.skinIcon.ImageSource = bitmapImage2;

            Storyboard startup = (Storyboard)FindResource("Startup");
            startup.Begin(this);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await Standard.Convert(item, option, label);
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            await Standard.Revert(item, option, label);
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
