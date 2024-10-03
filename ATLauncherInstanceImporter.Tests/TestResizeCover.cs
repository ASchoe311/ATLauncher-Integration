using ATLauncherInstanceImporter.Models;
using Newtonsoft.Json;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ATLauncherInstanceImporter.Tests
{
    public class TestResizeCover
    {
        private string GetInstanceUuid(string dir)
        {
            var input = File.ReadAllText(Path.Combine(dir, "instance.json"));
            var template = new { uuid = string.Empty };
            var result = JsonConvert.DeserializeAnonymousType(input, template);
            return result.uuid;
        }

        private void DeleteImages(string dir)
        {
            foreach (var file in Directory.EnumerateFiles(dir))
            {
                if (Path.GetExtension(file) == ".png")
                {
                    File.Delete(file);
                }
            }
        }

        private void DeleteAndCache(string dir)
        {
            DeleteImages(dir);
            _ = ATLauncherInstanceImporter.ResizeCover(dir, false, dir);
            _ = ATLauncherInstanceImporter.ResizeCover(dir, true, dir);
        }

        [Theory]
        [InlineData("cottagewitch")]
        [InlineData("vanilla")]
        [InlineData("cobblemon")]
        public void TestCacheImageStd(string testDir)
        {
            string dir = Path.Combine(@".\TestData", testDir);
            string imgCache = Path.Combine(dir, "ImageCache");
            string uuid = GetInstanceUuid(dir);
            DeleteImages(dir);
            _ = ATLauncherInstanceImporter.ResizeCover(dir, false, dir);
            Assert.True(File.Exists(Path.Combine(imgCache, $"{uuid}_cover.png")));
            Assert.True(File.Exists(Path.Combine(imgCache, $"{uuid}_bg.png")));
        }


    }
}
