using CineCore.ViewModels;
using System.Windows.Controls;

namespace CineCore.Views
{
    public partial class BookingHistoryView : UserControl
    {
        public BookingHistoryView()
        {
            InitializeComponent();
            DataContext = new BookingHistoryViewModel();
        }
    }
}
