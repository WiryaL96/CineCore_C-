using CineCore.ViewModels; //panggil untuk kepala
using System.Windows.Controls;

namespace CineCore.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();

            //tampilan for xaml njay
            this.DataContext = new DashboardViewModel();
        }
    }
}