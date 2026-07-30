using CineCore.Models;
using CineCore.Services;
using CineCore.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CineCore.ViewModels
{
    public class BookingHistoryViewModel : ViewModelBase
    {
        private readonly IDatabaseService _db = ServiceLocator.Get<IDatabaseService>();

        public ObservableCollection<TicketItemViewModel> Tickets { get; } = new();

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

        private bool _isEmpty;
        public bool IsEmpty
        {
            get => _isEmpty;
            set { if (SetProperty(ref _isEmpty, value)) OnPropertyChanged(nameof(HasTickets)); }
        }
        public bool HasTickets => !_isEmpty;

        public RelayCommand GoBackCommand { get; }
        public RelayCommand BrowseMoviesCommand { get; }
        public RelayCommand OpenBookingCommand { get; }

        public BookingHistoryViewModel()
        {
            GoBackCommand = new RelayCommand(() => NavigationService.Instance.NavigateTo(AppPage.Dashboard));
            BrowseMoviesCommand = new RelayCommand(() => NavigationService.Instance.NavigateTo(AppPage.Dashboard));
            OpenBookingCommand = new RelayCommand(p => OpenBooking(p as TicketItemViewModel), p => p is TicketItemViewModel);

            _ = LoadHistoryAsync();
        }

        private async Task LoadHistoryAsync()
        {
            IsBusy = true;
            try
            {
                int userId = SessionService.CurrentUser?.Id ?? 0;
                var bookings = await Task.Run(() => _db.GetBookingHistoryAsync(userId));

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Tickets.Clear();
                    foreach (var b in bookings)
                        Tickets.Add(new TicketItemViewModel(b));
                    IsEmpty = Tickets.Count == 0;
                });
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Tombol aksi per kartu: "paid" -> lihat e-ticket, "pending" -> lanjut bayar.
        private void OpenBooking(TicketItemViewModel? item)
        {
            if (item?.Booking is not { Movie: { } movie, Showtime: { } showtime } booking) return;

            var cinema = new Cinema { Name = showtime.CinemaName };
            var seats = booking.Seats.Select(s => s.SeatLabel).ToList();

            if (item.IsPaid)
            {
                NavigationService.Instance.NavigateTo(AppPage.BookingConfirmation,
                    new BookingConfirmationParams(movie, showtime, cinema, seats,
                        booking.TotalAmount, booking.PaymentMethod, booking.BookingCode));
            }
            else if (item.IsPending)
            {
                NavigationService.Instance.NavigateTo(AppPage.Payment,
                    new PaymentParams(movie, showtime, cinema, seats, booking.TotalAmount));
            }
        }
    }
}
