using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
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
        
        public RockTheVoteCommand(
            GameRules gameRules, 
            EndMapVoteManager endmapVoteManager, 
            StringLocalizer localizer, 
            PluginState pluginState)
        {
            _localizer = localizer;
            _gameRules = gameRules;
            _endmapVoteManager = endmapVoteManager;
            _pluginState = pluginState;
        }
        
        /*public void OnMapStart(string map)
        {
            _voteManager?.OnMapStart(map);
        }*/

        public void CommandHandler(CCSPlayerController? player)
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
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.disabled"));
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
            try
            {
                bool voteStarted = PanoramaVote.SendYesNoVoteToAll(
                    _config.VoteDuration,
                    VoteConstants.VOTE_CALLER_SERVER,
                    "#SFUI_vote_changelevel",
                    _localizer.Localize("rtv.ui.question"),
                    VoteResultCallback,
                    VoteHandlerCallback
                );
                if (voteStarted && PanoramaVote.VoteController != null)
                {
                    Server.NextFrame(() => {
                        if (player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected)
                        {
                            PanoramaVote.VoteController.VotesCast[player.Slot] = (int)CastVote.VOTE_OPTION1; // Auto cast yes vote for initiator
                            PanoramaVote.VoteController.VoteOptionCount[(int)CastVote.VOTE_OPTION1]++;
                            PanoramaVote.UpdateVoteCounts();
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RTV ERROR] Failed to start RTV vote: {ex.Message}");
            }
        }

        private bool VoteResultCallback(YesNoVoteInfo info)
        {
            int requiredYesVotes = (int)Math.Ceiling(info.num_clients * (_config.VotePercentage / 100.0));
            Server.PrintToChatAll($" {ChatColors.Green}[RTV] {ChatColors.White}YES votes: {info.yes_votes}/{requiredYesVotes}, NO votes: {info.no_votes}, Total Voters: {info.num_clients}");

            if (info.yes_votes >= requiredYesVotes)
            {
                Server.PrintToChatAll($" {ChatColors.Green}[RTV] Vote passed!");
                
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
                Server.PrintToChatAll($" {ChatColors.Red}[RTV] Vote failed!");
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
                        
                    player.PrintToChat($" {ChatColors.Lime}[RTV] {player.PlayerName} voted: " +
                                    (param2 == (int)CastVote.VOTE_OPTION1 ? 
                                    $" {ChatColors.Green}Yes" : 
                                    $" {ChatColors.Red}No"));
                    break;
                    
                case YesNoVoteAction.VoteAction_End:
                    if ((YesNoVoteEndReason)param1 == YesNoVoteEndReason.VoteEnd_Cancelled)
                    {
                        Server.PrintToChatAll($" {ChatColors.Red}[RTV] Vote Ended! Cancelled.");
                    }
                    else if ((YesNoVoteEndReason)param1 == YesNoVoteEndReason.VoteEnd_TimeUp)
                    {
                        Server.PrintToChatAll($" {ChatColors.Red}[RTV] Vote Ended! Time is up.");
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