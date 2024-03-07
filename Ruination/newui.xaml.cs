using Ruination.customcontrols;
using Ruination_v2.Models;
using Ruination_v2.Utils;
using Ruination_v2.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
using Newtonsoft.Json;

namespace Ruination_v2
{
    /// <summary>
    /// Interaktionslogik für newui.xaml
    /// </summary>
    public partial class newui : Window
    {

        private bool isUefnTab = false;
        private List<ApiUEFNSkinObject> uefnSkins = new();
        private List<Item> currentItems = new();
        private bool isAllowedToSearch = false;
        
        public newui()
        {
            InitializeComponent();
            Storyboard storyboard = (Storyboard)FindResource("Fadein");
            storyboard.Begin(this);
            versionText.Text = "v" + Utils.Utils.Version;
            flowlayoutPanel1.Children.Add(new HomeControl());

            string url = "https://media.valorant-api.com/competitivetiers/564d8e28-c226-3180-6285-e48a390db8b1/0/largeicon.png";
            string name = "Unkown";

            if (Utils.Utils.CurrentUser.AvatarUrl != null)
                url = Utils.Utils.CurrentUser.AvatarUrl;
            
            if (Utils.Utils.CurrentUser.Name != null)
                name = Utils.Utils.CurrentUser.Name;
            
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(url);
            bitmapImage.EndInit();
            cardImg.ImageSource = bitmapImage;
            nameLabel.Text = name;
            Utils.DiscordRPC.UpdatePresence("Watching Home");

            if (!Utils.Utils.IsPremium)
            {
                titleLabel.Text = "STANDARD";
            }
        }

        private async void MenuButtonClick(object sender, RoutedEventArgs e)
        {
            MenuButton btn = (MenuButton)sender;
            Logger.Log("MainWindow Button Pressed: " + btn.Text);
            flowlayoutPanel1.Children.Clear();
            isAllowedToSearch = false;
            switch (btn.Text)
            {
                case "Skins":
                    Task.Run(async () => await LoadUEFNSkins());
                    isAllowedToSearch = true;
                    break;
                case "Backpacks":
                    await LoadTab(ItemType.BACKPACK);
                    MessageBox.Show("Backpacks do not work on swapped Skins.", "Ruination", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    isAllowedToSearch = true;
                    break;
                case "Pickaxes":
                    await LoadTab(ItemType.PICKAXE);
                    isAllowedToSearch = true;
                    if (API.GetApi().Other.DoPickaxeWarning)
                    {
                        MessageBox.Show("Do not swap any pickaxes that have a different series.", "Ruination", MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    break;
                case "Emotes":
                    await LoadTab(ItemType.EMOTE);
                    isAllowedToSearch = true;
                    break;
                case "Misc":
                    await LoadTab(ItemType.MISC);
                    isAllowedToSearch = true;
                    break;
                case "Cars":
                    await LoadTab(ItemType.CAR);
                    isAllowedToSearch = true;
                    MessageBox.Show(
                        "If you swap any wheel/trail or boost you will only have the color options of the item you swap from\n\nIf your swapped cosmetics show in a different color you need to die",
                        "Ruination", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
                case "Home":
                    scrollViewer.Visibility = Visibility.Visible;
                    scrollViewerSearch.Visibility = Visibility.Hidden;
                    flowlayoutPanel1.Children.Clear();
                    flowlayoutPanel1.Children.Add(new HomeControl());
                    break;
                case "Settings":
                    scrollViewer.Visibility = Visibility.Visible;
                    scrollViewerSearch.Visibility = Visibility.Hidden;
                    flowlayoutPanel1.Children.Clear();
                    flowlayoutPanel1.Children.Add(new SettingsControl());
                    break;
                case "Plugins":
                    scrollViewer.Visibility = Visibility.Visible;
                    scrollViewerSearch.Visibility = Visibility.Hidden;
                    flowlayoutPanel1.Children.Clear();
                    flowlayoutPanel1.Children.Add(new PluginForm());
                    break;
            }
            
            Utils.DiscordRPC.UpdatePresence("Watching " + btn.Text);

            tabLabel.Content = btn.Text;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
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
                await Task.Delay(50);
        }

        public async Task LoadUEFNSkins()
        {
            if(!Utils.Utils.CachedItems.ContainsKey(ItemType.SKIN))
                Utils.Utils.CachedItems.Add(ItemType.SKIN, await Utils.Utils.GetTabItems("outfit"));

            isUefnTab = true;
            uefnSkins = API.GetApi().Characters;

            new Thread(async () =>
            {
                Thread.CurrentThread.IsBackground = true;
                await Dispatcher.InvokeAsync( async ()=>
                {
                    foreach (var item in API.GetApi().Characters)
                    {
                        ItemCard5 itemCard = CreateUefnCard(item);
                        flowlayoutPanel1.Children.Add(itemCard);
                    }

                    var versionsObj = API.GetPluginsApi().Versions
                        .FirstOrDefault(x => x.Version == Epicgames.GetAPIFnVersion());

                    if (versionsObj != null)
                    {
                        foreach (var item in versionsObj.Plugins)
                        {
                            try
                            {
                                ItemCard5 itemCard = CreateUefnCard(item);
                                flowlayoutPanel1.Children.Add(itemCard);
                            } catch(Exception ex) {}
                        }    
                    }
                    
                    scrollViewer.ScrollToTop();
                    scrollViewer.Visibility = Visibility.Visible;
                    scrollViewerSearch.Visibility = Visibility.Hidden;
                }, System.Windows.Threading.DispatcherPriority.Background);
            }).Start();
        }

        public async Task LoadTab(ItemType type)
        {
            isUefnTab = false;
            string tab = string.Empty;

            switch(type)
            {
                case ItemType.SKIN:
                    tab = "outfit";
                    break;
                case ItemType.BACKPACK:
                    tab = "backpack";
                    break;
                case ItemType.PICKAXE:
                    tab = "pickaxe";
                    break;
                case ItemType.EMOTE:
                    tab = "emote";
                    break;
                case ItemType.MISC:
                    tab = "misc";
                    break;
                case ItemType.CAR:
                    tab = "car";
                    break;
            }

            if (tab == string.Empty)
                return;

            var tabitems = Utils.Utils.CachedItems.ContainsKey(type) ? Utils.Utils.CachedItems[type] : await Utils.Utils.GetTabItems(tab);
            
            if(!Utils.Utils.CachedItems.ContainsKey(type))
                Utils.Utils.CachedItems.Add(type, tabitems);

            this.flowlayoutPanel1.Children.Clear();

            isUefnTab = false;
            currentItems = tabitems;

            new Thread(async () =>
            {
                Thread.CurrentThread.IsBackground = true;

                await Dispatcher.InvokeAsync(() =>
                {
                    foreach (var item in Utils.Utils.CachedItems[type])
                    {
                        if (item.Type == ItemType.BACKPACK && !API.GetApi().BackpackIDS.Contains(item.id))
                            continue;
                        
                        ItemCard5 _card = CreateNormalCard(item);
                        this.flowlayoutPanel1.Children.Add(_card);
                    }
                    scrollViewer.ScrollToTop();
                    scrollViewer.Visibility = Visibility.Visible;
                    scrollViewerSearch.Visibility = Visibility.Hidden;
                });

            }).Start();
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                if (!isAllowedToSearch)
                    return;
                
                string thingToSearch = searchBwar.Text;
                flowlayoutPanelSearch.Children.Clear();

                if (string.IsNullOrEmpty(thingToSearch))
                {
                    scrollViewer.Visibility = Visibility.Visible;
                    scrollViewerSearch.Visibility = Visibility.Hidden;
                    return;
                }

                if (isUefnTab)
                {
                    foreach (var item in uefnSkins)
                    {
                        if (item.Name.ToLower().Contains(thingToSearch.ToLower()))
                        {
                            flowlayoutPanelSearch.Children.Add(CreateUefnCard(item));
                        }
                    }
                    
                    var versionsObj = API.GetPluginsApi().Versions
                        .FirstOrDefault(x => x.Version == Epicgames.GetAPIFnVersion());

                    if (versionsObj != null)
                    {
                        foreach (var item in versionsObj.Plugins)
                        {
                            try
                            {
                                if (item.Name.ToLower().Contains(thingToSearch.ToLower()))
                                {
                                    ItemCard5 itemCard = CreateUefnCard(item);
                                    flowlayoutPanelSearch.Children.Add(itemCard);   
                                }
                            } catch(Exception ex) {}
                        }    
                    }
                    
                }
                else
                {
                    foreach (var item in currentItems)
                    {
                        if (item.Type == ItemType.BACKPACK && !API.GetApi().BackpackIDS.Contains(item.id))
                            continue;
                        
                        if (item.name.ToLower().Contains(thingToSearch.ToLower()))
                        {
                            flowlayoutPanelSearch.Children.Add(CreateNormalCard(item));
                        }
                    }
                }

                scrollViewer.Visibility = Visibility.Hidden;
                scrollViewerSearch.Visibility = Visibility.Visible;
            }
        }

        private ItemCard5 CreateUefnCard(ApiUEFNSkinObject skin)
        {
            ItemCard5 card = new ItemCard5(skin.Name, skin.Icon, skin.Name);
            card.MouseLeftButtonDown += async delegate
            {
                if (!string.IsNullOrEmpty(skin.Info))
                {
                    if (MessageBox.Show(skin.Info + "\n\nDo you wish to proceed?", "Ruination Swapper", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        flowlayoutPanel1.IsEnabled = false;
                        await WaitForAnimation("PanelFadeout");

                        new OptionForm(skin).Show();

                        await WaitForAnimation("PanelFadein");
                        flowlayoutPanel1.IsEnabled = true;
                    }
                }
                else
                {
                    flowlayoutPanel1.IsEnabled = false;
                    await WaitForAnimation("PanelFadeout");

                    new OptionForm(skin).Show();

                    await WaitForAnimation("PanelFadein");
                    flowlayoutPanel1.IsEnabled = true;
                }
            };
            return card;
        }
        
        private ItemCard5 CreateUefnCard(ApiPluginsVersionPluginObject Plugin)
        {
            try
            {
                ItemCard5 card = new ItemCard5(Plugin.Name, Plugin.Icon, Plugin.Name);
                card.MouseLeftButtonDown += async delegate
                {
                    flowlayoutPanel1.IsEnabled = false;
                    await WaitForAnimation("PanelFadeout");

                    try
                    {
                        string downloadedJson = new WebClient().DownloadString(Plugin.PluginURL);
                        PluginModel pluginModel = JsonConvert.DeserializeObject<PluginModel>(downloadedJson);

                        new OptionForm(pluginModel).Show();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex.Message, ex);
                        MessageBox.Show("This item is in an incorrect format: " + Plugin.Name, "Ruination", MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }

                    await WaitForAnimation("PanelFadein");
                    flowlayoutPanel1.IsEnabled = true;
                };
                return card;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message, ex);
                MessageBox.Show("This item is in an incorrect format: " + Plugin.Name, "Ruination", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return null;
            }
        }
        
        private void searchBwar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (searchBwar.Text.Length > 0)
            {
                textBlock.Visibility = Visibility.Hidden;
            }
            else
            {
                scrollViewerSearch.Visibility = Visibility.Hidden;
                scrollViewer.Visibility = Visibility.Visible;   
                textBlock.Visibility = Visibility.Visible;
            }
        }

        private ItemCard5 CreateNormalCard(Item item)
        {
            ItemCard5 card = new ItemCard5(item.name, item.icon, item.rarcolor);
            card.MouseLeftButtonDown += async delegate
            {
                flowlayoutPanel1.IsEnabled = false;
                await WaitForAnimation("PanelFadeout");

                if (item.id == "ruination_fov")
                {
                    new FOVForm().Show();
                }
                else if (item.id == "ruination_background")
                {
                    MessageBox.Show("Lobby Backgrounds will be re-added soon.", "Ruination", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    var options = Utils.Utils.CachedItems[item.Type].ToList()
                        .Where(x => x.Type == item.Type && Options.IsOption(x, item)).ToList();

                    new OptionForm(options, item).Show();   
                }

                await WaitForAnimation("PanelFadein");
                flowlayoutPanel1.IsEnabled = true;
            };

            return card;
        }

        private void Newui_OnClosed(object? sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
            {
                Environment.Exit(0);
            }
        }
    }
}
