namespace MedSys.Orm
{
    public sealed class AddPhoneToPatientsMigration : IMigration
    {
        public string Id => "20251106_add_phone_to_patients";

        public string UpSql => @"
            ALTER TABLE ""patients""
            ADD COLUMN IF NOT EXISTS ""phone"" VARCHAR(20);
        ";
        
        public string DownSql => @"
            ALTER TABLE ""patients""
            DROP COLUMN IF EXISTS ""phone"";
        ";
    }
}
