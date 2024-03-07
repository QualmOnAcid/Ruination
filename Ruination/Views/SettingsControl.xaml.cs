using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Ruination_v2.Utils;

namespace Ruination_v2.Views;

public partial class SettingsControl : UserControl
{
    public SettingsControl()
    {
        InitializeComponent();
    }

    private async void Button_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        await Utils.Utils.StartFortnite();
    }

    private async void Button_Click_1(object sender, System.Windows.RoutedEventArgs e)
    {
        await Utils.Utils.ShowConvertedItems();
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(
                "This will not revert the Cosmetics and instead just reset the Config.\nDo yo you wish to proceed?",
                "Ruination", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            Config.GetConfig().ConvertedItems.Clear();
            Config.Save();  
        }
    }

    private async void ButtonBase1_OnClick(object sender, RoutedEventArgs e)
    {
        await Logger.Open();
    }

    private void ButtonBase2_OnClick(object sender, RoutedEventArgs e)
    {
        Utils.Utils.StartUrl(API.GetApi().Other.Discord);
    }

    private async void ButtonBase3_OnClick(object sender, RoutedEventArgs e)
    {
        await Utils.Utils.RevertAllItems();
    }

    private async void ButtonBase4_OnClick(object sender, RoutedEventArgs e)
    {
        string backupFolder = Utils.Utils.AppDataFolder + "\\Backups\\";
        if(Directory.Exists(backupFolder))
            Directory.Delete(backupFolder, true);
        
        Config.GetConfig().ConvertedItems.Clear();
        Config.Save();
        await Utils.Utils.VerifyFortnite();
        Environment.Exit(0);
    }
}