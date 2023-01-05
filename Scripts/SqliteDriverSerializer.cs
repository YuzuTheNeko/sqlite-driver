using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

namespace SqliteDriver
{
    public enum SqliteDriverDataType : byte
    {
        Int,
        Text,
        Bool,
        Float,
        TinyInt,
        Json
    }

    public class SqliteDriverSerializer
    {
        public readonly Dictionary<string, SqliteDriverColumn> columnsDict = new();
        public readonly SqliteDriverColumn[] columns;

        public SqliteDriverSerializer(SqliteDriverColumn[] columns)
        {
            this.columns = columns;
            foreach (var clm in columns)
            {
                columnsDict.Add(clm.name, clm);
            }
        }

        public T Deserialize<T>(IDataReader reader)
        {
            var structure = Activator.CreateInstance<T>();
            var structType = structure.GetType();

            for (int i = 0; i < columns.Length; i++)
            {
                var column = columns[i];
                var field = structType.GetField(column.name);
                var value = DeserializeValue(column, reader);
                // We continue the loop because this should be set to a default value when initialized, which can be null anyway.
                if (value is null)
                    continue;
                field.SetValue(structure, value);
            }

            return structure;
        }

        private object SolveIntType(long value, Type type)
        {
            if (IsType<int>(type))
                return (int)value;
            else if (IsType<uint>(type))
                return (uint)value;
            else if (IsType<long>(type))
                return value;
            else if (IsType<ulong>(type))
                return value;
            else if (IsType<ushort>(type))
                return value;

            throw new Exception($"Unhandled int type {type.Name}");
        }

        public object DeserializeValue(SqliteDriverColumn column, IDataReader reader)
        {
            return column.type switch
            {
                SqliteDriverDataType.Json => JsonConvert.DeserializeObject(reader.GetString(column.index), column.realType),
                SqliteDriverDataType.Float => reader.GetFloat(column.index),
                SqliteDriverDataType.TinyInt => reader.GetInt16(column.index),
                SqliteDriverDataType.Bool => reader.GetInt16(column.index) == 1,
                SqliteDriverDataType.Int => SolveIntType(reader.GetInt32(column.index), column.realType),
                SqliteDriverDataType.Text => reader.GetString(column.index),
                _ => throw new Exception($"Unhandled driver data type {column.type}")
            };
        }

        public object[] Serialize(object structure, Func<object, object> fn = null)
        {
            var structType = structure.GetType();
            var values = new object[columns.Length];
            int i = 0;
            foreach (var column in columns)
            {
                var field = structType.GetField(column.name);
                var value = field.GetValue(structure);
                var serialized = SerializeValue(column.type, value);
                values[i++] = fn?.Invoke(serialized) ?? serialized;
            }
            return values;
        }

        public object SerializeValue(SqliteDriverDataType type, object value)
        {
            return type switch
            {
                SqliteDriverDataType.Json => Sanitize(JsonConvert.SerializeObject(value), true),
                SqliteDriverDataType.Float => value,
                SqliteDriverDataType.Bool => (bool)value ? 1 : 0,
                SqliteDriverDataType.TinyInt => (value).ToString(),
                SqliteDriverDataType.Int => value.ToString(),
                SqliteDriverDataType.Text => (string)Sanitize(value, true),
                _ => throw new Exception($"Unhandled driver data type {type}"),
            };
        }

        public static object Sanitize(object value, bool addCommas)
        {
            if (value is null)
                return "null";
            else if (value is string v)
            {
                var str = v.Replace('\'', '\0');
                return addCommas ? $"'{str}'" : str;
            }
            else
                return value;
        }

        public static bool IsType<T>(Type type)
        {
            return typeof(T) == type;
        }

        public static string ToSqlType(SqliteDriverDataType type)
        {
            return type switch
            {
                SqliteDriverDataType.Float => "REAL",
                SqliteDriverDataType.Bool => "INT",
                SqliteDriverDataType.Int => "INT",
                SqliteDriverDataType.Text => "TEXT",
                SqliteDriverDataType.TinyInt => "TINYINT",
                SqliteDriverDataType.Json => "TEXT",
                _ => throw new Exception($"Unhandled driver type {type}")
            };
        }

        public static SqliteDriverDataType GetDataTypeFor(Type type)
        {
            if (IsType<long>(type) ||
                IsType<ulong>(type) ||
                IsType<uint>(type) ||
                IsType<int>(type) ||
                IsType<ushort>(type))
                return SqliteDriverDataType.Int;
            if (IsType<short>(type) ||
                IsType<byte>(type) ||
                IsType<sbyte>(type))
                return SqliteDriverDataType.TinyInt;
            else if (IsType<float>(type))
                return SqliteDriverDataType.Float;
            else if (IsType<string>(type))
                return SqliteDriverDataType.Text;
            else if (IsType<bool>(type))
                return SqliteDriverDataType.Bool;
            else
                return SqliteDriverDataType.Json;
        }
    }
}