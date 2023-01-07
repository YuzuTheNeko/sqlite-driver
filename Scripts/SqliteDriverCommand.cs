using Mono.Data.Sqlite;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SqliteDriver
{
    public class SqliteDriverCommand
    {
        private string query = "";
        private bool notClause = false;

        public SqliteDriverCommand Select(string table)
        {
            Write("SELECT * FROM " + table);
            return this;
        }

        public SqliteDriverCommand WriteRaw(string raw)
        {
            Write(raw);
            return this;
        }

        private void Write(string q)
        {
            query += $" {q} ";
        }

        public SqliteDriverCommand RowNumber(string table, SqliteDriverQueryOptions options)
        {
            var cmd = this;

            Write($"SELECT ROW_NUMBER() OVER(");
            options.sort.Write(ref cmd);
            Write($") RowNum, * FROM {table}");

            SqliteDriverWhereOptions.Write(ref cmd, options.where);
            return this; 
        }

        public SqliteDriverCommand Insert(string table, string[] columns)
        {
            Write("INSERT INTO " + table + "(" + string.Join(", ", columns) + ")");
            return this;
        }

        public SqliteDriverCommand Count(string table)
        {
            Write("SELECT COUNT(*) FROM " + table);
            return this;
        }

        public SqliteDriverCommand DropColumn(string column)
        {
            Write($"DROP COLUMN {column}");
            return this;
        }

        public SqliteDriverCommand OrderBy(string[] columns, SortOperatorType operatorType, SortType sortType)
        {
            Write($"ORDER BY ({string.Join(operatorType.ToString(), columns)}) {sortType}");
            return this;
        }

        public SqliteDriverCommand Gte(string column, object value)
        {
            Write($"{column} >= {value}");
            return this;
        }

        public SqliteDriverCommand Gt(string column, object value)
        {
            Write($"{column} > {value}");
            return this;
        }

        public SqliteDriverCommand In(string column, object[] values)
        {
            Write($"{column} {InjectNotClause()}IN ({string.Join(',', values.Select(c => SqliteDriverSerializer.Sanitize(c, true)))})");
            return this;
        }

        public SqliteDriverCommand Lte(string column, object value)
        {
            Write($"{column} <= {value}");
            return this;
        }

        public SqliteDriverCommand Lt(string column, object value)
        {
            Write($"{column} < {value}");
            return this;
        }

        public SqliteDriverCommand Offset(uint offset)
        {
            Write($"OFFSET {offset}");
            return this;
        }

        public SqliteDriverCommand Limit(uint limit)
        {
            Write($"LIMIT {limit}");
            return this;
        }
        public SqliteDriverCommand Delete(string table)
        {
            Write("DELETE FROM " + table);
            return this;
        }

        public SqliteDriverCommand AddValue(string value, bool hasNext)
        {
            Write($"({value}){(hasNext ? "," : "")}");
            return this;
        }

        public SqliteDriverCommand Values()
        {
            Write("VALUES");
            return this;
        }

        public SqliteDriverCommand Values(string[] values)
        {
            Write("VALUES");
            for (int i = 0;i < values.Length;i++)
            {
                var fld = values[i];
                AddValue(fld, i + 1 != values.Length);
            }
            return this;
        }

        public SqliteDriverCommand WriteUpdateOp(string column, UpdateOperationType operation, object value)
        {
            Write($"{column} = {(operation == UpdateOperationType.Set ? value : $"{column} {(operation == UpdateOperationType.Add ? "+" : "-")} {value}")}");
            return this;
        }

        public SqliteDriverCommand Update(string table)
        {
            Write($"UPDATE {table} SET");
            return this; 
        }

        public SqliteDriverCommand TableInfo(string name)
        {
            Write($"table_info({name})");
            return this;
        }

        public SqliteDriverCommand CreateColumn(SqliteDriverColumn clm, bool addColumn)
        {
            Write($"{(addColumn ? "ADD COLUMN" : "")} {clm.name} {SqliteDriverSerializer.ToSqlType(clm.type)} {(clm.primary ? "PRIMARY KEY" : "")} {(clm.notNull ? "NOT NULL" : "")} {(clm.foreign != null ? $"FOREIGN KEY REFERENCES {clm.foreign.referenceTable}({clm.foreign.referenceColumn})" : "")}");
            return this;
        }

        public SqliteDriverCommand AlterTable(string name)
        {
            Write("ALTER TABLE " + name);
            return this;
        }

        public SqliteDriverCommand DropTable(string name)
        {
            Write("DROP TABLE " + name);
            return this;
        }

        public SqliteDriverCommand CreateTable<T>(string table, SqliteDriverColumn[] columns)
        {
            Write($"CREATE TABLE {table}");
            Write("(");

            for (int i = 0; i < columns.Length; i++)
            {
                var clm = columns[i];
                CreateColumn(clm, false);
                if (i + 1 != columns.Length)
                    Write(",");
            }

            Write(")");
            return this;
        }

        public SqliteDriverCommand Pragma
        {
            get
            {
                Write("PRAGMA");
                return this;
            }
        }

        public SqliteDriverCommand Select(string[] columns, string table)
        {
            Write($"SELECT {string.Join(", ", columns)} FROM {table}");
            return this;
        }

        public SqliteDriverCommand Where()
        {
            Write("WHERE");
            return this;
        }

        public SqliteDriverCommand Eq(string column, object value)
        {
            Write($"{column} = {SqliteDriverSerializer.Sanitize(value, true)}");
            return this;
        }

        public SqliteDriverCommand NotEq(string column, object value)
        {
            Write($"{column} != {SqliteDriverSerializer.Sanitize(value, true)}");
            return this;
        }

        public SqliteDriverCommand StartsWith(string column, string value)
        {
            Write($"{column} {InjectNotClause()}LIKE '{SqliteDriverSerializer.Sanitize(value, false)}%'");
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
            Write($"{column} {InjectNotClause()}LIKE '%{SqliteDriverSerializer.Sanitize(value, false)}'");
            return this;
        }

        public SqliteDriverCommand Includes(string column, string value)
        {
            Write($"{column} {InjectNotClause()}LIKE '%{SqliteDriverSerializer.Sanitize(value, false)}%'");
            return this;
        }

        public SqliteDriverCommand Or()
        {
            Write("OR");
            return this;
        }

        public SqliteDriverCommand And()
        {
            Write("AND");
            return this;
        }

        public string GetSqlQuery() => query;

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
