using CineCore.Models;
using CineCore.Services;
using CineCore.ViewModels.Base;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace CineCore.ViewModels
{
    public class MovieDetailViewModel : ViewModelBase
    {
        private readonly IDatabaseService _db = ServiceLocator.Get<IDatabaseService>();

        public Movie Movie { get; }

        // ── GENERATE 7 HARI KE DEPAN ──
        public ObservableCollection<DateTime> AvailableDates { get; } = new();

        private ObservableCollection<Cinema> _cinemas = new();
        public ObservableCollection<Cinema> Cinemas { get => _cinemas; set => SetProperty(ref _cinemas, value); }

        private Cinema? _selectedCinema;
        public Cinema? SelectedCinema
        {
            get => _selectedCinema;
            set { SetProperty(ref _selectedCinema, value); _ = LoadShowtimesAsync(); }
        }

        private ObservableCollection<Showtime> _showtimes = new();
        public ObservableCollection<Showtime> Showtimes { get => _showtimes; set => SetProperty(ref _showtimes, value); }

        private Showtime? _selectedShowtime;
        public Showtime? SelectedShowtime
        {
            get => _selectedShowtime;
            set { SetProperty(ref _selectedShowtime, value); SelectSeatsCommand.RaiseCanExecuteChanged(); }
        }

        private DateTime _selectedDate;
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set { SetProperty(ref _selectedDate, value); _ = LoadShowtimesAsync(); }
        }

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

        public RelayCommand WatchTrailerCommand { get; }
        public RelayCommand SelectSeatsCommand { get; }
        public RelayCommand GoBackCommand { get; }

        public MovieDetailViewModel(Movie movie)
        {
            Movie = movie;

            // Generate tanggal dari Hari ini sampai 6 hari ke depan
            for (int i = 0; i < 7; i++)
            {
                AvailableDates.Add(DateTime.Today.AddDays(i));
            }
            // Pilih hari ini sebagai default
            _selectedDate = AvailableDates[0];

            WatchTrailerCommand = new RelayCommand(WatchTrailer, () => !string.IsNullOrWhiteSpace(Movie.TrailerUrl));

            SelectSeatsCommand = new RelayCommand(
                () => NavigationService.Instance.NavigateTo(AppPage.SeatSelection, new SeatSelectionParams(Movie, SelectedShowtime!, SelectedCinema!)),
                () => SelectedShowtime != null && SelectedCinema != null);

            GoBackCommand = new RelayCommand(() => NavigationService.Instance.NavigateTo(AppPage.Dashboard));

            _ = LoadCinemasAsync();
        }

        private async Task LoadCinemasAsync()
        {
            IsBusy = true;
            var cinemas = await Task.Run(() => _db.GetCinemasAsync());

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Cinemas = new ObservableCollection<Cinema>(cinemas);
                if (cinemas.Count > 0) SelectedCinema = cinemas[0];
            });
            IsBusy = false;
        }

        private async Task LoadShowtimesAsync()
        {
            if (SelectedCinema == null) return;
            IsBusy = true;
            var showtimes = await Task.Run(() => _db.GetShowtimesAsync(Movie.Id, SelectedCinema.Id, SelectedDate));

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Showtimes = new ObservableCollection<Showtime>(showtimes);
                // Gak usah auto-select jadwal pertama, biar user yang milih sendiri
                SelectedShowtime = null;
            });
            IsBusy = false;
        }

        private void WatchTrailer()
        {
            if (!string.IsNullOrWhiteSpace(Movie.TrailerUrl))
                Process.Start(new ProcessStartInfo(Movie.TrailerUrl) { UseShellExecute = true });
        }
    }
}