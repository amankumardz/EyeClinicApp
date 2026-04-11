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
        public string? RightEyeSph { get; set; }
        public string? RightEyeCyl { get; set; }
        public string? RightEyeAxis { get; set; }
        public string? LeftEyeSph { get; set; }
        public string? LeftEyeCyl { get; set; }
        public string? LeftEyeAxis { get; set; }
        public decimal LineTotal => Price * Quantity;
    }
}
