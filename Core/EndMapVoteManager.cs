using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using cs2_rockthevote.Core;
using System.Data;
using static CounterStrikeSharp.API.Core.Listeners;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using Microsoft.Extensions.Logging;

namespace cs2_rockthevote
{
    public class EndMapVoteManager : IPluginDependency<Plugin, Config>
    {
        private readonly ILogger<EndMapVoteManager> _logger;
        private readonly ExtendRoundTimeManager _extendRoundTimeManager;
        const int MAX_OPTIONS_HUD_MENU = 6;
        private readonly TimeLimitManager _timeLimitManager;
        private readonly GameRules _gameRules;

        public EndMapVoteManager(MapLister mapLister, ChangeMapManager changeMapManager, NominationCommand nominationManager, StringLocalizer localizer, PluginState pluginState, MapCooldown mapCooldown, ExtendRoundTimeManager extendRoundTimeManager, TimeLimitManager timeLimitManager, GameRules gameRules, ILogger<EndMapVoteManager> logger)
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
            _logger = logger;
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
        private GeneralConfig _generalConfig = new();
        private VoteTypeConfig _voteTypeConfig = new();
        private EndOfMapConfig _endMapConfig = new();

        private int _canVote = 0;
        private Plugin? _plugin;
        HashSet<int> _voted = new();
        private DateTime _lastChatPrintTime = DateTime.MinValue;

        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
            if (_voteTypeConfig.EnableHudMenu || _endMapConfig.EnableCountdown)
            {
                plugin.RegisterListener<OnTick>(VoteDisplayTick);
            }
        }

        public void OnConfigParsed(Config config)
        {
            _generalConfig = config.General;
            _voteTypeConfig = config.VoteType;
            _endMapConfig = config.EndOfMapVote;

            // Check to make sure VoteDuration isn't >= TriggerSecondsBeforeEnd, if it is, use a fallback
            if (_endMapConfig.VoteDuration >= _endMapConfig.TriggerSecondsBeforeEnd)
            {
                var original = _endMapConfig.VoteDuration;
                var adjusted = Math.Max(1, _endMapConfig.TriggerSecondsBeforeEnd - 5);
                _endMapConfig.VoteDuration = adjusted;

                _logger.LogError(
                    $"EndOfMapVote config invalid: VoteDuration ({_endMapConfig.VoteDuration}s) must be less than " +
                    $"TriggerSecondsBeforeEnd ({_endMapConfig.TriggerSecondsBeforeEnd}s). Automatically adjusting VoteDuration to {adjusted}s.",
                    original,
                    config.EndOfMapVote.TriggerSecondsBeforeEnd,
                    adjusted
                );
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

        public void KillTimer()
        {
            timeLeft = -1;
            if (Timer is not null)
            {
                Timer!.Kill();
                Timer = null;
            }
        }

        public static IList<T> Shuffle<T>(Random rng, IList<T> array)
        {
            int n = array.Count;
            while (n > 1)
            {
                int k = rng.Next(n--);
                (array[k], array[n]) = (array[n], array[k]);
            }
            return array;
        }
        
        public void PrintCenterTextAll(string text)
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
            if (timeLeft < 0 || !_endMapConfig.EnableCountdown || !_pluginState.EofVoteHappening)
                return;

            string countdown = _localizer.Localize("emv.hud.hud-timer", timeLeft);

            var now = DateTime.UtcNow;
            int _chatIntervalSeconds = _generalConfig.ChatCountdownInterval;
            bool sendChat = !_endMapConfig.HudCountdown && (now - _lastChatPrintTime).TotalSeconds >= _chatIntervalSeconds;

            foreach (CCSPlayerController player in ServerManager.ValidPlayers())
            {
                if (_endMapConfig.HudCountdown)
                {
                    player.PrintToCenter(countdown);
                }
                else
                {
                    player.PrintToChat(countdown);
                }
            }

            if (sendChat)
                _lastChatPrintTime = now;
        }

        public void StartVote(IEndOfMapConfig config)
        {
            if (_pluginState.EofVoteHappening)
                return;
            
            if (_voteTypeConfig.EnablePanorama)
            {
                Server.ExecuteCommand("sv_allow_votes 0");
                Server.ExecuteCommand("sv_vote_allow_in_warmup 0");
                Server.ExecuteCommand("sv_vote_allow_spectators 0");
                Server.ExecuteCommand("sv_vote_count_spectator_votes 0");
            }
            
            Votes.Clear();
            _pluginState.EofVoteHappening = true;
            _config = config;
            
            int mapsToShow = _config.MapsToShow == 0 ? MAX_OPTIONS_HUD_MENU : _config.MapsToShow;
            if (_voteTypeConfig.EnableHudMenu && mapsToShow > MAX_OPTIONS_HUD_MENU)
                mapsToShow = MAX_OPTIONS_HUD_MENU;
            
            int maxExt = _generalConfig.MaxMapExtensions;
            bool unlimited = maxExt <= 0;  // treat 0 or negative as unlimited

            bool canShowExtendOption = _endMapConfig.IncludeExtendCurrentMap && (unlimited || _pluginState.MapExtensionCount < maxExt);
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
                if (_voteTypeConfig.EnableScreenMenu)
                {
                    Server.NextFrame(() =>
                        MapVoteScreenMenu.Open(_plugin!, player, voteOptions, MapVoted, _localizer.Localize("emv.screenmenu-title"))
                    );
                }
                if (!_voteTypeConfig.EnableScreenMenu)
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

        public void EndVote()
        {
            foreach (var player in ServerManager.ValidPlayers())
            {
                if (player.IsValid)
                {
                    MapVoteScreenMenu.Close(player);
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
                int maxExt = _generalConfig.MaxMapExtensions;
                bool unlimited = maxExt <= 0;

                if (unlimited || _pluginState.MapExtensionCount < maxExt)
                {
                    bool success = _extendRoundTimeManager.ExtendRoundTime(_generalConfig.RoundTimeExtension);
                    if (success)
                    {
                        Server.PrintToChatAll(_localizer.LocalizeWithPrefix("extendtime.vote-ended.passed", _generalConfig.RoundTimeExtension, percent, totalVotes));
                        _pluginState.MapExtensionCount++;
                    }
                    else
                    {
                        Server.PrintToChatAll(_localizer.LocalizeWithPrefix("extendtime.vote-ended.failed", percent, totalVotes));
                    }
                }
                
                int newRemainingSeconds = (int)(_gameRules.RoundTime - (Server.CurrentTime - _gameRules.GameStartTime));
                int triggerSeconds = ((EndOfMapConfig)_config!).TriggerSecondsBeforeEnd;
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
