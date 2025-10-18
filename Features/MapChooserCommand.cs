using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;
using Menu;
using Menu.Enums;

namespace cs2_rockthevote
{
    public class MapChooserCommand : IPluginDependency<Plugin, Config>
    {
        private readonly ILogger<MapChooserCommand> _logger;
        private readonly StringLocalizer _localizer;
        private readonly MapLister _mapLister;
        private Plugin? _plugin;
        private KitsuneMenu? _menuManager;

        private string[] _permission = ["@css/root"];
        private MapChooserConfig _config = new();

        public MapChooserCommand(StringLocalizer localizer, MapLister mapLister, ILogger<MapChooserCommand> logger)
        {
            _localizer = localizer;
            _mapLister = mapLister;
            _logger = logger;
        }

        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
            _menuManager = new KitsuneMenu(plugin, multiCast: false);
        }

        public void OnConfigParsed(Config config)
        {
            _config = config.MapChooser;
            _permission = string.IsNullOrWhiteSpace(_config.Permission)
                ? Array.Empty<string>()
                : [.. _config.Permission
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Distinct(StringComparer.Ordinal)];

            if (string.IsNullOrWhiteSpace(_config.Command))
                return;

            Server.NextFrame(() =>
            {
                foreach (var alias in _config.Command.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    _plugin?.AddCommand(alias, "Opens the Map Chooser Menu", ExecuteCommand);
                }
            });
        }

        private void ExecuteCommand(CCSPlayerController? player, CommandInfo info)
        {
            if (player?.IsValid != true)
                return;

            if (_permission.Length > 0)
            {
                bool allowed = _permission.Any(perm => AdminManager.PlayerHasPermissions(player, perm));
                if (!allowed)
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("general.incorrect.permission"));
                    return;
                }
            }

            var maps = _mapLister.Maps;
            if (maps is null || maps.Length == 0)
                return;

            var title = _localizer.Localize("general.choose.map");
            var items = new List<MenuItem> { };

            foreach (var map in maps)
            {
                var mapName = map.Name;

                items.Add(new MenuItem(MenuItemType.Button, new MenuValue(string.Empty),
                    [new MenuButtonCallback(mapName, mapName, (ctrl, _) =>
            {
                if (ctrl?.IsValid != true) return;

                _menuManager!.ClearMenus(ctrl);
                Server.ExecuteCommand($"changelevel {mapName}");
            })]
                ));
            }

            _menuManager!.ShowScrollableMenu(player, title, items, null, false, false, 5);
        }
    }
}