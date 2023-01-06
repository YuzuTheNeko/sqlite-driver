using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SqliteDriver
{
    public class SqliteDriverColumn
    {
        public readonly string name;
        public readonly SqliteDriverDataType type;
        public readonly int index;
        public readonly Type realType;
        public readonly bool primary;
        public readonly bool notNull;
        public readonly ForeignKeyAttribute foreign;

        public SqliteDriverColumn(FieldInfo info, int index = -1)
        {
            this.index = index;
            realType = info.FieldType;
            type = SqliteDriverSerializer.GetDataTypeFor(info.FieldType);
            name = info.Name;
            primary = info.GetCustomAttribute<PrimaryKeyAttribute>() != null;
            notNull = info.GetCustomAttribute<NotNullAttribute>() != null;
            foreign = info.GetCustomAttribute<ForeignKeyAttribute>();
        }
    }

}