using PdfSharp.Fonts;
using System.Reflection;

namespace Dashboard.Services;

public class HsaFontResolver : IFontResolver
{
    // Exact name of the file you uploaded
    private const string FontFilename = "Roboto-VariableFont_wdth,wght.ttf";

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        // Force every request to use the Roboto file
        return new FontResolverInfo(FontFilename);
    }

    public byte[] GetFont(string faceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        
        // We look for any resource that ENDS with your filename to bypass 
        // uncertainty about the Assembly prefix (Dashboard vs MyProject)
        string resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(x => x.EndsWith(FontFilename));

        if (string.IsNullOrEmpty(resourceName))
        {
            var available = string.Join(", ", assembly.GetManifestResourceNames());
            throw new Exception($"Font Resource not found. Looking for: {FontFilename}. Found: {available}");
        }

        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null) throw new Exception($"Stream null for: {resourceName}");
            byte[] data = new byte[stream.Length];
            stream.Read(data, 0, (int)stream.Length);
            return data;
        }
    }
}