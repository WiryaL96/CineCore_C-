using CineCore.Models;
using CineCore.Services;
using CineCore.ViewModels.Base;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace CineCore.ViewModels
{
    public class AdminPanelViewModel : ViewModelBase
    {
        private static readonly CultureInfo IdCulture = new("id-ID");
        private static string Rp(decimal v) => "Rp " + v.ToString("#,##0", IdCulture);

        private readonly IDatabaseService _db = ServiceLocator.Get<IDatabaseService>();
        private readonly DispatcherTimer _flashTimer;

        public AdminPanelViewModel()
        {
            _flashTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _flashTimer.Tick += (_, _) => { _flashTimer.Stop(); StatusMessage = string.Empty; };

            ShowMoviesCommand = new RelayCommand(() => ActiveSection = "movies");
            ShowReportCommand = new RelayCommand(() => { ActiveSection = "report"; _ = LoadReportAsync(); });
            GoToDashboardCommand = new RelayCommand(() => NavigationService.Instance.NavigateTo(AppPage.Dashboard));
            LogoutCommand = new RelayCommand(() => { SessionService.Logout(); NavigationService.Instance.NavigateTo(AppPage.Login); });

            AddNewMovieCommand = new RelayCommand(OpenCreateForm);
            EditMovieCommand = new RelayCommand(p => OpenEditForm(p as Movie), p => p is Movie);
            DeleteMovieCommand = new RelayCommand(async p => await DeleteSelectedMovieAsync(p), p => p is Movie);
            SaveMovieCommand = new AsyncRelayCommand(SaveMovieAsync);
            CancelEditCommand = new RelayCommand(() => IsEditingMovie = false);
            ChoosePosterCommand = new RelayCommand(ChoosePoster);
            RefreshReportCommand = new AsyncRelayCommand(LoadReportAsync);
            ApplyFilterCommand = new AsyncRelayCommand(ApplyFilterAsync);
            ResetFilterCommand = new AsyncRelayCommand(ResetFilterAsync);
            InitPreviewCommands();

            _ = LoadCatalogAsync();
        }

        // ════════════════════ SIDEBAR / SHELL ════════════════════
        private string _activeSection = "movies";
        public string ActiveSection
        {
            get => _activeSection;
            set { if (SetProperty(ref _activeSection, value)) { OnPropertyChanged(nameof(IsMoviesSection)); OnPropertyChanged(nameof(IsReportSection)); } }
        }
        public bool IsMoviesSection => _activeSection == "movies";
        public bool IsReportSection => _activeSection == "report";

        public string CurrentUserName => SessionService.CurrentUser?.FullName ?? "Admin";
        public string CurrentUserEmail => SessionService.CurrentUser?.Email ?? "";
        public string CurrentUserInitial =>
            string.IsNullOrWhiteSpace(CurrentUserName) ? "A" : CurrentUserName.Trim()[..1].ToUpperInvariant();

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set { if (SetProperty(ref _statusMessage, value)) OnPropertyChanged(nameof(HasStatus)); }
        }
        public bool HasStatus => !string.IsNullOrEmpty(_statusMessage);

        private void Flash(string msg)
        {
            StatusMessage = msg;
            _flashTimer.Stop();
            _flashTimer.Start(); // auto-hide setelah 5 detik
        }

        public RelayCommand ShowMoviesCommand { get; }
        public RelayCommand ShowReportCommand { get; }
        public RelayCommand GoToDashboardCommand { get; }
        public RelayCommand LogoutCommand { get; }

        // ════════════════════ MOVIE CATALOG ════════════════════
        private readonly List<Movie> _allMovies = new();
        public ObservableCollection<Movie> Movies { get; } = new();          // hasil filter search
        public ObservableCollection<CinemaCheck> GeneratorCinemas { get; } = new();

        public string[] FormatOptions { get; } = { "2D", "IMAX", "3D" };

        private string _searchText = string.Empty;
        public string SearchText { get => _searchText; set { if (SetProperty(ref _searchText, value)) ApplyMovieFilter(); } }

        public int TotalMovies { get; private set; }
        public int NowShowingCount { get; private set; }
        public int ComingSoonCount { get; private set; }

        public bool MoviesEmpty => Movies.Count == 0;

        // Form state
        private bool _isEditingMovie;
        public bool IsEditingMovie
        {
            get => _isEditingMovie;
            set { if (SetProperty(ref _isEditingMovie, value)) OnPropertyChanged(nameof(IsListView)); }
        }
        public bool IsListView => !_isEditingMovie;

        private bool _isEditMode;
        public bool IsEditMode { get => _isEditMode; set { if (SetProperty(ref _isEditMode, value)) { OnPropertyChanged(nameof(FormTitle)); OnPropertyChanged(nameof(FormSubtitle)); OnPropertyChanged(nameof(SaveButtonText)); } } }
        public string FormTitle => _isEditMode ? "Edit Movie" : "Add New Movie";
        public string FormSubtitle => _isEditMode ? "Update movie details and screenings" : "Add a new movie to your catalog";
        public string SaveButtonText => _isEditMode ? "Update Movie" : "Create Movie";

        private int _editingId;

        // Form fields
        private string _editTitle = ""; public string EditTitle { get => _editTitle; set => SetProperty(ref _editTitle, value); }
        private string _editDescription = ""; public string EditDescription { get => _editDescription; set => SetProperty(ref _editDescription, value); }
        private string _editGenre = ""; public string EditGenre { get => _editGenre; set => SetProperty(ref _editGenre, value); }
        private int _editDuration; public int EditDuration { get => _editDuration; set => SetProperty(ref _editDuration, value); }
        private string _editFormat = "2D"; public string EditFormat { get => _editFormat; set => SetProperty(ref _editFormat, value); }
        private string _editTrailerUrl = ""; public string EditTrailerUrl { get => _editTrailerUrl; set => SetProperty(ref _editTrailerUrl, value); }
        private string _editPosterUrl = ""; public string EditPosterUrl { get => _editPosterUrl; set { if (SetProperty(ref _editPosterUrl, value)) OnPropertyChanged(nameof(HasPoster)); } }
        public bool HasPoster => !string.IsNullOrWhiteSpace(_editPosterUrl);
        private DateTime _editReleaseDate = DateTime.Today; public DateTime EditReleaseDate { get => _editReleaseDate; set => SetProperty(ref _editReleaseDate, value); }
        private DateTime _editEndDate = DateTime.Today.AddDays(7); public DateTime EditEndDate { get => _editEndDate; set => SetProperty(ref _editEndDate, value); }
        private int _editStatusIndex; public int EditStatusIndex { get => _editStatusIndex; set => SetProperty(ref _editStatusIndex, value); } // 0=Now Showing, 1=Coming Soon

        // Showtime generator
        private string _generatorHours = "10:00, 13:15, 16:30, 19:45";
        public string GeneratorHours { get => _generatorHours; set => SetProperty(ref _generatorHours, value); }
        private decimal _generatorPrice = 45000;
        public decimal GeneratorPrice { get => _generatorPrice; set => SetProperty(ref _generatorPrice, value); }

        public ObservableCollection<string> ValidationErrors { get; } = new();
        public bool HasValidationErrors => ValidationErrors.Count > 0;

        public RelayCommand AddNewMovieCommand { get; }
        public RelayCommand EditMovieCommand { get; }
        public RelayCommand DeleteMovieCommand { get; }
        public AsyncRelayCommand SaveMovieCommand { get; }
        public RelayCommand CancelEditCommand { get; }
        public RelayCommand ChoosePosterCommand { get; }

        private async Task LoadCatalogAsync()
        {
            IsBusy = true;
            try
            {
                var movies = await Task.Run(() => _db.GetMoviesAsync());
                var cinemas = await Task.Run(() => _db.GetCinemasAsync());

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _allMovies.Clear();
                    _allMovies.AddRange(movies);

                    GeneratorCinemas.Clear();
                    foreach (var c in cinemas) GeneratorCinemas.Add(new CinemaCheck(c));

                    RecomputeStats();
                    ApplyMovieFilter();
                });
            }
            catch (Exception ex) { Flash($"Error: {ex.Message}"); }
            finally { IsBusy = false; }
        }

        private void RecomputeStats()
        {
            var now = DateTime.Now;
            TotalMovies = _allMovies.Count;
            NowShowingCount = _allMovies.Count(m => m.IsShowing);
            ComingSoonCount = _allMovies.Count(m => !m.IsShowing && m.ReleaseDate > now);
            OnPropertyChanged(nameof(TotalMovies));
            OnPropertyChanged(nameof(NowShowingCount));
            OnPropertyChanged(nameof(ComingSoonCount));
        }

        private void ApplyMovieFilter()
        {
            var q = _searchText?.Trim() ?? "";
            IEnumerable<Movie> result = _allMovies;
            if (q.Length > 0)
                result = _allMovies.Where(m =>
                    (m.Title?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (m.Genre?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));

            Movies.Clear();
            foreach (var m in result) Movies.Add(m);
            OnPropertyChanged(nameof(MoviesEmpty));
        }

        private void OpenCreateForm()
        {
            _editMode(false, 0);
            EditTitle = ""; EditDescription = ""; EditGenre = ""; EditDuration = 0;
            EditFormat = "2D"; EditTrailerUrl = ""; EditPosterUrl = "";
            EditReleaseDate = DateTime.Today; EditEndDate = DateTime.Today.AddDays(7);
            EditStatusIndex = 0;
            GeneratorHours = "10:00, 13:15, 16:30, 19:45"; GeneratorPrice = 45000;
            foreach (var c in GeneratorCinemas) c.IsSelected = false;
            ValidationErrors.Clear(); OnPropertyChanged(nameof(HasValidationErrors));
            IsEditingMovie = true;
        }

        private void OpenEditForm(Movie? movie)
        {
            if (movie == null) return;
            _editMode(true, movie.Id);
            EditTitle = movie.Title; EditDescription = movie.Description; EditGenre = movie.Genre;
            EditDuration = movie.DurationMinutes; EditFormat = string.IsNullOrWhiteSpace(movie.Format) ? "2D" : movie.Format;
            EditTrailerUrl = movie.TrailerUrl; EditPosterUrl = movie.PosterUrl;
            EditReleaseDate = movie.ReleaseDate == default ? DateTime.Today : movie.ReleaseDate;
            EditEndDate = movie.EndDate ?? EditReleaseDate.AddDays(7);
            EditStatusIndex = movie.IsShowing ? 0 : 1;
            GeneratorHours = "10:00, 13:15, 16:30, 19:45"; GeneratorPrice = 45000;
            foreach (var c in GeneratorCinemas) c.IsSelected = false;
            ValidationErrors.Clear(); OnPropertyChanged(nameof(HasValidationErrors));
            IsEditingMovie = true;
        }

        private void _editMode(bool edit, int id) { IsEditMode = edit; _editingId = id; }

        private void ChoosePoster()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Choose Poster",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.webp;*.bmp|All Files|*.*"
            };
            if (dlg.ShowDialog() == true)
                EditPosterUrl = dlg.FileName;
        }

        private bool Validate()
        {
            ValidationErrors.Clear();
            if (string.IsNullOrWhiteSpace(EditTitle)) ValidationErrors.Add("Title is required.");
            if (string.IsNullOrWhiteSpace(EditGenre)) ValidationErrors.Add("Genre is required.");
            if (EditDuration <= 0) ValidationErrors.Add("Duration must be greater than 0.");
            OnPropertyChanged(nameof(HasValidationErrors));
            return ValidationErrors.Count == 0;
        }

        private Movie BuildMovieFromForm(int id) => new()
        {
            Id = id,
            Title = EditTitle.Trim(),
            Description = EditDescription?.Trim() ?? "",
            Genre = EditGenre.Trim(),
            DurationMinutes = EditDuration,
            Format = string.IsNullOrWhiteSpace(EditFormat) ? "2D" : EditFormat,
            TrailerUrl = EditTrailerUrl?.Trim() ?? "",
            PosterUrl = EditPosterUrl?.Trim() ?? "",
            ReleaseDate = EditReleaseDate,
            EndDate = EditEndDate,
            IsShowing = EditStatusIndex == 0,
        };

        private async Task SaveMovieAsync()
        {
            if (!Validate()) return;
            IsBusy = true;
            try
            {
                int movieId;
                if (_isEditMode)
                {
                    await _db.UpdateMovieAsync(BuildMovieFromForm(_editingId));
                    movieId = _editingId;
                }
                else
                {
                    movieId = await _db.AddMovieAsync(BuildMovieFromForm(0));
                }

                int generated = await GenerateShowtimesAsync(movieId);

                await LoadCatalogAsync();
                IsEditingMovie = false;
                Flash(_isEditMode
                    ? $"Movie updated." + (generated > 0 ? $" {generated} showtimes generated." : "")
                    : $"Movie created." + (generated > 0 ? $" {generated} showtimes generated." : ""));
            }
            catch (Exception ex) { Flash($"Error: {ex.Message}"); }
            finally { IsBusy = false; }
        }

        // Generate showtimes: setiap hari (release → end) × setiap cinema terpilih × setiap jam. Skip yang sudah lewat.
        private async Task<int> GenerateShowtimesAsync(int movieId)
        {
            var cinemaIds = GeneratorCinemas.Where(c => c.IsSelected).Select(c => c.Cinema.Id).ToList();
            var hours = ParseHours(GeneratorHours);
            if (movieId <= 0 || cinemaIds.Count == 0 || hours.Count == 0) return 0;

            var start = EditReleaseDate.Date;
            var end = (EditEndDate.Date >= start) ? EditEndDate.Date : start.AddDays(7);
            var now = DateTime.Now;

            var items = new List<(int, int, DateTime, decimal)>();
            for (var day = start; day <= end; day = day.AddDays(1))
                foreach (var cinemaId in cinemaIds)
                    foreach (var hour in hours)
                    {
                        var startTime = day + hour;
                        if (startTime < now) continue; // skip past
                        items.Add((movieId, cinemaId, startTime, GeneratorPrice));
                    }

            if (items.Count == 0) return 0;
            return await _db.AddShowtimesBulkAsync(items);
        }

        private static List<TimeSpan> ParseHours(string raw)
        {
            var list = new List<TimeSpan>();
            if (string.IsNullOrWhiteSpace(raw)) return list;
            foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                if (TimeSpan.TryParse(part, out var ts)) list.Add(ts);
            return list;
        }

        private async Task DeleteSelectedMovieAsync(object? param)
        {
            if (param is not Movie movie) return;
            if (MessageBox.Show($"Are you sure you want to delete '{movie.Title}'?", "Delete Movie",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            IsBusy = true;
            try
            {
                await _db.DeleteMovieAsync(movie.Id);
                await LoadCatalogAsync();
                Flash($"Movie '{movie.Title}' deleted.");
            }
            catch (Exception ex) { Flash($"Error: {ex.Message}"); }
            finally { IsBusy = false; }
        }

        // ════════════════════ SALES & REPORT ════════════════════
        private SalesReport? _report;
        public SalesReport? Report => _report;

        public ObservableCollection<CinemaTypeRow> CinemaTypeRows { get; } = new();
        public ObservableCollection<MovieRow> TopMovieRows { get; } = new();
        public ObservableCollection<TransactionRow> TransactionRows { get; } = new();
        public bool ReportHasTransactions => TransactionRows.Count > 0;
        public bool ReportEmpty => !ReportHasTransactions;

        public AsyncRelayCommand RefreshReportCommand { get; }
        public AsyncRelayCommand ApplyFilterCommand { get; }
        public AsyncRelayCommand ResetFilterCommand { get; }

        // ── Filter: Tanggal (Date Range) ──
        private DateTime? _filterDateFrom;
        public DateTime? FilterDateFrom
        {
            get => _filterDateFrom;
            set
            {
                if (SetProperty(ref _filterDateFrom, value))
                {
                    // Guard: kalau To < From, clear To
                    if (_filterDateTo.HasValue && _filterDateFrom.HasValue && _filterDateTo < _filterDateFrom)
                        FilterDateTo = null;
                    OnPropertyChanged(nameof(FilterDateTo)); // refresh DisplayDateStart
                }
            }
        }
        private DateTime? _filterDateTo;
        public DateTime? FilterDateTo
        {
            get => _filterDateTo;
            set
            {
                if (SetProperty(ref _filterDateTo, value))
                    OnPropertyChanged(nameof(FilterDateFrom)); // refresh DisplayDateEnd
            }
        }

        private string _filterStatusText = string.Empty;
        public string FilterStatusText { get => _filterStatusText; set { if (SetProperty(ref _filterStatusText, value)) OnPropertyChanged(nameof(HasFilterStatus)); } }
        public bool HasFilterStatus => !string.IsNullOrEmpty(_filterStatusText);

        private async Task ApplyFilterAsync()
        {
            IsBusy = true;
            try
            {
                var from = _filterDateFrom;
                var to = _filterDateTo;
                var report = await Task.Run(() => _db.GetSalesReportAsync(from, to));
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ApplyReport(report);
                    FilterStatusText = report.PeriodLabel != "All Time"
                        ? $"Filtered: {report.PeriodLabel}"
                        : string.Empty;
                });
            }
            catch (Exception ex) { Flash($"Error: {ex.Message}"); }
            finally { IsBusy = false; }
        }

        private async Task ResetFilterAsync()
        {
            FilterDateFrom = null; FilterDateTo = null;
            FilterStatusText = string.Empty;
            await LoadReportAsync();
        }

        // ── PDF Preview Overlay ──
        private bool _isPreviewVisible;
        public bool IsPreviewVisible { get => _isPreviewVisible; set => SetProperty(ref _isPreviewVisible, value); }

        public ObservableCollection<System.Windows.Media.Imaging.BitmapImage> PreviewPages { get; } = new();

        private int _previewPageIndex;
        public int PreviewPageIndex
        {
            get => _previewPageIndex;
            set
            {
                if (SetProperty(ref _previewPageIndex, Math.Max(0, Math.Min(value, PreviewPages.Count - 1))))
                {
                    OnPropertyChanged(nameof(PreviewPageText));
                    OnPropertyChanged(nameof(CurrentPreviewPage));
                }
            }
        }
        public string PreviewPageText => PreviewPages.Count > 0 ? $"Page {_previewPageIndex + 1} / {PreviewPages.Count}" : "";
        public System.Windows.Media.Imaging.BitmapImage? CurrentPreviewPage =>
            PreviewPages.Count > 0 && _previewPageIndex < PreviewPages.Count ? PreviewPages[_previewPageIndex] : null;

        public AsyncRelayCommand ShowPreviewCommand { get; private set; } = null!;
        public RelayCommand ClosePreviewCommand { get; private set; } = null!;
        public RelayCommand PrevPageCommand { get; private set; } = null!;
        public RelayCommand NextPageCommand { get; private set; } = null!;

        private void InitPreviewCommands()
        {
            ShowPreviewCommand = new AsyncRelayCommand(ShowPreviewAsync);
            ClosePreviewCommand = new RelayCommand(() => IsPreviewVisible = false);
            PrevPageCommand = new RelayCommand(() => PreviewPageIndex--, () => _previewPageIndex > 0);
            NextPageCommand = new RelayCommand(() => PreviewPageIndex++, () => _previewPageIndex < PreviewPages.Count - 1);
        }

        private async Task ShowPreviewAsync()
        {
            if (_report == null) return;
            IsBusy = true;
            try
            {
                var report = _report;
                var images = await Task.Run(() => ReportPdfService.GeneratePreviewImages(report));
                Application.Current.Dispatcher.Invoke(() =>
                {
                    PreviewPages.Clear();
                    foreach (var png in images)
                    {
                        var bmp = new System.Windows.Media.Imaging.BitmapImage();
                        bmp.BeginInit();
                        bmp.StreamSource = new System.IO.MemoryStream(png);
                        bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        bmp.EndInit();
                        bmp.Freeze();
                        PreviewPages.Add(bmp);
                    }
                    PreviewPageIndex = 0;
                    OnPropertyChanged(nameof(PreviewPageText));
                    IsPreviewVisible = true;
                });
            }
            catch (Exception ex) { Flash($"Preview error: {ex.Message}"); }
            finally { IsBusy = false; }
        }

        private async Task LoadReportAsync()
        {
            IsBusy = true;
            try
            {
                var report = await Task.Run(() => _db.GetSalesReportAsync());
                Application.Current.Dispatcher.Invoke(() => ApplyReport(report));
            }
            catch (Exception ex) { Flash($"Error: {ex.Message}"); }
            finally { IsBusy = false; }
        }

        private void ApplyReport(SalesReport report)
        {
            _report = report;

            var maxType = report.RevenueByCinemaType.Count > 0 ? (double)report.RevenueByCinemaType.Max(x => x.Revenue) : 1;
            if (maxType <= 0) maxType = 1;
            CinemaTypeRows.Clear();
            foreach (var t in report.RevenueByCinemaType)
                CinemaTypeRows.Add(new CinemaTypeRow(t.Type, Rp(t.Revenue), $"{t.Bookings} bookings", (double)t.Revenue, maxType));

            var maxMovie = report.TopMovies.Count > 0 ? (double)report.TopMovies.Max(x => x.Revenue) : 1;
            if (maxMovie <= 0) maxMovie = 1;
            TopMovieRows.Clear();
            foreach (var m in report.TopMovies)
                TopMovieRows.Add(new MovieRow(m.Title, Rp(m.Revenue), (double)m.Revenue, maxMovie));

            TransactionRows.Clear();
            foreach (var tx in report.RecentTransactions)
                TransactionRows.Add(new TransactionRow(tx));

            // Notify semua teks stat + collections
            foreach (var name in new[]
            {
                nameof(Report), nameof(ReportHasTransactions), nameof(ReportEmpty),
                nameof(TotalRevenueText), nameof(TicketsSoldText), nameof(TicketsSoldSub),
                nameof(AvgTicketPriceText), nameof(ActiveMoviesText),
                nameof(RevenueTodayText), nameof(RevenueTodaySub),
                nameof(RevenueMonthText), nameof(RevenueMonthSub),
                nameof(PendingRevenueText), nameof(PendingSub),
                nameof(ConversionText), nameof(ConversionSub),
                nameof(TransactionCountText)
            }) OnPropertyChanged(name);
        }

        private SalesReport R => _report ?? new SalesReport();
        public string TotalRevenueText => Rp(R.TotalRevenue);
        public string TicketsSoldText => $"{R.TotalTickets} Pcs";
        public string TicketsSoldSub => $"From {R.TotalBookings} bookings";
        public string AvgTicketPriceText => Rp(R.AvgTicketPrice);
        public string ActiveMoviesText => $"{R.ActiveMovies} Titles";
        public string RevenueTodayText => Rp(R.RevenueToday);
        public string RevenueTodaySub => DateTime.Now.ToString("dddd, dd MMM yyyy", CultureInfo.InvariantCulture);
        public string RevenueMonthText => Rp(R.RevenueThisMonth);
        public string RevenueMonthSub => DateTime.Now.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        public string PendingRevenueText => Rp(R.PendingRevenue);
        public string PendingSub => $"{R.PendingBookings} pending (uncollected)";
        public string ConversionText => $"{R.ConversionRate}%";
        public string ConversionSub => $"{R.TotalBookings} paid · {R.CancelledBookings} cancelled";
        public string TransactionCountText => $"{TransactionRows.Count} total";
    }

    // ── Wrappers untuk binding ──
    public class CinemaCheck : ViewModelBase
    {
        public Cinema Cinema { get; }
        public CinemaCheck(Cinema c) { Cinema = c; }
        public string Display => string.IsNullOrWhiteSpace(Cinema.City) ? Cinema.Name : $"{Cinema.Name} · {Cinema.City}";
        private bool _isSelected;
        public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
    }

    public class CinemaTypeRow
    {
        public CinemaTypeRow(string type, string revenueText, string bookingsText, double value, double max)
        { Type = type; RevenueText = revenueText; BookingsText = bookingsText; Value = value; Max = max; }
        public string Type { get; }
        public string RevenueText { get; }
        public string BookingsText { get; }
        public double Value { get; }
        public double Max { get; }
    }

    public class MovieRow
    {
        public MovieRow(string title, string revenueText, double value, double max)
        { Title = title; RevenueText = revenueText; Value = value; Max = max; }
        public string Title { get; }
        public string RevenueText { get; }
        public double Value { get; }
        public double Max { get; }
    }

    public class TransactionRow
    {
        public TransactionRow(ReportTransaction tx)
        {
            DateText = tx.Date.ToString("dd MMM yyyy, HH:mm", CultureInfo.InvariantCulture);
            Reference = tx.Reference;
            Customer = tx.Customer;
            Movie = tx.Movie;
            Cinema = tx.Cinema;
            AmountText = "Rp " + tx.Amount.ToString("#,##0", new CultureInfo("id-ID"));
            Status = string.IsNullOrEmpty(tx.Status) ? "Paid" : char.ToUpper(tx.Status[0]) + tx.Status[1..];
            SeatsShown = tx.Seats.Take(3).ToList();
            SeatsMore = tx.Seats.Count > 3 ? $"+{tx.Seats.Count - 3}" : "";
        }
        public string DateText { get; }
        public string Reference { get; }
        public string Customer { get; }
        public string Movie { get; }
        public string Cinema { get; }
        public string AmountText { get; }
        public string Status { get; }
        public List<string> SeatsShown { get; }
        public string SeatsMore { get; }
        public bool HasMoreSeats => !string.IsNullOrEmpty(SeatsMore);
    }
}
