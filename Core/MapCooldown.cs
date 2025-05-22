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
            int InCoolDown = _generalConfig.MapsInCoolDown;
                
            mapLister.EventMapsLoaded += (e, maps) =>
            {
                var map = Server.MapName;
                if (map is not null)
                {
                    if (InCoolDown == 0)
                    {
                        mapsOnCoolDown.Clear();
                        return;
                    }

                    if (mapsOnCoolDown.Count > InCoolDown)
                        mapsOnCoolDown.RemoveAt(0);

                    mapsOnCoolDown.Add(map.Trim().ToLower());
                    EventCooldownRefreshed?.Invoke(this, maps);
                }
            };
        }

        public void OnConfigParsed(Config config)
        {
            _generalConfig = config.General;
        }

        public bool IsMapInCooldown(string map)
        {
            return mapsOnCoolDown.IndexOf(map) > -1;
        }
    }
}
