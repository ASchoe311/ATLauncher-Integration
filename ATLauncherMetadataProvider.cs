using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATLauncherInstanceImporter
{
    public class ATLauncherMetadataProvider : LibraryMetadataProvider
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private readonly ATLauncherInstanceImporter _plugin;

        public ATLauncherMetadataProvider(ATLauncherInstanceImporter plugin)
        {
            this._plugin = plugin;
        }


        public override GameMetadata GetMetadata(Game game)
        {
            Models.Instance instance = _plugin.GetInstance(game.InstallDirectory);
            //logger.Debug(Playnite.SDK.Data.Serialization.ToJson(instance));
            Tuple<MetadataFile, MetadataFile> imgs = Models.Instance.GetPackImages(instance, game.InstallDirectory);
            //var metaData = new GameMetadata()
            //{
            //    Name = instance.Launcher.Name ?? instance.Launcher.Pack,
            //    Description = GenerateInstanceDescription(instance),
            //    IsInstalled = true,
            //    Source = new MetadataNameProperty("ATLauncher"),
            //    Icon = imgs.Item1,
            //    CoverImage = imgs.Item2,
            //    BackgroundImage = imgs.Item2,
            //    Developers = instance.GetPackAuthors(),
            //    Links = instance.GetPackLinks(),
            //    ReleaseDate = instance.GetReleaseDate(),
            //    Publishers = instance.GetInstancePublishers(),
            //    Features = new HashSet<MetadataProperty> { new MetadataNameProperty("Single Player"), new MetadataNameProperty("Multiplayer") },
            //    Genres = new HashSet<MetadataProperty> { new MetadataNameProperty("Sandbox"), new MetadataNameProperty("Survival") },
            //    Platforms = new HashSet<MetadataProperty> { GetOS() }
            //};
            var metaData = new GameMetadata { };
            metaData.Name = instance.Launcher.Name ?? instance.Launcher.Pack;
            metaData.Description = GenerateInstanceDescription(instance);
            metaData.IsInstalled = true;
            metaData.Source = new MetadataNameProperty("ATLauncher");
            metaData.Icon = imgs.Item1;
            metaData.CoverImage = imgs.Item2;
            metaData.BackgroundImage = imgs.Item2;
            metaData.Developers = instance.GetPackAuthors();
            metaData.Links = instance.GetPackLinks();
            metaData.ReleaseDate = instance.GetReleaseDate();
            metaData.Publishers = instance.GetInstancePublishers();
            metaData.Features = new HashSet<MetadataProperty> { new MetadataNameProperty("Single Player"), new MetadataNameProperty("Multiplayer") };
            metaData.Genres = new HashSet<MetadataProperty> { new MetadataNameProperty("Sandbox"), new MetadataNameProperty("Survival") };
            metaData.Platforms = new HashSet<MetadataProperty> { GetOS() };
            return metaData;
        }

        private string GenerateInstanceDescription(Models.Instance instance)
        {
            //logger.Info($"Generating description for instance {instance.Launcher.Name}");
            string description = string.Empty;
            if (instance.Launcher.IsVanilla.HasValue && instance.Launcher.IsVanilla.Value)
            {
                description += $"<h1>Vanilla Minecraft {instance.McVersion}</h1>";
                return description;
            }
            if (instance.Launcher.Description != null)
            {
                description = $"<h2>{instance.Launcher.Description}</h2>";
            }
            description += $"<h1>Minecraft Version: {instance.McVersion}</h1>";
            description += $"<h1>Contains {instance.Launcher.Mods.Count} mods</h1>";
            description += "<h1>Mod List</h1><hr>";
            foreach (var mod in instance.Launcher.Mods)
            {
                description += "<p><h2>";
                if (mod.CurseForgeProject != null)
                {
                    description += mod.CurseForgeProject.Links.WebsiteUrl != string.Empty ? $"<a href={mod.CurseForgeProject.Links.WebsiteUrl}>{mod.Name}</a>" : $"{mod.Name}";
                }
                else if (mod.ModrinthProject != null)
                {
                    description += $"<a href=https://modrinth.com/mod/{mod.ModrinthProject.Slug}>{mod.Name}</a>";
                }
                else
                {
                    description += $"{mod.Name}";
                }
                description += "</h2>";
                var modAuths = Models.Instance.GetModAuthors(mod);
                string authString = modAuths.Count == 0 ? "No authors listed" : "By";
                //logger.Debug($"{mod.Authors.Count()}");
                for (int i = 0; i < modAuths.Count(); i++)
                {
                    if (i == modAuths.Count() - 1 && i != 0)
                    {
                        authString += " and";
                    }
                    authString += " " + modAuths[i];
                    if (modAuths.Count() > 2 && i != modAuths.Count() - 1)
                    {
                        authString += $",";
                    }

                }
                description += $"<i>{authString}</i>";
                description += $"<h3>{mod.Description}</h3></p><br>";
            }
            return description;
        }

        private MetadataNameProperty GetOS()
        {
            int platform = (int)Environment.OSVersion.Platform;
            if (platform == 4 || platform == 128)
            {
                return new MetadataNameProperty("PC (Linux)");
            }
            if (platform == 6)
            {
                return new MetadataNameProperty("Macintosh");
            }
            if (platform == 2)
            {
                return new MetadataNameProperty("PC (Windows)");
            }
            return new MetadataNameProperty("Other");
        }

    }
}
