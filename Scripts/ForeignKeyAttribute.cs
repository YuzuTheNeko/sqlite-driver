using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SqliteDriver
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ForeignKeyAttribute : Attribute
    {
        public string referenceTable;
        public string referenceColumn;

        public ForeignKeyAttribute(string referenceTable, string referenceColumn)
        {
            this.referenceColumn = referenceColumn;
            this.referenceTable = referenceTable;
        }
    }
}
