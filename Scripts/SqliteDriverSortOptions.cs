namespace SqliteDriver
{
    public enum SortType
    {
        Desc,
        Asc
    }

    public enum SortOperatorType
    {
        Plus,
        Minus
    }

    public class SqliteDriverSortOptions
    {
        public string[] columns;
        public SortType type;
        public SortOperatorType op;

        public void Write(ref SqliteDriverCommand cmd)
        {
            if (columns == null || columns.Length == 0)
                return;
            cmd.OrderBy(columns, op, type);
        }

        public static SqliteDriverSortOptions Asc(string column, SortOperatorType operatorType = default) => Asc(new string[]
        {
            column
        }, operatorType);
        public static SqliteDriverSortOptions Asc(string[] columns, SortOperatorType operatorType = default) => new SqliteDriverSortOptions
        {
            columns = columns,
            op = operatorType,
            type = SortType.Asc
        };

        public static SqliteDriverSortOptions Desc(string column, SortOperatorType operatorType = default) => Desc(new string[]
        {
            column
        }, operatorType);
        public static SqliteDriverSortOptions Desc(string[] columns, SortOperatorType operatorType = default) => new SqliteDriverSortOptions
        {
            columns = columns,
            op = operatorType,
            type = SortType.Desc
        };
    }
}