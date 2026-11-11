namespace ChotWeb.Services
{
    /// <summary>
    /// Where the site keeps data that changes at runtime: content JSON, the admin account,
    /// uploaded media and the sign-in encryption keys. Default: {project}/App_Data.
    /// Change with "Storage:DataPath" in appsettings.json (relative or absolute path).
    /// </summary>
    public class StoragePaths
    {
        public StoragePaths(string contentRootPath, string? dataPath)
        {
            DataDir = Path.GetFullPath(Path.Combine(contentRootPath, string.IsNullOrWhiteSpace(dataPath) ? "App_Data" : dataPath));
            UploadsDir = Path.Combine(DataDir, "uploads");
            KeysDir = Path.Combine(DataDir, "keys");

            Directory.CreateDirectory(DataDir);
            Directory.CreateDirectory(UploadsDir);
            Directory.CreateDirectory(KeysDir);
        }

        public string DataDir { get; }
        public string UploadsDir { get; }
        public string KeysDir { get; }

        public string ContentFile => Path.Combine(DataDir, "site-content.json");
        public string DraftFile => Path.Combine(DataDir, "site-content.draft.json");
        public string AccountFile => Path.Combine(DataDir, "admin.json");
        public string SetupCodeFile => Path.Combine(DataDir, "setup-code.txt");
    }

    internal static class AtomicFile
    {
        /// <summary>Writes to a temp file first, then swaps it in, so a crash never leaves a half-written file.</summary>
        public static void WriteAllText(string path, string contents)
        {
            string temp = path + ".tmp";
            File.WriteAllText(temp, contents);
            File.Move(temp, path, overwrite: true);
        }
    }
}
