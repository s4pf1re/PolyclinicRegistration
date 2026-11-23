using System.Drawing;
using PolyclinicRegistration.Models;
using PolyclinicRegistration.Services;

namespace PolyclinicRegistration.Views;

public class RescheduleForm : Form
{
    private readonly AppointmentService _service;
    private readonly Appointment _appointment;
    private readonly DateTimePicker _date = new();
    private readonly FlowLayoutPanel _times = new();
    private TimeSpan? _selectedTime;

    public RescheduleForm(AppointmentService service, Appointment appointment)
    {
        _service = service;
        _appointment = appointment;

        Text = "Перенос записи";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(475, 455);
        BackColor = UiTheme.Background;
        Font = new Font("Segoe UI", 10);

        Controls.Add(UiTheme.Label($"Перенос записи {appointment.TicketNumber}",
            24, 20, 430, 15, true));
        Controls.Add(UiTheme.Label(appointment.Doctor.ToString(),
            24, 58, 430, 9, false, UiTheme.Muted));
        Controls.Add(UiTheme.Label("Новая дата", 24, 104, 425, 10, true));
        _date.Location = new Point(24, 135);
        _date.Size = new Size(426, 34);
        _date.Format = DateTimePickerFormat.Custom;
        _date.CustomFormat = "dddd, dd.MM.yyyy";
        _date.MinDate = DateTime.Today;
        _date.MaxDate = DateTime.Today.AddYears(1);
        _date.Value = appointment.VisitDate.Date >= DateTime.Today &&
                      appointment.VisitDate.Date <= _date.MaxDate
            ? appointment.VisitDate.Date
            : AppointmentService.FirstWorkingDate();
        UiTheme.StyleInput(_date);
        _date.ValueChanged += (_, _) => RefreshTimes();
        Controls.Add(_date);

        Controls.Add(UiTheme.Label("Свободное время", 24, 187, 426, 10, true));
        _times.Location = new Point(18, 218);
        _times.Size = new Size(436, 170);
        _times.AutoScroll = true;
        Controls.Add(_times);

        var save = UiTheme.Button("Сохранить новое время", UiTheme.Blue);
        save.Location = new Point(24, 402);
        save.Size = new Size(426, 40);
        save.Click += Confirm;
        Controls.Add(save);
        RefreshTimes();
    }

    private void RefreshTimes()
    {
        _selectedTime = null;
        _times.Controls.Clear();
        var available = _service.GetAvailableTimes(_appointment.Doctor,
            _date.Value.Date, _appointment.Id);
        foreach (var time in available)
        {
            var button = new Button
            {
                Text = time.ToString(@"hh\:mm"),
                Tag = time,
                Size = new Size(92, 36),
                Margin = new Padding(4, 3, 4, 4),
                FlatStyle = FlatStyle.Flat,
                BackColor = UiTheme.PaleBlue,
                ForeColor = UiTheme.Blue,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += (_, _) => SelectTime(button);
            _times.Controls.Add(button);
        }
    }

    private void SelectTime(Button selected)
    {
        _selectedTime = (TimeSpan)selected.Tag!;
        foreach (Button button in _times.Controls)
        {
            button.BackColor = button == selected ? UiTheme.Blue : UiTheme.PaleBlue;
            button.ForeColor = button == selected ? Color.White : UiTheme.Blue;
        }
    }

    private void Confirm(object? sender, EventArgs e)
    {
        if (_selectedTime is null)
        {
            MessageBox.Show(this, "Выберите свободное время.", "Перенос записи",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        try
        {
            _service.Reschedule(_appointment, _date.Value.Date, _selectedTime.Value);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (InvalidOperationException error)
        {
            MessageBox.Show(this, error.Message, "Не удалось перенести запись",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            RefreshTimes();
        }
    }
}
