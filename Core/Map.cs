using CounterStrikeSharp.API;

namespace cs2_rockthevote
{
    public class Map(string name)
    {
        public string Name { get; set; } = name.Trim();
    }

    public class MapCooldown : IPluginDependency<Plugin, Config>
    {
        List<string> mapsOnCoolDown = new();
        private GeneralConfig _generalConfig = new();
        public event EventHandler<Map[]>? EventCooldownRefreshed;

        public MapCooldown(MapLister mapLister)
        {
            mapLister.EventMapsLoaded += (sender, maps) =>
            {
                var current = Server.MapName?.Trim();
                if (string.IsNullOrEmpty(current))
                    return;

                int maxEntries = _generalConfig.MapsInCoolDown;

                if (maxEntries <= 0)
                {
                    mapsOnCoolDown.Clear();
                    mapsOnCoolDown.Add(current.ToLowerInvariant());
                }
                else
                {
                    if (mapsOnCoolDown.Count >= maxEntries)
                        mapsOnCoolDown.RemoveAt(0);

                    mapsOnCoolDown.Add(current.ToLowerInvariant());
                }

                EventCooldownRefreshed?.Invoke(this, maps);
            };
        }

        public void OnConfigParsed(Config config)
        {
            _generalConfig = config.General;
        }

        public bool IsMapInCooldown(string map)
        {
            if (string.IsNullOrEmpty(map))
                return false;

            var baseName = map;
            var idx = map.IndexOf(' ');
            if (idx > 0)
                baseName = map[..idx];

            var lowerName = baseName.Trim().ToLowerInvariant();

            var current = Server.MapName?.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(current) && lowerName == current)
                return true;

            return mapsOnCoolDown.Contains(lowerName);
        }
    }
}