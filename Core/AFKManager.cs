using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace cs2_rockthevote
{
    public class AFKManager : IPluginDependency<Plugin, Config>
    {
        private readonly Plugin _plugin;
        private GeneralConfig _generalConfig;
        private readonly Dictionary<uint, Vector> _lastOrigin = new();
        private readonly HashSet<uint> _afkPlayers   = new();
        private Timer? Timer;
    

        public AFKManager(Plugin plugin, Config config)
        {
            _plugin = plugin;
            _generalConfig = config.General;
        }

        public void OnConfigParsed(Config config)
        {
            _generalConfig = config.General;

            if (!_generalConfig.IncludeAFK) 
                return;

            foreach (var player in Utilities.GetPlayers().Where(p => p.ReallyValid()))
            {
                var originNode = player.PlayerPawn.Value?.CBodyComponent?.SceneNode?.AbsOrigin;
                if (originNode != null)
                {
                    _lastOrigin[player.Index] = new Vector(originNode.X, originNode.Y, originNode.Z);
                }
            }

            Timer = _plugin.AddTimer(
                _generalConfig.AFKCheckInterval,
                CheckAllPlayers,
                TimerFlags.REPEAT
            );
        }

        private void CheckAllPlayers()
        {
            foreach (var player in Utilities.GetPlayers().Where(p => p.ReallyValid()))
            {
                var originNode = player.PlayerPawn.Value?.CBodyComponent?.SceneNode?.AbsOrigin;
                if (originNode == null)
                    continue;

                var curr = new Vector(originNode.X, originNode.Y, originNode.Z);
                var idx = player.Index;

                if (_lastOrigin.TryGetValue(idx, out var last))
                {
                    float dx = curr.X - last.X;
                    float dy = curr.Y - last.Y;
                    float dz = curr.Z - last.Z;
                    float distSq = dx * dx + dy * dy + dz * dz;

                    if (distSq < 0.01f)
                        _afkPlayers.Add(idx);
                    else
                    {
                        _afkPlayers.Remove(idx);
                        _lastOrigin[idx] = curr;
                    }
                }
                else
                {
                    _lastOrigin[idx] = curr;
                }
            }
        }

        public void OnMapStart(string map)
        {
            _afkPlayers.Clear();
            _afkPlayers.Clear();
        }

        public void KillAFKTimer()
        {
            Timer?.Kill();
            Timer = null;
            _afkPlayers.Clear();
        }
    }
}