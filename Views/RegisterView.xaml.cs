using CineCore.ViewModels;
using System.Windows;
using System.Windows.Controls;

// PASTIKAN namespace-nya adalah CineCore.Views
namespace CineCore.Views
{
    // PASTIKAN class-nya bernama RegisterView (huruf kapital harus sama persis)
    public partial class RegisterView : UserControl
    {
        public RegisterView()
        {
            // Jika InitializeComponent masih merah, coba Rebuild Solution dulu
            InitializeComponent();
            this.DataContext = new RegisterViewModel();
        }

        private void PasswordBox_Changed(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is RegisterViewModel vm)
            {
                vm.Password = ((PasswordBox)sender).Password;
            }
        }

        private void ConfirmPasswordBox_Changed(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is RegisterViewModel vm)
            {
                vm.ConfirmPassword = ((PasswordBox)sender).Password;
            }
        }
    }
}