namespace App.Models
{
    public class FormCancelModel
    {
        public string AccountNo { get; set; }

        public string applicationstatusid { get; set; }   
        public string ApplicationCode { get; set; }
        public string SaleDepCode { get; set; }
        public string SaleDepName { get; set; }
        public string ProductModelName { get; set; }
        public string ProductSerialNo { get; set; }
    }

    public class FormConfirmModel
    {
        public string? ApplicationCode { get; set; }
        public string? Remark { get; set; }
        public string? ExceptIMEI { get; set; }
        public string? ExceptCus { get; set; }
        public string? CANCEL_USER { get; set; }
    }
}
