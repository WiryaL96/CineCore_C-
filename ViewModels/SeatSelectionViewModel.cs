using CineCore.Models;
using CineCore.Services;
using CineCore.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Linq;

namespace CineCore.ViewModels
{
    public class SeatSelectionViewModel : ViewModelBase
    {
        private readonly IDatabaseService _db = ServiceLocator.Get<IDatabaseService>();
        private readonly SeatSelectionParams _params;

        // Rows/Columns dinamis dari Cinema
        private readonly string[] _rows;
        private readonly int _columns;

        // ── Properties ────────────────────────────────────────────────────────
        public Movie Movie => _params.Movie;
        public Showtime Showtime => _params.Showtime;
        public Cinema Cinema => _params.Cinema;

        private ObservableCollection<Seat> _seats = new();
        public ObservableCollection<Seat> Seats { get => _seats; set => SetProperty(ref _seats, value); }

        private ObservableCollection<string> _selectedSeatLabels = new();
        public ObservableCollection<string> SelectedSeatLabels
        {
            get => _selectedSeatLabels;
            set => SetProperty(ref _selectedSeatLabels, value);
        }

        public string SelectedSeatsText => SelectedSeatLabels.Count > 0
            ? string.Join(", ", SelectedSeatLabels)
            : "No seat selected";

        public decimal TotalPrice => SelectedSeatLabels.Count * Showtime.PricePerSeat;
        public string TotalPriceText => $"Rp {TotalPrice:N0}";

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

        // ── Commands ──────────────────────────────────────────────────────────
        public RelayCommand<Seat> ToggleSeatCommand { get; }
        public RelayCommand ContinueCommand { get; }
        public RelayCommand GoBackCommand { get; }

        public SeatSelectionViewModel(SeatSelectionParams p)
        {
            _params = p;

            // Generate row labels dari TotalRows cinema
            int rowCount = Cinema.TotalRows > 0 ? Cinema.TotalRows : 5;
            _rows = Enumerable.Range(0, rowCount).Select(i => ((char)('A' + i)).ToString()).ToArray();
            _columns = Cinema.TotalColumns > 0 ? Cinema.TotalColumns : 8;
            ToggleSeatCommand = new RelayCommand<Seat>(ToggleSeat,
                seat => seat != null && seat.State != SeatState.Booked);
            ContinueCommand = new RelayCommand(ContinueToPayment,
                () => SelectedSeatLabels.Count > 0);
            GoBackCommand = new RelayCommand(() =>
                NavigationService.Instance.NavigateTo(AppPage.MovieDetail, Movie));
            _ = LoadSeatsAsync();
        }

        private async Task LoadSeatsAsync()
        {
            IsBusy = true;
            var bookedLabels = await _db.GetBookedSeatsAsync(Showtime.Id);
            var seatList = new List<Seat>();
            foreach (var row in _rows)
                for (int col = 1; col <= _columns; col++)
                {
                    var label = $"{row}{col}";
                    seatList.Add(new Seat
                    {
                        //Label = label,
                        Row = row,
                        Column = col,
                        State = bookedLabels.Contains(label) ? SeatState.Booked : SeatState.Available
                    });
                }
            Seats = new ObservableCollection<Seat>(seatList);
            IsBusy = false;
        }

        private void ToggleSeat(Seat? seat)
        {
            if (seat == null || seat.State == SeatState.Booked) return;
            var target = Seats.First(s => s.Label == seat.Label);
            if (target.State == SeatState.Available)
            {
                target.State = SeatState.Selected;
                SelectedSeatLabels.Add(target.Label);
            }
            else
            {
                target.State = SeatState.Available;
                SelectedSeatLabels.Remove(target.Label);
            }
            // Refresh the seat in collection (trigger UI)
            var idx = Seats.IndexOf(target);
            Seats[idx] = target;

            OnPropertyChanged(nameof(SelectedSeatsText));
            OnPropertyChanged(nameof(TotalPrice));
            OnPropertyChanged(nameof(TotalPriceText));
            ContinueCommand.RaiseCanExecuteChanged();
        }

        private void ContinueToPayment()
        {
            NavigationService.Instance.NavigateTo(AppPage.Payment, new PaymentParams(
                Movie, Showtime, Cinema,
                SelectedSeatLabels.ToList(), TotalPrice));
        }
    }
}
