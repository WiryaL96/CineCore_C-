using CineCore.Services;
using CineCore.ViewModels.Base;
using System.Windows;
using System.Windows.Input;

namespace CineCore.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly IDatabaseService _db = ServiceLocator.Get<IDatabaseService>();

        // ── Bound Properties ─────────────────────────────────────────────────
        private string _email = string.Empty;
        public string Email { get => _email; set { SetProperty(ref _email, value); LoginCommand.RaiseCanExecuteChanged(); } }

        private string _password = string.Empty;
        public string Password { get => _password; set { SetProperty(ref _password, value); LoginCommand.RaiseCanExecuteChanged(); } }

        private bool _showPassword;
        public bool ShowPassword { get => _showPassword; set => SetProperty(ref _showPassword, value); }

        private bool _rememberMe;
        public bool RememberMe { get => _rememberMe; set => SetProperty(ref _rememberMe, value); }

        private string _errorMessage = string.Empty;
        public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set { SetProperty(ref _isBusy, value); LoginCommand.RaiseCanExecuteChanged(); } }

        // ── Commands ──────────────────────────────────────────────────────────
        public AsyncRelayCommand LoginCommand { get; }
        public RelayCommand GoToRegisterCommand { get; }
        public RelayCommand TogglePasswordCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new AsyncRelayCommand(
                ExecuteLoginAsync,
                () => !IsBusy && !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password));
            GoToRegisterCommand = new RelayCommand(() =>
                NavigationService.Instance.NavigateTo(AppPage.Register));
            TogglePasswordCommand = new RelayCommand(() => ShowPassword = !ShowPassword);

            // Load remembered email
            var saved = SessionService.LoadRememberedEmail();
            if (saved != null)
            {
                Email = saved;
                RememberMe = true;
            }
        }

        private async Task ExecuteLoginAsync()
        {
            ErrorMessage = string.Empty;
            IsBusy = true;
            try
            {
                var user = await _db.LoginAsync(Email.Trim(), Password);
                if (user == null)
                {
                    ErrorMessage = "Invalid email or password.";
                    return;
                }
                SessionService.CurrentUser = user;

                // Remember Me
                if (RememberMe)
                    SessionService.SaveRememberedEmail(Email.Trim());
                else
                    SessionService.ClearRememberedEmail();

                NavigationService.Instance.NavigateTo(AppPage.Dashboard);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Connection error: {ex.Message}";
            }
            finally { IsBusy = false; }
        }
    }
}
