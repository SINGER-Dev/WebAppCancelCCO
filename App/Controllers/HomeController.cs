using App.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Xml;
using Serilog;
using Newtonsoft.Json;
using System.Text;
using RestSharp;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace App.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        string strConnString, DATABASEK2, WSCANCEL, UrlEztax, UsernameEztax, PasswordEztax, ClientIdEztax;
        public HomeController(ILogger<HomeController> logger)
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var builder = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile($"appsettings.{env}.json", true, false)
                        .AddJsonFile($"appsettings.json", true, false)
                        .AddEnvironmentVariables()
                        .Build();
            _logger = logger;
            strConnString = builder.GetConnectionString("strConnString");
            DATABASEK2 = builder.GetConnectionString("DATABASEK2");
            WSCANCEL = builder.GetConnectionString("WSCANCEL");
            UrlEztax = builder.GetConnectionString("UrlEztax");
            UsernameEztax = builder.GetConnectionString("UsernameEztax");
            PasswordEztax = builder.GetConnectionString("PasswordEztax");
            ClientIdEztax = builder.GetConnectionString("ClientIdEztax");
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> FormCancel(string ApplicationCode)
        {
            FormCancelModel formCancelModel = new FormCancelModel();
            formCancelModel.ApplicationCode = ApplicationCode;

            GetApplication _GetApplication = new GetApplication();
            _GetApplication.ApplicationCode = ApplicationCode;
            GetApplicationRespone _GetApplicationRespone = await GetApplication(_GetApplication);


            formCancelModel.AccountNo = _GetApplicationRespone.AccountNo;
            formCancelModel.ApplicationCode = _GetApplicationRespone.ApplicationCode;
            formCancelModel.SaleDepCode = _GetApplicationRespone.SaleDepCode;
            formCancelModel.SaleDepName = _GetApplicationRespone.SaleDepName;
            formCancelModel.ProductModelName = _GetApplicationRespone.ProductModelName;
            formCancelModel.ProductSerialNo = _GetApplicationRespone.ProductSerialNo;
            formCancelModel.applicationstatusid = _GetApplicationRespone.applicationstatusid;
            
            return View(formCancelModel);
        }

        [HttpPost]
        public ActionResult Search(Models.ApplicationModel _ApplicationModel)
        {
            Log.Debug(JsonConvert.SerializeObject(_ApplicationModel));
            List<ApplicationResponeModel> _ApplicationResponeModelMaster = new List<ApplicationResponeModel>();
            SqlConnection connection = new SqlConnection();
            connection.ConnectionString = strConnString;

            try
            {

                SqlCommand sqlCommand;
                string strSQL = DATABASEK2 + ".[dbo].[GET_DATA_APPLICATION_CANCEL]";
                sqlCommand = new SqlCommand(strSQL, connection);
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.Parameters.AddWithValue("AccountNo", _ApplicationModel.AccountNo);
                sqlCommand.Parameters.AddWithValue("ApplicationCode", _ApplicationModel.ApplicationCode);
                SqlDataAdapter dtAdapter = new SqlDataAdapter();
                dtAdapter.SelectCommand = sqlCommand;
                DataTable dt = new DataTable();
                dtAdapter.Fill(dt);
                if (dt.Rows.Count > 0)
                {

                    foreach (DataRow row in dt.Rows)
                    {
                        ApplicationResponeModel _ApplicationResponeModel = new ApplicationResponeModel();
                        _ApplicationResponeModel.AccountNo = row["AccountNo"].ToString();
                        _ApplicationResponeModel.ApplicationID = row["ApplicationID"].ToString();
                        _ApplicationResponeModel.SaleDepCode = row["SaleDepCode"].ToString();
                        _ApplicationResponeModel.SaleDepName = row["SaleDepName"].ToString();
                        _ApplicationResponeModel.ProductModelName = row["ProductModelName"].ToString();
                        _ApplicationResponeModel.ProductSerialNo = row["ProductSerialNo"].ToString();
                        _ApplicationResponeModel.ApplicationStatusID = row["ApplicationStatusID"].ToString();
                        _ApplicationResponeModel.ESIG_CONFIRM_STATUS = row["ESIG_CONFIRM_STATUS"].ToString(); 
                        _ApplicationResponeModel.RECEIVE_FLAG = row["RECEIVE_FLAG"].ToString();

                        _ApplicationResponeModel.signedStatus = row["signedStatus"].ToString();
                        _ApplicationResponeModel.statusReceived = row["statusReceived"].ToString();
                        _ApplicationResponeModel.ApplicationCode = row["ApplicationCode"].ToString();
                        

                        _ApplicationResponeModel.ApplicationDate = row["ApplicationDate"].ToString();
                        
                        _ApplicationResponeModelMaster.Add(_ApplicationResponeModel);
                    }
                }

                Log.Debug(JsonConvert.SerializeObject(_ApplicationResponeModelMaster));

                sqlCommand.Parameters.Clear();  
                connection.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return PartialView("_SearchResults", _ApplicationResponeModelMaster);
        }
        // Dummy method to simulate search operation
        [HttpPost]
        public async Task<string> UpdateDataCancel(FormConfirmModel _FormConfirmModel)
        {
            string ResultDescription = "";
            try
            {
                
                SqlConnection connection = new SqlConnection();
                connection.ConnectionString = strConnString;
                connection.Open();

                GetApplication _GetApplication = new GetApplication();
                _GetApplication.ApplicationCode = _FormConfirmModel.ApplicationCode;
                GetApplicationRespone _GetApplicationRespone = await GetApplication(_GetApplication);

                //Cancel Application
                //CCOWebServiceModel _CCOWebService = new CCOWebServiceModel();
                //_CCOWebService.id = _GetApplicationRespone.ApplicationID;
                //MessageModel _MessageModel = await CCOWebService(_CCOWebService);

                //Cancel EZ Tax
                //GetTokenEZTaxRp _GetTokenEZTaxRp = await GetTokenEZTax();

                //Cancel econtract


                SqlCommand sqlCommand;
                string strSQL = DATABASEK2 + ".[dbo].[CancelApplication]";
                sqlCommand = new SqlCommand(strSQL, connection);
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.Parameters.AddWithValue("ApplicationCode", _GetApplicationRespone.ApplicationCode);
                sqlCommand.Parameters.AddWithValue("Remark", _FormConfirmModel.Remark);
                sqlCommand.Parameters.AddWithValue("CANCEL_USER", _FormConfirmModel.CANCEL_USER);
                sqlCommand.Parameters.AddWithValue("Except_IMEI", _FormConfirmModel.ExceptIMEI);
                sqlCommand.Parameters.AddWithValue("Except_CUST", _FormConfirmModel.ExceptCus);

                SqlDataAdapter dtAdapter = new SqlDataAdapter();
                dtAdapter.SelectCommand = sqlCommand;
                DataTable dt = new DataTable();
                dtAdapter.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    if ("ERROR" == dt.Rows[0]["Result"].ToString().ToUpper())
                    {
                        ResultDescription += _GetApplicationRespone.AccountNo + " " + dt.Rows[0]["ResultDescription"].ToString();
                    }
                }
                sqlCommand.Parameters.Clear();
                connection.Close();

                return ResultDescription;
            }
            catch(Exception ex)
            {
                ResultDescription = ex.Message;
                return ResultDescription;
            }

            
        }

        public IActionResult StatusSuccess()
        {
            return View();
        }
        protected HttpWebRequest CreateWebRequest(string url)
        {

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Headers.Add(@"SOAP:Action");

            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            return webRequest;
        }

        [HttpPost]
        async public Task<MessageModel> CCOWebService(CCOWebServiceModel _CCOWebService)
        {
            string result = "";
            MessageModel _MessageModel = new MessageModel();
            Log.Debug(JsonConvert.SerializeObject(_CCOWebService.id));

            try
            {

                HttpWebRequest request = CreateWebRequest(WSCANCEL);

                XmlDocument soapEnvelopeXml = new XmlDocument();
                soapEnvelopeXml.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap12:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap12=""http://www.w3.org/2003/05/soap-envelope"">
  <soap12:Body>
    <WorkflowGoToCancelRequest xmlns=""http://tempuri.org/"">
      <id>" + _CCOWebService.id + @"</id>
    </WorkflowGoToCancelRequest>
  </soap12:Body>
</soap12:Envelope>");

                using (Stream stream = request.GetRequestStream())
                {
                    soapEnvelopeXml.Save(stream);
                }

                using (WebResponse response = request.GetResponse())
                {
                    using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                    {
                        string soapResult = rd.ReadToEnd();
                        XmlDocument xmlDocument = new XmlDocument();
                        xmlDocument.LoadXml(soapResult);
                        result = xmlDocument.InnerText;
                        if ("TRUE" == result.ToUpper())
                        {
                            _MessageModel.StatusCode = "200";
                            _MessageModel.Message = "Success";
                            Log.Debug("WorkflowGoToCancelRequest Complete");
                        }
                        else
                        {
                            _MessageModel.StatusCode = "400";
                            _MessageModel.Message = result;
                            Log.Error("WorkflowGoToCancelRequest Fail : " + result);
                        }
                    }
                }
                return _MessageModel;
            }
            catch (Exception ex)
            {
                _MessageModel.StatusCode = "500";
                _MessageModel.Message = ex.Message;
                Log.Error("WorkflowGoToCancelRequest Fail : " + ex.Message);
                return _MessageModel;
            }
            
        }


        [HttpPost]
        async public Task<GetTokenEZTaxRp> GetTokenEZTax()
        {

            GetTokenEZTaxRp _GetTokenEZTaxRp = new GetTokenEZTaxRp();
            int i = 1;
            try
            {

                var body = "";
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var client = new RestClient(UrlEztax+ "/api/auth"); 
                client.Timeout = 60000;
                var request = new RestRequest(Method.POST);
                var Arr_Body = new
                {
                    username = UsernameEztax,
                    password = PasswordEztax,
                    client_id = ClientIdEztax
                };
                body = JsonConvert.SerializeObject(Arr_Body);
                request.AddParameter("application/json", body, ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);
                if ("OK" == response.StatusCode.ToString().ToUpper())
                {

                    _GetTokenEZTaxRp = JsonConvert.DeserializeObject<GetTokenEZTaxRp>(response.Content);
                    _GetTokenEZTaxRp.StatusCode = "PASS";
                    Log.Debug(JsonConvert.SerializeObject(_GetTokenEZTaxRp));
                }
                else
                {
                    _GetTokenEZTaxRp.StatusCode = response.StatusCode.ToString();

                    Log.Debug(JsonConvert.SerializeObject(_GetTokenEZTaxRp));
                }
                return _GetTokenEZTaxRp;
            }
            catch (Exception ex)
            {
                _GetTokenEZTaxRp.StatusCode = ex.Message;
                Log.Debug(JsonConvert.SerializeObject(_GetTokenEZTaxRp));
                return _GetTokenEZTaxRp;
            }

        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        [Route("GetApplication")]
        public async Task<GetApplicationRespone> GetApplication(GetApplication _GetApplication)
        {
            GetApplicationRespone _GetApplicationRespone = new GetApplicationRespone();
            DataTable dt = new DataTable();
            try
            {
                Log.Debug(JsonConvert.SerializeObject(_GetApplication));

                SqlConnection connection = new SqlConnection();
                connection.ConnectionString = strConnString;
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls11;
                connection.Open();
                SqlCommand sqlCommand;

                string sql = "SELECT app.ApplicationID,app.AccountNo,app.ApplicationStatusID, app.ApplicationCode ,app.applicationstatusid,app.ProductID, app.ProductModelName,app.ProductSerialNo ,app.SaleDepCode,app.SaleDepName FROM " + DATABASEK2 + ".[dbo].[Application] app WHERE app.ApplicationCode = @ApplicationCode";
                sqlCommand = new SqlCommand(sql, connection);
                sqlCommand.CommandType = CommandType.Text;
                sqlCommand.Parameters.Add("@ApplicationCode", SqlDbType.NChar);
                sqlCommand.Parameters["@ApplicationCode"].Value = _GetApplication.ApplicationCode;
                SqlDataAdapter dtAdapter = new SqlDataAdapter();
                dtAdapter.SelectCommand = sqlCommand;
                dtAdapter.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    _GetApplicationRespone.statusCode = "PASS";
                    _GetApplicationRespone.AccountNo = dt.Rows[0]["AccountNo"].ToString();
                    _GetApplicationRespone.ApplicationStatusID = dt.Rows[0]["ApplicationStatusID"].ToString();
                    _GetApplicationRespone.ApplicationCode = dt.Rows[0]["ApplicationCode"].ToString();
                    _GetApplicationRespone.ApplicationID = dt.Rows[0]["ApplicationID"].ToString();


                    _GetApplicationRespone.SaleDepCode = dt.Rows[0]["SaleDepCode"].ToString();
                    _GetApplicationRespone.SaleDepName = dt.Rows[0]["SaleDepName"].ToString();
                    _GetApplicationRespone.ProductModelName = dt.Rows[0]["ProductModelName"].ToString();
                    _GetApplicationRespone.ProductSerialNo = dt.Rows[0]["ProductSerialNo"].ToString();
                    _GetApplicationRespone.applicationstatusid = dt.Rows[0]["applicationstatusid"].ToString();

                }
                else
                {
                    _GetApplicationRespone.statusCode = "Not Found";
                }

                Log.Debug(JsonConvert.SerializeObject(_GetApplicationRespone));

                sqlCommand.Parameters.Clear();
                connection.Close();
                return _GetApplicationRespone;
            }
            catch (Exception ex)
            {
                _GetApplicationRespone.statusCode = "FAIL";
                Log.Debug("GetApplication FAIL : " + ex.Message);
                return _GetApplicationRespone;
            }

        }

        public class FormData
        {
            public string ApplicationID { get; set; }
            public string Remark { get; set; }
            public string CANCEL_USER { get; set; }
        }
    }
}
