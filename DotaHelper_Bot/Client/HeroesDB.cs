using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;

namespace DotaHelper_Bot
{
    public class HeroesDB
    {
        private SqlConnection connection = null;

        public HeroesDB()
        {
            Connect();
        }

        private void Connect() // Connection to the database
        {
            if (connection != null) return;

            connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DotaHeroesDB"].ConnectionString);

            connection.Open();
        }

        public Hero GetHeroData(string str)
        {
            Hero result = new Hero();

            // Search by abbreviated name
            result = SearchByReduction(str);

            if ( result.Name == null)
            {
                // We perform a strict search for the occurrence of a line
                result = RunSearchCommand(str);
                if (result.Name == null)
                {
                    // Search relative to the string occurrence
                    result = RunSearchCommand($"%{str}%");
                }
                else return result;
            }

            return result;
        }

        public void AddHeroesData(List<Hero> heroes)
        {
            ClearHeroTable();

            // Creating a script to fill the table
            string script = ""; 
            foreach (var hero in heroes)
            {
                script += $"INSERT INTO HeroesInfo(Name, PathToIcon) VALUES (N'{hero.Name}', N'{hero.Path}');\n";
            }

            // Script execution
            SqlCommand command = new SqlCommand(script, connection);
            command.ExecuteNonQuery();
        }

        private Hero RunSearchCommand(string scriptPart) // Search for the specified line in the Name field of the HeroesInfo table
        {
            Hero result = new Hero();
            SqlCommand command = new SqlCommand($"SELECT Name, PathToIcon FROM HeroesInfo WHERE Name LIKE '{scriptPart}';", connection);

            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    result.Name = reader["Name"].ToString();
                    result.Path = reader["PathToIcon"].ToString();
                }
            }

            return result;
        }

        private Hero SearchByReduction(string Name) // Strict search for occurrence of a string in the Name field of the HeroesInfo table
        {
            if (Name.Length > 4) return new Hero{ Name = null, Path = null };

            string script = "";
            int i = 0;

            // Bringing the reduction to the desired form
            while (i < Name.Length)
            {
                script += Name[i] + "% ";
                i++;
            }
            Hero result = RunSearchCommand(script.Trim());

            return result;
        }

        private void ClearHeroTable() // Clear the HeroesInfo table
        {
            try
            {
                // Delete the filled table
                SqlCommand commandDelete = new SqlCommand($"DROP TABLE HeroesInfo;", connection);
                commandDelete.ExecuteNonQuery();
            }
            catch { Console.WriteLine("The attempt to delete the table failed!"); }

            // Create a new clean table
            SqlCommand commandCreate = new SqlCommand(
                "CREATE TABLE [dbo].[HeroesInfo]\n " +
                "(\n [Id] INT IDENTITY(1, 1) NOT NULL, " +
                "\n  [Name] NVARCHAR(100) NOT NULL, " +
                "\n  [PathToIcon] NVARCHAR(100)  NOT NULL " +
                "\n);", connection);
            commandCreate.ExecuteNonQuery();
        }
    }
}