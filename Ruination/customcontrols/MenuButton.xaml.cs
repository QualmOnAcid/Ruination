﻿using System;
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

namespace Ruination.customcontrols
{
    /// <summary>
    /// Interaction logic for MenuButton.xaml
    /// </summary>
    public partial class MenuButton : UserControl
    {
        public MenuButton()
        {
            InitializeComponent();
            PreviewMouseLeftButtonUp += (sender, args) => OnClick();
        }


        public PathGeometry Icon
        {
            get { return (PathGeometry)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Icon.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(PathGeometry), typeof(MenuButton));



        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(MenuButton));


        //Since Padding is already a property and we are using same name here
        public new Thickness Padding
        {
            get { return (Thickness)GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Padding.  This enables animation, styling, binding, etc...
        public new static readonly DependencyProperty PaddingProperty =
            DependencyProperty.Register("Padding", typeof(Thickness), typeof(MenuButton));



        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsSelected.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register("IsSelected", typeof(bool), typeof(MenuButton));



        public string GroupName
        {
            get { return (string)GetValue(GroupNameProperty); }
            set { SetValue(GroupNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for GroupName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GroupNameProperty =
            DependencyProperty.Register("GroupName", typeof(string), typeof(MenuButton));

        public static readonly RoutedEvent ClickEvent = EventManager.RegisterRoutedEvent(
"Click", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MenuButton));

        public event RoutedEventHandler Click
        {
            add { AddHandler(ClickEvent, value); }
            remove { RemoveHandler(ClickEvent, value); }
        }

        void RaiseClickEvent()
        {
            RoutedEventArgs newEventArgs = new RoutedEventArgs(MenuButton.ClickEvent);
            RaiseEvent(newEventArgs);
        }

        void OnClick()
        {
            RaiseClickEvent();
        }

    }
}
