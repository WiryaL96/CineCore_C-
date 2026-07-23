using CineCore.ViewModels;
using System.Windows.Controls;

namespace CineCore.Views
{
    public partial class AdminPanelView : UserControl
    {
        public AdminPanelView()
        {
            InitializeComponent();
            DataContext = new AdminPanelViewModel();
        }
    }
}
