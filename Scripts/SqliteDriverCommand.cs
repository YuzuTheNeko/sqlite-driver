using Mono.Data.Sqlite;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using UnityEngine;

namespace SqliteDriver
{
    public class SqliteDriverCommand
    {
        private List<string> list = new();
        private bool notClause = false;

        public SqliteDriverCommand Select(string table)
        {
            list.Add("SELECT * FROM " + table);
            return this;
        }

        public SqliteDriverCommand WriteRaw(string raw)
        {
            list.Add(raw);
            return this;
        }

        public SqliteDriverCommand Insert(string table, string[] columns)
        {
            list.Add("INSERT INTO " + table + "(" + string.Join(", ", columns) + ")");
            return this;
        }

        public SqliteDriverCommand Delete(string table)
        {
            list.Add("DELETE FROM " + table);
            return this;
        }

        public SqliteDriverCommand Values(object[] values)
        {
            list.Add("VALUES");
            foreach (var i in values)
            {
                list.Add($"({i})");
            }
            return this;
        }

        public SqliteDriverCommand TableInfo(string name)
        {
            list.Add($"table_info({name})");
            return this;
        }

        public SqliteDriverCommand CreateColumn(FieldInfo field, bool addColumn)
        {
            var isPrimary = field.GetCustomAttribute<PrimaryKeyAttribute>() != null;
            var foreign = field.GetCustomAttribute<ForeignKeyAttribute>();
            var notNull = field.GetCustomAttribute<NotNullAttribute>();

            var name = field.Name;
            var type = SqliteDriverSerializer.GetDataTypeFor(field.FieldType);

            list.Add($"{(addColumn ? "ADD COLUMN" : "")} {name} {SqliteDriverSerializer.ToSqlType(type)} {(isPrimary ? "PRIMARY KEY" : "")} {(notNull != null ? "NOT NULL" : "")} {(foreign != null ? $"FOREIGN KEY REFERENCES {foreign.referenceTable}({foreign.referenceColumn})" : "")}");

            return this;
        }

        public SqliteDriverCommand AlterTable(string name)
        {
            list.Add("ALTER TABLE " + name);
            return this;
        }

        public SqliteDriverCommand DropTable(string name)
        {
            list.Add("DROP TABLE " + name);
            return this;
        }

        public SqliteDriverCommand CreateTable<T>(FieldInfo fld)
        {
            list.Add($"CREATE TABLE {fld.Name}");
            list.Add("(");

            var fields = SqliteDriverTable<T>.GetStructFields(fld.FieldType);
            for (int i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                CreateColumn(field, false);
                if (i + 1 != fields.Length)
                    list.Add(",");
            }

            list.Add(")");
            return this;
        }

        public SqliteDriverCommand Pragma
        {
            get
            {
                list.Add("PRAGMA");
                return this;
            }
        }

        public SqliteDriverCommand Select(string[] columns, string table)
        {
            list.Add($"SELECT {string.Join(", ", columns)} FROM {table}");
            return this;
        }

        public SqliteDriverCommand Where()
        {
            list.Add("WHERE");
            return this;
        }

        public SqliteDriverCommand Eq(string column, object value)
        {
            list.Add($"{column} = {SqliteDriverSerializer.Sanitize(value, true)}");
            return this;
        }

        public SqliteDriverCommand NotEq(string column, object value)
        {
            list.Add($"{column} != {SqliteDriverSerializer.Sanitize(value, true)}");
            return this;
        }

        public SqliteDriverCommand StartsWith(string column, string value)
        {
            list.Add($"{column} {InjectNotClause()}LIKE '{SqliteDriverSerializer.Sanitize(value, false)}%'");
            return this;
        }

        public SqliteDriverCommand Not()
        {
            notClause = true;
            return this;
        }

        private string InjectNotClause()
        {
            if (!notClause) return string.Empty;
            notClause = false;
            return "NOT ";
        }

        public SqliteDriverCommand EndsWith(string column, string value)
        {
            list.Add($"{column} {InjectNotClause()}LIKE '%{SqliteDriverSerializer.Sanitize(value, false)}'");
            return this;
        }

        public SqliteDriverCommand Includes(string column, string value)
        {
            list.Add($"{column} {InjectNotClause()}LIKE '%{SqliteDriverSerializer.Sanitize(value, false)}%'");
            return this;
        }

        public SqliteDriverCommand Or()
        {
            list.Add("OR");
            return this;
        }

        public SqliteDriverCommand And()
        {
            list.Add("AND");
            return this;
        }

        public string GetSqlQuery() => string.Join(' ', list);

        private void Prepare(ref SqliteCommand cmd)
        {
            cmd.CommandText = GetSqlQuery();
        }

        public SqliteDataReader Execute(SqliteConnection connection)
        {
            var cmd = connection.CreateCommand();
            Prepare(ref cmd);
            Debug.Log($"Executing query {cmd.CommandText}");
            return cmd.ExecuteReader();
        }

        public void ExecuteNonQuery(SqliteConnection connection)
        {
            var cmd = connection.CreateCommand();
            Prepare(ref cmd);
            Debug.Log($"Executing query {cmd.CommandText}");
            cmd.ExecuteNonQuery();
        }
    }
}
