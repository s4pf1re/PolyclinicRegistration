namespace PolyclinicRegistration;

public sealed class AdminForm : Form
{
    private readonly ClinicRepository _repository;
    private readonly DataGridView _grid;
    private readonly TextBox _search;
    private readonly DateTimePicker _date;
    private readonly CheckBox _allDates;
    private readonly Label _count;
    private List<Appointment> _items = [];

    public AdminForm(ClinicRepository repository)
    {
        _repository = repository;
        Text = "База записей · доступ сотрудника";
        ClientSize = new Size(1280, 640);
        MinimumSize = new Size(950, 580);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(247, 250, 251);
        Font = new Font("Segoe UI", 10);

        AutoScaleMode = AutoScaleMode.None;
        var header = new Panel { Bounds = new Rectangle(0, 0, 1280, 76), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BackColor = Color.FromArgb(22, 45, 61) };
        header.Controls.Add(RegistrationForm.Label("Журнал записей", 25, 8, 650, 58, 19, Color.White, true));
        Controls.Add(header);

        var filter = new Panel { Bounds = new Rectangle(0, 76, 1280, 114), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BackColor = Color.White };
        Controls.Add(filter);
        filter.Controls.Add(RegistrationForm.Label("Поиск по ФИО или талону", 24, 9, 260, 25, 9, Color.FromArgb(72, 92, 105)));
        _search = new TextBox { Bounds = new Rectangle(24, 38, 314, 33), PlaceholderText = "Иванов или Т-000001" };
        filter.Controls.Add(_search);
        filter.Controls.Add(RegistrationForm.Label("Дата", 365, 9, 200, 25, 9, Color.FromArgb(72, 92, 105)));
        _date = new DateTimePicker { Bounds = new Rectangle(365, 38, 185, 33), Format = DateTimePickerFormat.Short, Value = DateTime.Today };
        filter.Controls.Add(_date);
        _allDates = new CheckBox { Text = "Все даты", Bounds = new Rectangle(570, 40, 130, 28), Checked = true };
        filter.Controls.Add(_allDates);
        var refresh = RegistrationForm.Button("Обновить", 1123, 34, 131, 38);
        refresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        refresh.Click += (_, _) => RefreshData();
        filter.Controls.Add(refresh);
        _count = RegistrationForm.Label("", 24, 77, 890, 28, 9, Color.FromArgb(72, 92, 105));
        filter.Controls.Add(_count);
        _search.TextChanged += (_, _) => RefreshData();
        _date.ValueChanged += (_, _) => RefreshData();
        _allDates.CheckedChanged += (_, _) => { _date.Enabled = !_allDates.Checked; RefreshData(); };
        _date.Enabled = false;

        var bottom = new Panel { Bounds = new Rectangle(0, 568, 1280, 72), Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, BackColor = Color.White };
        Controls.Add(bottom);
        var cancel = RegistrationForm.Button("Отменить выбранную запись", 24, 16, 260, 40, secondary: true);
        cancel.Click += (_, _) => CancelSelected();
        bottom.Controls.Add(cancel);
        bottom.Controls.Add(RegistrationForm.Label("Отменённый талон освобождает время для новой записи.",
            308, 18, 700, 36, 9, Color.FromArgb(72, 92, 105)));

        _grid = new DataGridView
        {
            Bounds = new Rectangle(0, 190, 1280, 378), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            ReadOnly = true, AllowUserToAddRows = false,
            AllowUserToDeleteRows = false, AllowUserToResizeRows = false,
            MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
            RowHeadersVisible = false, RowTemplate = { Height = 34 },
            ColumnHeadersHeight = 42, EnableHeadersVisualStyles = false
        };
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(232, 242, 245);
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(22, 45, 61);
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(205, 235, 241);
        _grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(22, 45, 61);
        Controls.Add(_grid);
        RefreshData();
    }

    private void RefreshData()
    {
        try
        {
            DateOnly? date = _allDates.Checked ? null : DateOnly.FromDateTime(_date.Value);
            _items = _repository.GetAppointments(date, _search.Text).ToList();
            _grid.DataSource = _items.Select(a => new
            {
                a.Id,
                Талон = a.TicketNumber,
                Пациент = a.FullName,
                Рождение = a.BirthDate.ToString("dd.MM.yyyy"),
                Специалист = a.Specialty,
                Врач = a.DoctorName,
                Кабинет = a.Room,
                Дата = a.VisitDate.ToString("dd.MM.yyyy"),
                Время = a.VisitTime.ToString("HH:mm"),
                Статус = a.Status == "active" ? "Активна" : "Отменена"
            }).ToList();
            if (_grid.Columns["Id"] is { } id) id.Visible = false;
            if (_grid.Columns["Талон"] is { } ticket) ticket.FillWeight = 90;
            if (_grid.Columns["Пациент"] is { } patient) patient.FillWeight = 190;
            if (_grid.Columns["Рождение"] is { } birth) birth.FillWeight = 95;
            if (_grid.Columns["Специалист"] is { } specialty) specialty.FillWeight = 105;
            if (_grid.Columns["Врач"] is { } doctor) doctor.FillWeight = 195;
            if (_grid.Columns["Кабинет"] is { } room) room.FillWeight = 75;
            if (_grid.Columns["Дата"] is { } visitDate) visitDate.FillWeight = 95;
            if (_grid.Columns["Время"] is { } visitTime) visitTime.FillWeight = 75;
            if (_grid.Columns["Статус"] is { } status) status.FillWeight = 85;
            _count.Text = $"Найдено записей: {_items.Count} · активных: {_items.Count(a => a.Status == "active")}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка чтения базы", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CancelSelected()
    {
        if (_grid.CurrentRow?.Cells["Id"].Value is not long id) return;
        var appointment = _items.FirstOrDefault(a => a.Id == id);
        if (appointment is null || appointment.Status != "active")
        {
            MessageBox.Show("Выберите активную запись.", "Отмена записи", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var answer = MessageBox.Show($"Отменить талон {appointment.TicketNumber} для {appointment.FullName}?",
            "Подтверждение отмены", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (answer != DialogResult.Yes) return;
        try
        {
            _repository.Cancel(id);
            RefreshData();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка отмены", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
