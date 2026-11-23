namespace PolyclinicRegistration;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        try
        {
            var repository = new ClinicRepository();
            repository.Initialize();
            Application.Run(new RegistrationForm(repository));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось запустить приложение: {ex.Message}",
                "Ошибка запуска", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }    
}
