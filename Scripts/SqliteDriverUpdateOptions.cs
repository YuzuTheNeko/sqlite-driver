using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SqliteDriver
{
    public enum UpdateOperationType
    {
        Set,
        Add,
        Remove
    }

    public class SqliteDriverUpdateOptions
    {
        public UpdateOperationType operation; 
        public string column;
        public object value;

        public static SqliteDriverUpdateOptions Set(string column, object value)
        {
            return new SqliteDriverUpdateOptions
            {
                column = column,
                operation = UpdateOperationType.Set,
                value = value
            };
        }

        public static SqliteDriverUpdateOptions Add(string column, object value)
        {
            return new SqliteDriverUpdateOptions
            {
                column = column,
                operation = UpdateOperationType.Add,
                value = value
            };
        }

        public static SqliteDriverUpdateOptions Remove(string column, object value)
        {
            return new SqliteDriverUpdateOptions
            {
                column = column,
                operation = UpdateOperationType.Remove,
                value = value
            };
        }

        public static void Write(SqliteDriverUpdateOptions[] options, SqliteDriverSerializer serializer, ref SqliteDriverCommand cmd)
        {
            if (options == null || options.Length == 0)
                return;

            for (int i = 0;i < options.Length;i++)
            {
                bool hasNext = i + 1 != options.Length;
                var option = options[i];
                var clm = serializer.columnsDict[option.column];
                var raw = serializer.SerializeValue(clm.type, option.value);
                cmd.WriteUpdateOp(option.column, option.operation, raw);
                if (hasNext)
                    cmd.WriteRaw(",");
            }
        }

        public static SqliteDriverUpdateOptions[] CreateUpdateOptionsForValue<V>(V value)
        {
            var fields = SqliteDriverTable<V>.GetStructFields();
            var options = new SqliteDriverUpdateOptions[fields.Length];
            for (int i = 0;i < fields.Length;i++)
            {
                var field = fields[i];
                options[i] = new SqliteDriverUpdateOptions
                {
                    column = field.Name,
                    operation = UpdateOperationType.Set,
                    value = field.GetValue(value)
                };
            }

            return options;
        }
    }
}