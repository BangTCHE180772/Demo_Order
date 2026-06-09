using System.Collections.Concurrent;

namespace Demo_Order.Models
{
    /// <summary>
    /// In-memory data store cho demo. Lưu trữ bàn, menu, và orders.
    /// </summary>
    public static class DataStore
    {
        // === TABLES ===
        public static ConcurrentDictionary<int, Table> Tables { get; } = new(
            Enumerable.Range(1, 5).Select(i => new KeyValuePair<int, Table>(i, new Table
            {
                TableId = i,
                TableName = $"Bàn {i}",
                Status = TableStatus.Available
            }))
        );

        // === ORDERS (key = tableId) ===
        public static ConcurrentDictionary<int, Order> Orders { get; } = new();

        // === MENU ===
        public static List<MenuItem> MenuItems { get; } = new()
        {
            // Khai vị
            new MenuItem { Id = 1, Name = "Gỏi cuốn tôm thịt", Description = "Gỏi cuốn tươi với tôm, thịt heo, bún, rau sống", Price = 45000, Category = "Khai vị", ImageUrl = "https://images.unsplash.com/photo-1534422298391-e4f8c172dddb?w=300&auto=format&fit=crop&q=80" },
            new MenuItem { Id = 2, Name = "Chả giò rế", Description = "Chả giò chiên giòn vàng, chấm nước mắm chua ngọt", Price = 55000, Category = "Khai vị", ImageUrl = "https://images.unsplash.com/photo-1601050690597-df056fb4ce78?w=300&auto=format&fit=crop&q=80" },
            new MenuItem { Id = 3, Name = "Súp bào ngư", Description = "Súp bào ngư nấm đông cô, thơm ngon bổ dưỡng", Price = 65000, Category = "Khai vị", ImageUrl = "https://images.unsplash.com/photo-1547592180-85f173990554?w=300&auto=format&fit=crop&q=80" },

            // Món chính
            new MenuItem { Id = 4, Name = "Cơm chiên Dương Châu", Description = "Cơm chiên với tôm, lạp xưởng, trứng, rau củ", Price = 75000, Category = "Món chính", ImageUrl = "https://images.unsplash.com/photo-1603133872878-684f208fb84b?w=300&auto=format&fit=crop&q=80" },
            new MenuItem { Id = 5, Name = "Phở bò tái nạm", Description = "Phở bò truyền thống với nước dùng hầm xương 12 giờ", Price = 65000, Category = "Món chính", ImageUrl = "https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?w=300&auto=format&fit=crop&q=80" },
            new MenuItem { Id = 6, Name = "Bún chả Hà Nội", Description = "Bún chả nướng than hoa, kèm nước chấm đặc biệt", Price = 70000, Category = "Món chính", ImageUrl = "https://images.unsplash.com/photo-1569718212165-3a8278d5f624?w=300&auto=format&fit=crop&q=80" },
            new MenuItem { Id = 7, Name = "Cá kho tộ", Description = "Cá basa kho tộ đất, thấm vị caramel mắm", Price = 85000, Category = "Món chính", ImageUrl = "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=300&auto=format&fit=crop&q=80" },
            new MenuItem { Id = 8, Name = "Gà nướng mật ong", Description = "Gà nướng mật ong, thơm lừng, da giòn", Price = 95000, Category = "Món chính", ImageUrl = "https://images.unsplash.com/photo-1598515214211-89d3c73ae83b?w=300&auto=format&fit=crop&q=80" },

            // Đồ uống
            new MenuItem { Id = 9, Name = "Trà đào cam sả", Description = "Trà đào tươi mát với cam và sả thơm", Price = 35000, Category = "Đồ uống", ImageUrl = "https://images.unsplash.com/photo-1497534446932-c925b458314e?w=300&auto=format&fit=crop&q=80" },
            new MenuItem { Id = 10, Name = "Cà phê sữa đá", Description = "Cà phê phin truyền thống với sữa đặc", Price = 29000, Category = "Đồ uống", ImageUrl = "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?w=300&auto=format&fit=crop&q=80" },
            new MenuItem { Id = 11, Name = "Nước ép cam", Description = "Cam tươi vắt nguyên chất, không đường", Price = 35000, Category = "Đồ uống", ImageUrl = "https://images.unsplash.com/photo-1621506289937-a8e4df240d0b?w=300&auto=format&fit=crop&q=80" },
            new MenuItem { Id = 12, Name = "Sinh tố bơ", Description = "Sinh tố bơ béo ngậy, thêm sữa đặc", Price = 40000, Category = "Đồ uống", ImageUrl = "https://images.unsplash.com/photo-1540420773420-3366772f4999?w=300&auto=format&fit=crop&q=80" },

            // Tráng miệng
            new MenuItem { Id = 13, Name = "Chè khúc bạch", Description = "Chè khúc bạch mát lạnh với nước cốt dừa", Price = 30000, Category = "Tráng miệng", ImageUrl = "https://images.unsplash.com/photo-1563729784474-d77dbb933a9e?w=300&auto=format&fit=crop&q=80" },
            new MenuItem { Id = 14, Name = "Bánh flan caramel", Description = "Bánh flan mềm mịn với caramel đắng nhẹ", Price = 25000, Category = "Tráng miệng", ImageUrl = "https://images.unsplash.com/photo-1528975604071-b4dc52a2d18c?w=300&auto=format&fit=crop&q=80" },
        };

        /// <summary>
        /// Thêm items vào order hiện tại hoặc tạo mới nếu chưa có.
        /// Trả về order sau khi cập nhật.
        /// </summary>
        public static Order AddToOrder(int tableId, List<OrderItemRequest> items)
        {
            var order = Orders.GetOrAdd(tableId, _ => new Order { TableId = tableId });

            foreach (var req in items)
            {
                var menuItem = MenuItems.FirstOrDefault(m => m.Id == req.MenuItemId);
                if (menuItem == null) continue;

                // Kiểm tra xem item đã tồn tại trong order chưa
                var existingItem = order.Items.FirstOrDefault(i => i.MenuItemId == req.MenuItemId);
                if (existingItem != null)
                {
                    existingItem.Quantity += req.Quantity;
                }
                else
                {
                    order.Items.Add(new OrderItem
                    {
                        MenuItemId = req.MenuItemId,
                        Name = menuItem.Name,
                        Price = menuItem.Price,
                        Quantity = req.Quantity,
                        Note = req.Note,
                        OrderedAt = DateTime.Now
                    });
                }
            }

            return order;
        }

        /// <summary>
        /// Đóng bàn: xóa order, chuyển trạng thái về Available
        /// </summary>
        public static void CloseTable(int tableId)
        {
            Orders.TryRemove(tableId, out _);
            if (Tables.TryGetValue(tableId, out var table))
            {
                table.Status = TableStatus.Available;
            }
        }
    }
}
