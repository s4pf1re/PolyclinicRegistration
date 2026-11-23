namespace PolyclinicRegistration.Models;

public class Doctor
{
    public int Id { get; }
    public string Specialty { get; }
    public string FullName { get; }
    public string Room { get; }

    public Doctor(int id, string specialty, string fullName, string room)
    {
        Id = id;
        Specialty = specialty;
        FullName = fullName;
        Room = room;
    }

    public override string ToString() => $"{FullName} · каб. {Room}";
}
