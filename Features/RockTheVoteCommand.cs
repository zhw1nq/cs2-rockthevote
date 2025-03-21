using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace cs2_rockthevote
{
    public partial class Plugin
    {
        [ConsoleCommand("css_rtv", "Votes to rock the vote")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnRTV(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null)
                return;

            PanoramaVote.Init();
            _rtvManager.CommandHandler(player!);
        }

        [GameEventHandler(HookMode.Pre)]
        public HookResult EventPlayerDisconnectRTV(EventPlayerDisconnect @event, GameEventInfo @eventInfo)
        {
            var player = @event.Userid;
            if (player != null)
            {
                _rtvManager.PlayerDisconnected(player);
            }
            return HookResult.Continue;
        }
    }

    public class RockTheVoteCommand : IPluginDependency<Plugin, Config>
    {
        private readonly StringLocalizer _localizer;
        private readonly GameRules _gameRules;
        private readonly EndMapVoteManager _endmapVoteManager;
        private readonly PluginState _pluginState;
        private RtvConfig _config = new();
        //private AsyncVoteManager? _voteManager;
        private bool _isCooldownActive = false;
        private CCSPlayerController? _initiatingPlayer;
        private readonly ILogger<RockTheVoteCommand> _logger;
        private YesNoVoteInfo? _currentVoteInfo;
        
        public RockTheVoteCommand(
            GameRules gameRules, 
            EndMapVoteManager endmapVoteManager, 
            StringLocalizer localizer, 
            PluginState pluginState,
            ILogger<RockTheVoteCommand> logger)
        {
            _localizer = localizer;
            _gameRules = gameRules;
            _endmapVoteManager = endmapVoteManager;
            _pluginState = pluginState;
            _logger = logger;
        }
        
        /*public void OnMapStart(string map)
        {
            _voteManager?.OnMapStart(map);
        }*/

        public void CommandHandler(CCSPlayerController? player)
        {
            try
            {
                if (player == null)
                    return;

                _initiatingPlayer = player;

                if (_isCooldownActive)
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("rtv.cooldown"));
                    return;
                }
                if (_pluginState.DisableCommands || !_config.Enabled)
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("rtv.disabled"));
                    return;
                }
                if (_gameRules.WarmupRunning && !_config.EnabledInWarmup)
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.warmup"));
                    return;
                }
                if (_config.MinRounds > 0 && _config.MinRounds > _gameRules.TotalRoundsPlayed)
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.minimum-rounds", _config.MinRounds));
                    return;
                }
                if (ServerManager.ValidPlayerCount() < _config.MinPlayers)
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.minimum-players", _config.MinPlayers));
                    return;
                }

                PanoramaVote.SendYesNoVoteToAll(
                    _config.VoteDuration,
                    VoteConstants.VOTE_CALLER_SERVER,
                    "#SFUI_vote_changelevel",
                    _localizer.Localize("rtv.ui-question"),
                    VoteResultCallback,
                    VoteHandlerCallback
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Something went wrong with the rtv command: {message}", ex.Message);
            }
        }

        private bool VoteResultCallback(YesNoVoteInfo info)
        {
            _currentVoteInfo = info;
            int requiredYesVotes = (int)Math.Ceiling(info.num_clients * (_config.VotePercentage / 100.0));

            if (info.yes_votes >= requiredYesVotes)
            {
                Server.PrintToChatAll($"{_localizer.LocalizeWithPrefix("rtv.votes-reached")}");
                
                if (_config.ScreenMenu)
                {
                    _ = new Timer(3.5F, () =>
                    {
                        _endmapVoteManager.StartVote(_config);
                    });
                }
                else
                {
                    _endmapVoteManager.StartVote(_config);
                }
                
                return true;
            }
            else
            {
                Server.PrintToChatAll($"{_localizer.LocalizeWithPrefix("rtv.failed")}");
                ActivateCooldown();
                return false;
            }
        }

        private void VoteHandlerCallback(YesNoVoteAction action, int param1, int param2)
        {
            CCSPlayerController? player = null;

            switch (action)
            {
                case YesNoVoteAction.VoteAction_Start:
                    Server.PrintToChatAll($"{_localizer.LocalizeWithPrefix("rtv.rocked-the-vote", _initiatingPlayer!.PlayerName)}");
                    break;
                    
                case YesNoVoteAction.VoteAction_Vote:
                    if (player == null || !player.IsValid || player.Connected != PlayerConnectedState.PlayerConnected)
                        return;
                    break;
                    
                case YesNoVoteAction.VoteAction_End:
                    if ((YesNoVoteEndReason)param1 == YesNoVoteEndReason.VoteEnd_Cancelled)
                    {
                        Server.PrintToChatAll($"{_localizer.LocalizeWithPrefix("rtv.cancelled")}");
                    }
                    else if ((YesNoVoteEndReason)param1 == YesNoVoteEndReason.VoteEnd_TimeUp)
                    {
                        Server.PrintToChatAll($"{_localizer.LocalizeWithPrefix("rtv.time-up")}");
                    }
                    break;
            }
        }

        private void ActivateCooldown()
        {
            _isCooldownActive = true;

            _ = new Timer(_config.CooldownDuration, () =>
            {
                _isCooldownActive = false;
            });
        }

        public void PlayerDisconnected(CCSPlayerController? player)
        {
            if (player?.UserId != null)
            {
                PanoramaVote.RemovePlayerFromVote(player.Slot);
            }
        }

        public void OnConfigParsed(Config config)
        {
            _config = config.Rtv;
        }
    }
}