namespace Dashboard.Services
{
    public class NETHStateService
    {
        public int? AuthenticatedStylistId { get; set; }
        public string? AuthenticatedName { get; set; }
        
        public int? ImpersonatedStylistId { get; set; }
        public string? ImpersonatedName { get; set; }

        // Unified helpers for all pages
        public int? ActiveStylistId => AuthenticatedStylistId ?? ImpersonatedStylistId;
        public string? ActiveName => AuthenticatedName ?? ImpersonatedName;

        public bool IsImpersonating => ImpersonatedStylistId.HasValue;
        public bool IsStylistLocked => AuthenticatedStylistId.HasValue;

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

        public void Logout()
        {
            AuthenticatedStylistId = null;
            AuthenticatedName = null;
            Clear();
        }
    }
}