using System.Reflection;

namespace MedSys.Orm;

public sealed class ColumnMeta
{
    public PropertyInfo Prop { get; init; } = default!;
    public string Name { get; init; } = default!;
    public Type ClrType { get; init; } = default!;
    public ColumnAttribute? ColAttr { get; init; }
    public KeyAttribute? KeyAttr { get; init; }
    public ForeignKeyAttribute? FkAttr { get; init; }
    public bool IsKey => KeyAttr is not null;
}

public sealed class EntityMeta
{
    public Type ClrType { get; init; } = default!;
    public string TableName { get; init; } = default!;
    public ColumnMeta Key { get; init; } = default!;

    public List<ColumnMeta> Columns { get; init; } = new();

    public static EntityMeta From<T>() => From(typeof(T));

    private static EntityMeta From(Type t)
    {
        var table = t.GetCustomAttributes<TableAttribute>().FirstOrDefault()
            ?? throw new InvalidOperationException($"Missing [Table] attribute on {t.FullName}.");

        var cols = new List<ColumnMeta>();
        ColumnMeta? key = null;

        foreach (var prop in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var colAttr = prop.GetCustomAttributes<ColumnAttribute>().FirstOrDefault();
            var keyAttr = prop.GetCustomAttributes<KeyAttribute>().FirstOrDefault();
            var fkAttr = prop.GetCustomAttributes<ForeignKeyAttribute>().FirstOrDefault();

            if (keyAttr is not null && key is not null)
            {
                throw new InvalidOperationException($"Multiple [Key] attributes on {t.FullName}.");
            }

            var colMeta = new ColumnMeta
            {
                Prop = prop,
                Name = colAttr?.Name ?? prop.Name,
                ClrType = prop.PropertyType,
                ColAttr = colAttr,
                KeyAttr = keyAttr,
                FkAttr = fkAttr
            };

            cols.Add(colMeta);

            if (keyAttr is not null)
            {
                key = colMeta;
            }
        }

        if (key is null)
        {
            throw new InvalidOperationException($"Missing [Key] attribute on {t.FullName}.");
        }

        return new EntityMeta
        {
            ClrType = t,
            TableName = table.Name,
            Key = key,
            Columns = cols
        };
    }
}
