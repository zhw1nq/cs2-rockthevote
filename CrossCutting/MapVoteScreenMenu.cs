using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CS2ScreenMenuAPI;

namespace cs2_rockthevote
{
    public static class MapVoteScreenMenu
    {
        public static void Open(Plugin plugin, CCSPlayerController player, List<string> voteOptions, Action<CCSPlayerController, string> onOptionSelected, string title)
        {
            var cfg = plugin.Config.ScreenMenu;

            var menu = new Menu(player, plugin)
            {
                Title = title,
                ShowResolutionOption = cfg.EnableResolutionOption,
                HasExitButon = cfg.EnableExitOption,
                PostSelect = PostSelect.Close,
                MenuType = MenuType.Both,
                ShowDisabledOptionNum = false,
                ShowControlsInfo = true,
            };

            for (int i = 0; i < voteOptions.Count; i++)
            {
                int idx = i;
                menu.AddItem(voteOptions[idx], (p, _) => onOptionSelected(p, voteOptions[idx]));
            }

            menu.Display();
        }


        public static void Primer(Plugin plugin, CCSPlayerController player)
        {

            var menu = new Menu(player, plugin)
            {
                HasExitButon = false,
                ShowResolutionOption = false,
            };
            menu.AddItem(" ", (p, o) => { });
            menu.Close(player);
        }

        public static void Close(CCSPlayerController player)
        {
            MenuAPI.CloseActiveMenu(player);
        }
    }
}
