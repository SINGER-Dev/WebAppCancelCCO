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
using Dapper;
using Microsoft.Identity.Client;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using AspNetCoreGeneratedDocument;
using App.Model;

namespace App.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        string strConnString, DATABASEK2, WSCANCEL, UrlEztax, UsernameEztax, PasswordEztax, ClientIdEztax, ApiKey, SGAPIESIG, SGDIRECT, SGCESIGNATURE, SGCROSSBANK, C100 , C100Apikey, SGBCancelApikey, SGBCancelApi;
        private static readonly HttpClient client = new HttpClient();
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

            SGDIRECT = builder.GetConnectionString("SGDIRECT");
            SGCESIGNATURE = builder.GetConnectionString("SGCESIGNATURE");
            SGCROSSBANK = builder.GetConnectionString("SGCROSSBANK");
            C100 = builder.GetConnectionString("C100");
            C100Apikey = builder.GetConnectionString("C100Apikey");
            

            SGBCancelApi = builder.GetConnectionString("SGBCancelApi");
            SGBCancelApikey = builder.GetConnectionString("SGBCancelApikey");
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

        public IActionResult BypassCustomer()
        {
            var EMP_CODE = HttpContext.Session.GetString("EMP_CODE");
            if (EMP_CODE == null)
            {
                return Redirect("/Login");
            }
            var RoleDescription = HttpContext.Session.GetString("RoleDescription");

            bool containsBypass = RoleDescription.Contains("BypassCustomer");

            if (containsBypass == false)
            {
                return Redirect("/Home/UnderConstruction");
            }

            ViewBag.EMP_CODE = HttpContext.Session.GetString("EMP_CODE");
            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            return View();
        }

        public IActionResult BypassIMEI()
        {
            var EMP_CODE = HttpContext.Session.GetString("EMP_CODE");
            if (EMP_CODE == null)
            {
                return Redirect("/Login");
            }

            var RoleDescription = HttpContext.Session.GetString("RoleDescription");

            bool containsBypass = RoleDescription.Contains("BypassIMEI");

            if (containsBypass == false)
            {
                return Redirect("/Home/UnderConstruction");
            }

            ViewBag.EMP_CODE = HttpContext.Session.GetString("EMP_CODE");
            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            return View();
        }

        public IActionResult ChangeIMEI()
        {
            var EMP_CODE = HttpContext.Session.GetString("EMP_CODE");
            if (EMP_CODE == null)
            {
                return Redirect("/Login");
            }

            var RoleDescription = HttpContext.Session.GetString("RoleDescription");

            bool containsBypass = RoleDescription.Contains("ChangeIMEI");

            if (containsBypass == false)
            {
                return Redirect("/Home/UnderConstruction");
            }

            ViewBag.EMP_CODE = HttpContext.Session.GetString("EMP_CODE");
            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            return View();
        }

        public IActionResult UnderConstruction()
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
            formCancelModel.ApplicationCode = ApplicationCode;
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
            var EMP_CODE = HttpContext.Session.GetString("EMP_CODE");
            if (EMP_CODE == null)
            {
                return Redirect("/Login");
            }

            Log.Debug(JsonConvert.SerializeObject(_SearchGetApplicationHistory));

            List<SearchGetApplicationHistoryRespone> _SearchGetApplicationHistoryResponeMaster = new List<SearchGetApplicationHistoryRespone>();
            SqlConnection connection = new SqlConnection();
            connection.ConnectionString = strConnString;
            try
            {
                SqlCommand sqlCommand;
                string strSQL = DATABASEK2 + ".[GetApplicationHistory]";
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

            try
            {


                using (var connection = new SqlConnection(strConnString))
                {
                    connection.Open();

                    var sql = @$"

DECLARE 
    @TodayStart DATETIME = CAST(@startDate AS DATE),
    @TomorrowStart DATETIME = DATEADD(DAY, 1, CAST(@endDate AS DATE));


                    -- STEP 0: สร้าง Temp สำหรับ contracts
SELECT signedStatus, statusReceived, documentno
INTO #CONTRACTS_TEMP
FROM {SGCESIGNATURE}.[contracts] WITH (NOLOCK)
WHERE createdAt >= @TodayStart AND createdAt < @TomorrowStart;

-- STEP 1: ข้อมูล Serial
SELECT 
    s.AppOrderNo,
    SerialList = STUFF((
        SELECT ', ' + s2.ItemSerial
        FROM {SGDIRECT}.[AUTO_SALE_POS_SERIAL] s2 WITH (NOLOCK)
        WHERE s2.AppOrderNo = s.AppOrderNo
        FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '')
INTO #SERIAL_TEMP
FROM {SGDIRECT}.[AUTO_SALE_POS_SERIAL] s WITH (NOLOCK)
GROUP BY s.AppOrderNo;

-- STEP 2: เอกสารซ้ำใน CONTRACTS
SELECT COUNT(documentno) AS numdoc, documentno
INTO #CHECK_CONTRACT
FROM #CONTRACTS_TEMP WITH (NOLOCK)
GROUP BY documentno;

-- STEP 3: ARM_T_NEWSALES
SELECT COUNT(ARM_ACC_NO) AS newnum, ARM_ACC_NO, arm_Loaded_flag
INTO #NEWSALES_TEMP
FROM {DATABASEK2}.[ARM_T_NEWSALES] WITH (NOLOCK)
WHERE CREATED_USER = 'SG Finance'
  AND (@TodayStart IS NULL OR CREATED_DATE >= @TodayStart)
  AND (@TomorrowStart IS NULL OR CREATED_DATE <= @TomorrowStart)
GROUP BY ARM_ACC_NO, arm_Loaded_flag;

-- STEP 4: ARM_T_PAYMENT
SELECT COUNT(ARM_ACC_NO) AS paynum, ARM_ACC_NO, ARM_RECEIPT_STAT
INTO #PAYMENT_TEMP
FROM {DATABASEK2}.[ARM_T_PAYMENT] WITH (NOLOCK)
WHERE CREATED_USER = 'SG Finance'
  AND (@TodayStart IS NULL OR CREATED_DATE >= @TodayStart)
  AND (@TomorrowStart IS NULL OR CREATED_DATE <= @TomorrowStart)
GROUP BY ARM_ACC_NO, ARM_RECEIPT_STAT;

-- STEP 5: MAIN QUERY
SELECT 
    a.ApplicationID,
    a.ApplicationCode,
    a.AccountNo,
    a.SaleDepCode,
    a.SaleDepName,
    CONVERT(NVARCHAR, a.ApplicationDate, 20) AS ApplicationDate,
    CONVERT(NVARCHAR, a.ApplicationDate, 20) AS ApplicationDate2,
    a.ProductID,
    a.ProductModelName,
    a.CustomerID,
    cus.FirstName + ' ' + cus.LastName AS Cusname,
    cus.MobileNo1 AS CusMobile,
    a.SaleName,
    a.SaleTelephoneNo,
    ISNULL(ISNULL(serial.SerialList, a.ProductSerialNo), '') AS ProductSerialNo,
    a.ApplicationStatusID,
    CASE 
        WHEN con.signedStatus = 'COMP-Done' THEN N'เรียบร้อย' 
        WHEN con.signedStatus = 'Initial' THEN N'รอลงนาม'
        WHEN ISNULL(con.signedStatus, 'NULL') = 'NULL' THEN N'-'
        ELSE con.signedStatus 
    END AS signedStatus,
    CASE WHEN ISNULL(con.statusReceived, '0') = '1' THEN N'รับสินค้าแล้ว' ELSE N'ยังไม่รับสินค้า' END AS StatusReceived,
    a.ApprovedDate,
    CASE 
        WHEN appex.loanTypeCate = 'HP' THEN N'เรียบร้อย' 
        WHEN ISNULL(regis.IMEI, '') <> '' THEN N'เรียบร้อย' 
        ELSE N'รอลงทะเบียน' 
    END AS numregis,
    CASE WHEN checkcon.numdoc > 1 THEN N'พบรายการซ้ำ' ELSE N'ปกติ' END AS NumDoc,
    CASE 
        WHEN new.newnum = 1 AND new.arm_Loaded_flag IN (0, 1) THEN N'เรียบร้อย' 
        WHEN new.newnum = 1 AND new.arm_Loaded_flag = 2 THEN N'CANCELLED'
        WHEN new.newnum > 1 THEN N'รายการซ้ำ'
        ELSE N'ไม่พบรายการ' 
    END AS NewNum,
    CASE 
        WHEN pay.paynum = 1 AND pay.ARM_RECEIPT_STAT = 'APPROVED' THEN N'เรียบร้อย' 
        WHEN pay.paynum = 1 AND pay.ARM_RECEIPT_STAT = 'CANCELLED' THEN N'CANCELLED'
        WHEN pay.paynum > 1 THEN N'รายการซ้ำ'
        ELSE N'ไม่พบรายการ' 
    END AS PayNum,
    '' AS LINE_STATUS,
    '' AS TRANSFER_DATE,
    appex.RefCode,
    ISNULL(LEFT(appex.OU_Code, 3), '') AS OU_Code,
    appex.loanTypeCate,
    'DUMMY' AS Ref4,
    '' AS appIns,
    ISNULL(regis.Status, 'NULL') AS Status
FROM {DATABASEK2}.[Application] a WITH (NOLOCK)
INNER JOIN {DATABASEK2}.[ApplicationExtend] appex WITH (NOLOCK) ON appex.ApplicationID = a.ApplicationID
LEFT JOIN {DATABASEK2}.[Customer] cus WITH (NOLOCK) ON cus.CustomerID = a.CustomerID
LEFT JOIN {DATABASEK2}.[ApplicationRegisIMIE] regis WITH (NOLOCK)
    ON regis.IMEI = a.ProductSerialNo AND regis.Status IN ('REGISTER DEVICE SUCCESS', 'ALREADY REGISTERED')
LEFT JOIN #CONTRACTS_TEMP con ON a.ApplicationCode = con.documentno
LEFT JOIN #CHECK_CONTRACT checkcon ON checkcon.documentno = a.ApplicationCode
LEFT JOIN #NEWSALES_TEMP new ON new.ARM_ACC_NO = a.AccountNo
LEFT JOIN #PAYMENT_TEMP pay ON pay.ARM_ACC_NO = a.AccountNo
LEFT JOIN #SERIAL_TEMP serial ON serial.AppOrderNo = a.ApplicationCode
   WHERE (CONVERT(date,a.ApplicationDate,23) >= CONVERT(date,@TodayStart,23) OR ISNULL(@TodayStart,'') = '')
                      AND (CONVERT(date,a.ApplicationDate,23) <= CONVERT(date,@TomorrowStart,23) OR ISNULL(@TomorrowStart,'') = '')
                      AND (@status IS NULL OR a.ApplicationStatusID = @status)
                      AND (@loanTypeCate IS NULL OR appex.loanTypeCate = @loanTypeCate)
                      AND a.ApplicationDate >= '2024-05-01'
                      AND (@AccountNo IS NULL OR a.AccountNo = @AccountNo)
                      AND (@ApplicationCode IS NULL OR a.ApplicationCode = @ApplicationCode OR appex.RefCode = @ApplicationCode)
                      AND (@ProductSerialNo IS NULL OR a.ProductSerialNo = @ProductSerialNo)
                      AND (@CustomerID IS NULL OR a.CustomerID = @CustomerID)
                      AND (@CustomerName IS NULL OR cus.FirstName + ' ' + cus.LastName LIKE '%' + @CustomerName + '%')
ORDER BY a.ApplicationDate DESC
OPTION (RECOMPILE);

-- STEP 6: ลบ Temp Table
DROP TABLE #CONTRACTS_TEMP;
DROP TABLE #SERIAL_TEMP;
DROP TABLE #CHECK_CONTRACT;
DROP TABLE #NEWSALES_TEMP;
DROP TABLE #PAYMENT_TEMP;
";

                    var parameters = new
                    {
                        startdate = _ApplicationModel.startdate,
                        enddate = _ApplicationModel.enddate,
                        status = _ApplicationModel.status,
                        loanTypeCate = _ApplicationModel.loanTypeCate,
                        AccountNo = _ApplicationModel.AccountNo,
                        ApplicationCode = _ApplicationModel.ApplicationCode,
                        ProductSerialNo = _ApplicationModel.ProductSerialNo,
                        CustomerID = _ApplicationModel.CustomerID,
                        CustomerName = _ApplicationModel.CustomerName
                    };

                    var commandDefinition = new CommandDefinition(sql, parameters, commandTimeout: 300); // Timeout in seconds


                    var applications = connection.Query<ApplicationResponeModel>(commandDefinition);

                    // กำหนดค่าที่ต้องการตรวจสอบ

                    string[] validStatuses;

                    if (_ApplicationModel.StatusRegis == "1")
                    {
                        validStatuses = new string[] { "REGISTER DEVICE SUCCESS", "ALREADY REGISTERED" };
                    }
                    else if (_ApplicationModel.StatusRegis == "0")
                    {
                        validStatuses = new string[] { "NULL" };
                    }
                    else
                    {
                        validStatuses = new string[] { "REGISTER DEVICE SUCCESS", "ALREADY REGISTERED", "NULL" };
                    }


                    foreach (var application in applications)
                    {
                        bool exists = _ApplicationResponeModelMaster.Any(m => m.ApplicationCode == application.ApplicationCode);
                        if (!exists && Array.Exists(validStatuses, element => element == application.Status.ToUpper()))
                        {
                            string datenowText = DateTime.Now.ToString("yyyy-MM-dd", new CultureInfo("en-US"));
                            if (application.ApplicationDate.ToString() == datenowText)
                            {
                                application.datenowcheck = "1";
                            }
                            else
                            {
                                application.datenowcheck = "0";
                            }
                            _ApplicationResponeModelMaster.Add(application);
                        }

                    }

                }

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

                //if (isWithinPeriod)
                //{


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

                using (SqlConnection connection = new SqlConnection(strConnString))
                {
                    await connection.OpenAsync();

                    using (SqlCommand sqlCommand = new SqlCommand(DATABASEK2 + ".[CancelApplication]", connection))
                    {
                        sqlCommand.CommandType = CommandType.StoredProcedure;
                        sqlCommand.CommandTimeout = 120; // Set timeout to 120 seconds
                        sqlCommand.Parameters.AddWithValue("ApplicationCode", _FormConfirmModel.ApplicationCode);
                        sqlCommand.Parameters.AddWithValue("Remark", _FormConfirmModel.Remark + " " + _FormConfirmModel.Other);
                        sqlCommand.Parameters.AddWithValue("CANCEL_USER", HttpContext.Session.GetString("EMP_CODE"));
                        sqlCommand.Parameters.AddWithValue("Except_IMEI", _FormConfirmModel.ExceptIMEI);
                        sqlCommand.Parameters.AddWithValue("Except_CUST", _FormConfirmModel.ExceptCus);

                        using (SqlDataAdapter dtAdapter = new SqlDataAdapter(sqlCommand))
                        {
                            DataTable dt = new DataTable();
                            dtAdapter.Fill(dt);

                            if (dt.Rows.Count > 0)
                            {
                                if ("SUCCESS" != dt.Rows[0]["Result"].ToString().ToUpper())
                                {
                                    ResultDescription += _GetApplicationRespone.AccountNo + " " + dt.Rows[0]["ResultDescription"].ToString();
                                }
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(ResultDescription))
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
                        HttpResponseMessage responseDevice = await client.PostAsync(SGAPIESIG + "/sgesig/Service/C100_Status", content);
                        int DeviceStatusCode = (int)responseDevice.StatusCode;

                        Log.Debug("API BODY RESPONE : " + JsonConvert.SerializeObject(responseDevice.Content.ReadAsStringAsync()));

                        if (responseDevice.IsSuccessStatusCode)
                        {
                            var jsonResponseDevice = await responseDevice.Content.ReadAsStringAsync();

                        }
                    }
                }
                //}
                //else
                //{
                //    ResultDescription = "ไม่สามารถยกเลิกรายการได้ เนื่องจากเลยกำหนดเวลาการยกเลิกแล้ว";
                //}
                return ResultDescription;
            }
            catch (Exception ex)
            {
                ResultDescription = ex.Message;
                return ResultDescription;
            }


        }

        [HttpPost]
        public async Task<string> UpdateDataCancelCLOSED(FormConfirmModel _FormConfirmModel)
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

                //if (isWithinPeriod)
                //{
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
                string strSQL = DATABASEK2 + ".[CancelApplication_CLOSED]";
                sqlCommand = new SqlCommand(strSQL, connection);
                sqlCommand.CommandTimeout = 180;
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.Parameters.AddWithValue("ApplicationCode", _GetApplicationRespone.ApplicationCode);
                sqlCommand.Parameters.AddWithValue("Remark", _FormConfirmModel.Remark + "" + _FormConfirmModel.Other);
                sqlCommand.Parameters.AddWithValue("CANCEL_USER", HttpContext.Session.GetString("EMP_CODE"));

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
                    //ถ้าเป็น SGB
                    if (!string.IsNullOrEmpty(_GetApplicationRespone.appIns))
                    {
                        SGBCancelRespone sGBCancelRespone = new SGBCancelRespone();
                        sGBCancelRespone = await SGBCancel(_GetApplication);
                    }

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
                        HttpResponseMessage responseDevice = await client.PostAsync(SGAPIESIG + "/sgesig/Service/C100_Status", content);
                        int DeviceStatusCode = (int)responseDevice.StatusCode;

                        Log.Debug("API BODY RESPONE : " + JsonConvert.SerializeObject(responseDevice.Content.ReadAsStringAsync()));

                        if (responseDevice.IsSuccessStatusCode)
                        {
                            var jsonResponseDevice = await responseDevice.Content.ReadAsStringAsync();

                        }
                    }
                }
                //}
                //else
                //{
                //    ResultDescription = "ไม่สามารถยกเลิกรายการได้ เนื่องจากเลยกำหนดเวลาการยกเลิกแล้ว";
                //}
                return ResultDescription;
            }
            catch (Exception ex)
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
                var client = new RestClient(UrlEztax + "/api/auth");
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

                string sql = @$"SELECT 
                                    app.ApplicationID,
                                    app.AccountNo,
                                    app.ApplicationStatusID,
                                    app.CustomerID,
                                    app.ApplicationCode,
                                    app.ProductID,
                                    cus.FirstName + ' ' + cus.LastName AS Cusname,
                                    cus.MobileNo1 AS cusMobile,
                                    app.SaleName,
                                    app.SaleTelephoneNo,
                                    app.ProductModelName,
                                    app.ProductSerialNo,
                                    app.ProductBrandName,
                                    app.SaleDepCode,
                                    app.SaleDepName,
                                    appex.RefCode,
                                    app.Cash,
                                    app.DownPayment,
                                    app.ApplicationDate,
                                    appex.InterestPercent,
                                    app.InstallmentPeriod,
                                    app.Discount,
                                    ISNULL(appex.[ApplicationRef],'') AS ApplicationRef
                                FROM
                                    {DATABASEK2}.[Application] app WITH (NOLOCK)
                                LEFT JOIN
                                    {DATABASEK2}.[Customer] cus WITH (NOLOCK)
                                ON
                                    cus.CustomerID = app.CustomerID
                                LEFT JOIN
                                    {DATABASEK2}.[ApplicationExtend] appex WITH (NOLOCK)
                                ON
                                    appex.ApplicationID = app.ApplicationID
                                WHERE
                                    app.ApplicationCode = @ApplicationCode OR appex.RefCode = @ApplicationCode; ";
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
                    _GetApplicationRespone.RefCode = dt.Rows[0]["RefCode"].ToString();


                    _GetApplicationRespone.Cash = dt.Rows[0]["Cash"].ToString();
                    _GetApplicationRespone.DownPayment = dt.Rows[0]["DownPayment"].ToString();
                    _GetApplicationRespone.ApplicationDate = dt.Rows[0]["ApplicationDate"].ToString();
                    _GetApplicationRespone.InterestPercent = dt.Rows[0]["InterestPercent"].ToString();
                    _GetApplicationRespone.InstallmentPeriod = dt.Rows[0]["InstallmentPeriod"].ToString();
                    _GetApplicationRespone.Discount = dt.Rows[0]["Discount"].ToString();
                    _GetApplicationRespone.ApplicationRef = dt.Rows[0]["ApplicationRef"].ToString();

                    
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
            Log.Debug("GetStatusClosedSGFinance By " + HttpContext.Session.GetString("EMP_CODE") + " | " + HttpContext.Session.GetString("FullName") + " : " + JsonConvert.SerializeObject(_C100StatusRq));
            string ResultDescription = "";
            MessageReturn _MessageReturn = new MessageReturn();
            try
            {

                SqlConnection connection = new SqlConnection();
                connection.ConnectionString = strConnString;
                connection.Open();
                SqlCommand sqlCommand;
                string strSQL = DATABASEK2 + ".[GetStatusClosedSGFinance]";
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
                        remark = "",
                        losApplicationCode = _requestBodyValue.applicationCode,
                        contractNo = dt.Rows[0]["Accountno"].ToString()
                    };

                    Log.Debug("API BODY REQUEST : " + JsonConvert.SerializeObject(requestBody));

                    using (HttpClient client = new HttpClient())
                    {
                        string jsonBody = JsonConvert.SerializeObject(requestBody);

                        client.DefaultRequestHeaders.Add("Apikey", C100Apikey);

                        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                        HttpResponseMessage responseDevice = await client.PostAsync(C100 + "/los/v2/SgFinance/C100_Status", content);
                        int DeviceStatusCode = (int)responseDevice.StatusCode;

                        Log.Debug("API BODY RESPONE : " + JsonConvert.SerializeObject(responseDevice.Content.ReadAsStringAsync()));

                        if (responseDevice.IsSuccessStatusCode)
                        {
                            _MessageReturn.StatusCode = "200";
                            _MessageReturn.Message = "PASS";
                            
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
            Log.Debug("GenEsignature By " + HttpContext.Session.GetString("EMP_CODE") + " | " + HttpContext.Session.GetString("FullName") + " : " + JsonConvert.SerializeObject(_C100StatusRq));
            MessageReturn _MessageReturn = new MessageReturn();
            try
            {

                var requestBody = new
                {
                    applicationCode = _C100StatusRq.ApplicationCode
                };

                using (HttpClient client = new HttpClient())
                {
                    string jsonBody = JsonConvert.SerializeObject(requestBody);

                    //client.DefaultRequestHeaders.Add("apikey", ApiKey);
                    //client.DefaultRequestHeaders.Add("user", "DEV");

                    var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                    HttpResponseMessage responseDevice = await client.PostAsync("https://sg-posservice.singerthai.co.th:10082/v1/LOS/SGF_ReCreateESig", content);
                    //HttpResponseMessage responseDevice = await client.PostAsync(SGAPIESIG + "/sgesig/api/v2/GenEsignature", content);
                    int DeviceStatusCode = (int)responseDevice.StatusCode;

                    Log.Debug("API RESPONE : " + JsonConvert.SerializeObject(responseDevice.Content.ReadAsStringAsync()));

                    if (responseDevice.IsSuccessStatusCode)
                    {
                        var jsonResponseDevice = await responseDevice.Content.ReadAsStringAsync();

                        //_MessageReturn = JsonConvert.DeserializeObject<MessageReturn>(jsonResponseDevice);
                        _MessageReturn.StatusCode = "200";
                        _MessageReturn.Message = "Success.";
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

            Log.Debug("GetAddTNewSalesNewSGFinance By " + HttpContext.Session.GetString("EMP_CODE") + " | " + HttpContext.Session.GetString("FullName") + " : " + JsonConvert.SerializeObject(_C100StatusRq));
            MessageReturn _MessageReturn = new MessageReturn();
            GetOuCodeRespone _GetOuCodeRespone = new GetOuCodeRespone();
            try
            {
                SqlConnection connection = new SqlConnection();
                connection.ConnectionString = strConnString;
                connection.Open();
                SqlCommand sqlCommand;
                string strSQL = DATABASEK2 + ".[GetSendEsignatureStatusSGFinance]";
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

                    GetSendEsignatureStatusSGFinance _requestBodyValue = JsonConvert.DeserializeObject<GetSendEsignatureStatusSGFinance>(dt.Rows[0]["StatusDesc"].ToString());

                    
                    var requestBody = new
                    {
                        ApplicationCode = _requestBodyValue.ApplicationCode,
                        EsignatureConfirmStatus = _requestBodyValue.ApplicationCode,
                        EsignatureConfirmDate = _requestBodyValue.ApplicationCode,
                        ReceiveConfirmStatus = _requestBodyValue.ApplicationCode,
                        ReceiveConfirmDate = _requestBodyValue.ApplicationCode
                    };

                    Log.Debug("API REQUEST : " + JsonConvert.SerializeObject(requestBody));


                    using (HttpClient client = new HttpClient())
                    {
                        string jsonBody = JsonConvert.SerializeObject(requestBody);

                        client.DefaultRequestHeaders.Add("apikey", ApiKey);
                        client.DefaultRequestHeaders.Add("user", "DEV");

                        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                        HttpResponseMessage responseDevice;

                        responseDevice = await client.PostAsync(SGAPIESIG + "/sgesig/api/v1/SendEsignatureStatus", content);


                        int DeviceStatusCode = (int)responseDevice.StatusCode;

                        Log.Debug("API RESPONE : " + JsonConvert.SerializeObject(responseDevice.Content.ReadAsStringAsync()));

                        if (responseDevice.IsSuccessStatusCode)
                        {
                            var jsonResponseDevice = await responseDevice.Content.ReadAsStringAsync();

                            _MessageReturn = JsonConvert.DeserializeObject<MessageReturn>(jsonResponseDevice);
                        }
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
            Log.Debug("RegisIMEI By " + HttpContext.Session.GetString("EMP_CODE") + " | " + HttpContext.Session.GetString("FullName") + " : " + JsonConvert.SerializeObject(_GetApplication));
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
                    HttpResponseMessage responseDevice = await client.PostAsync(SGAPIESIG + "/sgesig/Service/RegisIMEI", content);
                    int DeviceStatusCode = (int)responseDevice.StatusCode;
                    Log.Debug("API RETURN : " + JsonConvert.SerializeObject(responseDevice.Content.ReadAsStringAsync()));
                    if (responseDevice.IsSuccessStatusCode)
                    {

                        if (_GetApplicationRespone.ProductBrandName.Trim().ToUpper() == "OPPO")
                        {
                            float new_loan = float.Parse(_GetApplicationRespone.Cash) - float.Parse(_GetApplicationRespone.DownPayment);
                            LendingInfoRq lendingInfoRq = new LendingInfoRq();
                            lendingInfoRq.ApplicationCode = _GetApplication.ApplicationCode;
                            lendingInfoRq.application_date = _GetApplicationRespone.ApplicationDate;
                            lendingInfoRq.product_serial = _GetApplicationRespone.ProductSerialNo;
                            lendingInfoRq.flat_rate = _GetApplicationRespone.InterestPercent;
                            lendingInfoRq.cash_price = _GetApplicationRespone.Cash;
                            lendingInfoRq.down_payment = ""; //%ดาวน์
                            lendingInfoRq.down_amount = _GetApplicationRespone.DownPayment.ToString();
                            lendingInfoRq.new_loan = new_loan.ToString();
                            lendingInfoRq.contract_term = _GetApplicationRespone.InstallmentPeriod.ToString();
                            lendingInfoRq.discount = _GetApplicationRespone.Discount;
                            await LendingInfo(lendingInfoRq);
                        }

                        var jsonResponseDevice = await responseDevice.Content.ReadAsStringAsync();

                        _RegisIMEIRespone = JsonConvert.DeserializeObject<RegisIMEIRespone>(jsonResponseDevice);

                        if (_GetApplicationRespone.ApplicationRef.Trim() == "SEAMLESS")
                        {
                            CheckRegisterIMEIRq checkRegisterIMEIRq = new CheckRegisterIMEIRq();
                            checkRegisterIMEIRq.AppOrderNo = _GetApplication.ApplicationCode;
                            CheckRegisterIMEI(checkRegisterIMEIRq);
                        }

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

        [HttpPost]
        [Route("ReceivedStatus")]
        public async Task ReceivedStatus([FromBody] ReceivedStatusRq _ReceivedStatus)
        {
            try
            {

                using (HttpClient client = new HttpClient())
                {
                    string jsonBody = JsonConvert.SerializeObject(_ReceivedStatus);

                    client.DefaultRequestHeaders.Add("apikey", ApiKey);
                    client.DefaultRequestHeaders.Add("user", "DEV");

                    var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                    HttpResponseMessage responseDevice = await client.PostAsync(SGAPIESIG + "/sgesig/Service/LendingInfo", content);
                    int DeviceStatusCode = (int)responseDevice.StatusCode;
                    Log.Debug("API RETURN : " + JsonConvert.SerializeObject(responseDevice.Content.ReadAsStringAsync()));
                }
            }
            catch (Exception ex)
            {
                Log.Debug("RETURN : " + JsonConvert.SerializeObject(ex.Message));
            }

        }

        [HttpPost]
        [Route("CheckRegisterIMEI")]
        public async void CheckRegisterIMEI(CheckRegisterIMEIRq checkRegisterIMEIRq)
        {
            GetApplicationRespone _GetApplicationRespone = new GetApplicationRespone();
            DataTable dt = new DataTable();
            try
            {
                Log.Debug(JsonConvert.SerializeObject(checkRegisterIMEIRq));

                SqlConnection connection = new SqlConnection();
                connection.ConnectionString = strConnString;
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls11;
                connection.Open();
                SqlCommand sqlCommand;


                string sql = @$"
                            SELECT 
                                AE.RefCode, 
                                ISNULL(H.InvoiceNo, '') AS InvoiceNo, 
                                SR.ItemSerial, 
                                ISNULL(RS.Status, '') AS RegisterIMEI
                            FROM {SGDIRECT}.[AUTO_SALE_POS_HEADER] H WITH (NOLOCK)
                            LEFT JOIN {DATABASEK2}.[Application] A WITH(NOLOCK) ON A.ApplicationCode = H.AppOrderNo
                            LEFT JOIN {DATABASEK2}.[ApplicationExtend] AE WITH(NOLOCK) ON AE.ApplicationId = A.ApplicationId
                            LEFT JOIN {SGDIRECT}.[AUTO_SALE_POS_SERIAL] SR WITH(NOLOCK) ON SR.AppOrderNo = H.AppOrderNo
                            LEFT JOIN {DATABASEK2}.[ApplicationRegisIMIE] RS WITH(NOLOCK) ON RS.ApplicationCode = H.AppOrderNo 
                            AND RS.IMEI = SR.ItemSerial 
                            AND (RS.Status = 'REGISTER DEVICE SUCCESS' OR RS.Status = 'ALREADY REGISTERED')
                            WHERE H.[AppOrderNo] = @AppOrderNo";

              
                sqlCommand = new SqlCommand(sql, connection);
                sqlCommand.CommandType = CommandType.Text;
                sqlCommand.Parameters.Add("@AppOrderNo", SqlDbType.NChar);
                sqlCommand.Parameters["@AppOrderNo"].Value = checkRegisterIMEIRq.AppOrderNo;
                SqlDataAdapter dtAdapter = new SqlDataAdapter();
                dtAdapter.SelectCommand = sqlCommand;
                dtAdapter.Fill(dt);
                connection.Close();

                CheckRegisterIMEIRp checkRegisterIMEIRp = new CheckRegisterIMEIRp();

                if (dt.Rows.Count > 0)
                {
                    Log.Debug(JsonConvert.SerializeObject(dt));

                    if (dt.Rows[0]["RegisterIMEI"].ToString() != "")
                    {
                        ReceivedStatusRq receivedStatusRq = new ReceivedStatusRq();
                        receivedStatusRq.applicationNo = checkRegisterIMEIRp.RefCode.ToString();
                        receivedStatusRq.type = "REGISTER";
                        receivedStatusRq.status = "Y";
                        receivedStatusRq.applicationNo = checkRegisterIMEIRp.ItemSerial.ToString();
                        ReceivedStatus(receivedStatusRq);
                    }


                }
                else
                {
                    _GetApplicationRespone.statusCode = "Not Found";
                }

                Log.Debug("RETURN : " + JsonConvert.SerializeObject(_GetApplicationRespone));

                sqlCommand.Parameters.Clear();
            }
            catch (Exception ex)
            {
                _GetApplicationRespone.statusCode = "FAIL";
                Log.Debug("RETURN : " + ex.Message);
            }
        }

        [HttpPost]
        [Route("LendingInfo")]
        public async Task LendingInfo([FromBody] LendingInfoRq _LendingInfoRq)
        {
            try
            {

                using (HttpClient client = new HttpClient())
                {
                    string jsonBody = JsonConvert.SerializeObject(_LendingInfoRq);

                    client.DefaultRequestHeaders.Add("apikey", ApiKey);
                    client.DefaultRequestHeaders.Add("user", "DEV");

                    var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                    HttpResponseMessage responseDevice = await client.PostAsync(SGAPIESIG + "/sgesig/Service/LendingInfo", content);
                    int DeviceStatusCode = (int)responseDevice.StatusCode;
                    Log.Debug("API RETURN : " + JsonConvert.SerializeObject(responseDevice.Content.ReadAsStringAsync()));
                }
            }
            catch (Exception ex)
            {
                Log.Debug("RETURN : " + JsonConvert.SerializeObject(ex.Message));
            }
           
        }

        [HttpPost]
        [Route("CancelledSGB")]
        public async Task CancelledSGB([FromBody] LendingInfoRq _LendingInfoRq)
        {
            try
            {

                using (HttpClient client = new HttpClient())
                {
                    string jsonBody = JsonConvert.SerializeObject(_LendingInfoRq);

                    client.DefaultRequestHeaders.Add("apikey", ApiKey);
                    client.DefaultRequestHeaders.Add("user", "DEV");

                    var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                    HttpResponseMessage responseDevice = await client.PostAsync(SGAPIESIG + "/sgesig/Service/LendingInfo", content);
                    int DeviceStatusCode = (int)responseDevice.StatusCode;
                    Log.Debug("API RETURN : " + JsonConvert.SerializeObject(responseDevice.Content.ReadAsStringAsync()));
                }
            }
            catch (Exception ex)
            {
                Log.Debug("RETURN : " + JsonConvert.SerializeObject(ex.Message));
            }

        }

        [HttpPost]
        [Route("SendEmail")]
        public async Task<SendEmailRespone> SendEmail([FromBody] SendEmailRq sendEmailRq)
        {
            Log.Debug("SendEmail By " + HttpContext.Session.GetString("EMP_CODE") + " | " + HttpContext.Session.GetString("FullName") + " : " + JsonConvert.SerializeObject(sendEmailRq));
            SendEmailRespone sendEmailRespone = new SendEmailRespone();
            try
            {

                GetApplication getApplication = new GetApplication();
                getApplication.ApplicationCode = sendEmailRq.ApplicationCode;
                GetApplicationRespone _GetApplicationRespone = await GetApplication(getApplication);

                SenEmailBody senEmailBody = new SenEmailBody();
                

                senEmailBody.fromName = "AutomailSystem@sgcapital.co.th";
                senEmailBody.fromEmail = "AutomailSystem@sgcapital.co.th";

                SenEmailTo senEmailTo = new SenEmailTo();
                senEmailTo.Name = "RattanapongT";
                senEmailTo.Email = "RattanapongT@singerthai.co.th";
                senEmailBody.to.Add(senEmailTo);

                SenEmailTo senEmailToCC1 = new SenEmailTo();
                senEmailToCC1.Name = "SiripornK";
                senEmailToCC1.Email = "SiripornK@singerthai.co.th";
                senEmailBody.cc.Add(senEmailToCC1);

                senEmailBody.subject = $@"[ระบบตรวจสอบใบคำขอ SG Finance+] มีการยกเลิกใบคำขอเลขที่ {_GetApplicationRespone.ApplicationCode}  บัญชีเลขที่ {_GetApplicationRespone.AccountNo}";
                senEmailBody.content = $@"
เรียนผู้เกี่ยวข้องทุกท่าน

มีการยกเลิกใบคำขอเลขที่ {_GetApplicationRespone.ApplicationCode} 
บัญชีเลขที่ {_GetApplicationRespone.AccountNo} โดย เทสระบบ 
เหตุผล {sendEmailRq.Remark} 
กรุณาดำเนินการในส่วนที่เกี่ยวข้องต่อไปด้วย

=========================================
ขอแสดงความนับถือ
=========================================
                    ";


                var requestBody = senEmailBody;

                using (HttpClient client = new HttpClient())
                {
                    string jsonBody = JsonConvert.SerializeObject(requestBody);

                    var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                    HttpResponseMessage responseDevice = await client.PostAsync("https://sg-posservice.singerthai.co.th:10082/v1/Mail/Mail", content);
                    int DeviceStatusCode = (int)responseDevice.StatusCode;
                    Log.Debug("API RETURN : " + JsonConvert.SerializeObject(responseDevice.Content.ReadAsStringAsync()));
                    if (responseDevice.IsSuccessStatusCode)
                    {
                        var jsonResponseDevice = await responseDevice.Content.ReadAsStringAsync();

                        sendEmailRespone = JsonConvert.DeserializeObject<SendEmailRespone>(jsonResponseDevice);
                    }
                }

                Log.Debug("RETURN : " + JsonConvert.SerializeObject(sendEmailRespone));
                return sendEmailRespone;
            }
            catch (Exception ex)
            {
                sendEmailRespone.statusCode = ex.Message;
                Log.Debug("RETURN : " + JsonConvert.SerializeObject(sendEmailRespone));
                return sendEmailRespone;
            }
        }
        
        public async Task<SGBCancelRespone> SGBCancel([FromBody] GetApplication _GetApplication)
        {
            Log.Debug("SGBCancel By " + HttpContext.Session.GetString("EMP_CODE") + " | " + HttpContext.Session.GetString("FullName") + " : " + JsonConvert.SerializeObject(_GetApplication));
            SGBCancelRespone sGBCancelRespone = new SGBCancelRespone();
            try
            {
                GetApplicationRespone _GetApplicationRespone = await GetApplication(_GetApplication);

                var requestBody = new
                {
                    applicationCode = _GetApplicationRespone.ProductSerialNo,
                    referenceNo = _GetApplicationRespone.RefCode
                };

                using (HttpClient client = new HttpClient())
                {
                    string jsonBody = JsonConvert.SerializeObject(requestBody);

                    client.DefaultRequestHeaders.Add("apikey", SGBCancelApikey);
                    client.DefaultRequestHeaders.Add("user", "DEV");

                    var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                    HttpResponseMessage responseDevice = await client.PostAsync(SGBCancelApi + "/sgbmobilecare/api/v1/policy/cancelpolicy", content);
                    int DeviceStatusCode = (int)responseDevice.StatusCode;
                    Log.Debug("API RETURN : " + JsonConvert.SerializeObject(responseDevice.Content.ReadAsStringAsync()));
                    if (responseDevice.IsSuccessStatusCode)
                    {
                        var jsonResponseDevice = await responseDevice.Content.ReadAsStringAsync();

                        sGBCancelRespone = JsonConvert.DeserializeObject<SGBCancelRespone>(jsonResponseDevice);
                    }
                }

                Log.Debug("RETURN : " + JsonConvert.SerializeObject(sGBCancelRespone));
                return sGBCancelRespone;
            }
            catch (Exception ex)
            {
                sGBCancelRespone.message = ex.Message;
                Log.Debug("RETURN : " + JsonConvert.SerializeObject(sGBCancelRespone));
                return sGBCancelRespone;
            }
        }

        public async Task<RegisIMEIRespone> LinkPayment([FromBody] GetApplication _GetApplication)
        {
            RegisIMEIRespone _RegisIMEIRespone = new RegisIMEIRespone();
            Log.Debug("SGBCancel By " + HttpContext.Session.GetString("EMP_CODE") + " | " + HttpContext.Session.GetString("FullName") + " : " + JsonConvert.SerializeObject(_GetApplication));
            var url = "http://sg-posservice.singerthai.co.th:8344/WebServiceGenLinkWithSms.asmx?op=GenLinkWithSms";

            var soapRequest = $@"<?xml version=""1.0"" encoding=""utf-8""?>
        <soap12:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap12=""http://www.w3.org/2003/05/soap-envelope"">
          <soap12:Body>
            <GenLinkWithSms xmlns=""http://tempuri.org/"">
              <AppCode>{_GetApplication.ApplicationCode}</AppCode>
            </GenLinkWithSms>
          </soap12:Body>
        </soap12:Envelope>";

            var content = new StringContent(soapRequest, Encoding.UTF8, "application/soap+xml");

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/soap+xml"));

            try
            {
                var response = await client.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
               
                _RegisIMEIRespone.statusCode = "PASS";
                Log.Debug(responseBody);
            }
            catch (Exception ex)
            {
                _RegisIMEIRespone.statusCode = ex.Message;
                Log.Error($"Error: {ex.Message}");
            }

            return _RegisIMEIRespone;
        }

        [HttpGet("CheckSession")]
        public IActionResult CheckSession()
        {
            if (HttpContext.Session.GetString("EMP_CODE") == null)
            {
                return Unauthorized();
            }

            return Ok();
        }

        public class FormData
        {
            public string ApplicationID { get; set; }
            public string Remark { get; set; }
            public string CANCEL_USER { get; set; }
        }

        [HttpPost]
        public ModelResult PostBypassCustomer(BypassCustomer _bypassCustomer)
        {
            var EMP_CODE = HttpContext.Session.GetString("EMP_CODE");

            _bypassCustomer.empCode = EMP_CODE;
            Log.Information(JsonConvert.SerializeObject(_bypassCustomer));
            ModelResult modelResult = new ModelResult();

            try
            {
                var jsonstring = JsonConvert.SerializeObject(_bypassCustomer);
                var client = new HttpClient();
                string apiUrl = "https://sg-posservice.singerthai.co.th:10082/v1/LOS/ByPassCustomer";
                var content = new StringContent(jsonstring, null, "application/json");
                HttpResponseMessage response = client.PostAsync(apiUrl, content).Result;
                if (response.IsSuccessStatusCode)
                {
                    string result = response.Content.ReadAsStringAsync().Result;

                    modelResult =  JsonConvert.DeserializeObject<ModelResult>(result);
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    string result = response.Content.ReadAsStringAsync().Result;

                    modelResult = JsonConvert.DeserializeObject<ModelResult>(result);
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
            return modelResult;

        }

        [HttpPost]
        public ModelResult PostBypassIMEI(BypassImei _bypassImei)
        {
            var EMP_CODE = HttpContext.Session.GetString("EMP_CODE");

            _bypassImei.empCode = EMP_CODE;
            Log.Information(JsonConvert.SerializeObject(_bypassImei));
            ModelResult modelResult = new ModelResult();

            try
            {
                var jsonstring = JsonConvert.SerializeObject(_bypassImei);
                var client = new HttpClient();
                string apiUrl = "https://sg-posservice.singerthai.co.th:10082/v1/LOS/ByPassIMEI";
                var content = new StringContent(jsonstring, null, "application/json");
                HttpResponseMessage response = client.PostAsync(apiUrl, content).Result;
                if (response.IsSuccessStatusCode)
                {
                    string result = response.Content.ReadAsStringAsync().Result;

                    modelResult = JsonConvert.DeserializeObject<ModelResult>(result);
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    string result = response.Content.ReadAsStringAsync().Result;

                    modelResult = JsonConvert.DeserializeObject<ModelResult>(result);
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
            return modelResult;

        }

        [HttpPost]
        public ModelResult PostChangeIMEI(ChangeImei _changeImei)
        {
            var EMP_CODE = HttpContext.Session.GetString("EMP_CODE");

            _changeImei.empCode = EMP_CODE;
            Log.Information(JsonConvert.SerializeObject(_changeImei));
            ModelResult modelResult = new ModelResult();

            try
            {
                var jsonstring = JsonConvert.SerializeObject(_changeImei);
                var client = new HttpClient();
                string apiUrl = "https://sg-posservice.singerthai.co.th:10082/v1/LOS/ChangeIMEI";
                var content = new StringContent(jsonstring, null, "application/json");
                HttpResponseMessage response = client.PostAsync(apiUrl, content).Result;
                if (response.IsSuccessStatusCode)
                {
                    string result = response.Content.ReadAsStringAsync().Result;

                    modelResult = JsonConvert.DeserializeObject<ModelResult>(result);
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    string result = response.Content.ReadAsStringAsync().Result;

                    modelResult = JsonConvert.DeserializeObject<ModelResult>(result);
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
            return modelResult;

        }
    }
}
