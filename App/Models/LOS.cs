namespace App.Models
{
    public class CancelLOS
    {
        public string refCode { get; set; }
        public string? userName { get; set; }
    }

    public class ChangeIMEI
    {
        public string accountNo { get; set; }
        public string originalSerialNo { get; set; }
        public string newSerialNo { get; set; }
    }
}
