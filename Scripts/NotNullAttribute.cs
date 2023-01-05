using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SqliteDriver
{
    [AttributeUsage(AttributeTargets.Field)]
    public class NotNullAttribute : Attribute
    { }

}