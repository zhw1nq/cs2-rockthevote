using CounterStrikeSharp.API.Core;
using CS2MenuManager.API.Class;
using CS2MenuManager.API.Menu;

namespace cs2_rockthevote
{
    public static class MapVoteScreenMenu
    {
        public static void Open(Plugin plugin, CCSPlayerController player, List<string> voteOptions, Action<CCSPlayerController, string> onOptionSelected, string title)
        {
            var screenCfg = plugin.Config.ScreenMenu;

            var menu = new ScreenMenu(title, plugin)
            {
                ScreenMenu_ShowResolutionsOption = screenCfg.EnableResolutionOption,
                ExitButton = screenCfg.EnableExitOption,
                ScreenMenu_ScrollUpKey = screenCfg.ScrollUpKey,
                ScreenMenu_ScrollDownKey = screenCfg.ScrollDownKey,
                ScreenMenu_SelectKey = screenCfg.SelectKey
            };

            for (int i = 0; i < voteOptions.Count; i++)
            {
                int idx = i; 
                menu.AddItem(
                    voteOptions[i],
                    (p, _) => onOptionSelected(p, voteOptions[idx])
                );
            }

            menu.Display(player, 0);
        }

        public static void Close(CCSPlayerController player)
        {
            MenuManager.CloseActiveMenu(player);
        }
    }
}