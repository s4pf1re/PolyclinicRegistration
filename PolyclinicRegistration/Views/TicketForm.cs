using System.Drawing;
using System.Text;
using PolyclinicRegistration.Models;

namespace PolyclinicRegistration.Views;

public class TicketForm : Form
{
    private readonly Appointment _appointment;

    public TicketForm(Appointment appointment)
    {
        _appointment = appointment;
        Text = "Талон на приём";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(570, 610);
        BackColor = UiTheme.Background;
        Font = new Font("Segoe UI", 10);

        var header = new Panel { Dock = DockStyle.Top, Height = 112, BackColor = UiTheme.Navy };
        header.Controls.Add(UiTheme.Label("ТАЛОН НА ПРИЁМ", 28, 18, 500,
            17, true, Color.White));
        header.Controls.Add(UiTheme.Label($"№ {appointment.TicketNumber}", 29, 57, 500,
            15, true, Color.FromArgb(194, 227, 255)));
        Controls.Add(header);

        var card = new Panel
        {
            Location = new Point(20, 132),
            Size = new Size(530, 380),
            BackColor = Color.White
        };
        Controls.Add(card);

        AddRow(card, 24, "Пациент", appointment.Patient.FullName);
        AddRow(card, 76, "Дата рождения", appointment.Patient.BirthDate.ToString("dd.MM.yyyy"));
        AddRow(card, 128, "Специальность", appointment.Doctor.Specialty);
        AddRow(card, 180, "Врач", appointment.Doctor.FullName);
        AddRow(card, 232, "Кабинет", appointment.Doctor.Room);
        AddRow(card, 284, "Дата и время",
            $"{appointment.VisitDate:dd.MM.yyyy}  ·  {appointment.VisitTime:hh\\:mm}");

        Controls.Add(UiTheme.Label("Пожалуйста, приходите за 10 минут до начала приёма.",
            24, 525, 520, 9, false, UiTheme.Muted));

        var save = UiTheme.Button("Сохранить талон", UiTheme.Blue);
        save.Location = new Point(24, 558);
        save.Size = new Size(240, 39);
        save.Click += SaveTicket;
        Controls.Add(save);
        var close = UiTheme.Button("Закрыть", UiTheme.Navy);
        close.Location = new Point(286, 558);
        close.Size = new Size(240, 39);
        close.Click += (_, _) => Close();
        Controls.Add(close);
        AcceptButton = close;
    }

    public static string BuildTicketText(Appointment appointment)
    {
        return $"""
            ТАЛОН НА ПРИЁМ № {appointment.TicketNumber}

            Пациент: {appointment.Patient.FullName}
            Дата рождения: {appointment.Patient.BirthDate:dd.MM.yyyy}
            Специальность: {appointment.Doctor.Specialty}
            Врач: {appointment.Doctor.FullName}
            Кабинет: {appointment.Doctor.Room}
            Дата приёма: {appointment.VisitDate:dd.MM.yyyy}
            Время приёма: {appointment.VisitTime:hh\:mm}

            Приходите за 10 минут до начала приёма.
            """;
    }

    private static void AddRow(Panel card, int y, string caption, string value)
    {
        card.Controls.Add(UiTheme.Label(caption.ToUpperInvariant(), 24, y, 482,
            8, true, UiTheme.Muted));
        var valueLabel = UiTheme.Label(value, 24, y + 20, 482, 11, true);
        valueLabel.Height = 29;
        card.Controls.Add(valueLabel);
    }

    private void SaveTicket(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Сохранить талон",
            Filter = "Текстовый файл (*.txt)|*.txt",
            FileName = $"Талон_{_appointment.TicketNumber}.txt",
            DefaultExt = "txt"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        try
        {
            File.WriteAllText(dialog.FileName, BuildTicketText(_appointment), Encoding.UTF8);
            MessageBox.Show(this, "Талон сохранён.", "Готово",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (IOException error)
        {
            MessageBox.Show(this, "Не удалось сохранить талон: " + error.Message,
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
