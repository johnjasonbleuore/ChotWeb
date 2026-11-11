using System.Text.RegularExpressions;
using ChotWeb.Models;

namespace ChotWeb.Services
{
    /// <summary>
    /// Cleans content coming from the designer before it is saved: trims and caps text,
    /// only allows safe links (no javascript: URLs), valid hex colours, known fonts/icons/styles,
    /// and makes sure every page section appears exactly once.
    /// </summary>
    public static partial class SiteContentSanitizer
    {
        private const int Short = 120;
        private const int Long = 1200;
        private const int MaxItems = 24;

        [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
        private static partial Regex HexColor();

        [GeneratedRegex(@"^~/(uploads|images)/[A-Za-z0-9._\-/]+$")]
        private static partial Regex LocalMedia();

        public static SiteContent Sanitize(SiteContent? input)
        {
            SiteContent d = SiteDefaults.Create();
            SiteContent c = input ?? d;

            ThemeSettings theme = c.Theme ?? d.Theme;
            HeaderSettings header = c.Header ?? d.Header;
            HeroSettings hero = c.Hero ?? d.Hero;
            VideosSection videos = c.Videos ?? d.Videos;
            PhotosSection photos = c.Photos ?? d.Photos;
            BrandsSection brands = c.Brands ?? d.Brands;
            ContactSection contact = c.Contact ?? d.Contact;
            FooterSettings footer = c.Footer ?? d.Footer;

            return new SiteContent
            {
                Theme = new ThemeSettings
                {
                    Primary = Color(theme.Primary, d.Theme.Primary),
                    Dark = Color(theme.Dark, d.Theme.Dark),
                    Text = Color(theme.Text, d.Theme.Text),
                    Surface = Color(theme.Surface, d.Theme.Surface),
                    Background = Color(theme.Background, d.Theme.Background),
                    DisplayFont = FontCatalog.Find(FontCatalog.Display, theme.DisplayFont).Name,
                    ScriptFont = FontCatalog.Find(FontCatalog.Script, theme.ScriptFont).Name,
                    BodyFont = FontCatalog.Find(FontCatalog.Body, theme.BodyFont).Name,
                },

                Sections = Sections(c.Sections, d.Sections),

                Header = new HeaderSettings
                {
                    SiteName = Text(header.SiteName, 60),
                    MediaKitLabel = Text(header.MediaKitLabel, 40),
                    MediaKitUrl = Link(header.MediaKitUrl),
                    CtaLabel = Text(header.CtaLabel, 40),
                },

                Hero = new HeroSettings
                {
                    TitleLine1 = Text(hero.TitleLine1, 24),
                    TitleLine2 = Text(hero.TitleLine2, 24),
                    Tagline = Text(hero.Tagline, 40),
                    PhotoUrl = Media(hero.PhotoUrl),
                    IntroTitle = Text(hero.IntroTitle, Short),
                    IntroText = Text(hero.IntroText, Long),
                    Specialties = Text(hero.Specialties, Short),
                    Location = Text(hero.Location, Short),
                    LocationBadge = Text(hero.LocationBadge, 8),
                    Availability = Text(hero.Availability, Short),
                    ShowDecorations = hero.ShowDecorations,
                },

                Stats = Items(c.Stats, 4, s => new StatItem
                {
                    Value = Text(s.Value, 16),
                    Label = Text(s.Label, 40),
                }),

                Socials = Items(c.Socials, 8, s => new SocialAccount
                {
                    Platform = Text(s.Platform, 40),
                    Icon = OneOf(s.Icon, SocialAccount.Icons, SocialAccount.IconTikTok),
                    Handle = Text(s.Handle, 60),
                    Count = Text(s.Count, 16),
                    CountLabel = Text(s.CountLabel, 30),
                    Url = Link(s.Url),
                }),

                Videos = new VideosSection
                {
                    TitleBold = Text(videos.TitleBold, 30),
                    TitleScript = Text(videos.TitleScript, 30),
                    LeftLabel = Text(videos.LeftLabel, 40),
                    RightLabel = Text(videos.RightLabel, 40),
                    Items = Items(videos.Items, MaxItems, v => new VideoItem
                    {
                        Title = Text(v.Title, 60),
                        Category = Text(v.Category, 40),
                        Duration = Text(v.Duration, 12),
                        Views = Text(v.Views, 24),
                        Tag = Text(v.Tag, 20),
                        Url = Link(v.Url),
                        ThumbnailUrl = Media(v.ThumbnailUrl),
                        VideoUrl = Media(v.VideoUrl),
                    }),
                },

                Photos = new PhotosSection
                {
                    TitleA = Text(photos.TitleA, 30),
                    TitleB = Text(photos.TitleB, 30),
                    Subtitle = Text(photos.Subtitle, 40),
                    Paragraphs = Strings(photos.Paragraphs, 6, Long),
                    Tags = Strings(photos.Tags, 16, 30),
                    ButtonLabel = Text(photos.ButtonLabel, 40),
                    GalleryUrl = Link(photos.GalleryUrl),
                    Portrait = Photo(photos.Portrait),
                    Items = Items(photos.Items, 16, Photo),
                },

                Brands = new BrandsSection
                {
                    TitleBold = Text(brands.TitleBold, 40),
                    TitleScript = Text(brands.TitleScript, 40),
                    LeftLabel = Text(brands.LeftLabel, 40),
                    RightLabel = Text(brands.RightLabel, 40),
                    ShowNote = brands.ShowNote,
                    Note = Text(brands.Note, 160),
                    Items = Items(brands.Items, 18, b => new BrandItem
                    {
                        Name = Text(b.Name, 40),
                        Style = OneOf(b.Style, BrandsSection.Styles, BrandsSection.Styles[0]),
                        LogoUrl = Media(b.LogoUrl),
                        Url = Link(b.Url),
                    }),
                },

                Contact = new ContactSection
                {
                    TitleBold = Text(contact.TitleBold, 40),
                    TitleScript = Text(contact.TitleScript, 40),
                    Text = Text(contact.Text, Long),
                    ButtonLabel = Text(contact.ButtonLabel, 40),
                    ButtonUrl = Link(contact.ButtonUrl),
                    Items = Items(contact.Items, 8, i => new ContactItem
                    {
                        Icon = OneOf(i.Icon, SocialAccount.Icons, "mail"),
                        Label = Text(i.Label, 40),
                        Value = Text(i.Value, Short),
                        Url = Link(i.Url),
                    }),
                },

                Footer = new FooterSettings
                {
                    Tagline = Text(footer.Tagline, 60),
                    Signature = Text(footer.Signature, 40),
                    ShowSocialLinks = footer.ShowSocialLinks,
                },
            };
        }

        private static List<SectionSettings> Sections(List<SectionSettings>? input, List<SectionSettings> defaults)
        {
            List<SectionSettings> result = [];
            foreach (SectionSettings s in input ?? [])
            {
                if (s is null || !SectionSettings.All.Contains(s.Id) || result.Any(r => r.Id == s.Id))
                {
                    continue;
                }

                result.Add(new SectionSettings
                {
                    Id = s.Id,
                    Visible = s.Visible,
                    ShowInNav = s.ShowInNav,
                    NavLabel = Text(s.NavLabel, 20),
                });
            }

            // Any section missing from the saved list is added back at its default position (end).
            result.AddRange(defaults.Where(d => result.All(r => r.Id != d.Id)));
            return result;
        }

        private static PhotoItem Photo(PhotoItem? p)
        {
            return new PhotoItem { Label = Text(p?.Label, 30), ImageUrl = Media(p?.ImageUrl) };
        }

        private static List<T> Items<T>(List<T>? items, int max, Func<T, T> map) where T : class
        {
            return (items ?? []).Where(i => i is not null).Take(max).Select(map).ToList();
        }

        private static List<string> Strings(List<string>? items, int max, int maxLength)
        {
            return (items ?? []).Select(s => Text(s, maxLength)).Where(s => s.Length > 0).Take(max).ToList();
        }

        private static string Text(string? value, int maxLength)
        {
            string s = (value ?? "").Trim();
            return s.Length > maxLength ? s[..maxLength] : s;
        }

        private static string Color(string? value, string fallback)
        {
            string s = (value ?? "").Trim();
            return HexColor().IsMatch(s) ? s.ToUpperInvariant() : fallback;
        }

        private static string OneOf(string? value, string[] allowed, string fallback)
        {
            return value is not null && allowed.Contains(value) ? value : fallback;
        }

        /// <summary>Links: http(s), mailto:, tel:, in-page #anchors, or files on this site (~/…).</summary>
        private static string Link(string? value)
        {
            string s = Text(value, 600);
            if (s.Length == 0 || s.StartsWith('#') || (s.StartsWith("~/") && !s.Contains("..")))
            {
                return s;
            }

            return Uri.TryCreate(s, UriKind.Absolute, out Uri? uri) && uri.Scheme is "http" or "https" or "mailto" or "tel" ? s : "";
        }

        /// <summary>Images/videos: uploaded files (~/uploads/…), files in wwwroot/images, or https URLs.</summary>
        private static string Media(string? value)
        {
            string s = Text(value, 600);
            if (s.Length == 0 || (LocalMedia().IsMatch(s) && !s.Contains("..")))
            {
                return s;
            }

            return Uri.TryCreate(s, UriKind.Absolute, out Uri? uri) && uri.Scheme == "https" ? s : "";
        }
    }
}
