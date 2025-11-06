using System.Text;


namespace MedSys.Orm
{
    public sealed class QueryBuilder
    {
        private readonly string _table;
        private readonly List<string> _where = new();
        private string? _orderBy;

        public QueryBuilder(string table)
        {
            _table = table;
        }

        public QueryBuilder Where(string condition)
        {
            _where.Add(condition);
            return this;
        }

        public QueryBuilder OrderBy(string orderBy)
        {
            _orderBy = orderBy;
            return this;
        }

        public string BuildSelect()
        {
            var sb = new StringBuilder();
            sb.Append($"SELECT * FROM {_table}");

            if (_where.Count > 0)
            {
                sb.Append(" WHERE ");
                sb.Append(string.Join(" AND ", _where));
            }

            if (!string.IsNullOrEmpty(_orderBy))
            {
                sb.Append(" ORDER BY ");
                sb.Append(_orderBy);
            }

            sb.Append(";");

            return sb.ToString();
        }
    }
}