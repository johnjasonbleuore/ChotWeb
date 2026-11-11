namespace ChotWeb.Models
{
    /// <param name="Name">Font family name as shown in the designer.</param>
    /// <param name="GoogleSpec">The family=… value for the Google Fonts CSS2 URL.</param>
    /// <param name="Fallback">CSS fallback stack.</param>
    /// <param name="Weight">font-weight to use for this role.</param>
    public record FontOption(string Name, string GoogleSpec, string Fallback, int Weight = 400);

    /// <summary>The fonts the designer can choose from. Only these are allowed (keeps the CSS safe).</summary>
    public static class FontCatalog
    {
        private const string BodyWeights = ":ital,wght@0,300;0,400;0,500;0,600;0,700;1,700";

        // Big condensed headline fonts (wide fonts would overflow the giant hero name).
        public static readonly FontOption[] Display =
        [
            new("Anton", "Anton", "Impact, \"Arial Narrow\", sans-serif"),
            new("Bebas Neue", "Bebas+Neue", "Impact, \"Arial Narrow\", sans-serif"),
            new("Oswald", "Oswald:wght@600", "Impact, \"Arial Narrow\", sans-serif", 600),
            new("League Gothic", "League+Gothic", "Impact, \"Arial Narrow\", sans-serif"),
            new("Fjalla One", "Fjalla+One", "Impact, \"Arial Narrow\", sans-serif"),
        ];

        public static readonly FontOption[] Script =
        [
            new("Yellowtail", "Yellowtail", "\"Brush Script MT\", cursive"),
            new("Pacifico", "Pacifico", "\"Brush Script MT\", cursive"),
            new("Dancing Script", "Dancing+Script:wght@600", "\"Brush Script MT\", cursive", 600),
            new("Great Vibes", "Great+Vibes", "\"Brush Script MT\", cursive"),
            new("Sacramento", "Sacramento", "\"Brush Script MT\", cursive"),
            new("Satisfy", "Satisfy", "\"Brush Script MT\", cursive"),
        ];

        public static readonly FontOption[] Body =
        [
            new("Poppins", "Poppins" + BodyWeights, "system-ui, -apple-system, \"Segoe UI\", sans-serif"),
            new("Plus Jakarta Sans", "Plus+Jakarta+Sans" + BodyWeights, "system-ui, -apple-system, \"Segoe UI\", sans-serif"),
            new("DM Sans", "DM+Sans" + BodyWeights, "system-ui, -apple-system, \"Segoe UI\", sans-serif"),
            new("Nunito", "Nunito" + BodyWeights, "system-ui, -apple-system, \"Segoe UI\", sans-serif"),
            new("Montserrat", "Montserrat" + BodyWeights, "system-ui, -apple-system, \"Segoe UI\", sans-serif"),
            new("Quicksand", "Quicksand:wght@300;400;500;600;700", "system-ui, -apple-system, \"Segoe UI\", sans-serif"),
        ];

        public static FontOption Find(FontOption[] options, string? name)
        {
            return options.FirstOrDefault(f => f.Name == name) ?? options[0];
        }

        public static string GoogleFontsUrl(params FontOption[] fonts)
        {
            return "https://fonts.googleapis.com/css2?" +
                   string.Join("&", fonts.Select(f => "family=" + f.GoogleSpec).Distinct()) +
                   "&display=swap";
        }
    }
}
