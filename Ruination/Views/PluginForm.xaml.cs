using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Ruination_v2.Plugins;
using Ruination_v2.Utils;
using Ruination_v2.Views.Plugins;

namespace Ruination_v2.Views;

public partial class PluginForm : UserControl
{
    public PluginForm()
    {
        InitializeComponent();

        LoadPlugins();
    }

    private void LoadPlugins()
    {
        flowlayoutPanelSearch.Children.Clear();
        foreach (var plugin in PluginUtils.GetInstalledPlugins())
        {
            try
            {
                string icon = plugin.Icon;
                if (string.IsNullOrEmpty(icon))
                    icon = "https://media.valorant-api.com/competitivetiers/564d8e28-c226-3180-6285-e48a390db8b1/0/largeicon.png";
                
                ItemCard5 itemCard = new ItemCard5(plugin.Name, icon, plugin.Name);

                itemCard.MouseDown += async delegate
                {
                    if (plugin.Type.ToLower().Equals("uefnskin"))
                    {
                        flowlayoutPanelSearch.IsEnabled = false;
                        await WaitForAnimation("PanelFadeout");
                        var opForm = new OptionForm(plugin);
                        opForm.Show();

                        while (!opForm.loaded)
                            await Task.Delay(100);

                        await WaitForAnimation("PanelFadein");
                        flowlayoutPanelSearch.IsEnabled = true;
                    }
                    else
                    {
                        new PluginSwapForm(plugin).Show();
                    }
                };
                
                flowlayoutPanelSearch.Children.Add(itemCard);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message, ex);
            }
        }
    }

    private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog();
        dialog.FileName = "Plugin File";
        dialog.DefaultExt = ".json";
        dialog.Filter = "JSON files (.json)|*.json";

        bool? result = dialog.ShowDialog();

        if (result == true)
        {
            await PluginUtils.AddPlugin(dialog.FileName);
            LoadPlugins();
            MessageBox.Show("Plugin was added.", "Ruination", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
    
    private async Task WaitForAnimation(string animation)
    {
        bool finished = false;
        Storyboard anim = (Storyboard)FindResource(animation);
        anim.Completed += delegate
        {
            finished = true;
        };
        anim.Begin(this);

        while (!finished)
            await Task.Delay(100);
    }
}