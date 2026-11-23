using PolyclinicRegistration.Data;
using PolyclinicRegistration.Services;
using PolyclinicRegistration.Views;
using System.Globalization;

namespace PolyclinicRegistration;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("ru-RU");
        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("ru-RU");
        ApplicationConfiguration.Initialize();

        try
        {
            var repository = new AppointmentRepository();
            repository.Initialize();
            var service = new AppointmentService(repository);
            Application.Run(new MainForm(service, repository));
        }
        catch (Exception error)
        {
            MessageBox.Show(
                "Не удалось открыть базу данных: " + error.Message,
                "Ошибка запуска",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
