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
            NavigationService.Instance.Initialize(PageHost);
            NavigationService.Instance.NavigateTo(AppPage.Login);

            // Pas maximize: kasih margin biar konten nggak ketutup border resize + ilangin sudut membulat
            StateChanged += (_, _) => UpdateWindowStateVisual();
            Loaded += (_, _) => UpdateWindowStateVisual();
        }

        // Klik kiri: single = drag, double = maximize/restore. Klik kanan = system menu.
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState != MouseButtonState.Pressed) return;

            if (e.ClickCount == 2)
            {
                ToggleMaximize();
                e.Handled = true;
                return;
            }

            if (WindowState == WindowState.Normal)
            {
                try { DragMove(); } catch { /* abaikan: mis. klik saat animasi */ }
                e.Handled = true;
            }
            else
            {
                // Drag saat maximized -> restore dulu lalu lanjut drag (kayak Chrome/VS Code)
                try
                {
                    var cursorOnScreen = PointToScreen(e.GetPosition(this));
                    double ratioX = e.GetPosition(this).X / ActualWidth;

                    WindowState = WindowState.Normal;

                    // Posisikan window supaya kursor tetap "nempel" di title bar secara proporsional
                    Left = cursorOnScreen.X - (ActualWidth * ratioX);
                    Top = 0;
                    UpdateLayout();
                    DragMove();
                    e.Handled = true;
                }
                catch { ToggleMaximize(); }
            }
        }

        private void TitleBar_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Menu sistem Windows (Restore/Move/Size/Minimize/Maximize/Close) via Alt+Space / klik kanan
            var pos = PointToScreen(e.GetPosition(this));
            SystemCommands.ShowSystemMenu(this, pos);
            e.Handled = true;
        }

        private void ToggleMaximize()
            => WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal : WindowState.Maximized;

        private void UpdateWindowStateVisual()
        {
            bool max = WindowState == WindowState.Maximized;
            RootBorder.Margin = max ? new Thickness(7) : new Thickness(0);
            RootBorder.CornerRadius = new CornerRadius(max ? 0 : 12);
            IconMaximize.Visibility = max ? Visibility.Collapsed : Visibility.Visible;
            IconRestore.Visibility = max ? Visibility.Visible : Visibility.Collapsed;
            MaxRestoreBtn.ToolTip = max ? "Restore Down" : "Maximize";
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
            => SystemCommands.MinimizeWindow(this);

        private void Maximize_Click(object sender, RoutedEventArgs e)
            => ToggleMaximize();

        private void Close_Click(object sender, RoutedEventArgs e)
            => SystemCommands.CloseWindow(this);
    }
}
