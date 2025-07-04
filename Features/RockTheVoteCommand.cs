using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Logging;

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
        private readonly ILogger<RockTheVoteCommand> _logger;
        private readonly StringLocalizer _localizer;
        private readonly GameRules _gameRules;
        private readonly EndMapVoteManager _endmapVoteManager;
        private readonly PluginState _pluginState;
        private RtvConfig _config = new();
        private VoteTypeConfig _voteTypeConfig = new();
        private GeneralConfig _generalConfig = new();
        private AsyncVoteManager? _voteManager;
        private bool _isCooldownActive = false;
        private CCSPlayerController? _initiatingPlayer;
        private DateTime _cooldownEndTime;
        private DateTime _rtvEndTime;
        private Plugin? _plugin;
        public int TimeLeft => (int)Math.Max(0, (_rtvEndTime - DateTime.UtcNow).TotalSeconds);


        public RockTheVoteCommand(GameRules gameRules, EndMapVoteManager endmapVoteManager, StringLocalizer localizer, PluginState pluginState, ILogger<RockTheVoteCommand> logger)
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
                bool otherVoteInProgress = _pluginState.MapChangeScheduled || _pluginState.EofVoteHappening || _pluginState.ExtendTimeVoteHappening;

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
                if (!_config.Enabled || otherVoteInProgress)
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
                if (_config.EnablePanorama)
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
                if (!_config.EnablePanorama)
                {
                    _pluginState.RtvVoteHappening = true;
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
                            _pluginState.RtvVoteHappening = false;
                            player.PrintToChat(_localizer.LocalizeWithPrefix("rtv.disabled"));
                            break;
                        case VoteResultEnum.VotesReached:
                            _pluginState.RtvVoteHappening = false;
                            _endmapVoteManager.StartVote(isRtv: true);
                            Server.PrintToChatAll($"{_localizer.LocalizeWithPrefix("rtv.rocked-the-vote", player.PlayerName)} {_localizer.Localize("general.votes-needed", result.VoteCount, result.RequiredVotes)}");
                            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("rtv.votes-reached"));
                            break;
                    }
                }
                _rtvEndTime = DateTime.UtcNow.AddSeconds(_config.RtvVoteDuration);

                _plugin?.AddTimer(0.1f, () =>
                    {
                        if (_config.EnableCountdown && _config.CountdownType == "chat")
                            ChatCountdown(_config.RtvVoteDuration);
                    }, TimerFlags.STOP_ON_MAPCHANGE
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Something went wrong with the rtv command: {ex.Message}");
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
                    _plugin?.AddTimer(3.5f, () =>
                        {
                            try
                            {
                                _endmapVoteManager.StartVote(isRtv: true);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning($"Something went wrong during the VoteResultCallback: {ex.Message}");
                            }
                        }, TimerFlags.STOP_ON_MAPCHANGE
                    );
                }
                else
                {
                    _endmapVoteManager.StartVote(isRtv: true);
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
                    _rtvEndTime = DateTime.UtcNow.AddSeconds(_config.RtvVoteDuration);
                    _pluginState.RtvVoteHappening = true;
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
                                Server.NextFrame(() =>
                                {
                                    try
                                    {
                                        PanoramaVote.EndVote(YesNoVoteEndReason.VoteEnd_Cancelled, overrideFailCode: 0);
                                        ActivateCooldown();
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError($"Error during vote cancellation: {ex.Message}");
                                    }
                                });
                                return;
                            }

                            // Early pass if enough yes votes are already in
                            if (yesVotes >= requiredYesVotes)
                            {
                                Server.NextFrame(() =>
                                {
                                    try
                                    {
                                        PanoramaVote.EndVote(YesNoVoteEndReason.VoteEnd_AllVotes);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError($"Error during early vote pass: {ex.Message}");
                                    }
                                });
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error processing vote: {ex.Message}");
                    }
                    break;

                case YesNoVoteAction.VoteAction_End:
                    _pluginState.RtvVoteHappening = false;
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

        public void ChatCountdown(int secondsLeft)
        {
            if (!_pluginState.RtvVoteHappening)
                return;

            string text = _localizer.LocalizeWithPrefix("general.chat-countdown", secondsLeft);
            foreach (var player in ServerManager.ValidPlayers())
                player.PrintToChat(text);

            int nextSecondsLeft = secondsLeft - _config.ChatCountdownInterval;
            if (nextSecondsLeft <= 0)
                return;

            _plugin?.AddTimer(
                _config.ChatCountdownInterval, () =>
                {
                    try
                    {
                        ChatCountdown(nextSecondsLeft);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"ChatCountdown timer callback failed: {ex.Message}");
                    }
                }, TimerFlags.STOP_ON_MAPCHANGE
            );
        }

        private void ActivateCooldown()
        {
            _isCooldownActive = true;

            _plugin?.AddTimer(
                _config.CooldownDuration, () =>
                {
                    try
                    {
                        _isCooldownActive = false;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Cooldown timer callback failed: {ex.Message}");
                    }
                }, TimerFlags.STOP_ON_MAPCHANGE
            );

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
            _generalConfig = config.General;
            _voteManager = new AsyncVoteManager(_config);
        }
        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
        }
    }
}