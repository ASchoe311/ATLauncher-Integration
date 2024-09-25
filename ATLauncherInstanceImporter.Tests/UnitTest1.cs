using System;
using System.Linq;
using Xunit;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System.IO;

namespace ATLauncherInstanceImporter.Tests
{
    public class TestJsonRead
    {

        private string JSONText(string folder)
        {
            return File.ReadAllText(Path.Combine(@".\jsons", folder, "instance.json"));
        }

        [Theory]
        [InlineData(@"atm9")]
        [InlineData(@"cobblemon")]
        [InlineData(@"the122")]
        [InlineData(@"mechmagic")]
        [InlineData(@"vanilla")]
        [InlineData(@"cottagewitch")]
        public void TestValidJSONS(string jsonPath)
        {
            var instance = Models.Instance.FromJson(JSONText(jsonPath));
            Assert.NotNull(instance);
            Assert.NotNull(instance.Launcher);
            Assert.False(string.IsNullOrEmpty(instance.Launcher.Name));
            Assert.False(string.IsNullOrEmpty(instance.McVersion));
            if (instance.Launcher.IsVanilla.HasValue && instance.Launcher.IsVanilla.Value)
            {
                Assert.True(instance.Launcher.Mods.Count == 0);
            }
            else
            {
                Assert.False(instance.Launcher.Mods.Count == 0);
            }
            Assert.False(string.IsNullOrEmpty(instance.GetReleaseDate().ToString()));
            Assert.NotEmpty(instance.GetInstancePublishers());
        }

        [Fact]
        public void TestEmptyJSON()
        {
            var instance = Models.Instance.FromJson(JSONText("empty"));
            Assert.Null(instance);
        }

        [Theory]
        [InlineData(@"nodatecf")]
        [InlineData(@"nodatemodrinth")]
        [InlineData(@"nodatetechnic")]
        [InlineData(@"baddatecf")]
        [InlineData(@"baddatemodrinth")]
        [InlineData(@"baddatetechnic")]
        public void TestBaddates(string jsonPath)
        {
            var instance = Models.Instance.FromJson(JSONText(jsonPath));
            Assert.True(string.IsNullOrEmpty(instance.GetReleaseDate().ToString()));
        }
    }
}
