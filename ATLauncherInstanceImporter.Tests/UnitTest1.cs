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

        [Theory]
        [InlineData(@"atm9")]
        [InlineData(@"cobblemon")]
        [InlineData(@"the122")]
        [InlineData(@"mechmagic")]
        [InlineData(@"vanilla")]
        [InlineData(@"cottagewitch")]
        [Trait("Instance", "atm9")]
        public void TestValidJSONS(string jsonPath)
        {
            string fullPath = Path.Combine(@"C:\Users\adamr\Documents\Coding\Playnite\ATLInt\ATLauncherInstanceImporter\ATLauncherInstanceImporter.Tests\jsons\", jsonPath);
            var instance = ATLauncherInstanceImporter.GetInstanceInfo(fullPath, @"C:\Users\adamr\AppData\Roaming\ATLauncher");
            Assert.False(string.IsNullOrEmpty(instance.Name));
            Assert.False(string.IsNullOrEmpty(instance.MCVer));
            if (instance.Vanilla)
            {
                Assert.True(instance.ModList.Count == 0);
            }
            else
            {
                Assert.False(instance.ModList.Count == 0);
            }
            Assert.False(string.IsNullOrEmpty(instance.MCVer));
            Assert.False(string.IsNullOrEmpty(instance.ReleaseDate.ToString()));
        }

        [Fact]
        public void TestEmptyJSON()
        {
            var instance = ATLauncherInstanceImporter.GetInstanceInfo(@"C:\Users\adamr\Documents\Coding\Playnite\ATLInt\ATLauncherInstanceImporter\ATLauncherInstanceImporter.Tests\jsons\empty", @"C:\Users\adamr\AppData\Roaming\ATLauncher");
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
            string fullPath = Path.Combine(@"C:\Users\adamr\Documents\Coding\Playnite\ATLInt\ATLauncherInstanceImporter\ATLauncherInstanceImporter.Tests\jsons\", jsonPath);
            var instance = ATLauncherInstanceImporter.GetInstanceInfo(fullPath, @"C:\Users\adamr\AppData\Roaming\ATLauncher");
            Assert.False(string.IsNullOrEmpty(instance.ReleaseDate.ToString()));
        }
    }
}
