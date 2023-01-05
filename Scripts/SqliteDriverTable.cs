using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.Linq;
using System;

namespace SqliteDriver
{
    public class SqliteDriverTable<T>
    {
        public static readonly Dictionary<Type, FieldInfo[]> FieldCache = new();

        private SqliteDriver<T> driver;
        public SqliteDriverSerializer serializer;
        public readonly string name;
        private string[] columnNames;

        public SqliteDriverTable(SqliteDriver<T> driver, string name)
        {
            this.name = name;
            this.driver = driver;
        }

        public void SetColumns(SqliteDriverColumn[] columns)
        {
            columnNames = columns.Select(c => c.name).ToArray();
            serializer = new(columns);
        }

        public void CreateColumn(FieldInfo field)
        {
            var cmd = driver.CreateCommand().AlterTable(name).CreateColumn(field, true);
            cmd.ExecuteNonQuery(driver.connection);
        }

        public void Create(FieldInfo field)
        {
            var cmd = driver.CreateCommand().CreateTable<T>(field);
            cmd.ExecuteNonQuery(driver.connection);
        }

        public static FieldInfo[] GetStructFields() => GetStructFields(typeof(T));
        public static FieldInfo[] GetStructFields(Type type)
        {
            // We try to pick the fields from cache in case they were requested before, this increases performance.
            if (FieldCache.TryGetValue(type, out var fields))
                return fields;
            fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            FieldCache.Add(type, fields);
            return fields;
        }

        public static SqliteDriverColumn[] GetColumns() => GetColumns(typeof(T));
        public static SqliteDriverColumn[] GetColumns(Type type)
        {
            var fields = GetStructFields(type);
            var columns = new SqliteDriverColumn[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                columns[i] = new(field.Name, i, field.FieldType, SqliteDriverSerializer.GetDataTypeFor(field.FieldType));
            }

            return columns;
        }

        public static FieldInfo[] GetMissingColumns(string[] existing) => GetMissingColumns(typeof(T), existing);
        public static FieldInfo[] GetMissingColumns(Type type, string[] existing)
        {
            var fields = GetStructFields(type);
            var missing = new List<FieldInfo>();

            for (int i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                var exists = existing.FirstOrDefault(name => name == field.Name) != null;
                if (exists)
                    continue;
                missing.Add(field);
            }

            return missing.ToArray();
        }

        public bool UpdateValue<V>(V value, SqliteDriverQueryOptions options = null)
        {
            return Update(SqliteDriverUpdateOptions.CreateUpdateOptionsForValue(value), options) != 0;
        }

        public int Update(SqliteDriverUpdateOptions[] updateOptions, SqliteDriverQueryOptions options = null)
        {
            var cmd = driver.CreateCommand().Update(name);
            SqliteDriverUpdateOptions.Write(updateOptions, serializer, ref cmd);
            options?.Write(ref cmd);
            return cmd.Execute(driver.connection).RecordsAffected;
        }

        public int Delete(SqliteDriverQueryOptions options = null)
        {
            var cmd = driver.CreateCommand().Delete(name);
            options?.Write(ref cmd);
            var got = cmd.Execute(driver.connection);
            return got.RecordsAffected;
        }

        public V Get<V>(SqliteDriverQueryOptions options)
        {
            var cmd = driver.CreateCommand().Select(name);
            options?.Write(ref cmd);
            var reader = cmd.Execute(driver.connection);
            return reader.Read() ? serializer.Deserialize<V>(reader) : default;
        }



        public void Insert<V>(V value)
        {
            driver.CreateCommand().Insert(name, columnNames).Values(new object[]
            {
            string.Join(',', serializer.Serialize(value))
            }).ExecuteNonQuery(driver.connection);
        }

        public V[] All<V>(SqliteDriverQueryOptions options = null)
        {
            var cmd = driver.CreateCommand().Select(name);
            options?.Write(ref cmd);

            var reader = cmd.Execute(driver.connection);
            var list = new List<V>();
            while (reader.Read())
            {
                list.Add(serializer.Deserialize<V>(reader));
            }
            return list.ToArray();
        }
    }
}
