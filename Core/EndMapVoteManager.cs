using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Timers;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using CS2MenuManager.API.Class;
using cs2_rockthevote.Core;
using System.Data;
using Microsoft.Extensions.Logging;
using CS2MenuManager.API.Menu;

namespace cs2_rockthevote
{
    public class EndMapVoteManager : IPluginDependency<Plugin, Config>
    {
        private readonly ILogger<EndMapVoteManager> _logger;
        private readonly GameRules _gameRules;
        private readonly MapLister _mapLister;
        private readonly ExtendRoundTimeManager _extendRoundTimeManager;
        private readonly TimeLimitManager _timeLimitManager;
        private readonly ChangeMapManager _changeMapManager;
        private readonly NominationCommand _nominationManager;
        private readonly StringLocalizer _localizer;
        private readonly PluginState _pluginState;
        private readonly MapCooldown _mapCooldown;
        private Timer? Timer;
        List<string> mapsElected = new();
        private int _canVote = 0;
        private Plugin? _plugin;

        public int TimeLeft { get; private set; } = -1;
        public int MaxOptionsHud { get; private set; } = 6;
        public ISet<int> VotedPlayers { get; private set; } = new HashSet<int>();
        public Dictionary<string,int> Votes { get; private set; } = new();
        public IReadOnlyDictionary<string, int> CurrentVotes => Votes;

        private GeneralConfig _generalConfig = new();
        private EndOfMapConfig _endMapConfig = new();
        private RtvConfig _rtvConfig = new();

        public EndMapVoteManager
        (
            MapLister mapLister,
            ChangeMapManager changeMapManager,
            NominationCommand nominationManager,
            StringLocalizer localizer,
            PluginState pluginState,
            MapCooldown mapCooldown,
            ExtendRoundTimeManager extendRoundTimeManager,
            TimeLimitManager timeLimitManager,
            GameRules gameRules,
            ILogger<EndMapVoteManager> logger
        )
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

        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
        }

        public void OnConfigParsed(Config config)
        {
            _generalConfig = config.General;
            _endMapConfig = config.EndOfMapVote;
            _rtvConfig = config.Rtv;
            //_screenConfig = config.ScreenMenu;

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
            TimeLeft = 0;
            mapsElected.Clear();
            KillTimer();
        }

        public void ScheduleNextVote()
        {
            int newRemainingSeconds = (int)(_gameRules.RoundTime - (Server.CurrentTime - _gameRules.GameStartTime));
            int triggerSeconds = _endMapConfig.TriggerSecondsBeforeEnd;
            int delay = Math.Max(newRemainingSeconds - triggerSeconds, 0);
            
            _plugin?.AddTimer(delay, () =>
            {
                _pluginState.EofVoteHappening = false;
                _changeMapManager.OnMapStart(Server.MapName);
                StartVote(isRtv: false);
            }, TimerFlags.STOP_ON_MAPCHANGE);
        }

        public void MapVoted(CCSPlayerController player, string mapName, bool isRtv)
        {
            var userId = player.UserId!.Value;

            // Block multiple votes if more than 1 VoteType is enabled
            if (VotedPlayers.Contains(userId))
                return;
            
            // Record the players vote
            VotedPlayers.Add(userId);
            
            // Count their vote towards the map
            Votes[mapName] += 1;
            player.PrintToChat(_localizer.LocalizeWithPrefix("emv.you-voted", mapName));

            // Make sure to close ScreenMenu if they voted via ChatMenu
            //if (_endMapConfig.MenuType == "ScreenMenu")
                //MapVoteScreenMenu.Close(player);
            
            // If we’ve reached the vote threshold, end the vote early
            if (Votes.Values.Sum() >= _canVote)
                EndVote(isRtv);
        }

        public void KillTimer()
        {
            TimeLeft = -1;
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
        
        public void ChatCountdown(int secondsLeft)
        {
            if (!_pluginState.EofVoteHappening || !_endMapConfig.EnableCountdown || _endMapConfig.CountdownType != "chat")
                return;

            string text = _localizer.LocalizeWithPrefix("general.chat-countdown", secondsLeft);
            foreach (var player in ServerManager.ValidPlayers())
                player.PrintToChat(text);

            int next = secondsLeft - _endMapConfig.ChatCountdownInterval;
            if (next > 0)
            {
                _plugin?.AddTimer(
                    _endMapConfig.ChatCountdownInterval, () =>
                    {
                        try
                        {
                            ChatCountdown(next);
                        }
                        catch (Exception ex)
                        {
                            _plugin.Logger.LogError($"ChatCountdown timer callback failed: {ex.Message}");
                        }
                    }, TimerFlags.STOP_ON_MAPCHANGE
                );
            }
        }

        public void StartVote(bool isRtv)
        {
            if (_pluginState.EofVoteHappening)
                return;
            
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

            int maxExt = _generalConfig.MaxMapExtensions;
            bool unlimited = maxExt <= 0;  // treat 0 or negative as unlimited

            bool canShowExtendOption = !isRtv
                && _endMapConfig.IncludeExtendCurrentMap
                && (unlimited || _pluginState.MapExtensionCount < maxExt);

            int mapsToShow = !isRtv
                ? (_endMapConfig.MapsToShow == 0 ? MaxOptionsHud : _endMapConfig.MapsToShow)
                : (_rtvConfig.MapsToShow    == 0 ? MaxOptionsHud : _rtvConfig.MapsToShow);
            
            // Cap for CenterHtmlMenu (HUD) pages
            if (string.Equals(_endMapConfig.MenuType?.Trim(), "CenterHtmlMenu", StringComparison.Ordinal)
                && mapsToShow > MaxOptionsHud)
            {
                mapsToShow = MaxOptionsHud;
            }

            int mapOptionsCount = canShowExtendOption ? mapsToShow - 1 : mapsToShow;

            // Get map list
            var mapsScrambled = Shuffle(new Random(), _mapLister.Maps!.Select(x => x.Name)
                .Where(x => x != Server.MapName && !_mapCooldown.IsMapInCooldown(x)).ToList());

            mapsElected = [.. _nominationManager.NominationWinners().Concat(mapsScrambled).Distinct()];

            // Create vote list
            List<string> voteOptions = new();
            foreach (var map in mapsElected.Take(mapOptionsCount))
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

            var title = _localizer.Localize("emv.hud.menu-title");
            var key = _endMapConfig.MenuType?.Trim() ?? "";
            var menuType = MenuManager.MenuTypesList.TryGetValue(key, out var resolvedType)
                ? resolvedType
                : MenuTypeManager.GetDefaultMenu();

            // Open Menu (config dependant)
            foreach (var player in ServerManager.ValidPlayers())
            {
                var menu = MenuManager.MenuByType(menuType, title, _plugin!);
                if (menu is CenterHtmlMenu)
                    menu.ExitButton = false;

                //if (menu is WasdMenu wasd)
                    //wasd.WasdMenu_FreezePlayer = false;

                foreach (var option in voteOptions)
                {
                    var chosen = option;
                    menu.AddItem(chosen, (p, _) =>
                    {
                        MapVoted(p, chosen, isRtv);
                    });
                }

                menu.Display(player, _endMapConfig.VoteDuration);

                if (menu is not ChatMenu)
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("emv.vote-started"));

                if (_endMapConfig.SoundEnabled)
                    player.ExecuteClientCommand($"play {_endMapConfig.SoundPath}");
            }
            
            ChatCountdown(isRtv ? _rtvConfig.MapVoteDuration : _endMapConfig.VoteDuration);

            TimeLeft = isRtv ? _rtvConfig.MapVoteDuration : _endMapConfig.VoteDuration;

            Timer = _plugin?.AddTimer(1.0F, () =>
            {
                if (TimeLeft <= 0)
                {
                    EndVote(isRtv);
                }
                else
                {
                    TimeLeft--;
                }
            }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        }

        public void EndVote(bool isRtv)
        {
            /*foreach (var player in ServerManager.ValidPlayers())
            {
                if (player.IsValid)
                {
                    MapVoteScreenMenu.Close(player);
                }
            }*/
            
            KillTimer();
            
            bool mapEnd = !isRtv && !_endMapConfig.ChangeMapImmediately;
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
                int maxVotes = Votes.Values.Max();
                var tiedMaps = Votes.Where(kv => kv.Value == maxVotes).Select(kv => kv.Key).ToList();
                string chosenKey = tiedMaps[rnd.Next(tiedMaps.Count)];
                winner = new KeyValuePair<string,int>(chosenKey, maxVotes);
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
                
                ScheduleNextVote();
            }
            else
            {
                _changeMapManager.ScheduleMapChange(winner.Key, mapEnd: mapEnd);

                if (!isRtv)
                {
                    if (_endMapConfig.ChangeMapImmediately)
                    {
                        _changeMapManager.ChangeNextMap(mapEnd);
                    }
                    else
                    {
                        if (!mapEnd)
                            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("general.changing-map-next-round", winner.Key));

                        var ignoreRoundWinConditions = ConVar.Find("mp_ignore_round_win_conditions");
                        if (ignoreRoundWinConditions?.GetPrimitiveValue<bool>() == true)
                        {
                            Timer? checkTimer = null;
                            checkTimer = _plugin?.AddTimer(1.0F, () =>
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
                }
                else
                {
                    if (!_rtvConfig.ChangeAtRoundEnd)
                    {
                        var delay = _rtvConfig.MapChangeDelay;
                        if (delay <= 0) // Immediate
                        {
                            _changeMapManager.ChangeNextMap(mapEnd);
                        }
                        else // Timer for MapChangeDelay seconds
                        {
                            _plugin?.AddTimer(delay, () =>
                            {
                                _changeMapManager.ChangeNextMap(mapEnd);
                            }, TimerFlags.STOP_ON_MAPCHANGE);
                        }
                    }
                    else
                    {
                        _changeMapManager.ChangeNextMap(mapEnd: true);
                        Server.PrintToChatAll(_localizer.LocalizeWithPrefix("general.changing-map-next-round", winner.Key));
                    }
                }
                _pluginState.EofVoteHappening = false;
            }
        }
    }
}
