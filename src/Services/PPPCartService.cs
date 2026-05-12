using Dashboard;

namespace Dashboard.Services
{
    public class PPPCartService
    {
        public List<CartItem> Items { get; private set; } = new();
        public event Action? OnChange;

        public void AddToCart(CartItem item)
        {
            Items.Add(item);
            NotifyStateChanged();
        }

        public void RemoveItem(Guid cartItemId)
        {
            Items.RemoveAll(i => i.CartItemId == cartItemId);
            NotifyStateChanged();
        }

        public void ClearCart()
        {
            Items.Clear();
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }

    public class CartItem
    {
        public Guid CartItemId { get; set; } = Guid.NewGuid();
        public int MenuItemId { get; set; }
        public string RecipeName { get; set; } = "";
        public string? ImageId { get; set; }
        
        public int SizeId { get; set; }
        public string SizeName { get; set; } = "";
        public double Price { get; set; }
        
        public string Fulfillment { get; set; } = "Pickup";
        public string TimeSlot { get; set; } = "Dinner";
        public double DeliveryFee { get; set; }
        public double CalculatedDistance { get; set; } // Added to fix CS0117
        
        public List<ServingConfig> Servings { get; set; } = new();
    }

    public class ServingConfig
    {
        public string Label { get; set; } = "";
        public string Notes { get; set; } = "";
        public List<string> SelectedOptionNames { get; set; } = new();
        public List<double> SelectedOptionPrices { get; set; } = new(); // Added to fix CS0117
    }
}