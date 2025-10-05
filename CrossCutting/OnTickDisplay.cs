using System.Text;
using static CounterStrikeSharp.API.Core.Listeners;

namespace cs2_rockthevote.CrossCutting
{
    public class OnTickDisplay : IPluginDependency<Plugin, Config>
    {
        private readonly PluginState _pluginState;
        private readonly EndMapVoteManager _endMap;
        private readonly RockTheVoteCommand _rtv;
        private readonly StringLocalizer _localizer;
        private GeneralConfig _generalConfig = new();
        private EndOfMapConfig _endMapConfig = new();
        private RtvConfig _rtvConfig = new();

        public OnTickDisplay(PluginState pluginState, StringLocalizer localizer, EndMapVoteManager endMap, RockTheVoteCommand rtv)
        {
            _pluginState = pluginState;
            _localizer = localizer;
            _endMap = endMap;
            _rtv = rtv;
        }

        public void OnConfigParsed(Config config)
        {
            _generalConfig = config.General;
            _endMapConfig = config.EndOfMapVote;
            _rtvConfig = config.Rtv;
        }

        public void OnLoad(Plugin plugin)
        {
            if (_endMapConfig.CountdownType == "hud" || _rtvConfig.CountdownType == "hud")
            {
                plugin.RegisterListener<OnTick>(PlayerOnTick);
            }
        }

        public void Unload(Plugin plugin)
        {
            plugin.RemoveListener<OnTick>(PlayerOnTick);
        }

        public void PlayerOnTick()
        {
            if (!_pluginState.EofVoteHappening && !_pluginState.RtvVoteHappening)
                return;

            if (_endMapConfig.EnableCountdown && _endMapConfig.CountdownType == "hud" && _pluginState.EofVoteHappening)
            {
                string countdown = _localizer.Localize("emv.hud.timer", _endMap.TimeLeft);
                foreach (var player in ServerManager.ValidPlayers())
                    player.PrintToCenter(countdown);
            }

            if (_rtvConfig.EnableCountdown && _rtvConfig.CountdownType == "hud" && _pluginState.RtvVoteHappening)
            {
                string countdown = _localizer.Localize("general.hud-countdown", _rtv.TimeLeft);
                foreach (var player in ServerManager.ValidPlayers())
                    player.PrintToCenter(countdown);
            }
        }
    }
}