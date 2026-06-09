using Microsoft.AspNetCore.SignalR;

namespace Demo_Order.Hubs
{
    public class OrderHub : Hub
    {
        /// <summary>
        /// Khi nhân viên kết nối, join vào group "staff"
        /// </summary>
        public async Task JoinStaffGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "staff");
        }

        /// <summary>
        /// Gửi thông báo order mới tới tất cả nhân viên
        /// </summary>
        public async Task NotifyNewOrder(int tableId, string tableName, string orderSummary, decimal totalAmount)
        {
            await Clients.Group("staff").SendAsync("ReceiveNewOrder", tableId, tableName, orderSummary, totalAmount);
        }

        /// <summary>
        /// Gửi thông báo gọi nhân viên
        /// </summary>
        public async Task NotifyCallStaff(int tableId, string tableName)
        {
            await Clients.Group("staff").SendAsync("ReceiveCallStaff", tableId, tableName);
        }

        /// <summary>
        /// Gửi thông báo thay đổi trạng thái bàn
        /// </summary>
        public async Task NotifyTableStatusChanged(int tableId, string status, string statusText)
        {
            await Clients.All.SendAsync("ReceiveTableStatusChanged", tableId, status, statusText);
        }
    }
}
