using CineCore.Models;
using CineCore.ViewModels;
using System.Windows.Controls;

namespace CineCore.Views
{
    public partial class MovieDetailView : UserControl
    {
        public MovieDetailView(Movie movie)
        {
            InitializeComponent();
            DataContext = new MovieDetailViewModel(movie);
        }
    }
}
