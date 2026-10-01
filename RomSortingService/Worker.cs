using Microsoft.Extensions.Options;

namespace RomSortingService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IRomSorterService _romSorterService;
    private readonly IExtractorService _extractorService;
    private readonly Settings _settings;

    public Worker(ILogger<Worker> logger, IOptions<Settings> settings, IRomSorterService romSorterService,
        IExtractorService extractorService)
    {
        _logger = logger;
        _extractorService = extractorService;
        _romSorterService = romSorterService;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker started running at: {time}", DateTimeOffset.Now);
            }

            _extractorService.CleanTempFolder();

            var zipFilesToExtract = _extractorService.FindNewArchivedFiles();

            foreach (var zipFile in zipFilesToExtract)
            {
                try
                {
                    _extractorService.Extract(zipFile, cancellationToken);
                    await _romSorterService.ProcessFiles(cancellationToken);
                    _romSorterService.DeleteFromDownloads(zipFile);
                    _extractorService.CleanTempFolder();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error while processing file {file}", zipFile);
                }
            }
        }
    }
}