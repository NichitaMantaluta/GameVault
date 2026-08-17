using System.Text.RegularExpressions;

namespace GameStore.Tests.Persistence;

public class SeedDevDataTests
{
    private static readonly Dictionary<string, string?> ExpectedImageUrls = new()
    {
        ["Celeste"] = "https://upload.wikimedia.org/wikipedia/commons/0/0f/Celeste_box_art_full.png",
        ["Hades"] = "https://upload.wikimedia.org/wikipedia/en/c/cc/Hades_cover_art.jpg",
        ["Stardew Valley"] = "https://upload.wikimedia.org/wikipedia/en/f/fd/Logo_of_Stardew_Valley.png",
        ["The Witcher 3"] = "https://upload.wikimedia.org/wikipedia/en/0/0c/Witcher_3_cover_art.jpg",
        ["Portal 2"] = "https://upload.wikimedia.org/wikipedia/en/f/f9/Portal2cover.jpg",
        ["Hollow Knight"] = "https://upload.wikimedia.org/wikipedia/en/d/de/Hollow_Knight_2026_cover_art.jpg",
        ["Civilization VI"] = "https://upload.wikimedia.org/wikipedia/en/3/3b/Civilization_VI_cover_art.jpg",
        ["Forza Horizon 5"] = "https://upload.wikimedia.org/wikipedia/en/8/86/Forza_Horizon_5_cover_art.jpg",
        ["Resident Evil 4 Remake"] = "https://upload.wikimedia.org/wikipedia/en/d/df/Resident_Evil_4_remake_cover_art.jpg",
        ["Street Fighter 6"] = "https://upload.wikimedia.org/wikipedia/en/9/94/Street_Fighter_6_box_art.jpg",
        ["Doom Eternal"] = "https://upload.wikimedia.org/wikipedia/en/9/9d/Cover_Art_of_Doom_Eternal.png",
        ["Uncharted 4"] = "https://upload.wikimedia.org/wikipedia/en/1/1a/Uncharted_4_box_artwork.jpg",
        ["FIFA 23"] = "https://upload.wikimedia.org/wikipedia/en/a/a6/FIFA_23_Cover.jpg",
        ["Disco Elysium"] = "https://upload.wikimedia.org/wikipedia/en/0/0d/Disco_Elysium_Poster.jpeg",
        ["Slay the Spire"] = "https://upload.wikimedia.org/wikipedia/en/b/b7/Slay_the_spire_cover.jpg",
        ["Animal Crossing: New Horizons"] = "https://upload.wikimedia.org/wikipedia/en/1/1f/Animal_Crossing_New_Horizons.jpg",
        ["Sekiro: Shadows Die Twice"] = "https://upload.wikimedia.org/wikipedia/en/6/6e/Sekiro_art.jpg",
        ["Ori and the Will of the Wisps"] = "https://upload.wikimedia.org/wikipedia/en/9/94/Ori_and_the_Will_of_the_Wisps.jpg",
        ["Baba Is You"] = "https://upload.wikimedia.org/wikipedia/en/2/28/Baba_is_you_cover_art.jpg",
        ["Gran Turismo 7"] = "https://upload.wikimedia.org/wikipedia/en/1/14/Gran_Turismo_7_cover_art.jpg",
        ["Dead Space Remake"] = "https://upload.wikimedia.org/wikipedia/en/3/36/Dead_Space_2022_Teaser_Art.jpg",
        ["Tekken 8"] = "https://upload.wikimedia.org/wikipedia/en/b/b4/Tekken_8_cover_art.jpg",
        ["Titanfall 2"] = "https://upload.wikimedia.org/wikipedia/en/1/13/Titanfall_2.jpg",
        ["Firewatch"] = "https://upload.wikimedia.org/wikipedia/en/a/a5/Firewatch_cover.jpg",
        ["NBA 2K24"] = "https://upload.wikimedia.org/wikipedia/en/4/48/NBA_2K24_cover_art.jpg",
        ["Baldur's Gate 3"] = "https://upload.wikimedia.org/wikipedia/en/1/12/Baldur%27s_Gate_3_cover_art.jpg",
        ["XCOM 2"] = "https://upload.wikimedia.org/wikipedia/en/c/c3/XCOM_2_cover_art.jpg",
        ["Microsoft Flight Simulator"] = "https://upload.wikimedia.org/wikipedia/en/8/84/Microsoft_Flight_Simulator_2020_cover_art.png",
        ["Devil May Cry 5"] = "https://upload.wikimedia.org/wikipedia/en/c/cb/Devil_May_Cry_5.jpg",
        ["Super Mario Odyssey"] = "https://upload.wikimedia.org/wikipedia/en/8/8d/Super_Mario_Odyssey.jpg",
        ["The Witness"] = "https://upload.wikimedia.org/wikipedia/en/f/f4/The_Witness_cover.jpg",
        ["Need for Speed Unbound"] = "https://upload.wikimedia.org/wikipedia/en/d/db/Need_for_Speed_Unbound.png",
        ["Alien: Isolation"] = "https://upload.wikimedia.org/wikipedia/en/6/6e/Alien_Isolation.jpg",
        ["Mortal Kombat 1"] = "https://upload.wikimedia.org/wikipedia/en/5/5b/Mortal_Kombat_1_key_art.jpeg",
        ["Halo Infinite"] = "https://upload.wikimedia.org/wikipedia/en/1/14/Halo_Infinite.png",
        ["Journey"] = "https://upload.wikimedia.org/wikipedia/en/6/64/Journey_Title_Poster.png",
        ["Rocket League"] = "https://upload.wikimedia.org/wikipedia/commons/e/e0/Rocket_League_coverart.jpg",
        ["Persona 5 Royal"] = null,
        ["Age of Empires IV"] = "https://upload.wikimedia.org/wikipedia/en/0/08/Age_of_Empires_IV_Cover_Art.png",
        ["The Sims 4"] = "https://upload.wikimedia.org/wikipedia/en/7/7f/Sims4_Rebrand.png",
        ["Bayonetta 3"] = "https://upload.wikimedia.org/wikipedia/en/f/fe/Bayonetta_3_cover.webp",
        ["Rayman Legends"] = "https://upload.wikimedia.org/wikipedia/en/f/f6/Rayman_Legends_Box_Art.jpg",
        ["Tetris Effect"] = "https://upload.wikimedia.org/wikipedia/en/a/ae/Tetris_Effect_cover.jpg",
        ["F1 24"] = "https://upload.wikimedia.org/wikipedia/en/5/55/F1_24_cover_art.jpg",
        ["Outlast 2"] = "https://upload.wikimedia.org/wikipedia/en/1/1b/Outlast2.png",
        ["Guilty Gear Strive"] = "https://upload.wikimedia.org/wikipedia/en/7/7d/Guilty_Gear_Strive.jpg",
        ["Overwatch 2"] = "https://upload.wikimedia.org/wikipedia/en/8/89/Overwatch_2_Steam_artwork.jpg",
        ["Outer Wilds"] = "https://upload.wikimedia.org/wikipedia/en/f/f6/Outer_Wilds_Steam_artwork.jpg",
        ["Tony Hawk's Pro Skater 1+2"] = "https://upload.wikimedia.org/wikipedia/en/8/8f/Tony_Hawk_Pro_Skater_Remaster_cover_art.png",
        ["Elden Ring"] = "https://upload.wikimedia.org/wikipedia/en/b/b9/Elden_Ring_Box_art.jpg"
    };

    [Fact]
    public void SeedDevData_AssignsExpectedWikipediaCoverUrls()
    {
        var seedPath = Path.Combine(AppContext.BaseDirectory, "seed-dev-data.sql");
        Assert.True(File.Exists(seedPath), $"Seed file was not copied to the test output: {seedPath}");

        var seededImageUrls = ParseSeededImageUrls(File.ReadAllText(seedPath));

        Assert.Equal(ExpectedImageUrls.Count, seededImageUrls.Count);
        Assert.Equal(ExpectedImageUrls.Keys.Order(), seededImageUrls.Keys.Order());

        foreach (var (name, expectedUrl) in ExpectedImageUrls)
        {
            Assert.True(seededImageUrls.ContainsKey(name), $"Missing seeded game '{name}'.");
            Assert.Equal(expectedUrl, seededImageUrls[name]);
        }

        Assert.Null(seededImageUrls["Persona 5 Royal"]);
    }

    private static Dictionary<string, string?> ParseSeededImageUrls(string sql)
    {
        var matches = Regex.Matches(
            sql,
            @"\(gen_random_uuid\(\), '(?<name>(?:[^']|'')*)', '(?:[^']|'')*', [^,]+, \d+, NOW\(\), NOW\(\), TRUE, (?<image>NULL|'[^']+')\)");

        var result = new Dictionary<string, string?>();
        foreach (Match match in matches)
        {
            var name = match.Groups["name"].Value.Replace("''", "'", StringComparison.Ordinal);
            var image = match.Groups["image"].Value;
            result[name] = image == "NULL" ? null : image.Trim('\'');
        }

        return result;
    }
}
