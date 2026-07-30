using CineCore.Models;
using CineCore.Services;
using CineCore.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CineCore.ViewModels
{
    public class BookingConfirmationViewModel : ViewModelBase
    {
        private static readonly CultureInfo IdCulture = new("id-ID");
        private readonly BookingConfirmationParams _params;

        public Movie Movie => _params.Movie;
        public Showtime Showtime => _params.Showtime;
        public Cinema Cinema => _params.Cinema;
        public decimal TotalPrice => _params.TotalPrice;
        public string PaymentMethod => _params.PaymentMethod;
        public string BookingCode => _params.BookingCode;

        // ── Data turunan buat tampilan E-Ticket ──
        public string Format => "2D"; // Model Movie belum punya kolom format -> default "2D"
        public string CinemaName => _params.Cinema.Name;
        public string CinemaType => _params.Cinema.Type;
        public bool HasCinemaType => !string.IsNullOrWhiteSpace(_params.Cinema.Type);

        // "Wednesday, 30 Jul 2026"
        public string DateText =>
            (_params.Showtime.ShowDate.Date + _params.Showtime.StartTime)
                .ToString("dddd, dd MMM yyyy", CultureInfo.InvariantCulture);

        // "14:00 WIB"
        public string TimeText => _params.Showtime.TimeFormatted + " WIB";

        public IReadOnlyList<string> Seats => _params.SelectedSeats;
        public string SeatsText => string.Join(", ", _params.SelectedSeats);
        public string TotalFormatted => "Rp " + _params.TotalPrice.ToString("#,##0", IdCulture);

        // QR code di-generate lewat API online (di-download async oleh PosterUrl converter yang sudah ada).
        public string QrImageUrl =>
            $"https://api.qrserver.com/v1/create-qr-code/?size=160x160&data={Uri.EscapeDataString(BookingCode)}";

        public RelayCommand GoToMyTicketsCommand { get; }
        public RelayCommand GoToHomeCommand { get; }

        public BookingConfirmationViewModel(BookingConfirmationParams p)
        {
            _params = p;
            GoToMyTicketsCommand = new RelayCommand(() =>
                NavigationService.Instance.NavigateTo(AppPage.BookingHistory));
            GoToHomeCommand = new RelayCommand(() =>
                NavigationService.Instance.NavigateTo(AppPage.Dashboard));
        }
    }
}
