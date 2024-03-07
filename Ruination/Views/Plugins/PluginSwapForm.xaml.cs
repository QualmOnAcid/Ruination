using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Ruination_v2.Models;

namespace Ruination_v2.Views.Plugins;

public partial class PluginSwapForm : Window
{
    public PluginSwapForm(PluginModel Plugin)
    {
        InitializeComponent();
        
        headerLabel.Content = Plugin.Option.Name + " To " + Plugin.Name;
        this.Title = headerLabel.Content.ToString();

        string optionIcon = Plugin.Option.Icon;
        if (string.IsNullOrEmpty(optionIcon))
            optionIcon =
                "https://media.valorant-api.com/competitivetiers/564d8e28-c226-3180-6285-e48a390db8b1/0/largeicon.png";
        
        string icon = Plugin.Icon;
        if (string.IsNullOrEmpty(icon))
            icon =
                "https://media.valorant-api.com/competitivetiers/564d8e28-c226-3180-6285-e48a390db8b1/0/largeicon.png";

        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.UriSource = new Uri(optionIcon);
        bitmapImage.EndInit();
        fromItem.skinIcon.ImageSource = bitmapImage;
        fromItem.skinnamelabel.Text = Plugin.Option.Name;

        var bitmapImage2 = new BitmapImage();
        bitmapImage2.BeginInit();
        bitmapImage2.UriSource = new Uri(icon);
        bitmapImage2.EndInit();

        toItem.skinnamelabel.Text = Plugin.Name;
        toItem.skinIcon.ImageSource = bitmapImage2;

        Storyboard startup = (Storyboard)FindResource("Startup");
        startup.Begin(this);

        convertBtn.Click += async delegate
        {
            await Swapper.Plugin.ConvertPlugin(Plugin, label);
        };

        revertBtn.Click += async delegate
        {
            await Swapper.Plugin.RevertPlugin(Plugin, label);
        };
    }

    private void PluginSwapForm_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if(e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        this.Close();
    }
}