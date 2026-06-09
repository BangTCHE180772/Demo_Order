namespace Demo_Order.Models
{
    public enum OrderStatus
    {
        Pending,    // Đang chờ
        Confirmed,  // Đã xác nhận
        Completed   // Hoàn thành
    }

    public class OrderItem
    {
        public int MenuItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? Note { get; set; }
        public DateTime OrderedAt { get; set; } = DateTime.Now;
    }

    public class Order
    {
        public string OrderId { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpper();
        public int TableId { get; set; }
        public List<OrderItem> Items { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public decimal TotalAmount => Items.Sum(i => i.Price * i.Quantity);
        public int TotalItems => Items.Sum(i => i.Quantity);
    }

    // DTO nhận từ client khi order
    public class PlaceOrderRequest
    {
        public int TableId { get; set; }
        public List<OrderItemRequest> Items { get; set; } = new();
    }

    public class OrderItemRequest
    {
        public int MenuItemId { get; set; }
        public int Quantity { get; set; }
        public string? Note { get; set; }
    }
}
