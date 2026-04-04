namespace EyeClinicApp.ViewModels
{
    public class MyOrdersViewModel
    {
        public List<MyOrderRowViewModel> Orders { get; set; } = [];
    }

    public class MyOrderRowViewModel
    {
        public int OrderId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public decimal TotalAmount { get; set; }
        public List<MyOrderItemRowViewModel> Items { get; set; } = [];
    }

    public class MyOrderItemRowViewModel
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
