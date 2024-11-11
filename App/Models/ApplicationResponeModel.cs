using System.Reflection.Emit;

namespace App.Models
{
    public class ApplicationResponeModel
    {
        public string ApplicationID { get; set; }
        public string ApplicationCode { get; set; }
        public string AccountNo { get; set; }
        public string SaleName { get; set; }
        public string DepartmentID { get; set; }
        public string CustomerID { get; set; }
        public string ProductModelName { get; set; }
        public string? ProductSerialNo { get; set; }
        public string CreateDate { get; set; }
        public string ApplicationStatusID { get; set; }
        public string SaleDepCode { get; set; }
        public string SaleDepName { get; set; }
        public string SaleTelephoneNo { get; set; }
        public string? ESigStatus { get; set; }
        public string? ESIG_CONFIRM_STATUS { get; set; }
        public string? RECEIVE_FLAG { get; set; }
        public string? ApplicationDate { get; set; }
        public string? signedStatus { get; set; }
        public string? statusReceived { get; set; }
        public string?  numregis { get; set; }
        public string? newnum { get; set; }
        public string? paynum { get; set; }
        public string? numdoc { get; set; }
        public string datenowcheck { get; set; }
        public string Cusname { get; set; }
        public string cusMobile { get; set; }
        public string LINE_STATUS { get; set; }
        public string RefCode { get; set; }
        public string? OU_Code { get; set; }
        public string? loanTypeCate { get; set; }
        public string? Ref4 { get; set; }
        public string? appIns { get; set; }
        public string? Status { get; set; }
        
    }

    public class LendingInfoRq
    {
        public string? ApplicationCode { get; set; }
        public string? application_date { get; set; }
        public string? product_serial { get; set; }
        public string? flat_rate { get; set; }
        public string? cash_price { get; set; }
        public string? down_payment { get; set; }
        public string? down_amount { get; set; }
        public string? new_loan { get; set; }
        public string? contract_term { get; set; }
        public string? discount { get; set; }
    }
    public class SearchGetApplicationHistoryRespone
    {
        public string ApplicationCode { get; set; }
        public string AccountNo { get; set; }
        public string ProductSerialNo { get; set; }
        public string ProductModelName { get; set; }
        public string ApplicationRemark { get; set; }
        public string CreateDate { get; set; }
        public string CreateBy { get; set; }
        public string SaleDepName { get; set; }
        public string CustomerID { get; set; }
        public string cusMobile { get; set; }
        public string Cusname { get; set; }
        public string ApplicationStatusID { get; set; } 
        public string SaleDepCode { get; set; }
        public string SaleTelephoneNo { get; set; }

    }
}
