using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
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
            Tuple<MetadataFile, MetadataFile, MetadataFile> imgs = Models.Instance.GetPackImages(instance, game.InstallDirectory);
            var metaData = new GameMetadata()
            {
                Description = GenerateInstanceDescription(instance),
                IsInstalled = true,
                Source = new MetadataNameProperty("ATLauncher"),
                Developers = instance.GetPackAuthors(),
                Links = instance.GetPackLinks(),
                ReleaseDate = instance.GetReleaseDate(),
                Publishers = instance.GetInstancePublishers(),
                Features = new HashSet<MetadataProperty> { new MetadataNameProperty("Single Player"), new MetadataNameProperty("Multiplayer") },
                Genres = new HashSet<MetadataProperty> { new MetadataNameProperty("Sandbox"), new MetadataNameProperty("Survival") },
                Platforms = new HashSet<MetadataProperty> { GetOS() },
                Name = instance.Launcher.Name ?? instance.Launcher.Pack,
                Icon = imgs.Item1,
                CoverImage = imgs.Item2,
                BackgroundImage = imgs.Item3
            };
            return metaData;
        }

        public static string GenerateInstanceDescription(Models.Instance instance)
        {
            //logger.Info($"Generating description for instance {instance.Launcher.Name}");
            string description = string.Empty;
            
            if (instance.Launcher.IsVanilla.HasValue && instance.Launcher.IsVanilla.Value)
            {
                if ((instance.Launcher.LoaderVersion == null || instance.Launcher.LoaderVersion.Type == null) && instance.Launcher.Mods.Count == 0)
                {
                    return $"<h1>{ResourceProvider.GetString("LOCATLauncherVanilla")} {ResourceProvider.GetString("LOCATLauncherMinecraft")} {instance.McVersion}</h1>";
                }
            }
            if (instance.Launcher.Description != null)
            {
                description = $"<h2>{instance.Launcher.Description}</h2>";
            }
            description += $"<h1>{ResourceProvider.GetString("LOCATLauncherMinecraft")} {ResourceProvider.GetString("LOCVersionLabel")}: {instance.McVersion}</h1>";
            if (instance.Launcher.LoaderVersion != null && instance.Launcher.LoaderVersion.Type != null)
            {
                description += $"<h1>{ResourceProvider.GetString("LOCATLauncherModLoader")}: {instance.Launcher.LoaderVersion.Type}</h1>";
            }
            description += $"<h1>{ResourceProvider.GetString("LOCATLauncherContains")} {instance.Launcher.Mods.Count} {ResourceProvider.GetString("LOCATLauncherMods")}</h1>";
            if (instance.Launcher.Mods.Count != 0) 
            {
                description += $"<h1>{ResourceProvider.GetString("LOCATLauncherModList")}</h1><hr>";
            }
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
                string authString = ResourceProvider.GetString("LOCATLauncherBy");
                //logger.Debug($"{mod.Authors.Count()}");
                for (int i = 0; i < modAuths.Count(); i++)
                {
                    if (i == modAuths.Count() - 1 && i != 0)
                    {
                        authString += $" {ResourceProvider.GetString("LOCATLauncherAnd")}";
                    }
                    authString += " " + $"<i>{modAuths[i]}</i>";
                    if (modAuths.Count() > 2 && i != modAuths.Count() - 1)
                    {
                        authString += $",";
                    }

                }
                if (modAuths.Count != 0)
                {
                    description += $"{authString}";
                }
                description += $"<h3>{mod.Description}</h3></p><br>";
            }
            return description;
        }

        public static MetadataNameProperty GetOS()
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
