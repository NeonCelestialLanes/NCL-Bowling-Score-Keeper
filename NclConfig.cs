using System;
using Dalamud.Configuration;

namespace FFXIVBowling;

[Serializable]
internal sealed class NclConfig : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public float FullWidth { get; set; } = 860;
    public float FullHeight { get; set; } = 900;
}
