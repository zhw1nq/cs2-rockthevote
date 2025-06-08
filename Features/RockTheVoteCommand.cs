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
            
            _rtvManager.CommandHandler(player);
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
        private VoteTypeConfig _voteTypeConfig = new();
        private AsyncVoteManager? _voteManager;
        private bool _isCooldownActive = false;
        private CCSPlayerController? _initiatingPlayer;
        private readonly ILogger<RockTheVoteCommand> _logger;
        private DateTime _cooldownEndTime;

        
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
        
        public void OnMapStart(string map)
        {
            _voteManager?.OnMapStart(map);
        }

        public void CommandHandler(CCSPlayerController? player)
        {
            try
            {
                if (player == null)
                    return;

                _initiatingPlayer = player;

                double elapsed = Server.CurrentTime - _gameRules.GameStartTime;
                if (elapsed < _config.MapStartDelay)
                {
                    int secondsLeft = (int)Math.Ceiling(_config.MapStartDelay - elapsed);
                    player.PrintToChat(
                        _localizer.LocalizeWithPrefix("rtv.cooldown", secondsLeft)
                    );
                    return;
                }
                if (_isCooldownActive)
                {
                    double secondsLeft = Math.Max(0, (_cooldownEndTime - DateTime.UtcNow).TotalSeconds);
                    int secondsInt = (int)Math.Ceiling(secondsLeft);

                    player.PrintToChat(_localizer.LocalizeWithPrefix("rtv.cooldown", secondsInt));
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
                if (_voteTypeConfig.EnablePanorama)
                {
                    PanoramaVote.Init();
                    Server.ExecuteCommand("sv_allow_votes 1");
                    Server.ExecuteCommand("sv_vote_allow_in_warmup 1");
                    Server.ExecuteCommand("sv_vote_allow_spectators 1");
                    Server.ExecuteCommand("sv_vote_count_spectator_votes 1");

                    PanoramaVote.SendYesNoVoteToAll(
                        _config.RtvVoteDuration,
                        player.Slot, // player.Slot Header = Vote by: playerName. VoteConstants.VOTE_CALLER_SERVER Header = Vote by: Server
                        "#SFUI_vote_changelevel",
                        _localizer.Localize("rtv.ui-question"),
                        VoteResultCallback,
                        VoteHandlerCallback
                    );
                }
                if (!_voteTypeConfig.EnablePanorama)
                {
                    VoteResult result = _voteManager!.AddVote(player.UserId!.Value);
                    switch (result.Result)
                    {
                        case VoteResultEnum.Added:
                            Server.PrintToChatAll($"{_localizer.LocalizeWithPrefix("rtv.rocked-the-vote", player.PlayerName)} {_localizer.Localize("general.votes-needed", result.VoteCount, result.RequiredVotes)}");
                            break;
                        case VoteResultEnum.AlreadyAddedBefore:
                            player.PrintToChat($"{_localizer.LocalizeWithPrefix("rtv.already-rocked-the-vote")} {_localizer.Localize("general.votes-needed", result.VoteCount, result.RequiredVotes)}");
                            break;
                        case VoteResultEnum.VotesAlreadyReached:
                            player.PrintToChat(_localizer.LocalizeWithPrefix("rtv.disabled"));
                            break;
                        case VoteResultEnum.VotesReached:
                            Server.PrintToChatAll($"{_localizer.LocalizeWithPrefix("rtv.rocked-the-vote", player.PlayerName)} {_localizer.Localize("general.votes-needed", result.VoteCount, result.RequiredVotes)}");
                            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("rtv.votes-reached"));
                            _endmapVoteManager.StartVote(_config, isRtv: true);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Something went wrong with the rtv command: {message}", ex.Message);
            }
        }

        private bool VoteResultCallback(YesNoVoteInfo info)
        {
            int requiredYesVotes = (int)Math.Ceiling(info.num_clients * (_config.VotePercentage / 100.0));

            if (info.yes_votes >= requiredYesVotes)
            {
                Server.PrintToChatAll($"{_localizer.LocalizeWithPrefix("rtv.votes-reached")}");
                
                if (_voteTypeConfig.EnableScreenMenu)
                {
                    _ = new Timer(3.5F, () =>
                    {
                        _endmapVoteManager.StartVote(_config, isRtv: true);
                    });
                }
                else
                {
                    _endmapVoteManager.StartVote(_config, isRtv: true);
                }
                
                return true;
            }
            else
            {
                Server.ExecuteCommand("sv_allow_votes 0");
                Server.ExecuteCommand("sv_vote_allow_in_warmup 0");
                Server.ExecuteCommand("sv_vote_allow_spectators 0");
                Server.ExecuteCommand("sv_vote_count_spectator_votes 0");
                ActivateCooldown();
                return false;
            }
        }

        private void VoteHandlerCallback(YesNoVoteAction action, int param1, int param2)
        {
            switch (action)
            {
                case YesNoVoteAction.VoteAction_Start:
                    Server.PrintToChatAll($"{_localizer.LocalizeWithPrefix("rtv.rocked-the-vote", _initiatingPlayer!.PlayerName)}");
                    break;

                case YesNoVoteAction.VoteAction_Vote:
                    try
                    {
                        var vc = PanoramaVote.VoteController;
                        if (vc != null)
                        {
                            if (!vc.IsValid)
                                return;

                            int potentialVotes = vc.PotentialVotes;

                            if (vc.VoteOptionCount.Length <= (int)CastVote.VOTE_OPTION2)
                                return;

                            int yesVotes = vc.VoteOptionCount[(int)CastVote.VOTE_OPTION1];
                            int noVotes = vc.VoteOptionCount[(int)CastVote.VOTE_OPTION2];
                            int requiredYesVotes = (int)Math.Ceiling(potentialVotes * (_config.VotePercentage / 100.0));

                            // Early cancel if the vote can no longer pass
                            if ((potentialVotes - noVotes) < requiredYesVotes)
                            {
                                Server.NextFrame(() => {
                                    try {
                                        PanoramaVote.EndVote(YesNoVoteEndReason.VoteEnd_Cancelled, overrideFailCode: 0);
                                        ActivateCooldown();
                                    }
                                    catch (Exception ex) {
                                        _logger.LogError(ex, "Error during vote cancellation: {Message}", ex.Message);
                                    }
                                });
                                return;
                            }

                            // Early pass if enough yes votes are already in
                            if (yesVotes >= requiredYesVotes)
                            {
                                Server.NextFrame(() => {
                                    try {
                                        PanoramaVote.EndVote(YesNoVoteEndReason.VoteEnd_AllVotes);
                                    }
                                    catch (Exception ex) {
                                        _logger.LogError(ex, "Error during early vote pass: {Message}", ex.Message);
                                    }
                                });
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing vote: {Message}", ex.Message);
                    }
                    break;

                case YesNoVoteAction.VoteAction_End:
                    if ((YesNoVoteEndReason)param1 == YesNoVoteEndReason.VoteEnd_Cancelled)
                    {
                        Server.PrintToChatAll($"{_localizer.LocalizeWithPrefix("rtv.failed")}");
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

            _cooldownEndTime = DateTime.UtcNow.AddSeconds(_config.CooldownDuration);
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
            _voteTypeConfig = config.VoteType;
            _voteManager = new AsyncVoteManager(_config);
        }
    }
}