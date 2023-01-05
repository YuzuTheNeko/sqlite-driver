using Mono.Data.Sqlite; // 1
using System;
using System.Collections.Generic;
using System.Data; // 1
using System.Linq;
using UnityEngine;

namespace SqliteDriver
{
    public class SqliteDriver<T>
    {
        private static readonly string ConnectionPrefix = "URI=file:";
        private readonly Dictionary<Type, SqliteDriverTable<T>> tables = new();
        public readonly SqliteConnection connection;

        public SqliteDriver(string name)
        {
            connection = new SqliteConnection($"{ConnectionPrefix}{name}");
        }

        public SqliteDriverCommand CreateCommand() => new();

        public string[] GetTableNames()
        {
            var cmd = CreateCommand().Select(new string[] { "name" }, "sqlite_schema").Where().Not().EndsWith("name", "_master");
            var reader = cmd.Execute(connection);
            var list = new List<string>();

            while (reader.Read())
            {
                list.Add(reader.GetString(0));
            }

            return list.ToArray();
        }

        public long GetRowCount<V>(SqliteDriverQueryOptions options = null) => GetTable(typeof(V)).GetRowCount(options);

        public bool UpdateValue<V>(V value, SqliteDriverQueryOptions options = null)
        {
            return GetTable(typeof(V)).UpdateValue(value, options);
        }

        public int Update<V>(SqliteDriverUpdateOptions[] updateOptions, SqliteDriverQueryOptions options = null)
        {
            return GetTable(typeof(V)).Update(updateOptions, options);
        }

        public V[] All<V>(SqliteDriverQueryOptions options = null)
        {
            return GetTable(typeof(V)).All<V>(options);
        }

        public int Delete<V>(SqliteDriverQueryOptions options = null)
        {
            return GetTable(typeof(V)).Delete(options);
        }

        public V Get<V>(SqliteDriverQueryOptions options)
        {
            return GetTable(typeof(V)).Get<V>(options);
        }

        public long GetRowPosition<V>(SqliteDriverQueryOptions options) => GetTable<V>().GetRowPosition(options);
        public bool Has<V>(SqliteDriverQueryOptions options) => GetTable<V>().Has(options);
        public bool Upsert<V>(V value, SqliteDriverQueryOptions options = null) => GetTable<V>().Upsert(value, options);

        public void Insert<V>(V value)
        {
            GetTable(typeof(V)).Insert(value);
        }

        public string[] GetTableColumns(string name)
        {
            var cmd = CreateCommand().Pragma.TableInfo(name);
            var reader = cmd.Execute(connection);
            var columns = new List<string>();

            while (reader.Read())
            {
                columns.Add(reader.GetString(1));
            }

            return columns.ToArray();
        }

        private void Init()
        {
            var structType = typeof(T);
            var tables = GetTableNames();
            var structTables = SqliteDriverTable<T>.GetStructFields();
            for (int i = 0; i < structTables.Length; i++)
            {
                var memoryTable = structTables[i];
                Debug.Log($"Analyzing table {memoryTable.Name}...");

                var exists = tables.FirstOrDefault(n => n == memoryTable.Name) != null;
                if (!exists)
                {
                    var memoryTableColumns = SqliteDriverTable<T>.GetColumns(memoryTable.FieldType);
                    Debug.LogWarning("Creating table " + memoryTable.Name + " with " + memoryTableColumns.Length + " columns");
                    var newTable = new SqliteDriverTable<T>(this, memoryTable.Name);
                    newTable.SetColumns(memoryTableColumns);
                    newTable.Create(memoryTable);
                    this.tables.Add(memoryTable.FieldType, newTable);
                    continue;
                }

                var columns = new List<SqliteDriverColumn>();

                Debug.LogWarning($"Table {memoryTable.Name} found in database!");

                var existingColumnNames = GetTableColumns(memoryTable.Name);
                Debug.Log($"Found existing columns {string.Join(' ', existingColumnNames)}");

                var tbl = new SqliteDriverTable<T>(this, memoryTable.Name);
                this.tables.Add(memoryTable.FieldType, tbl);

                var difference = SqliteDriverTable<T>.GetMissingColumns(memoryTable.FieldType, existingColumnNames);
                if (difference.Length == 0)
                {
                    Debug.LogWarning("All columns exist!");
                    var clms = new SqliteDriverColumn[existingColumnNames.Length];
                    for (int y = 0; y < existingColumnNames.Length; y++)
                    {
                        var name = existingColumnNames[y];
                        var reference = memoryTable.FieldType.GetField(name);
                        clms[y] = new(name, y, reference.FieldType, SqliteDriverSerializer.GetDataTypeFor(reference.FieldType));
                    }
                    tbl.SetColumns(clms);
                    continue;
                }

                var tableColumns = new List<SqliteDriverColumn>();
                int x = 0;
                foreach (var alreadyExisting in existingColumnNames)
                {
                    var existsInMemory = memoryTable.FieldType.GetField(alreadyExisting);
                    if (existsInMemory != null)
                    {
                        Debug.Log($"Column {alreadyExisting} exists on memory table");
                        var type = SqliteDriverSerializer.GetDataTypeFor(existsInMemory.FieldType);
                        tableColumns.Add(new(existsInMemory.Name, x, existsInMemory.FieldType, type));
                    }
                    else
                        Debug.LogWarning($"Column {alreadyExisting} does not exist on memory table.");
                    x++;
                }

                foreach (var nonExisting in difference)
                {
                    var type = SqliteDriverSerializer.GetDataTypeFor(nonExisting.FieldType);
                    var clm = new SqliteDriverColumn(nonExisting.Name, x, nonExisting.FieldType, type);
                    tbl.CreateColumn(nonExisting);
                    Debug.Log($"Appended column {nonExisting.Name} to table {tbl.name}.");
                    tableColumns.Add(clm);
                    x++;
                }

                tbl.SetColumns(tableColumns.ToArray());
            }
            Debug.Log(this.tables.Count);
        }

        public SqliteDriverTable<T> GetTable<V>() => GetTable(typeof(V));
        public SqliteDriverTable<T> GetTable(Type type) => tables[type];

        public void Open()
        {
            connection.Open();
            Init();
        }

        public void Close()
        {
            connection.Close();
        }
    }
}