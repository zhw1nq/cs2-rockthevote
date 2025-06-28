using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using cs2_rockthevote.Core;
using Microsoft.Extensions.Logging;

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
        private readonly ILogger<NominationCommand> _logger;
        Dictionary<int, List<string>> Nominations = new();
        ChatMenu? nominationMenu = null;
        CenterHtmlMenu? nominationMenuHud = null;
        private RtvConfig _config = new();
        private VoteTypeConfig _voteTypeConfig = new();
        private GameRules _gamerules;
        private StringLocalizer _localizer;
        private PluginState _pluginState;
        private MapCooldown _mapCooldown;
        private MapLister _mapLister;
        private Plugin? _plugin;

        public NominationCommand(MapLister mapLister, GameRules gamerules, StringLocalizer localizer, PluginState pluginState, MapCooldown mapCooldown, ILogger<NominationCommand> logger)
        {
            _mapLister = mapLister;
            _mapLister.EventMapsLoaded += OnMapsLoaded;
            _gamerules = gamerules;
            _localizer = localizer;
            _pluginState = pluginState;
            _mapCooldown = mapCooldown;
            _logger = logger;
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
            nominationMenu = new(_localizer.Localize("nominate.title"));
            foreach (var map in _mapLister.Maps!.Where(x => !GetBaseMapName(x.Name).Equals(Server.MapName, StringComparison.OrdinalIgnoreCase)))
            {
                nominationMenu.AddMenuOption(_mapCooldown.IsMapInCooldown(map.Name) ? $"{ChatColors.Grey}{map.Name}" : map.Name, (player, option) =>
                {
                    Nominate(player, option.Text);
                },
                _mapCooldown.IsMapInCooldown(map.Name)
            );
            }
        }

        public void OpenScreenMenu(CCSPlayerController player)
        {
            // Build the list of map names, skipping the current map and the ones on cool down
            var voteOptions = _mapLister.Maps!
                .Where(m => !GetBaseMapName(m.Name)
                       .Equals(Server.MapName, StringComparison.OrdinalIgnoreCase)
                    && !_mapCooldown.IsMapInCooldown(m.Name))
                .Select(m => m.Name)
                .ToList();

            // Guard: nothing to nominate
            if (voteOptions.Count == 0)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("An error occured."));
                _logger.LogError("[Nominate] An error occured while using the !nominate command with ScreenMenu, no maps could be found.");
                return;
            }

            // Once the list is built, we open the menu on the next frame
            Server.NextFrame(() =>
                MapVoteScreenMenu.Open(
                    _plugin!,
                    player,
                    voteOptions,
                    (p, mapName) => CommandHandler(p, mapName),
                    _localizer.Localize("nominate.title")
            ));
        }

        public void CommandHandler(CCSPlayerController? player, string map)
        {
            if (player == null)
                return;
            
            var userId = player.UserId!.Value;
            var mapName = map.Trim().ToLower();

            if (_pluginState.DisableCommands || !_config.NominationEnabled || _pluginState.EofVoteHappening)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.disabled"));
                return;
            }

            // Can't nominate more than once per map
            if (Nominations.ContainsKey(userId))
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("nominate.limit"));
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
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.minimum-rounds", _config.MinRounds));
                return;
            }

            if (ServerManager.ValidPlayerCount() < _config.MinPlayers)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.minimum-players", _config!.MinPlayers));
                return;
            }

            if (string.IsNullOrEmpty(mapName))
            {
                // All 3 menu types can be used. However, if none are enabled for some reason, throw an error and fall back to chat menu
                if (!(_voteTypeConfig.EnableScreenMenu || _voteTypeConfig.EnableHudMenu || _voteTypeConfig.EnableChatMenu))
                {
                    _plugin!.Logger.LogError("No menu types enabled in VoteType section of the config, please enable at least one. Falling back to chat menu.");
                    OpenChatNomination(player);
                    return;
                }

                if (_voteTypeConfig.EnableScreenMenu)
                    OpenScreenMenu(player);

                if (_voteTypeConfig.EnableHudMenu && nominationMenuHud != null)
                    MenuManager.OpenCenterHtmlMenu(_plugin!, player, nominationMenuHud);

                if (_voteTypeConfig.EnableChatMenu)
                    OpenChatNomination(player);
                
                return;
            }

            var resolved = ResolveMapNameOrPrompt(player, mapName, _localizer);
            if (resolved == null)
                return;

            // Now there is exactly one map name to nominate
            Nominate(player, resolved);
        }

        public void OpenChatNomination(CCSPlayerController player)
        {
            MenuManager.OpenChatMenu(player, nominationMenu!);
        }

        public void Nominate(CCSPlayerController player, string map)
        {
            var mapName = map.Trim();
            var baseName = GetBaseMapName(mapName);

            // Can't nominate the current map
            if (baseName.Equals(Server.MapName, StringComparison.OrdinalIgnoreCase))
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.current-map"));
                return;
            }

            // Can't nominate a map on cooldown
            if (_mapCooldown.IsMapInCooldown(baseName))
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.map-played-recently"));
                return;
            }

            var userId = player.UserId!.Value;

            if (Nominations.ContainsKey(userId))
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("nominate.limit"));
                return;
            }

            Nominations[userId] = new List<string> { map };

            int totalVotes = Nominations.Values.Count(list => list.Contains(map));

            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("nominate.nominated", player.PlayerName, map, totalVotes));
        }

        private void ShowMultipleMatchesMenu(CCSPlayerController player, List<string> matchingMaps)
        {
            var menu = new ChatMenu(_localizer.Localize("nominate.multiple-maps"));
            foreach (var name in matchingMaps)
            {
                menu.AddMenuOption(
                    name,
                    (p, opt) => Nominate(p, opt.Text),
                    false
                );
            }
            MenuManager.OpenChatMenu(player, menu);
        }

        // Attempt to resolve the user’s text into exactly one map. If 0 matches → send "invalid" and return null.
        // If > 1 matches → show the mini‐menu and return null. Otherwise → return the single map name.
        private string? ResolveMapNameOrPrompt(CCSPlayerController player, string input, StringLocalizer localizer)
        {
            // Exact match
            var exact = _mapLister.GetExactMapName(input);
            if (exact is not null)
                return exact;

            // Find all "contains" matches
            var matches = _mapLister.GetMatchingMapNames(input);

            // No matches found
            if (matches.Count == 0)
            {
                player.PrintToChat(localizer.LocalizeWithPrefix("general.invalid-map"));
                return null;
            }
            // Found more than 1 match
            if (matches.Count > 1)
            {
                ShowMultipleMatchesMenu(player, matches);
                return null;
            }

            // Exactly one
            return matches[0];
        }

        private string GetBaseMapName(string displayName)
        {
            var idx = displayName.IndexOf(" (", StringComparison.Ordinal);
            return idx >= 0
                ? displayName.Substring(0, idx)
                : displayName;
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
            Nominations.Remove(userId);
        }
    }
}