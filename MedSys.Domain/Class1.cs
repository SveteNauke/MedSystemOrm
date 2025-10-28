using System.Linq.Expressions;

namespace MedSys.Domain;

public enum VisitType { GP, BLOOD, XRAY, CT, MR, ULTRA, EKG, ECHO, EYE, DERM, DENT, MAMMO, EEG }

public class Patient
{
    public int Id { get; set; }
    public string Fname { get; set; } = default!;
    public int Lname { get; set; } = default!;
    public DateTime? BirthDate { get; set; }


    public List<Visit> Visits { get; set; } = new();
    public List<Prescription> Prescriptions { get; set; } = new();

}

public class Visit
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public VisitType Type { get; set; }
    public DateTime Date { get; set; }

    public Patient? Patient { get; set; }
}

public class Medicine
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Manufacturer { get; set; }
}

public class Prescription
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int MedicineId { get; set; }
    public string Dosage { get; set; } = default!;

    public Patient? Patient { get; set; }
    public Medicine? Medicine { get; set; }
}
