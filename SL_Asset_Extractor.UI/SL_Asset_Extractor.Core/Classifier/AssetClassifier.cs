using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace SL_Asset_Extractor.Core.Classifier
{
    public class SpecialCharacterConfig
    {
        public bool HasRunePattern { get; set; }
    }

    public class RulesConfig
    {
        public List<string> Characters { get; set; } = new();
        public List<string> Bosses { get; set; } = new();
        public Dictionary<string, SpecialCharacterConfig> SpecialCharacters { get; set; } = new();
        public List<string> MobPrefixes { get; set; } = new();
        public List<string> BannerKeywords { get; set; } = new();
        public string ItemMaterialPattern { get; set; } = "";
        public string ShadowPattern { get; set; } = "Shadow";
        public string DefaultCategory { get; set; } = "Unclassified";
    }

    public class ClassificationResult
    {
        public string Category { get; set; } = "Unclassified";
        public string? SubFolder { get; set; }
        public string? SubSubFolder { get; set; }
        public string FullPath => BuildFullPath();

        private string BuildFullPath()
        {
            var parts = new List<string> { Category };
            if (!string.IsNullOrEmpty(SubFolder)) parts.Add(SubFolder);
            if (!string.IsNullOrEmpty(SubSubFolder)) parts.Add(SubSubFolder);
            return Path.Combine(parts.ToArray());
        }
    }

    public class AssetClassifier
    {
        private RulesConfig _config = new();
        private Regex _skillPattern = null!;
        private Regex _runePattern = null!;
        private Regex _itemMaterialPattern = null!;

        public AssetClassifier(string rulesJsonPath)
        {
            LoadRules(rulesJsonPath);
        }

        private void LoadRules(string jsonPath)
        {
            var json = File.ReadAllText(jsonPath);
            _config = JsonConvert.DeserializeObject<RulesConfig>(json,
                new JsonSerializerSettings
                {
                    MetadataPropertyHandling = MetadataPropertyHandling.Ignore
                }) ?? new RulesConfig();
            CompilePatterns();
        }

        private void CompilePatterns()
        {
            _skillPattern = new Regex(
                @"^(.+?)_(\d+|QTE|qte|ultimate|supporter)(_.*)?$",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

            _runePattern = new Regex(
                @"^SungJinWoo_(\d+)_Rune$",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

            _itemMaterialPattern = new Regex(
                string.IsNullOrEmpty(_config.ItemMaterialPattern) ? "(?!)" : _config.ItemMaterialPattern,
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public void ReloadRules(string rulesJsonPath) => LoadRules(rulesJsonPath);

        public ClassificationResult Classify(string assetName)
        {
            // anniv
            if (assetName.StartsWith("1stAnniReport_", StringComparison.OrdinalIgnoreCase) ||
                assetName.StartsWith("1stAnniversary_", StringComparison.OrdinalIgnoreCase) ||
                assetName.IndexOf("1stAnniversary", StringComparison.OrdinalIgnoreCase) >= 0)
                return R("1st Anniversary");

            if (assetName.StartsWith("2stAnniversary", StringComparison.OrdinalIgnoreCase) ||
                assetName.IndexOf("2ndAnniversary", StringComparison.OrdinalIgnoreCase) >= 0)
                return R("2nd Anniversary");

            // event
            if (assetName.StartsWith("24Halloween_", StringComparison.OrdinalIgnoreCase))
                return R("Halloween 2024");

            if (assetName.StartsWith("25Spring_", StringComparison.OrdinalIgnoreCase))
                return R("Spring 2025");

            if (assetName.StartsWith("25SummerEvent_", StringComparison.OrdinalIgnoreCase))
                return R("Summer 2025");

            if (assetName.StartsWith("26NewYear_", StringComparison.OrdinalIgnoreCase))
                return R("New Year 2026");

            if (assetName.StartsWith("100thDay_", StringComparison.OrdinalIgnoreCase) ||
                assetName.IndexOf("100thDay", StringComparison.OrdinalIgnoreCase) >= 0)
                return R("100th Day");

            if (assetName.StartsWith("2024_", StringComparison.OrdinalIgnoreCase) ||
                assetName.StartsWith("2025_", StringComparison.OrdinalIgnoreCase) ||
                assetName.StartsWith("2026_", StringComparison.OrdinalIgnoreCase))
                return R("Holidays");

            // AchieveMedal
            if (assetName.StartsWith("AchieveMedal_", StringComparison.OrdinalIgnoreCase))
                return R("AchieveMedal");

            //Mobs 
            foreach (var prefix in _config.MobPrefixes)
            {
                if (assetName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return R("Mobs");
            }

            // Artefacts
            if (assetName.StartsWith("Artifact_", StringComparison.OrdinalIgnoreCase))
                return R("Artifacts");

            // Banners 
            if (assetName.StartsWith("Banner_", StringComparison.OrdinalIgnoreCase) ||
                assetName.StartsWith("banner_", StringComparison.OrdinalIgnoreCase))
                return R("Banners");

            // BG
            if (assetName.StartsWith("BG_", StringComparison.OrdinalIgnoreCase) ||
                assetName.StartsWith("Levelup_", StringComparison.OrdinalIgnoreCase))
                return R("Background");

            // BGM
            if (assetName.StartsWith("BGM", StringComparison.OrdinalIgnoreCase))
                return R("BGM");

            // Packs
            if (assetName.StartsWith("Big_", StringComparison.OrdinalIgnoreCase) ||
                assetName.StartsWith("Small_", StringComparison.OrdinalIgnoreCase))
                return R("Packs");

            // Buff
            if (assetName.StartsWith("buff_", StringComparison.OrdinalIgnoreCase) ||
                assetName.StartsWith("Buff_", StringComparison.OrdinalIgnoreCase))
                return R("Buff");

            // ChaHaeV2
            if (assetName.StartsWith("ChaHaeIn_MM", StringComparison.OrdinalIgnoreCase) ||
                assetName.StartsWith("ChaHaeInLegendButerrflyDress", StringComparison.OrdinalIgnoreCase))
                return new ClassificationResult
                {
                    Category = "Characters",
                    SubFolder = "ChaHaeIn Valkyrie"
                };

            // Core
            if (assetName.StartsWith("Core_", StringComparison.OrdinalIgnoreCase) ||
                assetName.StartsWith("Item_Core_", StringComparison.OrdinalIgnoreCase))
                return R("Core");

            // Currency
            if (assetName.StartsWith("Currency_", StringComparison.OrdinalIgnoreCase))
                return R("Currency");

            // DG
            if (assetName.StartsWith("DG_", StringComparison.OrdinalIgnoreCase) ||
                assetName.StartsWith("Dimensiongate_", StringComparison.OrdinalIgnoreCase) ||
                assetName.StartsWith("Item_DG_", StringComparison.OrdinalIgnoreCase))
                return R("DimensionGate");

            // Events
            if (assetName.StartsWith("Event_", StringComparison.OrdinalIgnoreCase) ||
                assetName.StartsWith("EventNoticePopup_", StringComparison.OrdinalIgnoreCase) ||
                assetName.StartsWith("EventPack_", StringComparison.OrdinalIgnoreCase) ||
                assetName.StartsWith("EventPuzzel_", StringComparison.OrdinalIgnoreCase) ||
                assetName.StartsWith("EventTap_", StringComparison.OrdinalIgnoreCase) ||
                assetName.StartsWith("EventTrust", StringComparison.OrdinalIgnoreCase) ||
                assetName.StartsWith("EvJeju", StringComparison.OrdinalIgnoreCase) ||
                assetName.StartsWith("EvLR_", StringComparison.OrdinalIgnoreCase) ||
                assetName.StartsWith("EvSH_", StringComparison.OrdinalIgnoreCase) ||
                assetName.StartsWith("EvSummer_", StringComparison.OrdinalIgnoreCase))
                return R("Events");

            // Collab Frieren
            if (assetName.StartsWith("FRRCollab_", StringComparison.OrdinalIgnoreCase))
                return R("Collab Frieren");

            // Pull banners 
            if (assetName.StartsWith("Gacha_", StringComparison.OrdinalIgnoreCase))
                return R("Portails Invoc");

            // Weapons
            if (assetName.StartsWith("GSWeapon_", StringComparison.OrdinalIgnoreCase) ||
                assetName.StartsWith("GSWeapons_", StringComparison.OrdinalIgnoreCase) ||
                assetName.StartsWith("Relic_", StringComparison.OrdinalIgnoreCase))
                return R("Weapons");

            // Icons
            if (assetName.StartsWith("Icon_", StringComparison.OrdinalIgnoreCase))
                return R("Icons");

            // Collab IDLE
            if (assetName.StartsWith("IdleCollab1_", StringComparison.OrdinalIgnoreCase) ||
                assetName.StartsWith("IdleCollab2_", StringComparison.OrdinalIgnoreCase))
                return R("Collab IDLE");

            // Items
            if (assetName.StartsWith("Item_ArtifactEnchant_", StringComparison.OrdinalIgnoreCase))
                return R("Books");

            if (assetName.StartsWith("Item_ArtifactReforge_", StringComparison.OrdinalIgnoreCase))
                return R("Forge");

            if (assetName.StartsWith("Item_ArtiGem_", StringComparison.OrdinalIgnoreCase) ||
                assetName.StartsWith("Item_GemGroup_", StringComparison.OrdinalIgnoreCase) ||
                assetName.StartsWith("Item_GemUpgrade_", StringComparison.OrdinalIgnoreCase))
                return R("Gems");

            if (assetName.StartsWith("Item_", StringComparison.OrdinalIgnoreCase))
                return R("Items");

            // Lobby
            if (assetName.StartsWith("Lobby_", StringComparison.OrdinalIgnoreCase))
                return R("Lobby");

            // Mission
            if (assetName.StartsWith("Mission_", StringComparison.OrdinalIgnoreCase))
                return R("Missions");

            // Runes
            if (assetName.StartsWith("Rune_", StringComparison.OrdinalIgnoreCase))
                return R("Runes");

            // Atlas
            if (assetName.StartsWith("sactx-", StringComparison.OrdinalIgnoreCase))
                return R("Atlas");

            // Shadows
            if (assetName.StartsWith("Shadow_", StringComparison.OrdinalIgnoreCase))
                return R("Shadows");

            // Shop
            if (assetName.StartsWith("Shop_", StringComparison.OrdinalIgnoreCase) ||
                assetName.StartsWith("ShopGoods_", StringComparison.OrdinalIgnoreCase) ||
                assetName.StartsWith("ShopGoodsSlot", StringComparison.OrdinalIgnoreCase) ||
                assetName.StartsWith("ShopIcon_", StringComparison.OrdinalIgnoreCase) ||
                assetName.StartsWith("ShopRecommend_", StringComparison.OrdinalIgnoreCase) ||
                assetName.StartsWith("ShopTab_", StringComparison.OrdinalIgnoreCase))
                return R("Shop");

            // Skins
            if (assetName.StartsWith("Skin_", StringComparison.OrdinalIgnoreCase) ||
                assetName.StartsWith("SkinSlot_", StringComparison.OrdinalIgnoreCase))
            {
                var afterSkin = assetName.StartsWith("Skin_", StringComparison.OrdinalIgnoreCase)
                    ? assetName.Substring(5)
                    : assetName.Substring(9);

                var matchedChar = FindCharacterFromStart(afterSkin);
                if (matchedChar != null)
                    return new ClassificationResult
                    {
                        Category = "Characters",
                        SubFolder = matchedChar
                    };

                return R("Skins");
            }

            // Tutorial
            if (assetName.StartsWith("Tutorial_", StringComparison.OrdinalIgnoreCase))
                return R("Tutorial");

            // Rune SJW
            if (_runePattern.IsMatch(assetName))
                return new ClassificationResult
                {
                    Category = "Characters",
                    SubFolder = "SungJinWoo",
                    SubSubFolder = "Skills"
                };

            // Skills
            var skillMatch = _skillPattern.Match(assetName);
            if (skillMatch.Success)
            {
                var matchedChar = FindCharacter(skillMatch.Groups[1].Value);
                if (matchedChar != null)
                    return new ClassificationResult
                    {
                        Category = "Characters",
                        SubFolder = matchedChar,
                        SubSubFolder = "Skills"
                    };
            }

            // Characters
            var charFromStart = FindCharacterFromStart(assetName);
            if (charFromStart != null)
                return new ClassificationResult
                {
                    Category = "Characters",
                    SubFolder = charFromStart
                };

            // Boss
            var boss = FindBoss(assetName);
            if (boss != null)
                return new ClassificationResult
                {
                    Category = "Bosses",
                    SubFolder = boss
                };

            return new ClassificationResult { Category = _config.DefaultCategory };
        }

        private static ClassificationResult R(string category) =>
            new ClassificationResult { Category = category };

        private bool IsBanner(string assetName) =>
            _config.BannerKeywords.Any(kw => assetName.StartsWith(kw, StringComparison.OrdinalIgnoreCase));

        private string? FindCharacterFromStart(string assetName) =>
            _config.Characters.FirstOrDefault(c => assetName.StartsWith(c, StringComparison.OrdinalIgnoreCase));

        private string? FindCharacter(string name) =>
            _config.Characters.FirstOrDefault(c => string.Equals(c, name, StringComparison.OrdinalIgnoreCase));

        private string? FindBoss(string assetName) =>
            _config.Bosses.FirstOrDefault(b => assetName.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0);

        public List<string> GetAllCategories() => new()
        {
            "1st Anniversary", "2nd Anniversary", "Halloween 2024", "Spring 2025",
            "Summer 2025", "New Year 2026", "100th Day", "Holidays", "AchieveMedal",
            "Characters", "Bosses", "Mobs", "Artifacts", "Banners", "Background",
            "BGM", "Packs", "Buff", "Core", "Currency", "DimensionGate", "Events",
            "Collab Frieren", "Portails Invoc", "Weapons", "Icons", "Collab IDLE",
            "Books", "Forge", "Gems", "Items", "Lobby", "Missions", "Runes",
            "Atlas", "Shadows", "Shop", "Skins", "Tutorial", _config.DefaultCategory
        };
    }
}