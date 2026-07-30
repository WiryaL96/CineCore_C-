using CineCore.Models;
using CineCore.Services;
using CineCore.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace CineCore.ViewModels
{
    public class MovieDetailViewModel : ViewModelBase
    {
        private readonly IDatabaseService _db = ServiceLocator.Get<IDatabaseService>();

        // Semua jadwal film ini (semua bioskop & tanggal), plus lookup bioskop by id.
        private List<Showtime> _allShowtimes = new();
        private Dictionary<int, Cinema> _cinemaById = new();

        public Movie Movie { get; }

        // Judul di-uppercase biar sesuai desain (WPF TextBlock ga punya text-transform).
        public string TitleUpper => (Movie.Title ?? string.Empty).ToUpperInvariant();
        public bool HasDescription => !string.IsNullOrWhiteSpace(Movie.Description);
        public bool HasTrailer => !string.IsNullOrWhiteSpace(Movie.TrailerUrl);

        // Tanggal unik dari jadwal (hari ini ke atas), urut menaik.
        public ObservableCollection<DateTime> AvailableDates { get; } = new();
        // Tab filter: "All" + type bioskop unik yang punya jadwal film ini.
        public ObservableCollection<string> CinemaTypes { get; } = new();
        // Kartu bioskop yang tampil untuk tanggal + type terpilih.
        public ObservableCollection<CinemaShowtimeGroup> CinemaGroups { get; } = new();

        private DateTime? _selectedDate;
        public DateTime? SelectedDate
        {
            get => _selectedDate;
            set { if (SetProperty(ref _selectedDate, value)) RebuildGroups(); }
        }

        private string _selectedType = "All";
        public string SelectedType
        {
            get => _selectedType;
            set { if (SetProperty(ref _selectedType, value)) RebuildGroups(); }
        }

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

        private bool _hasShowtimes;
        public bool HasShowtimes
        {
            get => _hasShowtimes;
            set { if (SetProperty(ref _hasShowtimes, value)) OnPropertyChanged(nameof(NoShowtimes)); }
        }
        public bool NoShowtimes => !_hasShowtimes;

        public RelayCommand WatchTrailerCommand { get; }
        public RelayCommand SelectShowtimeCommand { get; }
        public RelayCommand GoBackCommand { get; }

        public MovieDetailViewModel(Movie movie)
        {
            Movie = movie;

            WatchTrailerCommand = new RelayCommand(_ => WatchTrailer(), _ => HasTrailer);
            SelectShowtimeCommand = new RelayCommand(OnSelectShowtime, p => p is Showtime);
            GoBackCommand = new RelayCommand(() => NavigationService.Instance.NavigateTo(AppPage.Dashboard));

            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            IsBusy = true;

            var cinemas = await Task.Run(() => _db.GetCinemasAsync());
            var allShowtimes = await Task.Run(() => _db.GetAllShowtimesAsync());

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _cinemaById = cinemas.GroupBy(c => c.Id).ToDictionary(g => g.Key, g => g.First());
                _allShowtimes = allShowtimes.Where(s => s.MovieId == Movie.Id).ToList();

                // Tanggal unik dari jadwal (hari ini ke atas), urut menaik.
                var today = DateTime.Today;
                AvailableDates.Clear();
                foreach (var d in _allShowtimes
                             .Select(s => s.ShowDate.Date)
                             .Where(d => d >= today)
                             .Distinct()
                             .OrderBy(d => d))
                    AvailableDates.Add(d);

                // Tab type: "All" + type unik dari bioskop yang punya jadwal film ini.
                CinemaTypes.Clear();
                CinemaTypes.Add("All");
                foreach (var type in _allShowtimes
                             .Select(s => _cinemaById.TryGetValue(s.CinemaId, out var c) ? c.Type : null)
                             .Where(t => !string.IsNullOrWhiteSpace(t))
                             .Select(t => t!)
                             .Distinct()
                             .OrderBy(t => t))
                    CinemaTypes.Add(type);

                HasShowtimes = AvailableDates.Count > 0;

                _selectedType = "All";
                OnPropertyChanged(nameof(SelectedType));

                if (AvailableDates.Count > 0)
                    SelectedDate = AvailableDates[0]; // otomatis pilih tanggal pertama -> trigger RebuildGroups
                else
                {
                    _selectedDate = null;
                    OnPropertyChanged(nameof(SelectedDate));
                    RebuildGroups();
                }
            });

            IsBusy = false;
        }

        // Filtering 100% di sisi client (in-memory), tanpa reload halaman:
        // ganti tanggal / type cuma nyusun ulang koleksi kartu bioskop.
        private void RebuildGroups()
        {
            CinemaGroups.Clear();
            if (SelectedDate is not DateTime date) return;

            var groups = _allShowtimes
                .Where(s => s.ShowDate.Date == date.Date)
                .GroupBy(s => s.CinemaId)
                .OrderBy(g => g.First().CinemaName);

            foreach (var g in groups)
            {
                if (!_cinemaById.TryGetValue(g.Key, out var cinema)) continue;

                // Filter type: tampil kalau "All" atau type-nya cocok.
                if (!string.Equals(SelectedType, "All", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(cinema.Type, SelectedType, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Pakai nama polos dari jadwal (tanpa suffix "(type)") buat judul kartu.
                var plainName = g.First().CinemaName;
                CinemaGroups.Add(new CinemaShowtimeGroup(cinema, plainName, g));
            }
        }

        private void OnSelectShowtime(object? param)
        {
            if (param is not Showtime showtime) return;
            if (!_cinemaById.TryGetValue(showtime.CinemaId, out var cinema)) return;

            NavigationService.Instance.NavigateTo(
                AppPage.SeatSelection,
                new SeatSelectionParams(Movie, showtime, cinema));
        }

        private void WatchTrailer()
        {
            if (!string.IsNullOrWhiteSpace(Movie.TrailerUrl))
                Process.Start(new ProcessStartInfo(Movie.TrailerUrl) { UseShellExecute = true });
        }
    }
}
