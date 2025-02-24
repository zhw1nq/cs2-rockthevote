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
        public bool HudMenu { get; set; } = true;
        public bool ChangeMapImmediatly { get; set; } = false;
        public int VoteDuration { get; set; } = 30;
        public bool HideHudAfterVote { get; set; } = false;
        public bool SoundEnabled { get; set; } = true;
        public string SoundPath { get; set; } = "sounds/vo/announcer/cs2_classic/felix_broken_fang_pick_1_map_tk01.vsnd_c";
        public int TriggerSecondsBeforeEnd { get; set; } = 120;
        public int TriggerRoundsBeforEnd { get; set; } = 2;
        public float DelayToChangeInTheEnd { get; set; } = 6F;
        public bool IncludeExtendCurrentMap { get; set; } = true;
        public int RoundTimeExtension { get; set; } = 15;
        public int MaxMapExtensions { get; set; } = 2;

    }

    public class RtvConfig : ICommandConfig, IVoteConfig, IEndOfMapConfig
    {
        public bool Enabled { get; set; } = true;
        public bool EnabledInWarmup { get; set; } = true;
        public bool NominationEnabled { get; set; } = true;
        public int MinPlayers { get; set; } = 0;
        public int MinRounds { get; set; } = 0;
        public bool ChangeMapImmediatly { get; set; } = true;
        public bool HideHudAfterVote { get; set; } = false;
        public bool SoundEnabled { get; set; } = true;
        public string SoundPath { get; set; } = "sounds/vo/announcer/cs2_classic/felix_broken_fang_pick_1_map_tk01.vsnd_c";
        public int MapsToShow { get; set; } = 6;
        public int VoteDuration { get; set; } = 30;
        public int VotePercentage { get; set; } = 60;
        public bool HudMenu { get; set; } = true;
        public bool IncludeExtendCurrentMap { get; set; } = true;
        public int RoundTimeExtension { get; set; } = 15;
        public int MaxMapExtensions { get; set; } = 2;
    }

    public class VotemapConfig : ICommandConfig, IVoteConfig
    {
        public bool Enabled { get; set; } = true;
        public int VotePercentage { get; set; } = 60;
        public bool ChangeMapImmediatly { get; set; } = true;
        public bool EnabledInWarmup { get; set; } = true;
        public int MinPlayers { get; set; } = 0;
        public int MinRounds { get; set; } = 0;
        public bool HudMenu { get; set; } = false;
    }

    public class TimeleftConfig
    {
        public bool ShowToAll { get; set; } = false;
    }

    public class NextmapConfig
    {
        public bool ShowToAll { get; set; } = false;
    }


    public class Config : IBasePluginConfig
    {
        public int Version { get; set; } = 10;
        public RtvConfig Rtv { get; set; } = new();
        public VotemapConfig Votemap { get; set; } = new();
        public EndOfMapConfig EndOfMapVote { get; set; } = new();
        public TimeleftConfig Timeleft { get; set; } = new();
        public NextmapConfig Nextmap { get; set; } = new();
        public ushort MapsInCoolDown { get; set; } = 3;
    }
}
