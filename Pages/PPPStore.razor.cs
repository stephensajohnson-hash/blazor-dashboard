using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Dashboard.Services;
using Microsoft.AspNetCore.Components.Web;
using System.Net.Http.Json;
using Dashboard;

namespace Dashboard.Pages
{
    public partial class PPPStore : ComponentBase
    {
        // MANUALLY MAPPING INJECTED SERVICES
        [Inject] protected IDbContextFactory<AppDbContext> DbFactory { get; set; } = default!;
        [Inject] protected NavigationManager Nav { get; set; } = default!;
        [Inject] protected IJSRuntime JS { get; set; } = default!;
        [Inject] protected GeocodingService GeoService { get; set; } = default!;
        [Inject] protected PPPStoreService StoreService { get; set; } = default!;

        [Parameter] public int OwnerId { get; set; }

        protected PPP_Owner? owner;
        protected List<PPP_Menu> activeMenus = new();
        protected List<ToastItem> toasts = new();
        protected PPP_Order? cart;
        protected string? customerId;
        protected bool showCart = false;
        protected bool showContactForm = false;

        protected string selTimeframe = "";
        protected string selFulfillment = "Pickup";
        protected int selPickupId = 0;
        protected string selZip = "";
        protected PPP_MenuItem? selectedItem;
        protected PPP_MenuItemSize? currentSize;

        protected (double Lat, double Lon)? ownerCoords;
        protected (double Lat, double Lon)? customerCoords;
        protected double currentDistance = 0;

        protected class ServingConfig 
        { 
            public int ServingNumber { get; set; } 
            public string LabelName { get; set; } = ""; 
            public HashSet<int> OptionIds { get; set; } = new(); 
        }
        protected List<ServingConfig> tempServingConfigs = new();

        protected List<PPP_PickupLocation> pickupLocations = new();
        protected List<PPP_DeliveryZipCode> ownerZips = new();
        protected List<PPP_DeliveryRadiusRule> ownerRadiusRules = new();

        protected PPP_User? sessionUser; 
        protected PPP_User? currentUser; 
        protected PPP_User tempUser = new();
        protected PPP_Address tempAddress = new() { Label = "Home" };

        protected override async Task OnInitializedAsync()
        {
            try
            {
                await using var db = await DbFactory.CreateDbContextAsync();
                owner = await StoreService.GetOwnerAsync(OwnerId);
                activeMenus = await StoreService.GetActiveMenusAsync(OwnerId);

                pickupLocations = await db.Set<PPP_PickupLocation>().Where(l => l.OwnerId == OwnerId).ToListAsync();
                ownerZips = await db.Set<PPP_DeliveryZipCode>().Where(z => z.OwnerId == OwnerId).ToListAsync();
                ownerRadiusRules = await db.Set<PPP_DeliveryRadiusRule>().Where(r => r.OwnerId == OwnerId && r.Enabled).OrderBy(r => r.MaxMiles).ToListAsync();

                if (owner?.Address != null)
                {
                    ownerCoords = await GeoService.GetCoordinatesAsync(owner.Address.Street, owner.Address.City, owner.Address.State, owner.Address.ZipCode);
                    if (owner.OffersPickup && !owner.OffersDelivery) selFulfillment = "Pickup";
                    else if (owner.OffersDelivery && !owner.OffersPickup) selFulfillment = "Delivery";
                }
            }
            catch (Exception ex) { Console.WriteLine($"Init Error: {ex.Message}"); }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    customerId = await JS.InvokeAsync<string>("localStorage.getItem", "ppp_customer_id") ?? Guid.NewGuid().ToString();
                    await JS.InvokeVoidAsync("localStorage.setItem", "ppp_customer_id", customerId);
                    
                    var token = await JS.InvokeAsync<string>("eval", "document.cookie.split('; ').find(row => row.startsWith('ppp_token='))?.split('=')[1]");

                    if (!string.IsNullOrEmpty(token))
                    {
                        await using var db = await DbFactory.CreateDbContextAsync();
                        sessionUser = await db.PPP_Users.Include(u => u.Addresses).FirstOrDefaultAsync(u => u.MagicToken == token && u.TokenExpiresAt > DateTime.UtcNow);

                        if (sessionUser != null) 
                        {
                            currentUser = !string.IsNullOrEmpty(sessionUser.ImpersonatingEmail) 
                                ? await db.PPP_Users.Include(u => u.Addresses).FirstOrDefaultAsync(u => u.Email == sessionUser.ImpersonatingEmail)
                                : sessionUser;

                            if (currentUser != null) ApplyIdentityToUI();
                        }
                    }
                    await LoadCart();
                    if (!string.IsNullOrEmpty(tempAddress.Street)) await UpdateCustomerCoords();
                    await InvokeAsync(StateHasChanged);
                }
                catch (Exception ex) { Console.WriteLine($"Render Error: {ex.Message}"); }
            }
        }

        protected async Task AddToCart()
        {
            if (selectedItem == null || currentSize == null || cart == null || selectedItem.Recipe == null) return;
            try 
            {
                await using var db = await DbFactory.CreateDbContextAsync();
                var newEntry = new PPP_OrderItem
                {
                    OrderId = cart.Id,
                    MenuItemId = selectedItem.Id,
                    RecipeName = selectedItem.Recipe.Name,
                    SizeName = currentSize.Name,
                    BasePrice = currentSize.BasePrice,
                    ScheduledDate = selectedItem.StartDate.ToUniversalTime(),
                    TimeframeName = selTimeframe,
                    FulfillmentMethod = selFulfillment,
                    PickupLocationId = selFulfillment == "Pickup" ? selPickupId : null,
                    DeliveryFee = selFulfillment == "Delivery" ? GetDeliveryFee(tempAddress.ZipCode) : 0,
                    DeliveryAddressSummary = selFulfillment == "Delivery" ? $"{tempAddress.Street}, {tempAddress.City}" : "",
                    Latitude = customerCoords?.Lat,
                    Longitude = customerCoords?.Lon,
                    CalculatedDistanceMiles = currentDistance
                };

                db.PPP_OrderItems.Add(newEntry);
                await db.SaveChangesAsync(); 
                
                foreach (var cfg in tempServingConfigs)
                {
                    var serving = new PPP_OrderItemServing {
                        OrderItemId = newEntry.Id,
                        ServingNumber = cfg.ServingNumber,
                        LabelName = cfg.LabelName,
                        SelectedOptions = currentSize.Options?
                            .Where(o => cfg.OptionIds.Contains(o.Id))
                            .Select(o => new PPP_OrderItemOption { OptionName = o.Name, AddOnPrice = o.AddOnPrice }).ToList() 
                            ?? new List<PPP_OrderItemOption>()
                    };
                    db.PPP_OrderItemServings.Add(serving);
                }
                await db.SaveChangesAsync();
                await LoadCart(); 
                selectedItem = null;
                await ShowToast("Bundle added to cart!");
            }
            catch (Exception ex) { await ShowToast("Error: " + ex.Message, true); }
        }

        protected async Task UpdateCustomerCoords()
        {
            if (!string.IsNullOrEmpty(tempAddress.Street) && !string.IsNullOrEmpty(tempAddress.ZipCode))
            {
                customerCoords = await GeoService.GetCoordinatesAsync(tempAddress.Street, tempAddress.City, tempAddress.State, tempAddress.ZipCode);
                if (ownerCoords.HasValue && customerCoords.HasValue) 
                    currentDistance = GeoService.CalculateDistance(ownerCoords.Value.Lat, ownerCoords.Value.Lon, customerCoords.Value.Lat, customerCoords.Value.Lon);
            }
            await InvokeAsync(StateHasChanged);
        }

        protected (double Fee, bool IsOutOfArea, string Label) GetDeliveryFeeInfo(string zip)
        {
            if (string.IsNullOrWhiteSpace(zip)) return (0, false, "Enter Zip Code");
            var match = ownerZips.FirstOrDefault(z => z.ZipCode == zip.Trim());
            if (match != null) return (match.Fee, false, "Included Zip Code");
            if (ownerCoords.HasValue && customerCoords.HasValue) {
                var rule = ownerRadiusRules.FirstOrDefault(r => currentDistance <= r.MaxMiles);
                if (rule != null) return (rule.Fee, false, $"Within {rule.MaxMiles} Mile Radius");
            }
            return (ownerRadiusRules.FirstOrDefault()?.Fee ?? 5.00, true, "Outside Area");
        }

        protected double GetDeliveryFee(string zip) => GetDeliveryFeeInfo(zip).Fee;
        protected string GetPickupLocationName(int? id) => pickupLocations.FirstOrDefault(l => l.Id == id)?.Name ?? "Pickup";
        protected double GetCartTotal() => cart?.Items?.Sum(i => i.BasePrice + i.DeliveryFee + (i.Servings?.Sum(s => s.SelectedOptions?.Sum(o => o.AddOnPrice) ?? 0) ?? 0)) ?? 0;
        
        protected async Task LoadCart() {
            await using var db = await DbFactory.CreateDbContextAsync();
            var identifier = currentUser?.Email ?? customerId;
            var existingOrder = await db.Set<PPP_Order>().Include(o => o.Items).ThenInclude(i => i.Servings).ThenInclude(s => s.SelectedOptions).FirstOrDefaultAsync(o => o.OwnerId == OwnerId && o.CustomerIdentifier == identifier && !o.IsCheckedOut);
            if (existingOrder == null) {
                existingOrder = new PPP_Order { OwnerId = OwnerId, CustomerIdentifier = identifier ?? "Guest", CreatedAt = DateTime.UtcNow };
                db.Add(existingOrder); await db.SaveChangesAsync();
            }
            cart = existingOrder;
        }

        protected void ApplyIdentityToUI() {
            if (currentUser == null) return;
            tempUser.FirstName = currentUser.FirstName; tempUser.LastName = currentUser.LastName; tempUser.Email = currentUser.Email; tempUser.Phone = currentUser.Phone;
            var addr = currentUser.Addresses?.FirstOrDefault(a => a.IsDefault) ?? currentUser.Addresses?.FirstOrDefault();
            if (addr != null) { tempAddress.Id = addr.Id; tempAddress.Label = addr.Label; tempAddress.Street = addr.Street; tempAddress.City = addr.City; tempAddress.State = addr.State; tempAddress.ZipCode = addr.ZipCode; selZip = addr.ZipCode; }
            foreach (var config in tempServingConfigs) { if (string.IsNullOrEmpty(config.LabelName)) config.LabelName = currentUser.FirstName; }
        }

        protected async Task FinalizeOrder() {
            try {
                if (string.IsNullOrWhiteSpace(tempUser.Email) || string.IsNullOrWhiteSpace(tempUser.FirstName)) { await ShowToast("Email and Name are required", true); return; }
                await using var db = await DbFactory.CreateDbContextAsync();
                var user = await db.PPP_Users.Include(u => u.Addresses).FirstOrDefaultAsync(u => u.Email.ToLower() == tempUser.Email.ToLower());
                if (user == null) { user = new PPP_User { Email = tempUser.Email.ToLower(), FirstName = tempUser.FirstName, LastName = tempUser.LastName, Phone = tempUser.Phone, MagicToken = Guid.NewGuid().ToString("N"), TokenExpiresAt = DateTime.UtcNow.AddMonths(6) }; db.PPP_Users.Add(user); await db.SaveChangesAsync(); }
                else { user.FirstName = tempUser.FirstName; user.LastName = tempUser.LastName; user.Phone = tempUser.Phone; if (string.IsNullOrEmpty(user.MagicToken)) user.MagicToken = Guid.NewGuid().ToString("N"); user.TokenExpiresAt = DateTime.UtcNow.AddMonths(6); }
                var dbOrder = await db.PPP_Orders.FirstOrDefaultAsync(o => o.Id == cart!.Id);
                if (dbOrder != null) { dbOrder.IsCheckedOut = true; dbOrder.CheckedOutAt = DateTime.UtcNow; dbOrder.CustomerIdentifier = user.Email; dbOrder.Notes = cart.Notes; await db.SaveChangesAsync(); if (sessionUser?.Role != "Admin") await JS.InvokeVoidAsync("eval", $"document.cookie = 'ppp_token={user.MagicToken}; max-age={15552000}; path=/; SameSite=Lax';"); await ShowToast("Order placed successfully!"); cart = null; showCart = false; showContactForm = false; await LoadCart(); }
            } catch (Exception ex) { await ShowToast("Checkout Error: " + ex.Message, true); }
        }

        protected async Task ShowToast(string msg, bool err = false) { var t = new ToastItem { Message = msg, IsError = err }; toasts.Add(t); await InvokeAsync(StateHasChanged); await Task.Delay(3000); toasts.Remove(t); await InvokeAsync(StateHasChanged); }
        protected void SetSize(PPP_MenuItemSize? sz) { currentSize = sz; tempServingConfigs.Clear(); if (currentSize != null) for (int i = 0; i < currentSize.ServingsPerUnit; i++) tempServingConfigs.Add(new ServingConfig { ServingNumber = i + 1, LabelName = currentUser?.FirstName ?? "" }); }
        protected void ToggleServingOption(int index, int optionId) { if (tempServingConfigs[index].OptionIds.Contains(optionId)) tempServingConfigs[index].OptionIds.Remove(optionId); else tempServingConfigs[index].OptionIds.Add(optionId); }
        protected bool IsConfigValid() => !string.IsNullOrEmpty(selTimeframe) && (selFulfillment != "Pickup" || (selPickupId != 0)) && (selFulfillment != "Delivery" || !string.IsNullOrEmpty(tempAddress.ZipCode));
        protected double GetConfigTotal() { if (currentSize == null) return 0; double fee = selFulfillment == "Delivery" ? GetDeliveryFee(tempAddress.ZipCode) : 0; double addonTotal = 0; foreach (var cfg in tempServingConfigs) { addonTotal += currentSize.Options?.Where(o => cfg.OptionIds.Contains(o.Id)).Sum(o => o.AddOnPrice) ?? 0; } return currentSize.BasePrice + fee + addonTotal; }
        protected async Task OpenCart() => showCart = true;
        protected async Task ProcessCheckout() { showCart = false; showContactForm = true; }
        protected async Task HandleSavedAddressChange(ChangeEventArgs e) { int id = int.Parse(e.Value?.ToString() ?? "0"); if (id == 0) tempAddress = new PPP_Address { Label = "Home" }; else { var addr = currentUser?.Addresses?.FirstOrDefault(a => a.Id == id); if (addr != null) { tempAddress.Id = addr.Id; tempAddress.Label = addr.Label; tempAddress.Street = addr.Street; tempAddress.City = addr.City; tempAddress.State = addr.State; tempAddress.ZipCode = addr.ZipCode; selZip = addr.ZipCode; await UpdateCustomerCoords(); } } }
        protected async Task HandleFeeZipInput(ChangeEventArgs e) { var zip = e.Value?.ToString() ?? ""; tempAddress.ZipCode = zip; if (zip.Length == 5) await UpdateCustomerCoords(); }
        protected async Task RemoveFromCart(int id) { await using var db = await DbFactory.CreateDbContextAsync(); var item = await db.PPP_OrderItems.FindAsync(id); if (item != null) { db.Remove(item); await db.SaveChangesAsync(); await LoadCart(); } }
        protected async Task SaveCartNotes() => showCart = false;
        protected async Task EndImpersonation() { Nav.NavigateTo("/ppp-admin/impersonator", forceLoad: true); }
        protected bool IsPastCutoff(PPP_MenuItem i) => DateTime.Now > i.StartDate.Date.AddDays(-1).Add(i.CutoffTime.ToTimeSpan());
        protected bool IsItemSoldOut(PPP_MenuItem i) => false;
        protected class ToastItem { public string Message { get; set; } = ""; public bool IsError { get; set; } }
        public class ZipResponse { [System.Text.Json.Serialization.JsonPropertyName("places")] public List<ZipPlace> Places { get; set; } = new(); }
        public class ZipPlace { [System.Text.Json.Serialization.JsonPropertyName("place name")] public string PlaceName { get; set; } = ""; [System.Text.Json.Serialization.JsonPropertyName("state abbreviation")] public string StateAbbreviation { get; set; } = ""; }
    }
}