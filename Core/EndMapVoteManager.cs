using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using cs2_rockthevote.Core;
using System.Data;
using System.Text;
using static CounterStrikeSharp.API.Core.Listeners;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace cs2_rockthevote
{
    public class EndMapVoteManager : IPluginDependency<Plugin, Config>
    {
        private readonly ExtendRoundTimeManager _extendRoundTimeManager;
        const int MAX_OPTIONS_HUD_MENU = 6;
        private readonly TimeLimitManager _timeLimitManager;
        private readonly GameRules _gameRules;
        
        public EndMapVoteManager(MapLister mapLister, ChangeMapManager changeMapManager, NominationCommand nominationManager, StringLocalizer localizer, PluginState pluginState, MapCooldown mapCooldown,ExtendRoundTimeManager extendRoundTimeManager, TimeLimitManager timeLimitManager, GameRules gameRules)
        {
            _mapLister = mapLister;
            _changeMapManager = changeMapManager;
            _nominationManager = nominationManager;
            _localizer = localizer;
            _pluginState = pluginState;
            _mapCooldown = mapCooldown;
            _extendRoundTimeManager = extendRoundTimeManager;
            _timeLimitManager = timeLimitManager;
            _gameRules = gameRules;
        }

        private readonly MapLister _mapLister;
        private readonly ChangeMapManager _changeMapManager;
        private readonly NominationCommand _nominationManager;
        private readonly StringLocalizer _localizer;
        private PluginState _pluginState;
        private MapCooldown _mapCooldown;
        private Timer? Timer;

        Dictionary<string, int> Votes = new();
        int timeLeft = -1;

        List<string> mapsEllected = new();

        private IEndOfMapConfig? _config = null;
        private int _canVote = 0;
        private Plugin? _plugin;

        HashSet<int> _voted = new();

        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
            if (_config?.HudMenu == true)
            {
                plugin.RegisterListener<OnTick>(VoteDisplayTick);
            }
        }

        public void OnMapStart(string map)
        {
            Votes.Clear();
            timeLeft = 0;
            mapsEllected.Clear();
            KillTimer();
        }

        public void MapVoted(CCSPlayerController player, string mapName)
        {
            if (_config!.HideHudAfterVote)
                _voted.Add(player.UserId!.Value);

            Votes[mapName] += 1;
            player.PrintToChat(_localizer.LocalizeWithPrefix("emv.you-voted", mapName));
            if (Votes.Select(x => x.Value).Sum() >= _canVote)
            {
                EndVote();
            }
        }

        public void PlaySound(CCSPlayerController player)
        {
            string soundPath = _config != null ? _config.SoundPath : "sounds/vo/announcer/cs2_classic/felix_broken_fang_pick_1_map_tk01.vsnd_c";
            player.ExecuteClientCommand($"play {soundPath}");
        }

        void KillTimer()
        {
            timeLeft = -1;
            if (Timer is not null)
            {
                Timer!.Kill();
                Timer = null;
            }
        }

        static IList<T> Shuffle<T>(Random rng, IList<T> array)
        {
            int n = array.Count;
            while (n > 1)
            {
                int k = rng.Next(n--);
                (array[k], array[n]) = (array[n], array[k]);
            }
            return array;
        }
        
        void PrintCenterTextAll(string text)
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (player.IsValid)
                {
                    player.PrintToCenter(text);
                }
            }
        }
        
        public void VoteDisplayTick()
        {
            if (timeLeft < 0)
                return;

            int index = 1;
            StringBuilder stringBuilder = new();
            stringBuilder.AppendFormat($"<b>{_localizer.Localize("emv.hud.hud-timer", timeLeft)}</b>");
            if (!_config!.HudMenu)
                foreach (var kv in Votes.OrderByDescending(x => x.Value).Take(MAX_OPTIONS_HUD_MENU).Where(x => x.Value > 0))
                {
                    stringBuilder.AppendFormat($"<br>{kv.Key} <font color='green'>({kv.Value})</font>");
                }
            else
                foreach (var kv in Votes.Take(MAX_OPTIONS_HUD_MENU))
                {
                    stringBuilder.AppendFormat($"<br><font color='yellow'>!{index++}</font> {kv.Key} <font color='green'>({kv.Value})</font>");
                }
            foreach (CCSPlayerController player in ServerManager.ValidPlayers())
            {
                player.PrintToCenterHtml(stringBuilder.ToString());
            }
        }

        public void StartVote(IEndOfMapConfig config)
        {
            if (_pluginState.EofVoteHappening)
                return; // Prevent duplicate vote if one is already running
            
            Server.ExecuteCommand("sv_allow_votes 0");
            Server.ExecuteCommand("sv_vote_allow_in_warmup 0");
            Server.ExecuteCommand("sv_vote_allow_spectators 0");
            Server.ExecuteCommand("sv_vote_count_spectator_votes 0");
            
            Votes.Clear();
            _pluginState.EofVoteHappening = true;
            _config = config;
            
            int mapsToShow = _config.MapsToShow == 0 ? MAX_OPTIONS_HUD_MENU : _config.MapsToShow;
            if (_config.HudMenu && mapsToShow > MAX_OPTIONS_HUD_MENU)
                mapsToShow = MAX_OPTIONS_HUD_MENU;
            
            bool canShowExtendOption = _config.IncludeExtendCurrentMap && _pluginState.MapExtensionCount < _config.MaxMapExtensions;
            int mapOptionsCount = canShowExtendOption ? mapsToShow - 1 : mapsToShow;
            
            // Get map list
            var mapsScrambled = Shuffle(new Random(), _mapLister.Maps!.Select(x => x.Name)
                .Where(x => x != Server.MapName && !_mapCooldown.IsMapInCooldown(x)).ToList());
            
            mapsEllected = _nominationManager.NominationWinners().Concat(mapsScrambled).Distinct().ToList();
            
            // Create vote list
            List<string> voteOptions = new();
            foreach (var map in mapsEllected.Take(mapOptionsCount))
            {
                Votes[map] = 0;
                voteOptions.Add(map);
            }
            
            if (canShowExtendOption)
            {
                string extendOption = _localizer.Localize("extendtime.list-name");
                Votes[extendOption] = 0;
                voteOptions.Add(extendOption);
            }

            _canVote = ServerManager.ValidPlayerCount();

            // Open Chat or Screen Menu (config dependant)
            foreach (var player in ServerManager.ValidPlayers())
            {
                if (_config.ScreenMenu)
                {
                    MapVoteScreenMenu.Prime(_plugin!, player);
                    _plugin!.AddTimer(0.1f, () =>
                    {
                        MapVoteScreenMenu.Open(_plugin!, player, voteOptions, MapVoted, _localizer.Localize("emv.screenmenu-title"));
                    });
                }
                if (_config.ChatMenu)
                {
                    ChatMenu chatMenu = new ChatMenu(_localizer.Localize("emv.hud.menu-title"));
                    foreach (var option in voteOptions)
                    {
                        chatMenu.AddMenuOption(option, (p, selectedOption) =>
                        {
                            MapVoted(p, option);
                            MenuManager.CloseActiveMenu(p);
                        });
                    }
                    MenuManager.OpenChatMenu(player, chatMenu);
                }
                if (_config.SoundEnabled)
                {
                    PlaySound(player);
                }
            }
            
            timeLeft = _config.VoteDuration;
            Timer = _plugin!.AddTimer(1.0F, () =>
            {
                if (timeLeft <= 0)
                {
                    EndVote();
                }
                else
                {
                    timeLeft--;
                }
            }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        }

        void EndVote()
        {
            foreach (var player in ServerManager.ValidPlayers())
            {
                if (player.IsValid)
                {
                    MapVoteScreenMenu.Close(_plugin!, player);
                }
            }
            
            KillTimer();
            
            bool mapEnd = _config is EndOfMapConfig;
            string extendOption = _localizer.Localize("extendtime.list-name");
            
            decimal totalVotes = Votes.Select(x => x.Value).Sum();
            KeyValuePair<string, int> winner;
            Random rnd = new();
            
            if (totalVotes == 0)
            {
                // No votes cast, pick a random map(not the extend).
                var candidateMaps = Votes.Keys.Where(x => x != extendOption).ToList();
                if (candidateMaps.Count == 0)
                    candidateMaps = Votes.Keys.ToList();
                string chosen = candidateMaps[rnd.Next(candidateMaps.Count)];
                winner = new KeyValuePair<string, int>(chosen, 0);
            }
            else
            {
                decimal maxVotes = Votes.Select(x => x.Value).Max();
                var potentialWinners = Votes.Where(x => x.Value == maxVotes);
                winner = potentialWinners.ElementAt(rnd.Next(0, potentialWinners.Count()));
            }
            
            decimal percent = totalVotes > 0 ? winner.Value / totalVotes * 100M : 0;
            
            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("emv.vote-ended", winner.Key, percent, totalVotes));
            
            if (winner.Key == extendOption)
            {
                if (_pluginState.MapExtensionCount < _config!.MaxMapExtensions)
                {
                    bool success = _extendRoundTimeManager.ExtendRoundTime(_config.RoundTimeExtension);
                    if (success)
                    {
                        Server.PrintToChatAll(_localizer.LocalizeWithPrefix("extendtime.vote-ended.passed", _config.RoundTimeExtension, percent, totalVotes));
                        _pluginState.MapExtensionCount++;
                    }
                    else
                    {
                        Server.PrintToChatAll(_localizer.LocalizeWithPrefix("extendtime.vote-ended.failed", percent, totalVotes));
                    }
                }
                
                int newRemainingSeconds = (int)(_gameRules.RoundTime - (Server.CurrentTime - _gameRules.GameStartTime));
                int triggerSeconds = ((EndOfMapConfig)_config).TriggerSecondsBeforeEnd;
                int delay = Math.Max(newRemainingSeconds - triggerSeconds, 0);
                
                _plugin?.AddTimer(delay, () =>
                {
                    _pluginState.EofVoteHappening = false;
                    _changeMapManager.OnMapStart(Server.MapName);
                    StartVote(_config);
                }, TimerFlags.STOP_ON_MAPCHANGE);
            }
            else
            {
                _changeMapManager.ScheduleMapChange(winner.Key, mapEnd: mapEnd);
                
                if (_config!.ChangeMapImmediatly)
                {
                    _changeMapManager.ChangeNextMap(mapEnd);
                }
                else
                {
                    if (!mapEnd)
                        Server.PrintToChatAll(_localizer.LocalizeWithPrefix("general.changing-map-next-round", winner.Key));
                    
                    var ignoreRoundWinConditions = ConVar.Find("mp_ignore_round_win_conditions");
                    if (ignoreRoundWinConditions != null && ignoreRoundWinConditions.GetPrimitiveValue<bool>())
                    {
                        Timer? checkTimer = null;
                        checkTimer = _plugin!.AddTimer(1.0F, () =>
                        {
                            int remainingSeconds = (int)(_gameRules.RoundTime - (Server.CurrentTime - _gameRules.GameStartTime));
                            if (remainingSeconds <= 3)
                            {
                                _changeMapManager.ChangeNextMap(mapEnd);
                                checkTimer?.Kill();
                            }
                        }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
                    }
                }
                _pluginState.EofVoteHappening = false;
            }
        }
    }
}
