namespace RomSortingService;

public interface IExtractorService
{
    List<string> FindNewArchivedFiles();
    void Extract(string archiveFile, CancellationToken stoppingToken);
    void CleanTempFolder();
}