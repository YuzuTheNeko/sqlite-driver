using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SqliteDriver
{
    public class SqliteDriverColumn
    {
        public readonly string name;
        public readonly SqliteDriverDataType type;
        public readonly int index;
        public readonly Type realType;

        public SqliteDriverColumn(string name, int index, Type realType, SqliteDriverDataType type)
        {
            this.realType = realType;
            this.name = name;
            this.index = index;
            this.type = type;
        }
    }

}