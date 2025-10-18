using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Timers;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using Microsoft.Extensions.Logging;
using Menu;
using Menu.Enums;

namespace cs2_rockthevote
{
    public class EndMapVoteManager : IPluginDependency<Plugin, Config>
    {
        private readonly ILogger<EndMapVoteManager> _logger;
        private readonly GameRules _gameRules;
        private readonly MapLister _mapLister;
        private readonly ChangeMapManager _changeMapManager;
        private readonly StringLocalizer _localizer;
        private readonly PluginState _pluginState;
        private readonly MapCooldown _mapCooldown;
        private Timer? Timer;
        private readonly List<string> mapsElected = new();
        private int _canVote;
        private Plugin? _plugin;
        private KitsuneMenu? _menuManager;

        public int TimeLeft { get; private set; } = -1;
        public int MaxOptionsHud { get; private set; } = 6;
        public ISet<int> VotedPlayers { get; private set; } = new HashSet<int>();
        public Dictionary<string, int> Votes { get; private set; } = new();
        public IReadOnlyDictionary<string, int> CurrentVotes => Votes;
        private EndOfMapConfig _endMapConfig = new();
        private RtvConfig _rtvConfig = new();

        public EndMapVoteManager(
            MapLister mapLister,
            ChangeMapManager changeMapManager,
            StringLocalizer localizer,
            PluginState pluginState,
            MapCooldown mapCooldown,
            GameRules gameRules,
            ILogger<EndMapVoteManager> logger)
        {
            _mapLister = mapLister;
            _changeMapManager = changeMapManager;
            _localizer = localizer;
            _pluginState = pluginState;
            _mapCooldown = mapCooldown;
            _gameRules = gameRules;
            _logger = logger;
        }

        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
            _menuManager = new KitsuneMenu(plugin, multiCast: false);
        }

        public void OnConfigParsed(Config config)
        {
            _endMapConfig = config.EndOfMapVote;
            _rtvConfig = config.Rtv;

            if (_endMapConfig.VoteDuration >= _endMapConfig.TriggerSecondsBeforeEnd)
            {
                var adjusted = Math.Max(1, _endMapConfig.TriggerSecondsBeforeEnd - 5);
                _endMapConfig.VoteDuration = adjusted;
                _logger.LogError($"VoteDuration adjusted to {adjusted}s (must be < TriggerSecondsBeforeEnd)");
            }
        }

        public void OnMapStart(string map)
        {
            Votes.Clear();
            VotedPlayers.Clear();
            TimeLeft = -1;
            mapsElected.Clear();
            KillTimer();
        }

        public void ScheduleNextVote()
        {
            int remaining = (int)(_gameRules.RoundTime - (Server.CurrentTime - _gameRules.GameStartTime));
            int delay = Math.Max(remaining - _endMapConfig.TriggerSecondsBeforeEnd, 0);

            _plugin?.AddTimer(delay, () =>
            {
                if (_pluginState.EofVoteHappening)
                    return;
                    
                _changeMapManager.OnMapStart(Server.MapName);
                StartVote(isRtv: false);
            }, TimerFlags.STOP_ON_MAPCHANGE);
        }

        public void MapVoted(CCSPlayerController player, string mapName, bool isRtv)
        {
            if (player?.UserId == null || !player.IsValid)
                return;

            var userId = player.UserId.Value;
            if (VotedPlayers.Contains(userId))
                return;

            VotedPlayers.Add(userId);
            Votes[mapName]++;
            player.PrintToChat(_localizer.LocalizeWithPrefix("emv.you-voted", mapName));

            if (Votes.Values.Sum() >= _canVote)
                EndVote(isRtv);
        }

        public void KillTimer()
        {
            TimeLeft = -1;
            Timer?.Kill();
            Timer = null;
        }

        private static IList<T> Shuffle<T>(Random rng, IList<T> array)
        {
            int n = array.Count;
            while (n > 1)
            {
                int k = rng.Next(n--);
                (array[k], array[n]) = (array[n], array[k]);
            }
            return array;
        }

        public void StartVote(bool isRtv)
        {
            if (_pluginState.EofVoteHappening) return;

            VotedPlayers.Clear();

            if (_rtvConfig.EnablePanorama)
            {
                Server.ExecuteCommand("sv_allow_votes 0");
                Server.ExecuteCommand("sv_vote_allow_in_warmup 0");
                Server.ExecuteCommand("sv_vote_allow_spectators 0");
                Server.ExecuteCommand("sv_vote_count_spectator_votes 0");
            }

            Votes.Clear();
            _pluginState.EofVoteHappening = true;

            int mapsToShow = isRtv
                ? (_rtvConfig.MapsToShow == 0 ? MaxOptionsHud : _rtvConfig.MapsToShow)
                : (_endMapConfig.MapsToShow == 0 ? MaxOptionsHud : _endMapConfig.MapsToShow);

            var available = _mapLister.Maps!.Select(x => x.Name)
                .Where(x => x != Server.MapName && !_mapCooldown.IsMapInCooldown(x))
                .ToList();

            var shuffled = Shuffle(new Random(), available);
            mapsElected.Clear();
            mapsElected.AddRange(shuffled.Distinct());

            var voteOptions = mapsElected.Take(mapsToShow).ToList();
            foreach (var map in voteOptions)
                Votes[map] = 0;

            _canVote = ServerManager.ValidPlayerCount();
            var title = _localizer.Localize("emv.hud.menu-title");
            var players = ServerManager.ValidPlayers().Where(p => p?.IsValid == true).ToList();

            foreach (var player in players)
            {
                if (!player.IsValid)
                    continue;

                var items = new List<MenuItem>();

                foreach (var opt in voteOptions)
                {
                    var map = opt;
                    items.Add(new MenuItem(MenuItemType.Button, new MenuValue(string.Empty),
                        [new MenuButtonCallback(map, map, (ctrl, data) => MapVoted(ctrl, data, isRtv))]));
                }

                try
                {
                    _menuManager?.ShowScrollableMenu(player, title, items, null, false, false, 5);

                    if (_endMapConfig.SoundEnabled && !string.IsNullOrEmpty(_endMapConfig.SoundPath))
                        player.ExecuteClientCommand($"play {_endMapConfig.SoundPath}");
                }
                catch { }
            }

            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("emv.vote-started"));

            int duration = isRtv ? _rtvConfig.MapVoteDuration : _endMapConfig.VoteDuration;
            TimeLeft = duration;

            Timer = _plugin?.AddTimer(1.0f, () =>
            {
                if (TimeLeft <= 0) EndVote(isRtv);
                else TimeLeft--;
            }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        }

        public void EndVote(bool isRtv)
        {
            _pluginState.EofVoteHappening = false;
            KillTimer();
            
            foreach (var p in ServerManager.ValidPlayers().Where(p => p?.IsValid == true))
            {
                try { _menuManager?.ClearMenus(p); } 
                catch { }
            }

            bool mapEnd = !isRtv && !_endMapConfig.ChangeMapImmediately;
            decimal total = Votes.Values.Sum();

            KeyValuePair<string, int> winner;
            if (total == 0)
            {
                var options = Votes.Keys.ToList();
                winner = new(options[new Random().Next(options.Count)], 0);
            }
            else
            {
                int max = Votes.Values.Max();
                var tied = Votes.Where(kv => kv.Value == max).Select(kv => kv.Key).ToList();
                string chosen = tied[new Random().Next(tied.Count)];
                winner = new(chosen, max);
            }

            decimal pct = total > 0 ? winner.Value / total * 100M : 0;
            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("emv.vote-ended", winner.Key, pct, total));

            _changeMapManager.ScheduleMapChange(winner.Key, mapEnd);

            if (!isRtv)
            {
                if (_endMapConfig.ChangeMapImmediately)
                    _changeMapManager.ChangeNextMap(mapEnd);
                else
                {
                    if (!mapEnd)
                        Server.PrintToChatAll(_localizer.LocalizeWithPrefix("general.changing-map-next-round", winner.Key));

                    if (ConVar.Find("mp_ignore_round_win_conditions")?.GetPrimitiveValue<bool>() == true)
                    {
                        Timer? check = null;
                        check = _plugin?.AddTimer(1.0f, () =>
                        {
                            int remaining = (int)(_gameRules.RoundTime - (Server.CurrentTime - _gameRules.GameStartTime));
                            if (remaining <= 3)
                            {
                                _changeMapManager.ChangeNextMap(mapEnd);
                                check?.Kill();
                            }
                        }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
                    }
                }
            }
            else
            {
                if (!_rtvConfig.ChangeAtRoundEnd)
                {
                    int delay = _rtvConfig.MapChangeDelay;
                    if (delay <= 0)
                        _changeMapManager.ChangeNextMap(mapEnd);
                    else
                        _plugin?.AddTimer(delay, () => _changeMapManager.ChangeNextMap(mapEnd), TimerFlags.STOP_ON_MAPCHANGE);
                }
                else
                {
                    _changeMapManager.ChangeNextMap(true);
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("general.changing-map-next-round", winner.Key));
                }
            }
        }
    }
}