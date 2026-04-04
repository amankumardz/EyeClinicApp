namespace EyeClinicApp.ViewModels
{
    public class CartPageViewModel
    {
        public List<CartRowViewModel> Items { get; set; } = [];
        public decimal Total => Items.Sum(i => i.LineTotal);
    }

    public class CartRowViewModel
    {
        public int CartItemId { get; set; }
        public int GlassId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ImageBase64 { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal => Price * Quantity;
    }
}
