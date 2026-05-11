using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SL_Asset_Extractor.Core.Settings
{
    public class RulesService
    {
        private readonly string _rulesPath;

        public RulesService(string rulesPath)
        {
            _rulesPath = rulesPath;
        }

        public List<string> GetCharacters()
        {
            var json = File.ReadAllText(_rulesPath);
            var obj = JObject.Parse(json);
            var characters = obj["characters"]?.ToObject<List<string>>();
            return characters ?? new List<string>();
        }

        public void AddCharacter(string characterName)
        {
            var json = File.ReadAllText(_rulesPath);
            var obj = JObject.Parse(json);

            var characters = obj["characters"]?.ToObject<List<string>>() ?? new List<string>();

            if (!characters.Any(c => string.Equals(c, characterName, StringComparison.OrdinalIgnoreCase)))
            {
                characters.Add(characterName);
                obj["characters"] = JArray.FromObject(characters);
                File.WriteAllText(_rulesPath, obj.ToString(Formatting.Indented));
            }
        }

        public void RemoveCharacter(string characterName)
        {
            var json = File.ReadAllText(_rulesPath);
            var obj = JObject.Parse(json);

            var characters = obj["characters"]?.ToObject<List<string>>() ?? new List<string>();
            characters.RemoveAll(c => string.Equals(c, characterName, StringComparison.OrdinalIgnoreCase));

            obj["characters"] = JArray.FromObject(characters);
            File.WriteAllText(_rulesPath, obj.ToString(Formatting.Indented));
        }
    }
}