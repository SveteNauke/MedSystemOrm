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

        var migrator = new Migrator(session);
        var migrations = new IMigration[]
        {
            new AddPhoneToPatientsMigration()
        };
        await migrator.ApplyAsync(migrations);
        Console.WriteLine("Migracije su primijenjene.");


        var patientRepo = new Repository<Patient>(session);
        var visitRepo = new Repository<Visit>(session);
        var medicineRepo = new Repository<Medicine>(session);
        var prescriptionRepo = new Repository<Prescription>(session);

        var existingPatients = await patientRepo.QueryAsync(q =>
            q.Where("\"lname\" = 'Mirkić'")
             .OrderBy("\"id\" ASC"));

        Patient patient;
        int patientId;

        if (existingPatients.Any())
        {
            patient = existingPatients.First();
            patientId = patient.Id;
            Console.WriteLine($"Pacijent '{patient.Fname} {patient.Lname}' već postoji (ID = {patientId}).");
        }
        else
        {
            await session.BeginTransactionAsync();
            try
            {
                patient = new Patient
                {
                    Fname = "Ana",
                    Lname = "Anić",
                    BirthDate = new DateTime(1992, 1, 12),
                    Email = "a.anic@example.com",
                    Phone = "+385 91 555 9921",
                    CreatedAt = DateTime.UtcNow
                };

                patientId = await patientRepo.InsertAsync(patient);
                await session.CommitTransactionAsync();
                Console.WriteLine($"Ubačen novi pacijent '{patient.Fname} {patient.Lname}' (ID = {patientId}).");
            }
            catch
            {
                await session.RollbackTransactionAsync();
                Console.WriteLine("Došlo je do greške transakcija je rollback-ana.");
                throw;
            }
        }

        var existingMedicine = await medicineRepo.QueryAsync(q =>
            q.Where("\"name\" = 'Zoloft'")
             .OrderBy("\"id\" ASC"));

        int medicineId;
        if (existingMedicine.Any())
        {
            medicineId = existingMedicine.First().Id;
            Console.WriteLine($"Lijek već postoji (ID = {medicineId}).");
        }
        else
        {
            var medicine = new Medicine
            {
                Name = "Zithromax",
                Manufacturer = "Pfizer",
                StrengthMg = 500
            };
            medicineId = await medicineRepo.InsertAsync(medicine);
            Console.WriteLine($"Ubačen novi lijek (ID = {medicineId}).");
        }

        await session.BeginTransactionAsync();
        try
        {
            var visit = new Visit
            {
                PatientId = patientId,
                Type = VisitType.BLOOD,
                Date = DateTime.UtcNow,
                Price = 20.12m,
                DurationMinutes = 13
            };
            await visitRepo.InsertAsync(visit);

            var prescription = new Prescription
            {
                PatientId = patientId,
                MedicineId = medicineId,
                Dosage = 500,
                Unit = "mg",
                IssuedAt = DateTime.UtcNow,
                IsActive = true
            };
            await prescriptionRepo.InsertAsync(prescription);

            await session.CommitTransactionAsync();
            Console.WriteLine($"Dodan novi posjet i recept za pacijenta ID = {patientId}.");
        }
        catch
        {
            await session.RollbackTransactionAsync();
            Console.WriteLine("Greška pri dodavanju posjeta ili recepta, rollback.");
            throw;
        }

        var patients = await patientRepo.QueryAsync(q =>
            q.Where("\"id\" = " + patientId));

        var patientList = patients.ToList();

        await NavigationLoader.LoadVisitsAsync(session, patientList);
        await NavigationLoader.LoadPrescriptionsAsync(session, patientList);
        var allPrescriptions = patientList.SelectMany(p => p.Prescriptions).ToList();
        await NavigationLoader.LoadMedicinesAsync(session, allPrescriptions);

        var p0 = patientList.First();
        var birthText = p0.BirthDate?.ToString("yyyy-MM-dd") ?? "N/A";

        Console.WriteLine();
        Console.WriteLine("=== Pacijent ===");
        Console.WriteLine($"ID:      {p0.Id}");
        Console.WriteLine($"Ime:     {p0.Fname}");
        Console.WriteLine($"Prezime: {p0.Lname}");
        Console.WriteLine($"Rođen:   {birthText}");
        Console.WriteLine($"Telefon: {p0.Phone ?? "N/A"}");
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
            Console.WriteLine($"    [ID: {pr.Id}] {medName} {pr.Dosage} {pr.Unit}  " +
                              $"(izdan: {pr.IssuedAt:yyyy-MM-dd HH:mm}, aktivan: {pr.IsActive})");
        }
    }
}
