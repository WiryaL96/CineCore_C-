using CineCore.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace CineCore.ViewModels
{
    // Satu kartu bioskop di halaman Movie Detail:
    // nama bioskop + badge type + kota + harga tiket + daftar jam tayang (buat tanggal terpilih).
    public class CinemaShowtimeGroup
    {
        private static readonly CultureInfo IdCulture = new("id-ID");

        public Cinema Cinema { get; }
        public ObservableCollection<Showtime> Showtimes { get; }

        public string CinemaName { get; }
        public string CinemaType { get; }
        public string City { get; }
        public bool HasType => !string.IsNullOrWhiteSpace(CinemaType);
        public bool HasCity => !string.IsNullOrWhiteSpace(City);

        // Format mata uang Indonesia: "Rp 65.000" (titik sebagai pemisah ribuan).
        public string PriceFormatted { get; }

        public CinemaShowtimeGroup(Cinema cinema, string cinemaName, IEnumerable<Showtime> showtimes)
        {
            Cinema = cinema;
            CinemaName = cinemaName;
            CinemaType = cinema.Type;
            City = cinema.City;

            var ordered = showtimes.OrderBy(s => s.StartTime).ToList();
            Showtimes = new ObservableCollection<Showtime>(ordered);

            var price = ordered.Count > 0 ? ordered[0].PricePerSeat : 0m;
            PriceFormatted = "Rp " + price.ToString("#,##0", IdCulture);
        }
    }
}
