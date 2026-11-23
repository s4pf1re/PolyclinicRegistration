using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using PolyclinicRegistration.Data;
using PolyclinicRegistration.Models;

namespace PolyclinicRegistration.Services;

public class AppointmentService
{
    private readonly AppointmentRepository _repository;

    public AppointmentService(AppointmentRepository repository)
    {
        _repository = repository;
    }

    public static DateTime FirstWorkingDate()
    {
        var date = DateTime.Today;
        if (DateTime.Now.TimeOfDay > new TimeSpan(16, 30, 0))
            date = date.AddDays(1);
        while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            date = date.AddDays(1);
        return date;
    }

    public List<TimeSpan> GetAvailableTimes(Doctor doctor, DateTime date, int? exceptAppointmentId = null)
    {
        if (date.Date < DateTime.Today || date.Date > DateTime.Today.AddYears(1) ||
            date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return new List<TimeSpan>();

        var occupied = _repository.GetOccupiedSlots(doctor.Id, date, exceptAppointmentId).ToHashSet();
        var result = new List<TimeSpan>();
        for (var time = new TimeSpan(9, 0, 0);
             time <= new TimeSpan(16, 30, 0);
             time = time.Add(TimeSpan.FromMinutes(30)))
        {
            if (!occupied.Contains(time) && date.Date.Add(time) > DateTime.Now)
                result.Add(time);
        }
        return result;
    }

    public Appointment Register(string fullName, DateTime birthDate, Doctor doctor,
        DateTime visitDate, TimeSpan visitTime)
    {
        var cleanName = NormalizeName(fullName);
        if (birthDate.Date > DateTime.Today || birthDate.Year < 1900)
            throw new ArgumentException("Укажите корректную дату рождения.");
        if (!DoctorCatalog.All.Any(item => item.Id == doctor.Id))
            throw new ArgumentException("Выберите врача из списка.");
        if (!GetAvailableTimes(doctor, visitDate).Contains(visitTime))
            throw new InvalidOperationException("Выбранное время уже занято или недоступно. Выберите другое время.");

        try
        {
            var patient = new Patient { FullName = cleanName, BirthDate = birthDate.Date };
            return _repository.CreateAppointment(patient, doctor, visitDate, visitTime);
        }
        catch (SqliteException error) when (error.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException("Это время только что заняли. Обновите список и выберите другой слот.");
        }
    }

    public void Reschedule(Appointment appointment, DateTime date, TimeSpan time)
    {
        if (appointment.Status != "Активна")
            throw new InvalidOperationException("Перенести можно только активную запись.");
        if (!GetAvailableTimes(appointment.Doctor, date, appointment.Id).Contains(time))
            throw new InvalidOperationException("Новое время уже занято или недоступно.");

        try
        {
            if (!_repository.RescheduleAppointment(appointment.Id, date, time))
                throw new InvalidOperationException("Запись не найдена или уже отменена.");
        }
        catch (SqliteException error) when (error.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException("Это время только что заняли. Выберите другой слот.");
        }
    }

    private static string NormalizeName(string value)
    {
        var parts = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3 || parts.Any(part =>
                !Regex.IsMatch(part, @"^[\p{L}]{2,}(?:-[\p{L}]{2,})?$")))
            throw new ArgumentException("Введите фамилию, имя и отчество буквами через пробел.");
        return string.Join(' ', parts);
    }
}
