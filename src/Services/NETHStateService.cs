namespace Dashboard.Services;

public class NETHStateService
{
    public int? ImpersonatedStylistId { get; set; }
    public string? ImpersonatedName { get; set; }

    public bool IsImpersonating => ImpersonatedStylistId.HasValue;

    public void Impersonate(int id, string name)
    {
        ImpersonatedStylistId = id;
        ImpersonatedName = name;
    }

    public void Clear()
    {
        ImpersonatedStylistId = null;
        ImpersonatedName = null;
    }
}