using System.Drawing.Drawing2D;

namespace PolyclinicRegistration;

public sealed class RegistrationForm : Form
{
    private readonly ClinicRepository _repository;
    private readonly Panel _body = new();
    private readonly Label _stepLabel = new();
    private readonly Label _footer = new();
    private int _step = 1;
    private string _fullName = "";
    private DateOnly _birthDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-30));
    private string _specialty = ClinicSchedule.Specialties[0];
    private string _doctorId = "";
    private DateOnly _visitDate = DateOnly.FromDateTime(DateTime.Today);
    private TimeOnly? _visitTime;
    private Appointment? _created;

    private static readonly Color Ink = Color.FromArgb(22, 45, 61);
    private static readonly Color Accent = Color.FromArgb(0, 115, 135);
    private static readonly Color Pale = Color.FromArgb(239, 247, 248);
    private static readonly Color Muted = Color.FromArgb(91, 110, 121);

    public RegistrationForm(ClinicRepository repository)
    {
        _repository = repository;
        Text = "Поликлиника · электронная регистратура";
        ClientSize = new Size(1000, 690);
        MinimumSize = MaximumSize = Size;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(247, 250, 251);
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.None;

        var banner = new Panel { Bounds = new Rectangle(0, 0, 1000, 104), BackColor = Ink };
        Controls.Add(banner);
        banner.Controls.Add(Label("Электронная регистратура", 31, 14, 690, 58, 20, Color.White, true));
        banner.Controls.Add(Label("Запись на приём по электронному талону", 32, 64, 650, 24, 10, Color.FromArgb(189, 216, 222)));
        var admin = Button("База записей", 814, 31, 152, 42, secondary: true);
        admin.Click += (_, _) => OpenAdmin();
        banner.Controls.Add(admin);

        _stepLabel.Bounds = new Rectangle(65, 118, 850, 30);
        _stepLabel.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _stepLabel.ForeColor = Accent;
        Controls.Add(_stepLabel);

        _body.Bounds = new Rectangle(64, 154, 872, 475);
        _body.BackColor = Color.White;
        _body.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(224, 233, 237));
            e.Graphics.DrawRectangle(pen, 0, 0, _body.Width - 1, _body.Height - 1);
        };
        Controls.Add(_body);

        _footer.Bounds = new Rectangle(66, 644, 850, 27);
        _footer.ForeColor = Muted;
        _footer.Font = new Font("Segoe UI", 9);
        _footer.Text = "Приём: понедельник–пятница, 09:00–17:00 · длительность талона 30 минут";
        Controls.Add(_footer);
        ShowStep();
    }

    private void ShowStep()
    {
        _body.Controls.Clear();
        _stepLabel.Text = _step switch
        {
            1 => "Шаг 1 из 4   ·   Данные посетителя",
            2 => "Шаг 2 из 4   ·   Выбор специальности",
            3 => "Шаг 3 из 4   ·   Выбор врача",
            4 => "Шаг 4 из 4   ·   Дата и свободное время",
            _ => "Запись подтверждена"
        };
        switch (_step)
        {
            case 1: ShowPatient(); break;
            case 2: ShowSpecialty(); break;
            case 3: ShowDoctor(); break;
            case 4: ShowSlots(); break;
            case 5: ShowTicket(); break;
        }
    }

    private void ShowPatient()
    {
        _body.Controls.Add(Label("Данные посетителя", 36, 23, 700, 60, 18, Ink, true));
        _body.Controls.Add(Label("Укажите данные для оформления талона.", 38, 83, 730, 27, 10, Muted));
        _body.Controls.Add(Label("Фамилия, имя, отчество", 39, 126, 470, 26, 10, Ink, true));
        var name = new TextBox { Bounds = new Rectangle(39, 158, 575, 39), Font = new Font("Segoe UI", 12), Text = _fullName, MaxLength = 120 };
        name.PlaceholderText = "Например, Иванов Иван Иванович";
        _body.Controls.Add(name);
        _body.Controls.Add(Label("Дата рождения", 39, 222, 470, 26, 10, Ink, true));
        var birth = new DateTimePicker
        {
            Bounds = new Rectangle(39, 254, 290, 36), Format = DateTimePickerFormat.Long,
            MinDate = new DateTime(1900, 1, 1), MaxDate = DateTime.Today,
            Value = _birthDate.ToDateTime(TimeOnly.MinValue), Font = new Font("Segoe UI", 11)
        };
        _body.Controls.Add(birth);
        var consent = new CheckBox
        {
            Text = "Согласен(на) на хранение данных для оформления записи",
            Bounds = new Rectangle(39, 321, 730, 36), ForeColor = Ink, AutoSize = false
        };
        _body.Controls.Add(consent);
        var next = Button("Выбрать специалиста", 591, 400, 238, 44);
        next.Click += (_, _) =>
        {
            var normalized = string.Join(' ', name.Text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
            if (normalized.Length < 5 || normalized.Split(' ').Length < 2)
            {
                MessageBox.Show("Введите как минимум фамилию и имя.", "Проверьте данные", MessageBoxButtons.OK, MessageBoxIcon.Information);
                name.Focus(); return;
            }
            if (!consent.Checked)
            {
                MessageBox.Show("Для оформления талона подтвердите согласие на хранение введённых данных.",
                    "Подтверждение", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            _fullName = normalized;
            _birthDate = DateOnly.FromDateTime(birth.Value);
            _step = 2;
            ShowStep();
        };
        _body.Controls.Add(next);
    }

    private void ShowSpecialty()
    {
        _body.Controls.Add(Label("Выберите специальность", 36, 23, 760, 60, 18, Ink, true));
        _body.Controls.Add(Label("После этого вы сможете выбрать конкретного врача и кабинет.", 38, 83, 760, 27, 10, Muted));
        var list = new ListBox
        {
            Bounds = new Rectangle(39, 121, 490, 262), Font = new Font("Segoe UI", 12),
            ItemHeight = 31, BorderStyle = BorderStyle.FixedSingle
        };
        list.Items.AddRange(ClinicSchedule.Specialties);
        list.SelectedItem = _specialty;
        _body.Controls.Add(list);
        var hint = new Panel { Bounds = new Rectangle(558, 121, 268, 262), BackColor = Pale };
        hint.Controls.Add(Label("Как проходит запись", 20, 23, 228, 38, 12, Accent, true));
        hint.Controls.Add(Label("1. Выберите специальность.\n\n2. Выберите врача.\n\n3. Укажите дату и время.\n\n4. Получите талон.",
            20, 73, 228, 164, 10, Ink));
        _body.Controls.Add(hint);
        var back = Button("Назад", 39, 400, 129, 44, secondary: true);
        back.Click += (_, _) => { _step = 1; ShowStep(); };
        _body.Controls.Add(back);
        var next = Button("Выбрать врача", 631, 400, 198, 44);
        next.Click += (_, _) =>
        {
            if (list.SelectedItem is not string specialty) return;
            if (_specialty != specialty) _doctorId = "";
            _specialty = specialty;
            _visitTime = null;
            _step = 3;
            ShowStep();
        };
        _body.Controls.Add(next);
    }

    private void ShowDoctor()
    {
        _body.Controls.Add(Label("Выберите врача", 36, 23, 760, 60, 18, Ink, true));
        _body.Controls.Add(Label($"Направление: {_specialty}. Кабинет указан рядом с ФИО врача.", 38, 83, 760, 27, 10, Muted));
        var doctors = ClinicSchedule.DoctorsFor(_specialty);
        var list = new ListBox
        {
            Bounds = new Rectangle(39, 121, 790, 240), Font = new Font("Segoe UI", 12),
            ItemHeight = 38, BorderStyle = BorderStyle.FixedSingle
        };
        foreach (var doctor in doctors)
            list.Items.Add($"{doctor.FullName}   ·   кабинет №{doctor.Room}");
        var selected = Array.FindIndex(doctors, doctor => doctor.Id == _doctorId);
        if (selected >= 0) list.SelectedIndex = selected;
        _body.Controls.Add(list);
        var back = Button("Назад", 39, 400, 129, 44, secondary: true);
        back.Click += (_, _) => { _step = 2; ShowStep(); };
        _body.Controls.Add(back);
        var next = Button("Выбрать время", 631, 400, 198, 44);
        next.Click += (_, _) =>
        {
            if (list.SelectedIndex < 0)
            {
                MessageBox.Show("Выберите врача из списка.", "Врач", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            _doctorId = doctors[list.SelectedIndex].Id;
            _visitTime = null;
            _step = 4;
            ShowStep();
        };
        _body.Controls.Add(next);
    }

    private void ShowSlots()
    {
        var doctor = ClinicSchedule.FindDoctor(_doctorId)!;
        _body.Controls.Add(Label("Выберите время приёма", 36, 23, 760, 60, 18, Ink, true));
        _body.Controls.Add(Label($"{doctor.FullName} · кабинет №{doctor.Room}. Занятые талоны недоступны.", 38, 83, 790, 27, 10, Muted));
        _body.Controls.Add(Label("Дата приёма", 39, 119, 300, 26, 10, Ink, true));
        var day = new DateTimePicker
        {
            Bounds = new Rectangle(39, 150, 300, 36), Format = DateTimePickerFormat.Long,
            MinDate = DateTime.Today, MaxDate = DateTime.Today.AddDays(30),
            Value = _visitDate.ToDateTime(TimeOnly.MinValue), Font = new Font("Segoe UI", 11)
        };
        _body.Controls.Add(day);
        var note = Label("", 370, 153, 430, 50, 10, Muted);
        _body.Controls.Add(note);
        var slots = new FlowLayoutPanel
        {
            Bounds = new Rectangle(39, 217, 790, 165), FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true, AutoScroll = false
        };
        _body.Controls.Add(slots);
        void RefreshSlots()
        {
            _visitDate = DateOnly.FromDateTime(day.Value);
            _visitTime = null;
            slots.Controls.Clear();
            if (_visitDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                note.Text = "Выходной день. Выберите понедельник–пятницу.";
                return;
            }
            note.Text = "Талоны по 30 минут. Серые уже заняты или прошли.";
            var occupied = _repository.OccupiedSlots(_doctorId, _visitDate);
            foreach (var time in ClinicSchedule.Times())
            {
                var available = !occupied.Contains(time) && ClinicSchedule.IsValidSlot(_visitDate, time);
                var slot = Button(time.ToString("HH:mm"), 0, 0, 87, 40, secondary: true);
                slot.Margin = new Padding(0, 0, 11, 10);
                slot.Enabled = available;
                if (!available) slot.BackColor = Color.FromArgb(231, 235, 237);
                slot.Click += (_, _) =>
                {
                    _visitTime = time;
                    foreach (Control other in slots.Controls)
                        if (other is Button b && b.Enabled) { b.BackColor = Color.White; b.ForeColor = Accent; }
                    slot.BackColor = Accent;
                    slot.ForeColor = Color.White;
                };
                slots.Controls.Add(slot);
            }
        }
        day.ValueChanged += (_, _) => RefreshSlots();
        RefreshSlots();
        var back = Button("Назад", 39, 400, 129, 44, secondary: true);
        back.Click += (_, _) => { _step = 3; ShowStep(); };
        _body.Controls.Add(back);
        var book = Button("Оформить талон", 627, 400, 202, 44);
        book.Click += (_, _) =>
        {
            if (_visitTime is null)
            {
                MessageBox.Show("Выберите свободное время.", "Время приёма", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                _created = _repository.Book(_fullName, _birthDate, _doctorId, _visitDate, _visitTime.Value);
                _step = 5;
                ShowStep();
            }
            catch (SlotUnavailableException ex)
            {
                MessageBox.Show(ex.Message, "Время занято", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshSlots();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Не удалось оформить талон", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };
        _body.Controls.Add(book);
    }

    private void ShowTicket()
    {
        var appointment = _created!;
        _body.Controls.Add(Label("Талон оформлен", 39, 22, 700, 67, 20, Accent, true));
        _body.Controls.Add(Label("Сохраните номер талона и приходите за 10 минут до приёма.", 40, 91, 780, 29, 10, Muted));
        var ticket = new Panel { Bounds = new Rectangle(39, 132, 790, 241), BackColor = Pale };
        ticket.Controls.Add(Label(appointment.TicketNumber, 28, 12, 390, 72, 25, Ink, true));
        ticket.Controls.Add(Label($"Пациент: {appointment.FullName}", 30, 74, 710, 28, 10, Ink));
        ticket.Controls.Add(Label($"Специальность: {appointment.Specialty}", 30, 105, 710, 28, 10, Ink));
        ticket.Controls.Add(Label($"Врач: {appointment.DoctorName} · кабинет №{appointment.Room}", 30, 136, 730, 28, 10, Ink));
        ticket.Controls.Add(Label($"Дата и время: {appointment.VisitDate:dd.MM.yyyy} в {appointment.VisitTime:HH:mm}",
            30, 167, 710, 28, 10, Ink));
        ticket.Controls.Add(Label("Для отмены записи обратитесь к сотруднику регистратуры.", 30, 204, 710, 24, 9, Muted));
        _body.Controls.Add(ticket);
        var again = Button("Оформить ещё один талон", 580, 400, 249, 44);
        again.Click += (_, _) =>
        {
            _fullName = "";
            _birthDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-30));
            _doctorId = "";
            _visitDate = DateOnly.FromDateTime(DateTime.Today);
            _visitTime = null;
            _created = null;
            _step = 1;
            ShowStep();
        };
        _body.Controls.Add(again);
    }

    private void OpenAdmin()
    {
        using var login = new StaffLoginForm();
        if (login.ShowDialog(this) != DialogResult.OK) return;
        using var admin = new AdminForm(_repository);
        admin.ShowDialog(this);
        if (_step == 4) ShowStep();
    }

    internal static Label Label(string text, int x, int y, int width, int height, float size, Color color, bool bold = false) =>
        new()
        {
            Text = text, Bounds = new Rectangle(x, y, width, height),
            Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
            ForeColor = color, TextAlign = ContentAlignment.MiddleLeft
        };

    internal static Button Button(string text, int x, int y, int width, int height, bool secondary = false)
    {
        var button = new Button
        {
            Text = text, Bounds = new Rectangle(x, y, width, height),
            BackColor = secondary ? Color.White : Accent,
            ForeColor = secondary ? Accent : Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderColor = secondary ? Accent : Accent;
        button.FlatAppearance.BorderSize = 1;
        return button;
    }
}

public sealed class StaffLoginForm : Form
{
    public StaffLoginForm()
    {
        Text = "Доступ сотрудника";
        ClientSize = new Size(380, 210);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = MinimizeBox = false;
        Font = new Font("Segoe UI", 10);
        Controls.Add(RegistrationForm.Label("Введите пароль сотрудника", 26, 18, 330, 35, 14, Color.FromArgb(22, 45, 61), true));
        var password = new TextBox
        {
            Bounds = new Rectangle(27, 69, 325, 35), UseSystemPasswordChar = true,
            Font = new Font("Segoe UI", 12), MaxLength = 32
        };
        Controls.Add(password);
        var error = RegistrationForm.Label("", 27, 109, 330, 25, 9, Color.Firebrick);
        Controls.Add(error);
        var enter = RegistrationForm.Button("Войти", 230, 151, 122, 39);
        enter.Click += (_, _) =>
        {
            if (password.Text == "111") { DialogResult = DialogResult.OK; Close(); }
            else { error.Text = "Неверный пароль"; password.Clear(); password.Focus(); }
        };
        Controls.Add(enter);
        AcceptButton = enter;
    }
}
