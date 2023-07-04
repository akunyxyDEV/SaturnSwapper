﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
using Saturn.Backend.Data.Compression;
using Saturn.Backend.Data.SaturnAPI.Models;
using Saturn.Backend.Data.Variables;

namespace Saturn.Backend.Data.Swapper.Generation;

public class GliderGenerator : Generator
{
    private CompressionBase compressor = new Oodle();
    private async Task<DisplayItemModel> GetDisplayCharacterInfo(DefaultFileProvider provider, string path)
    {
        DisplayItemModel item = new DisplayItemModel();

        var uasset = await provider.TryLoadObjectAsync(path);
        if (uasset == null)
        {
            Logger.Log($"Failed to load uasset with path \"{path}\"", LogLevel.Warning);
            return null;
        }

        item.ID = Path.GetFileNameWithoutExtension(path);
        item.Name = uasset.TryGetValue(out FText DisplayName, "DisplayName") ? DisplayName.Text : "TBD";
        item.Description = uasset.TryGetValue(out FText Description, "Description")
            ? Description.Text
            : "To be determined...";

        return item;
    }

    private async Task<SaturnItemModel> GetCharacterInfo(DefaultFileProvider provider, string path)
    {
        SaturnItemModel item = new SaturnItemModel();

        var uasset = await provider.TryLoadObjectAsync(path);
        if (uasset == null)
        {
            Logger.Log($"Failed to load uasset with path \"{path}\"", LogLevel.Warning);
            return null;
        }

        item.ID = Path.GetFileNameWithoutExtension(path);
        item.Name = uasset.TryGetValue(out FText DisplayName, "DisplayName") ? DisplayName.Text : "TBD";
        item.Description = uasset.TryGetValue(out FText Description, "Description")
            ? Description.Text
            : "To be determined...";

        Dictionary<string, string> Enums = new();
        Enums.Add("Series", uasset.TryGetValue(out UObject Series, "Series") ? Series.GetPathName() : "None");
        Enums.Add("SkeletalMesh", uasset.TryGetValue(out FSoftObjectPath SkeletalMesh, "SkeletalMesh") ? SkeletalMesh.AssetPathName.Text : "None");

        item.CharacterParts = new Dictionary<string, CharacterPart>()
        {
            {
                "Glider", new CharacterPart()
                {
                    Path = uasset.GetPathName(),
                    Enums = Enums
                }
            }
        };

        return item;

    }
    
    public async Task<List<DisplayItemModel>> Generate()
    {
        List<DisplayItemModel> items = new();

        foreach (var file in Constants.Provider.Files.Keys)
        {
            if (!file.Contains("fortnitegame/content/athena/items/cosmetics/gliders/") && !file.Contains("fortnitegame/plugins/gamefeatures/brcosmetics/content/athena/items/cosmetics/gliders/")) continue;

            var item = await GetDisplayCharacterInfo(Constants.Provider, file.Split('.')[0]);
            if (item == null) continue;
            if (item.Name == "TBD" || string.IsNullOrWhiteSpace(item.Name)) item.Name = item.ID;
            items.Add(item);
        }

        return items;
    }

    public async Task<SaturnItemModel> GetItemData(DisplayItemModel item)
    {
        List<SaturnItemModel> options = new();

        string file = Constants.Provider.Files.First(x => x.Key.Contains(item.ID + ".uasset") && (x.Key.Contains("fortnitegame/content/athena/items/cosmetics/gliders/") || x.Key.Contains("fortnitegame/plugins/gamefeatures/brcosmetics/content/athena/items/cosmetics/gliders/"))).Key;
        var glider = await GetCharacterInfo(Constants.Provider, file.Split('.')[0]);

        foreach (var optionId in Constants.PotentialOptions)
        {
            if (!optionId.ToLower().Contains("glider") && !optionId.ToLower().Contains("umbrella")) continue;
            file = Constants.Provider.Files.FirstOrDefault(x => x.Key.Contains(optionId.ToLower() + ".uasset") && (x.Key.Contains("fortnitegame/content/athena/items/cosmetics/gliders/") || x.Key.Contains("fortnitegame/plugins/gamefeatures/brcosmetics/content/athena/items/cosmetics/gliders/")), new KeyValuePair<string, GameFile>()).Key;
            if (string.IsNullOrWhiteSpace(file)) continue;
            
            var option = await GetCharacterInfo(Constants.Provider, file.Split('.')[0]);
            
            if (option.CharacterParts["Glider"].Enums["Series"] != glider.CharacterParts["Glider"].Enums["Series"]) continue;
            if (option.CharacterParts["Glider"].Enums["SkeletalMesh"] != glider.CharacterParts["Glider"].Enums["SkeletalMesh"]) continue;

            options.Add(option);
        }

        glider.Options = options;
        return glider;
    }
}