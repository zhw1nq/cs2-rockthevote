using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace cs2_rockthevote
{
    public class AFKManager : IPluginDependency<Plugin, Config>
    {
        private Plugin? _plugin;
        private GeneralConfig _generalConfig = new();
        private readonly Dictionary<uint, Vector> _lastOrigin = new();
        private readonly HashSet<uint> _afkPlayers = new();
        private Timer? _timer;

        public AFKManager() { }

        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
        }

        public void OnConfigParsed(Config config)
        {
            _generalConfig = config.General;

            //If config reloads mid map, apply changes immediately
            if (_timer != null)
            {
                if (!_generalConfig.IncludeAFK) KillAFKTimer();
                else RestartAfkTimer();
            }
        }

        public void OnMapStart(string map)
        {
            _afkPlayers.Clear();
            _lastOrigin.Clear();

            if (!_generalConfig.IncludeAFK)
            {
                KillAFKTimer();
                return;
            }

            Server.NextFrame(() =>
            {
                InitializeLastOrigins();
                RestartAfkTimer();
            });
        }

        private void RestartAfkTimer()
        {
            KillAFKTimer();

            _timer = _plugin!.AddTimer(
                _generalConfig.AFKCheckInterval,
                CheckAllPlayers,
                TimerFlags.REPEAT
            );
        }

        private void InitializeLastOrigins()
        {
            foreach (var player in Utilities.GetPlayers().Where(p => p.ReallyValid()))
            {
                var origin = player.PlayerPawn.Value?.CBodyComponent?.SceneNode?.AbsOrigin;
                if (origin != null)
                    _lastOrigin[player.Index] = new Vector(origin.X, origin.Y, origin.Z);
            }
        }

        private void CheckAllPlayers()
        {
            foreach (var player in Utilities.GetPlayers().Where(p => p.ReallyValid()))
            {
                var origin = player.PlayerPawn.Value?.CBodyComponent?.SceneNode?.AbsOrigin;
                if (origin == null) continue;

                var current = new Vector(origin.X, origin.Y, origin.Z);
                var idx  = player.Index;

                if (_lastOrigin.TryGetValue(idx, out var last))
                {
                    float dx = current.X - last.X;
                    float dy = current.Y - last.Y;
                    float dz = current.Z - last.Z;
                    float distSq = dx * dx + dy * dy + dz * dz;

                    if (distSq < 0.01f)
                        _afkPlayers.Add(idx);
                    else
                    {
                        _afkPlayers.Remove(idx);
                        _lastOrigin[idx] = current;
                    }
                }
                else
                {
                    _lastOrigin[idx] = current;
                }
            }
        }

        public void KillAFKTimer()
        {
            _timer?.Kill();
            _timer = null;
            _afkPlayers.Clear();
        }
    }
}