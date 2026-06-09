using Demo_Order.Hubs;
using Demo_Order.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using QRCoder;

namespace Demo_Order.Controllers
{
    public class TableController : Controller
    {
        private readonly IHubContext<OrderHub> _hubContext;

        public TableController(IHubContext<OrderHub> hubContext)
        {
            _hubContext = hubContext;
        }

        /// <summary>
        /// Trang sơ đồ bàn cho nhân viên
        /// </summary>
        public IActionResult FloorPlan()
        {
            var tables = DataStore.Tables.Values.OrderBy(t => t.TableId).ToList();
            return View(tables);
        }

        /// <summary>
        /// Lấy thông tin chi tiết bàn + order (JSON)
        /// </summary>
        [HttpGet]
        public IActionResult GetTableDetail(int id)
        {
            if (!DataStore.Tables.TryGetValue(id, out var table))
                return NotFound();

            DataStore.Orders.TryGetValue(id, out var order);

            return Json(new
            {
                table = new
                {
                    table.TableId,
                    table.TableName,
                    status = table.Status.ToString(),
                    table.StatusText,
                    table.StatusCss
                },
                order = order == null ? null : new
                {
                    order.OrderId,
                    order.TotalAmount,
                    order.TotalItems,
                    createdAt = order.CreatedAt.ToString("HH:mm dd/MM"),
                    items = order.Items.Select(i => new
                    {
                        i.MenuItemId,
                        i.Name,
                        i.Price,
                        i.Quantity,
                        i.Note,
                        orderedAt = i.OrderedAt.ToString("HH:mm")
                    })
                }
            });
        }

        /// <summary>
        /// Chuyển trạng thái bàn
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SetStatus(int id, [FromBody] SetStatusRequest request)
        {
            if (!DataStore.Tables.TryGetValue(id, out var table))
                return NotFound();

            if (Enum.TryParse<TableStatus>(request.Status, out var status))
            {
                table.Status = status;
                await _hubContext.Clients.All.SendAsync("ReceiveTableStatusChanged", id, status.ToString(), table.StatusText);
                return Json(new { success = true, status = table.Status.ToString(), statusText = table.StatusText });
            }

            return BadRequest();
        }

        /// <summary>
        /// Đóng bàn (clear order + về trạng thái Available)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CloseTable(int id)
        {
            DataStore.CloseTable(id);
            await _hubContext.Clients.All.SendAsync("ReceiveTableStatusChanged", id, "Available", "Trống");
            return Json(new { success = true });
        }

        /// <summary>
        /// Sinh QR code cho bàn (trả về base64 PNG)
        /// </summary>
        [HttpGet]
        public IActionResult GetQrCode(int id)
        {
            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            var orderUrl = $"{baseUrl}/Order/Menu/{id}";

            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(orderUrl, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(8, new byte[] { 30, 30, 46 }, new byte[] { 255, 255, 255 });

            return Json(new
            {
                qrBase64 = Convert.ToBase64String(qrCodeBytes),
                orderUrl
            });
        }

        /// <summary>
        /// Lấy danh sách tất cả bàn (JSON)
        /// </summary>
        [HttpGet]
        public IActionResult GetAllTables()
        {
            var tables = DataStore.Tables.Values.OrderBy(t => t.TableId).Select(t =>
            {
                DataStore.Orders.TryGetValue(t.TableId, out var order);
                return new
                {
                    t.TableId,
                    t.TableName,
                    status = t.Status.ToString(),
                    t.StatusText,
                    t.StatusCss,
                    totalItems = order?.TotalItems ?? 0,
                    totalAmount = order?.TotalAmount ?? 0
                };
            });

            return Json(tables);
        }
    }

    public class SetStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}
