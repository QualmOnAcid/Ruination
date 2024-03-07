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
    /// Interaktionslogik für ItemCard2.xaml
    /// </summary>
    public partial class ItemCard5 : UserControl
    {
        public ItemCard5(string name, string icon, string raritycolor)
        {
            InitializeComponent();

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(icon);
            bitmapImage.EndInit();
            this.skinIcon.ImageSource = bitmapImage;
            skinnamelabel.Text = name;
            if(name.Length >= 18)
            {
                skinnamelabel.FontSize = 10;
            } else if(name.Length >= 15)
            {
                skinnamelabel.FontSize = 12;
            }
        }

        public ItemCard5 ()
        {
            InitializeComponent();
        }

    }
}
