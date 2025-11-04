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

    public bool IsKey => KeyAttr != null;
}

public sealed class EntityMeta
{
    public Type ClrType { get; init; } = default!;
    public string TableName { get; init; } = default!;
    public ColumnMeta Key { get; init; } = default!;
    public List<ColumnMeta> Columns { get; init; } = new();

    public static EntityMeta From<T>() => From(typeof(T));

    public static EntityMeta From(Type t)
    {
        var tableAttr = t.GetCustomAttribute<TableAttribute>()
                         ?? throw new InvalidOperationException($"Missing [Table] on {t.Name}");

        var cols = new List<ColumnMeta>();
        ColumnMeta? key = null;

        foreach (var prop in t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var colAttr = prop.GetCustomAttribute<ColumnAttribute>();
            var keyAttr = prop.GetCustomAttribute<KeyAttribute>();

            if (colAttr == null && keyAttr == null)
                continue;

            var col = new ColumnMeta
            {
                Prop = prop,
                Name = (colAttr?.Name ?? prop.Name).ToLower(),
                ClrType = prop.PropertyType,
                ColAttr = colAttr,
                KeyAttr = keyAttr,
                FkAttr = prop.GetCustomAttribute<ForeignKeyAttribute>()
            };

            cols.Add(col);
            if (keyAttr != null)
                key = col;
        }

        if (key == null)
            throw new InvalidOperationException($"Entity {t.Name} must have [Key] property.");

        return new EntityMeta
        {
            ClrType = t,
            TableName = tableAttr.Name,
            Key = key,
            Columns = cols
        };
    }
}
