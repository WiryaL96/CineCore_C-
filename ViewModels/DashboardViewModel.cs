using CineCore.Models;
using CineCore.Services;
using CineCore.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CineCore.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly IDatabaseService _db = ServiceLocator.Get<IDatabaseService>();
        private List<Movie> _allMovies = new();

        // Auto-seed jadwal cukup sekali per sesi aplikasi
        private static bool _showtimesEnsured;

        // Koleksi data untuk di-bind ke UI
        public ObservableCollection<Movie> NowShowingMovies { get; } = new();
        public ObservableCollection<Movie> ComingSoonMovies { get; } = new();

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

        // Search
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { SetProperty(ref _searchText, value); FilterMovies(); }
        }

        // Welcome text dari SessionService
        public string WelcomeText => $"Welcome, {SessionService.CurrentUser?.FullName ?? "User"}";

        // Commands
        public AsyncRelayCommand LoadDataCommand { get; }
        public RelayCommand<Movie> BuyTicketCommand { get; }
        public RelayCommand LogoutCommand { get; }
        public RelayCommand BrowseMoviesCommand { get; }
        public RelayCommand FindCinemasCommand { get; }
        public RelayCommand ViewHistoryCommand { get; }
        public RelayCommand AdminPanelCommand { get; }

        public DashboardViewModel()
        {
            LoadDataCommand = new AsyncRelayCommand(LoadMoviesAsync);

            BuyTicketCommand = new RelayCommand<Movie>(movie =>
            {
                if (movie != null) NavigationService.Instance.NavigateTo(AppPage.MovieDetail, movie);
            });

            LogoutCommand = new RelayCommand(() =>
            {
                SessionService.Logout();
                NavigationService.Instance.NavigateTo(AppPage.Login);
            });

            // Browse Movies scrolls ke Now Showing section (reuse: navigate to self to refresh)
            BrowseMoviesCommand = new RelayCommand(() =>
            {
                // Scroll focus ke daftar film — cukup clear search agar semua tampil
                SearchText = string.Empty;
            });

            FindCinemasCommand = new RelayCommand(() =>
            {
                // Pilih film pertama yang Now Showing agar user bisa lihat cinema di MovieDetail
                var first = NowShowingMovies.FirstOrDefault();
                if (first != null)
                    NavigationService.Instance.NavigateTo(AppPage.MovieDetail, first);
            });

            ViewHistoryCommand = new RelayCommand(() =>
                NavigationService.Instance.NavigateTo(AppPage.BookingHistory));

            AdminPanelCommand = new RelayCommand(() =>
                NavigationService.Instance.NavigateTo(AppPage.AdminPanel));

            // Otomatis load data saat halaman dibuka
            _ = LoadMoviesAsync();
        }

        private void FilterMovies()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                NowShowingMovies.Clear();
                ComingSoonMovies.Clear();

                var query = string.IsNullOrWhiteSpace(SearchText)
                    ? _allMovies
                    : _allMovies.Where(m =>
                        m.Title.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase) ||
                        m.Genre.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase)).ToList();

                foreach (var movie in query)
                {
                    if (movie.IsShowing)
                        NowShowingMovies.Add(movie);
                    else
                        ComingSoonMovies.Add(movie);
                }
            });
        }

        private async Task LoadMoviesAsync()
        {
            // Kalau udah jalan, jangan ditumpuk
            if (IsBusy) return;

            IsBusy = true;
            try
            {
                // Pastikan jadwal 7 hari ke depan tersedia (sekali per sesi)
                if (!_showtimesEnsured)
                {
                    try { await Task.Run(() => _db.EnsureShowtimesAsync()); _showtimesEnsured = true; }
                    catch { /* kalau gagal seed, jangan ganggu load film */ }
                }

                _allMovies = await Task.Run(async () => await _db.GetMoviesAsync());
                FilterMovies();
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"Database Error Bang:\n{ex.Message}", "CRITICAL ERROR");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}