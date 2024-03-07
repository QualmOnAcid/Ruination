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
using System.Threading;
using System.ComponentModel;
using System.IO;
using Ruination_v2.Models;
using Ruination_v2.Utils;

namespace Ruination_v2.Views
{
    /// <summary>
    /// Interaktionslogik für OptionForm.xaml
    /// </summary>
    public partial class OptionForm : Window
    {
        private List<Item> _options;
        private bool isUefnItem = false;
        private Item item;
        private ApiUEFNSkinObject uefnItem;
        private bool isUefnSkinPlugin = false;
        private PluginModel Plugin;
        public bool loaded = false;
        
        public OptionForm(List<Item> options, Item item)
        {
            InitializeComponent();

            _options = options;
            isUefnItem = false;
            this.item = item;
            
            Utils.DiscordRPC.UpdatePresence($"Watching {options.Count} Options for " + item.name);
            this.Title = item.name;
            
             new Thread(async () =>
             {
                 Thread.CurrentThread.IsBackground = true;

                 if(item.Type == ItemType.SKIN)
                 {
                     var transformCharacters = Options.GetAllTransformOptions(SwapUtils.GetProvider(), item);
                     foreach(var transformCharacter in transformCharacters)
                     {
                         var actualItem = Utils.Utils.GetActualChararacterFromLoadeditems(transformCharacter.ID);
                         if (actualItem == null) continue;
                         actualItem.IsTransformCharacter = true;
                         options.Add(actualItem);
                     }
                 }

                 await Dispatcher.InvokeAsync(() =>
                 {
                     foreach(var op in options)
                     {
                         ItemCard5 _card = new ItemCard5(op.name, op.icon, op.rarcolor);

                         _card.MouseLeftButtonDown += delegate
                         {
                             new SwapForm(item, op).Show();
                             this.Close();
                         };

                         this.optionPanl.Children.Add(_card);
                     }

                     label.Content = $"Choose one of the {options.Count} options for {item.name}";

                     Storyboard startup = (Storyboard)FindResource("Startup");
                     startup.Begin(this);

                 });

             }).Start();

        }

        public OptionForm(ApiUEFNSkinObject item)
        {
            InitializeComponent();
            isUefnItem = true;
            this.uefnItem = item;
            
            this.Title = item.Name;
            
            new Thread(async () =>
            {
                var options = Utils.Utils.CachedItems[ItemType.SKIN].Where(x => x.rarcolor != "#a6a2a2").ToList();
                
                Utils.DiscordRPC.UpdatePresence($"Watching {options.Count} Options for " + item.Name);

                options.Insert(0, new()
                {
                    id = "defaultskinuefnoption",
                    name = "Default Skin",
                    description = "Swap with every Default Skin.",
                    icon = "https://fortnite-api.com/images/cosmetics/br/cid_a_275_athena_commando_f_prime_d/icon.png",
                    rarity = Rarities.GetRarityImage("common"),
                    rarcolor = Rarities.GetRarityColor("common"),
                    IsTransformCharacter = false,
                    isDefault = true
                });
                
                _options = options;

                await Dispatcher.InvokeAsync(() =>
                {
                    foreach (var op in options)
                    {
                        if (API.GetApi().BlacklistedOptionIDS.Contains(op.id))
                            continue;
                            
                        ItemCard5 _card = new ItemCard5(op.name, op.icon, op.rarcolor);

                        _card.MouseLeftButtonDown += delegate
                        {
                            new UEFNSkinSwapForm(item, op).Show();
                            this.Close();
                        };

                        this.optionPanl.Children.Add(_card);
                    }

                    label.Content = $"Choose one of the {options.Count} options for {item.Name}";

                    Storyboard startup = (Storyboard)FindResource("Startup");
                    startup.Begin(this);
                });

            }).Start();
        }
        
        public OptionForm(PluginModel Plugin)
        {
            InitializeComponent();
            isUefnSkinPlugin = true;
            this.Plugin = Plugin;
            
            this.Title = Plugin.Name;
            
            new Task(async () =>
            {
                if(!Utils.Utils.CachedItems.ContainsKey(ItemType.SKIN))
                    Utils.Utils.CachedItems.Add(ItemType.SKIN, await Utils.Utils.GetTabItems("outfit"));
                
                var options = Utils.Utils.CachedItems[ItemType.SKIN].Where(x => x.rarcolor != "#a6a2a2").ToList();
                
                Utils.DiscordRPC.UpdatePresence($"Watching {options.Count} Options for " + Plugin.Name);

                options.Insert(0, new()
                {
                    id = "defaultskinuefnoption",
                    name = "Default Skin",
                    description = "Swap with every Default Skin.",
                    icon = "https://fortnite-api.com/images/cosmetics/br/cid_a_275_athena_commando_f_prime_d/icon.png",
                    rarity = Rarities.GetRarityImage("common"),
                    rarcolor = Rarities.GetRarityColor("common"),
                    IsTransformCharacter = false,
                    isDefault = true
                });
                
                _options = options;

                await Dispatcher.InvokeAsync(() =>
                {
                    foreach (var op in options)
                    {
                        if (API.GetApi().BlacklistedOptionIDS.Contains(op.id))
                            continue;
                        
                        ItemCard5 _card = new ItemCard5(op.name, op.icon, op.rarcolor);

                        _card.MouseLeftButtonDown += delegate
                        {
                            new UEFNSkinSwapForm(Plugin, op).Show();
                            this.Close();
                        };

                        this.optionPanl.Children.Add(_card);
                    }

                    label.Content = $"Choose one of the {options.Count} options for {Plugin.Name}";

                    Storyboard startup = (Storyboard)FindResource("Startup");
                    startup.Begin(this);
                    loaded = true;
                });
            }).Start();
        }

        private void SearchBwar_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string thingToSearch = searchBwar.Text;

                if (string.IsNullOrEmpty(thingToSearch))
                {
                    searchScrollViewer.Visibility = Visibility.Hidden;
                    scrollViewer.Visibility = Visibility.Visible;
                    return;
                }
                
                searchOptionPanl.Children.Clear();

                foreach (var option in _options)
                {
                    if (option.name.ToLower().Contains(thingToSearch.ToLower()))
                    {
                        if (API.GetApi().BlacklistedOptionIDS.Contains(option.id))
                            continue;
                        
                        ItemCard5 card = new ItemCard5(option.name, option.icon, option.name);
                        
                        card.MouseDown += async delegate
                        {
                            if(isUefnSkinPlugin)
                                new UEFNSkinSwapForm(Plugin, option).Show();
                            else if(isUefnItem)
                                new UEFNSkinSwapForm(uefnItem, option).Show();
                            else
                                new SwapForm(item, option).Show();
                            
                            this.Close();
                        };

                        searchOptionPanl.Children.Add(card);
                    }
                }
                
                scrollViewer.Visibility = Visibility.Hidden;
                searchScrollViewer.Visibility = Visibility.Visible;
            }
        }

        private void SearchBwar_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (searchBwar.Text.Length > 0)
            {
                textBlock.Visibility = Visibility.Hidden;
            }
            else
            {
                textBlock.Visibility = Visibility.Visible;
            }
        }

        private void OptionForm_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.Close();
        }
    }
}
