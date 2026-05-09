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
        public List<string> BannerKeywords { get; set; } = new();
        public string ItemMaterialPattern { get; set; } = "";
        public string ShadowPattern { get; set; } = "Shadow";
        public string DefaultCategory { get; set; } = "Unclassified";
    }


    public class ClassificationResult
    {
        // Catégorie principale : "Characters", "Bosses", "Shadow", "Banners", etc.
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

        // regex
        private Regex _skillPattern = null!;
        private Regex _runePattern = null!;
        private Regex _skinPattern = null!;
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

            _skinPattern = new Regex(
                @"^Skin_(.+)$",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

            _itemMaterialPattern = new Regex(
                _config.ItemMaterialPattern,
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public void ReloadRules(string rulesJsonPath)
        {
            LoadRules(rulesJsonPath);
        }

        public ClassificationResult Classify(string assetName)
        {
            if (ContainsShadow(assetName))
            {
                return new ClassificationResult
                {
                    Category = "Shadow",
                    SubFolder = null
                };
            }

            if (IsBanner(assetName))
            {
                return new ClassificationResult { Category = "Banners" };
            }

            if (_itemMaterialPattern.IsMatch(assetName))
            {
                return new ClassificationResult { Category = "ItemMaterials" };
            }

            var skinMatch = _skinPattern.Match(assetName);
            if (skinMatch.Success)
            {
                var skinCharName = skinMatch.Groups[1].Value;
                var matchedChar = FindCharacter(skinCharName);

                if (matchedChar != null)
                {
                    return new ClassificationResult
                    {
                        Category = "Characters",
                        SubFolder = matchedChar
                    };
                }
            }

            var skillMatch = _skillPattern.Match(assetName);
            if (skillMatch.Success)
            {
                var baseName = skillMatch.Groups[1].Value;
                var matchedChar = FindCharacter(baseName);

                if (matchedChar != null)
                {
                    if (_runePattern.IsMatch(assetName))
                    {
                        return new ClassificationResult
                        {
                            Category = "Characters",
                            SubFolder = matchedChar,
                            SubSubFolder = "Skills"
                        };
                    }

                    return new ClassificationResult
                    {
                        Category = "Characters",
                        SubFolder = matchedChar,
                        SubSubFolder = "Skills"
                    };
                }
            }

            var charFromStart = FindCharacterFromStart(assetName);
            if (charFromStart != null)
            {
                return new ClassificationResult
                {
                    Category = "Characters",
                    SubFolder = charFromStart
                };
            }

            var boss = FindBoss(assetName);
            if (boss != null)
            {
                return new ClassificationResult
                {
                    Category = "Bosses",
                    SubFolder = boss
                };
            }

            return new ClassificationResult
            {
                Category = _config.DefaultCategory
            };
        }

        private bool ContainsShadow(string assetName)
        {
            return assetName.IndexOf(_config.ShadowPattern,
                StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool IsBanner(string assetName)
        {
            return _config.BannerKeywords.Any(kw =>
                assetName.StartsWith(kw, StringComparison.OrdinalIgnoreCase));
        }
        private string? FindCharacterFromStart(string assetName)
        {
            return _config.Characters.FirstOrDefault(c =>
                assetName.StartsWith(c, StringComparison.OrdinalIgnoreCase));
        }

        private string? FindCharacter(string name)
        {
            return _config.Characters.FirstOrDefault(c =>
                string.Equals(c, name, StringComparison.OrdinalIgnoreCase));
        }

        private string? FindBoss(string assetName)
        {
            return _config.Bosses.FirstOrDefault(b =>
                assetName.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0);
        }
        public List<string> GetAllCategories()
        {
            return new List<string>
            {
                "Characters",
                "Bosses",
                "Shadow",
                "Banners",
                "ItemMaterials",
                _config.DefaultCategory
            };
        }
    }
}