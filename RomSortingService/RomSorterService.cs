using Microsoft.Extensions.Options;

namespace RomSortingService;

public class RomSorterService : IRomSorterService
{
    private readonly Settings _settings;
    private readonly ILogger<Worker> _logger;

    public RomSorterService(IOptions<Settings> settings, ILogger<Worker> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task ProcessFiles(CancellationToken cancellationToken)
    {
        var fileLoc = _settings.TempFolder;
        var files = Directory.GetFiles(fileLoc).ToList();

        var folderName = MatchFolder(files);

        if (string.IsNullOrEmpty(folderName))
        {
            _logger.LogInformation("No recognized ROM extension in temp folder — skipping it");
            return;
        }

        var destinationBase = _settings.RomBaseFolder;
        var fullDestination = Path.Combine(destinationBase, folderName);
        Directory.CreateDirectory(fullDestination);

        foreach (var file in files)
        {
            var destFile = Path.Combine(fullDestination, Path.GetFileName(file));
            _logger.LogInformation("Attempting to move: {Source} -> {Dest}", file, destFile);

            var maxAttempts = 3;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    File.Move(file, destFile, overwrite: true);
                    _logger.LogInformation("Moved {File} to {Destination}", Path.GetFileName(file), destFile);
                    break;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    if (attempt == maxAttempts)
                    {
                        _logger.LogWarning(ex, "Skipped {File} — could not move to destination",
                            Path.GetFileName(file));
                    }

                    await Task.Delay(500, cancellationToken);
                }
            }
        }
    }

    private string? MatchFolder(List<string> files)
    {
        var fileMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { ".32x", "32X" },
            { ".lnx", "Atari Lynx" },
            { ".d64", "Commodore 64" },
            { ".t64", "Commodore 64" },
            { ".crt", "Commodore 64" },
            { ".gg", "Game Gear" },
            { ".gb", "Gameboy" },
            { ".gba", "GBA" },
            { ".gbc", "GBC" },
            { ".j64", "Jaguar" },
            { ".md", "Megadrive" },
            { ".gen", "Megadrive" },
            { ".nes", "NES" },
            { ".sms", "Sega Master System" },
            { ".sfc", "SNES" },
            { ".smc", "SNES" }
        };

        foreach (var file in files)
        {
            var ext = Path.GetExtension(file);

            if (fileMap.TryGetValue(ext, out var folderName))
            {
                return folderName;
            }
        }

        return null;
    }

    public void DeleteFromDownloads(string zipFile)
    {
        try
        {
            File.Delete(zipFile);
            _logger.LogInformation("Deleted {ZipFile}", zipFile);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while deleting file {file}", zipFile);
        }
    }
}