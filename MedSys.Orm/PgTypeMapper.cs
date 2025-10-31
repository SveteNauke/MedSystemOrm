namespace MedSys.Orm
{
    public static class PgTypeMapper
    {
        public static string Quote(string ident) => $"\"{ident}\"";

        public static string ToSqlType(ColumnMeta c)
        {
            var ca = c.ColAttr;

            if (!string.IsNullOrWhiteSpace(ca?.TypeName))
            {
                if (ca!.TypeName.Equals("varchar", StringComparison.OrdinalIgnoreCase) && ca.Length > 0)
                    return $"VARCHAR({ca.Length})";
                return ca.TypeName!;
            }

            var t = Nullable.GetUnderlyingType(c.ClrType) ?? c.ClrType;

            if (t == typeof(int)) return "INT";
            if (t == typeof(long)) return "BIGINT";
            if (t == typeof(bool)) return "BOOLEAN";
            if (t == typeof(float)) return "REAL";                 
            if (t == typeof(double)) return "DOUBLE PRECISION";
            if (t == typeof(decimal)) return "DECIMAL(18,2)";
            if (t == typeof(DateTime)) return "TIMESTAMP";
            if (t.IsEnum) return "INT";

            var len = ca?.Length > 0 ? ca!.Length : 255;
            return $"VARCHAR({len})";
        }
    }
}
