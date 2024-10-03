using Xunit;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System.IO;
using System;

namespace ATLauncherInstanceImporter.Tests
{
    public class TestTokenFormatter
    {
        private string JSONText(string folder)
        {
            return File.ReadAllText(Path.Combine(@".\jsons", folder, "instance.json"));
        }

        private string RandomizeCase(string token)
        {
            string randomized = string.Empty;
            Random rand = new Random();
            for (int i = 0; i < token.Length; i++)
            {
                randomized += (rand.Next() > 0.5) ? Char.ToUpper(token[i]) : token[i];
            }
            return token;
        }

        [Theory]
        [InlineData(@"atm9")]
        [InlineData(@"cobblemon")]
        [InlineData(@"the122")]
        [InlineData(@"mechmagic")]
        [InlineData(@"vanilla")]
        [InlineData(@"cottagewitch")]
        public void TestInstanceName(string jsonPath)
        {
            var instance = Models.Instance.FromJson(JSONText(jsonPath));
            string tokenString = "{instanceName}";
            string formatted = ATLauncherInstanceImporter.ChangeInstanceName(tokenString, Path.Combine(@".\jsons", jsonPath));
            Assert.Equal(instance.Launcher.Name, formatted);
        }

        [Theory]
        [InlineData(@"atm9")]
        [InlineData(@"cobblemon")]
        [InlineData(@"the122")]
        [InlineData(@"mechmagic")]
        [InlineData(@"vanilla")]
        [InlineData(@"cottagewitch")]
        public void TestPackName(string jsonPath)
        {
            var instance = Models.Instance.FromJson(JSONText(jsonPath));
            string tokenString = "{packName}";
            string formatted = ATLauncherInstanceImporter.ChangeInstanceName(tokenString, Path.Combine(@".\jsons", jsonPath));
            Assert.Equal(instance.Launcher.Pack, formatted);
        }

        [Theory]
        [InlineData(@"atm9")]
        [InlineData(@"cobblemon")]
        [InlineData(@"the122")]
        [InlineData(@"mechmagic")]
        [InlineData(@"vanilla")]
        [InlineData(@"cottagewitch")]
        public void TestPackVersion(string jsonPath)
        {
            var instance = Models.Instance.FromJson(JSONText(jsonPath));
            string tokenString = "{packVersion}";
            string formatted = ATLauncherInstanceImporter.ChangeInstanceName(tokenString, Path.Combine(@".\jsons", jsonPath));
            Assert.Equal(instance.Launcher.Version, formatted);
        }

        [Theory]
        [InlineData(@"atm9")]
        [InlineData(@"cobblemon")]
        [InlineData(@"the122")]
        [InlineData(@"mechmagic")]
        [InlineData(@"vanilla")]
        [InlineData(@"cottagewitch")]
        public void TestMcVersion(string jsonPath)
        {
            var instance = Models.Instance.FromJson(JSONText(jsonPath));
            string tokenString = "{mcVersion}";
            string formatted = ATLauncherInstanceImporter.ChangeInstanceName(tokenString, Path.Combine(@".\jsons", jsonPath));
            Assert.Equal(instance.McVersion, formatted);
        }

        [Theory]
        [InlineData(@"atm9")]
        [InlineData(@"cobblemon")]
        [InlineData(@"the122")]
        [InlineData(@"mechmagic")]
        [InlineData(@"cottagewitch")]
        public void TestModLoaderModded(string jsonPath)
        {
            var instance = Models.Instance.FromJson(JSONText(jsonPath));
            string tokenString = "{modLoader}";
            string formatted = ATLauncherInstanceImporter.ChangeInstanceName(tokenString, Path.Combine(@".\jsons", jsonPath));
            Assert.Equal(instance.Launcher.LoaderVersion.Type, formatted);
        }

        [Fact]
        public void TestModLoaderVanilla()
        {
            var instance = Models.Instance.FromJson(JSONText("vanilla"));
            string tokenString = "{modLoader}";
            string formatted = ATLauncherInstanceImporter.ChangeInstanceName(tokenString, Path.Combine(@".\jsons", "vanilla"));
            Assert.Equal("Vanilla", formatted);
        }

        [Theory]
        [InlineData(@"atm9")]
        [InlineData(@"cobblemon")]
        [InlineData(@"the122")]
        [InlineData(@"mechmagic")]
        [InlineData(@"cottagewitch")]
        public void TestMultipleTokens(string jsonPath)
        {
            var instance = Models.Instance.FromJson(JSONText(jsonPath));
            string tokenString = "{packName} {packVersion} for MC {mcVersion} ({modLoader})";
            string formatted = ATLauncherInstanceImporter.ChangeInstanceName(tokenString, Path.Combine(@".\jsons", jsonPath));
            Assert.Equal($"{instance.Launcher.Pack} {instance.Launcher.Version} for MC {instance.McVersion} ({instance.Launcher.LoaderVersion.Type})", formatted);
        }

        [Theory]
        [InlineData(@"atm9")]
        [InlineData(@"cobblemon")]
        [InlineData(@"the122")]
        [InlineData(@"mechmagic")]
        [InlineData(@"cottagewitch")]
        public void TestBadTokens(string jsonPath)
        {
            var instance = Models.Instance.FromJson(JSONText(jsonPath));
            string tokenString = "{packNae} {packVersion} for MC {mcVersion ({modLoader})";
            string formatted = ATLauncherInstanceImporter.ChangeInstanceName(tokenString, Path.Combine(@".\jsons", jsonPath));
            Assert.Equal($"{{packNae}} {instance.Launcher.Version} for MC {{mcVersion ({instance.Launcher.LoaderVersion.Type})", formatted);
        }

    }
}
