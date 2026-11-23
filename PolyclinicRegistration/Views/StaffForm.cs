using System.Drawing;
using PolyclinicRegistration.Data;
using PolyclinicRegistration.Models;
using PolyclinicRegistration.Services;

namespace PolyclinicRegistration.Views;

public class StaffForm : Form
{
    private readonly AppointmentRepository _repository;
    private readonly AppointmentService _service;
    private readonly DataGridView _appointmentsGrid = CreateGrid();
    private readonly DataGridView _patientsGrid = CreateGrid();
    private readonly TextBox _search = new();
    private readonly ComboBox _status = new();
    private readonly Label _count = new();
    private List<Appointment> _appointments = new();

    public StaffForm(AppointmentRepository repository, AppointmentService service)
    {
        _repository = repository;
        _service = service;

        Text = "База данных — сотрудник";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1100, 680);
        Size = new Size(1340, 780);
        BackColor = UiTheme.Background;
        Font = new Font("Segoe UI", 10);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 73));
        Controls.Add(root);

        var header = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Navy };
        header.Controls.Add(UiTheme.Label("БАЗА ДАННЫХ ПОЛИКЛИНИКИ", 22, 13,
            900, 18, true, Color.White));
        header.Controls.Add(UiTheme.Label("Записи пациентов и журнал приёма",
            24, 51, 800, 9, false, Color.FromArgb(207, 222, 236)));
        root.Controls.Add(header, 0, 0);

        var toolbar = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Background };
        toolbar.Controls.Add(UiTheme.Label("Поиск", 22, 17, 70, 9, true));
        _search.Location = new Point(92, 14);
        _search.Size = new Size(340, 34);
        _search.PlaceholderText = "ФИО, талон или врач";
        UiTheme.StyleInput(_search);
        _search.TextChanged += (_, _) => ApplyFilter();
        toolbar.Controls.Add(_search);

        toolbar.Controls.Add(UiTheme.Label("Статус", 456, 17, 80, 9, true));
        _status.Location = new Point(527, 14);
        _status.Size = new Size(170, 34);
        _status.DropDownStyle = ComboBoxStyle.DropDownList;
        UiTheme.StyleInput(_status);
        _status.Items.AddRange(new object[] { "Все", "Активна", "Отменена" });
        _status.SelectedIndexChanged += (_, _) => ApplyFilter();
        toolbar.Controls.Add(_status);

        var refresh = UiTheme.Button("Обновить", UiTheme.Navy);
        refresh.Location = new Point(715, 12);
        refresh.Size = new Size(138, 38);
        refresh.Click += (_, _) => Reload();
        toolbar.Controls.Add(refresh);
        root.Controls.Add(toolbar, 0, 1);

        var tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10),
            Margin = new Padding(20, 0, 20, 0) };
        var appointmentsPage = new TabPage("Записи на приём");
        var patientsPage = new TabPage("Пациенты");
        tabs.TabPages.Add(appointmentsPage);
        tabs.TabPages.Add(patientsPage);
        appointmentsPage.Controls.Add(_appointmentsGrid);
        patientsPage.Controls.Add(_patientsGrid);
        root.Controls.Add(tabs, 0, 2);

        ConfigureAppointmentColumns();
        ConfigurePatientColumns();

        var actions = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Background };
        _count.Location = new Point(22, 21);
        _count.Size = new Size(360, 30);
        _count.ForeColor = UiTheme.Muted;
        actions.Controls.Add(_count);
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 800,
            Height = 55,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 14, 0),
            WrapContents = false
        };
        AddAction(buttons, "Удалить запись", UiTheme.Navy, DeleteSelected);
        AddAction(buttons, "Отменить запись", Color.FromArgb(182, 95, 54), CancelSelected);
        AddAction(buttons, "Перенести", UiTheme.Blue, RescheduleSelected);
        AddAction(buttons, "Показать талон", UiTheme.Blue, ShowSelectedTicket);
        actions.Controls.Add(buttons);
        root.Controls.Add(actions, 0, 3);

        _status.SelectedIndex = 0;
        Reload();
        Shown += (_, _) =>
        {
            _search.Focus();
            _appointmentsGrid.ClearSelection();
            _appointmentsGrid.CurrentCell = null;
        };
    }

    private static DataGridView CreateGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ColumnHeadersHeight = 40,
            RowTemplate = { Height = 36 },
            EnableHeadersVisualStyles = false,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = UiTheme.Navy,
                ForeColor = Color.White,
                SelectionBackColor = UiTheme.Navy,
                SelectionForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            },
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 9),
                ForeColor = UiTheme.Text,
                SelectionBackColor = UiTheme.PaleBlue,
                SelectionForeColor = UiTheme.Text
            },
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 250, 252)
            }
        };
        return grid;
    }

    private void ConfigureAppointmentColumns()
    {
        AddColumn(_appointmentsGrid, "ticket", "Талон", 80);
        AddColumn(_appointmentsGrid, "patient", "Пациент", 255);
        AddColumn(_appointmentsGrid, "birth", "Рожд.", 90);
        AddColumn(_appointmentsGrid, "specialty", "Спец.", 105);
        AddColumn(_appointmentsGrid, "doctor", "Врач", 250);
        AddColumn(_appointmentsGrid, "room", "Каб.", 50);
        AddColumn(_appointmentsGrid, "date", "Дата", 105);
        AddColumn(_appointmentsGrid, "time", "Время", 90);
        AddColumn(_appointmentsGrid, "status", "Статус", 100);
        _appointmentsGrid.Columns["birth"]!.HeaderCell.ToolTipText = "Дата рождения";
        _appointmentsGrid.Columns["specialty"]!.HeaderCell.ToolTipText = "Специальность";
    }

    private void ConfigurePatientColumns()
    {
        AddColumn(_patientsGrid, "id", "№ в базе", 85);
        AddColumn(_patientsGrid, "name", "ФИО пациента", 320);
        AddColumn(_patientsGrid, "birth", "Дата рождения", 125);
        AddColumn(_patientsGrid, "count", "Записей в журнале", 160);
    }

    private static void AddColumn(DataGridView grid, string name, string header, float weight)
    {
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = name,
            HeaderText = header,
            FillWeight = weight,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                ForeColor = UiTheme.Text,
                SelectionForeColor = UiTheme.Text
            }
        });
    }

    private static void AddAction(FlowLayoutPanel bar, string text, Color color, EventHandler handler)
    {
        var button = UiTheme.Button(text, color);
        button.Size = new Size(175, 41);
        button.Margin = new Padding(5, 0, 5, 0);
        button.Click += handler;
        bar.Controls.Add(button);
    }

    private void Reload()
    {
        _appointments = _repository.GetAppointments();
        ApplyFilter();
        ReloadPatients();
    }

    private void ApplyFilter()
    {
        _appointmentsGrid.Rows.Clear();
        if (_status.SelectedItem is not string status)
            return;
        var query = _search.Text.Trim();
        var shown = _appointments.Where(appointment =>
            (status == "Все" || appointment.Status == status) &&
            (query.Length == 0 ||
             appointment.TicketNumber.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
             appointment.Patient.FullName.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
             appointment.Doctor.FullName.Contains(query, StringComparison.CurrentCultureIgnoreCase)));

        foreach (var appointment in shown)
        {
            var index = _appointmentsGrid.Rows.Add(
                appointment.TicketNumber,
                appointment.Patient.FullName,
                appointment.Patient.BirthDate.ToString("dd.MM.yyyy"),
                appointment.Doctor.Specialty,
                appointment.Doctor.FullName,
                appointment.Doctor.Room,
                appointment.VisitDate.ToString("dd.MM.yyyy"),
                appointment.VisitTime.ToString(@"hh\:mm"),
                appointment.Status);
            _appointmentsGrid.Rows[index].Tag = appointment;
        }
        _appointmentsGrid.ClearSelection();
        _appointmentsGrid.CurrentCell = null;
        _count.Text = $"Записей: {_appointmentsGrid.Rows.Count} из {_appointments.Count}";
    }

    private void ReloadPatients()
    {
        _patientsGrid.Rows.Clear();
        foreach (var patient in _repository.GetPatients())
        {
            _patientsGrid.Rows.Add(patient.Id, patient.FullName,
                patient.BirthDate.ToString("dd.MM.yyyy"), patient.AppointmentCount);
        }
        _patientsGrid.ClearSelection();
        _patientsGrid.CurrentCell = null;
    }

    private Appointment? SelectedAppointment()
    {
        if (_appointmentsGrid.SelectedRows.Count == 0)
        {
            MessageBox.Show(this, "Выберите запись в таблице.", "Журнал записей",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return null;
        }
        return _appointmentsGrid.SelectedRows[0].Tag as Appointment;
    }

    private void ShowSelectedTicket(object? sender, EventArgs e)
    {
        var appointment = SelectedAppointment();
        if (appointment is null) return;
        using var form = new TicketForm(appointment);
        form.ShowDialog(this);
    }

    private void RescheduleSelected(object? sender, EventArgs e)
    {
        var appointment = SelectedAppointment();
        if (appointment is null) return;
        if (appointment.Status != "Активна")
        {
            MessageBox.Show(this, "Отменённую запись перенести нельзя.", "Перенос",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var form = new RescheduleForm(_service, appointment);
        if (form.ShowDialog(this) == DialogResult.OK)
            Reload();
    }

    private void CancelSelected(object? sender, EventArgs e)
    {
        var appointment = SelectedAppointment();
        if (appointment is null) return;
        if (appointment.Status != "Активна")
        {
            MessageBox.Show(this, "Эта запись уже отменена.", "Отмена записи",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (MessageBox.Show(this,
                $"Отменить запись {appointment.TicketNumber}? Время снова станет свободным.",
                "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;
        _repository.CancelAppointment(appointment.Id);
        Reload();
    }

    private void DeleteSelected(object? sender, EventArgs e)
    {
        var appointment = SelectedAppointment();
        if (appointment is null) return;
        if (MessageBox.Show(this,
                $"Удалить запись {appointment.TicketNumber} из журнала? Данные пациента останутся в базе.",
                "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;
        _repository.DeleteAppointment(appointment.Id);
        Reload();
    }
}
