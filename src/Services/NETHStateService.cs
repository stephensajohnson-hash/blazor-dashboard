namespace Dashboard.Services
{
    public class NETHStateService
    {
        // Set when a stylist logs in directly
        public int? AuthenticatedStylistId { get; set; }
        public string? AuthenticatedName { get; set; }
        
        // Set when an admin chooses to view as a stylist
        public int? ImpersonatedStylistId { get; set; }
        public string? ImpersonatedName { get; set; }

        // The active ID used by all pages for filtering
        public int? ActiveStylistId => AuthenticatedStylistId ?? ImpersonatedStylistId;
        public string? ActiveName => AuthenticatedName ?? ImpersonatedName;

        public bool IsImpersonating => ImpersonatedStylistId.HasValue;
        public bool IsStylistLocked => AuthenticatedStylistId.HasValue;

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