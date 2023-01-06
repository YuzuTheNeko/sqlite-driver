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

        public void DropColumn(string column)
        {
            var cmd = driver.CreateCommand().AlterTable(name).DropColumn(column);
            cmd.ExecuteNonQuery(driver.connection);
        }

        public void SetColumns(SqliteDriverColumn[] columns)
        {
            columnNames = columns.Select(c => c.name).ToArray();
            serializer = new(columns);
        }

        public void CreateColumn(SqliteDriverColumn clm)
        {
            var cmd = driver.CreateCommand().AlterTable(name).CreateColumn(clm, true);
            cmd.ExecuteNonQuery(driver.connection);
        }

        public void Create()
        {
            var cmd = driver.CreateCommand().CreateTable<T>(name, serializer.columns);
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
                columns[i] = new(field, i);
            }
            return columns;
        }

        public static string[] GetUnusedColumns(Type type, List<string> existing)
        {
            var fields = GetStructFields(type);
            var unused = new List<string>();
            for (int i = 0;i < existing.Count;i++)
            {
                var current = existing[i];
                var exists = fields.FirstOrDefault(c => c.Name == current) != null;
                if (exists)
                    continue;
                unused.Add(current);
            }

            return unused.ToArray();
        }

        public static FieldInfo[] GetMissingColumns(List<string> existing) => GetMissingColumns(typeof(T), existing);
        public static FieldInfo[] GetMissingColumns(Type type, List<string> existing)
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

        /// <summary>
        /// Returns true if the value has been inserted and not updated.
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="value"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public bool Upsert<V>(V value, SqliteDriverQueryOptions options = null)
        {
            if (!Has(options))
            {
                Insert(value);
                return true; 
            }
            else
            {
                UpdateValue(value, options);
                return false;
            }
        }

        public bool Has(SqliteDriverQueryOptions options)
        {
            var cmd = driver.CreateCommand().Select(name);
            options.Write(ref cmd);
            var reader = cmd.Execute(driver.connection);
            return reader.HasRows;
        }

        public long GetRowPosition(SqliteDriverQueryOptions options)
        {
            var cmd = driver.CreateCommand().RowNumber(name, options);
            var reader = cmd.Execute(driver.connection);
            return (long)reader.GetValue(0);
        }

        public long GetRowCount(SqliteDriverQueryOptions options = null)
        {
            var cmd = driver.CreateCommand().Count(name);
            options?.Write(ref cmd);
            using var reader = cmd.Execute(driver.connection);
            return (long)reader.GetValue(0);
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
