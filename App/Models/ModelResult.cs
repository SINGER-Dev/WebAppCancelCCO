using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace App.Models
{
    public class ModelResult
    {
        public string status { get; set; }
        public string message { get; set; }
        public string errorsDetail { get; set; }
        public object result { get; set; }
    }
}
