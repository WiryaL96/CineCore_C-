using CineCore.Models;
using CineCore.Services;
using CineCore.ViewModels.Base;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace CineCore.ViewModels
{
    public class PaymentViewModel : ViewModelBase
    {
        private readonly IDatabaseService _db = ServiceLocator.Get<IDatabaseService>();
        private readonly PaymentParams _params;

        // ── Data Summary ──────────────────────────────────────────────────────
        public Movie Movie => _params.Movie;
        public Showtime Showtime => _params.Showtime;
        public Cinema Cinema => _params.Cinema;
        public decimal TotalPrice => _params.TotalPrice;
        public string SelectedSeatsText => string.Join(", ", _params.SelectedSeats);

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

        // ── Payment Method ────────────────────────────────────────────────────
        private string _selectedPaymentMethod = "GoPay";
        public string SelectedPaymentMethod
        {
            get => _selectedPaymentMethod;
            set => SetProperty(ref _selectedPaymentMethod, value);
        }

        // ── Commands ──────────────────────────────────────────────────────────
        public AsyncRelayCommand ProcessPaymentCommand { get; }
        public RelayCommand GoBackCommand { get; }

        public PaymentViewModel(PaymentParams p)
        {
            _params = p;

            ProcessPaymentCommand = new AsyncRelayCommand(ExecutePaymentAsync, () => !IsBusy);

            GoBackCommand = new RelayCommand(() =>
                NavigationService.Instance.NavigateTo(AppPage.SeatSelection,
                new SeatSelectionParams(Movie, Showtime, Cinema)));
        }

        private async Task ExecutePaymentAsync()
        {
            IsBusy = true;
            try
            {
                int userId = SessionService.CurrentUser?.Id ?? 0;

                var (success, bookingCode) = await Task.Run(() => _db.CreateBookingAsync(
                    userId,
                    Showtime.Id,
                    _params.SelectedSeats,
                    TotalPrice,
                    SelectedPaymentMethod
                ));

                if (success)
                {
                    // Navigate ke halaman konfirmasi
                    NavigationService.Instance.NavigateTo(AppPage.BookingConfirmation,
                        new BookingConfirmationParams(Movie, Showtime, Cinema,
                            _params.SelectedSeats, TotalPrice, SelectedPaymentMethod, bookingCode));
                }
                else
                {
                    MessageBox.Show("Yah, kursi sudah dibooking orang lain. Coba pilih kursi lain.", "Gagal");
                    NavigationService.Instance.NavigateTo(AppPage.SeatSelection,
                        new SeatSelectionParams(Movie, Showtime, Cinema));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error Pembayaran: {ex.Message}", "Database Error");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}