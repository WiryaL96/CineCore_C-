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

            // Pas maximize: kasih margin biar konten nggak ketutup border resize + ilangin sudut membulat
            StateChanged += (_, _) =>
            {
                bool max = WindowState == WindowState.Maximized;
                RootBorder.Margin = max ? new Thickness(7) : new Thickness(0);
                RootBorder.CornerRadius = new CornerRadius(max ? 0 : 12);
            };
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
