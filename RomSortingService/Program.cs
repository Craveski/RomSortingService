using RomSortingService;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.Configure<Settings>(builder.Configuration.GetSection("Settings"));
builder.Services.AddSingleton<IRomSorterService, RomSorterService>();
builder.Services.AddSingleton<IExtractorService, ExtractorService>();

builder.Services.AddOptions<Settings>()
    .Bind(builder.Configuration.GetSection("Settings"))
    .Validate(s => !string.IsNullOrWhiteSpace(s.DownloadsPath) &&
                   !string.IsNullOrWhiteSpace(s.TempFolder) &&
                   !string.IsNullOrWhiteSpace(s.RomBaseFolder),
        "Settings section is missing file location paths")
    .ValidateOnStart();

var host = builder.Build();
host.Run();