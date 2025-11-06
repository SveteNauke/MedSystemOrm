using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MedSys.Domain;
using MedSys.Orm;

namespace MedSys.App
{
    public static class NavigationLoader
    {
        /// <summary>
        /// Učita Visit listu za dane pacijente i napuni Patient.Visits + Visit.Patient.
        /// </summary>
        public static async Task LoadVisitsAsync(DbSession session, IEnumerable<Patient> patients)
        {
            var list = patients.ToList();
            if (list.Count == 0) return;

            var ids = list.Select(p => p.Id).Distinct().ToArray();
            var idsCsv = string.Join(",", ids);

            var visitRepo = new Repository<Visit>(session);

            var visits = await visitRepo.QueryAsync(q =>
                q.Where($"\"patientid\" IN ({idsCsv})")
                 .OrderBy("\"date\" ASC"));

            var byPatient = visits
                .GroupBy(v => v.PatientId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var p in list)
            {
                if (byPatient.TryGetValue(p.Id, out var vs))
                {
                    p.Visits = vs;
                    foreach (var v in vs)
                    {
                        v.Patient = p;
                    }
                }
                else
                {
                    p.Visits = new List<Visit>();
                }
            }
        }

        /// <summary>
        /// Učita Prescription listu za dane pacijente i napuni Patient.Prescriptions + Prescription.Patient.
        /// </summary>
        public static async Task LoadPrescriptionsAsync(DbSession session, IEnumerable<Patient> patients)
        {
            var list = patients.ToList();
            if (list.Count == 0) return;

            var ids = list.Select(p => p.Id).Distinct().ToArray();
            var idsCsv = string.Join(",", ids);

            var prescriptionRepo = new Repository<Prescription>(session);

            var prescriptions = await prescriptionRepo.QueryAsync(q =>
                q.Where($"\"patientid\" IN ({idsCsv})")
                 .OrderBy("\"issued_at\" DESC"));

            var byPatient = prescriptions
                .GroupBy(pr => pr.PatientId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var p in list)
            {
                if (byPatient.TryGetValue(p.Id, out var prs))
                {
                    p.Prescriptions = prs;
                    foreach (var pr in prs)
                    {
                        pr.Patient = p;
                    }
                }
                else
                {
                    p.Prescriptions = new List<Prescription>();
                }
            }
        }

        /// <summary>
        /// Učita Medicine za dane recepte i napuni Prescription.Medicine + Medicine.Prescriptions.
        /// </summary>
        public static async Task LoadMedicinesAsync(DbSession session, IEnumerable<Prescription> prescriptions)
        {
            var list = prescriptions.ToList();
            if (list.Count == 0) return;

            var medIds = list.Select(pr => pr.MedicineId).Distinct().ToArray();
            var idsCsv = string.Join(",", medIds);

            var medRepo = new Repository<Medicine>(session);

            var meds = await medRepo.QueryAsync(q =>
                q.Where($"\"id\" IN ({idsCsv})")
                 .OrderBy("\"name\" ASC"));

            var medsById = meds.ToDictionary(m => m.Id);

            foreach (var pr in list)
            {
                if (medsById.TryGetValue(pr.MedicineId, out var med))
                {
                    pr.Medicine = med;

                    if (med.Prescriptions == null)
                        med.Prescriptions = new List<Prescription>();

                    if (!med.Prescriptions.Contains(pr))
                        med.Prescriptions.Add(pr);
                }
            }
        }
    }
}
