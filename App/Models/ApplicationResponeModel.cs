namespace App.Models
{
    public class ApplicationResponeModel
    {
        public string ApplicationID { get; set; }
        public string ApplicationCode { get; set; }
        
        public string AccountNo { get; set; }
        public string SaleName { get; set; }
        public string DepartmentID { get; set; }
        public string ProductModelName { get; set; }
        public string ProductSerialNo { get; set; }
        public string CreateDate { get; set; }
        public string ApplicationStatusID { get; set; }
        public string SaleDepCode { get; set; }
        public string SaleDepName { get; set; }

        public string? ESigStatus { get; set; }
        public string? ESIG_CONFIRM_STATUS { get; set; }
        public string? RECEIVE_FLAG { get; set; }
        public string? ApplicationDate { get; set; }
        public string? signedStatus { get; set; }
        public string? statusReceived { get; set; }
        
        
    }
    
}
