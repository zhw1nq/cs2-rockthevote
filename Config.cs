using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace cs2_rockthevote
{
    public interface ICommandConfig
    {
        public bool EnabledInWarmup { get; set; }
        public int MinPlayers { get; set; }
        public int MinRounds { get; set; }
    }

    public interface IVoteConfig
    {
        public int VotePercentage { get; set; }
        public bool ChangeMapImmediatly { get; set; }
    }

    public class RtvConfig : ICommandConfig, IVoteConfig
    {
        public bool Enabled { get; set; } = true;
        public bool EnabledInWarmup { get; set; } = false;
        public bool EnablePanorama { get; set; } = true;
        public int MinPlayers { get; set; } = 0;
        public int MinRounds { get; set; } = 0;
        public bool ChangeMapImmediatly { get; set; } = true;
        public bool SoundEnabled { get; set; } = false;
        public string SoundPath { get; set; } = "sounds/vo/announcer/cs2_classic/felix_broken_fang_pick_1_map_tk01.vsnd_c";
        public int MapsToShow { get; set; } = 6;
        public int RtvVoteDuration { get; set; } = 60;
        public int MapVoteDuration { get; set; } = 60;
        public int CooldownDuration { get; set; } = 180;
        public int MapStartDelay { get; set; } = 180;
        public int VotePercentage { get; set; } = 51;
        public bool EnableCountdown { get; set; } = true;
        public string CountdownType { get; set; } = "chat";
        public int ChatCountdownInterval { get; set; } = 15;
    }

    public class EndOfMapConfig
    {
        public bool Enabled { get; set; } = true;
        public int MapsToShow { get; set; } = 6;
        public string MenuType { get; set; } = "ScreenMenu";
        public bool ChangeMapImmediatly { get; set; } = false;
        public int VoteDuration { get; set; } = 150;
        public bool SoundEnabled { get; set; } = false;
        public string SoundPath { get; set; } = "sounds/vo/announcer/cs2_classic/felix_broken_fang_pick_1_map_tk01.vsnd_c";
        public int TriggerSecondsBeforeEnd { get; set; } = 180;
        public int TriggerRoundsBeforeEnd { get; set; } = 0;
        public float DelayToChangeInTheEnd { get; set; } = 0F;
        public bool IncludeExtendCurrentMap { get; set; } = true;
        public bool EnableCountdown { get; set; } = false;
        public string CountdownType { get; set; } = "chat";
        public int ChatCountdownInterval { get; set; } = 30;
    }

    public class VotemapConfig : ICommandConfig, IVoteConfig
    {
        public bool Enabled { get; set; } = false;
        public int VotePercentage { get; set; } = 50;
        public bool ChangeMapImmediatly { get; set; } = true;
        public bool EnabledInWarmup { get; set; } = false;
        public int MinPlayers { get; set; } = 0;
        public int MinRounds { get; set; } = 0;
        public string Permission { get; set; } = "@css/vip";
    }

    public class VoteExtendConfig
    {
        public bool Enabled { get; set; } = false;
        public bool EnablePanorama { get; set; } = true;
        public int VoteDuration { get; set; } = 60;
        public int VotePercentage { get; set; } = 50;
        public int CooldownDuration { get; set; } = 180;
        public bool EnableCountdown { get; set; } = true;
        public string CountdownType { get; set; } = "chat";
        public int ChatCountdownInterval { get; set; } = 15;
        public string Permission { get; set; } = "@css/vip";
    }

    public class NominateConfig
    {
        public bool Enabled { get; set; } = true;
        public bool EnabledInWarmup { get; set; } = true;
        public string MenuType { get; set; } = "ScreenMenu";
        public int NominateLimit { get; set; } = 1;
        public string Permission { get; set; } = "";
    }

    public class ScreenMenuConfig
    {
        public bool EnableResolutionOption { get; set; } = false;
        public bool EnableExitOption { get; set; } = false;
        public bool FreezePlayer { get; set; } = false;
        public string ScrollUpKey { get; set; } = "Attack";
        public string ScrollDownKey { get; set; } = "Attack2";
        public string SelectKey { get; set; } = "E";
    }

    public class GeneralConfig
    {
        public int MaxMapExtensions { get; set; } = 2;
        public int RoundTimeExtension { get; set; } = 15;
        public int MapsInCoolDown { get; set; } = 3;
        public bool HideHudAfterVote { get; set; } = true;
        public bool RandomStartMap { get; set; } = false;
        public string DiscordWebhook { get; set; } = "";
    }

    public class Config : BasePluginConfig, IBasePluginConfig
    {
        [JsonPropertyName("ConfigVersion")]
        public override int Version { get; set; } = 15;
        public RtvConfig Rtv { get; set; } = new();
        public EndOfMapConfig EndOfMapVote { get; set; } = new();
        public NominateConfig Nominate { get; set; } = new();
        public VotemapConfig Votemap { get; set; } = new();
        public VoteExtendConfig VoteExtend { get; set; } = new();
        public ScreenMenuConfig ScreenMenu { get; set; } = new();
        public GeneralConfig General { get; set; } = new();
    }
}
