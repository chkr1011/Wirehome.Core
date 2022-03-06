namespace Wirehome.Core.Packages;

public sealed class PackageMetaData
{
    public string Author { get; set; }
    
    public string Caption { get; set; }

    public string ForkOf { get; set; }

    public bool IsDownloaded { get; set; }
}