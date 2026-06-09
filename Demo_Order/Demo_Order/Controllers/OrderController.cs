using Demo_Order.Hubs;
using Demo_Order.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Demo_Order.Controllers
{
    public class OrderController : Controller
    {
        private readonly IHubContext<OrderHub> _hubContext;

        public OrderController(IHubContext<OrderHub> hubContext)
        {
            _hubContext = hubContext;
        }

        /// <summary>
        /// Điểm nhận quét QR (ví dụ: /Order/Menu/1)
        /// Thiết lập cookie cho phiên hiện tại rồi redirect sang trang Menu sạch /Order/Menu
        /// </summary>
        [HttpGet("Order/Menu/{id:int}")]
        public IActionResult MenuRedirect(int id)
        {
            if (!DataStore.Tables.TryGetValue(id, out var table))
                return NotFound();

            if (table.Status != TableStatus.Serving)
            {
                return RedirectToAction("CallStaff", new { id });
            }

            // Thiết lập cookie TableId hiện tại và SessionToken tương ứng
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddHours(4)
            };

            Response.Cookies.Append("ActiveTableId", id.ToString(), cookieOptions);
            Response.Cookies.Append($"TableSession_{id}", table.SessionToken, cookieOptions);

            return RedirectToAction("Menu");
        }

        /// <summary>
        /// Trang menu sạch không hiện ID trên thanh địa chỉ (/Order/Menu)
        /// </summary>
        [HttpGet("Order/Menu")]
        public IActionResult Menu()
        {
            // Đọc TableId từ cookie
            if (!Request.Cookies.TryGetValue("ActiveTableId", out var tableIdStr) || !int.TryParse(tableIdStr, out var id))
            {
                return RedirectToAction("Expired");
            }

            if (!DataStore.Tables.TryGetValue(id, out var table))
                return NotFound();

            if (table.Status != TableStatus.Serving)
            {
                return RedirectToAction("CallStaff", new { id });
            }

            // Kiểm tra Token hợp lệ
            string cookieKey = $"TableSession_{id}";
            if (!Request.Cookies.TryGetValue(cookieKey, out var clientToken) || clientToken != table.SessionToken)
            {
                // Xóa cookie vì token không hợp lệ
                Response.Cookies.Delete("ActiveTableId");
                Response.Cookies.Delete(cookieKey);
                return RedirectToAction("Expired");
            }

            ViewBag.TableId = id;
            ViewBag.TableName = table.TableName;

            // Truyền order hiện tại nếu có
            DataStore.Orders.TryGetValue(id, out var existingOrder);
            ViewBag.ExistingOrder = existingOrder;

            var menuByCategory = DataStore.MenuItems
                .GroupBy(m => m.Category)
                .ToDictionary(g => g.Key, g => g.ToList());

            return View(menuByCategory);
        }

        /// <summary>
        /// Trang thông báo phiên làm việc hết hạn
        /// </summary>
        [HttpGet("Order/Expired")]
        public IActionResult Expired()
        {
            return View();
        }

        /// <summary>
        /// Trang gọi nhân viên (khi bàn chưa bật phục vụ)
        /// </summary>
        [HttpGet]
        public IActionResult CallStaff(int id)
        {
            if (!DataStore.Tables.TryGetValue(id, out var table))
                return NotFound();

            ViewBag.TableId = id;
            ViewBag.TableName = table.TableName;
            return View();
        }

        /// <summary>
        /// API: Đặt món (cộng dồn vào order hiện tại)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
        {
            if (!DataStore.Tables.TryGetValue(request.TableId, out var table))
                return NotFound();

            if (table.Status != TableStatus.Serving)
                return BadRequest(new { message = "Bàn chưa sẵn sàng phục vụ" });

            // Kiểm tra token phiên làm việc bảo mật
            string cookieKey = $"TableSession_{request.TableId}";
            if (!Request.Cookies.TryGetValue(cookieKey, out var token) || token != table.SessionToken)
            {
                return BadRequest(new { message = "Phiên làm việc đã hết hạn hoặc bàn đã thanh toán. Vui lòng quét lại mã QR tại bàn." });
            }

            if (request.Items == null || !request.Items.Any())
                return BadRequest(new { message = "Vui lòng chọn ít nhất một món" });

            var order = DataStore.AddToOrder(request.TableId, request.Items);

            // Tạo summary cho notification
            var newItemNames = request.Items.Select(i =>
            {
                var menu = DataStore.MenuItems.FirstOrDefault(m => m.Id == i.MenuItemId);
                return menu != null ? $"{menu.Name} x{i.Quantity}" : "";
            }).Where(s => !string.IsNullOrEmpty(s));

            var orderSummary = string.Join(", ", newItemNames);

            // Gửi notification real-time tới nhân viên
            await _hubContext.Clients.Group("staff").SendAsync(
                "ReceiveNewOrder",
                request.TableId,
                table.TableName,
                orderSummary,
                order.TotalAmount
            );

            return Json(new
            {
                success = true,
                orderId = order.OrderId,
                totalAmount = order.TotalAmount,
                totalItems = order.TotalItems,
                message = "Đặt món thành công!"
            });
        }

        /// <summary>
        /// API: Gọi nhân viên
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RequestCallStaff([FromBody] CallStaffRequest request)
        {
            if (!DataStore.Tables.TryGetValue(request.TableId, out var table))
                return NotFound();

            // Gửi thông báo real-time tới nhân viên
            await _hubContext.Clients.Group("staff").SendAsync(
                "ReceiveCallStaff",
                request.TableId,
                table.TableName
            );

            return Json(new { success = true, message = "Đã gọi nhân viên, vui lòng đợi!" });
        }

        /// <summary>
        /// API: Lấy order hiện tại của bàn
        /// </summary>
        [HttpGet]
        public IActionResult GetCurrentOrder(int id)
        {
            DataStore.Orders.TryGetValue(id, out var order);
            if (order == null)
                return Json(new { hasOrder = false });

            return Json(new
            {
                hasOrder = true,
                orderId = order.OrderId,
                totalAmount = order.TotalAmount,
                totalItems = order.TotalItems,
                items = order.Items.Select(i => new
                {
                    i.MenuItemId,
                    i.Name,
                    i.Price,
                    i.Quantity,
                    i.Note,
                    orderedAt = i.OrderedAt.ToString("HH:mm")
                })
            });
        }
    }

    public class CallStaffRequest
    {
        public int TableId { get; set; }
    }
}
