using System;
using System.Linq;
using System.Threading.Tasks;
using MedSys.Domain;
using MedSys.Orm;
using MedSys.App; 

internal class Program
{
    private static async Task Main()
    {
        var cs = Environment.GetEnvironmentVariable("MEDSYS_CONN")
                 ?? throw new InvalidOperationException("MEDSYS_CONN nije postavljen.");

        await using var session = new DbSession(cs);
        await session.OpenAsync();
        Console.WriteLine("Uspješno spojeno na Supabase PostgreSQL");

        var schemaCreator = new SchemaCreator(session.Connection);
        await schemaCreator.EnsureAsync(
            typeof(Patient),
            typeof(Visit),
            typeof(Medicine),
            typeof(Prescription)
        );
        Console.WriteLine("Shema je osigurana (tablice kreirane ako nisu postojale).");


        var patientRepo = new Repository<Patient>(session);
        var visitRepo = new Repository<Visit>(session);
        var medicineRepo = new Repository<Medicine>(session);
        var prescriptionRepo = new Repository<Prescription>(session);

        await session.BeginTransactionAsync();
        int patientId;
        try
        {
            var patient = new Patient
            {
                Fname = "Mirko",
                Lname = "Mirkić",
                BirthDate = new DateTime(1995, 2, 25),
                Email = "m.mirkic@example.com",
                CreatedAt = DateTime.UtcNow
            };

            patientId = await patientRepo.InsertAsync(patient);

            var medicine = new Medicine
            {
                Name = "Zoloft",
                Manufacturer = "Pliva",
                StrengthMg = 50
            };

            var medicineId = await medicineRepo.InsertAsync(medicine);

            var visit = new Visit
            {
                PatientId = patientId,
                Type = VisitType.GP,
                Date = DateTime.UtcNow,
                Price = 50.00m,
                DurationMinutes = 15
            };

            await visitRepo.InsertAsync(visit);

            var prescription = new Prescription
            {
                PatientId = patientId,
                MedicineId = medicineId,
                Dosage = 400m,
                Unit = "mg",
                IssuedAt = DateTime.UtcNow,
                IsActive = true
            };

            await prescriptionRepo.InsertAsync(prescription);

            await session.CommitTransactionAsync();
            Console.WriteLine($"Ubačen pacijent s ID = {patientId} zajedno s posjetom i receptom (ORM + transakcija).");
        }
        catch
        {
            await session.RollbackTransactionAsync();
            Console.WriteLine("Došlo je do greške transakcija je rollback-ana.");
            throw;
        }

        var patients = await patientRepo.QueryAsync(q =>
            q.Where("\"lname\" = 'Mirkić'")
             .OrderBy("\"id\" DESC"));

        var patientList = patients.ToList();
        if (patientList.Count == 0)
        {
            Console.WriteLine("Nema pacijenata s prezimenom Kovač.");
            return;
        }

        await NavigationLoader.LoadVisitsAsync(session, patientList);
        await NavigationLoader.LoadPrescriptionsAsync(session, patientList);

        var allPrescriptions = patientList.SelectMany(p => p.Prescriptions).ToList();
        await NavigationLoader.LoadMedicinesAsync(session, allPrescriptions);

        var p0 = patientList[0];
        var birthText = p0.BirthDate?.ToString("yyyy-MM-dd") ?? "N/A";

        Console.WriteLine();
        Console.WriteLine("=== Pacijent ===");
        Console.WriteLine($"ID:      {p0.Id}");
        Console.WriteLine($"Ime:     {p0.Fname}");
        Console.WriteLine($"Prezime: {p0.Lname}");
        Console.WriteLine($"Rođen:   {birthText}");
        Console.WriteLine();

        Console.WriteLine("  Posjeti:");
        foreach (var v in p0.Visits)
        {
            Console.WriteLine($"    [{v.Date:yyyy-MM-dd HH:mm}] {v.Type}  (cijena: {v.Price}, trajanje: {v.DurationMinutes} min)");
        }

        Console.WriteLine();
        Console.WriteLine("  Recepti:");
        foreach (var pr in p0.Prescriptions)
        {
            var medName = pr.Medicine?.Name ?? "(nepoznat lijek)";
            Console.WriteLine($"    {medName} {pr.Dosage} {pr.Unit}  (izdan: {pr.IssuedAt:yyyy-MM-dd}, aktivan: {pr.IsActive})");
        }
    }
}
