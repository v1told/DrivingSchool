using System.Windows;
using DrivingSchool.App.Infrastructure;

namespace DrivingSchool.App;

/// <summary>Точка входу застосунку: ініціалізація бази даних і показ головного вікна.</summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            Db.Initialize();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Не вдалося відкрити базу даних:\n" + ex.Message, "Помилка запуску",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }
        var window = new MainWindow();
        MainWindow = window;
        window.Show();
    }
}
