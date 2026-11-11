namespace ChotWeb.Models
{
    /// <summary>
    /// Everything the public site shows: text, links, media, layout and theme.
    /// Stored as JSON in App_Data (see SiteContentStore) and edited in /admin.
    /// </summary>
    public class SiteContent
    {
        public ThemeSettings Theme { get; set; } = new();
        public List<SectionSettings> Sections { get; set; } = [];
        public HeaderSettings Header { get; set; } = new();
        public HeroSettings Hero { get; set; } = new();
        public List<StatItem> Stats { get; set; } = [];
        public List<SocialAccount> Socials { get; set; } = [];
        public VideosSection Videos { get; set; } = new();
        public PhotosSection Photos { get; set; } = new();
        public BrandsSection Brands { get; set; } = new();
        public ContactSection Contact { get; set; } = new();
        public FooterSettings Footer { get; set; } = new();
    }

    public class ThemeSettings
    {
        public string Primary { get; set; } = "#FF7B22";
        public string Dark { get; set; } = "#3A1A0B";
        public string Text { get; set; } = "#2E1D14";
        public string Surface { get; set; } = "#FFF4EC";
        public string Background { get; set; } = "#FFFFFF";
        public string DisplayFont { get; set; } = "Anton";
        public string ScriptFont { get; set; } = "Yellowtail";
        public string BodyFont { get; set; } = "Poppins";
    }

    /// <summary>One page section. The order of the list is the order on the page.</summary>
    public class SectionSettings
    {
        public const string Hero = "hero";
        public const string Videos = "videos";
        public const string Photos = "photos";
        public const string Brands = "brands";
        public const string Contact = "contact";
        public static readonly string[] All = [Hero, Videos, Photos, Brands, Contact];

        public string Id { get; set; } = "";
        public bool Visible { get; set; } = true;
        public bool ShowInNav { get; set; } = true;
        public string NavLabel { get; set; } = "";

        /// <summary>The #anchor used in links (the hero is "#home").</summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public string Anchor => Id == Hero ? "home" : Id;
    }

    public class HeaderSettings
    {
        public string SiteName { get; set; } = "";
        public string MediaKitLabel { get; set; } = "";
        public string MediaKitUrl { get; set; } = "";
        public string CtaLabel { get; set; } = "";
    }

    public class HeroSettings
    {
        public string TitleLine1 { get; set; } = "";
        public string TitleLine2 { get; set; } = "";
        public string Tagline { get; set; } = "";
        public string PhotoUrl { get; set; } = "";
        public string IntroTitle { get; set; } = "";
        public string IntroText { get; set; } = "";
        public string Specialties { get; set; } = "";
        public string Location { get; set; } = "";
        public string LocationBadge { get; set; } = "";
        public string Availability { get; set; } = "";
        public bool ShowDecorations { get; set; } = true;
    }

    public class StatItem
    {
        public string Value { get; set; } = "";
        public string Label { get; set; } = "";
    }

    public class SocialAccount
    {
        public const string IconTikTok = "tiktok";
        public static readonly string[] Icons = ["tiktok", "instagram", "youtube", "facebook", "mail", "phone", "pin", "heart"];

        public string Platform { get; set; } = "";
        public string Icon { get; set; } = IconTikTok;
        public string Handle { get; set; } = "";
        public string Count { get; set; } = "";
        public string CountLabel { get; set; } = "";
        public string Url { get; set; } = "";
    }

    public class VideosSection
    {
        public string TitleBold { get; set; } = "";
        public string TitleScript { get; set; } = "";
        public string LeftLabel { get; set; } = "";
        public string RightLabel { get; set; } = "";
        public List<VideoItem> Items { get; set; } = [];
    }

    public class VideoItem
    {
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public string Duration { get; set; } = "";
        public string Views { get; set; } = "";
        public string Tag { get; set; } = "";
        /// <summary>External link (TikTok, YouTube…). Used when no uploaded video is set.</summary>
        public string Url { get; set; } = "";
        public string ThumbnailUrl { get; set; } = "";
        /// <summary>Uploaded video file; plays in a pop-up player on the site.</summary>
        public string VideoUrl { get; set; } = "";
    }

    public class PhotosSection
    {
        public string TitleA { get; set; } = "";
        public string TitleB { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public List<string> Paragraphs { get; set; } = [];
        public List<string> Tags { get; set; } = [];
        public string ButtonLabel { get; set; } = "";
        public string GalleryUrl { get; set; } = "";
        public PhotoItem Portrait { get; set; } = new();
        public List<PhotoItem> Items { get; set; } = [];
    }

    public class PhotoItem
    {
        public string Label { get; set; } = "";
        public string ImageUrl { get; set; } = "";
    }

    public class BrandsSection
    {
        public static readonly string[] Styles = ["glow", "soleil", "petal", "blush", "honey", "luxe"];

        public string TitleBold { get; set; } = "";
        public string TitleScript { get; set; } = "";
        public string LeftLabel { get; set; } = "";
        public string RightLabel { get; set; } = "";
        public bool ShowNote { get; set; }
        public string Note { get; set; } = "";
        public List<BrandItem> Items { get; set; } = [];
    }

    public class BrandItem
    {
        public string Name { get; set; } = "";
        /// <summary>Look of the text logo when no LogoUrl is set (see BrandsSection.Styles).</summary>
        public string Style { get; set; } = "glow";
        public string LogoUrl { get; set; } = "";
        public string Url { get; set; } = "";
    }

    public class ContactSection
    {
        public string TitleBold { get; set; } = "";
        public string TitleScript { get; set; } = "";
        public string Text { get; set; } = "";
        public string ButtonLabel { get; set; } = "";
        public string ButtonUrl { get; set; } = "";
        public List<ContactItem> Items { get; set; } = [];
    }

    public class ContactItem
    {
        public string Icon { get; set; } = "mail";
        public string Label { get; set; } = "";
        public string Value { get; set; } = "";
        public string Url { get; set; } = "";
    }

    public class FooterSettings
    {
        public string Tagline { get; set; } = "";
        public string Signature { get; set; } = "";
        public bool ShowSocialLinks { get; set; } = true;
    }
}
