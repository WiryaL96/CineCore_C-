using System;
using System.Collections.Generic;

namespace CineCore.Models
{
    // Semua angka & list yang dibutuhin halaman Sales & Report + Export PDF.
    public class SalesReport
    {
        public decimal TotalRevenue { get; set; }
        public int TotalTickets { get; set; }
        public int TotalBookings { get; set; }        // paid bookings
        public int ActiveMovies { get; set; }
        public decimal AvgTicketPrice { get; set; }
        public double AvgSeatsPerBooking { get; set; }

        public decimal RevenueToday { get; set; }
        public decimal RevenueThisMonth { get; set; }

        public int PendingBookings { get; set; }
        public decimal PendingRevenue { get; set; }
        public int CancelledBookings { get; set; }
        public int AllBookings { get; set; }          // semua status
        public double ConversionRate { get; set; }    // paid / all * 100

        public List<CinemaTypeRevenue> RevenueByCinemaType { get; set; } = new();
        public List<MovieRevenue> TopMovies { get; set; } = new();
        public List<ReportTransaction> RecentTransactions { get; set; } = new(); // semua paid, terbaru dulu
    }

    public class CinemaTypeRevenue
    {
        public string Type { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int Bookings { get; set; }
    }

    public class MovieRevenue
    {
        public string Title { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }

    public class ReportTransaction
    {
        public DateTime Date { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Customer { get; set; } = string.Empty;
        public string Movie { get; set; } = string.Empty;
        public string Cinema { get; set; } = string.Empty;
        public List<string> Seats { get; set; } = new();
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;

        public string SeatsText => string.Join(", ", Seats);
    }
}
