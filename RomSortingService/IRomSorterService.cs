namespace RomSortingService;

public interface IRomSorter
{
    void Extract(string archivePath, string outputDirectory, CancellationToken ct);
}