using System;
using System.Windows;
using System.Windows.Input;
using Ruination_v2.Swapper;

namespace Ruination_v2.Views;

public partial class FOVForm : Window
{
    public FOVForm()
    {
        InitializeComponent();
    }

    private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            DragMove();
    }

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        try
        {
            fovLabel.Content = ((int)e.NewValue).ToString();
        } catch(Exception ex)
        {

        }
    }

    private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        int currentFov = (int)slider.Value;
        await FOV.SwapFOV(currentFov);
    }

    private async void ButtonBase1_OnClick(object sender, RoutedEventArgs e)
    {
        await FOV.RevertFov();
    }

    private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        this.Close();
    }
}