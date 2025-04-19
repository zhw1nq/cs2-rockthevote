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


    public interface IEndOfMapConfig
    {
        public int MapsToShow { get; set; }
        public bool ChangeMapImmediatly { get; set; }
        public int VoteDuration { get; set; }
        public bool HudMenu { get; set; }
        public bool ScreenMenu { get; set; }
        public bool ChatMenu { get; set; }
        public bool HideHudAfterVote { get; set; }
        public bool SoundEnabled { get; set; }
        public string SoundPath { get; set; }
        public bool IncludeExtendCurrentMap { get; set; }
        public int RoundTimeExtension { get; set; }
        int MaxMapExtensions { get; set; }
    }

    public class EndOfMapConfig : IEndOfMapConfig
    {
        public bool Enabled { get; set; } = true;
        public int MapsToShow { get; set; } = 6;
        public bool HudMenu { get; set; } = false;
        public bool ScreenMenu { get; set; } = true;
        public bool ChatMenu { get; set; } = false;
        public bool ChangeMapImmediatly { get; set; } = false;
        public int VoteDuration { get; set; } = 150;
        public bool HideHudAfterVote { get; set; } = true;
        public bool SoundEnabled { get; set; } = false;
        public string SoundPath { get; set; } = "sounds/vo/announcer/cs2_classic/felix_broken_fang_pick_1_map_tk01.vsnd_c";
        public int TriggerSecondsBeforeEnd { get; set; } = 180;
        public int TriggerRoundsBeforEnd { get; set; } = 0;
        public float DelayToChangeInTheEnd { get; set; } = 0F;
        public bool IncludeExtendCurrentMap { get; set; } = true;
        public int RoundTimeExtension { get; set; } = 15;
        public int MaxMapExtensions { get; set; } = 2;

    }

    public class RtvConfig : ICommandConfig, IVoteConfig, IEndOfMapConfig
    {
        public bool Enabled { get; set; } = true;
        public bool EnabledInWarmup { get; set; } = false;
        public bool NominationEnabled { get; set; } = true;
        public int MinPlayers { get; set; } = 0;
        public int MinRounds { get; set; } = 0;
        public bool ChangeMapImmediatly { get; set; } = true;
        public bool HideHudAfterVote { get; set; } = true;
        public bool SoundEnabled { get; set; } = false;
        public string SoundPath { get; set; } = "sounds/vo/announcer/cs2_classic/felix_broken_fang_pick_1_map_tk01.vsnd_c";
        public int MapsToShow { get; set; } = 6;
        public int VoteDuration { get; set; } = 60;
        public int RtvVoteDuration { get; set; } = 60;
        public int CooldownDuration { get; set; } = 180;
        public int VotePercentage { get; set; } = 51;
        public bool HudMenu { get; set; } = false;
        public bool ScreenMenu { get; set; } = true;
        public bool ChatMenu { get; set; } = false;
        public bool IncludeExtendCurrentMap { get; set; } = false;
        public int RoundTimeExtension { get; set; } = 15;
        public int MaxMapExtensions { get; set; } = 2;
    }

    public class VotemapConfig : ICommandConfig, IVoteConfig
    {
        public bool Enabled { get; set; } = false;
        public int VotePercentage { get; set; } = 60;
        public bool ChangeMapImmediatly { get; set; } = true;
        public bool EnabledInWarmup { get; set; } = false;
        public int MinPlayers { get; set; } = 0;
        public int MinRounds { get; set; } = 0;
        public bool HudMenu { get; set; } = false;
        public bool ScreenMenu { get; set; } = true;
        public bool ChatMenu { get; set; } = false;
        public string Permission { get; set; } = "@css/vip";
    }

    public class TimeleftConfig
    {
        public bool ShowToAll { get; set; } = false;
    }

    public class NextmapConfig
    {
        public bool ShowToAll { get; set; } = false;
    }

    public class VoteExtendConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; } = 60;
        public int VotePercentage { get; set; } = 51;
        public string Permission { get; set; } = "@css/vip";
    }

    public class ScreenMenuConfig
    {
        public bool EnabledResolutionOption { get; set; } = true;
        public bool EnabledExitOption { get; set; } = true;
    }

    public class Config : BasePluginConfig, IBasePluginConfig
    {
        [JsonPropertyName("ConfigVersion")]
        public override int Version { get; set; } = 12;
        public RtvConfig Rtv { get; set; } = new();
        public VotemapConfig Votemap { get; set; } = new();
        public VoteExtendConfig VoteExtend { get; set; } = new();
        public EndOfMapConfig EndOfMapVote { get; set; } = new();
        public ScreenMenuConfig ScreenMenu { get; set; } = new();
        public TimeleftConfig Timeleft { get; set; } = new();
        public NextmapConfig Nextmap { get; set; } = new();
        public ushort MapsInCoolDown { get; set; } = 3;
    }
}
