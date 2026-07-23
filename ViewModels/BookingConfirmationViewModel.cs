using CineCore.Models;
using CineCore.Services;
using CineCore.ViewModels.Base;

namespace CineCore.ViewModels
{
    public class BookingConfirmationViewModel : ViewModelBase
    {
        private readonly BookingConfirmationParams _params;

        public Movie Movie => _params.Movie;
        public Showtime Showtime => _params.Showtime;
        public Cinema Cinema => _params.Cinema;
        public decimal TotalPrice => _params.TotalPrice;
        public string PaymentMethod => _params.PaymentMethod;
        public string BookingCode => _params.BookingCode;
        public string SeatsText => string.Join(", ", _params.SelectedSeats);

        public RelayCommand GoToDashboardCommand { get; }

        public BookingConfirmationViewModel(BookingConfirmationParams p)
        {
            _params = p;
            GoToDashboardCommand = new RelayCommand(() =>
                NavigationService.Instance.NavigateTo(AppPage.Dashboard));
        }
    }
}
