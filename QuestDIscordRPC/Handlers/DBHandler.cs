using System;
using System.Data.SQLite;
using System.IO;
using QuestDiscordRPC.Objects;

namespace QuestDiscordRPC.Handlers;

public class DBHandler
{
    private const string connectionString = "Data Source=cache/AppInfo.db;Version=3;";
    
    public DBHandler()
    {
        if (!File.Exists("cache/AppInfo.db"))
        {
            SQLiteConnection.CreateFile("cache/AppInfo.db");
        }

        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();

            var createTableQuery = "CREATE TABLE IF NOT EXISTS AppInfo (" +
                                      "Id INTEGER PRIMARY KEY AUTOINCREMENT," +
                                      "PackageName TEXT NOT NULL," +
                                      "AppName TEXT NOT NULL," +
                                      "ImageURL TEXT NULL)";

            using (var command = new SQLiteCommand(createTableQuery, connection))
            {
                command.ExecuteNonQuery();
            }

            connection.Close();
        }
    }
    
    internal void Insert(string packageName, string appName, string imageURL)
    {
        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();

            var insertQuery = "INSERT INTO AppInfo (PackageName, AppName, ImageURL) VALUES (@PackageName, @AppName, @ImageURL)";

            using (var command = new SQLiteCommand(insertQuery, connection))
            {
                command.Parameters.AddWithValue("@PackageName", packageName);
                command.Parameters.AddWithValue("@AppName", appName);
                
                if (imageURL != null)
                {
                    command.Parameters.AddWithValue("@ImageURL", imageURL);
                }
                else
                {
                    command.Parameters.AddWithValue("@ImageURL", DBNull.Value);
                }

                command.ExecuteNonQuery();
            }

            connection.Close();
        }
    }

    internal DBObject Select(string? PackageName = null, string? AppName = null)
    {
        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();

            string selectQuery;
            if (PackageName != null)
            {
                selectQuery = "SELECT * FROM AppInfo WHERE PackageName = @PackageName";
            }
            else if (AppName != null)
            {
                selectQuery = "SELECT * FROM AppInfo WHERE AppName = @AppName";
            }
            else
            {
                selectQuery = "SELECT * FROM AppInfo";
            }

            using (var command = new SQLiteCommand(selectQuery, connection))
            {
                if (PackageName != null)
                {
                    command.Parameters.AddWithValue("@PackageName", PackageName);
                }
                else if (AppName != null)
                {
                    command.Parameters.AddWithValue("@AppName", AppName);
                }
                
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var id = reader.GetInt32(0);
                        var packageName = reader.GetString(1);
                        var appName = reader.GetString(2);
                        var imageURL = reader.IsDBNull(3) ? null : reader.GetString(3);
                        var dbObject = new DBObject(id, packageName, appName, imageURL);
                        
                        connection.Close();
                        
                        return dbObject;
                    }
                }
            }

            connection.Close();
            return null;
        }
    }
    
    internal void Delete(int id)
    {
        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();

            var deleteQuery = "DELETE FROM AppInfo WHERE Id = @Id";

            using (var command = new SQLiteCommand(deleteQuery, connection))
            {
                command.Parameters.AddWithValue("@Id", id);

                command.ExecuteNonQuery();
            }

            connection.Close();
        }
    }

    internal void Update(int id, string newPackageName, string newAppName, string newImageURL = null)
    {
        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();

            var updateQuery = "UPDATE AppInfo SET PackageName = @NewPackageName, AppName = @NewAppName, ImageURL = @NewImageURL WHERE Id = @Id";

            using (var command = new SQLiteCommand(updateQuery, connection))
            {
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@NewPackageName", newPackageName);
                command.Parameters.AddWithValue("@NewAppName", newAppName);
                
                if (newImageURL != null)
                {
                    command.Parameters.AddWithValue("@NewImageURL", newImageURL);
                }
                else
                {
                    command.Parameters.AddWithValue("@NewImageURL", DBNull.Value);
                }

                command.ExecuteNonQuery();
            }

            connection.Close();
        }
    }

    internal void ClearImageCache()
    {
        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();

            var updateQuery = "UPDATE AppInfo SET ImageURL = @ImageURL";

            using (var command = new SQLiteCommand(updateQuery, connection))
            {
                command.Parameters.AddWithValue("@ImageURL", null);

                command.ExecuteNonQuery();
            }

            connection.Close();
        }
    }
}