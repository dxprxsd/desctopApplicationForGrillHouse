using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GrillHouseNNProg.Models;
using GrillHouseNNProg.ViewModels;
using GrillHouseNNProg.Views;

namespace GrillHouseNNProg
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var dbContext = new GrillcitynnContext(); // Создаем экземпляр БД
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(dbContext),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}