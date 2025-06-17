using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using static CounterStrikeSharp.API.Core.Listeners;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Extensions;
using cs2_rockthevote.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        NominationCommand nominationManager,
        ChangeMapManager changeMapManager,
        VotemapCommand voteMapManager,
        RockTheVoteCommand rtvManager,
        NextMapCommand nextMap,
        ExtendRoundTimeCommand extendRoundTime,
        VoteExtendRoundTimeCommand voteExtendRoundTime,
        ILogger<Plugin> logger) : BasePlugin, IPluginConfig<Config>
    {
        public override string ModuleName => "RockTheVote";
        public override string ModuleVersion => "2.0.4";
        public override string ModuleAuthor => "abnerfs (Updated by Marchand)";

        private readonly DependencyManager<Plugin, Config> _dependencyManager = dependencyManager;
        private readonly NominationCommand _nominationManager = nominationManager;
        private readonly ChangeMapManager _changeMapManager = changeMapManager;
        private readonly VotemapCommand _votemapManager = voteMapManager;
        private readonly RockTheVoteCommand _rtvManager = rtvManager;
        private readonly NextMapCommand _nextMap = nextMap;
        private readonly ExtendRoundTimeCommand _extendRoundTime = extendRoundTime;
        private readonly VoteExtendRoundTimeCommand _voteExtendRoundTime = voteExtendRoundTime;
        private readonly ILogger<Plugin> _logger = logger;


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

        public void OnConfigParsed(Config config)
        {
            Config = config;
            _dependencyManager.OnConfigParsed(config);

            if (Config.Version < 13)
            {
                _logger.LogError("Your config file is too old, please delete it from addons/counterstrikesharp/configs/plugins/RockTheVote and let the plugin recreate it on load.");
            }
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