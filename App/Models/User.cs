namespace App.Model
{
	public class Login
	{
		public string? user_id { get; set; }	
		public string? password { get; set; }
	}

    public class User
    {
        public string? StatusCode { get; set; }
        public string? Message { get; set; }
        public string? user_id { get; set; }
        public string? id_card { get; set; }
        public string? mobile_no { get; set; }
        public string? name { get; set; }
        public string? position { get; set; }
        public string? company { get; set; }
        public string? first_name_en { get; set; }
        public string? last_name_en { get; set; }
        public string[]? Role { get; set; }
    }
}
