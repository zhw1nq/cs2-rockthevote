using System.Text;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using static CounterStrikeSharp.API.Core.Listeners;

namespace cs2_rockthevote.CrossCutting
{
    public class OnTickDisplay : IPluginDependency<Plugin, Config>
    {
        private readonly PluginState _pluginState;
        private readonly EndMapVoteManager _endMap;
        private readonly StringLocalizer _localizer;
        private readonly GeneralConfig _generalConfig;
        private readonly VoteTypeConfig _voteTypeConfig;
        private EndOfMapConfig _endMapConfig = new();
        private VoteExtendConfig _voteExtendConfig = new();
        private RtvConfig _rtvConfig = new();

        public OnTickDisplay(
            PluginState pluginState,
            StringLocalizer localizer,
            GeneralConfig generalConfig,
            VoteTypeConfig voteTypeConfig,
            EndMapVoteManager endMap
        )
        {
            _pluginState = pluginState;
            _localizer = localizer;
            _generalConfig = generalConfig;
            _voteTypeConfig = voteTypeConfig;
            _endMap = endMap;
        }

        public void OnConfigParsed(Config config)
        {
            _endMapConfig = config.EndOfMapVote;
            _voteExtendConfig = config.VoteExtend;
            _rtvConfig = config.Rtv;
        }

        public void OnLoad(Plugin plugin)
        {
            if (_voteTypeConfig.EnableHudMenu || _endMapConfig.HudCountdown || _rtvConfig.HudCountdown || _voteExtendConfig.HudCountdown)
            {
                plugin.RegisterListener<OnTick>(PlayerOnTick);
            }
        }

        public void PlayerOnTick()
        {
            // Only shown while the vote is running
            if (!_pluginState.EofVoteHappening || !_pluginState.ExtendTimeVoteHappening || !PanoramaVote.IsVoteInProgress())
                return;

            // EndMapVote HUD Countdown
            if (_endMap.TimeLeft > 0 && _endMapConfig.EnableCountdown && _endMapConfig.HudCountdown)
            {
                string countdown = _localizer.Localize("emv.hud.hud-timer", _endMap.TimeLeft);
                foreach (var player in ServerManager.ValidPlayers())
                    player.PrintToCenter(countdown);
            }

            // HUD vote list
            if (_voteTypeConfig.EnableHudMenu)
            {
                var sb = new StringBuilder();
                sb.Append($"<b><font color='yellow'>{_localizer.Localize("emv.hud.menu-title")}</font></b>");

                int idx = 1;
                foreach (var kv in _endMap.CurrentVotes.OrderByDescending(x => x.Value).Take(_endMap.MaxOptionsHud))
                {
                    var header = "<br><font color='yellow'>!{0}</font> {1} <font color='lime'>({2})</font>";
                    sb.AppendFormat(header, idx++, kv.Key, kv.Value);
                }

                foreach (var player in ServerManager.ValidPlayers())
                {
                    var userId = player.UserId!.Value;
                    if (_generalConfig.HideHudAfterVote && _endMap.VotedPlayers.Contains(userId))
                        continue;

                    player.PrintToCenterHtml(sb.ToString());
                }
            }
        }
        
    }
}
