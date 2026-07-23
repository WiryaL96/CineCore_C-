using CineCore.Models;
using CineCore.Services;
using CineCore.ViewModels.Base;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CineCore.ViewModels
{
    public class AdminPanelViewModel : ViewModelBase
    {
        private readonly IDatabaseService _db = ServiceLocator.Get<IDatabaseService>();

        // ── Tab ───────────────────────────────────────────────────────────────
        private int _selectedTab;
        public int SelectedTab { get => _selectedTab; set => SetProperty(ref _selectedTab, value); }

        // ── Movies ────────────────────────────────────────────────────────────
        public ObservableCollection<Movie> Movies { get; } = new();

        private Movie? _selectedMovie;
        public Movie? SelectedMovie
        {
            get => _selectedMovie;
            set
            {
                SetProperty(ref _selectedMovie, value);
                if (value != null)
                {
                    EditTitle = value.Title;
                    EditGenre = value.Genre;
                    EditDuration = value.DurationMinutes;
                    EditPosterUrl = value.PosterUrl;
                    EditTrailerUrl = value.TrailerUrl;
                    EditIsShowing = value.IsShowing;
                    EditReleaseDate = value.ReleaseDate;
                }
            }
        }

        // Movie form fields
        private string _editTitle = string.Empty;
        public string EditTitle { get => _editTitle; set => SetProperty(ref _editTitle, value); }

        private string _editGenre = string.Empty;
        public string EditGenre { get => _editGenre; set => SetProperty(ref _editGenre, value); }

        private int _editDuration;
        public int EditDuration { get => _editDuration; set => SetProperty(ref _editDuration, value); }

        private string _editPosterUrl = string.Empty;
        public string EditPosterUrl { get => _editPosterUrl; set => SetProperty(ref _editPosterUrl, value); }

        private string _editTrailerUrl = string.Empty;
        public string EditTrailerUrl { get => _editTrailerUrl; set => SetProperty(ref _editTrailerUrl, value); }

        private bool _editIsShowing = true;
        public bool EditIsShowing { get => _editIsShowing; set => SetProperty(ref _editIsShowing, value); }

        private DateTime _editReleaseDate = DateTime.Today;
        public DateTime EditReleaseDate { get => _editReleaseDate; set => SetProperty(ref _editReleaseDate, value); }

        // ── Showtimes ─────────────────────────────────────────────────────────
        public ObservableCollection<Showtime> Showtimes { get; } = new();
        public ObservableCollection<Cinema> Cinemas { get; } = new();

        private Movie? _showtimeMovie;
        public Movie? ShowtimeMovie { get => _showtimeMovie; set => SetProperty(ref _showtimeMovie, value); }

        private Cinema? _showtimeCinema;
        public Cinema? ShowtimeCinema { get => _showtimeCinema; set => SetProperty(ref _showtimeCinema, value); }

        private DateTime _showtimeDate = DateTime.Today;
        public DateTime ShowtimeDate { get => _showtimeDate; set => SetProperty(ref _showtimeDate, value); }

        private string _showtimeTime = "19:00";
        public string ShowtimeTime { get => _showtimeTime; set => SetProperty(ref _showtimeTime, value); }

        private decimal _showtimePrice = 45000;
        public decimal ShowtimePrice { get => _showtimePrice; set => SetProperty(ref _showtimePrice, value); }

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

        private string _statusMessage = string.Empty;
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        // ── Commands ──────────────────────────────────────────────────────────
        public RelayCommand GoBackCommand { get; }
        public AsyncRelayCommand SaveMovieCommand { get; }
        public AsyncRelayCommand AddNewMovieCommand { get; }
        public AsyncRelayCommand DeleteMovieCommand { get; }
        public AsyncRelayCommand AddShowtimeCommand { get; }
        public RelayCommand<Showtime> DeleteShowtimeCommand { get; }
        public RelayCommand ClearMovieFormCommand { get; }

        public AdminPanelViewModel()
        {
            GoBackCommand = new RelayCommand(() =>
                NavigationService.Instance.NavigateTo(AppPage.Dashboard));

            SaveMovieCommand = new AsyncRelayCommand(SaveMovieAsync);
            AddNewMovieCommand = new AsyncRelayCommand(AddNewMovieAsync);
            DeleteMovieCommand = new AsyncRelayCommand(DeleteMovieAsync);
            AddShowtimeCommand = new AsyncRelayCommand(AddShowtimeAsync);
            DeleteShowtimeCommand = new RelayCommand<Showtime>(async st =>
            {
                if (st == null) return;
                if (MessageBox.Show($"Hapus showtime {st.MovieTitle} - {st.ShowDate:dd MMM} {st.TimeFormatted}?",
                    "Konfirmasi", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
                await _db.DeleteShowtimeAsync(st.Id);
                Showtimes.Remove(st);
                StatusMessage = "Showtime dihapus.";
            });

            ClearMovieFormCommand = new RelayCommand(() =>
            {
                SelectedMovie = null;
                EditTitle = ""; EditGenre = ""; EditDuration = 0;
                EditPosterUrl = ""; EditTrailerUrl = "";
                EditIsShowing = true; EditReleaseDate = DateTime.Today;
            });

            _ = LoadAllAsync();
        }

        private async Task LoadAllAsync()
        {
            IsBusy = true;
            try
            {
                var movies = await Task.Run(() => _db.GetMoviesAsync());
                var cinemas = await Task.Run(() => _db.GetCinemasAsync());
                var showtimes = await Task.Run(() => _db.GetAllShowtimesAsync());

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Movies.Clear();
                    foreach (var m in movies) Movies.Add(m);

                    Cinemas.Clear();
                    foreach (var c in cinemas) Cinemas.Add(c);

                    Showtimes.Clear();
                    foreach (var s in showtimes) Showtimes.Add(s);
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        private Movie BuildMovieFromForm(int id = 0) => new()
        {
            Id = id,
            Title = EditTitle.Trim(),
            Genre = EditGenre.Trim(),
            DurationMinutes = EditDuration,
            PosterUrl = EditPosterUrl.Trim(),
            TrailerUrl = EditTrailerUrl.Trim(),
            IsShowing = EditIsShowing,
            ReleaseDate = EditReleaseDate,
        };

        private async Task AddNewMovieAsync()
        {
            if (string.IsNullOrWhiteSpace(EditTitle)) { StatusMessage = "Title wajib diisi."; return; }
            IsBusy = true;
            try
            {
                await _db.AddMovieAsync(BuildMovieFromForm());
                StatusMessage = "Film baru ditambahkan!";
                await LoadAllAsync();
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private async Task SaveMovieAsync()
        {
            if (SelectedMovie == null) { StatusMessage = "Pilih film dulu."; return; }
            if (string.IsNullOrWhiteSpace(EditTitle)) { StatusMessage = "Title wajib diisi."; return; }
            IsBusy = true;
            try
            {
                await _db.UpdateMovieAsync(BuildMovieFromForm(SelectedMovie.Id));
                StatusMessage = "Film di-update!";
                await LoadAllAsync();
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private async Task DeleteMovieAsync()
        {
            if (SelectedMovie == null) { StatusMessage = "Pilih film dulu."; return; }
            if (MessageBox.Show($"Hapus film \"{SelectedMovie.Title}\"?", "Konfirmasi",
                MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            IsBusy = true;
            try
            {
                await _db.DeleteMovieAsync(SelectedMovie.Id);
                StatusMessage = "Film dihapus!";
                ClearMovieFormCommand.Execute(null);
                await LoadAllAsync();
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private async Task AddShowtimeAsync()
        {
            if (ShowtimeMovie == null || ShowtimeCinema == null)
            { StatusMessage = "Pilih film dan cinema dulu."; return; }
            if (!TimeSpan.TryParse(ShowtimeTime, out var time))
            { StatusMessage = "Format waktu salah (HH:mm)."; return; }

            var startTime = ShowtimeDate.Date + time;
            IsBusy = true;
            try
            {
                await _db.AddShowtimeAsync(ShowtimeMovie.Id, ShowtimeCinema.Id, startTime, ShowtimePrice);
                StatusMessage = "Showtime ditambahkan!";
                await LoadAllAsync();
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }
    }
}
