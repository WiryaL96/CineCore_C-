using CineCore.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Media;

namespace CineCore.ViewModels
{
    // Wrapper tampilan untuk satu kartu booking di halaman My Tickets.
    // Nyediain teks & warna badge yang udah jadi biar XAML tinggal bind.
    public class TicketItemViewModel
    {
        private static readonly CultureInfo IdCulture = new("id-ID");

        public Booking Booking { get; }

        public TicketItemViewModel(Booking booking)
        {
            Booking = booking;

            var status = (booking.Status ?? string.Empty).Trim().ToLowerInvariant();
            IsPaid = status == "paid";
            IsPending = status == "pending";
            HasAction = IsPaid || IsPending;

            StatusLabel = string.IsNullOrEmpty(status)
                ? "Unknown"
                : IdCulture.TextInfo.ToTitleCase(status);

            // Warna badge status (mengikuti palette Tailwind /10 dan /20).
            if (IsPaid)      { StatusBg = B("#1A22C55E"); StatusFg = B("#4ADE80"); StatusBorder = B("#3322C55E"); }
            else if (IsPending) { StatusBg = B("#1AEAB308"); StatusFg = B("#FACC15"); StatusBorder = B("#33EAB308"); }
            else             { StatusBg = B("#1AEF4444"); StatusFg = B("#F87171"); StatusBorder = B("#33EF4444"); }

            Seats = booking.Seats.Select(s => s.SeatLabel).ToList();

            var st = booking.Showtime;
            if (st != null)
            {
                var when = st.ShowDate.Date + st.StartTime;
                ShowtimeText = when.ToString("dd MMM yyyy, HH:mm", CultureInfo.InvariantCulture);
                CinemaName = st.CinemaName;
            }

            TotalFormatted = "Rp " + booking.TotalAmount.ToString("#,##0", IdCulture);
        }

        public string Title => Booking.Movie?.Title ?? string.Empty;
        public string PosterUrl => Booking.Movie?.PosterUrl ?? string.Empty;
        public string Reference => Booking.BookingCode;
        public string CinemaName { get; } = string.Empty;
        public string ShowtimeText { get; } = string.Empty;
        public IReadOnlyList<string> Seats { get; }
        public string TotalFormatted { get; }

        public string StatusLabel { get; }
        public Brush StatusBg { get; }
        public Brush StatusFg { get; }
        public Brush StatusBorder { get; }

        public bool IsPaid { get; }
        public bool IsPending { get; }
        public bool HasAction { get; }

        private static SolidColorBrush B(string hex)
        {
            var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
            brush.Freeze();
            return brush;
        }
    }
}
