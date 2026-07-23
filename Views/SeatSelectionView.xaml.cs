using CineCore.Services;
using CineCore.ViewModels;
using System.Windows.Controls;

namespace CineCore.Views
{
    public partial class SeatSelectionView : UserControl
    {
        public SeatSelectionView(SeatSelectionParams p)
        {
            InitializeComponent();
            DataContext = new SeatSelectionViewModel(p);
        }
    }
}
