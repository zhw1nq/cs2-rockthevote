using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using cs2_rockthevote.Core;
using Microsoft.Extensions.Logging;

namespace cs2_rockthevote
{
    public partial class Plugin
    {
        [ConsoleCommand("css_nom", "Nominate a map to appear in the vote.")]
        [ConsoleCommand("css_nominate", "Nominate a map to appear in the vote.")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnNominateCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null)
                return;
            
            // If "Permission" is blank or whitespace, allow everyone. Otherwise enforce it
            string perm = Config.Nominate.Permission;
            bool hasPerm = string.IsNullOrWhiteSpace(perm) || AdminManager.PlayerHasPermissions(player, perm);

            if (!hasPerm)
            {
                command.ReplyToCommand(_localizer.LocalizeWithPrefix("general.incorrect.permission"));
                return;
            }

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
        private NominateConfig _nomConfig = new();
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
            _nomConfig = config.Nominate;
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

        public void CommandHandler(CCSPlayerController? player, string map)
        {
            if (player == null)
                return;
            
            var userId = player.UserId!.Value;
            var mapName = map.Trim();

            if (_pluginState.DisableCommands || !_nomConfig.Enabled || _pluginState.EofVoteHappening)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.disabled"));
                return;
            }

            // Get or init this players noms
            if (!Nominations.TryGetValue(userId, out var userNominations))
            {
                userNominations = new List<string>();
                Nominations[userId] = userNominations;
            }

            // Enforce per‐player nomination limit
            if (userNominations.Count >= _nomConfig.NominateLimit)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("nominate.limit", _nomConfig.NominateLimit));
                return;
            }

            // Prevent nominating the same map multiple times
            if (userNominations.Contains(mapName, StringComparer.OrdinalIgnoreCase))
            {
                int voteCount = Nominations.Values.SelectMany(v => v).Count(m => m.Equals(mapName, StringComparison.OrdinalIgnoreCase));
                player.PrintToChat( _localizer.LocalizeWithPrefix("nominate.already-nominated", mapName, voteCount));
                return;
            }

            if (_gamerules.WarmupRunning)
            {
                if (!_nomConfig.EnabledInWarmup)
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.warmup"));
                    return;
                }
            }

            if (string.IsNullOrEmpty(mapName))
            {
                if (_nomConfig.MenuType == "ScreenMenu")
                {
                    OpenScreenMenu(player);
                }
                else if (_nomConfig.MenuType == "HudMenu" && nominationMenuHud != null)
                {
                    MenuManager.OpenCenterHtmlMenu(_plugin!, player, nominationMenuHud);
                }
                else if (_nomConfig.MenuType == "ChatMenu")
                {
                    OpenChatMenu(player);
                }
                else
                {
                    OpenChatMenu(player);
                    _logger.LogError("Incorrect MenuType set in the Nominate config. Please choose either ScreenMenu/ChatMenu/HudMenu. Falling back to ChatMenu.");
                }
                
                return;
            }

            var resolved = ResolveMapNameOrPrompt(player, mapName, _localizer);
            if (resolved == null)
                return;
            
            // Save the nom count per-player
            userNominations.Add(resolved);

            // Now there is exactly one map name to nominate
            Nominate(player, resolved);
        }

        public void OpenChatMenu(CCSPlayerController player)
        {
            MenuManager.OpenChatMenu(player, nominationMenu!);
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

        // Attempt to resolve the user's text into exactly one map. If 0 matches -> send "invalid" and return null.
        // If > 1 matches -> show the chat based menu and return null. Otherwise -> return the single map name.
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