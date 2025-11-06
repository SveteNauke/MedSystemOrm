namespace MedSys.Orm
{
    public interface IMigration
    {
        string Id { get; }  
        string UpSql { get; } 
        string DownSql { get; } 
    }
}
