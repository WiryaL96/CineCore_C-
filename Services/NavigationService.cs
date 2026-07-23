using CineCore.Models;
using CineCore.Views;
using System.Windows.Controls;

namespace CineCore.Services
{
    public class NavigationService
    {
        private static NavigationService? _instance;
        public static NavigationService Instance => _instance ??= new NavigationService();

        private ContentControl? _host;

        public void Initialize(ContentControl host)
        {
            _host = host;
        }

        public void NavigateTo(AppPage page, object? parameter = null)
        {
            if (_host == null) return;
            _host.Content = page switch
            {
                AppPage.Login               => new LoginView(),
                AppPage.Register            => new RegisterView(),
                AppPage.Dashboard           => new DashboardView(),
                AppPage.MovieDetail         => new MovieDetailView((Movie)parameter!),
                AppPage.SeatSelection       => new SeatSelectionView((SeatSelectionParams)parameter!),
                AppPage.Payment             => new PaymentView((PaymentParams)parameter!),
                AppPage.BookingConfirmation  => new BookingConfirmationView((BookingConfirmationParams)parameter!),
                AppPage.BookingHistory       => new BookingHistoryView(),
                AppPage.AdminPanel          => new AdminPanelView(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    public enum AppPage { Login, Register, Dashboard, MovieDetail, SeatSelection, Payment, BookingConfirmation, BookingHistory, AdminPanel }

    public record SeatSelectionParams(Movie Movie, Showtime Showtime, Cinema Cinema);
    public record PaymentParams(Movie Movie, Showtime Showtime, Cinema Cinema,
        List<string> SelectedSeats, decimal TotalPrice);
    public record BookingConfirmationParams(Movie Movie, Showtime Showtime, Cinema Cinema,
        List<string> SelectedSeats, decimal TotalPrice, string PaymentMethod, string BookingCode);
}
