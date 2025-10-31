using MedSys.Orm;

namespace MedSys.Domain;

public enum VisitType { GP, BLOOD, XRAY, CT, MR, ULTRA, EKG, ECHO, EYE, DERM, DENT, MAMMO, EEG }

[Table("patients")]
public class Patient
{
    [Key] 
    [Column(Name = "id", TypeName = "INT", Nullable = false)]
    public int Id { get; set; }

    [Column(Name = "fname", TypeName = "VARCHAR", Length = 80, Nullable = false)]
    public string Fname { get; set; } = default!;

    [Column(Name = "lname", TypeName = "VARCHAR", Length = 80, Nullable = false)]
    public string Lname { get; set; } = default!

    [Column(Name = "birth_date", TypeName = "TIMESTAMP", Nullable = true)]
    public DateTime? BirthDate { get; set; }

    public List<Visit> Visits { get; set; } = new();
    public List<Prescription> Prescriptions { get; set; } = new();
}

[Table("visit")]
public class Visit
{
    [Key]
    [Column(Name = "id", TypeName = "INT", Nullable = false)]
    public int Id { get; set; }

    [Column(Name = "patientid", TypeName = "INT", Nullable = false)]
    [ForeignKey("patients", "id")]
    public int PatientId { get; set; }

    [Column(Name = "type", TypeName = "INT", Nullable = false)]
    public VisitType Type { get; set; }

    [Column(Name = "date", TypeName = "TIMESTAMP", Nullable = false, DefaultSql = "NOW()")]
    public DateTime Date { get; set; }

    public Patient? Patient { get; set; }
}

[Table("medicine")]
public class Medicine
{
    [Key]
    [Column(Name = "id", TypeName = "INT", Nullable = false)]
    public int Id { get; set; }

    [Column(Name = "name", TypeName = "VARCHAR", Length = 150, Nullable = false, Unique = true)]
    public string Name { get; set; } = default!;

    [Column(Name = "manufacturer", TypeName = "VARCHAR", Length = 150, Nullable = true)]
    public string? Manufacturer { get; set; }
}

[Table("prescription")]
public class Prescription
{
    [Key]
    [Column(Name = "id", TypeName = "INT", Nullable = false)]
    public int Id { get; set; }

    [Column(Name = "patientid", TypeName = "INT", Nullable = false)]
    [ForeignKey("patients", "id")]
    public int PatientId { get; set; }

    [Column(Name = "medicineid", TypeName = "INT", Nullable = false)]
    [ForeignKey("medicine", "id")]
    public int MedicineId { get; set; }

    [Column(Name = "dosage", TypeName = "VARCHAR", Length = 120, Nullable = false)]
    public string Dosage { get; set; } = default!;

    public Patient? Patient { get; set; }
    public Medicine? Medicine { get; set; }
}
