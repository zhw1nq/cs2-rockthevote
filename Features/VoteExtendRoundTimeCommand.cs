using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using cs2_rockthevote.Core;
using Microsoft.Extensions.Localization;

namespace cs2_rockthevote
{
    public partial class Plugin
    {
        [ConsoleCommand("css_voteextend", "Extends time for the current map")]
        [ConsoleCommand("css_ve", "Extends time for the current map")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        [RequiresPermissions("@css/vip")]
        public void OnVoteExtendRoundTimeCommandCommand(CCSPlayerController? player, CommandInfo commandInfo)
        {
            _voteExtendRoundTime.CommandHandler(player!, commandInfo);
        }

        [GameEventHandler(HookMode.Pre)]
        public HookResult EventPlayerDisconnectExtend(EventPlayerDisconnect @event, GameEventInfo @eventInfo)
        {
            var player = @event.Userid;
            _rtvManager.PlayerDisconnected(player);
            return HookResult.Continue;
        }
    }

    public class VoteExtendRoundTimeCommand(TimeLimitManager timeLimitManager, ExtendRoundTimeManager extendRoundTimeManager, GameRules gameRules, IStringLocalizer stringLocalizer, PluginState pluginState) : IPluginDependency<Plugin, Config>
    {
        private TimeLimitManager _timeLimitManager = timeLimitManager;
        private ExtendRoundTimeManager _extendRoundTimeManager = extendRoundTimeManager;
        private readonly GameRules _gameRules = gameRules;
        private StringLocalizer _localizer = new StringLocalizer(stringLocalizer, "extendtime.prefix");
        private PluginState _pluginState = pluginState;
        private RtvConfig _config = new();
        private VoteExtendConfig _voteExtendConfig = new();


        public void CommandHandler(CCSPlayerController player, CommandInfo commandInfo)
        {
            if (!_voteExtendConfig.Enabled)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("extendtime.disbled"));
                return;
            }
            
            if (_gameRules.WarmupRunning)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.warmup"));
                return;
            }

            if (!_timeLimitManager.UnlimitedTime)
            {
                if (!_pluginState.ExtendTimeVoteHappening)
                {
                    _extendRoundTimeManager.StartVote(_config);
                }
                else
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("extendtime.notapplicable"));
                }
            }
            else
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("extendtime.notapplicable"));
            }
        }

        public void OnConfigParsed(Config config)
        {
            _config = config.Rtv;
        }
           internal void CommandHandler(CCSPlayerController player)
        {
            throw new NotImplementedException();
        }
    }
}