namespace AgileBot
{
    using System;
    using System.IO;

    /// <summary>
    /// Loading and parsing of .env file.
    /// All credit for this class goes to Dustin Moris Gorski
    /// https://dusted.codes/dotenv-in-dotnet
    /// </summary>
    public static class DotEnv
    {
        public static void Load(string filePath)
        {
            if (!File.Exists(filePath))
                return;
            int i = 1;
            foreach (var line in File.ReadAllLines(filePath))
            {
                var parts = line.Split(
                    '=',
                    2,
                    StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != 2)
                    continue;

                Environment.SetEnvironmentVariable(parts[0], parts[1]);
            }
        }
    }
}