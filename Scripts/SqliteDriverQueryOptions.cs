using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SqliteDriver
{
    public class SqliteDriverQueryOptions
    {
        public SqliteDriverWhereOptions[] where;
        public uint? offset;
        public uint? limit;

        public void Write(ref SqliteDriverCommand cmd)
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

            if (offset.HasValue)
                cmd.Offset(offset.Value);

            if (limit.HasValue)
                cmd.Limit(limit.Value);
        }
    }
}
