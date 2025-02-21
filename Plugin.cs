using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using static CounterStrikeSharp.API.Core.Listeners;
using cs2_rockthevote.Features;
using Microsoft.Extensions.DependencyInjection;

namespace cs2_rockthevote
{
    public class PluginDependencyInjection : IPluginServiceCollection<Plugin>
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
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
        TimeLeftCommand timeLeft,
        NextMapCommand nextMap,
        ExtendRoundTimeCommand extendRoundTime,
        VoteExtendRoundTimeCommand voteExtendRoundTime) : BasePlugin, IPluginConfig<Config>
    {
        public override string ModuleName => "RockTheVote";
        public override string ModuleVersion => "2.0.1";
        public override string ModuleAuthor => "abnerfs (Updated by Marchand)";

        private readonly DependencyManager<Plugin, Config> _dependencyManager = dependencyManager;
        private readonly NominationCommand _nominationManager = nominationManager;
        private readonly ChangeMapManager _changeMapManager = changeMapManager;
        private readonly VotemapCommand _votemapManager = voteMapManager;
        private readonly RockTheVoteCommand _rtvManager = rtvManager;
        private readonly TimeLeftCommand _timeLeft = timeLeft;
        private readonly NextMapCommand _nextMap = nextMap;
        private readonly ExtendRoundTimeCommand _extendRoundTime = extendRoundTime;
        private readonly VoteExtendRoundTimeCommand _voteExtendRoundTime = voteExtendRoundTime;

        public Config Config { get; set; } = new Config();

        public string Localize(string prefix, string key, params object[] values)
        {
            return $"{Localizer[prefix]} {Localizer[key, values]}";
        }
        public override void Load(bool hotReload)
        {
            _dependencyManager.OnPluginLoad(this);
            RegisterListener<OnMapStart>(_dependencyManager.OnMapStart);
        }

        [GameEventHandler(HookMode.Post)]
        public HookResult OnChat(EventPlayerChat @event, GameEventInfo info)
        {
            var player = Utilities.GetPlayerFromUserid(@event.Userid);
            if (player is not null)
            {
                var text = @event.Text.Trim().ToLower();
                if (text == "rtv")
                {
                    _rtvManager.CommandHandler(player);
                }
                else if (text.StartsWith("nominate"))
                {
                    var split = text.Split("nominate");
                    var map = split.Length > 1 ? split[1].Trim() : "";
                    _nominationManager.CommandHandler(player, map);
                }
                else if (text.StartsWith("votemap"))
                {
                    var split = text.Split("votemap");
                    var map = split.Length > 1 ? split[1].Trim() : "";
                    _votemapManager.CommandHandler(player, map);
                }
                else if (text.StartsWith("timeleft"))
                {
                    _timeLeft.CommandHandler(player);
                }
                else if (text.StartsWith("nextmap"))
                {
                    _nextMap.CommandHandler(player);
                }
                else if (text.StartsWith("extendroundtime"))
                {
                    _extendRoundTime.CommandHandler(player);
                }
                else if (text.StartsWith("voteextendroundtime"))
                {
                    _voteExtendRoundTime.CommandHandler(player);
                }
            }
            return HookResult.Continue;
        }

        public void OnConfigParsed(Config config)
        {
            Config = config;

            if (Config.Version < 9)
                Console.WriteLine("[RockTheVote] please delete it from addons/counterstrikesharp/configs/plugins/RockTheVote and let the plugin recreate it on load");

            if (Config.Version < 7)
                throw new Exception("Your config file is too old, please delete it from addons/counterstrikesharp/configs/plugins/RockTheVote and let the plugin recreate it on load");

            _dependencyManager.OnConfigParsed(config);
        }
    }
}
