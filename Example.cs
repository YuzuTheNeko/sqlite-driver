using SqliteDriver;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class User
{
    [PrimaryKey]
    public uint id;
    public uint money = 0;
}

public class DatabaseTables
{
    public User users;
}

public class Example : MonoBehaviour
{
    void Start()
    {
        var driver = new SqliteDriver<DatabaseTables>("db.sqlite");
        
        // Open connection to database
        driver.Open();

        // Insert a new user to database table
        driver.Insert(new User
        {
            id = 1
        });

        // Retrieve the user we inserted
        var user = driver.Get<User>(new SqliteDriverQueryOptions
        {
            where = new SqliteDriverWhereOptions[]
            {
                SqliteDriverWhereOptions.Eq(nameof(User.id), 1)
            }
        });

        Debug.Log($"User id {user.id} with money {user.money}");

        // Close the connection when we dont need it anymore.
        driver.Close();
    }
}
