using CineCore.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CineCore.Services
{
    public interface IDatabaseService
    {
        Task<User?> LoginAsync(string email, string plainPassword);
        Task<bool> RegisterAsync(string fullName, string email, string passwordHash);
        Task<List<Movie>> GetMoviesAsync();
        Task<List<Cinema>> GetCinemasAsync();
        Task EnsureShowtimesAsync();
        Task<List<Showtime>> GetShowtimesAsync(int movieId, int cinemaId, DateTime date);
        Task<List<string>> GetBookedSeatsAsync(int showtimeId);
        Task<(bool Success, string BookingCode)> CreateBookingAsync(int userId, int showtimeId, List<string> seats, decimal total, string paymentMethod);
        Task<List<Booking>> GetBookingHistoryAsync(int userId);
        Task<int> AddMovieAsync(Movie movie);
        Task<bool> UpdateMovieAsync(Movie movie);
        Task<bool> DeleteMovieAsync(int movieId);
        Task<bool> AddShowtimeAsync(int movieId, int cinemaId, DateTime startTime, decimal price);
        Task<int> AddShowtimesBulkAsync(IEnumerable<(int MovieId, int CinemaId, DateTime StartTime, decimal Price)> items);
        Task<bool> DeleteShowtimeAsync(int showtimeId);
        Task<List<Showtime>> GetAllShowtimesAsync();
        Task<SalesReport> GetSalesReportAsync(DateTime? from = null, DateTime? to = null);
    }
}
