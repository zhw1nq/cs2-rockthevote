using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace cs2_rockthevote
{
    public class MaplistCommand : IPluginDependency<Plugin, Config>
    {
        private readonly MapLister _mapLister;
        private Dictionary<ulong, DateTime> _lastCommandUse = new Dictionary<ulong, DateTime>();
        private const int CommandCooldown = 5;

        public MaplistCommand(MapLister mapLister)
        {
            _mapLister = mapLister;
        }


        [ConsoleCommand("css_maps", "Displays the available maps in console")]
        [ConsoleCommand("css_maplist", "Displays the available maps in console")]
        public void OnMaplistCommand(CCSPlayerController? player, CommandInfo command)
        {
            try
            {
                if (player != null)
                {
                    ulong steamId = player.SteamID;
                    if (_lastCommandUse.TryGetValue(steamId, out DateTime lastUse))
                    {
                        int secondsLeft = CommandCooldown - (int)(DateTime.Now - lastUse).TotalSeconds;
                        if (secondsLeft > 0)
                        {
                            player.PrintToChat($"{ChatColors.Blue}[MapList]{ChatColors.Default} Please wait {secondsLeft} seconds before using this command again");
                            return;
                        }
                    }
                    _lastCommandUse[steamId] = DateTime.Now;
                }

                var maps = _mapLister.Maps;
                if (maps == null || maps.Length == 0)
                {
                    ReplyToCommand(player, "The map list is empty!");
                    return;
                }

                string breaker = "====================================";

                // If called by player, tell them to check console
                player?.PrintToChat($" {ChatColors.LightRed}[MapList]{ChatColors.Default} Maps have been printed to the console.");

                // Pick how to send
                Action<string> printLine = player != null
                    ? new Action<string>(player.PrintToConsole)
                    : Console.WriteLine;

                printLine(breaker);
                printLine("             Server Map List");
                printLine($"             Total Maps: {maps.Length}");
                printLine(breaker);

                foreach (var map in maps)
                {
                    if (string.IsNullOrWhiteSpace(map.Name)) continue;
                    printLine(map.Name);
                }

                printLine(breaker);
            }
            catch (Exception ex)
            {
                ReplyToCommand(player, $"An error occurred while reading the map list: {ex.Message}");
            }
        }

        private void ReplyToCommand(CCSPlayerController? player, string message)
        {
            if (player == null)
                Console.WriteLine(message);
            else
                player.PrintToChat($" {ChatColors.LightRed}[MapList]{ChatColors.Default} {message}");
        }
    }
}