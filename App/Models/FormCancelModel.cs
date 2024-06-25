namespace App.Models
{
    public class FormCancelModel
    {
        public string AccountNo { get; set; }
        public string ApplicationStatusID { get; set; }   
        public string ApplicationCode { get; set; }
        public string SaleDepCode { get; set; }
        public string SaleDepName { get; set; }
        public string ProductModelName { get; set; }
        public string ProductSerialNo { get; set; }


        public string CustomerID { get; set; }
        public string Cusname { get; set; }
        public string cusMobile { get; set; }
        public string SaleName { get; set; }
        public string SaleTelephoneNo { get; set; }
    }

    public class FormConfirmModel
    {
        public string? ApplicationCode { get; set; }
        public string? Remark { get; set; }
        public string? ExceptIMEI { get; set; }
        public string? ExceptCus { get; set; }
        public string? Other { get; set; }
    }
}
