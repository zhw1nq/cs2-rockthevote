using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Timers;

namespace cs2_rockthevote
{
    public partial class Plugin
    {
        [GameEventHandler(HookMode.Post)]
        public HookResult OnRoundEndMapChanger(EventRoundEnd @event, GameEventInfo info)
        {
            _changeMapManager.ChangeNextMap();
            return HookResult.Continue;
        }

        [GameEventHandler(HookMode.Post)]
        public HookResult OnRoundStartMapChanger(EventRoundStart @event, GameEventInfo info)
        {
            _changeMapManager.ChangeNextMap();
            return HookResult.Continue;
        }
    }

    public class ChangeMapManager : IPluginDependency<Plugin, Config>
    {
        private Plugin? _plugin;
        private readonly StringLocalizer _localizer;
        private readonly PluginState _pluginState;
        private readonly MapLister _mapLister;

        public string? NextMap { get; private set; } = null;
        private string _prefix = DEFAULT_PREFIX;
        private const string DEFAULT_PREFIX = "rtv.prefix";
        private bool _mapEnd = false;

        private Map[] _maps = [];
        private Config? _config;

        public ChangeMapManager(StringLocalizer localizer, PluginState pluginState, MapLister mapLister)
        {
            _localizer = localizer;
            _pluginState = pluginState;
            _mapLister = mapLister;
            _mapLister.EventMapsLoaded += OnMapsLoaded;
        }

        public void OnMapsLoaded(object? sender, Map[] maps)
        {
            _maps = maps;
        }


        public void ScheduleMapChange(string map, bool mapEnd = false, string prefix = DEFAULT_PREFIX)
        {
            NextMap = map;
            _prefix = prefix;
            _pluginState.MapChangeScheduled = true;
            _mapEnd = mapEnd;
        }

        public void OnMapStart(string _map)
        {
            NextMap = null;
            _prefix = DEFAULT_PREFIX;
            _pluginState.MapChangeScheduled = false;
            _mapEnd = false;
        }

        public bool ChangeNextMap(bool mapEnd = false)
        {
            if (mapEnd != _mapEnd)
                return false;

            if (!_pluginState.MapChangeScheduled)
                return false;

            var map = _maps.FirstOrDefault(x => string.Equals(x.Name, NextMap, StringComparison.OrdinalIgnoreCase));
            if (map == null)
                return false;

            _pluginState.MapChangeScheduled = false;

            Server.PrintToChatAll(_localizer.LocalizeWithPrefixInternal(_prefix, "general.changing-map", map.Name));

            _plugin?.AddTimer(3.0F, () =>
            {
                Server.ExecuteCommand($"changelevel {map.Name}");
            }, TimerFlags.STOP_ON_MAPCHANGE);

            return true;
        }

        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
            plugin.RegisterEventHandler<EventCsWinPanelMatch>((ev, info) =>
            {
                if (_pluginState.MapChangeScheduled)
                {
                    var delay = (_config?.EndOfMapVote.DelayToChangeInTheEnd ?? 0) - 3.0F;
                    if (delay < 0)
                        delay = 0;

                    _plugin?.AddTimer(delay, () =>
                    {
                        ChangeNextMap(true);
                    }, TimerFlags.STOP_ON_MAPCHANGE);
                }
                return HookResult.Continue;
            });
        }
    }
}
