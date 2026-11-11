using System.Text;
using System.Text.RegularExpressions;

namespace ChotWeb.Services
{
    public record MediaFile(string Name, string Url, string Kind, long Size, DateTimeOffset UploadedAt, bool InUse);

    public class MediaException(string message) : Exception(message);

    /// <summary>
    /// Uploaded photos and videos, stored in App_Data/uploads and served at /uploads/….
    /// Files get random names, and only real image/video files (checked by their first bytes) are accepted.
    /// </summary>
    public partial class MediaStore(StoragePaths paths, SiteContentStore content)
    {
        public const long MaxImageBytes = 15L * 1024 * 1024;
        public const long MaxVideoBytes = 300L * 1024 * 1024;
        public const string Image = "image";
        public const string Video = "video";

        private static readonly Dictionary<string, string> Kinds = new()
        {
            [".jpg"] = Image, [".jpeg"] = Image, [".png"] = Image, [".webp"] = Image, [".gif"] = Image, [".avif"] = Image,
            [".mp4"] = Video, [".m4v"] = Video, [".mov"] = Video, [".webm"] = Video,
        };

        [GeneratedRegex(@"^[a-f0-9]{32}\.[a-z0-9]{3,4}$")]
        private static partial Regex StoredName();

        public static string? KindOf(string fileName)
        {
            return Kinds.GetValueOrDefault(Path.GetExtension(fileName).ToLowerInvariant());
        }

        public IReadOnlyList<MediaFile> List(string? kind)
        {
            return new DirectoryInfo(paths.UploadsDir)
                .EnumerateFiles()
                .Where(f => StoredName().IsMatch(f.Name) && KindOf(f.Name) is not null)
                .Select(ToMediaFile)
                .Where(m => string.IsNullOrEmpty(kind) || m.Kind == kind)
                .OrderByDescending(m => m.UploadedAt)
                .ToList();
        }

        public async Task<MediaFile> SaveAsync(IFormFile file, CancellationToken cancellationToken)
        {
            string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            string kind = KindOf(file.FileName)
                ?? throw new MediaException("Only photos (JPG, PNG, WebP, GIF, AVIF) and videos (MP4, MOV, WebM) can be uploaded.");

            long max = kind == Video ? MaxVideoBytes : MaxImageBytes;
            if (file.Length == 0)
            {
                throw new MediaException("The file is empty.");
            }

            if (file.Length > max)
            {
                throw new MediaException($"That file is too large. The limit is {max / 1024 / 1024} MB for {kind}s.");
            }

            byte[] header = new byte[16];
            await using (Stream peek = file.OpenReadStream())
            {
                int read = await peek.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
                if (read < 12 || !LooksLike(ext, header))
                {
                    throw new MediaException("That file doesn't look like a real " + kind + ". Try exporting it again.");
                }
            }

            string name = Guid.NewGuid().ToString("N") + (ext == ".jpeg" ? ".jpg" : ext);
            string path = Path.Combine(paths.UploadsDir, name);
            await using (FileStream target = new(path, FileMode.CreateNew, FileAccess.Write))
            {
                await file.CopyToAsync(target, cancellationToken);
            }

            return ToMediaFile(new FileInfo(path));
        }

        public bool Delete(string name)
        {
            if (!StoredName().IsMatch(name))
            {
                return false;
            }

            string path = Path.Combine(paths.UploadsDir, name);
            if (!File.Exists(path))
            {
                return false;
            }

            File.Delete(path);
            return true;
        }

        private MediaFile ToMediaFile(FileInfo f)
        {
            string url = "~/uploads/" + f.Name;
            return new MediaFile(f.Name, url, KindOf(f.Name)!, f.Length, f.CreationTimeUtc, content.IsMediaInUse(url));
        }

        /// <summary>Checks the file's first bytes ("magic number") match its extension.</summary>
        private static bool LooksLike(string ext, byte[] b)
        {
            string ascii = Encoding.ASCII.GetString(b);
            return ext switch
            {
                ".jpg" or ".jpeg" => b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF,
                ".png" => b[0] == 0x89 && ascii[1..4] == "PNG",
                ".gif" => ascii.StartsWith("GIF8"),
                ".webp" => ascii.StartsWith("RIFF") && ascii[8..12] == "WEBP",
                ".avif" or ".mp4" or ".m4v" => ascii[4..8] == "ftyp",
                ".mov" => ascii[4..8] is "ftyp" or "moov" or "mdat" or "wide" or "free" or "skip",
                ".webm" => b[0] == 0x1A && b[1] == 0x45 && b[2] == 0xDF && b[3] == 0xA3,
                _ => false,
            };
        }
    }
}
