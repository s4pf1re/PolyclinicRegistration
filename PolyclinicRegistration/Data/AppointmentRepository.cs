using System.Globalization;
using Microsoft.Data.Sqlite;
using PolyclinicRegistration.Models;

namespace PolyclinicRegistration.Data;

public class AppointmentRepository
{
    private readonly string _connectionString;

    public string DatabasePath { get; }

    public AppointmentRepository(string? databasePath = null)
    {
        var configuredPath = Environment.GetEnvironmentVariable("POLYCLINIC_DB_PATH");
        DatabasePath = databasePath ?? (string.IsNullOrWhiteSpace(configuredPath) ? null : configuredPath) ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PolyclinicRegistration", "clinic.db");
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();
    }

    public void Initialize()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Patients (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FullName TEXT NOT NULL,
                BirthDate TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS Appointments (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                PatientId INTEGER NOT NULL REFERENCES Patients(Id),
                DoctorId INTEGER NOT NULL,
                VisitDate TEXT NOT NULL,
                VisitTime TEXT NOT NULL,
                Status TEXT NOT NULL DEFAULT 'Active'
                    CHECK (Status IN ('Active', 'Cancelled'))
            );
            CREATE UNIQUE INDEX IF NOT EXISTS UX_ActiveDoctorSlot
                ON Appointments(DoctorId, VisitDate, VisitTime)
                WHERE Status = 'Active';
            """;
        command.ExecuteNonQuery();
    }

    public List<TimeSpan> GetOccupiedSlots(int doctorId, DateTime visitDate, int? exceptAppointmentId = null)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT VisitTime FROM Appointments
            WHERE DoctorId = $doctor AND VisitDate = $date
              AND Status = 'Active' AND Id <> $except;
            """;
        command.Parameters.AddWithValue("$doctor", doctorId);
        command.Parameters.AddWithValue("$date", SqlDate(visitDate));
        command.Parameters.AddWithValue("$except", exceptAppointmentId ?? -1);
        using var reader = command.ExecuteReader();
        var result = new List<TimeSpan>();
        while (reader.Read())
            result.Add(ParseTime(reader.GetString(0)));
        return result;
    }

    public Appointment CreateAppointment(Patient patient, Doctor doctor, DateTime visitDate, TimeSpan visitTime)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        using var patientCommand = connection.CreateCommand();
        patientCommand.Transaction = transaction;
        patientCommand.CommandText = """
            INSERT INTO Patients(FullName, BirthDate)
            VALUES ($name, $birth);
            SELECT last_insert_rowid();
            """;
        patientCommand.Parameters.AddWithValue("$name", patient.FullName);
        patientCommand.Parameters.AddWithValue("$birth", SqlDate(patient.BirthDate));
        patient.Id = Convert.ToInt32(patientCommand.ExecuteScalar());

        using var appointmentCommand = connection.CreateCommand();
        appointmentCommand.Transaction = transaction;
        appointmentCommand.CommandText = """
            INSERT INTO Appointments(PatientId, DoctorId, VisitDate, VisitTime, Status)
            VALUES ($patient, $doctor, $date, $time, 'Active');
            SELECT last_insert_rowid();
            """;
        appointmentCommand.Parameters.AddWithValue("$patient", patient.Id);
        appointmentCommand.Parameters.AddWithValue("$doctor", doctor.Id);
        appointmentCommand.Parameters.AddWithValue("$date", SqlDate(visitDate));
        appointmentCommand.Parameters.AddWithValue("$time", SqlTime(visitTime));
        var id = Convert.ToInt32(appointmentCommand.ExecuteScalar());
        transaction.Commit();

        return new Appointment
        {
            Id = id,
            Patient = patient,
            Doctor = doctor,
            VisitDate = visitDate.Date,
            VisitTime = visitTime
        };
    }

    public List<Appointment> GetAppointments()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT a.Id, p.Id, p.FullName, p.BirthDate,
                   a.DoctorId, a.VisitDate, a.VisitTime, a.Status
            FROM Appointments AS a
            JOIN Patients AS p ON p.Id = a.PatientId
            ORDER BY a.VisitDate DESC, a.VisitTime DESC, a.Id DESC;
            """;
        using var reader = command.ExecuteReader();
        var result = new List<Appointment>();
        while (reader.Read())
        {
            result.Add(new Appointment
            {
                Id = reader.GetInt32(0),
                Patient = new Patient
                {
                    Id = reader.GetInt32(1),
                    FullName = reader.GetString(2),
                    BirthDate = ParseDate(reader.GetString(3))
                },
                Doctor = DoctorCatalog.GetById(reader.GetInt32(4)),
                VisitDate = ParseDate(reader.GetString(5)),
                VisitTime = ParseTime(reader.GetString(6)),
                Status = reader.GetString(7) == "Active" ? "Активна" : "Отменена"
            });
        }
        return result;
    }

    public List<Patient> GetPatients()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT p.Id, p.FullName, p.BirthDate, COUNT(a.Id)
            FROM Patients AS p
            LEFT JOIN Appointments AS a ON a.PatientId = p.Id
            GROUP BY p.Id
            ORDER BY p.Id DESC;
            """;
        using var reader = command.ExecuteReader();
        var result = new List<Patient>();
        while (reader.Read())
        {
            result.Add(new Patient
            {
                Id = reader.GetInt32(0),
                FullName = reader.GetString(1),
                BirthDate = ParseDate(reader.GetString(2)),
                AppointmentCount = reader.GetInt32(3)
            });
        }
        return result;
    }

    public bool CancelAppointment(int id)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Appointments SET Status = 'Cancelled'
            WHERE Id = $id AND Status = 'Active';
            """;
        command.Parameters.AddWithValue("$id", id);
        return command.ExecuteNonQuery() == 1;
    }

    public bool RescheduleAppointment(int id, DateTime visitDate, TimeSpan visitTime)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Appointments SET VisitDate = $date, VisitTime = $time
            WHERE Id = $id AND Status = 'Active';
            """;
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$date", SqlDate(visitDate));
        command.Parameters.AddWithValue("$time", SqlTime(visitTime));
        return command.ExecuteNonQuery() == 1;
    }

    public bool DeleteAppointment(int id)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Appointments WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        return command.ExecuteNonQuery() == 1;
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON;";
        command.ExecuteNonQuery();
        return connection;
    }

    private static string SqlDate(DateTime date) =>
        date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string SqlTime(TimeSpan time) =>
        time.ToString(@"hh\:mm", CultureInfo.InvariantCulture);

    private static DateTime ParseDate(string value) =>
        DateTime.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static TimeSpan ParseTime(string value) =>
        TimeSpan.ParseExact(value, @"hh\:mm", CultureInfo.InvariantCulture);
}
