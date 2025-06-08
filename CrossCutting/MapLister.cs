namespace cs2_rockthevote
{
    public class MapLister : IPluginDependency<Plugin, Config>
    {
        public Map[]? Maps { get; private set; } = null;
        public bool MapsLoaded { get; private set; } = false;
        public event EventHandler<Map[]>? EventMapsLoaded;
        private Plugin? _plugin;

        public void Clear()
        {
            MapsLoaded = false;
            Maps = null;
        }

        public void LoadMaps()
        {
            Clear();
            string mapsFile = Path.Combine(_plugin!.ModulePath, "../maplist.txt");
            if (!File.Exists(mapsFile))
                throw new FileNotFoundException(mapsFile);

            Maps = [.. File.ReadAllText(mapsFile)
                .Replace("\r\n", "\n")
                .Split("\n")
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("//"))
                .Select(mapLine =>
                {
                    string[] args = mapLine.Split(":");
                    string mapName = args[0];
                    string? mapValue = args.Length == 2 ? args[1] : null;
                    return new Map(mapName, mapValue);
                })];

            MapsLoaded = true;
            EventMapsLoaded?.Invoke(this, Maps!);
        }

        public void OnMapStart(string _map)
        {
            if (_plugin is not null)
                LoadMaps();
        }


        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
            LoadMaps();
        }

        // Returns the exact map name, or null if none found
        public string? GetExactMapName(string name)
        {
            if (Maps == null) return null;
            return Maps
                .Select(m => m.Name)
                .FirstOrDefault(n => n.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        // Returns all map names that contain the given argument
        public List<string> GetMatchingMapNames(string partial)
        {
            if (Maps == null) return new List<string>();
            return [.. Maps
                .Select(m => m.Name)
                .Where(n => n.Contains(partial, StringComparison.OrdinalIgnoreCase))];
        }
    }
}