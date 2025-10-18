using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using static CounterStrikeSharp.API.Core.Listeners;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;

namespace cs2_rockthevote
{
    public class PluginDependencyInjection : IPluginServiceCollection<Plugin>
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddLogging();
            var di = new DependencyManager<Plugin, Config>();
            di.LoadDependencies(typeof(Plugin).Assembly);
            di.AddIt(serviceCollection);
            serviceCollection.AddScoped<StringLocalizer>();
        }
    }

    public partial class Plugin(DependencyManager<Plugin, Config> dependencyManager,
        ChangeMapManager changeMapManager,
        RockTheVoteCommand rtvManager,
        TimeLeftCommand timeLeft,
        MaplistCommand maplistManager,
        PluginState pluginState,
        IStringLocalizer stringLocalizer,
        ILogger<Plugin> logger) : BasePlugin, IPluginConfig<Config>
    {
        public override string ModuleName => "RockTheVote";
        public override string ModuleVersion => "2.2.0";
        public override string ModuleAuthor => "abnerfs (Updated by Marchand) (Updated more by zhw1nq)";

        private readonly DependencyManager<Plugin, Config> _dependencyManager = dependencyManager;
        private readonly ChangeMapManager _changeMapManager = changeMapManager;
        private readonly RockTheVoteCommand _rtvManager = rtvManager;
        private readonly TimeLeftCommand _timeLeft = timeLeft;
        private readonly MaplistCommand _maplistManager = maplistManager;
        private StringLocalizer _localizer = new(stringLocalizer, "rtv.prefix");
        private readonly ILogger<Plugin> _logger = logger;
        private readonly PluginState _pluginState = pluginState;


        public Config Config { get; set; } = new Config();

        public string Localize(string prefix, string key, params object[] values)
        {
            return $"{Localizer[prefix]} {Localizer[key, values]}";
        }

        public override void Load(bool hotReload)
        {
            _dependencyManager.OnPluginLoad(this);
            RegisterListener<OnMapStart>(_dependencyManager.OnMapStart);

            RegisterEventHandler<EventVoteCast>((ev, info) =>
            {
                PanoramaVote.VoteCast(ev);
                return HookResult.Continue;
            });
        }

        public override void OnAllPluginsLoaded(bool hotReload)
        {
        }

        public override void Unload(bool hotReload)
        {
            RemoveListener<OnMapStart>(_dependencyManager.OnMapStart);
        }

        public void OnConfigParsed(Config config)
        {
            Config = config;
            _dependencyManager.OnConfigParsed(config);

            if (config.Version < Config.Version)
                Logger.LogWarning($"Configuration version mismatch (Expected: {0} | Current: {1})", Config.Version, config.Version);
        }

        [ConsoleCommand("css_reloadrtv", "Reloads the RTV config.")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        public void ReloadCommand(CCSPlayerController? player, CommandInfo command)
        {
            string permission = "@css/root";

            if (player != null && !AdminManager.PlayerHasPermissions(player, permission))
            {
                command?.ReplyToCommand($"[RTV] {ChatColors.Red}You do not have the correct permission to execute this command.");
                return;
            }

            try
            {
                Config.Reload();
                command.ReplyToCommand($"[RTV] {ChatColors.Lime}Configuration reloaded successfully!");
            }
            catch (Exception ex)
            {
                command.ReplyToCommand($"Failed to reload configuration: {ex.Message}");
            }
        }
    }
}