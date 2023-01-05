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

        public SqliteDriverWhereOptions Not()
        {
            not = true;
            return this;
        }

        public SqliteDriverWhereOptions Or()
        {
            or = true;
            return this;
        }

        public static SqliteDriverWhereOptions Eq(string column, object value) => new SqliteDriverWhereOptions
        {
            column = column,
            equals = value
        };

        public static SqliteDriverWhereOptions NotEq(string column, object value) => new SqliteDriverWhereOptions
        {
            column = column,
            notEquals = value
        };

        public static SqliteDriverWhereOptions In(string column, object[] values) => new SqliteDriverWhereOptions
        {
            column = column,
            values = values
        };

        public static SqliteDriverWhereOptions Gt(string column, object value) => new SqliteDriverWhereOptions
        {
            column = column,
            greaterThan = value
        };

        public static SqliteDriverWhereOptions Gte(string column, object value) => new SqliteDriverWhereOptions
        {
            column = column,
            greatherThanEquals = value
        };

        public static SqliteDriverWhereOptions Lt(string column, object value) => new SqliteDriverWhereOptions
        {
            column = column,
            lesserThan = value
        };

        public static SqliteDriverWhereOptions Lte(string column, object value) => new SqliteDriverWhereOptions
        {
            column = column,
            lesserThanEquals = value
        };

        public static SqliteDriverWhereOptions Includes(string column, string value) => new SqliteDriverWhereOptions
        {
            column = column,
            includes = value
        };

        public static SqliteDriverWhereOptions EndsWith(string column, string value) => new SqliteDriverWhereOptions
        {
            column = column,
            endsWith = value
        };

        public static SqliteDriverWhereOptions StartsWith(string column, string value) => new SqliteDriverWhereOptions
        {
            column = column,
            startsWith = value
        };

        public static void Write(ref SqliteDriverCommand cmd, SqliteDriverWhereOptions[] where)
        {
            if (where != null && where.Length != 0)
            {
                cmd.Where();

                for (int i = 0; i < where.Length; i++)
                {
                    bool hasNext = i + 1 != where.Length;
                    where[i].Write(ref cmd, hasNext);
                }
            }
        }

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