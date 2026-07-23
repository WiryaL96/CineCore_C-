using CineCore.Services;
using CineCore.ViewModels.Base;
using System.Text.RegularExpressions;

namespace CineCore.ViewModels
{
    public class RegisterViewModel : ViewModelBase
    {
        private readonly IDatabaseService _db = ServiceLocator.Get<IDatabaseService>();

        private string _fullName = string.Empty;
        public string FullName { get => _fullName; set { SetProperty(ref _fullName, value); RegisterCommand.RaiseCanExecuteChanged(); } }

        private string _email = string.Empty;
        public string Email { get => _email; set { SetProperty(ref _email, value); RegisterCommand.RaiseCanExecuteChanged(); } }

        private string _password = string.Empty;
        public string Password { get => _password; set { SetProperty(ref _password, value); RegisterCommand.RaiseCanExecuteChanged(); } }

        private string _confirmPassword = string.Empty;
        public string ConfirmPassword { get => _confirmPassword; set { SetProperty(ref _confirmPassword, value); RegisterCommand.RaiseCanExecuteChanged(); } }

        private bool _showPassword;
        public bool ShowPassword { get => _showPassword; set => SetProperty(ref _showPassword, value); }

        private string _errorMessage = string.Empty;
        public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set { SetProperty(ref _isBusy, value); RegisterCommand.RaiseCanExecuteChanged(); } }

        public AsyncRelayCommand RegisterCommand { get; }
        public RelayCommand GoToLoginCommand { get; }
        public RelayCommand TogglePasswordCommand { get; }

        public RegisterViewModel()
        {
            RegisterCommand = new AsyncRelayCommand(ExecuteRegisterAsync, CanRegister);
            GoToLoginCommand = new RelayCommand(() => NavigationService.Instance.NavigateTo(AppPage.Login));
            TogglePasswordCommand = new RelayCommand(() => ShowPassword = !ShowPassword);
        }

        private bool CanRegister() =>
            !IsBusy &&
            !string.IsNullOrWhiteSpace(FullName) &&
            !string.IsNullOrWhiteSpace(Email) &&
            !string.IsNullOrWhiteSpace(Password) &&
            Password == ConfirmPassword;

        private async Task ExecuteRegisterAsync()
        {
            ErrorMessage = string.Empty;
            if (!Regex.IsMatch(Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            { ErrorMessage = "Please enter a valid email address."; return; }
            if (Password.Length < 6)
            { ErrorMessage = "Password must be at least 6 characters."; return; }

            IsBusy = true;
            try
            {
                var hash = AuthService.HashPassword(Password);
                var success = await _db.RegisterAsync(FullName.Trim(), Email.Trim().ToLower(), hash);
                if (!success) { ErrorMessage = "Email already registered."; return; }
                NavigationService.Instance.NavigateTo(AppPage.Login);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error: {ex.Message}";
            }
            finally { IsBusy = false; }
        }
    }
}
