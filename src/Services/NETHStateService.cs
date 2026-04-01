namespace Dashboard.Services
{
    public class NETHStateService
    {
        public int? AuthenticatedStylistId { get; set; }
        public string? AuthenticatedName { get; set; }
        
        public int? ImpersonatedStylistId { get; set; }
        public string? ImpersonatedName { get; set; }

        public int? ActiveStylistId => AuthenticatedStylistId ?? ImpersonatedStylistId;
        public bool IsImpersonating => ImpersonatedStylistId.HasValue;
        public bool IsStylistLocked => AuthenticatedStylistId.HasValue;

        // Add this method to fix the compiler error
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