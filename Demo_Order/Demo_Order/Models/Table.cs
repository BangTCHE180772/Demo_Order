namespace Demo_Order.Models
{
    public enum TableStatus
    {
        Available,  // Trống - chưa phục vụ
        Serving,    // Đang phục vụ - khách có thể order
    }

    public class Table
    {
        public int TableId { get; set; }
        public string TableName { get; set; } = string.Empty;
        public TableStatus Status { get; set; } = TableStatus.Available;

        public string StatusText => Status switch
        {
            TableStatus.Available => "Trống",
            TableStatus.Serving => "Đang phục vụ",
            _ => "Không xác định"
        };

        public string StatusCss => Status switch
        {
            TableStatus.Available => "available",
            TableStatus.Serving => "serving",
            _ => "unknown"
        };
    }
}
