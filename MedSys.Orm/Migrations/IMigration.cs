namespace MedSys.Orm
{
    public interface IMigration
    {
 
        string Id { get; }


        string Name { get; }


        Task UpAsync(DbSession session);


        Task DownAsync(DbSession session);
    }
}
