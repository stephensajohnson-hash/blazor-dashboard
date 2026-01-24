using PdfSharp.Fonts;
using System.Reflection;

namespace Dashboard.Services;

public class HsaFontResolver : IFontResolver
{
    private const string FontFilename = "Roboto-VariableFont_wdth,wght.ttf";

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        return new FontResolverInfo(FontFilename);
    }

    public byte[]? GetFont(string faceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        
        string? resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(x => x.EndsWith(FontFilename));

        if (string.IsNullOrEmpty(resourceName)) return null;

        using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null) return null;
            byte[] data = new byte[stream.Length];
            stream.Read(data, 0, (int)stream.Length);
            return data;
        }
    }
}