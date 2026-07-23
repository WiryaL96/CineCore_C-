using CineCore.Services;
using CineCore.Views;
using System.Windows;
using System.Windows.Input;

namespace CineCore
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            NavigationService.Instance.Initialize(this);
            // Set the Content property of MainWindow, using PageHost as the host
            NavigationService.Instance.NavigateTo(AppPage.Login);
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void Maximize_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal : WindowState.Maximized;

        private void Close_Click(object sender, RoutedEventArgs e)
            => Application.Current.Shutdown();
    }
}
