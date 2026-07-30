using System.Linq;

namespace CineCore.Models
{
    public class Cinema
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;  // XXI, Premiere, IMAX
        public string Location { get; set; } = string.Empty;
        // ── Dipisah dari Name/Location biar bisa dipakai buat filter type & tampil kota di kartu ──
        public string Type { get; set; } = string.Empty;  // XXI / Premiere / IMAX
        public string City { get; set; } = string.Empty;  // contoh: "Bandung"
        public int TotalRows { get; set; } = 5;
        public int TotalColumns { get; set; } = 8;
    }

    public class Showtime
    {
        public int Id { get; set; }
        public int MovieId { get; set; }
        public int CinemaId { get; set; }
        public DateTime ShowDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public decimal PricePerSeat { get; set; }
        public string TimeFormatted => StartTime.ToString(@"hh\:mm");
        public string CinemaName { get; set; } = string.Empty;
        public string MovieTitle { get; set; } = string.Empty;
    }

    public class Booking
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ShowtimeId { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public DateTime BookedAt { get; set; }
        public string BookingCode { get; set; } = string.Empty;

        // Navigation
        public Movie? Movie { get; set; }
        public Showtime? Showtime { get; set; }
        public List<BookingSeat> Seats { get; set; } = new();

        // Display helper
        public string SeatsDisplay => string.Join(", ", Seats.Select(s => s.SeatLabel));
    }

    public class BookingSeat
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public string SeatLabel { get; set; } = string.Empty; // e.g. "A1", "B3"
    }
}