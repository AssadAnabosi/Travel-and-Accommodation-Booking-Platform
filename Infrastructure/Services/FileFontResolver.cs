using PdfSharpCore.Fonts;

namespace Infrastructure.Services;

// PdfSharpCore's default font resolver needs OS-registered fonts, which the slim .NET container
// does not have ("No Fonts installed on this device!"). This resolver loads a single sans-serif
// TTF from a known location — DejaVu on Linux (installed in the Docker image) or Arial on Windows
// dev machines — and uses it for every requested face, so PDF generation works in every
// environment without embedding a font binary in the repo.
public class FileFontResolver(string defaultFontName) : IFontResolver
{
    private const string FaceName = "AppSans";
    private static readonly byte[] FontData = LoadFont();

    public byte[] GetFont(string faceName) => FontData;
    public string DefaultFontName { get; } = defaultFontName;

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic) => new(FaceName);

    private static byte[] LoadFont()
    {
        string[] candidates =
        [
            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
            "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf")
        ];

        var path = candidates.FirstOrDefault(File.Exists)
                   ?? throw new FileNotFoundException(
                       "No usable TTF font found for PDF generation. Install a font package " +
                       "(e.g. fonts-dejavu-core) or add one to a path in FileFontResolver.");

        return File.ReadAllBytes(path);
    }
}