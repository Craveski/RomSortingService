namespace RomSortingService;

public interface IRomSorterService
{
    Task ProcessFiles(CancellationToken cancellationToken);
    void DeleteFromDownloads(string zipFile);
}