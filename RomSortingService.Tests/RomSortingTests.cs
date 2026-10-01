using System.IO.Compression;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace RomSortingService.Tests;

public class RomSortingTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "RomTests_" + Guid.NewGuid());
    private readonly Settings _settings;
    private readonly ExtractorService _extractor;
    private readonly RomSorterService _sorter;

    public RomSortingTests()
    {
        _settings = new Settings
        {
            DownloadsPath = Directory.CreateDirectory(Path.Combine(_root, "Downloads")).FullName,
            TempFolder = Directory.CreateDirectory(Path.Combine(_root, "Temp")).FullName,
            RomBaseFolder = Directory.CreateDirectory(Path.Combine(_root, "Roms")).FullName,
            NumberOfDaysToCheck = 2
        };

        var options = Options.Create(_settings);
        _extractor = new ExtractorService(options, NullLogger<ExtractorService>.Instance);
        _sorter = new RomSorterService(options, NullLogger<Worker>.Instance); // it takes ILogger<Worker> today
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Theory]
    [InlineData("Game.gba", "GBA")]
    [InlineData("Game.nes", "NES")]
    [InlineData("Game.sfc", "SNES")]
    [InlineData("Game.md", "Megadrive")]
    public async Task Rom_ends_ip_in_correct_folder(string romName, string expectedFolder)
    {
        var ct = TestContext.Current.CancellationToken;
        var zip = CreateZipFile("testgame.zip", romName);

        _extractor.Extract(zip, ct);
        await _sorter.ProcessFiles(ct);

        Assert.True(File.Exists(Path.Combine(_settings.RomBaseFolder, expectedFolder, romName)));
        Assert.Empty(Directory.GetFiles(_settings.TempFolder));
    }

    [Fact]
    public void Files_older_than_specified_cutoff_are_ignored()
    {
        //Create myself a zip file with an Atari Lyn rom that is older than whatever the cutoff is
        var zipFile = CreateZipFile("too old.zip", "too old.lnx");
        File.SetLastWriteTime(zipFile, DateTime.Now.AddDays(-(_settings.NumberOfDaysToCheck) + 1));
    }

    private string CreateZipFile(string zipFileName, string romName)
    {
        var path = Path.Combine(_settings.DownloadsPath, zipFileName);
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        using var writer = new StreamWriter(zip.CreateEntry(romName).Open());
        writer.Write("test rom data");
        return path;
    }
}