using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SqliteDriver
{
    public class SqliteDriverWhereOptions
    {
        public string column;

        public object equals;
        public object notEquals;
        public string includes;
        public string startsWith;
        public string endsWith;
        public object greaterThan;
        public object greatherThanEquals;
        public object lesserThan;
        public object lesserThanEquals;
        public object[] values;

        public bool not;
        public bool or;

        public static SqliteDriverWhereOptions Eq(string column, object value) => new SqliteDriverWhereOptions
        {
            column = column,
            equals = value
        };

        public void Write(ref SqliteDriverCommand cmd, bool hasNext)
        {
            if (not)
                cmd.Not();

            if (equals != null)
                cmd.Eq(column, equals);
            else if (notEquals != null)
                cmd.NotEq(column, equals);
            else if (includes != null)
                cmd.Includes(column, includes);
            else if (startsWith != null)
                cmd.StartsWith(column, startsWith);
            else if (endsWith != null)
                cmd.EndsWith(column, endsWith);
            else if (greaterThan != null)
                cmd.Gt(column, greaterThan);
            else if (greatherThanEquals != null)
                cmd.Gte(column, greatherThanEquals);
            else if (lesserThan != null)
                cmd.Lt(column, lesserThan);
            else if (lesserThanEquals != null)
                cmd.Lte(column, lesserThanEquals);
            else if (values != null && values.Length != 0)
                cmd.In(column, values);

            if (hasNext)
            {
                if (or)
                    cmd.Or();
                else
                    cmd.And();
            }
        }
    }
}