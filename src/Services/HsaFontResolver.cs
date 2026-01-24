using PdfSharp.Fonts;

namespace Dashboard.Services;

public class HsaFontResolver : IFontResolver
{
    public byte[] GetFont(string faceName) => null; // Fallback to system default
    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        // This forces PDFSharp to use the platform's default font (usually DejaVu)
        return PlatformFontResolver.ResolveTypeface(familyName, isBold, isItalic);
    }
}