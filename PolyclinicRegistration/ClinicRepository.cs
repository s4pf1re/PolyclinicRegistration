using Microsoft.Data.Sqlite;
using System.Globalization;

namespace PolyclinicRegistration;

public sealed record Appointment(
    long Id, string TicketNumber, string FullName, DateOnly BirthDate,
    string Specialty, string DoctorId, string DoctorName, string Room,
    DateOnly VisitDate, TimeOnly VisitTime,
    string Status, DateTime CreatedAt);

public sealed record Doctor(string Id, string Specialty, string FullName, string Room);

public sealed class SlotUnavailableException : Exception
{
    public SlotUnavailableException() : base("Это время уже занято. Выберите другой талон.") { }
}

public sealed class ClinicRepository
{
    private readonly string _connectionString;

    public string DatabasePath { get; }

    public ClinicRepository(string? directory = null)
    {
        directory ??= Environment.GetEnvironmentVariable("CLINIC_TICKETS_DATA_DIR");
        directory ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClinicTickets");
        Directory.CreateDirectory(directory);
        DatabasePath = Path.Combine(directory, "clinic.db");
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false,
            DefaultTimeout = 10
        }.ToString();
    }

    public void Initialize()
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS appointments (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                full_name TEXT NOT NULL,
                birth_date TEXT NOT NULL,
                specialty TEXT NOT NULL,
                doctor_id TEXT NOT NULL DEFAULT '',
                doctor_name TEXT NOT NULL DEFAULT '',
                room TEXT NOT NULL DEFAULT '',
                visit_date TEXT NOT NULL,
                visit_time TEXT NOT NULL,
                status TEXT NOT NULL DEFAULT 'active' CHECK (status IN ('active', 'cancelled')),
                created_at TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_appointments_date ON appointments(visit_date);
            """;
        command.ExecuteNonQuery();

        command.CommandText = "PRAGMA table_info(appointments)";
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var reader = command.ExecuteReader())
            while (reader.Read()) columns.Add(reader.GetString(1));
        foreach (var column in new[] { "doctor_id", "doctor_name", "room" })
        {
            if (columns.Contains(column)) continue;
            command.CommandText = $"ALTER TABLE appointments ADD COLUMN {column} TEXT NOT NULL DEFAULT ''";
            command.ExecuteNonQuery();
        }
        foreach (var specialty in ClinicSchedule.Specialties)
        {
            var doctor = ClinicSchedule.DoctorsFor(specialty)[0];
            command.CommandText = """
                UPDATE appointments SET doctor_id = $id, doctor_name = $name, room = $room
                WHERE specialty = $specialty AND doctor_id = ''
                """;
            command.Parameters.Clear();
            command.Parameters.AddWithValue("$id", doctor.Id);
            command.Parameters.AddWithValue("$name", doctor.FullName);
            command.Parameters.AddWithValue("$room", doctor.Room);
            command.Parameters.AddWithValue("$specialty", specialty);
            command.ExecuteNonQuery();
        }
        command.Parameters.Clear();
        command.CommandText = """
            DROP INDEX IF EXISTS ux_active_slot;
            CREATE UNIQUE INDEX IF NOT EXISTS ux_active_doctor_slot
                ON appointments(doctor_id, visit_date, visit_time)
                WHERE status = 'active';
            """;
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    public HashSet<TimeOnly> OccupiedSlots(string doctorId, DateOnly date)
    {
        if (ClinicSchedule.FindDoctor(doctorId) is null)
            throw new ArgumentException("Выберите врача из списка.");
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT visit_time FROM appointments
            WHERE doctor_id = $doctorId AND visit_date = $date AND status = 'active'
            """;
        command.Parameters.AddWithValue("$doctorId", doctorId);
        command.Parameters.AddWithValue("$date", date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        using var reader = command.ExecuteReader();
        var result = new HashSet<TimeOnly>();
        while (reader.Read()) result.Add(TimeOnly.ParseExact(reader.GetString(0), "HH:mm", CultureInfo.InvariantCulture));
        return result;
    }

    public Appointment Book(string fullName, DateOnly birthDate, string doctorId, DateOnly date, TimeOnly time)
    {
        fullName = string.Join(' ', fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (fullName.Length < 5 || fullName.Length > 120 || fullName.Split(' ').Length < 2)
            throw new ArgumentException("Введите фамилию и имя полностью.");
        if (birthDate > DateOnly.FromDateTime(DateTime.Today) || birthDate < new DateOnly(1900, 1, 1))
            throw new ArgumentException("Проверьте дату рождения.");
        var doctor = ClinicSchedule.FindDoctor(doctorId);
        if (doctor is null)
            throw new ArgumentException("Выберите врача из списка.");
        if (!ClinicSchedule.IsValidSlot(date, time))
            throw new ArgumentException("Выберите доступные дату и время приёма.");

        try
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO appointments(full_name, birth_date, specialty, doctor_id,
                    doctor_name, room, visit_date, visit_time, created_at)
                VALUES ($name, $birth, $specialty, $doctorId,
                    $doctorName, $room, $date, $time, $created);
                SELECT last_insert_rowid();
                """;
            command.Parameters.AddWithValue("$name", fullName);
            command.Parameters.AddWithValue("$birth", birthDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$specialty", doctor.Specialty);
            command.Parameters.AddWithValue("$doctorId", doctor.Id);
            command.Parameters.AddWithValue("$doctorName", doctor.FullName);
            command.Parameters.AddWithValue("$room", doctor.Room);
            command.Parameters.AddWithValue("$date", date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$time", time.ToString("HH:mm", CultureInfo.InvariantCulture));
            var created = DateTime.Now;
            command.Parameters.AddWithValue("$created", created.ToString("O", CultureInfo.InvariantCulture));
            var id = (long)command.ExecuteScalar()!;
            return new Appointment(id, Ticket(id), fullName, birthDate,
                doctor.Specialty, doctor.Id, doctor.FullName, doctor.Room,
                date, time, "active", created);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new SlotUnavailableException();
        }
    }

    public IReadOnlyList<Appointment> GetAppointments(DateOnly? date = null, string? search = null)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, full_name, birth_date, specialty, doctor_id, doctor_name, room,
                   visit_date, visit_time, status, created_at
            FROM appointments
            WHERE ($date IS NULL OR visit_date = $date)
              AND ($search = '' OR lower(full_name) LIKE '%' || lower($search) || '%'
                   OR ('Т-' || printf('%06d', id)) LIKE '%' || $search || '%')
            ORDER BY visit_date DESC, visit_time, specialty, id
            """;
        command.Parameters.AddWithValue("$date", date is null ? DBNull.Value : date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$search", search?.Trim() ?? "");
        using var reader = command.ExecuteReader();
        var items = new List<Appointment>();
        while (reader.Read())
        {
            var id = reader.GetInt64(0);
            items.Add(new Appointment(id, Ticket(id), reader.GetString(1),
                DateOnly.ParseExact(reader.GetString(2), "yyyy-MM-dd", CultureInfo.InvariantCulture),
                reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetString(6),
                DateOnly.ParseExact(reader.GetString(7), "yyyy-MM-dd", CultureInfo.InvariantCulture),
                TimeOnly.ParseExact(reader.GetString(8), "HH:mm", CultureInfo.InvariantCulture),
                reader.GetString(9), DateTime.Parse(reader.GetString(10), CultureInfo.InvariantCulture)));
        }
        return items;
    }

    public bool Cancel(long id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE appointments SET status = 'cancelled' WHERE id = $id AND status = 'active'";
        command.Parameters.AddWithValue("$id", id);
        return command.ExecuteNonQuery() == 1;
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static string Ticket(long id) => $"Т-{id:000000}";
}

public static class ClinicSchedule
{
    public static readonly string[] Specialties =
    [
        "Терапевт", "Педиатр", "Стоматолог", "Травматолог",
        "Невролог", "Кардиолог", "Офтальмолог", "Отоларинголог"
    ];

    public static readonly Doctor[] Doctors =
    [
        new("ther-1", "Терапевт", "Смирнова Елена Викторовна", "101"),
        new("ther-2", "Терапевт", "Кузнецов Андрей Павлович", "102"),
        new("ped-1", "Педиатр", "Орлова Мария Сергеевна", "201"),
        new("ped-2", "Педиатр", "Волков Дмитрий Олегович", "202"),
        new("dent-1", "Стоматолог", "Соколова Наталья Игоревна", "301"),
        new("dent-2", "Стоматолог", "Морозов Алексей Петрович", "302"),
        new("trauma-1", "Травматолог", "Фёдоров Илья Андреевич", "103"),
        new("trauma-2", "Травматолог", "Павлова Ольга Николаевна", "104"),
        new("neuro-1", "Невролог", "Михайлова Анна Романовна", "203"),
        new("neuro-2", "Невролог", "Белов Сергей Викторович", "204"),
        new("cardio-1", "Кардиолог", "Зайцева Ирина Владимировна", "205"),
        new("cardio-2", "Кардиолог", "Никитин Павел Сергеевич", "206"),
        new("eye-1", "Офтальмолог", "Алексеева Татьяна Михайловна", "207"),
        new("eye-2", "Офтальмолог", "Егоров Максим Юрьевич", "208"),
        new("ent-1", "Отоларинголог", "Григорьева Светлана Олеговна", "209"),
        new("ent-2", "Отоларинголог", "Романов Денис Александрович", "210")
    ];

    public static Doctor[] DoctorsFor(string specialty) =>
        Doctors.Where(d => d.Specialty == specialty).ToArray();

    public static Doctor? FindDoctor(string id) => Doctors.FirstOrDefault(d => d.Id == id);

    public static IEnumerable<TimeOnly> Times()
    {
        for (var minutes = 9 * 60; minutes < 17 * 60; minutes += 30)
            yield return new TimeOnly(minutes / 60, minutes % 60);
    }

    public static bool IsValidSlot(DateOnly date, TimeOnly time)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (date < today || date > today.AddDays(30)) return false;
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) return false;
        if (!Times().Contains(time)) return false;
        return date != today || time > TimeOnly.FromDateTime(DateTime.Now);
    }
}
