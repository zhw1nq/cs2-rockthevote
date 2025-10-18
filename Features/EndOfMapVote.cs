using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Timers;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using Microsoft.Extensions.Logging;

namespace cs2_rockthevote
{
    public class EndOfMapVote(
        TimeLimitManager timeLimit,
        MaxRoundsManager maxRounds,
        PluginState pluginState,
        GameRules gameRules,
        EndMapVoteManager voteManager,
        ILogger<EndOfMapVote> logger) : IPluginDependency<Plugin, Config>
    {
        private readonly ILogger<EndOfMapVote> _logger = logger;
        private readonly TimeLimitManager _timeLimit = timeLimit;
        private readonly MaxRoundsManager _maxRounds = maxRounds;
        private readonly PluginState _pluginState = pluginState;
        private readonly GameRules _gameRules = gameRules;
        private readonly EndMapVoteManager _voteManager = voteManager;

        private EndOfMapConfig _config = new();
        private Timer? _timer;
        private bool _hasInitializedTimer;

        // Cache ConVar lookups
        private ConVar? _gameType;
        private ConVar? _gameMode;
        private int? _cachedGameType;
        private int? _cachedGameMode;

        private bool DeathMatch =>
            (_cachedGameMode ?? (_cachedGameMode = _gameMode?.GetPrimitiveValue<int>())) == 2 &&
            (_cachedGameType ?? (_cachedGameType = _gameType?.GetPrimitiveValue<int>())) == 1;

        private bool ShouldSkipVote =>
            _pluginState.DisableCommands ||
            (_gameRules?.WarmupRunning ?? false) ||
            !_config.Enabled;

        private bool CheckMaxRounds()
        {
            if (_maxRounds.UnlimitedRounds)
                return false;

            if (_maxRounds.RemainingRounds <= _config.TriggerRoundsBeforeEnd)
                return true;

            return _maxRounds.CanClinch &&
                   _maxRounds.RemainingWins <= _config.TriggerRoundsBeforeEnd;
        }

        private bool CheckTimeLeft()
        {
            return !_timeLimit.UnlimitedTime &&
                   _timeLimit.TimeRemaining <= _config.TriggerSecondsBeforeEnd;
        }

        public void StartVote()
        {
            KillTimer();
            _voteManager.StartVote(isRtv: false);
        }

        public void OnMapStart(string map)
        {
            KillTimer();
            // Reset cached values on map change
            _cachedGameType = null;
            _cachedGameMode = null;
            _hasInitializedTimer = false;
        }

        private void KillTimer()
        {
            if (_timer != null)
            {
                _timer.Kill();
                _timer = null;
            }
        }

        private void MaybeStartTimer(Plugin plugin)
        {
            KillTimer();

            if (_timeLimit.UnlimitedTime || !_config.Enabled)
                return;

            _timer = plugin.AddTimer(1.0f, () =>
            {
                if (_gameRules == null || ShouldSkipVote || _timeLimit.TimeRemaining <= 0)
                    return;

                if (CheckTimeLeft())
                    StartVote();
            }, TimerFlags.REPEAT);
        }

        public void OnLoad(Plugin plugin)
        {
            // Cache ConVar references
            _gameMode = ConVar.Find("game_mode");
            _gameType = ConVar.Find("game_type");

            plugin.RegisterEventHandler<EventRoundStart>((ev, info) =>
            {
                if (ShouldSkipVote)
                {
                    if (DeathMatch)
                        MaybeStartTimer(plugin);
                    return HookResult.Continue;
                }

                if (CheckMaxRounds())
                    StartVote();
                else if (DeathMatch)
                    MaybeStartTimer(plugin);

                return HookResult.Continue;
            });

            plugin.RegisterEventHandler<EventRoundAnnounceMatchStart>((ev, info) =>
            {
                if (_hasInitializedTimer)
                    MaybeStartTimer(plugin);
                else
                    _hasInitializedTimer = true;

                return HookResult.Continue;
            });
        }

        public void OnConfigParsed(Config config)
        {
            _config = config.EndOfMapVote;
        }
    }
}