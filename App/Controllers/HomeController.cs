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
using System.Globalization;
using Microsoft.AspNetCore.Http;
using System.Reflection.Emit;

namespace App.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        string strConnString, DATABASEK2, WSCANCEL, UrlEztax, UsernameEztax, PasswordEztax, ClientIdEztax, ApiKey, SGAPIESIG;

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

            ApiKey = builder.GetConnectionString("ApiKey");
            SGAPIESIG = builder.GetConnectionString("SGAPIESIG");


        }

        public IActionResult Index()
        {
            var EMP_CODE = HttpContext.Session.GetString("EMP_CODE");
            if (EMP_CODE == null)
            {
                return Redirect("/Login");
            }
            ViewBag.EMP_CODE = HttpContext.Session.GetString("EMP_CODE");
            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            return View();
        }

        public IActionResult GetApplicationHistory()
        {
            var EMP_CODE = HttpContext.Session.GetString("EMP_CODE");
            if (EMP_CODE == null)
            {
                return Redirect("/Login");
            }

            ViewBag.EMP_CODE = HttpContext.Session.GetString("EMP_CODE");
            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            return View();
        }

        public async Task<IActionResult> FormCancel(string ApplicationCode)
        {
            var EMP_CODE = HttpContext.Session.GetString("EMP_CODE");
            if (EMP_CODE == null)
            {
                return Redirect("/Login");
            }

            ViewBag.EMP_CODE = HttpContext.Session.GetString("EMP_CODE");
            ViewBag.FullName = HttpContext.Session.GetString("FullName");

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
            formCancelModel.ApplicationStatusID = _GetApplicationRespone.ApplicationStatusID;

            formCancelModel.CustomerID = _GetApplicationRespone.CustomerID;
            formCancelModel.Cusname = _GetApplicationRespone.Cusname;
            formCancelModel.cusMobile = _GetApplicationRespone.cusMobile;
            formCancelModel.SaleName = _GetApplicationRespone.SaleName;
            formCancelModel.SaleTelephoneNo = _GetApplicationRespone.SaleTelephoneNo;

            return View(formCancelModel);
        }

        [HttpPost]
        public ActionResult SearchGetApplicationHistory(SearchGetApplicationHistory _SearchGetApplicationHistory)
        {
            Log.Debug(JsonConvert.SerializeObject(_SearchGetApplicationHistory));

            List<SearchGetApplicationHistoryRespone> _SearchGetApplicationHistoryResponeMaster = new List<SearchGetApplicationHistoryRespone>();
            SqlConnection connection = new SqlConnection();
            connection.ConnectionString = strConnString;
            try
            {
                SqlCommand sqlCommand;
                string strSQL = DATABASEK2 + ".[dbo].[GetApplicationHistory]";
                sqlCommand = new SqlCommand(strSQL, connection);
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.Parameters.AddWithValue("AccountNo", _SearchGetApplicationHistory.AccountNo);
                sqlCommand.Parameters.AddWithValue("ApplicationCode", _SearchGetApplicationHistory.ApplicationCode);
                sqlCommand.Parameters.AddWithValue("startdate", _SearchGetApplicationHistory.startdate);
                sqlCommand.Parameters.AddWithValue("enddate", _SearchGetApplicationHistory.enddate);

                SqlDataAdapter dtAdapter = new SqlDataAdapter();
                dtAdapter.SelectCommand = sqlCommand;
                DataTable dt = new DataTable();
                dtAdapter.Fill(dt);
                connection.Close();
                if (dt.Rows.Count > 0)
                {

                    foreach (DataRow row in dt.Rows)
                    {
                        SearchGetApplicationHistoryRespone _SearchGetApplicationHistoryRespone = new SearchGetApplicationHistoryRespone();

                        _SearchGetApplicationHistoryRespone.ApplicationCode = row["ApplicationCode"].ToString();
                        _SearchGetApplicationHistoryRespone.AccountNo = row["AccountNo"].ToString();
                        _SearchGetApplicationHistoryRespone.ProductSerialNo = row["ProductSerialNo"].ToString();
                        _SearchGetApplicationHistoryRespone.ProductModelName = row["ProductModelName"].ToString();
                        _SearchGetApplicationHistoryRespone.ApplicationRemark = row["ApplicationRemark"].ToString();
                        _SearchGetApplicationHistoryRespone.CreateDate = row["CreateDate"].ToString();
                        _SearchGetApplicationHistoryRespone.CreateBy = row["CreateBy"].ToString();
                        _SearchGetApplicationHistoryRespone.SaleDepName = row["SaleDepName"].ToString();
                        _SearchGetApplicationHistoryRespone.SaleDepCode = row["SaleDepCode"].ToString();
                        _SearchGetApplicationHistoryRespone.CustomerID = row["CustomerID"].ToString();
                        _SearchGetApplicationHistoryRespone.cusMobile = row["cusMobile"].ToString();
                        _SearchGetApplicationHistoryRespone.Cusname = row["Cusname"].ToString();
                        _SearchGetApplicationHistoryRespone.ApplicationStatusID = row["ApplicationStatusID"].ToString();
                        _SearchGetApplicationHistoryResponeMaster.Add(_SearchGetApplicationHistoryRespone);
                    }
                }

                Log.Debug(JsonConvert.SerializeObject(_SearchGetApplicationHistoryResponeMaster));

                sqlCommand.Parameters.Clear();
                
            }
            catch (Exception ex)
            {
                Log.Debug(ex.Message);
            }
            return PartialView("_SearchGetApplicationHistory", _SearchGetApplicationHistoryResponeMaster);
        }

        [HttpPost]
        public ActionResult Search(ApplicationRq _ApplicationModel)
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
                sqlCommand.Parameters.AddWithValue("ProductSerialNo", _ApplicationModel.ProductSerialNo);
                sqlCommand.Parameters.AddWithValue("CustomerID", _ApplicationModel.CustomerID);
                sqlCommand.Parameters.AddWithValue("status", _ApplicationModel.status);
                sqlCommand.Parameters.AddWithValue("startdate", _ApplicationModel.startdate);
                sqlCommand.Parameters.AddWithValue("enddate", _ApplicationModel.enddate);
                sqlCommand.Parameters.AddWithValue("CustomerName", _ApplicationModel.CustomerName);
                sqlCommand.Parameters.AddWithValue("StatusRegis", _ApplicationModel.StatusRegis);
                
                SqlDataAdapter dtAdapter = new SqlDataAdapter();
                dtAdapter.SelectCommand = sqlCommand;
                DataTable dt = new DataTable();
                dtAdapter.Fill(dt);
                connection.Close();
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
                        _ApplicationResponeModel.CustomerID = row["CustomerID"].ToString();

                        _ApplicationResponeModel.SaleTelephoneNo = row["SaleTelephoneNo"].ToString();
                        _ApplicationResponeModel.ApplicationDate = row["ApplicationDate2"].ToString();

                        _ApplicationResponeModel.numregis = row["numregis"].ToString();
                        _ApplicationResponeModel.newnum = row["newnum"].ToString();
                        _ApplicationResponeModel.paynum = row["paynum"].ToString();
                        _ApplicationResponeModel.numdoc = row["numdoc"].ToString();
                        _ApplicationResponeModel.Cusname = row["Cusname"].ToString();
                        _ApplicationResponeModel.cusMobile = row["cusMobile"].ToString();
                        _ApplicationResponeModel.SaleName = row["SaleName"].ToString();
                        _ApplicationResponeModel.LINE_STATUS = row["LINE_STATUS"].ToString();
                        _ApplicationResponeModel.RefCode = row["RefCode"].ToString();

                        _ApplicationResponeModel.OU_Code = row["OU_Code"].ToString();
                        _ApplicationResponeModel.loanTypeCate = row["loanTypeCate"].ToString();

                        




                        string datenowText = DateTime.Now.ToString("yyyy-MM-dd", new CultureInfo("en-US"));

                        if (row["ApplicationDate"].ToString() == datenowText)
                        {
                            _ApplicationResponeModel.datenowcheck = "1";
                        }
                        else
                        {
                            _ApplicationResponeModel.datenowcheck = "0";
                        }
                        _ApplicationResponeModelMaster.Add(_ApplicationResponeModel);
                    }
                }

                sqlCommand.Parameters.Clear();  
                
            }
            catch (Exception ex)
            {
                Log.Debug(ex.Message);
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


                // Define the start and end times for the period (8:00 AM - 10:00 PM)
                TimeSpan periodStart = new TimeSpan(8, 0, 0); // 8:00 AM
                TimeSpan periodEnd = new TimeSpan(22, 0, 0); // 10:00 PM

                // Example time to check
                DateTime now = DateTime.Now;
                TimeSpan currentTime = now.TimeOfDay;

                // Check if the current time is within the period
                bool isWithinPeriod = currentTime >= periodStart && currentTime <= periodEnd;

                if (isWithinPeriod)
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
                    sqlCommand.Parameters.AddWithValue("Remark", _FormConfirmModel.Remark + "" + _FormConfirmModel.Other);
                    sqlCommand.Parameters.AddWithValue("CANCEL_USER", HttpContext.Session.GetString("EMP_CODE"));
                    sqlCommand.Parameters.AddWithValue("Except_IMEI", _FormConfirmModel.ExceptIMEI);
                    sqlCommand.Parameters.AddWithValue("Except_CUST", _FormConfirmModel.ExceptCus);

                    SqlDataAdapter dtAdapter = new SqlDataAdapter();
                    dtAdapter.SelectCommand = sqlCommand;
                    DataTable dt = new DataTable();
                    dtAdapter.Fill(dt);
                    connection.Close();
                    if (dt.Rows.Count > 0)
                    {
                        if ("SUCCESS" != dt.Rows[0]["Result"].ToString().ToUpper())
                        {
                            ResultDescription += _GetApplicationRespone.AccountNo + " " + dt.Rows[0]["ResultDescription"].ToString();
                        }
                    }
                    sqlCommand.Parameters.Clear();

                    if (ResultDescription == "")
                    {
                        string currentDateTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                        var requestBody = new
                        {
                            applicationCode = _GetApplicationRespone.ApplicationCode,
                            applicationStatus = "CANCELLED",
                            approvalStatus = "CANCELLED",
                            approvalDatetime = currentDateTime,
                            remark = _FormConfirmModel.Remark + "" + _FormConfirmModel.Other
                        };

                        Log.Debug("API BODY REQUEST : " + JsonConvert.SerializeObject(requestBody));

                        using (HttpClient client = new HttpClient())
                        {
                            string jsonBody = JsonConvert.SerializeObject(requestBody);

                            client.DefaultRequestHeaders.Add("apikey", ApiKey);
                            client.DefaultRequestHeaders.Add("user", "DEV");

                            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                            HttpResponseMessage responseDevice = await client.PostAsync(SGAPIESIG + "/Service/C100_Status", content);
                            int DeviceStatusCode = (int)responseDevice.StatusCode;

                            Log.Debug("API BODY RESPONE : " + JsonConvert.SerializeObject(responseDevice.Content.ReadAsStringAsync()));

                            if (responseDevice.IsSuccessStatusCode)
                            {
                                var jsonResponseDevice = await responseDevice.Content.ReadAsStringAsync();

                            }
                        }
                    }
                }
                else
                {
                    ResultDescription = "ไม่สามารถยกเลิกรายการได้ เนื่องจากเลยกำหนดเวลาการยกเลิกแล้ว";
                }
                return ResultDescription;
            }
            catch(Exception ex)
            {
                ResultDescription = ex.Message;
                return ResultDescription;
            }

            
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
        public async Task<MessageModel> CCOWebService(CCOWebServiceModel _CCOWebService)
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
        public async Task<GetTokenEZTaxRp> GetTokenEZTax()
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

                string sql = "SELECT app.ApplicationID,app.AccountNo,app.ApplicationStatusID,app.CustomerID, app.ApplicationCode ,app.ProductID, cus.FirstName + ' ' + cus.LastName as Cusname ,cus.MobileNo1 as cusMobile ,app.SaleName ,app.SaleTelephoneNo,app.ProductModelName,app.ProductSerialNo,app.ProductBrandName ,app.SaleDepCode,app.SaleDepName FROM " + DATABASEK2 + ".[dbo].[Application] app left join " + DATABASEK2 + ".[dbo].Customer cus on cus.CustomerID = app.CustomerID  WHERE app.ApplicationCode = @ApplicationCode";
                sqlCommand = new SqlCommand(sql, connection);
                sqlCommand.CommandType = CommandType.Text;
                sqlCommand.Parameters.Add("@ApplicationCode", SqlDbType.NChar);
                sqlCommand.Parameters["@ApplicationCode"].Value = _GetApplication.ApplicationCode;
                SqlDataAdapter dtAdapter = new SqlDataAdapter();
                dtAdapter.SelectCommand = sqlCommand;
                dtAdapter.Fill(dt);
                connection.Close();
                if (dt.Rows.Count > 0)
                {
                    Log.Debug(JsonConvert.SerializeObject(dt));

                    _GetApplicationRespone.statusCode = "PASS";
                    _GetApplicationRespone.AccountNo = dt.Rows[0]["AccountNo"].ToString();
                    _GetApplicationRespone.ApplicationStatusID = dt.Rows[0]["ApplicationStatusID"].ToString();
                    _GetApplicationRespone.ApplicationCode = dt.Rows[0]["ApplicationCode"].ToString();
                    _GetApplicationRespone.ApplicationID = dt.Rows[0]["ApplicationID"].ToString();

                    _GetApplicationRespone.ProductSerialNo = dt.Rows[0]["ProductSerialNo"].ToString();

                    _GetApplicationRespone.SaleDepName = dt.Rows[0]["SaleDepName"].ToString();
                    _GetApplicationRespone.ProductModelName = dt.Rows[0]["ProductModelName"].ToString();

                    _GetApplicationRespone.ProductBrandName = dt.Rows[0]["ProductBrandName"].ToString();
                    

                    _GetApplicationRespone.CustomerID = dt.Rows[0]["CustomerID"].ToString();
                    _GetApplicationRespone.Cusname = dt.Rows[0]["Cusname"].ToString();
                    _GetApplicationRespone.cusMobile = dt.Rows[0]["cusMobile"].ToString();
                    _GetApplicationRespone.SaleName = dt.Rows[0]["SaleName"].ToString();
                    _GetApplicationRespone.SaleTelephoneNo = dt.Rows[0]["SaleTelephoneNo"].ToString();
                    
                }
                else
                {
                    _GetApplicationRespone.statusCode = "Not Found";
                }

                Log.Debug("RETURN : " + JsonConvert.SerializeObject(_GetApplicationRespone));

                sqlCommand.Parameters.Clear();
                
                return _GetApplicationRespone;
            }
            catch (Exception ex)
            {
                _GetApplicationRespone.statusCode = "FAIL";
                Log.Debug("RETURN : " + ex.Message);
                return _GetApplicationRespone;
            }

        }

        [HttpPost]
        public async Task<MessageReturn> GetStatusClosedSGFinance([FromBody] C100StatusRq _C100StatusRq)
        {
            Log.Debug(JsonConvert.SerializeObject(_C100StatusRq));
            string ResultDescription = "";
            MessageReturn _MessageReturn = new MessageReturn();
            try
            {

                SqlConnection connection = new SqlConnection();
                connection.ConnectionString = strConnString;
                connection.Open();
                SqlCommand sqlCommand;
                string strSQL = DATABASEK2 + ".[dbo].[GetStatusClosedSGFinance]";
                sqlCommand = new SqlCommand(strSQL, connection);
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.Parameters.AddWithValue("ApplicationCode", _C100StatusRq.ApplicationCode);

                SqlDataAdapter dtAdapter = new SqlDataAdapter();
                dtAdapter.SelectCommand = sqlCommand;
                DataTable dt = new DataTable();
                dtAdapter.Fill(dt);
                connection.Close();
                sqlCommand.Parameters.Clear();

                if (dt.Rows.Count > 0)
                {
                    Log.Debug(JsonConvert.SerializeObject(dt));

                    requestBodyValue _requestBodyValue = JsonConvert.DeserializeObject<requestBodyValue>(dt.Rows[0]["StatusDesc"].ToString());

                    var requestBody = new
                    {
                        applicationCode = _requestBodyValue.applicationCode,
                        applicationStatus = _requestBodyValue.applicationStatus,
                        approvalStatus = _requestBodyValue.approvalStatus,
                        approvalDatetime = _requestBodyValue.approvalDatetime,
                        remark = _requestBodyValue.remark
                    };

                    Log.Debug("API BODY REQUEST : " + JsonConvert.SerializeObject(requestBody));

                    using (HttpClient client = new HttpClient())
                    {
                        string jsonBody = JsonConvert.SerializeObject(requestBody);

                        client.DefaultRequestHeaders.Add("apikey", ApiKey);
                        client.DefaultRequestHeaders.Add("user", "DEV");

                        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                        HttpResponseMessage responseDevice = await client.PostAsync(SGAPIESIG+ "/Service/C100_Status", content);
                        int DeviceStatusCode = (int)responseDevice.StatusCode;

                        Log.Debug("API BODY RESPONE : " + JsonConvert.SerializeObject(responseDevice.Content.ReadAsStringAsync()));

                        if (responseDevice.IsSuccessStatusCode)
                        {
                            var jsonResponseDevice = await responseDevice.Content.ReadAsStringAsync();

                            _MessageReturn = JsonConvert.DeserializeObject<MessageReturn>(jsonResponseDevice);
                        }
                    }
                }

                Log.Debug("RETURN : " + JsonConvert.SerializeObject(_MessageReturn));
                return _MessageReturn;
            }
            catch (Exception ex)
            {
                _MessageReturn.StatusCode = "500";
                _MessageReturn.Message = ex.Message;

                Log.Debug("RETURN : " + JsonConvert.SerializeObject(_MessageReturn));

                return _MessageReturn;
            }
        }

        [HttpPost]
        public async Task<MessageReturn> GenEsignature([FromBody] C100StatusRq _C100StatusRq)
        {
            Log.Debug(JsonConvert.SerializeObject(_C100StatusRq));
            MessageReturn _MessageReturn = new MessageReturn();
            try
            {

                var requestBody = new
                {
                    APPLICATION_CODE = _C100StatusRq.ApplicationCode
                };

                using (HttpClient client = new HttpClient())
                {
                    string jsonBody = JsonConvert.SerializeObject(requestBody);

                    client.DefaultRequestHeaders.Add("apikey", ApiKey);
                    client.DefaultRequestHeaders.Add("user", "DEV");

                    var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                    HttpResponseMessage responseDevice = await client.PostAsync(SGAPIESIG + "/api/v2/GenEsignature", content);
                    int DeviceStatusCode = (int)responseDevice.StatusCode;

                    Log.Debug("API RESPONE : " + JsonConvert.SerializeObject(responseDevice.Content.ReadAsStringAsync()));

                    if (responseDevice.IsSuccessStatusCode)
                    {
                        var jsonResponseDevice = await responseDevice.Content.ReadAsStringAsync();

                        _MessageReturn = JsonConvert.DeserializeObject<MessageReturn>(jsonResponseDevice);
                    }
                }

                Log.Debug("RETURN : " + JsonConvert.SerializeObject(_MessageReturn));
                return _MessageReturn;
            }
            catch (Exception ex)
            {
                _MessageReturn.StatusCode = "500";
                _MessageReturn.Message = ex.Message;
                Log.Debug("RETURN : " + JsonConvert.SerializeObject(_MessageReturn));
                return _MessageReturn;
            }
        }

        [HttpPost]
        public async Task<MessageReturn> GetAddTNewSalesNewSGFinance([FromBody] C100StatusRq _C100StatusRq)
        {
            Log.Debug(JsonConvert.SerializeObject(_C100StatusRq));
            MessageReturn _MessageReturn = new MessageReturn();
            GetOuCodeRespone _GetOuCodeRespone = new GetOuCodeRespone();
            try
            {

                    var requestBody = new
                    {
                        APPLICATION_CODE = _C100StatusRq.ApplicationCode
                    };

                    Log.Debug("API REQUEST : " + JsonConvert.SerializeObject(requestBody));


                    using (HttpClient client = new HttpClient())
                    {
                        string jsonBody = JsonConvert.SerializeObject(requestBody);

                        client.DefaultRequestHeaders.Add("apikey", ApiKey);
                        client.DefaultRequestHeaders.Add("user", "DEV");

                        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                        HttpResponseMessage responseDevice;

                        responseDevice = await client.PostAsync(SGAPIESIG + "/SubmitSale", content);


                        int DeviceStatusCode = (int)responseDevice.StatusCode;

                        Log.Debug("API RESPONE : " + JsonConvert.SerializeObject(responseDevice.Content.ReadAsStringAsync()));

                        if (responseDevice.IsSuccessStatusCode)
                        {
                            var jsonResponseDevice = await responseDevice.Content.ReadAsStringAsync();

                            _MessageReturn = JsonConvert.DeserializeObject<MessageReturn>(jsonResponseDevice);
                        }
                    }
                //}
                Log.Debug("RETURN : " + JsonConvert.SerializeObject(_MessageReturn));
                return _MessageReturn;
            }
            catch (Exception ex)
            {
                _MessageReturn.StatusCode = "500";
                _MessageReturn.Message = ex.Message;
                Log.Debug("RETURN : " + JsonConvert.SerializeObject(_MessageReturn));
                return _MessageReturn;
            }
        }

        public async Task<RegisIMEIRespone> RegisIMEI([FromBody] GetApplication _GetApplication)
        {
            RegisIMEIRespone _RegisIMEIRespone = new RegisIMEIRespone();
            try
            {
                GetApplicationRespone _GetApplicationRespone = await GetApplication(_GetApplication);

                var requestBody = new
                {
                    SerrialNo = _GetApplicationRespone.ProductSerialNo,
                    APPLICATION_CODE = _GetApplicationRespone.ApplicationCode,
                    Brand = _GetApplicationRespone.ProductBrandName
                };

                using (HttpClient client = new HttpClient())
                {
                    string jsonBody = JsonConvert.SerializeObject(requestBody);

                    client.DefaultRequestHeaders.Add("apikey", ApiKey);
                    client.DefaultRequestHeaders.Add("user", "DEV");

                    var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                    HttpResponseMessage responseDevice = await client.PostAsync(SGAPIESIG + "/Service/RegisIMEI", content);
                    int DeviceStatusCode = (int)responseDevice.StatusCode;
                    Log.Debug("API RETURN : " + JsonConvert.SerializeObject(responseDevice.Content.ReadAsStringAsync()));
                    if (responseDevice.IsSuccessStatusCode)
                    {
                        var jsonResponseDevice = await responseDevice.Content.ReadAsStringAsync();

                        _RegisIMEIRespone = JsonConvert.DeserializeObject<RegisIMEIRespone>(jsonResponseDevice);
                    }
                }

                Log.Debug("RETURN : " + JsonConvert.SerializeObject(_RegisIMEIRespone));
                return _RegisIMEIRespone;
            }
            catch (Exception ex)
            {
                _RegisIMEIRespone.statusCode = ex.Message;
                Log.Debug("RETURN : " + JsonConvert.SerializeObject(_RegisIMEIRespone));
                return _RegisIMEIRespone;
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
