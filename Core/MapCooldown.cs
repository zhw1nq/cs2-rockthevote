using CounterStrikeSharp.API;

namespace cs2_rockthevote.Core
{
    public class MapCooldown : IPluginDependency<Plugin, Config>
    {
        List<string> mapsOnCoolDown = new();
        private GeneralConfig _generalConfig = new();
        public event EventHandler<Map[]>? EventCooldownRefreshed;

        public MapCooldown(MapLister mapLister)
        {
            // Each time the maps load (i.e. on map start), refresh our list
            mapLister.EventMapsLoaded += (sender, maps) =>
            {
                var current = Server.MapName?.Trim();
                if (string.IsNullOrEmpty(current))
                    return;

                int maxEntries = _generalConfig.MapsInCoolDown;

                // If cooldown is disabled, clear everything
                if (maxEntries <= 0)
                {
                    mapsOnCoolDown.Clear();
                }
                else
                {
                    // Drop the oldest if we're already at the limit
                    if (mapsOnCoolDown.Count >= maxEntries)
                        mapsOnCoolDown.RemoveAt(0);

                    // Store just the base map name, lowercase
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

            // Grab the base map name (everything before the first space or parenthesis)
            // E.g. "surf_beginner (T1, Staged)" -> "surf_beginner"
            var baseName = map;
            var idx = map.IndexOf(' ');
            if (idx > 0)
                baseName = map.Substring(0, idx);

            // Compare lowercase
            return mapsOnCoolDown.Contains(baseName.Trim().ToLowerInvariant());
        }
    }
}
