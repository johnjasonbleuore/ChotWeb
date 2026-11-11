using System.Text.Json;
using ChotWeb.Models;

namespace ChotWeb.Services
{
    /// <summary>
    /// Holds the site content. There are two copies:
    ///  • live  (App_Data/site-content.json)       — what visitors see
    ///  • draft (App_Data/site-content.draft.json) — what the designer edits and previews
    /// Publishing copies the draft over the live copy.
    /// </summary>
    public class SiteContentStore
    {
        public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

        private readonly StoragePaths _paths;
        private readonly ILogger<SiteContentStore> _logger;
        private readonly Lock _lock = new();

        private SiteContent _live;
        private string _liveJson;
        private SiteContent _draft;
        private string _draftJson;

        public SiteContentStore(StoragePaths paths, ILogger<SiteContentStore> logger)
        {
            _paths = paths;
            _logger = logger;

            _live = Load(paths.ContentFile) ?? SiteContentSanitizer.Sanitize(SiteDefaults.Create());
            _liveJson = Serialize(_live);
            if (!File.Exists(paths.ContentFile))
            {
                AtomicFile.WriteAllText(paths.ContentFile, _liveJson);
            }

            _draft = Load(paths.DraftFile) ?? Clone(_live);
            _draftJson = Serialize(_draft);
            DraftSavedAt = File.Exists(paths.DraftFile) ? File.GetLastWriteTimeUtc(paths.DraftFile) : null;
            PublishedAt = File.GetLastWriteTimeUtc(paths.ContentFile);
        }

        public DateTimeOffset? DraftSavedAt { get; private set; }
        public DateTimeOffset? PublishedAt { get; private set; }

        public SiteContent GetLive()
        {
            return _live;
        }

        public SiteContent GetDraft()
        {
            return _draft;
        }

        public bool HasUnpublishedChanges
        {
            get
            {
                lock (_lock)
                {
                    return _draftJson != _liveJson;
                }
            }
        }

        public void SaveDraft(SiteContent content)
        {
            SiteContent clean = SiteContentSanitizer.Sanitize(content);
            string json = Serialize(clean);
            lock (_lock)
            {
                AtomicFile.WriteAllText(_paths.DraftFile, json);
                _draft = clean;
                _draftJson = json;
                DraftSavedAt = DateTimeOffset.UtcNow;
            }
        }

        public void Publish()
        {
            lock (_lock)
            {
                AtomicFile.WriteAllText(_paths.ContentFile, _draftJson);
                _live = Clone(_draft);
                _liveJson = _draftJson;
                PublishedAt = DateTimeOffset.UtcNow;
            }

            _logger.LogInformation("Site content published.");
        }

        public void DiscardDraft()
        {
            lock (_lock)
            {
                AtomicFile.WriteAllText(_paths.DraftFile, _liveJson);
                _draft = Clone(_live);
                _draftJson = _liveJson;
                DraftSavedAt = DateTimeOffset.UtcNow;
            }
        }

        public void ResetDraftToDefaults()
        {
            SaveDraft(SiteDefaults.Create());
        }

        /// <summary>True when the live or draft content refers to this media URL.</summary>
        public bool IsMediaInUse(string url)
        {
            lock (_lock)
            {
                return _liveJson.Contains(url, StringComparison.Ordinal) || _draftJson.Contains(url, StringComparison.Ordinal);
            }
        }

        private SiteContent? Load(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                SiteContent? content = JsonSerializer.Deserialize<SiteContent>(File.ReadAllText(path), JsonOptions);
                return SiteContentSanitizer.Sanitize(content);
            }
            catch (Exception ex) when (ex is JsonException or IOException)
            {
                string backup = path + ".broken-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                File.Copy(path, backup, overwrite: true);
                _logger.LogError(ex, "Could not read {Path}; a copy was saved as {Backup} and defaults are used instead.", path, backup);
                return null;
            }
        }

        private static string Serialize(SiteContent content)
        {
            return JsonSerializer.Serialize(content, JsonOptions);
        }

        private static SiteContent Clone(SiteContent content)
        {
            return JsonSerializer.Deserialize<SiteContent>(Serialize(content), JsonOptions)!;
        }
    }
}
