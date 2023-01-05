using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SqliteDriver
{
    public class SqliteDriverQueryOptions
    {
        public SqliteDriverSortOptions sort;
        public SqliteDriverWhereOptions[] where;
        public uint? offset;
        public uint? limit;

        public void Write(ref SqliteDriverCommand cmd)
        {
            SqliteDriverWhereOptions.Write(ref cmd, where);

            if (sort != null)
                sort.Write(ref cmd);

            if (offset.HasValue)
                cmd.Offset(offset.Value);

            if (limit.HasValue)
                cmd.Limit(limit.Value);
        }

        public SqliteDriverQueryOptions AppendWhereOption(SqliteDriverWhereOptions option)
        {
            where ??= Array.Empty<SqliteDriverWhereOptions>();
            var pos = where.Length;
            Array.Resize(ref where, where.Length + 1);
            where[pos] = option;
            return this; 
        }
    }
}
