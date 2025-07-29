using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CS2MenuManager.API.Class;
using Microsoft.Extensions.Logging;

namespace cs2_rockthevote
{
    public class MapChooserCommand : IPluginDependency<Plugin, Config>
    {
        private readonly ILogger<MapChooserCommand> _logger;
        private readonly StringLocalizer _localizer;
        private readonly MapLister _mapLister;
        private readonly BasePlugin _plugin;
        private readonly string _permission;
        private BaseMenu? _mapMenu;
        private MapChooserConfig _config = new();


        public MapChooserCommand(StringLocalizer localizer, BasePlugin plugin, MapLister mapLister, ILogger<MapChooserCommand> logger, MapChooserConfig config)
        {
            _localizer = localizer;
            _plugin = plugin;
            _mapLister = mapLister;
            _logger = logger;
            _config = config;
            _permission = config.Permission;

            BuildMenu();
        }

        public void OnConfigParsed(Config config)
        {
            _config = config.MapChooser;

            foreach (var alias in _config.Command.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                _plugin.AddCommand(alias, "Open Map Chooser", ExecuteCommand);
            }
        }

        private void ExecuteCommand(CCSPlayerController? player, CommandInfo info)
        {
            if (player == null || !player.IsValid)
                return;

            if (!AdminManager.PlayerHasPermissions(player, _permission))
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.incorrect.permission"));
                return;
            }

            BuildMenu();
            _mapMenu?.Display(player, -1);
        }

        private void BuildMenu()
        {
            var menuType = _config.MenuType;
            _mapMenu = MenuManager.MenuByType(menuType, "Choose a Map:", _plugin);

            foreach (var map in _mapLister.Maps!)
            {
                _mapMenu.AddItem(map.Name, (player, option) =>
                {
                    if (player == null || !player.IsValid)
                        return;

                    MenuManager.CloseActiveMenu(player);

                    if (!string.IsNullOrEmpty(map.Id) && ulong.TryParse(map.Id, out var mapId))
                        Server.ExecuteCommand($"host_workshop_map {mapId}");
                    else
                        Server.ExecuteCommand($"changelevel {map.Name}");
                });
            }
        }
    }
}