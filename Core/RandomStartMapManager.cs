using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Timers;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using Microsoft.Extensions.Logging;

namespace cs2_rockthevote
{
    public class RandomStartMapManager(MapLister mapLister, ILogger<RandomStartMapManager> logger) : IPluginDependency<Plugin, Config>
    {
        private readonly ILogger<RandomStartMapManager> _logger = logger;
        private readonly MapLister _mapLister = mapLister;
        private bool _firstMapStart = true;
        private Timer? _timerChangeMap;
        private GeneralConfig _generalConfig = new();
        private Plugin? _plugin;

        private void KillTimer()
        {
            _timerChangeMap?.Kill();
            _timerChangeMap = null;
        }

        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
        }

        public void OnConfigParsed(Config config)
        {
            _generalConfig = config.General;
        }

        public void OnMapStart(string currentMap)
        {
            KillTimer();

            if (!_generalConfig.RandomStartMap || !_firstMapStart)
                return;

            _firstMapStart = false;

            var candidates = _mapLister.Maps?
                .Where(m => !string.Equals(m.Name, currentMap, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (candidates == null || candidates.Count == 0)
                return;

            var pick = candidates[new Random().Next(candidates.Count)];

            _timerChangeMap = _plugin?.AddTimer(3.0f, () =>
            {
                Server.ExecuteCommand($"changelevel {pick.Name}");
            }, TimerFlags.STOP_ON_MAPCHANGE);
        }
    }
}