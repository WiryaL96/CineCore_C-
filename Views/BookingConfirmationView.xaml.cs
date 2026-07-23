using CineCore.Services;
using CineCore.ViewModels;
using System.Windows.Controls;

namespace CineCore.Views
{
    public partial class BookingConfirmationView : UserControl
    {
        public BookingConfirmationView(BookingConfirmationParams p)
        {
            InitializeComponent();
            DataContext = new BookingConfirmationViewModel(p);
        }
    }
}
