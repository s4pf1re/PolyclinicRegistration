using Microsoft.Data.Sqlite;
using PolyclinicRegistration.Data;
using PolyclinicRegistration.Models;
using PolyclinicRegistration.Services;
using PolyclinicRegistration.Views;

var databasePath = Path.Combine(Path.GetTempPath(),
    "PolyclinicRegistrationChecks", Guid.NewGuid().ToString("N"), "clinic.db");
var repository = new AppointmentRepository(databasePath);
repository.Initialize();
var service = new AppointmentService(repository);
var doctor = DoctorCatalog.GetById(1);
var otherDoctor = DoctorCatalog.GetById(2);
var date = DateTime.Today.AddDays(1);
while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
    date = date.AddDays(1);
var time = new TimeSpan(9, 0, 0);

Check(service.GetAvailableTimes(doctor, date).Contains(time), "Свободный слот");
Check(service.GetAvailableTimes(doctor, DateTime.Today.AddDays(-1)).Count == 0,
    "Запрет прошлой даты");
Expect<ArgumentException>(() => service.Register("Иванов Иван", new DateTime(1990, 2, 15),
    doctor, date, time), "Проверка ФИО");

var first = service.Register("Иванов Иван Иванович", new DateTime(1990, 2, 15),
    doctor, date, time);
Check(first.TicketNumber == "Т-000001", "Номер талона");
Check(!service.GetAvailableTimes(doctor, date).Contains(time), "Занятый слот недоступен");
Expect<InvalidOperationException>(() => service.Register("Петров Пётр Петрович",
    new DateTime(1992, 6, 10), doctor, date, time), "Запрет двойной записи");
Expect<SqliteException>(() => repository.CreateAppointment(new Patient
{
    FullName = "Сидорова Анна Ивановна", BirthDate = new DateTime(1989, 3, 4)
}, doctor, date, time), "Уникальность слота в базе");
Check(repository.GetPatients().Count == 1, "Откат пациента при конфликте");

var second = service.Register("Петров Пётр Петрович", new DateTime(1992, 6, 10),
    otherDoctor, date, time);
Check(second.Id == 2, "Параллельная запись к другому врачу");
Check(repository.CancelAppointment(first.Id), "Отмена записи");
Check(service.GetAvailableTimes(doctor, date).Contains(time), "Освобождение слота");

var third = service.Register("Сидорова Анна Ивановна", new DateTime(1989, 3, 4),
    doctor, date, time);
service.Reschedule(third, date, time.Add(TimeSpan.FromMinutes(30)));
Check(service.GetAvailableTimes(doctor, date).Contains(time), "Освобождение при переносе");
Check(!service.GetAvailableTimes(doctor, date).Contains(time.Add(TimeSpan.FromMinutes(30))),
    "Занятие нового времени");

var reopened = new AppointmentRepository(databasePath);
reopened.Initialize();
Check(reopened.GetAppointments().Count == 3, "Сохранение после повторного открытия");
Check(reopened.GetPatients().Count == 3, "Журнал пациентов");
Check(TicketForm.BuildTicketText(third).Contains(third.Patient.FullName),
    "Состав талона");
Check(reopened.DeleteAppointment(second.Id), "Удаление записи");
Check(reopened.GetAppointments().Count == 2 && reopened.GetPatients().Count == 3,
    "Пациент сохраняется после удаления записи");

Console.WriteLine("ПРОВЕРКИ ПРОЙДЕНЫ: 18 сценариев. База: " + databasePath);

static void Check(bool condition, string label)
{
    if (!condition) throw new Exception("Ошибка проверки: " + label);
    Console.WriteLine("OK: " + label);
}

static void Expect<T>(Action action, string label) where T : Exception
{
    try
    {
        action();
    }
    catch (T)
    {
        Console.WriteLine("OK: " + label);
        return;
    }
    throw new Exception("Ошибка проверки: " + label);
}
