namespace ChotWeb.Models
{
    /// <summary>
    /// The starting content, used the first time the site runs and by "Restore defaults" in the designer.
    /// After that, edit the site in /admin — changes there are saved to App_Data/site-content.json.
    /// </summary>
    public static class SiteDefaults
    {
        private const string TikTokUrl = "https://www.tiktok.com/@chotumandal_";
        private const string InstagramUrl = "https://www.instagram.com/chotumandal/";
        private const string YouTubeUrl = "https://www.youtube.com/@chotumandal";
        private const string FacebookUrl = "https://www.facebook.com/UmandalChot";
        private const string Email = "collab@chotumandal.com";

        public static SiteContent Create()
        {
            return new SiteContent
            {
                Theme = new ThemeSettings(),

                Sections =
                [
                    new SectionSettings { Id = SectionSettings.Hero, NavLabel = "Home" },
                    new SectionSettings { Id = SectionSettings.Videos, NavLabel = "Videos" },
                    new SectionSettings { Id = SectionSettings.Photos, NavLabel = "Photos" },
                    new SectionSettings { Id = SectionSettings.Brands, NavLabel = "Brands" },
                    new SectionSettings { Id = SectionSettings.Contact, NavLabel = "Contact", ShowInNav = false },
                ],

                Header = new HeaderSettings
                {
                    SiteName = "Chot Umandal",
                    MediaKitLabel = "Media Kit",
                    MediaKitUrl = $"mailto:{Email}?subject=Media%20Kit%20request",
                    CtaLabel = "Let's Collab",
                },

                Hero = new HeroSettings
                {
                    TitleLine1 = "Chot",
                    TitleLine2 = "Umandal",
                    Tagline = "content creator",
                    IntroTitle = "Hi, I'm Chot!",
                    IntroText = "Lifestyle, beauty & everyday-girl content that makes your brand feel like a best friend's recommendation.",
                    Specialties = "UGC · Reels · TikTok",
                    Location = "Based in the Philippines",
                    LocationBadge = "PH",
                    Availability = "Open for paid collabs & PR packages",
                },

                // Counts are blank until the real numbers are entered; blank ones are hidden on the site.
                Stats =
                [
                    new StatItem { Label = "Total followers" },
                    new StatItem { Label = "Monthly views" },
                    new StatItem { Label = "Engagement" },
                ],

                Socials =
                [
                    new SocialAccount { Platform = "TikTok", Icon = "tiktok", Handle = "@chotumandal_", CountLabel = "followers", Url = TikTokUrl },
                    new SocialAccount { Platform = "Instagram", Icon = "instagram", Handle = "@chotumandal", CountLabel = "followers", Url = InstagramUrl },
                    new SocialAccount { Platform = "YouTube", Icon = "youtube", Handle = "Chot Umandal", CountLabel = "subscribers", Url = YouTubeUrl },
                    new SocialAccount { Platform = "Facebook", Icon = "facebook", Handle = "Chot Umandal", CountLabel = "followers", Url = FacebookUrl },
                ],

                Videos = new VideosSection
                {
                    TitleBold = "My",
                    TitleScript = "Videos",
                    LeftLabel = "Brand Campaigns",
                    RightLabel = "UGC & Reels",
                    Items =
                    [
                        new VideoItem { Title = "Skincare GRWM", Category = "Beauty", Duration = "0:45", Tag = "TikTok", Url = TikTokUrl },
                        new VideoItem { Title = "Unboxing Haul", Category = "Fashion", Duration = "0:30", Tag = "Reels", Url = InstagramUrl },
                        new VideoItem { Title = "Product Review", Category = "Lifestyle", Duration = "0:58", Tag = "UGC Ad", Url = TikTokUrl },
                        new VideoItem { Title = "Day in My Life", Category = "Vlog", Duration = "8:12", Tag = "YouTube", Url = YouTubeUrl },
                    ],
                },

                Photos = new PhotosSection
                {
                    TitleA = "My",
                    TitleB = "Photos",
                    Subtitle = "a little peek",
                    Paragraphs =
                    [
                        "I create soft, aesthetic and relatable content — from beauty routines and outfit hauls to food trips and everyday lifestyle moments.",
                        "Every shoot is styled, lit and edited by me, so your product looks pretty, real and ready to sell.",
                    ],
                    Tags = ["Beauty", "Skincare", "Fashion", "Lifestyle", "Food", "Product Photos", "Flat lays"],
                    ButtonLabel = "See full gallery",
                    GalleryUrl = InstagramUrl,
                    Portrait = new PhotoItem { Label = "Portrait" },
                    Items =
                    [
                        new PhotoItem { Label = "Flat lay" },
                        new PhotoItem { Label = "Beauty" },
                        new PhotoItem { Label = "OOTD" },
                        new PhotoItem { Label = "Product shoot" },
                    ],
                },

                Brands = new BrandsSection
                {
                    TitleBold = "Brands I've",
                    TitleScript = "Worked With",
                    LeftLabel = "Trusted by",
                    RightLabel = "Partners",
                    ShowNote = true,
                    Note = "Brand logos are placeholders — replace with your partner brands",
                    Items =
                    [
                        new BrandItem { Name = "Glow & Co.", Style = "glow" },
                        new BrandItem { Name = "Soleil", Style = "soleil" },
                        new BrandItem { Name = "Petal", Style = "petal" },
                        new BrandItem { Name = "blush.", Style = "blush" },
                        new BrandItem { Name = "Honey Bee", Style = "honey" },
                        new BrandItem { Name = "Luxe", Style = "luxe" },
                    ],
                },

                Contact = new ContactSection
                {
                    TitleBold = "Let's Work",
                    TitleScript = "together",
                    Text = "Open for UGC videos, sponsored posts, product reviews, event coverage and long-term ambassadorships. Send me your brief and let's create something pretty!",
                    ButtonLabel = "Book a Collab",
                    ButtonUrl = $"mailto:{Email}?subject=Collab%20inquiry",
                    Items =
                    [
                        new ContactItem { Icon = "mail", Label = "Email", Value = Email, Url = $"mailto:{Email}" },
                        new ContactItem { Icon = "phone", Label = "Phone / Viber", Value = "+63 9XX XXX XXXX" },
                        new ContactItem { Icon = "pin", Label = "Based in", Value = "Philippines · Available worldwide" },
                        new ContactItem { Icon = "heart", Label = "DM me on", Value = "@chotumandal · TikTok & IG", Url = TikTokUrl },
                    ],
                },

                Footer = new FooterSettings
                {
                    Tagline = "Content Creator",
                    Signature = "xoxo, Chot",
                },
            };
        }
    }
}
