namespace ATLauncherInstanceImporter.Models
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using Newtonsoft.Json;
    using Playnite;
    using Playnite.SDK;
    using Playnite.SDK.Models;

    public enum SourceEnum
    {
        CurseForge = 0,
        Modrinth = 1,
        Technic = 2,
        ATLauncher = 3,
        Vanilla = 4
    }

    public partial class Instance
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        [JsonProperty("launcher", NullValueHandling = NullValueHandling.Ignore)]
        public Launcher Launcher { get; set; }

        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string McVersion { get; set; }

        [JsonProperty("releaseTime", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? ReleaseTime { get; set; }

        public SourceEnum PackSource()
        {
            if (Launcher.CurseForgeProject != null)
            {
                return SourceEnum.CurseForge;
            }
            if (Launcher.ModrinthProject != null)
            {
                return SourceEnum.Modrinth;
            }
            if (Launcher.TechnicModpack != null)
            {
                return SourceEnum.Technic;
            }
            return (Launcher.IsVanilla.HasValue && Launcher.IsVanilla.Value) ? SourceEnum.Vanilla : SourceEnum.ATLauncher;
        }

        public MetadataNameProperty GetPackSource()
        {
            switch (PackSource())
            {
                case SourceEnum.CurseForge:
                    return new MetadataNameProperty("CurseForge");
                case SourceEnum.Modrinth:
                    return new MetadataNameProperty("Modrinth");
                case SourceEnum.Technic:
                    return new MetadataNameProperty("Technic");
                case SourceEnum.ATLauncher:
                    return new MetadataNameProperty("ATLauncher");
                default:
                    return new MetadataNameProperty("Mojang Studios");
            }
        }

        public HashSet<MetadataProperty> GetPackAuthors()
        {
            HashSet<MetadataProperty> authors = new HashSet<MetadataProperty>();
            switch (PackSource())
            {
                case SourceEnum.CurseForge:
                    foreach (var auth in Launcher.CurseForgeProject.Authors)
                    {
                        authors.Add(new MetadataNameProperty(auth.Name));
                    }
                    return authors;
                case SourceEnum.Modrinth:
                    try
                    {
                        WebRequest request = WebRequest.Create($"https://api.modrinth.com/v2/project/{Launcher.ModrinthProject.Slug}/members");
                        request.Timeout = 5000;
                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                        Stream dataStream = response.GetResponseStream();
                        StreamReader reader = new StreamReader(dataStream);
                        string responseFromServer = reader.ReadToEnd();
                        //Console.WriteLine(responseFromServer.Trim('[').Trim(']'));
                        dynamic json = JsonConvert.DeserializeObject(responseFromServer);
                        foreach (var member in json)
                        {
                            authors.Add(new MetadataNameProperty(member["user"]["username"].ToString()));
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Warn($"Failed to request modpack authors from Modrinth with error message: {e.Message}");
                    }
                    return authors;
                case SourceEnum.Technic:
                    authors.Add(new MetadataNameProperty(Launcher.TechnicModpack.Author));
                    return authors;
                default:
                    return authors;
            }
        }

        public static List<string> GetModAuthors(Mod mod)
        {
            List<string> authors = new List<string>();
            if (mod.CurseForgeProject != null)
            {
                foreach (var auth in mod.CurseForgeProject.Authors)
                {
                    authors.Add(auth.Name);
                }
            }
            else if (mod.ModrinthProject != null)
            {
                try
                {
                    WebRequest request = WebRequest.Create($"https://api.modrinth.com/v2/project/{mod.ModrinthProject.Slug}/members");
                    request.Timeout = 5000;
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    Stream dataStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);
                    string responseFromServer = reader.ReadToEnd();
                    //Console.WriteLine(responseFromServer.Trim('[').Trim(']'));
                    dynamic json = JsonConvert.DeserializeObject(responseFromServer);
                    foreach (var member in json)
                    {
                        authors.Add(member["user"]["username"].ToString());
                    }
                }
                catch (Exception e)
                {
                    logger.Warn($"Failed to request mod authors from Modrinth with error message: {e.Message}");
                }
            }
            return authors;
        }

        public List<Link> GetPackLinks()
        {
            List<Link> links = new List<Link>();
            switch (PackSource())
            {
                case SourceEnum.CurseForge:
                    if (Launcher.CurseForgeProject.Links.WebsiteUrl != string.Empty)
                    {
                        links.Add(new Link("CurseForge Page", Launcher.CurseForgeProject.Links.WebsiteUrl));
                    }
                    if (Launcher.CurseForgeProject.Links.SourceUrl != string.Empty)
                    {
                        links.Add(new Link("Modpack Source", Launcher.CurseForgeProject.Links.SourceUrl));
                    }
                    if (Launcher.CurseForgeProject.Links.WikiUrl != string.Empty)
                    {
                        links.Add(new Link("Modpack Wiki", Launcher.CurseForgeProject.Links.WikiUrl));
                    }
                    break;
                case SourceEnum.Modrinth:
                    links.Add(new Link("Modrinth Page", $"https://modrinth.com/modpack/{Launcher.ModrinthProject.Slug}"));
                    if (Launcher.ModrinthProject.SourceUrl != null && Launcher.ModrinthProject.SourceUrl != string.Empty)
                    {
                        links.Add(new Link("Modpack Source", Launcher.ModrinthProject.SourceUrl));
                    }
                    if (Launcher.ModrinthProject.WikiUrl != null && Launcher.ModrinthProject.WikiUrl != string.Empty)
                    {
                        links.Add(new Link("Modpack Wiki", Launcher.ModrinthProject.WikiUrl));
                    }
                    break;
                case SourceEnum.Technic:
                    if (Launcher.TechnicModpack.PackUrl != null && Launcher.TechnicModpack.PackUrl != string.Empty)
                    {
                        links.Add(new Link("Technic Page", Launcher.TechnicModpack.PackUrl));
                    }
                    break;
                case SourceEnum.ATLauncher:
                    Regex rgx = new Regex("[^a-zA-Z0-9-]");
                    string packSlug = rgx.Replace(Launcher.Pack, "").ToLower();
                    links.Add(new Link("ATLauncher Page", $"https://atlauncher.com/pack/{packSlug}"));
                    break;
                case SourceEnum.Vanilla:
                    links.Add(new Link("Minecraft.net", "https://www.minecraft.net/"));
                    links.Add(new Link("Minecraft Wiki", "https://minecraft.wiki/"));
                    break;
            }
            return links;
        }

        public static Tuple<MetadataFile, MetadataFile> GetPackImages(Instance instance, string instanceDir)
        {
            var icon = new MetadataFile(Path.Combine(ATLauncherInstanceImporter.AssemblyPath, "icon.png"));
            var cover = new MetadataFile(Path.Combine(ATLauncherInstanceImporter.AssemblyPath, @"Resources\defaultimage.png"));
            switch (instance.PackSource())
            {
                case SourceEnum.CurseForge:
                    if (instance.Launcher.CurseForgeProject.Logo.ThumbnailUrl != null && instance.Launcher.CurseForgeProject.Logo.ThumbnailUrl != string.Empty)
                    {
                        icon = new MetadataFile(instance.Launcher.CurseForgeProject.Logo.ThumbnailUrl);
                    }
                    if (instance.Launcher.CurseForgeProject.Logo.Url != null && instance.Launcher.CurseForgeProject.Logo.Url != string.Empty)
                    {
                        cover = new MetadataFile(instance.Launcher.CurseForgeProject.Logo.Url);
                    }
                    break;
                case SourceEnum.Modrinth:
                    if (instance.Launcher.ModrinthProject.IconUrl != null && instance.Launcher.ModrinthProject.IconUrl != string.Empty)
                    {
                        icon = new MetadataFile(instance.Launcher.ModrinthProject.IconUrl);
                    }
                    if (File.Exists(Path.Combine(instanceDir, "instance.png")))
                    {
                        cover = new MetadataFile(Path.Combine(instanceDir, "instance.png"));
                    }
                    break;
                case SourceEnum.Technic:
                    if (instance.Launcher.TechnicModpack.Icon.Url != null && instance.Launcher.TechnicModpack.Icon.Url != string.Empty)
                    {
                        icon = new MetadataFile(instance.Launcher.TechnicModpack.Icon.Url);
                    }
                    if (instance.Launcher.TechnicModpack.Logo.Url != null && instance.Launcher.TechnicModpack.Logo.Url != string.Empty)
                    {
                        cover = new MetadataFile(instance.Launcher.TechnicModpack.Logo.Url);
                    }
                    break;
                case SourceEnum.ATLauncher:
                    Regex rgx = new Regex("[^a-zA-Z0-9-]");
                    string packSlug = rgx.Replace(instance.Launcher.Pack.ToLower(), "");
                    packSlug = Regex.Replace(packSlug, @"\s+", "");
                    cover = new MetadataFile($"https://cdn.atlcdn.net/images/packs/{packSlug}.png");
                    break;
                case SourceEnum.Vanilla:
                    icon = new MetadataFile("https://minecraft.wiki/images/Grass_Block_JE7_BE6.png");
                    break;
            }
            return Tuple.Create(icon, cover);
        }
    }

    public partial class Launcher
    {
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("pack", NullValueHandling = NullValueHandling.Ignore)]
        public string Pack { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        [JsonProperty("curseForgeProject", NullValueHandling = NullValueHandling.Ignore)]
        public CurseForgeProject CurseForgeProject { get; set; }

        [JsonProperty("modrinthProject", NullValueHandling = NullValueHandling.Ignore)]
        public ModrinthProject ModrinthProject { get; set; }

        [JsonProperty("technicModpack", NullValueHandling = NullValueHandling.Ignore)]
        public TechnicModpack TechnicModpack { get; set; }

        [JsonProperty("mods", NullValueHandling = NullValueHandling.Ignore)]
        public List<Mod> Mods { get; set; }

        [JsonProperty("vanillaInstance", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsVanilla { get; set; }
    }

    public partial class CurseForgeProject
    {
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("authors", NullValueHandling = NullValueHandling.Ignore)]
        public List<Author> Authors { get; set; }

        [JsonProperty("summary", NullValueHandling = NullValueHandling.Ignore)]
        public string Summary { get; set; }

        [JsonProperty("dateReleased", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? DateReleased { get; set; }

        [JsonProperty("links", NullValueHandling = NullValueHandling.Ignore)]
        public Links Links { get; set; }

        [JsonProperty("logo", NullValueHandling = NullValueHandling.Ignore)]
        public Logo Logo { get; set; }
    }

    public partial class Author
    {
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }
    }

    public partial class Links
    {
        [JsonProperty("websiteUrl", NullValueHandling = NullValueHandling.Ignore)]
        public string WebsiteUrl { get; set; }

        [JsonProperty("wikiUrl", NullValueHandling = NullValueHandling.Ignore)]
        public string WikiUrl { get; set; }

        [JsonProperty("sourceUrl", NullValueHandling = NullValueHandling.Ignore)]
        public string SourceUrl { get; set; }
    }

    public partial class Logo
    {
        [JsonProperty("thumbnailUrl", NullValueHandling = NullValueHandling.Ignore)]
        public string ThumbnailUrl { get; set; }

        [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
        public string Url { get; set; }
    }

    public partial class ModrinthProject
    {
        [JsonProperty("slug", NullValueHandling = NullValueHandling.Ignore)]
        public string Slug { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        [JsonProperty("published", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? DateReleased { get; set; }

        [JsonProperty("icon_url", NullValueHandling = NullValueHandling.Ignore)]
        public string IconUrl { get; set; }

        [JsonProperty("source_url", NullValueHandling = NullValueHandling.Ignore)]
        public string SourceUrl { get; set; }

        [JsonProperty("wiki_url", NullValueHandling = NullValueHandling.Ignore)]
        public string WikiUrl { get; set; }
    }

    public partial class TechnicModpack
    {
        [JsonProperty("user", NullValueHandling = NullValueHandling.Ignore)]
        public string Author { get; set; }

        [JsonProperty("platformUrl", NullValueHandling = NullValueHandling.Ignore)]
        public string PackUrl { get; set; }

        [JsonProperty("icon", NullValueHandling = NullValueHandling.Ignore)]
        public Icon Icon { get; set; }

        [JsonProperty("logo", NullValueHandling = NullValueHandling.Ignore)]
        public Logo Logo { get; set; }
    }

    public partial class Icon
    {
        [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
        public string Url { get; set; }
    }

    public partial class Mod
    {
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        [JsonProperty("curseForgeProject", NullValueHandling = NullValueHandling.Ignore)]
        public CurseForgeProject CurseForgeProject { get; set; }

        [JsonProperty("modrinthProject", NullValueHandling = NullValueHandling.Ignore)]
        public ModrinthProject ModrinthProject { get; set; }
    }

    public partial class Instance
    {
        public static Instance FromJson(string json) => JsonConvert.DeserializeObject<Instance>(json);
    }

    public static class Serialize
    {
        public static string ToJson(this Instance self) => JsonConvert.SerializeObject(self);
    }



}