using System.Drawing;
using PolyclinicRegistration.Data;
using PolyclinicRegistration.Models;
using PolyclinicRegistration.Services;

namespace PolyclinicRegistration.Views;

public class MainForm : Form
{
    private readonly AppointmentService _service;
    private readonly AppointmentRepository _repository;
    private readonly TextBox _fullName = new();
    private readonly DateTimePicker _birthDate = new();
    private readonly ComboBox _specialty = new();
    private readonly ComboBox _doctor = new();
    private readonly Label _room = new();
    private readonly DateTimePicker _visitDate = new();
    private readonly FlowLayoutPanel _timeSlots = new();
    private readonly Label _slotHint = new();
    private TimeSpan? _selectedTime;

    public MainForm(AppointmentService service, AppointmentRepository repository)
    {
        _service = service;
        _repository = repository;

        Text = "Поликлиника — запись на приём";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1040, 735);
        Size = new Size(1120, 785);
        BackColor = UiTheme.Background;
        Font = new Font("Segoe UI", 10);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        Controls.Add(root);

        var header = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Navy };
        header.Controls.Add(UiTheme.Label("ПОЛИКЛИНИКА  ·  ЗАПИСЬ НА ПРИЁМ", 24, 12, 900,
            19, true, Color.White));
        header.Controls.Add(UiTheme.Label("Выберите врача и свободное время — талон появится после записи.",
            26, 53, 900, 10, false, Color.FromArgb(207, 222, 236)));
        root.Controls.Add(header, 0, 0);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(20, 20, 20, 14),
            BackColor = UiTheme.Background
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        root.Controls.Add(content, 0, 1);

        var patientCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White,
            Margin = new Padding(0, 0, 9, 0) };
        var bookingCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White,
            Margin = new Padding(9, 0, 0, 0) };
        content.Controls.Add(patientCard, 0, 0);
        content.Controls.Add(bookingCard, 1, 0);
        BuildPatientCard(patientCard);
        BuildBookingCard(bookingCard);

        var footer = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Background };
        footer.Controls.Add(UiTheme.Label("Приём: понедельник–пятница, 09:00–17:00 · шаг 30 минут",
            22, 16, 650, 9, false, UiTheme.Muted));
        var staffContainer = new Panel
        {
            Dock = DockStyle.Right,
            Width = 310,
            Padding = new Padding(0, 8, 22, 10)
        };
        var staffButton = UiTheme.Button("Вход для сотрудников", UiTheme.Navy);
        staffButton.Dock = DockStyle.Fill;
        staffButton.Click += OpenStaffArea;
        staffContainer.Controls.Add(staffButton);
        footer.Controls.Add(staffContainer);
        root.Controls.Add(footer, 0, 2);

        _specialty.Items.AddRange(DoctorCatalog.All.Select(item => item.Specialty)
            .Distinct().ToArray());
        _specialty.SelectedIndexChanged += (_, _) => UpdateDoctors();
        _doctor.SelectedIndexChanged += (_, _) => UpdateDoctor();
        _visitDate.ValueChanged += (_, _) => RefreshTimeSlots();
        _specialty.SelectedIndex = 0;
        _visitDate.Value = AppointmentService.FirstWorkingDate();
    }

    private void BuildPatientCard(Panel card)
    {
        card.Controls.Add(UiTheme.Label("Данные пациента", 24, 22, 430, 15, true));
        card.Controls.Add(UiTheme.Label("Фамилия, имя, отчество", 25, 77, 420));
        _fullName.Location = new Point(25, 108);
        _fullName.Size = new Size(430, 35);
        _fullName.PlaceholderText = "Например: Иванов Иван Иванович";
        _fullName.BorderStyle = BorderStyle.FixedSingle;
        UiTheme.StyleInput(_fullName);
        card.Controls.Add(_fullName);

        card.Controls.Add(UiTheme.Label("Дата рождения", 25, 168, 420));
        _birthDate.Location = new Point(25, 198);
        _birthDate.Size = new Size(430, 35);
        _birthDate.Format = DateTimePickerFormat.Custom;
        _birthDate.CustomFormat = "dd.MM.yyyy";
        _birthDate.MinDate = new DateTime(1900, 1, 1);
        _birthDate.MaxDate = DateTime.Today;
        _birthDate.Value = DateTime.Today.AddYears(-25);
        UiTheme.StyleInput(_birthDate);
        card.Controls.Add(_birthDate);

        card.Controls.Add(UiTheme.Label("Специальность", 25, 258, 420));
        _specialty.Location = new Point(25, 288);
        _specialty.Size = new Size(430, 35);
        _specialty.DropDownStyle = ComboBoxStyle.DropDownList;
        UiTheme.StyleInput(_specialty);
        card.Controls.Add(_specialty);

        card.Controls.Add(UiTheme.Label("Врач", 25, 348, 420));
        _doctor.Location = new Point(25, 378);
        _doctor.Size = new Size(430, 35);
        _doctor.DropDownStyle = ComboBoxStyle.DropDownList;
        UiTheme.StyleInput(_doctor);
        card.Controls.Add(_doctor);

        _room.Location = new Point(25, 437);
        _room.Size = new Size(430, 31);
        _room.ForeColor = UiTheme.Blue;
        _room.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        card.Controls.Add(_room);

        var note = UiTheme.Label("Проверьте данные перед подтверждением записи.",
            25, 491, 440, 9, false, UiTheme.Muted);
        note.Height = 45;
        card.Controls.Add(note);

        card.Resize += (_, _) =>
        {
            var width = Math.Max(250, card.ClientSize.Width - 50);
            _fullName.Width = width;
            _birthDate.Width = width;
            _specialty.Width = width;
            _doctor.Width = width;
            _room.Width = width;
            note.Width = width;
        };
    }

    private void BuildBookingCard(Panel card)
    {
        card.Controls.Add(UiTheme.Label("Дата и время приёма", 24, 22, 460, 15, true));
        card.Controls.Add(UiTheme.Label("Дата приёма", 25, 77, 440));
        _visitDate.Location = new Point(25, 108);
        _visitDate.Size = new Size(455, 35);
        _visitDate.Format = DateTimePickerFormat.Custom;
        _visitDate.CustomFormat = "dddd, dd.MM.yyyy";
        _visitDate.MinDate = DateTime.Today;
        _visitDate.MaxDate = DateTime.Today.AddYears(1);
        UiTheme.StyleInput(_visitDate);
        card.Controls.Add(_visitDate);

        card.Controls.Add(UiTheme.Label("Свободное время", 25, 165, 440, 11, true));
        _timeSlots.Location = new Point(20, 200);
        _timeSlots.Size = new Size(470, 235);
        _timeSlots.AutoScroll = true;
        _timeSlots.WrapContents = true;
        _timeSlots.BackColor = Color.White;
        card.Controls.Add(_timeSlots);

        _slotHint.Location = new Point(25, 447);
        _slotHint.Size = new Size(450, 38);
        _slotHint.Font = new Font("Segoe UI", 9);
        _slotHint.ForeColor = UiTheme.Muted;
        card.Controls.Add(_slotHint);

        var registerButton = UiTheme.Button("Записаться и получить талон", UiTheme.Blue);
        registerButton.Location = new Point(25, 491);
        registerButton.Size = new Size(455, 48);
        registerButton.Click += RegisterPatient;
        card.Controls.Add(registerButton);

        card.Resize += (_, _) =>
        {
            var width = Math.Max(280, card.ClientSize.Width - 50);
            _visitDate.Width = width;
            _timeSlots.Width = Math.Max(280, card.ClientSize.Width - 40);
            _slotHint.Width = width;
            registerButton.Width = width;
        };
    }

    private void UpdateDoctors()
    {
        _doctor.Items.Clear();
        if (_specialty.SelectedItem is string specialty)
        {
            foreach (var doctor in DoctorCatalog.All.Where(item => item.Specialty == specialty))
                _doctor.Items.Add(doctor);
            if (_doctor.Items.Count > 0)
                _doctor.SelectedIndex = 0;
        }
    }

    private void UpdateDoctor()
    {
        _room.Text = _doctor.SelectedItem is Doctor doctor
            ? $"Кабинет {doctor.Room}"
            : "";
        RefreshTimeSlots();
    }

    private void RefreshTimeSlots()
    {
        _selectedTime = null;
        _timeSlots.Controls.Clear();
        if (_doctor.SelectedItem is not Doctor doctor)
            return;

        var available = _service.GetAvailableTimes(doctor, _visitDate.Value.Date).ToHashSet();
        var count = 0;
        for (var time = new TimeSpan(9, 0, 0);
             time <= new TimeSpan(16, 30, 0);
             time = time.Add(TimeSpan.FromMinutes(30)))
        {
            var enabled = available.Contains(time);
            var button = new Button
            {
                Text = time.ToString(@"hh\:mm"),
                Tag = time,
                Size = new Size(94, 38),
                Margin = new Padding(5, 3, 5, 5),
                Enabled = enabled,
                FlatStyle = FlatStyle.Flat,
                BackColor = enabled ? UiTheme.PaleBlue : Color.FromArgb(239, 242, 245),
                ForeColor = enabled ? UiTheme.Blue : Color.FromArgb(160, 170, 180),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = enabled ? Cursors.Hand : Cursors.Default
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += (_, _) => SelectTime(button);
            _timeSlots.Controls.Add(button);
            if (enabled) count++;
        }
        _slotHint.Text = count == 0
            ? "Свободного времени нет. Выберите другую дату."
            : $"Свободно: {count}. Серые интервалы недоступны.";
    }

    private void SelectTime(Button selected)
    {
        _selectedTime = (TimeSpan)selected.Tag!;
        foreach (Button button in _timeSlots.Controls)
        {
            if (!button.Enabled) continue;
            button.BackColor = button == selected ? UiTheme.Blue : UiTheme.PaleBlue;
            button.ForeColor = button == selected ? Color.White : UiTheme.Blue;
        }
    }

    private void RegisterPatient(object? sender, EventArgs e)
    {
        if (_doctor.SelectedItem is not Doctor doctor || _selectedTime is null)
        {
            MessageBox.Show(this, "Выберите врача и свободное время приёма.",
                "Запись на приём", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            var appointment = _service.Register(_fullName.Text, _birthDate.Value.Date,
                doctor, _visitDate.Value.Date, _selectedTime.Value);
            using var ticket = new TicketForm(appointment);
            ticket.ShowDialog(this);
            _fullName.Clear();
            RefreshTimeSlots();
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException)
        {
            MessageBox.Show(this, error.Message, "Проверьте запись",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            RefreshTimeSlots();
        }
    }

    private void OpenStaffArea(object? sender, EventArgs e)
    {
        using var login = new StaffLoginForm();
        if (login.ShowDialog(this) != DialogResult.OK)
            return;
        using var staff = new StaffForm(_repository, _service);
        staff.ShowDialog(this);
        RefreshTimeSlots();
    }
}
