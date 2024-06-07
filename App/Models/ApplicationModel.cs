namespace App.Models
{
    public class ApplicationModel
    {
        public string AccountNo { get; set; }
        public string ApplicationCode { get; set; }
    }

    public class ApplicationCancelModel
    {
        public string AccountNo { get; set; }
        public string remark { get; set; }
    }
    public class GetTokenEZTaxRp
    {
        public string? StatusCode { get; set; }
        public string? access_token { get; set; }
        public string? refresh_token { get; set; }
        public string? token_type { get; set; }
    }
    public class GetTokenEZTaxRq
    {
        public string? username { get; set; }
        public string? password { get; set; }
        public string? client_id { get; set; }
    }
    public class CCOWebServiceModel
    {
        public string id { get; set;}
    }

    public class GetApplication
    {
        public string? ApplicationCode { get; set; }
    }
    public class GetApplicationRespone
    {
        public string? statusCode { get; set; }
        public string? AccountNo { get; set; }
        public string? ApplicationStatusID { get; set; }
        public string? ApplicationID { get; set; }
        public string? ApplicationCode { get; set; }

        public string? applicationstatusid { get; set; }
        public string? SaleDepCode { get; set; }
        public string? SaleDepName { get; set; }
        public string? ProductModelName { get; set; }
        public string? ProductSerialNo { get; set; }



    }
}
