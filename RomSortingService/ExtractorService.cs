using Microsoft.Extensions.Options;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace RomSortingService;

public class ExtractorService : IExtractorService
{
    private static readonly string[] ArchiveExtensions = [".zip", ".7z"];

    private readonly Settings _settings;
    private readonly ILogger<ExtractorService> _logger;

    public ExtractorService(IOptions<Settings> settings, ILogger<ExtractorService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public List<string> FindNewArchivedFiles()
    {
        var cutoff = DateTime.Now.AddDays(-_settings.NumberOfDaysToCheck);

        return Directory.GetFiles(_settings.DownloadsPath)
            .Where(f => ArchiveExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .Where(f => File.GetLastWriteTime(f) >= cutoff)
            .ToList();
    }

    public void Extract(string archiveFile, CancellationToken stoppingToken)
    {
        var options = new ExtractionOptions { ExtractFullPath = false, Overwrite = true };

        using var archive = ArchiveFactory.OpenArchive(archiveFile);

        //Note to self - this is to handle the 7zip files
        if (archive.IsSolid)
        {
            using var reader = archive.ExtractAllEntries();
            while (reader.MoveToNextEntry())
            {
                stoppingToken.ThrowIfCancellationRequested();
                if (reader.Entry.IsDirectory) continue;

                _logger.LogInformation("Extracting {Entry} from {Archive}", reader.Entry.Key,
                    Path.GetFileName(archiveFile));
                reader.WriteEntryToDirectory(_settings.TempFolder, options);
            }
        }
        else
        {
            foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
            {
                stoppingToken.ThrowIfCancellationRequested();

                _logger.LogInformation("Extracting {Entry} from {Archive}", entry.Key, Path.GetFileName(archiveFile));
                entry.WriteToDirectory(_settings.TempFolder, options);
            }
        }
    }

    public void CleanTempFolder()
    {
        foreach (var file in Directory.GetFiles(_settings.TempFolder))
        {
            File.Delete(file);
        }
    }
}