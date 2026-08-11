using System;
using System.Collections.Generic;

namespace CollectionViewer.Data;

/// <summary>
/// Maps a world (server) name to the Lodestone regional subdomain that must be used to search
/// for characters on it (na./eu./jp.finalfantasyxiv.com). The FFXIV API has no endpoint that
/// returns this mapping, so it is derived from the known data-center groupings.
/// Oceanian worlds (Materia) are hosted on the same Lodestone infrastructure as North America,
/// confirmed by direct query - https://na.finalfantasyxiv.com/lodestone/character/?worldname=Bismarck
/// returns results.
/// </summary>
public static class WorldRegions
{
    private static readonly string[] JapanWorlds =
    {
        "Aegis", "Atomos", "Carbuncle", "Garuda", "Gungnir", "Kujata", "Tonberry", "Typhon",
        "Alexander", "Bahamut", "Durandal", "Fenrir", "Ifrit", "Ridill", "Tiamat", "Ultima",
        "Anima", "Asura", "Chocobo", "Hades", "Ixion", "Masamune", "Pandaemonium", "Titan",
        "Belias", "Mandragora", "Ramuh", "Shinryu", "Unicorn", "Valefor", "Yojimbo", "Zeromus",
    };

    private static readonly string[] EuropeWorlds =
    {
        "Cerberus", "Louisoix", "Moogle", "Omega", "Phantom", "Ragnarok", "Sagittarius", "Spriggan",
        "Alpha", "Lich", "Odin", "Phoenix", "Raiden", "Shiva", "Twintania", "Zodiark",
    };

    private static readonly HashSet<string> JapanSet = new(JapanWorlds, StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> EuropeSet = new(EuropeWorlds, StringComparer.OrdinalIgnoreCase);

    /// <summary>Returns the Lodestone subdomain ("na", "eu" or "jp") to use when searching for a
    /// character on the given world. Defaults to "na" for unrecognized/NA/Oceania worlds.</summary>
    public static string GetLodestoneSubdomain(string worldName)
    {
        if (JapanSet.Contains(worldName))
            return "jp";
        if (EuropeSet.Contains(worldName))
            return "eu";
        return "na";
    }
}
