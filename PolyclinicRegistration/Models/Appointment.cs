namespace PolyclinicRegistration.Models;

public class Appointment
{
    public int Id { get; set; }
    public string TicketNumber => $"Т-{Id:000000}";
    public Patient Patient { get; set; } = new();
    public Doctor Doctor { get; set; } = null!;
    public DateTime VisitDate { get; set; }
    public TimeSpan VisitTime { get; set; }
    public string Status { get; set; } = "Активна";
}
