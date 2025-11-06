using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MedSys.Domain;
using MedSys.Orm;

Console.OutputEncoding = Encoding.UTF8;

try
{
    var connString = Environment.GetEnvironmentVariable("MEDSYS_CONN")
                     ?? throw new InvalidOperationException("MEDSYS_CONN nije postavljen u environment varijablama.");

    await using var session = new DbSession(connString);
    await session.OpenAsync();

    Console.WriteLine("Uspješno spojeno na bazu.\n");

    var patientRepo      = new Repository<Patient>(session);
    var visitRepo        = new Repository<Visit>(session);
    var medicineRepo     = new Repository<Medicine>(session);
    var prescriptionRepo = new Repository<Prescription>(session);

    while (true)
    {
        PrintMenu();
        Console.Write("\nOdabir: ");
        var choice = Console.ReadLine();
        Console.WriteLine();

        switch (choice)
        {
            case "1":
                await ListPatientsAsync(patientRepo);
                break;
            case "2":
                await CreatePatientAsync(patientRepo);
                break;
            case "3":
                await ListPatientVisitsAsync(patientRepo, visitRepo);
                break;
            case "4":
                await CreateVisitAsync(patientRepo, visitRepo);
                break;
            case "5":
                await ListPatientPrescriptionsAsync(patientRepo, prescriptionRepo, medicineRepo);
                break;
            case "6":
                await CreatePrescriptionAsync(patientRepo, prescriptionRepo, medicineRepo);
                break;
            case "7":
                await ListMedicinesAsync(medicineRepo);
                break;
            case "8":
                await CreateMedicineAsync(medicineRepo);
                break;
            case "0":
                Console.WriteLine("Izlaz iz aplikacije...");
                return;
            default:
                Console.WriteLine("Nepoznata opcija, pokušaj ponovno.");
                break;
        }

        Console.WriteLine("\nPritisni Enter za povratak u meni...");
        Console.ReadLine();
        Console.Clear();
    }
}
catch (Exception ex)
{
    Console.WriteLine("Greška pri izvođenju aplikacije:");
    Console.WriteLine(ex.Message);
}

static void PrintMenu()
{
    Console.WriteLine("======================================");
    Console.WriteLine("        MedSys – Glavni meni");
    Console.WriteLine("======================================");
    Console.WriteLine("1) Prikaži sve pacijente");
    Console.WriteLine("2) Dodaj novog pacijenta");
    Console.WriteLine("3) Prikaži posjete pacijenta");
    Console.WriteLine("4) Dodaj posjet pacijentu");
    Console.WriteLine("5) Prikaži recepte pacijenta");
    Console.WriteLine("6) Dodaj recept pacijentu");
    Console.WriteLine("7) Prikaži sve lijekove");
    Console.WriteLine("8) Dodaj novi lijek");
    Console.WriteLine("0) Izlaz");
}

/// =========================
/// PACIJENTI
/// =========================

static async Task ListPatientsAsync(Repository<Patient> repo)
{
    Console.WriteLine("=== Popis pacijenata ===");

    var patients = await repo.GetAllAsync();

    if (patients.Count == 0)
    {
        Console.WriteLine("Nema pacijenata u bazi.");
        return;
    }

    foreach (var p in patients)
    {
        Console.WriteLine(
            $"{p.Id}: {p.Fname} {p.Lname} | Rođen: {p.BirthDate:yyyy-MM-dd} | Email: {p.Email} | Tel: {p.Phone} | Kreiran: {p.CreatedAt:yyyy-MM-dd HH:mm}");
    }
}

static async Task CreatePatientAsync(Repository<Patient> repo)
{
    Console.WriteLine("=== Dodavanje pacijenta ===");

    Console.Write("Ime: ");
    var fname = Console.ReadLine() ?? string.Empty;

    Console.Write("Prezime: ");
    var lname = Console.ReadLine() ?? string.Empty;

    Console.Write("Datum rođenja (yyyy-MM-dd) [prazno = nepoznato]: ");
    var birthStr = Console.ReadLine();
    DateTime? birthDate = null;
    if (!string.IsNullOrWhiteSpace(birthStr) && DateTime.TryParse(birthStr, out var bd))
        birthDate = bd;

    Console.Write("Email [opcionalno]: ");
    var email = Console.ReadLine();

    Console.Write("Telefon [opcionalno]: ");
    var phone = Console.ReadLine();

    var patient = new Patient
    {
        Fname     = fname,
        Lname     = lname,
        BirthDate = birthDate,
        Email     = string.IsNullOrWhiteSpace(email) ? null : email,
        Phone     = string.IsNullOrWhiteSpace(phone) ? null : phone,
    };

    await repo.InsertAsync(patient);
    Console.WriteLine($"Pacijent je uspješno dodan. Novi ID = {patient.Id}");
}

/// =========================
/// POSJETE
/// =========================

static async Task ListPatientVisitsAsync(
    Repository<Patient> patientRepo,
    Repository<Visit> visitRepo)
{
    Console.WriteLine("=== Posjete pacijenta ===");
    Console.Write("Unesi ID pacijenta: ");

    if (!int.TryParse(Console.ReadLine(), out var pid))
    {
        Console.WriteLine("Neispravan ID.");
        return;
    }

    var patient = await patientRepo.FindAsync(pid);
    if (patient is null)
    {
        Console.WriteLine("Pacijent nije pronađen.");
        return;
    }

    Console.WriteLine($"Pacijent: {patient.Fname} {patient.Lname}");

    var allVisits = await visitRepo.GetAllAsync();
    var visits = allVisits.Where(v => v.PatientId == pid).ToList();

    if (visits.Count == 0)
    {
        Console.WriteLine("Nema evidentiranih posjeta.");
        return;
    }

    foreach (var v in visits)
    {
        Console.WriteLine(
            $"{v.Id}: {v.Date:yyyy-MM-dd HH:mm} | {v.Type} | Cijena: {v.Price:0.00} € | Trajanje: {v.DurationMinutes} min");
    }
}

static async Task CreateVisitAsync(
    Repository<Patient> patientRepo,
    Repository<Visit> visitRepo)
{
    Console.WriteLine("=== Dodavanje posjeta ===");
    Console.Write("Unesi ID pacijenta: ");

    if (!int.TryParse(Console.ReadLine(), out var pid))
    {
        Console.WriteLine("Neispravan ID.");
        return;
    }

    var patient = await patientRepo.FindAsync(pid);
    if (patient is null)
    {
        Console.WriteLine("Pacijent nije pronađen.");
        return;
    }

    Console.WriteLine($"Pacijent: {patient.Fname} {patient.Lname}");

    Console.Write("Tip posjete (GP/BLOOD/XRAY/CT/MR/ULTRA/EKG/ECHO/EYE/DERM/DENT/MAMMO/EEG): ");
    var typeStr = Console.ReadLine();

    VisitType type;
    if (!Enum.TryParse(typeStr, ignoreCase: true, result: out type))
    {
        Console.WriteLine("Nepoznat tip posjete, koristim GP.");
        type = VisitType.GP;
    }

    Console.Write("Datum i vrijeme posjete (yyyy-MM-dd HH:mm) [prazno = sada]: ");
    var dateStr = Console.ReadLine();
    DateTime date;
    if (!string.IsNullOrWhiteSpace(dateStr) && DateTime.TryParse(dateStr, out var dt))
        date = dt;
    else
        date = DateTime.UtcNow;

    Console.Write("Cijena (decimal, npr. 50.00): ");
    var priceStr = Console.ReadLine();
    if (!decimal.TryParse(priceStr, out var price))
        price = 0m;

    Console.Write("Trajanje u minutama (npr. 30): ");
    var durationStr = Console.ReadLine();
    if (!double.TryParse(durationStr, out var duration))
        duration = 0;

    var visit = new Visit
    {
        PatientId        = pid,
        Type             = type,
        Date             = date,
        Price            = price,
        DurationMinutes  = duration
    };

    await visitRepo.InsertAsync(visit);
    Console.WriteLine($"Posjet je uspješno dodan. ID posjeta = {visit.Id}");
}

/// =========================
/// LIJEKOVI
/// =========================

static async Task ListMedicinesAsync(Repository<Medicine> medicineRepo)
{
    Console.WriteLine("=== Popis lijekova ===");

    var meds = await medicineRepo.GetAllAsync();

    if (meds.Count == 0)
    {
        Console.WriteLine("Nema lijekova u bazi.");
        return;
    }

    foreach (var m in meds)
    {
        Console.WriteLine(
            $"{m.Id}: {m.Name} | Proizvođač: {m.Manufacturer} | Jačina: {(m.StrengthMg.HasValue ? $"{m.StrengthMg} mg" : "n/a")}");
    }
}

static async Task CreateMedicineAsync(Repository<Medicine> medicineRepo)
{
    Console.WriteLine("=== Dodavanje lijeka ===");

    Console.Write("Naziv lijeka: ");
    var name = Console.ReadLine() ?? string.Empty;

    Console.Write("Proizvođač [opcionalno]: ");
    var manufacturer = Console.ReadLine();

    Console.Write("Jačina u mg [opcionalno, npr. 500]: ");
    var strengthStr = Console.ReadLine();
    double? strength = null;
    if (!string.IsNullOrWhiteSpace(strengthStr) && double.TryParse(strengthStr, out var s))
        strength = s;

    var med = new Medicine
    {
        Name         = name,
        Manufacturer = string.IsNullOrWhiteSpace(manufacturer) ? null : manufacturer,
        StrengthMg   = strength
    };

    await medicineRepo.InsertAsync(med);
    Console.WriteLine($"Lijek je uspješno dodan. ID lijeka = {med.Id}");
}

/// =========================
/// RECEPTI
/// =========================

static async Task ListPatientPrescriptionsAsync(
    Repository<Patient> patientRepo,
    Repository<Prescription> prescriptionRepo,
    Repository<Medicine> medicineRepo)
{
    Console.WriteLine("=== Recepti pacijenta ===");
    Console.Write("Unesi ID pacijenta: ");

    if (!int.TryParse(Console.ReadLine(), out var pid))
    {
        Console.WriteLine("Neispravan ID.");
        return;
    }

    var patient = await patientRepo.FindAsync(pid);
    if (patient is null)
    {
        Console.WriteLine("Pacijent nije pronađen.");
        return;
    }

    Console.WriteLine($"Pacijent: {patient.Fname} {patient.Lname}");

    var prescriptions = (await prescriptionRepo.GetAllAsync())
        .Where(r => r.PatientId == pid)
        .ToList();

    if (prescriptions.Count == 0)
    {
        Console.WriteLine("Nema recepata za ovog pacijenta.");
        return;
    }

    var meds = await medicineRepo.GetAllAsync();
    var medDict = meds.ToDictionary(m => m.Id);

    foreach (var r in prescriptions)
    {
        medDict.TryGetValue(r.MedicineId, out var med);
        var medName = med?.Name ?? $"[ID {r.MedicineId}]";

        Console.WriteLine(
            $"{r.Id}: {r.IssuedAt:yyyy-MM-dd} | {medName} | Doza: {r.Dosage} {r.Unit} | Aktivno: {(r.IsActive ? "DA" : "NE")}");
    }
}

static async Task CreatePrescriptionAsync(
    Repository<Patient> patientRepo,
    Repository<Prescription> prescriptionRepo,
    Repository<Medicine> medicineRepo)
{
    Console.WriteLine("=== Dodavanje recepta ===");
    Console.Write("Unesi ID pacijenta: ");

    if (!int.TryParse(Console.ReadLine(), out var pid))
    {
        Console.WriteLine("Neispravan ID.");
        return;
    }

    var patient = await patientRepo.FindAsync(pid);
    if (patient is null)
    {
        Console.WriteLine("Pacijent nije pronađen.");
        return;
    }

    Console.WriteLine($"Pacijent: {patient.Fname} {patient.Lname}");

    var meds = await medicineRepo.GetAllAsync();
    if (meds.Count == 0)
    {
        Console.WriteLine("Nema lijekova u bazi. Najprije dodaj lijek (opcija 8).");
        return;
    }

    Console.WriteLine("Dostupni lijekovi:");
    foreach (var m in meds)
    {
        Console.WriteLine($"{m.Id}: {m.Name} ({(m.StrengthMg.HasValue ? $"{m.StrengthMg} mg" : "jačina n/a")})");
    }

    Console.Write("Unesi ID lijeka: ");
    if (!int.TryParse(Console.ReadLine(), out var mid))
    {
        Console.WriteLine("Neispravan ID lijeka.");
        return;
    }

    var medicine = meds.FirstOrDefault(m => m.Id == mid);
    if (medicine is null)
    {
        Console.WriteLine("Lijek nije pronađen.");
        return;
    }

    Console.Write("Doza (decimal, npr. 500.00): ");
    var doseStr = Console.ReadLine();
    if (!decimal.TryParse(doseStr, out var dosage))
        dosage = 0m;

    Console.Write("Jedinica (npr. mg, ml, tableta) [prazno = 'mg']: ");
    var unit = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(unit))
        unit = "mg";

    var prescription = new Prescription
    {
        PatientId  = pid,
        MedicineId = mid,
        Dosage     = dosage,
        Unit       = unit,
        IssuedAt   = DateTime.UtcNow,
        IsActive   = true
    };

    await prescriptionRepo.InsertAsync(prescription);
    Console.WriteLine(
        $"Recept je uspješno dodan. ID recepta = {prescription.Id}, IssuedAt = {prescription.IssuedAt:yyyy-MM-dd HH:mm}");
}
