using CounterStrikeSharp.API.Core;
using CS2ScreenMenuAPI.Enums;
using CS2ScreenMenuAPI.Internal;

namespace cs2_rockthevote
{
    public static class MapVoteScreenMenu
    {
        public static void Open(Plugin plugin, CCSPlayerController player, List<string> voteOptions, Action<CCSPlayerController, string> onOptionSelected)
        {
            ScreenMenu menu = new ScreenMenu("Map Vote:", plugin)
            {
                PostSelectAction = PostSelectAction.Close,
            };

            int optionIndex = 1;
            foreach (var option in voteOptions)
            {
                // Prefix the option with a number e.g. "!1 surf_test"
                string optionText = $"!{optionIndex} {option}";
                menu.AddOption(optionText, (p, selectedOption) =>
                {
                    onOptionSelected(p, option);
                });
                optionIndex++;
            }
            menu.Open(player);
        }
    }
}