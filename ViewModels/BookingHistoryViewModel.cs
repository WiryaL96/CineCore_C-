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

        public ObservableCollection<Booking> Bookings { get; } = new();

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

        private bool _isEmpty;
        public bool IsEmpty { get => _isEmpty; set => SetProperty(ref _isEmpty, value); }

        public RelayCommand GoBackCommand { get; }

        public BookingHistoryViewModel()
        {
            GoBackCommand = new RelayCommand(() =>
                NavigationService.Instance.NavigateTo(AppPage.Dashboard));
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
                    Bookings.Clear();
                    foreach (var b in bookings)
                        Bookings.Add(b);
                    IsEmpty = Bookings.Count == 0;
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
    }
}
