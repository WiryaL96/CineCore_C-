using CineCore.Services;
using CineCore.ViewModels;
using System.Windows.Controls;

namespace CineCore.Views
{
    public partial class PaymentView : UserControl
    {
        // Constructor-nya wajib nerima PaymentParams dari NavigationService
        public PaymentView(PaymentParams parameters)
        {
            InitializeComponent();
            // Masukin mesinnya (ViewModel) dan kasih sesajen parameter-nya
            this.DataContext = new PaymentViewModel(parameters);
        }
    }
}