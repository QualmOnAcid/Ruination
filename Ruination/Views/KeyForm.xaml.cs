using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Ruination_v2.Utils;

namespace Ruination_v2.Views;

public partial class KeyForm : Window
{
    public KeyForm()
    {
        InitializeComponent();
    }

    private void KeyForm_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        this.DragMove();
    }

    private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        this.Close();
    }

    private async void SearchBwar_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await ValidateKey();
        }
    }

    private async Task ValidateKey()
    {
        string key = searchBwar.Text;

        if (string.IsNullOrEmpty(key))
        {
            MessageBox.Show("Please enter a valid key.", "Ruination", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var keyResponse = await BstlarKey.CheckKey(key);

        if (!keyResponse)
        {
            MessageBox.Show("Please enter a valid key.", "Ruination", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        Config.GetConfig().Key = key;
        Config.Save();
            
        new LoadingForm().Show();
        this.Close();
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

    private void UIElement_OnMouseDown1(object sender, MouseButtonEventArgs e)
    {
        Utils.Utils.StartUrl(API.GetApi().Other.KeyLink);
    }

    private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        await ValidateKey();
    }

    private async void ButtonBase_OnClick1(object sender, RoutedEventArgs e)
    {
        DiscordOAuth2 discordOAuth2 = new DiscordOAuth2("1046483495096160306", "x4wKouvPfrSMvkokox5KJ1yyihmk9u02", "1043851554643513424", new() { "1043851554643513427", "1056543301131567144", "1050477482781966438", "1043851554643513432", "1191784179826970684" });

        new Thread(discordOAuth2.Authenticate).Start();

        while (!discordOAuth2.finished)
            await Task.Delay(500);

        if (!discordOAuth2.HasRole())
        {
            MessageBox.Show("Your account does not have Premium.", "Ruination", MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        DateTime dt = DateTime.Now;
        dt = dt.AddHours(16);
        Config.GetConfig().KeyValidTime = dt;
        Config.Save();
        Utils.Utils.IsPremium = true;
        new LoadingForm().Show();
        this.Close();
    }
}