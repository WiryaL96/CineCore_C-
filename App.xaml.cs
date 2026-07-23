using CineCore.Services;
using System.Windows;

namespace CineCore
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ServiceLocator.Register<IDatabaseService>(new DatabaseService());
        }
    }
}
