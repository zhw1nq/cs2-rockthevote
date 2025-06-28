using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Timers;
using cs2_rockthevote.Core;
using Microsoft.Extensions.Logging;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace cs2_rockthevote
{
    public class EndOfMapVote(TimeLimitManager timeLimit, MaxRoundsManager maxRounds, PluginState pluginState, GameRules gameRules, EndMapVoteManager voteManager, ILogger<EndOfMapVote> logger) : IPluginDependency<Plugin, Config>
    {
        private readonly ILogger<EndOfMapVote> _logger = logger;
        private TimeLimitManager _timeLimit = timeLimit;
        private MaxRoundsManager _maxRounds = maxRounds;
        private PluginState _pluginState = pluginState;
        private GameRules _gameRules = gameRules;
        private EndMapVoteManager _voteManager = voteManager;
        private EndOfMapConfig _config = new();
        private VoteTypeConfig _voteTypeConfig = new();
        private Timer? _timer;
        private bool DeathMatch => _gameMode?.GetPrimitiveValue<int>() == 2 && _gameType?.GetPrimitiveValue<int>() == 1;
        private ConVar? _gameType;
        private ConVar? _gameMode;
        private Plugin? _plugin;
        private bool _hasInitializedTimer = false;

        bool CheckMaxRounds()
        {
            //Server.PrintToChatAll($"Remaining rounds {_maxRounds.RemainingRounds}, remaining wins: {_maxRounds.RemainingWins}, triggerBefore {_config.TriggerRoundsBeforEnd}");
            if (_maxRounds.UnlimitedRounds)
                return false;

            if (_maxRounds.RemainingRounds <= _config.TriggerRoundsBeforEnd)
                return true;

            return _maxRounds.CanClinch && _maxRounds.RemainingWins <= _config.TriggerRoundsBeforEnd;
        }


        bool CheckTimeLeft()
        {
            return !_timeLimit.UnlimitedTime && _timeLimit.TimeRemaining <= _config.TriggerSecondsBeforeEnd;
        }

        public void StartVote()
        {
            KillTimer();

            if (_config.Enabled)
            {
                if (_voteTypeConfig.EnableScreenMenu && PanoramaVote.IsVoteInProgress())
                {
                    PanoramaVote.EndVote(YesNoVoteEndReason.VoteEnd_Cancelled, overrideFailCode: 0);
                    _plugin?.AddTimer(
                        3.5f, () =>
                        {
                            try
                            {
                                _voteManager.StartVote(_config);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Vote start timer callback failed");
                            }
                        }, TimerFlags.STOP_ON_MAPCHANGE
                    );
                }
                else
                {
                    _voteManager.StartVote(_config);
                }
            }
        }

        public void OnMapStart(string map)
        {
            KillTimer();
        }

        void KillTimer()
        {
            _timer?.Kill();
            _timer = null;
        }

        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
            _gameMode = ConVar.Find("game_mode");
            _gameType = ConVar.Find("game_type");

            void MaybeStartTimer()
            {
                KillTimer();
                if (!_timeLimit.UnlimitedTime && _config.Enabled)
                {
                    _timer = plugin.AddTimer(1.0F, () =>
                    {
                        if (_gameRules is not null && !_gameRules.WarmupRunning && !_pluginState.DisableCommands && _timeLimit.TimeRemaining > 0)
                        {
                            if (CheckTimeLeft())
                                StartVote();
                        }
                    }, TimerFlags.REPEAT);
                }
            }

            plugin.RegisterEventHandler<EventRoundStart>((ev, info) =>
            {

                if (!_pluginState.DisableCommands && !_gameRules.WarmupRunning && CheckMaxRounds() && _config.Enabled)
                    StartVote();
                else if (DeathMatch)
                {
                    MaybeStartTimer();
                }

                return HookResult.Continue;
            });


            plugin.RegisterEventHandler<EventRoundAnnounceMatchStart>((ev, info) =>
            {
                if (_hasInitializedTimer)
                {
                    MaybeStartTimer();
                }
                else
                {
                    _hasInitializedTimer = true;
                }
                return HookResult.Continue;
            });
        }

        public void OnConfigParsed(Config config)
        {
            _config = config.EndOfMapVote;
        }
    }
}
