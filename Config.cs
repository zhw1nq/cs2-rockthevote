using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace cs2_rockthevote
{
    public class RtvConfig
    {
        public bool Enabled { get; set; } = true;
        public bool EnabledInWarmup { get; set; } = false;
        public bool EnablePanorama { get; set; } = true;
        public int MinPlayers { get; set; } = 0;
        public int MinRounds { get; set; } = 0;
        public bool ChangeAtRoundEnd { get; set; } = false;
        public int MapChangeDelay { get; set; } = 5;
        public bool SoundEnabled { get; set; } = false;
        public string SoundPath { get; set; } = "sounds/vo/announcer/cs2_classic/felix_broken_fang_pick_1_map_tk01.vsnd_c";
        public int MapsToShow { get; set; } = 6;
        public int RtvVoteDuration { get; set; } = 60;
        public int MapVoteDuration { get; set; } = 60;
        public int CooldownDuration { get; set; } = 180;
        public int MapStartDelay { get; set; } = 180;
        public int VotePercentage { get; set; } = 51;
    }

    public class EndOfMapConfig
    {
        public bool Enabled { get; set; } = true;
        public int MapsToShow { get; set; } = 6;
        public bool ChangeMapImmediately { get; set; } = false;
        public int VoteDuration { get; set; } = 150;
        public bool SoundEnabled { get; set; } = false;
        public string SoundPath { get; set; } = "sounds/vo/announcer/cs2_classic/felix_broken_fang_pick_1_map_tk01.vsnd_c";
        public int TriggerSecondsBeforeEnd { get; set; } = 180;
        public int TriggerRoundsBeforeEnd { get; set; } = 0;
        public float DelayToChangeInTheEnd { get; set; } = 0F;
    }
    public class MapChooserConfig
    {
        public string Command { get; set; } = "mm";
        public string Permission { get; set; } = "@scolor/staff";
    }

    public class GeneralConfig
    {
        public int MapsInCoolDown { get; set; } = 3;
        public bool HideHudAfterVote { get; set; } = true;
        public bool RandomStartMap { get; set; } = false;
    }

    public class Config : BasePluginConfig, IBasePluginConfig
    {
        [JsonPropertyName("ConfigVersion")]
        public override int Version { get; set; } = 19;
        public RtvConfig Rtv { get; set; } = new();
        public EndOfMapConfig EndOfMapVote { get; set; } = new();
        public MapChooserConfig MapChooser { get; set; } = new();
        public GeneralConfig General { get; set; } = new();
    }
}