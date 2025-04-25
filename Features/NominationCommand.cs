
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using cs2_rockthevote.Core;

namespace cs2_rockthevote
{
    public partial class Plugin
    {
        [ConsoleCommand("nominate", "Nominate a map to appear in the vote.")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnNominateCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) return;
            _nominationManager.CommandHandler(player, command.GetArg(1)?.Trim().ToLower() ?? "");
        }

        [GameEventHandler(HookMode.Pre)]
        public HookResult EventPlayerDisconnectNominate(EventPlayerDisconnect @event, GameEventInfo @eventInfo)
        {
            var player = @event.Userid;
            if (player != null)
            {
                _nominationManager.PlayerDisconnected(player);
            }
            return HookResult.Continue;
        }
    }

    public class NominationCommand : IPluginDependency<Plugin, Config>
    {
        Dictionary<int, List<string>> Nominations = new();
        ChatMenu? nominationMenu = null;
        private RtvConfig _config = new();
        private VoteTypeConfig _voteTypeConfig = new();
        private GameRules _gamerules;
        private StringLocalizer _localizer;
        private PluginState _pluginState;
        private MapCooldown _mapCooldown;
        private MapLister _mapLister;
        private Plugin? _plugin;

        public NominationCommand(MapLister mapLister, GameRules gamerules, StringLocalizer localizer, PluginState pluginState, MapCooldown mapCooldown)
        {
            _mapLister = mapLister;
            _mapLister.EventMapsLoaded += OnMapsLoaded;
            _gamerules = gamerules;
            _localizer = localizer;
            _pluginState = pluginState;
            _mapCooldown = mapCooldown;
            _mapCooldown.EventCooldownRefreshed += OnMapsLoaded;
        }


        public void OnMapStart(string map)
        {
            Nominations.Clear();
        }
        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
        }

        public void OnConfigParsed(Config config)
        {
            _config = config.Rtv;
            _voteTypeConfig = config.VoteType;
        }

        public void OnMapsLoaded(object? sender, Map[] maps)
        {
            nominationMenu = new("Nomination");
            foreach (var map in _mapLister.Maps!.Where(x => x.Name != Server.MapName))
            {
                nominationMenu.AddMenuOption(map.Name, (CCSPlayerController player, ChatMenuOption option) =>
                {
                    Nominate(player, option.Text);
                }, _mapCooldown.IsMapInCooldown(map.Name));
            }
        }

        public void OpenScreenMenu(CCSPlayerController player)
        {
            // Build the list of map names, skipping the current map and the ones on cool down
            var voteOptions = _mapLister.Maps!
                .Where(m => m.Name != Server.MapName 
                        && !_mapCooldown.IsMapInCooldown(m.Name))
                .Select(m => m.Name)
                .ToList();

            // Prime the menu show it appears on the first call
            MapVoteScreenMenu.Prime(_plugin!, player);

            _plugin!.AddTimer(0.1f, () =>
                MapVoteScreenMenu.Open(
                    _plugin!, 
                    player, 
                    voteOptions, 
                    (p, mapName) => CommandHandler(p, mapName),
                    "Nominate a Map"
                )
            );
        }

        public void CommandHandler(CCSPlayerController? player, string map)
        {
            if (player is null)
                return;

            map = map.ToLower().Trim();

            if (_pluginState.DisableCommands || !_config.NominationEnabled || _pluginState.EofVoteHappening)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.disabled"));
                return;
            }

            if (_gamerules.WarmupRunning)
            {
                if (!_config.EnabledInWarmup)
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.warmup"));
                    return;
                }
            }
            else if (_config.MinRounds > 0 && _config.MinRounds > _gamerules.TotalRoundsPlayed)
            {
                player!.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.minimum-rounds", _config.MinRounds));
                return;
            }

            if (ServerManager.ValidPlayerCount() < _config!.MinPlayers)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.minimum-players", _config!.MinPlayers));
                return;
            }

            if (string.IsNullOrEmpty(map))
            {
                if (_voteTypeConfig.EnableScreenMenu)
                {
                    OpenScreenMenu(player);
                }
                else
                {
                    OpenChatNomination(player!);
                }
            }
            else
            {
                Nominate(player, map);
            }
        }

        public void OpenChatNomination(CCSPlayerController player)
        {
            MenuManager.OpenChatMenu(player!, nominationMenu!);
        }

        void Nominate(CCSPlayerController player, string map)
        {
            map = map.ToLower().Trim();

            if (map == Server.MapName)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.current-map"));
                return;
            }

            string matchingMap = _mapLister.GetSingleMatchingMapName(map, player, _localizer);
            if (string.IsNullOrEmpty(matchingMap))
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.invalid-map"));
                return;
            }

            if (_mapCooldown.IsMapInCooldown(matchingMap))
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.map-played-recently"));
                return;
            }

            var userId = player.UserId!.Value;
            if (!Nominations.ContainsKey(userId))
                Nominations[userId] = new();

            bool alreadyVoted = Nominations[userId].Contains(matchingMap);
            if (!alreadyVoted)
                Nominations[userId].Add(matchingMap);

            var totalVotes = Nominations.Select(x => x.Value.Count(y => y == matchingMap)).Sum();

            if (!alreadyVoted)
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("nominate.nominated", player.PlayerName, matchingMap, totalVotes));
            else
                player.PrintToChat(_localizer.LocalizeWithPrefix("nominate.already-nominated", matchingMap, totalVotes));
        }

        public List<string> NominationWinners()
        {
            if (Nominations.Count == 0)
                return new List<string>();

            var rawNominations = Nominations
                .Select(x => x.Value)
                .Aggregate((acc, x) => acc.Concat(x).ToList());

            return [.. rawNominations
                .Distinct()
                .Select(map => new KeyValuePair<string, int>(map, rawNominations.Count(x => x == map)))
                .OrderByDescending(x => x.Value)
                .Select(x => x.Key)];
        }

        public void PlayerDisconnected(CCSPlayerController player)
        {
            int userId = player.UserId!.Value;
            if (!Nominations.ContainsKey(userId))
                Nominations.Remove(userId);
        }
    }
}
