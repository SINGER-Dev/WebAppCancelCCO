using App.Clients;
using App.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using App.Filters;
using App.Model;
using App.Models;
using AspNetCoreGeneratedDocument;
using Azure;
using Azure.Core;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Data.SqlClient;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Serilog;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Xml;
using static System.Net.Mime.MediaTypeNames;

namespace App.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        string strConnString, DATABASEK2, WSCANCEL, UrlEztax, UsernameEztax, PasswordEztax, ClientIdEztax, ApiKey, SGAPIESIG, SGDIRECT, SGCESIGNATURE, SGCROSSBANK, C100 , C100Apikey, SGBCancelApikey, SGBCancelApi;
        private static readonly HttpClient client = new HttpClient();
        private readonly IDownstreamApi _api;
        private readonly IMemoryCache _cache;
        private readonly ApplicationIdBounds _idBounds;

        // อายุของผลค้นหาที่เก็บไว้ — ตั้งได้จาก config (Search:CacheSeconds), 0 = ปิด cache
        //
        // ตั้งไว้สั้นมากโดยตั้งใจ เพราะระบบนี้สถานะของแต่ละใบวิ่งเปลี่ยนตลอดจนกว่างานจะจบ
        // และคนเปลี่ยนสถานะส่วนใหญ่คือระบบอื่น (K2 / eSig / LMS / งานตามเวลา) ไม่ได้ผ่านแอปนี้
        // แอปจึงไม่มีทางรู้ว่าต้องล้าง cache เมื่อไร — จะพึ่งการล้างอย่างเดียวไม่ได้
        // cache ตรงนี้มีไว้ "รวบคำขอที่ถล่มเข้ามาพร้อมกัน" ไม่ได้มีไว้ลดการอ่านข้อมูลระยะยาว
        private static int _cacheSeconds = 10;

        /// <summary>
        /// คอลัมน์ที่เรียงลำดับได้ — จำกัดไว้เป็นรายการตายตัว ไม่รับชื่อคอลัมน์จากหน้าจอตรง ๆ
        /// (กัน SQL injection และกันเรียงด้วยคอลัมน์ที่ไม่มี)
        ///
        /// เรียงได้เฉพาะข้อมูลที่อยู่ในขั้นคัดหน้า — ส่วนสถานะสัญญา / NewSale / ลงทะเบียน
        /// ดึงมาทีหลังเฉพาะแถวของหน้านั้น จึงเรียงทั้งชุดไม่ได้ถ้าไม่ย้ายกลับไปคำนวณก่อนแบ่งหน้า
        /// (ซึ่งจะช้าเหมือนเดิม)
        /// </summary>
        private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            ["date"]     = "SortDate",
            ["code"]     = "ApplicationCode",
            ["account"]  = "AccountNo",
            ["customer"] = "FirstName",
            ["branch"]   = "SaleDepName",
            ["product"]  = "ProductModelName",
            ["status"]   = "ApplicationStatusID",
        };

        private static string BuildOrderBy(string sort, string dir)
        {
            if (string.IsNullOrWhiteSpace(sort) || !SortColumns.TryGetValue(sort, out var col))
            {
                col = "SortDate";
                dir = "desc";
            }
            var direction = string.Equals(dir, "asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";
            // ต่อท้ายด้วย ApplicationID เสมอ เพื่อให้ลำดับคงที่ตอนค่าซ้ำกัน (ไม่งั้นแบ่งหน้าแล้วแถวสลับไปมา)
            return $"{col} {direction}, ApplicationID DESC";
        }

        /// <summary>ผลค้นหาที่เก็บไว้ พร้อมเวลาที่อ่านจากฐานข้อมูลจริง</summary>
        private sealed record CachedSearch(SearchResultDto Dto, DateTime ReadAtUtc);

        /// <summary>คืนผลชุดเดิม พร้อมบอกว่าข้อมูลเก่ากี่วินาทีแล้ว (ไม่แก้ของที่เก็บไว้)</summary>
        private static SearchResultDto WithAge(CachedSearch cached)
        {
            var m = cached.Dto.Meta;
            return new SearchResultDto
            {
                Success = cached.Dto.Success,
                Message = cached.Dto.Message,
                Data = cached.Dto.Data,
                Meta = new SearchMetaDto
                {
                    Page = m.Page, PageSize = m.PageSize, Total = m.Total, TotalPages = m.TotalPages,
                    PageSizes = m.PageSizes, GeneratedAt = m.GeneratedAt, Sort = m.Sort, Dir = m.Dir,
                    AgeSeconds = (int)Math.Round((DateTime.UtcNow - cached.ReadAtUtc).TotalSeconds)
                }
            };
        }
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> CacheLocks = new();

        public static void ConfigureCache(int seconds) => _cacheSeconds = Math.Max(0, seconds);

        public HomeController(ILogger<HomeController> logger, IDownstreamApi api, IMemoryCache cache,
                              IConfiguration configuration, ApplicationIdBounds idBounds)
        {
            _api = api;
            _cache = cache;
            _idBounds = idBounds;

            // เดิมเปิดไฟล์ appsettings จากดิสก์แล้วแปลง JSON ใหม่ทุก request
            // ตอนนี้ใช้ค่าที่แอปอ่านไว้ตั้งแต่ตอนเปิดระบบแทน
            _logger = logger;
            strConnString = configuration.GetConnectionString("strConnString");
            DATABASEK2 = configuration.GetConnectionString("DATABASEK2");
            WSCANCEL = configuration.GetConnectionString("WSCANCEL");
            UrlEztax = configuration.GetConnectionString("UrlEztax");
            UsernameEztax = configuration.GetConnectionString("UsernameEztax");
            PasswordEztax = configuration.GetConnectionString("PasswordEztax");
            ClientIdEztax = configuration.GetConnectionString("ClientIdEztax");

            ApiKey = configuration.GetConnectionString("ApiKey");
            SGAPIESIG = configuration.GetConnectionString("SGAPIESIG");

            SGDIRECT = configuration.GetConnectionString("SGDIRECT");
            SGCESIGNATURE = configuration.GetConnectionString("SGCESIGNATURE");
            SGCROSSBANK = configuration.GetConnectionString("SGCROSSBANK");
            C100 = configuration.GetConnectionString("C100");
            C100Apikey = configuration.GetConnectionString("C100Apikey");
            

            SGBCancelApi = configuration.GetConnectionString("SGBCancelApi");
            SGBCancelApikey = configuration.GetConnectionString("SGBCancelApikey");
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

        [RequireLogin]
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
                sqlCommand.Parameters.AddWithValue("AccountNo", (object?)_SearchGetApplicationHistory.AccountNo ?? DBNull.Value);
                sqlCommand.Parameters.AddWithValue("ApplicationCode", (object?)_SearchGetApplicationHistory.ApplicationCode ?? DBNull.Value);
                sqlCommand.Parameters.AddWithValue("startdate", (object?)_SearchGetApplicationHistory.startdate ?? DBNull.Value);
                sqlCommand.Parameters.AddWithValue("enddate", (object?)_SearchGetApplicationHistory.enddate ?? DBNull.Value);

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

                // เดิม log ผลลัพธ์ทั้งชุด ซึ่งมีชื่อ/เลขบัตร/เบอร์โทรของลูกค้า
                Log.Debug("history returned {RowCount} row(s)", _SearchGetApplicationHistoryResponeMaster.Count);

                sqlCommand.Parameters.Clear();

            }
            catch (Exception ex)
            {
                Log.Debug(ex.Message);
            }
            return PartialView("_SearchGetApplicationHistory", _SearchGetApplicationHistoryResponeMaster);
        }

        /// <summary>
        /// ค้นหาใบคำขอ — คืนเป็น JSON ทีละหน้า
        /// เดิม action นี้เรนเดอร์ HTML ทั้งตารางส่งกลับ (หน้าละหลายร้อย KB) ตอนนี้ส่งเฉพาะข้อมูล
        /// แล้วให้เบราว์เซอร์ประกอบตารางเอง
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Search(ApplicationRq _ApplicationModel, int page = 1, int pageSize = DefaultPageSize, bool noCache = false, string sort = "date", string dir = "desc")
        {
            // ต้องล็อกอินก่อน — เดิม action นี้ไม่เช็ค session ทำให้ดึงข้อมูลลูกค้าได้โดยไม่ล็อกอิน
            if (HttpContext.Session.GetString("EMP_CODE") == null)
            {
                return StatusCode(401, new SearchResultDto
                {
                    Success = false,
                    Message = "หมดเวลาใช้งาน กรุณาเข้าสู่ระบบใหม่"
                });
            }

            if (page < 1) page = 1;
            if (!AllowedPageSizes.Contains(pageSize)) pageSize = DefaultPageSize;

            // จับเวลาที่ผู้ใช้รอจริง รวมทั้งกรณีที่ตอบจากแคช เพื่อให้หน้าสถิติสะท้อนของจริง
            var swRequest = System.Diagnostics.Stopwatch.StartNew();
            var shape = DescribeSearchShape(_ApplicationModel);

            var cacheKey = BuildSearchCacheKey(_ApplicationModel, page, pageSize, sort, dir);

            if (cacheKey != null && !noCache && _cache.TryGetValue(cacheKey, out CachedSearch hit))
            {
                SearchMetrics.Record(shape, -1, -1, swRequest.ElapsedMilliseconds, fromCache: true);
                return Json(WithAge(hit));
            }

            // กันหลายคนวิ่งไปถาม DB พร้อมกันตอน cache หมดอายุ (cache stampede)
            // ให้ผ่านไปถามได้ทีละคน คนที่เหลือรอแล้วใช้ผลเดียวกัน
            SemaphoreSlim gate = null;
            if (cacheKey != null)
            {
                gate = CacheLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
                await gate.WaitAsync();
            }

            try
            {
                if (cacheKey != null && !noCache && _cache.TryGetValue(cacheKey, out CachedSearch hit2))
                {
                    SearchMetrics.Record(shape, -1, -1, swRequest.ElapsedMilliseconds, fromCache: true);
                    return Json(WithAge(hit2));
                }

                List<ApplicationResponeModel> rows;
                try
                {
                    rows = await RunSearch(_ApplicationModel, (page - 1) * pageSize, pageSize, sort, dir);
                    SearchMetrics.Record(shape, _lastPageMs, _lastEnrichMs, swRequest.ElapsedMilliseconds, fromCache: false);
                }
                catch (Exception ex)
                {
                    // เดิม catch แล้วคืนตารางว่าง ทำให้ "ค้นหาไม่สำเร็จ" หน้าตาเหมือน "ไม่พบข้อมูล"
                    Log.Error(ex, "Search failed");
                    return StatusCode(500, new SearchResultDto
                    {
                        Success = false,
                        Message = "ค้นหาไม่สำเร็จ กรุณาลองใหม่อีกครั้ง หากยังไม่ได้ให้แจ้งทีมผู้ดูแล"
                    });
                }

                int totalRows = rows.Count > 0 ? rows[0].TotalRows : 0;

                var result = new SearchResultDto
                {
                    Success = true,
                    Data = rows.Select(ToRowDto).ToList(),
                    Meta = new SearchMetaDto
                    {
                        Page = page,
                        PageSize = pageSize,
                        Total = totalRows,
                        TotalPages = totalRows == 0 ? 0 : (int)Math.Ceiling(totalRows / (double)pageSize),
                        PageSizes = AllowedPageSizes,
                        GeneratedAt = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                        AgeSeconds = 0,
                        Sort = SortColumns.ContainsKey(sort ?? "") ? sort.ToLowerInvariant() : "date",
                        Dir = string.Equals(dir, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc" 
                    }
                };

                if (cacheKey != null)
                {
                    _cache.Set(cacheKey, new CachedSearch(result, DateTime.UtcNow), TimeSpan.FromSeconds(_cacheSeconds));
                }

                return Json(result);
            }
            finally
            {
                gate?.Release();
            }
        }

        /// <summary>
        /// คืน key สำหรับเก็บผลค้นหาไว้ในหน่วยความจำ หรือ null ถ้าเคสนี้ไม่ควรเก็บ
        ///
        /// เก็บเฉพาะการ "เปิดดูรายวัน" ซึ่งเป็นเคสที่ทุกคนเปิดเหมือนกันและกดซ้ำบ่อย
        /// ส่วนการค้นเจาะจง (เลขที่ใบคำขอ/สัญญา/serial/เลขบัตร/ชื่อลูกค้า) ไม่เก็บ
        /// เพราะแต่ละคนค้นไม่เหมือนกัน เก็บไปก็ไม่มีใครใช้ซ้ำ และเป็นข้อมูลเฉพาะบุคคล
        /// </summary>
        private static string BuildSearchCacheKey(ApplicationRq m, int page, int pageSize, string sort, string dir)
        {
            if (_cacheSeconds <= 0) return null;
            if (page != 1) return null;
            if (Nz(m.ApplicationCode) != null || Nz(m.AccountNo) != null || Nz(m.ProductSerialNo) != null
                || Nz(m.CustomerID) != null || Nz(m.CustomerName) != null)
            {
                return null;
            }

            return string.Join("|", "search", InvalidateSearchCacheAttribute.Version,
                Nz(m.startdate) ?? DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Nz(m.enddate) ?? DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Nz(m.status), Nz(m.loanTypeCate), Nz(m.StatusRegis), Nz(m.quickSearch), pageSize, sort, dir);
        }

        /// <summary>
        /// แปลงแถวจากฐานข้อมูลเป็นข้อมูลที่หน้าจอใช้ พร้อมคำนวณว่าปุ่มซ่อมตัวไหนควรแสดง
        /// เงื่อนไขทั้งหมดยกมาจากไฟล์ Razor เดิม (_SearchResults.cshtml) แบบตรงตัว
        /// </summary>
        private static SearchRowDto ToRowDto(ApplicationResponeModel r)
        {
            string signed = (r.signedStatus ?? "").Trim();
            string received = (r.statusReceived ?? "").Trim();
            string status = (r.ApplicationStatusID ?? "").Trim();
            string loanType = (r.loanTypeCate ?? "").Trim();
            string ou = (r.OU_Code ?? "").Trim();
            string serial = (r.ProductSerialNo ?? "").Trim();
            string ref4 = (r.Ref4 ?? "").Trim();
            string regis = (r.numregis ?? "").Trim();

            bool isLockphone = loanType == "LOCKPHONE";
            bool isHp = loanType == "HP";
            bool receivedGoods = received == "รับสินค้าแล้ว";
            bool signedDone = signed == "เรียบร้อย";

            // ปุ่มส่งสถานะ CLOSED ซ้ำ
            bool canPushStatusClosed = signedDone && receivedGoods && status == "CLOSED" && regis == "เรียบร้อย";

            // ปุ่มสร้างลิงก์ e-signature ใหม่ — 3 กรณีตามประเภทสินเชื่อและ OU
            bool canGenEsignature =
                   (isLockphone && ou == "SGC" && status == "CLOSING" && (signed == "รอลงนาม" || signed == "-") && serial != "")
                || (isLockphone && ou == "STL" && status == "CLOSING" && signed == "รอลงนาม" && serial != "" && ref4 != "")
                || (isHp && ou == "STL" && status == "CLOSING" && signed == "รอลงนาม" && serial != "" && ref4 != "");

            // ปุ่มลงทะเบียนเครื่อง — ของเดิมเขียนแยก 4 สาขา แต่ยุบได้เป็นเงื่อนไขเดียว
            // ไม่ดู numregis โดยตั้งใจ: ใบที่ลงทะเบียนไปแล้วต้องกดยิงซ้ำได้ เพราะบางครั้ง
            // ปลายทางไม่ได้รับ หรือต้องส่งใหม่ — CCO ใช้ปุ่มนี้ยิงซ้ำเป็นงานประจำ
            bool regisDone = regis == "เรียบร้อย";
            bool regisEligible = isLockphone && receivedGoods && (signedDone || signed == "COMP-Fail");
            bool canRegisImei = regisEligible;

            // ถ้ายังไม่ลงทะเบียนแต่กดไม่ได้ ให้บอกสาเหตุบนหน้าจอ แทนที่จะไม่มีอะไรขึ้นเลย
            string? regisBlockedReason = null;
            if (isLockphone && !regisDone && !regisEligible)
            {
                var reasons = new List<string>();
                if (!receivedGoods) reasons.Add("ยังไม่รับสินค้า");
                if (!signedDone && signed != "COMP-Fail") reasons.Add("สัญญายังไม่เรียบร้อย");
                regisBlockedReason = "ยังลงทะเบียนเครื่องไม่ได้ เพราะ" + string.Join(" และ ", reasons);
            }

            // ปุ่มส่ง NewSale ซ้ำ (แสดงเฉพาะตอนที่ยังไม่มี NewSale)
            bool canRepushNewSale = signedDone && receivedGoods && ou != "STL" && (r.newnum ?? "").Trim() != "เรียบร้อย";

            // ปุ่มแจ้งยกเลิกไปยัง e-contract ซ้ำ
            // ขั้นสุดท้ายของการยกเลิกคือแจ้งสถานะไปยัง e-contract ถ้าขั้นนั้นล้ม
            // ใบคำขอจะถูกยกเลิกในระบบเรียบร้อยแล้วแต่ e-contract ยังเห็นสถานะเดิม
            // เดิมไม่มีทางแจ้งซ้ำเลย (จรวดส่งสถานะซ้ำครอบคลุมแค่ CLOSED) ต้องให้คนไปแก้ให้
            //
            // ดูจากผลที่ระบบบันทึกไว้ตอนแจ้ง ไม่ใช่เดาจากสถานะสัญญา
            // เพราะการเรียกเส้นแจ้งสถานะไม่ได้เปลี่ยนคอลัมน์ไหนฝั่งสัญญาให้มองเห็นได้เลย
            // (ทดสอบแล้ว ปลายทางตอบ PASS แต่ signedStatus คงเดิม) ถ้าเดาจากตรงนั้นจรวดจะไม่มีวันหาย
            // ใบเก่าที่ยกเลิกก่อนมีการบันทึกผลจะไม่มีจรวด เพราะไม่รู้จริง ๆ ว่าแจ้งไปถึงหรือไม่
            bool canRenotifyCancel = status == "CANCELLED"
                && string.Equals((r.CancelNotifyStatus ?? "").Trim(), "FAILED", StringComparison.OrdinalIgnoreCase);

            return new SearchRowDto
            {
                ApplicationCode = r.ApplicationCode,
                RefCode = r.RefCode,
                ApplicationDate = r.ApplicationDate,
                AccountNo = r.AccountNo,
                CustomerId = r.CustomerID,
                CustomerName = r.Cusname,
                CustomerMobile = r.cusMobile,
                SaleDepCode = r.SaleDepCode,
                SaleDepName = r.SaleDepName,
                SaleName = r.SaleName,
                SaleTelephoneNo = r.SaleTelephoneNo,
                ProductModelName = r.ProductModelName,
                ProductSerialNo = r.ProductSerialNo,
                ApplicationStatusId = r.ApplicationStatusID,
                LineStatus = r.LINE_STATUS,
                SignedStatus = r.signedStatus,
                StatusReceived = r.statusReceived,
                NumRegis = r.numregis,
                NumDoc = r.numdoc,
                NewNum = r.newnum,
                PayNum = r.paynum,
                LoanTypeCate = r.loanTypeCate,
                OuCode = r.OU_Code,
                CanPushStatusClosed = canPushStatusClosed,
                CanGenEsignature = canGenEsignature,
                CanRegisImei = canRegisImei,
                CanRepushNewSale = canRepushNewSale,
                CanFixDuplicateContract = (r.numdoc ?? "").Trim() == "พบรายการซ้ำ",
                CanRenotifyCancel = canRenotifyCancel,
                RegisBlockedReason = regisBlockedReason
            };
        }

        /// <summary>
        /// ดาวน์โหลดผลการค้นหา "ทั้งหมด" ตามเงื่อนไขที่กรอก (ไม่ใช่เฉพาะหน้าที่แสดงอยู่)
        /// จำเป็นต้องทำฝั่ง server เพราะเมื่อแบ่งหน้าที่ server แล้ว ปุ่ม export ของ DataTables
        /// จะเห็นข้อมูลแค่หน้าเดียว
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ExportSearch(ApplicationRq _ApplicationModel)
        {
            if (HttpContext.Session.GetString("EMP_CODE") == null)
            {
                return Unauthorized();
            }

            List<ApplicationResponeModel> rows;
            try
            {
                rows = await RunSearch(_ApplicationModel, 0, ExportMaxRows, "date", "desc");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExportSearch failed");
                return StatusCode(500, "ดาวน์โหลดไม่สำเร็จ กรุณาลองใหม่อีกครั้ง");
            }

            var fileName = $"search-result-{DateTime.Now:yyyyMMdd-HHmmss}.csv";
            Response.Headers.ContentDisposition = $"attachment; filename={fileName}";

            // ทยอยเขียนลงสายส่งทีละแถว แทนการต่อสตริงทั้งไฟล์ไว้ในหน่วยความจำก่อน
            // (5 หมื่นแถวจะกินแรมหลายสิบเมกะไบต์ต่อคนที่กดดาวน์โหลดพร้อมกัน)
            return new FileCallbackResult("text/csv; charset=utf-8", async stream =>
            {
                // UTF-8 BOM เพื่อให้ Excel อ่านภาษาไทยได้ถูกต้องเมื่อเปิดไฟล์ CSV โดยตรง
                await stream.WriteAsync(Encoding.UTF8.GetPreamble());

                await using var writer = new StreamWriter(stream, new UTF8Encoding(false));

                await writer.WriteLineAsync(string.Join(",", new[]
                {
                    "วันที่สร้างใบคำขอ", "เลขที่ใบคำขอ", "RefCode", "เลขที่สัญญา", "เลขบัตรประชาชน",
                    "ชื่อลูกค้า", "เบอร์โทรศัพท์ลูกค้า", "รหัสสาขา", "ชื่อสาขา", "ชื่อพนักงานขาย",
                    "เบอร์พนักงานขาย", "ชื่อสินค้า", "Serial / IMEI", "สถานะ", "จำนวนสัญญา",
                    "สถานะสัญญา", "สถานะรับสินค้า", "ลงทะเบียนเครื่อง", "NewSale", "NewPayment",
                    "ประเภทรายการ", "OU"
                }.Select(Csv)));

                foreach (var r in rows)
                {
                    await writer.WriteLineAsync(string.Join(",", new[]
                    {
                        r.ApplicationDate, r.ApplicationCode, r.RefCode, r.AccountNo, r.CustomerID,
                        r.Cusname, r.cusMobile, r.SaleDepCode, r.SaleDepName, r.SaleName,
                        r.SaleTelephoneNo, r.ProductModelName, r.ProductSerialNo, r.ApplicationStatusID, r.numdoc,
                        r.signedStatus, r.statusReceived, r.numregis, r.newnum, r.paynum,
                        r.loanTypeCate, r.OU_Code
                    }.Select(Csv)));
                }

                await writer.FlushAsync();
            });
        }

        /// <summary>
        /// หน้าสถิติการใช้งานหน้าค้นหา
        ///
        /// มีไว้ให้ตอบได้ว่า "ตอนนี้ช้าตรงไหน" ด้วยตัวเลขจริง แทนการเดา
        /// ค่าทั้งหมดอยู่ในหน่วยความจำของแอป จะหายเมื่อรีสตาร์ท
        /// </summary>
        [RequireLogin]
        [HttpGet]
        public IActionResult Stats()
        {
            ViewBag.Stats = SearchMetrics.Snapshot();
            ViewBag.StartedAt = SearchMetrics.StartedAt;
            ViewBag.CacheSeconds = _cacheSeconds;
            return View();
        }

        /// <summary>ครอบค่าให้ปลอดภัยสำหรับไฟล์ CSV (กันคอมมา ตัวขึ้นบรรทัดใหม่ และ formula injection)</summary>
        private static string Csv(string value)
        {
            var v = value ?? "";
            if (v.Length > 0 && (v[0] == '=' || v[0] == '+' || v[0] == '-' || v[0] == '@'))
            {
                v = "'" + v;
            }
            return "\"" + v.Replace("\"", "\"\"") + "\"";
        }

        /// <summary>
        /// รันคำค้นเดียวกันกับที่หน้าจอใช้ โดยระบุช่วงแถวที่ต้องการ (offset/take)
        /// ใช้ร่วมกันระหว่างการแสดงผลทีละหน้าและการดาวน์โหลดทั้งผลลัพธ์
        /// </summary>
        private async Task<List<ApplicationResponeModel>> RunSearch(ApplicationRq model, int offset, int take, string sort, string dir)
        {
            // ปรับช่วงวันที่ก่อนส่งเข้า SQL
            // - ถ้าระบุ key เจาะจง (เลขที่ใบคำขอ/สัญญา/serial/เลขบัตร) → ไม่ต้องจำกัดวันที่ ค้นได้ทั้งหมด
            // - ถ้าไม่ระบุ key และไม่ใส่วันที่ → ใช้ "วันนี้" แทนการดึงย้อนหลังทั้งหมดตั้งแต่ 2024-05-01
            string accountNo = Nz(model.AccountNo);
            string applicationCode = Nz(model.ApplicationCode);
            string productSerialNo = Nz(model.ProductSerialNo);
            string customerId = Nz(model.CustomerID);

            bool hasSpecificKey = accountNo != null || applicationCode != null
                                  || productSerialNo != null || customerId != null;

            string startDate = Nz(model.startdate);
            string endDate = Nz(model.enddate);

            if (!hasSpecificKey)
            {
                if (startDate == null && endDate == null)
                {
                    startDate = endDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                }
                else
                {
                    startDate ??= endDate;
                    endDate ??= startDate;
                }
            }

            // ตาราง Application ไม่มี index บน ApplicationDate การกรองด้วยวันที่จึงต้องกวาดทั้งตาราง
            // แปลงวันที่เริ่มค้นเป็นขอบล่างของ ApplicationID เพื่อให้ใช้ clustered index ได้แทน
            int idLowerBound = startDate == null
                ? ApplicationIdBounds.NoBound
                : await _idBounds.GetLowerBoundAsync(startDate);

            using var connection = new SqlConnection(strConnString);
            await connection.OpenAsync();

            // ---- รอบที่ 1: คัดเฉพาะใบคำขอของหน้านี้ (≤ 100 แถว) ----
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var swTotal = System.Diagnostics.Stopwatch.StartNew();

            var pageRows = (await connection.QueryAsync<PageRow>(new CommandDefinition(BuildPageSql(BuildOrderBy(sort, dir)), new
            {
                startDate,
                endDate,
                status = Nz(model.status),
                loanTypeCate = Nz(model.loanTypeCate),
                AccountNo = accountNo,
                ApplicationCode = applicationCode,
                ProductSerialNo = productSerialNo,
                CustomerID = customerId,
                CustomerName = Nz(model.CustomerName),
                quickSearch = Nz(model.quickSearch),
                StatusRegis = Nz(model.StatusRegis),
                idLowerBound,
                offset,
                pageSize = take
            }, commandTimeout: 120))).ToList();

            _lastPageMs = sw.ElapsedMilliseconds;
            Log.Information("ค้นหา: เลือกหน้า {Ms} ms ({Rows} แถว)", sw.ElapsedMilliseconds, pageRows.Count);

            if (pageRows.Count == 0)
            {
                _lastEnrichMs = 0;
                return new List<ApplicationResponeModel>();
            }

            // ---- รอบที่ 2: ดึงข้อมูลประกอบ "เฉพาะคีย์ของหน้านี้" ----
            // เดิมผูกด้วย EXISTS กับตารางชั่วคราว ซึ่ง SQL Server ส่งเงื่อนไขข้ามไปให้เซิร์ฟเวอร์ปลายทาง
            // (contracts อยู่คนละเครื่อง) ไม่ได้ จึงต้องกวาดตารางฝั่งโน้นทั้งก้อนทุกครั้ง — บน PROD กินไป ~3.4 วินาที
            // ส่งเป็นรายการคีย์แทน ปลายทาง seek ตรง ๆ ได้ เหลือ ~0.15 วินาที
            sw.Restart();
            var codes = pageRows.Select(r => r.ApplicationCode).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
            var accounts = pageRows.Select(r => r.AccountNo).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
            var serials = pageRows.Select(r => r.ProductSerialNo).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();

            // ต้องแบ่งรายการคีย์เป็นก้อน — SQL Server รับพารามิเตอร์ได้สูงสุด 2,100 ตัวต่อคำสั่ง
            // ตอนแสดงผลทีละหน้าไม่เคยชน แต่ตอนดาวน์โหลดทั้งหมด (หลายหมื่นแถว) จะชนทันที
            //
            // ยิงทั้ง 5 ตัวพร้อมกัน — ไม่มีตัวไหนพึ่งผลของกันเลย คีย์ที่ใช้คำนวณเสร็จหมดแล้วข้างบน
            // ของเดิมยิงต่อกันทีละตัวบน connection เดียว เวลาจึงเป็นผลรวมของทั้ง 5 (วัดได้ ~490 ms)
            // ทั้งที่ควรเสียแค่เท่าตัวที่ช้าที่สุด · ต้องแยก connection เพราะ SqlConnection ใช้ข้ามเธรดไม่ได้
            var contractsTask = QueryInChunksAsync<ContractRow>(BuildContractSql(), "codes", codes);
            var newSalesTask = QueryInChunksAsync<NewSaleRow>(BuildNewSaleSql(), "accounts", accounts);
            var paymentsTask = QueryInChunksAsync<PaymentRow>(BuildPaymentSql(), "accounts", accounts);
            var regisTask = QueryInChunksAsync<RegisRow>(BuildRegisSql(), "serials", serials);
            var cancelNotifyTask = QueryInChunksAsync<CancelNotifyRow>(BuildCancelNotifySql(), "codes", codes);

            await Task.WhenAll(contractsTask, newSalesTask, paymentsTask, regisTask, cancelNotifyTask);

            var contracts = contractsTask.Result;
            var newSales = newSalesTask.Result;
            var payments = paymentsTask.Result;
            var regis = regisTask.Result;
            var cancelNotify = cancelNotifyTask.Result;

            _lastEnrichMs = sw.ElapsedMilliseconds;
            Log.Information("ค้นหา: ข้อมูลประกอบ {Ms} ms (สัญญา {C} / newsale {N} / payment {P} / regis {R}) — รวม {Total} ms",
                sw.ElapsedMilliseconds, contracts.Count, newSales.Count, payments.Count, regis.Count, swTotal.ElapsedMilliseconds);

            var contractByCode = contracts.GroupBy(c => c.documentno).ToDictionary(g => g.Key, g => g.First());
            var newSaleByAcc = newSales.GroupBy(n => n.ARM_ACC_NO).ToDictionary(g => g.Key, g => g.First());
            var paymentByAcc = payments.GroupBy(p => p.ARM_ACC_NO).ToDictionary(g => g.Key, g => g.First());
            var regisBySerial = regis.GroupBy(r => r.IMEI).ToDictionary(g => g.Key, g => g.First());
            var notifyByCode = cancelNotify.GroupBy(n => n.OrderID).ToDictionary(g => g.Key, g => g.First().StatusCode ?? "");

            return pageRows.Select(r =>
            {
                var item = Compose(r, contractByCode, newSaleByAcc, paymentByAcc, regisBySerial);
                notifyByCode.TryGetValue(r.ApplicationCode ?? "", out var notifyStatus);
                item.CancelNotifyStatus = notifyStatus ?? "";
                return item;
            }).ToList();
        }

        /// <summary>จำนวนคีย์สูงสุดต่อคำสั่ง — ต่ำกว่าเพดานพารามิเตอร์ 2,100 ของ SQL Server พอสมควร</summary>
        private const int KeyChunkSize = 1000;

        /// <summary>
        /// ยิงคำสั่งเดิมซ้ำเป็นก้อน ๆ ตามจำนวนคีย์ แล้วรวมผลลัพธ์
        /// เปิด connection ของตัวเองเพราะถูกเรียกพร้อมกันหลายตัว และ SqlConnection ใช้ข้ามเธรดไม่ได้
        /// (connection pool ของ ADO.NET จัดการให้อยู่แล้ว ไม่ได้เปิดต่อจริงใหม่ทุกครั้ง)
        /// </summary>
        private async Task<List<T>> QueryInChunksAsync<T>(string sql, string paramName, List<string> keys)
        {
            var result = new List<T>();
            if (keys.Count == 0) return result;

            using var connection = new SqlConnection(strConnString);
            await connection.OpenAsync();

            for (int i = 0; i < keys.Count; i += KeyChunkSize)
            {
                var chunk = keys.GetRange(i, Math.Min(KeyChunkSize, keys.Count - i));
                var parameters = new DynamicParameters();
                parameters.Add(paramName, chunk);
                result.AddRange(await connection.QueryAsync<T>(new CommandDefinition(sql, parameters, commandTimeout: 180)));
            }
            return result;
        }

        /// <summary>ประกอบแถวผลลัพธ์ — ข้อความสถานะทุกตัวยกมาจาก CASE เดิมใน SQL แบบตรงตัว</summary>
        private static ApplicationResponeModel Compose(
            PageRow r,
            IReadOnlyDictionary<string, ContractRow> contracts,
            IReadOnlyDictionary<string, NewSaleRow> newSales,
            IReadOnlyDictionary<string, PaymentRow> payments,
            IReadOnlyDictionary<string, RegisRow> regis)
        {
            contracts.TryGetValue(r.ApplicationCode ?? "", out var con);
            newSales.TryGetValue(r.AccountNo ?? "", out var nsale);
            payments.TryGetValue(r.AccountNo ?? "", out var pay);
            regis.TryGetValue(r.ProductSerialNo ?? "", out var reg);

            string signed = con?.signedStatus switch
            {
                "COMP-Done" => "เรียบร้อย",
                "Initial" => "รอลงนาม",
                null or "" => "-",
                var other => other
            };

            string newNum = nsale == null ? "ไม่พบรายการ"
                : nsale.newnum > 1 ? "รายการซ้ำ"
                : nsale.arm_Loaded_flag == 2 ? "CANCELLED"
                : (nsale.arm_Loaded_flag == 0 || nsale.arm_Loaded_flag == 1) ? "เรียบร้อย"
                : "ไม่พบรายการ";

            string payNum = pay == null ? "ไม่พบรายการ"
                : pay.paynum > 1 ? "รายการซ้ำ"
                : pay.ARM_RECEIPT_STAT == "APPROVED" ? "เรียบร้อย"
                : pay.ARM_RECEIPT_STAT == "CANCELLED" ? "CANCELLED"
                : "ไม่พบรายการ";

            return new ApplicationResponeModel
            {
                ApplicationID = r.ApplicationID,
                ApplicationCode = r.ApplicationCode,
                AccountNo = r.AccountNo,
                SaleDepCode = r.SaleDepCode,
                SaleDepName = r.SaleDepName,
                ApplicationDate = r.ApplicationDate,
                ProductModelName = r.ProductModelName,
                CustomerID = r.CustomerID,
                Cusname = string.Join(" ", new[] { r.FirstName, r.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))),
                cusMobile = r.MobileNo1,
                SaleName = r.SaleName,
                SaleTelephoneNo = r.SaleTelephoneNo,
                ProductSerialNo = r.ProductSerialNo,
                ApplicationStatusID = r.ApplicationStatusID,
                signedStatus = signed,
                statusReceived = con?.statusReceived == "1" ? "รับสินค้าแล้ว" : "ยังไม่รับสินค้า",
                numregis = r.loanTypeCate == "HP" || !string.IsNullOrEmpty(reg?.IMEI) ? "เรียบร้อย" : "รอลงทะเบียน",
                numdoc = (con?.numdoc ?? 0) > 1 ? "พบรายการซ้ำ" : "ปกติ",
                newnum = newNum,
                paynum = payNum,
                LINE_STATUS = "",
                RefCode = r.RefCode,
                OU_Code = string.IsNullOrEmpty(r.OU_Code) ? "" : r.OU_Code[..Math.Min(3, r.OU_Code.Length)],
                loanTypeCate = r.loanTypeCate,
                Ref4 = "DUMMY",
                appIns = "",
                Status = reg?.Status ?? "NULL",
                TotalRows = r.TotalRows
            };
        }

        private class PageRow
        {
            public string? ApplicationID { get; set; }
            public string? ApplicationCode { get; set; }
            public string? AccountNo { get; set; }
            public string? SaleDepCode { get; set; }
            public string? SaleDepName { get; set; }
            public string? ApplicationDate { get; set; }
            public string? ProductModelName { get; set; }
            public string? CustomerID { get; set; }
            public string? SaleName { get; set; }
            public string? SaleTelephoneNo { get; set; }
            public string? ProductSerialNo { get; set; }
            public string? ApplicationStatusID { get; set; }
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? MobileNo1 { get; set; }
            public string? RefCode { get; set; }
            public string? OU_Code { get; set; }
            public string? loanTypeCate { get; set; }
            public int TotalRows { get; set; }
        }

        private class ContractRow
        {
            public string documentno { get; set; } = "";
            public string? signedStatus { get; set; }
            public string? statusReceived { get; set; }
            public int numdoc { get; set; }
        }

        private class NewSaleRow
        {
            public string ARM_ACC_NO { get; set; } = "";
            public int newnum { get; set; }
            public int arm_Loaded_flag { get; set; }
        }

        private class PaymentRow
        {
            public string ARM_ACC_NO { get; set; } = "";
            public int paynum { get; set; }
            public string? ARM_RECEIPT_STAT { get; set; }
        }

        private class RegisRow
        {
            public string IMEI { get; set; } = "";
            public string? Status { get; set; }
        }

        private class CancelNotifyRow
        {
            public string OrderID { get; set; } = "";
            public string? StatusCode { get; set; }
        }

        // SQL ทั้งหมดย้ายไปอยู่ที่ App/Data/SearchSql.cs เพื่อให้งานอุ่นข้อมูลเบื้องหลังใช้ชุดเดียวกัน
        private string BuildPageSql(string orderBy) => App.Data.SearchSql.Page(DATABASEK2, orderBy);
        private string BuildContractSql() => App.Data.SearchSql.Contract(DATABASEK2, SGCESIGNATURE);
        private string BuildNewSaleSql() => App.Data.SearchSql.NewSale(DATABASEK2);
        private string BuildPaymentSql() => App.Data.SearchSql.Payment(DATABASEK2);
        private string BuildRegisSql() => App.Data.SearchSql.Regis(DATABASEK2);
        private string BuildCancelNotifySql() => App.Data.SearchSql.CancelNotify(DATABASEK2);


        // ขนาดหน้าเริ่มต้นและตัวเลือกที่อนุญาต (จำกัดไว้เพื่อไม่ให้ยิงค่าใหญ่ ๆ เข้ามาทาง query string)
        private const int DefaultPageSize = 5;
        private static readonly int[] AllowedPageSizes = { 5, 10, 20, 50, 100 };

        // เพดานจำนวนแถวของการดาวน์โหลดไฟล์ (ดึงทั้งผลลัพธ์ ไม่ใช่เฉพาะหน้าที่แสดง)
        private const int ExportMaxRows = 50000;

        // แปลงค่าว่าง/ช่องว่างให้เป็น null เพื่อให้เงื่อนไข (@p IS NULL OR ...) ใน SQL ทำงานถูกต้อง
        private static string Nz(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        // เวลาของรอบล่าสุด ใช้ส่งต่อให้หน้าสถิติ — ปลอดภัยเพราะ controller หนึ่งตัวรับผิดชอบ request เดียว
        private long _lastPageMs = -1;
        private long _lastEnrichMs = -1;

        /// <summary>จัดกลุ่มการค้นตามรูปแบบ เพื่อให้หน้าสถิติเทียบของที่เทียบกันได้</summary>
        private static string DescribeSearchShape(ApplicationRq m)
        {
            if (Nz(m.ApplicationCode) != null || Nz(m.AccountNo) != null
                || Nz(m.ProductSerialNo) != null || Nz(m.CustomerID) != null)
            {
                return "ระบุเลขเจาะจง";
            }
            if (Nz(m.CustomerName) != null) return "ค้นด้วยชื่อลูกค้า";
            if (Nz(m.startdate) != null || Nz(m.enddate) != null) return "ระบุช่วงวันที่";
            return "วันนี้";
        }
        // Dummy method to simulate search operation
        [InvalidateSearchCache]
        [RequireLogin]
        [HttpPost]
        public async Task<IActionResult> UpdateDataCancel(FormConfirmModel _FormConfirmModel)
        {
            // บันทึกผลทีละขั้น เพื่อให้ผู้ใช้เห็นว่าอะไรทำไปแล้วบ้างเมื่อพังกลางทาง
            var steps = new CancelStepRecorder();
            var actor = HttpContext.Session.GetString("EMP_CODE");
            Log.Information("ยกเลิกใบคำขอ (วันเดียว): {Code} โดย {Actor}", _FormConfirmModel?.ApplicationCode, actor);

            string ResultDescription = "";
            var stepLog = steps.Begin("บันทึกคำขอยกเลิก");
            CancelStepDto stepCancel = null, stepNotify = null;
            try
            {
                DataTable dt1 = new DataTable();
                SqlConnection connection1 = new SqlConnection();
                connection1.ConnectionString = strConnString;
                connection1.Open();
                //Write Log
                SqlCommand sqlCommand1;
                string strSQL = DATABASEK2 + ".[CCO_CANCEL]";
                sqlCommand1 = new SqlCommand(strSQL, connection1);
                sqlCommand1.CommandTimeout = 180;
                sqlCommand1.CommandType = CommandType.StoredProcedure;
                sqlCommand1.Parameters.AddWithValue("ApplicationCode", (object?)_FormConfirmModel.ApplicationCode ?? DBNull.Value);
                sqlCommand1.Parameters.AddWithValue("Remark", (object?)_FormConfirmModel.Remark ?? DBNull.Value);
                sqlCommand1.Parameters.AddWithValue("ExceptIMEI", (object?)_FormConfirmModel.ExceptIMEI ?? DBNull.Value);
                sqlCommand1.Parameters.AddWithValue("ExceptCus", (object?)_FormConfirmModel.ExceptCus ?? DBNull.Value);
                sqlCommand1.Parameters.AddWithValue("Other", (object?)_FormConfirmModel.Other ?? DBNull.Value);
                sqlCommand1.Parameters.AddWithValue("CreateBy", (object?)HttpContext.Session.GetString("EMP_CODE") ?? DBNull.Value);
                sqlCommand1.Parameters.AddWithValue("Source", "Cancel");

                SqlDataAdapter dtAdapter1 = new SqlDataAdapter();
                dtAdapter1.SelectCommand = sqlCommand1;
                dt1 = new DataTable();
                dtAdapter1.Fill(dt1);
                connection1.Close();
                steps.Ok(stepLog);
                stepCancel = steps.Begin("ยกเลิกใบคำขอในระบบ (อัปเดตสถานะเป็น CANCELLED)", irreversible: true);
                //CancelLOS cancelLOS = new CancelLOS();
                //cancelLOS.refCode = _FormConfirmModel.ApplicationCode;
                //cancelLOS.userName = HttpContext.Session.GetString("EMP_CODE");





                //using (HttpClient client = new HttpClient())
                //{
                //    string jsonBody = JsonConvert.SerializeObject(cancelLOS);

                //    client.DefaultRequestHeaders.Add("Apikey", C100Apikey);

                //    var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                //    HttpResponseMessage responseDevice = await client.PostAsync(C100 + "/v2/SgFinance/CancelContractToLMS", content);
                //    int DeviceStatusCode = (int)responseDevice.StatusCode;

                //    Log.Debug("API BODY RESPONE : " + JsonConvert.SerializeObject(responseDevice.Content.ReadAsStringAsync()));

                //    if (!responseDevice.IsSuccessStatusCode)
                //    {
                //        var ResponseContent = await responseDevice.Content.ReadAsStringAsync();
                //        ModelResult modelResult = new ModelResult();
                //        modelResult = JsonConvert.DeserializeObject<ModelResult>(ResponseContent);

                //        ResultDescription = modelResult.message;
                //        return ResultDescription;
                //    }
                //}


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

                //Cancel econtract

                using (SqlConnection connection = new SqlConnection(strConnString))
                {
                    await connection.OpenAsync();

                    using (SqlCommand sqlCommand = new SqlCommand(DATABASEK2 + ".[CancelApplication]", connection))
                    {
                        sqlCommand.CommandType = CommandType.StoredProcedure;
                        sqlCommand.CommandTimeout = 120; // Set timeout to 120 seconds
                        sqlCommand.Parameters.AddWithValue("ApplicationCode", (object?)_FormConfirmModel.ApplicationCode ?? DBNull.Value);
                        sqlCommand.Parameters.AddWithValue("Remark", _FormConfirmModel.Remark + " " + _FormConfirmModel.Other);
                        sqlCommand.Parameters.AddWithValue("CANCEL_USER", (object?)HttpContext.Session.GetString("EMP_CODE") ?? DBNull.Value);
                        sqlCommand.Parameters.AddWithValue("Except_IMEI", (object?)_FormConfirmModel.ExceptIMEI ?? DBNull.Value);
                        sqlCommand.Parameters.AddWithValue("Except_CUST", (object?)_FormConfirmModel.ExceptCus ?? DBNull.Value);

                        using (SqlDataAdapter dtAdapter = new SqlDataAdapter(sqlCommand))
                        {
                            DataTable dt = new DataTable();
                            dtAdapter.Fill(dt);

                            if (dt.Rows.Count > 0)
                            {
                                if ("SUCCESS" != dt.Rows[0]["Result"].ToString().ToUpper())
                                {
                                    ResultDescription += _GetApplicationRespone.AccountNo + " " + dt.Rows[0]["ResultDescription"].ToString();
                                    steps.Failed(stepCancel, dt.Rows[0]["ResultDescription"].ToString());
                                }
                                else
                                {
                                    steps.Ok(stepCancel);
                                }
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(ResultDescription))
                {
                    // อ่านสถานะกลับมายืนยันว่าเปลี่ยนจริง — SP มีเงื่อนไขคัดกรองอยู่ข้างใน
                    // ถ้าใบคำขอไม่เข้าเงื่อนไข SP จะไม่คืนแถวใด ๆ กลับมาโดยไม่แจ้งอะไร
                    // เดิมกรณีนี้จะแจ้งว่ายกเลิกสำเร็จทั้งที่สถานะไม่ได้เปลี่ยน หน้าจอจึงไม่ตรงกับความจริง
                    string statusAfter;
                    using (var verifyConn = new SqlConnection(strConnString))
                    {
                        statusAfter = (await verifyConn.QueryAsync<string>(new CommandDefinition(
                            $"SELECT ApplicationStatusID FROM {DATABASEK2}.[Application] WITH (NOLOCK) WHERE ApplicationCode = @ApplicationCode",
                            new { ApplicationCode = _GetApplicationRespone.ApplicationCode },
                            commandTimeout: 60))).FirstOrDefault() ?? "";
                    }

                    if (!string.Equals(statusAfter.Trim(), "CANCELLED", StringComparison.OrdinalIgnoreCase))
                    {
                        ResultDescription = $"ใบคำขอนี้ไม่เข้าเงื่อนไขการยกเลิก (สถานะยังเป็น {statusAfter.Trim()})";
                        if (stepCancel != null) steps.Failed(stepCancel, ResultDescription);
                        return BuildCancelResult(_FormConfirmModel?.ApplicationCode, steps, ResultDescription);
                    }

                    if (stepCancel != null && stepCancel.Status == "pending") steps.Ok(stepCancel);
                    stepNotify = steps.Begin("แจ้งสถานะไปยัง e-contract");

                   


                    string currentDateTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                    var requestBody = new
                    {
                        applicationCode = _GetApplicationRespone.ApplicationCode,
                        applicationStatus = "CANCELLED",
                        approvalStatus = "CANCELLED",
                        approvalDatetime = currentDateTime,
                        remark = _FormConfirmModel.Remark + "" + _FormConfirmModel.Other
                    };

                    const string renotifyHint = " — ใบคำขอถูกยกเลิกเรียบร้อยแล้ว แจ้งซ้ำได้ที่จรวดในคอลัมน์สถานะ หน้ารายการค้นหา";

                    var notify = await _api.PostJsonAsync("esig", "/sgesig/Service/C100_Status", requestBody,
                                                          $"แจ้งยกเลิก [{_GetApplicationRespone.ApplicationCode}] โดย {actor}");

                    if (!notify.Reached)
                    {
                        steps.Failed(stepNotify, "ติดต่อ e-contract ไม่ได้" + renotifyHint);
                        await RecordCancelNotifyAsync(_GetApplicationRespone.ApplicationCode, false, notify.TransportError);
                    }
                    else if (!notify.IsSuccess)
                    {
                        steps.Failed(stepNotify, DescribeError(notify.Body) + renotifyHint);
                        await RecordCancelNotifyAsync(_GetApplicationRespone.ApplicationCode, false, notify.Body);
                    }
                    else
                    {
                        steps.Ok(stepNotify);
                        await RecordCancelNotifyAsync(_GetApplicationRespone.ApplicationCode, true, null);
                    }
                }
                //}
                //else
                //{
                //    ResultDescription = "ไม่สามารถยกเลิกรายการได้ เนื่องจากเลยกำหนดเวลาการยกเลิกแล้ว";
                //}
                return BuildCancelResult(_FormConfirmModel?.ApplicationCode, steps, ResultDescription);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ยกเลิกใบคำขอไม่สำเร็จ: {Code}", _FormConfirmModel?.ApplicationCode);
                var current = steps.Steps.LastOrDefault(x => x.Status == "pending");
                if (current != null) steps.Failed(current, ex.Message);
                return BuildCancelResult(_FormConfirmModel?.ApplicationCode, steps, ex.Message);
            }


        }

        /// <summary>ประกอบผลลัพธ์การยกเลิกให้ผู้ใช้เห็นทุกขั้น พร้อมเตือนเมื่อมีขั้นที่ย้อนกลับไม่ได้ทำไปแล้ว</summary>
        /// <summary>
        /// บันทึกว่าแจ้งยกเลิกไปยัง e-contract สำเร็จหรือไม่
        ///
        /// จำเป็นเพราะเส้นแจ้งสถานะไม่ได้เปลี่ยนข้อมูลฝั่งสัญญาให้มองเห็นได้
        /// ถ้าไม่บันทึกไว้เอง จะไม่มีทางรู้ทีหลังว่าใบไหนแจ้งไปถึงแล้วบ้าง
        /// </summary>
        private async Task RecordCancelNotifyAsync(string? code, bool ok, string? detail)
        {
            if (string.IsNullOrWhiteSpace(code)) return;

            try
            {
                using var connection = new SqlConnection(strConnString);
                await connection.ExecuteAsync(new CommandDefinition(
                    $@"INSERT INTO {DATABASEK2}.[LOG_TRANSACTTION_SGFINANCE] (OrderID, StatusCode, StatusDesc, CreateDate, [Type])
                       VALUES (@OrderID, @StatusCode, @StatusDesc, GETDATE(), @Type)",
                    new
                    {
                        OrderID = code,
                        StatusCode = ok ? "SUCCESS" : "FAILED",
                        StatusDesc = detail ?? "",
                        Type = App.Data.SearchSql.CancelNotifyType
                    }, commandTimeout: 60));
            }
            catch (Exception ex)
            {
                // บันทึกไม่ได้ก็ไม่ควรทำให้การยกเลิกทั้งงานล้ม แค่บอกไว้ใน log ของแอป
                Log.Error(ex, "บันทึกผลการแจ้งยกเลิกไม่สำเร็จ: {Code}", code);
            }
        }

        private IActionResult BuildCancelResult(string? code, CancelStepRecorder steps, string error)
        {
            bool ok = string.IsNullOrWhiteSpace(error) && !steps.AnyFailed;

            string message;
            if (ok)
            {
                message = $"ยกเลิกใบคำขอ {code} เรียบร้อย";
            }
            else if (steps.HasIrreversibleDone)
            {
                // กรณีอันตรายที่สุด: ระบบอื่นถูกแก้ไปแล้วแต่ขั้นตอนไม่จบ ต้องบอกให้ชัดว่าอย่าเพิ่งกดซ้ำ
                message = "ยกเลิกไม่สมบูรณ์ — บางขั้นตอนทำไปแล้วและย้อนกลับเองไม่ได้ " +
                          "กรุณาแจ้งทีมผู้ดูแลพร้อมข้อมูลด้านล่าง อย่าเพิ่งกดยกเลิกซ้ำ";
            }
            else
            {
                message = $"ยกเลิกใบคำขอ {code} ไม่สำเร็จ ยังไม่มีการเปลี่ยนแปลงข้อมูล";
            }

            Log.Information("ผลการยกเลิก {Code}: {Result} | {Steps}", code, ok ? "สำเร็จ" : "ไม่สำเร็จ",
                string.Join(" -> ", steps.Steps.Select(x => x.Name + "=" + x.Status)));

            return Ok(new CancelResultDto
            {
                Ok = ok,
                Message = message,
                Detail = string.IsNullOrWhiteSpace(error) ? null : error,
                Steps = steps.Steps
            });
        }


        [InvalidateSearchCache]
        [RequireLogin]
        [HttpPost]
        public async Task<IActionResult> UpdateDataCancelCLOSED(FormConfirmModel _FormConfirmModel)
        {
            // ยกเลิกแบบข้ามวันไล่ทำหลายระบบต่อกัน จึงบันทึกผลทีละขั้น
            // เพื่อให้ผู้ใช้เห็นว่าอะไรทำไปแล้วบ้างเมื่อพังกลางทาง แทนที่จะได้แค่ข้อความ error ก้อนเดียว
            var steps = new CancelStepRecorder();
            var actor = HttpContext.Session.GetString("EMP_CODE");
            var code = _FormConfirmModel?.ApplicationCode;
            var remark = (_FormConfirmModel?.Remark ?? "") + (_FormConfirmModel?.Other ?? "");
            Log.Information("ยกเลิกใบคำขอ (ข้ามวัน): {Code} โดย {Actor}", code, actor);

            string ResultDescription = "";
            var stepLog = steps.Begin("บันทึกคำขอยกเลิก");
            try
            {
                // ---- 1. บันทึกคำขอยกเลิกลง log ----
                using (var connection = new SqlConnection(strConnString))
                {
                    await connection.ExecuteAsync(new CommandDefinition(
                        $"{DATABASEK2}.[CCO_CANCEL]",
                        new
                        {
                            ApplicationCode = code,
                            Remark = _FormConfirmModel?.Remark,
                            ExceptIMEI = _FormConfirmModel?.ExceptIMEI,
                            ExceptCus = _FormConfirmModel?.ExceptCus,
                            Other = _FormConfirmModel?.Other,
                            CreateBy = actor,
                            Source = "CancelCLOSED"
                        },
                        commandType: CommandType.StoredProcedure, commandTimeout: 180));
                }
                steps.Ok(stepLog);

                // ---- 2. อ่านข้อมูลใบคำขอ ----
                var stepRead = steps.Begin("ตรวจสอบข้อมูลใบคำขอ");
                var _GetApplication = new GetApplication { ApplicationCode = code };
                var _GetApplicationRespone = await GetApplication(_GetApplication);

                if (string.IsNullOrWhiteSpace(_GetApplicationRespone?.ApplicationID))
                {
                    // เดิมกรณีนี้จะวิ่งต่อไปยิง SOAP ด้วย id ว่าง แล้วค่อยพังปลายทางแบบไม่รู้สาเหตุ
                    steps.Failed(stepRead, "ไม่พบใบคำขอนี้ในระบบ");
                    return BuildCancelResult(code, steps, $"ไม่พบใบคำขอ {code} ในระบบ");
                }
                steps.Ok(stepRead, $"เลขที่สัญญา {(string.IsNullOrWhiteSpace(_GetApplicationRespone.AccountNo) ? "-" : _GetApplicationRespone.AccountNo.Trim())}");

                // ---- 3. ยกเลิกงานที่ค้างอยู่ในระบบพิจารณาสินเชื่อ (K2) ----
                var stepK2 = steps.Begin("ยกเลิกงานในระบบพิจารณาสินเชื่อ", irreversible: true);
                var _MessageModel = await CCOWebService(new CCOWebServiceModel { id = _GetApplicationRespone.ApplicationID });
                if (_MessageModel?.StatusCode == "200")
                {
                    steps.Ok(stepK2);
                }
                else
                {
                    // เดิมผลลัพธ์ตรงนี้ถูกทิ้งไปทั้งหมด ต่อให้ยกเลิกงานใน K2 ไม่สำเร็จก็วิ่งต่อเงียบ ๆ
                    // ใบคำขอที่ยกเลิกแบบข้ามวันคือใบที่ปิดงานไปแล้ว งานใน K2 จึงมักไม่มีให้ยกเลิก
                    // กรณีนี้ไม่ถือว่าล้มเหลว แต่ต้องบอกให้เห็น ไม่ใช่รายงานว่าสำเร็จทั้งที่ไม่ได้ทำ
                    steps.Skipped(stepK2, "ไม่มีงานค้างให้ยกเลิกในระบบพิจารณาสินเชื่อ (" + _MessageModel?.Message + ")");
                }

                // ---- 4. ดูประเภทสินเชื่อ เพื่อตัดสินใจว่าต้องยกเลิกสัญญาที่ระบบสินเชื่อด้วยหรือไม่ ----
                // เดิมเรียก SP [LoanTypeCate] ซึ่งมีเฉพาะบน PROD ไม่มีบน DEV ทำให้ทดสอบบน DEV แล้วพังทุกครั้ง
                // ตรงนี้จึงย้ายมาเป็น query ตรงด้วยเงื่อนไขเดียวกับ SP เป๊ะ ๆ (loanTypeCate + AccountNo)
                var stepType = steps.Begin("ตรวจสอบประเภทสินเชื่อ");
                LoanTypeCateRow typeRow;
                using (var connection = new SqlConnection(strConnString))
                {
                    typeRow = (await connection.QueryAsync<LoanTypeCateRow>(new CommandDefinition(
                        $@"SELECT e.loanTypeCate, a.AccountNo
                             FROM {DATABASEK2}.[Application] a WITH (NOLOCK)
                       INNER JOIN {DATABASEK2}.[ApplicationExtend] e WITH (NOLOCK)
                               ON a.ApplicationID = e.ApplicationID
                            WHERE a.ApplicationCode = @ApplicationCode",
                        new { ApplicationCode = _GetApplicationRespone.ApplicationCode },
                        commandTimeout: 180))).FirstOrDefault();
                }

                var loanTypeCate = (typeRow?.loanTypeCate ?? "").Trim();
                var accountNo = (typeRow?.AccountNo ?? "").Trim();
                bool needLmsCancel = loanTypeCate.ToUpper() == "LOCKPHONE" && accountNo != "";
                steps.Ok(stepType, string.IsNullOrEmpty(loanTypeCate) ? "ไม่ระบุประเภท" : loanTypeCate);

                // ---- 5. ยกเลิกสัญญาที่ระบบสินเชื่อ (LMS) ----
                var stepLms = steps.Begin("ยกเลิกสัญญาในระบบสินเชื่อ", irreversible: true);
                if (!needLmsCancel)
                {
                    steps.Skipped(stepLms, loanTypeCate.ToUpper() != "LOCKPHONE"
                        ? "ใบคำขอนี้ไม่ใช่ประเภท LOCKPHONE จึงไม่มีสัญญาที่ต้องยกเลิก"
                        : "ใบคำขอนี้ยังไม่มีเลขที่สัญญา จึงไม่มีสัญญาที่ต้องยกเลิก");
                }
                else
                {
                    // เดิมส่ง ApplicationCode (เช่น 060-2608-00010) ไปในช่อง refCode
                    // แต่ระบบสินเชื่อรู้จักใบคำขอด้วยเลขอ้างอิงของ LOS (เช่น REQ-2026-08-000164) จึงตอบกลับว่าไม่พบรายการ
                    // จะยกเลิกผ่านหรือไม่จึงขึ้นกับว่า CCO ค้นด้วยเลขไหน ซึ่งเป็นที่มาของอาการ "ยกเลิกแล้วหน้าจอไม่อัปเดต"
                    var refCode = (_GetApplicationRespone.RefCode ?? "").Trim();
                    if (string.IsNullOrEmpty(refCode)) refCode = code;
                    var cancelLOS = new CancelLOS { refCode = refCode, userName = actor };
                    var lms = await _api.PostJsonAsync("c100", "/v2/SgFinance/CancelContractToLMS", cancelLOS,
                                                       $"ยกเลิกสัญญาข้ามวัน [{code}] โดย {actor}");

                    if (!lms.Reached)
                    {
                        steps.Failed(stepLms, "ติดต่อระบบสินเชื่อไม่ได้ — " + lms.TransportError);
                        return BuildCancelResult(code, steps, lms.TransportError);
                    }
                    if (!lms.IsSuccess)
                    {
                        // เดิมจุดนี้ return ข้อความดิบออกไปเฉย ๆ สถานะจึงไม่เคยถูกอัปเดต
                        // และหน้าจอ CCO ยังแสดงสถานะเดิมทั้งที่งานใน K2 ถูกยกเลิกไปแล้ว
                        steps.Failed(stepLms, DescribeError(lms.Body));
                        return BuildCancelResult(code, steps, lms.Body);
                    }
                    steps.Ok(stepLms, "เลขที่สัญญา " + accountNo);
                }

                // ---- 6. อัปเดตสถานะใบคำขอเป็น CANCELLED ----
                var stepCancel = steps.Begin("ยกเลิกใบคำขอในระบบ (อัปเดตสถานะเป็น CANCELLED)", irreversible: true);
                CancelSpRow cancelRow;
                using (var connection = new SqlConnection(strConnString))
                {
                    cancelRow = (await connection.QueryAsync<CancelSpRow>(new CommandDefinition(
                        $"{DATABASEK2}.[CancelApplication_CLOSED]",
                        new
                        {
                            ApplicationCode = _GetApplicationRespone.ApplicationCode,
                            Remark = remark,
                            CANCEL_USER = actor
                        },
                        commandType: CommandType.StoredProcedure, commandTimeout: 180))).FirstOrDefault();
                }

                if (cancelRow != null && !string.Equals(cancelRow.Result, "SUCCESS", StringComparison.OrdinalIgnoreCase))
                {
                    ResultDescription = (_GetApplicationRespone.AccountNo ?? "").Trim() + " " + cancelRow.ResultDescription;
                    steps.Failed(stepCancel, cancelRow.ResultDescription);
                    return BuildCancelResult(code, steps, ResultDescription);
                }

                // อ่านสถานะกลับมายืนยันว่าเปลี่ยนจริง ไม่เชื่อผลจาก SP อย่างเดียว
                // เพราะ SP มีเงื่อนไขคัดกรองอยู่ข้างใน (ต้องมีเลขที่สัญญา สัญญาต้อง ACTIVE ฯลฯ)
                // ถ้าใบคำขอไม่เข้าเงื่อนไข SP จะไม่คืนแถวใด ๆ กลับมาโดยไม่แจ้งอะไรเลย
                // เดิมกรณีนี้จะถือว่าผ่านทั้งที่สถานะไม่ได้เปลี่ยน — เป็นที่มาของอาการ "ยกเลิกแล้วหน้าจอไม่อัปเดต"
                string statusAfter;
                using (var connection = new SqlConnection(strConnString))
                {
                    statusAfter = (await connection.QueryAsync<string>(new CommandDefinition(
                        $"SELECT ApplicationStatusID FROM {DATABASEK2}.[Application] WITH (NOLOCK) WHERE ApplicationCode = @ApplicationCode",
                        new { ApplicationCode = _GetApplicationRespone.ApplicationCode },
                        commandTimeout: 60))).FirstOrDefault() ?? "";
                }

                if (!string.Equals(statusAfter.Trim(), "CANCELLED", StringComparison.OrdinalIgnoreCase))
                {
                    ResultDescription = cancelRow?.ResultDescription
                        ?? $"ใบคำขอนี้ไม่เข้าเงื่อนไขการยกเลิกแบบข้ามวัน (สถานะยังเป็น {statusAfter.Trim()})";
                    steps.Failed(stepCancel, ResultDescription);
                    return BuildCancelResult(code, steps, ResultDescription);
                }
                steps.Ok(stepCancel);

                // ---- 7. ยกเลิกกรมธรรม์ประกันภัย (SGB) ----
                var stepSgb = steps.Begin("ยกเลิกกรมธรรม์ประกันภัย");
                if (string.IsNullOrEmpty(_GetApplicationRespone.appIns))
                {
                    steps.Skipped(stepSgb, "ใบคำขอนี้ไม่มีกรมธรรม์ประกันภัยผูกอยู่");
                }
                else
                {
                    await SGBCancel(_GetApplication);
                    steps.Ok(stepSgb);
                }

                // ---- 8. แจ้งสถานะกลับไปยัง e-contract ----
                var stepNotify = steps.Begin("แจ้งสถานะไปยัง e-contract");
                var requestBody = new
                {
                    applicationCode = _GetApplicationRespone.ApplicationCode,
                    applicationStatus = "CANCELLED",
                    approvalStatus = "CANCELLED",
                    approvalDatetime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    remark = remark
                };

                var notify = await _api.PostJsonAsync("esig", "/sgesig/Service/C100_Status", requestBody,
                                                      $"แจ้งยกเลิกข้ามวัน [{code}] โดย {actor}");

                const string renotifyHint = " — ใบคำขอถูกยกเลิกเรียบร้อยแล้ว แจ้งซ้ำได้ที่จรวดในคอลัมน์สถานะ หน้ารายการค้นหา";

                if (!notify.Reached)
                {
                    steps.Failed(stepNotify, "ติดต่อ e-contract ไม่ได้" + renotifyHint);
                    await RecordCancelNotifyAsync(code, false, notify.TransportError);
                }
                else if (!notify.IsSuccess)
                {
                    steps.Failed(stepNotify, DescribeError(notify.Body) + renotifyHint);
                    await RecordCancelNotifyAsync(code, false, notify.Body);
                }
                else
                {
                    steps.Ok(stepNotify);
                    await RecordCancelNotifyAsync(code, true, null);
                }

                return BuildCancelResult(code, steps, ResultDescription);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ยกเลิกใบคำขอ (ข้ามวัน) ไม่สำเร็จ: {Code}", code);
                var current = steps.Steps.LastOrDefault();
                if (current != null && current.Status == "pending")
                {
                    steps.Failed(current, ex.Message);
                }
                return BuildCancelResult(code, steps, ex.Message);
            }
        }

        private sealed class LoanTypeCateRow
        {
            public string? loanTypeCate { get; set; }
            public string? AccountNo { get; set; }
        }

        private sealed class CancelSpRow
        {
            public string? Result { get; set; }
            public string? ResultDescription { get; set; }
        }

        /// <summary>ดึงข้อความที่ปลายทางตอบกลับมาให้อ่านรู้เรื่อง ถ้าแกะไม่ได้ก็คืนตัวดิบไป</summary>
        private static string DescribeError(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return "ระบบปลายทางไม่รับรายการนี้";
            try
            {
                var parsed = JsonConvert.DeserializeObject<ModelResult>(body);
                if (!string.IsNullOrWhiteSpace(parsed?.message)) return parsed.message;
            }
            catch { /* ปลายทางไม่ได้ตอบเป็น JSON — ใช้ตัวดิบ */ }
            return body;
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

        [RequireLogin]
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
            catch (WebException wex)
            {
                // ปลายทางตอบ 500 มาพร้อมรายละเอียดใน body แต่ GetResponse() โยน exception ทิ้งไป
                // ทำให้เดิมเห็นแค่ "(500) Internal Server Error" ไล่ต่อไม่ได้ว่าเพราะอะไร
                string detail = "";
                try
                {
                    using var errStream = wex.Response?.GetResponseStream();
                    if (errStream != null)
                    {
                        using var rd = new StreamReader(errStream);
                        detail = rd.ReadToEnd();
                    }
                }
                catch { /* อ่าน body ไม่ได้ก็ใช้ข้อความ exception ตามเดิม */ }

                _MessageModel.StatusCode = "500";
                _MessageModel.Message = string.IsNullOrWhiteSpace(detail) ? wex.Message : wex.Message + " | " + detail;
                Log.Error("WorkflowGoToCancelRequest Fail : {Message} | {Detail}", wex.Message, detail);
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
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [RequireLogin]
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
                    // เดิม log ทั้ง DataTable ซึ่งมีเลขบัตรประชาชน/เบอร์โทรของลูกค้าลงไฟล์ (ระดับ Debug เปิดบน prod)
                    Log.Debug("query returned {RowCount} row(s)", dt.Rows.Count);

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

        [InvalidateSearchCache]
        [RequireLogin]
        [HttpPost]
        public async Task<IActionResult> GetStatusClosedSGFinance([FromBody] C100StatusRq _C100StatusRq)
        {
            var actor = HttpContext.Session.GetString("EMP_CODE");
            var code = _C100StatusRq?.ApplicationCode;
            Log.Information("ส่งสถานะ CLOSED ซ้ำ: {Code} โดย {Actor}", code, actor);

            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest(ActionResultDto.Fail("ไม่พบเลขที่ใบคำขอ กรุณาค้นหาใหม่อีกครั้ง"));
            }

            try
            {
                // ดึง request เดิมที่เคยส่งไปปลายทางจาก log แล้วส่งซ้ำ (SP เป็นตัวหาให้)
                StatusReplayRow row;
                using (var connection = new SqlConnection(strConnString))
                {
                    row = (await connection.QueryAsync<StatusReplayRow>(
                        new CommandDefinition($"{DATABASEK2}.[GetStatusClosedSGFinance]",
                            new { ApplicationCode = code },
                            commandType: CommandType.StoredProcedure,
                            commandTimeout: 60))).FirstOrDefault();
                }

                if (row == null || string.IsNullOrWhiteSpace(row.StatusDesc))
                {
                    // เดิมกรณีนี้เงียบสนิท ผู้ใช้กดแล้วไม่มีอะไรเกิดขึ้นและไม่รู้ว่าทำไม
                    return NotFound(ActionResultDto.Fail(
                        $"ใบคำขอ {code} ไม่เคยส่งสถานะไปยังระบบสินเชื่อมาก่อน จึงไม่มีรายการให้ส่งซ้ำ"));
                }

                var original = JsonConvert.DeserializeObject<requestBodyValue>(row.StatusDesc);

                var requestBody = new
                {
                    applicationCode = original.applicationCode,
                    applicationStatus = original.applicationStatus,
                    approvalStatus = original.approvalStatus,
                    approvalDatetime = original.approvalDatetime,
                    remark = "",
                    losApplicationCode = original.applicationCode,
                    contractNo = row.Accountno
                };

                var response = await _api.PostJsonAsync("c100", "/v2/SgFinance/C100_Status", requestBody,
                                                        $"ส่งสถานะ CLOSED ซ้ำ [{code}] โดย {actor}");

                if (!response.Reached)
                {
                    return StatusCode(StatusCodes.Status502BadGateway, ActionResultDto.Fail(
                        "ตอนนี้ติดต่อระบบสินเชื่อไม่ได้ กรุณาลองใหม่อีกครั้งในอีกสักครู่", response.TransportError));
                }

                if (!response.IsSuccess)
                {
                    // เดิมกรณีนี้คืน object ว่างพร้อม HTTP 200 หน้าจอจึงขึ้น [object Object]
                    return StatusCode(StatusCodes.Status502BadGateway, ActionResultDto.Fail(
                        "ระบบสินเชื่อไม่รับรายการนี้ สถานะจึงยังไม่ถูกอัปเดต", response.Body));
                }

                return Ok(ActionResultDto.Success($"อัปเดตสถานะใบคำขอ {code} ไปยังระบบสินเชื่อแล้ว", response.Body));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ส่งสถานะ CLOSED ซ้ำไม่สำเร็จ: {Code}", code);
                return StatusCode(StatusCodes.Status500InternalServerError, ActionResultDto.Fail(
                    "ทำรายการไม่สำเร็จ กรุณาลองใหม่อีกครั้ง หากยังไม่ได้ให้แจ้งทีมผู้ดูแล", ex.Message));
            }
        }

        /// <summary>แถวที่ SP คืนมา — StatusDesc คือ request เดิมที่เคยส่งไปปลายทาง (JSON)</summary>
        private class StatusReplayRow
        {
            public string? StatusDesc { get; set; }
            public string? Accountno { get; set; }
        }

        /// <summary>
        /// แจ้งยกเลิกไปยัง e-contract ซ้ำ
        ///
        /// ใช้เมื่อยกเลิกใบคำขอสำเร็จแล้วแต่ขั้นสุดท้าย (แจ้ง e-contract) ล้มเหลว
        /// ทำให้ใบคำขอเป็น CANCELLED ในระบบ แต่ e-contract ยังเห็นสถานะเดิม
        /// เดิมไม่มีทางแจ้งซ้ำ ต้องรอให้คนไปแก้ให้ทีละใบ
        /// </summary>
        [InvalidateSearchCache]
        [RequireLogin]
        [HttpPost]
        public async Task<IActionResult> RenotifyCancel([FromBody] C100StatusRq request)
        {
            var actor = HttpContext.Session.GetString("EMP_CODE");
            var code = request?.ApplicationCode;

            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest(ActionResultDto.Fail("ไม่พบเลขที่ใบคำขอ กรุณาค้นหาใหม่อีกครั้ง"));
            }

            // อ่านสถานะจริงก่อนส่ง กันกรณีหน้าจอค้างอยู่กับข้อมูลเก่า
            // แล้วเผลอแจ้ง e-contract ว่ายกเลิก ทั้งที่ใบคำขอยังไม่ได้ถูกยกเลิก
            string status;
            using (var connection = new SqlConnection(strConnString))
            {
                status = (await connection.QueryAsync<string>(new CommandDefinition(
                    $"SELECT ApplicationStatusID FROM {DATABASEK2}.[Application] WITH (NOLOCK) WHERE ApplicationCode = @ApplicationCode",
                    new { ApplicationCode = code }, commandTimeout: 60))).FirstOrDefault() ?? "";
            }

            if (string.IsNullOrWhiteSpace(status))
            {
                return NotFound(ActionResultDto.Fail($"ไม่พบใบคำขอ {code} ในระบบ"));
            }

            if (!string.Equals(status.Trim(), "CANCELLED", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(ActionResultDto.Fail(
                    $"ใบคำขอ {code} ยังไม่ได้ถูกยกเลิก (สถานะปัจจุบันคือ {status.Trim()}) จึงยังแจ้งยกเลิกไม่ได้"));
            }

            var requestBody = new
            {
                applicationCode = code,
                applicationStatus = "CANCELLED",
                approvalStatus = "CANCELLED",
                approvalDatetime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                remark = "แจ้งยกเลิกซ้ำจากหน้าตรวจสอบใบคำขอ"
            };

            var response = await _api.PostJsonAsync("esig", "/sgesig/Service/C100_Status", requestBody,
                                                    $"แจ้งยกเลิกซ้ำ [{code}] โดย {actor}");

            if (!response.Reached)
            {
                await RecordCancelNotifyAsync(code, false, response.TransportError);
                return StatusCode(StatusCodes.Status502BadGateway, ActionResultDto.Fail(
                    "ตอนนี้ติดต่อ e-contract ไม่ได้ กรุณาลองใหม่อีกครั้งในอีกสักครู่", response.TransportError));
            }

            if (!response.IsSuccess)
            {
                await RecordCancelNotifyAsync(code, false, response.Body);
                return StatusCode(StatusCodes.Status502BadGateway, ActionResultDto.Fail(
                    "e-contract ไม่รับรายการนี้ สถานะฝั่งสัญญาจึงยังไม่ถูกอัปเดต", response.Body));
            }

            await RecordCancelNotifyAsync(code, true, null);
            return Ok(ActionResultDto.Success($"แจ้งยกเลิกใบคำขอ {code} ไปยัง e-contract แล้ว", response.Body));
        }

        [InvalidateSearchCache]
        [RequireLogin]
        [HttpPost]
        public async Task<IActionResult> GenEsignature([FromBody] C100StatusRq _C100StatusRq)
        {
            var actor = HttpContext.Session.GetString("EMP_CODE");
            var code = _C100StatusRq?.ApplicationCode;
            Log.Information("สร้างลิงก์ e-signature ใหม่: {Code} โดย {Actor}", code, actor);

            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest(ActionResultDto.Fail("ไม่พบเลขที่ใบคำขอ กรุณาค้นหาใหม่อีกครั้ง"));
            }

            try
            {
                var response = await _api.PostJsonAsync("posservice", "/v1/LOS/SGF_ReCreateESig",
                    new { applicationCode = code },
                    $"สร้างลิงก์ e-signature ใหม่ [{code}] โดย {actor}");

                if (!response.Reached)
                {
                    return StatusCode(StatusCodes.Status502BadGateway, ActionResultDto.Fail(
                        "ตอนนี้ติดต่อ e-contract ไม่ได้ กรุณาลองใหม่อีกครั้งในอีกสักครู่", response.TransportError));
                }

                if (!response.IsSuccess)
                {
                    return StatusCode(StatusCodes.Status502BadGateway, ActionResultDto.Fail(
                        "e-contract ไม่รับรายการนี้ ลิงก์ลงนามใหม่จึงยังไม่ถูกสร้าง", response.Body));
                }

                return Ok(ActionResultDto.Success(
                    $"สร้างลิงก์ลงนามใหม่ให้ใบคำขอ {code} แล้ว ลูกค้าจะได้รับลิงก์ใหม่", response.Body));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "สร้างลิงก์ e-signature ใหม่ไม่สำเร็จ: {Code}", code);
                return StatusCode(StatusCodes.Status500InternalServerError, ActionResultDto.Fail(
                    "ทำรายการไม่สำเร็จ กรุณาลองใหม่อีกครั้ง หากยังไม่ได้ให้แจ้งทีมผู้ดูแล", ex.Message));
            }
        }

        [RequireLogin]
        [InvalidateSearchCache]
        [HttpPost]
        public async Task<IActionResult> GetAddTNewSalesNewSGFinance([FromBody] C100StatusRq _C100StatusRq)
        {
            var actor = HttpContext.Session.GetString("EMP_CODE");
            var code = _C100StatusRq?.ApplicationCode;
            Log.Information("ส่ง NewSale ซ้ำ: {Code} โดย {Actor}", code, actor);

            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest(ActionResultDto.Fail("ไม่พบเลขที่ใบคำขอ กรุณาค้นหาใหม่อีกครั้ง"));
            }

            try
            {
                // SP ดึง "คำขอเดิมที่เคยส่งไปปลายทาง" ออกมาจาก log เพื่อส่งซ้ำ
                // ต้องระบุชนิดที่มีชื่อคอลัมน์ — SP คืนหลายคอลัมน์ (OrderID, StatusCode, StatusDesc)
                // ถ้าใช้ Query<string> Dapper จะหยิบคอลัมน์แรกคือ OrderID ไม่ใช่ payload ที่ต้องการ
                string statusDesc;
                using (var connection = new SqlConnection(strConnString))
                {
                    statusDesc = (await connection.QueryAsync<StatusReplayRow>(new CommandDefinition(
                        $"{DATABASEK2}.[GetSendEsignatureStatusSGFinance]",
                        new { ApplicationCode = code },
                        commandType: CommandType.StoredProcedure,
                        commandTimeout: 60))).FirstOrDefault()?.StatusDesc;
                }

                if (string.IsNullOrWhiteSpace(statusDesc))
                {
                    return NotFound(ActionResultDto.Fail(
                        $"ใบคำขอ {code} ยังไม่มีรายการขายที่ยืนยันการรับสินค้าแล้ว จึงยังส่งซ้ำไม่ได้"));
                }

                var original = JsonConvert.DeserializeObject<GetSendEsignatureStatusSGFinance>(statusDesc);

                // เดิมโค้ดใส่ ApplicationCode ทับลงไปในทั้ง 4 ฟิลด์ ปลายทางจึงได้ค่าขยะ
                // เช่น EsignatureConfirmStatus = "911-2502-00086" แทนที่จะเป็น "TRUE"
                // ที่ถูกคือส่งค่าเดิมกลับไปตามที่ SP ดึงมา
                var requestBody = new
                {
                    ApplicationCode = original.ApplicationCode,
                    EsignatureConfirmStatus = original.EsignatureConfirmStatus,
                    EsignatureConfirmDate = original.EsignatureConfirmDate,
                    ReceiveConfirmStatus = original.ReceiveConfirmStatus,
                    ReceiveConfirmDate = original.ReceiveConfirmDate
                };

                var response = await _api.PostJsonAsync("esig", "/sgesig/api/v1/SendEsignatureStatus",
                    requestBody, $"ส่ง NewSale ซ้ำ [{code}] โดย {actor}");

                if (!response.Reached)
                {
                    return StatusCode(StatusCodes.Status502BadGateway, ActionResultDto.Fail(
                        "ตอนนี้ติดต่อ e-contract ไม่ได้ กรุณาลองใหม่อีกครั้งในอีกสักครู่", response.TransportError));
                }

                if (!response.IsSuccess)
                {
                    return StatusCode(StatusCodes.Status502BadGateway, ActionResultDto.Fail(
                        "e-contract ไม่รับรายการนี้ รายการขายจึงยังไม่ถูกส่ง", response.Body));
                }

                return Ok(ActionResultDto.Success(
                    $"ส่งรายการขายของใบคำขอ {code} ไปยัง e-contract แล้ว", response.Body));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ส่ง NewSale ซ้ำไม่สำเร็จ: {Code}", code);
                return StatusCode(StatusCodes.Status500InternalServerError, ActionResultDto.Fail(
                    "ทำรายการไม่สำเร็จ กรุณาลองใหม่อีกครั้ง หากยังไม่ได้ให้แจ้งทีมผู้ดูแล", ex.Message));
            }
        }

        [RequireLogin]
        [InvalidateSearchCache]
        [HttpPost]
        public async Task<IActionResult> RegisIMEI([FromBody] GetApplication _GetApplication)
        {
            var actor = HttpContext.Session.GetString("EMP_CODE");
            var code = _GetApplication?.ApplicationCode;
            Log.Information("ลงทะเบียนเครื่อง: {Code} โดย {Actor}", code, actor);

            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest(ActionResultDto.Fail("ไม่พบเลขที่ใบคำขอ กรุณาค้นหาใหม่อีกครั้ง"));
            }

            try
            {
                var app = await GetApplication(_GetApplication);
                if (app == null || string.IsNullOrWhiteSpace(app.ProductSerialNo))
                {
                    return NotFound(ActionResultDto.Fail(
                        $"ใบคำขอ {code} ยังไม่มีหมายเลขเครื่อง (Serial/IMEI) จึงยังลงทะเบียนไม่ได้"));
                }

                var response = await _api.PostJsonAsync("esig", "/sgesig/Service/RegisIMEI", new
                {
                    SerrialNo = app.ProductSerialNo,
                    APPLICATION_CODE = app.ApplicationCode,
                    Brand = app.ProductBrandName
                }, $"ลงทะเบียนเครื่อง [{code}] โดย {actor}");

                if (!response.Reached)
                {
                    return StatusCode(StatusCodes.Status502BadGateway, ActionResultDto.Fail(
                        "ตอนนี้ติดต่อระบบลงทะเบียนเครื่องไม่ได้ กรุณาลองใหม่อีกครั้งในอีกสักครู่", response.TransportError));
                }

                if (!response.IsSuccess)
                {
                    return StatusCode(StatusCodes.Status502BadGateway, ActionResultDto.Fail(
                        "ระบบลงทะเบียนเครื่องไม่รับรายการนี้ เครื่องจึงยังไม่ถูกลงทะเบียน", response.Body));
                }

                // ปลายทางตอบ HTTP 200 เสมอ ไม่ว่างานจะสำเร็จหรือไม่ ผลจริงอยู่ในฟิลด์ statusCode
                // ซึ่งเป็น "ข้อความ JSON ซ้อนอยู่ข้างใน" อีกชั้น ต้องแกะออกมาถึงจะรู้ผล
                var outcome = ParseRegisResult(response.Body);

                if (!outcome.Ok)
                {
                    // ไม่ใช่ข้อผิดพลาดของระบบ แต่เป็น "ทำรายการไม่สำเร็จ" ที่ปลายทางบอกเหตุผลมา
                    return Ok(ActionResultDto.Fail(outcome.Message, response.Body));
                }

                // งานต่อพ่วงหลังลงทะเบียนสำเร็จ — ล้มเหลวตรงนี้ไม่ทำให้การลงทะเบียนเป็นโมฆะ
                await RunPostRegisStepsAsync(_GetApplication, app);

                return Ok(ActionResultDto.Success(outcome.Message, response.Body));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ลงทะเบียนเครื่องไม่สำเร็จ: {Code}", code);
                return StatusCode(StatusCodes.Status500InternalServerError, ActionResultDto.Fail(
                    "ทำรายการไม่สำเร็จ กรุณาลองใหม่อีกครั้ง หากยังไม่ได้ให้แจ้งทีมผู้ดูแล", ex.Message));
            }
        }

        /// <summary>
        /// ซ่อมรายการที่มีสัญญาซ้ำ — เปลี่ยนเลขที่เอกสารของใบที่ยังไม่ลงนามเสร็จ (signedStatus ไม่ใช่ COMP-Done)
        /// ให้เติม _D ต่อท้าย เพื่อให้เหลือสัญญาที่ผูกกับใบคำขอเพียงใบเดียว
        ///
        /// ข้อควรระวังที่เจอจากข้อมูลจริง: ถ้าเติม _D ให้ทุกแถวพร้อมกัน แถวเหล่านั้นจะได้ชื่อเดียวกันหมด
        /// แล้วกลายเป็นรายการซ้ำชุดใหม่ (เกิดขึ้นแล้วกับ 936-2607-02033_D และ 881-2607-00006_D)
        /// จึงต้องไล่เลขต่อท้ายให้ไม่ชนกัน และตรวจก่อนว่าชื่อใหม่ยังไม่มีใครใช้
        /// </summary>
        [RequireLogin]
        [InvalidateSearchCache]
        [HttpPost]
        public async Task<IActionResult> FixDuplicateContract([FromBody] C100StatusRq request)
        {
            var actor = HttpContext.Session.GetString("EMP_CODE");
            var code = request?.ApplicationCode;
            Log.Information("ซ่อมสัญญาซ้ำ: {Code} โดย {Actor}", code, actor);

            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest(ActionResultDto.Fail("ไม่พบเลขที่ใบคำขอ กรุณาค้นหาใหม่อีกครั้ง"));
            }

            // รับเฉพาะตัวอักษร ตัวเลข ขีดกลาง และขีดล่าง — ค่านี้ถูกนำไปประกอบเป็นคำสั่ง SQL
            if (!System.Text.RegularExpressions.Regex.IsMatch(code, @"^[A-Za-z0-9_\-]{1,60}$"))
            {
                return BadRequest(ActionResultDto.Fail("เลขที่ใบคำขอไม่ถูกต้อง"));
            }

            try
            {
                using var connection = new SqlConnection(strConnString);
                await connection.OpenAsync();

                // ใช้ค่าคงที่แทนพารามิเตอร์ เพราะเงื่อนไขแบบพารามิเตอร์ส่งข้ามเซิร์ฟเวอร์ไปไม่ได้
                // (code ผ่านการตรวจรูปแบบมาแล้วด้านบน)
                var rows = (await connection.QueryAsync<DuplicateContractRow>(new CommandDefinition($@"
                    SELECT id, documentno, signedStatus, createdAt
                    FROM {SGCESIGNATURE}.[contracts] WITH (NOLOCK)
                    WHERE documentno = N'{code}'
                    ORDER BY createdAt DESC, id DESC", commandTimeout: 180))).ToList();

                if (rows.Count <= 1)
                {
                    return Ok(ActionResultDto.Fail(
                        $"ใบคำขอ {code} มีสัญญาเพียงใบเดียว ไม่ได้ซ้ำ จึงไม่ต้องแก้ไข"));
                }

                // แถวที่ต้องเปลี่ยนชื่อ = ที่ยังไม่ลงนามเสร็จ
                var toRename = rows.Where(r => !string.Equals(r.signedStatus, "COMP-Done", StringComparison.OrdinalIgnoreCase)).ToList();

                if (toRename.Count == 0)
                {
                    return Ok(ActionResultDto.Fail(
                        $"ใบคำขอ {code} มีสัญญาที่ลงนามเสร็จแล้ว {rows.Count} ใบ ระบบไม่แก้ให้อัตโนมัติ กรุณาให้ทีมผู้ดูแลตรวจสอบก่อน"));
                }

                // ถ้าจะเปลี่ยนชื่อทุกใบ จะไม่เหลือสัญญาผูกกับใบคำขอเลย — เก็บใบล่าสุดไว้หนึ่งใบ
                if (toRename.Count == rows.Count)
                {
                    toRename = toRename.Skip(1).ToList();
                }

                var used = await ExistingSuffixesAsync(connection, code);
                var renamed = new List<string>();
                foreach (var row in toRename)
                {
                    var newNo = NextFreeDocumentNo(used, code);

                    var affected = await connection.ExecuteAsync(new CommandDefinition($@"
                        UPDATE {SGCESIGNATURE}.[contracts]
                        SET documentno = N'{newNo}'
                        WHERE id = {row.id} AND documentno = N'{code}'", commandTimeout: 180));

                    if (affected == 1)
                    {
                        renamed.Add($"id {row.id} ({row.signedStatus}) → {newNo}");
                        Log.Information("ซ่อมสัญญาซ้ำ: {Code} id={Id} status={Status} เปลี่ยนเป็น {NewNo} โดย {Actor}",
                            code, row.id, row.signedStatus, newNo, actor);
                    }
                }

                if (renamed.Count == 0)
                {
                    return Ok(ActionResultDto.Fail("ไม่มีอะไรถูกแก้ไข ข้อมูลอาจถูกแก้ไปแล้วโดยคนอื่นระหว่างนี้ กรุณาค้นหาใหม่อีกครั้ง"));
                }

                return Ok(ActionResultDto.Success(
                    $"แก้สัญญาซ้ำของใบคำขอ {code} แล้ว — ย้ายสัญญาที่ยังไม่ลงนาม {renamed.Count} ใบออกไป เหลือสัญญาที่ใช้งานจริง 1 ใบ",
                    string.Join(" · ", renamed)));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ซ่อมสัญญาซ้ำไม่สำเร็จ: {Code}", code);
                return StatusCode(StatusCodes.Status500InternalServerError, ActionResultDto.Fail(
                    "แก้สัญญาซ้ำไม่สำเร็จ กรุณาลองใหม่อีกครั้ง หากยังไม่ได้ให้แจ้งทีมผู้ดูแล", ex.Message));
            }
        }

        /// <summary>
        /// อ่านว่าเลขที่เอกสารแบบ {code}_D, _D2, ... ตัวไหนถูกใช้ไปแล้วบ้าง — ครั้งเดียวจบ
        ///
        /// ต้องเขียนค่าลงไปในคำสั่งตรง ๆ แทนการใช้พารามิเตอร์ เพราะตาราง contracts อยู่คนละเซิร์ฟเวอร์
        /// และเงื่อนไขที่เป็นพารามิเตอร์จะถูกส่งข้ามไปประมวลผลฝั่งโน้นไม่ได้ กลายเป็นลากตารางทั้งก้อน
        /// (1.76 ล้านแถว) มาจนหมดเวลา — ส่วนค่าคงที่ส่งข้ามไปได้ ใช้เวลาไม่ถึงวินาที
        ///
        /// ปลอดภัยเพราะ code ถูกตรวจรูปแบบก่อนแล้วว่าเป็นตัวอักษร/ตัวเลข/ขีดเท่านั้น
        /// </summary>
        private async Task<HashSet<string>> ExistingSuffixesAsync(SqlConnection connection, string code)
        {
            var candidates = Enumerable.Range(1, 50)
                .Select(i => i == 1 ? $"{code}_D" : $"{code}_D{i}")
                .ToList();

            var inList = string.Join(",", candidates.Select(c => $"N'{c}'"));

            var used = await connection.QueryAsync<string>(new CommandDefinition($@"
                SELECT documentno FROM {SGCESIGNATURE}.[contracts] WITH (NOLOCK)
                WHERE documentno IN ({inList})", commandTimeout: 180));

            return new HashSet<string>(used, StringComparer.OrdinalIgnoreCase);
        }

        private static string NextFreeDocumentNo(HashSet<string> used, string code)
        {
            for (int i = 1; i <= 50; i++)
            {
                var candidate = i == 1 ? $"{code}_D" : $"{code}_D{i}";
                if (used.Add(candidate)) return candidate;
            }
            throw new InvalidOperationException($"หาเลขที่เอกสารว่างสำหรับ {code} ไม่ได้");
        }

        private class DuplicateContractRow
        {
            public long id { get; set; }
            public string? documentno { get; set; }
            public string? signedStatus { get; set; }
            // คอลัมน์นี้เป็น datetimeoffset ไม่ใช่ datetime ธรรมดา
            public DateTimeOffset? createdAt { get; set; }
        }

        /// <summary>
        /// แกะผลจริงออกจากคำตอบของระบบลงทะเบียนเครื่อง
        ///
        /// คำตอบมีหน้าตาแบบนี้ — ผลจริงถูกใส่เป็นข้อความ JSON ซ้อนอยู่ในฟิลด์ statusCode อีกชั้น
        ///   {"statusCode":"{\"status\":\"401\",\"message\":\"... ยังทำรายการไม่สมบูรณ์\"}"}
        /// และปลายทางตอบ HTTP 200 เสมอ ไม่ว่าจะสำเร็จหรือไม่
        /// </summary>
        private static (bool Ok, string Message) ParseRegisResult(string body)
        {
            try
            {
                var outer = JObject.Parse(body);
                var inner = outer["statusCode"]?.ToString();
                if (string.IsNullOrWhiteSpace(inner))
                {
                    return (false, "ระบบลงทะเบียนเครื่องตอบกลับมาในรูปแบบที่ไม่คาดคิด กรุณาแจ้งทีมผู้ดูแล");
                }

                // บางกรณีปลายทางส่งเป็นข้อความสั้น ๆ ไม่ใช่ JSON
                if (!inner.TrimStart().StartsWith("{"))
                {
                    var ok = inner.Equals("PASS", StringComparison.OrdinalIgnoreCase);
                    return (ok, ok ? "ลงทะเบียนเครื่องสำเร็จ" : $"ลงทะเบียนไม่สำเร็จ: {inner}");
                }

                var detail = JObject.Parse(inner);
                var status = detail["status"]?.ToString() ?? "";
                var message = detail["message"]?.ToString();
                var deviceStatus = detail["deviceStatus"]?.ToString();

                bool success = status.StartsWith("2", StringComparison.Ordinal);

                if (string.IsNullOrWhiteSpace(message))
                {
                    message = success ? "ลงทะเบียนเครื่องสำเร็จ" : "ลงทะเบียนไม่สำเร็จ";
                }
                else if (!success)
                {
                    message = $"ลงทะเบียนไม่สำเร็จ: {message}";
                }

                if (!string.IsNullOrWhiteSpace(deviceStatus))
                {
                    message += $" · สถานะเครื่อง: {deviceStatus}";
                }

                return (success, message);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "อ่านคำตอบของระบบลงทะเบียนเครื่องไม่ได้: {Body}", body);
                return (false, "ระบบลงทะเบียนเครื่องตอบกลับมาในรูปแบบที่ไม่คาดคิด กรุณาแจ้งทีมผู้ดูแล");
            }
        }

        /// <summary>งานต่อพ่วงหลังลงทะเบียนเครื่องสำเร็จ (OPPO ต้องส่งข้อมูลสินเชื่อ / SEAMLESS ต้องเช็คซ้ำ)</summary>
        private async Task RunPostRegisStepsAsync(GetApplication request, GetApplicationRespone app)
        {
            try
            {
                if (string.Equals(app.ProductBrandName?.Trim(), "OPPO", StringComparison.OrdinalIgnoreCase))
                {
                    float.TryParse(app.Cash, out var cash);
                    float.TryParse(app.DownPayment, out var down);

                    await LendingInfo(new LendingInfoRq
                    {
                        ApplicationCode = request.ApplicationCode,
                        application_date = app.ApplicationDate,
                        product_serial = app.ProductSerialNo,
                        flat_rate = app.InterestPercent,
                        cash_price = app.Cash,
                        down_payment = "",
                        down_amount = app.DownPayment?.ToString(),
                        new_loan = (cash - down).ToString(),
                        contract_term = app.InstallmentPeriod?.ToString(),
                        discount = app.Discount
                    });
                }

                if (string.Equals(app.ApplicationRef?.Trim(), "SEAMLESS", StringComparison.OrdinalIgnoreCase))
                {
                    // เดิมเป็น async void แล้วเรียกแบบไม่รอผล ถ้าพังจะหลุดออกนอก request และทำให้ระบบล้มได้
                    await CheckRegisterIMEI(new CheckRegisterIMEIRq { AppOrderNo = request.ApplicationCode });
                }
            }
            catch (Exception ex)
            {
                // ลงทะเบียนสำเร็จไปแล้ว งานต่อพ่วงล้มไม่ควรทำให้ผู้ใช้เข้าใจว่าลงทะเบียนไม่สำเร็จ
                Log.Error(ex, "งานต่อพ่วงหลังลงทะเบียนเครื่องไม่สำเร็จ: {Code}", request.ApplicationCode);
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

        [InvalidateSearchCache]
        [RequireLogin]
        [HttpPost]
        [Route("CheckRegisterIMEI")]
        public async Task CheckRegisterIMEI(CheckRegisterIMEIRq checkRegisterIMEIRq)
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
                    // เดิม log ทั้ง DataTable ซึ่งมีเลขบัตรประชาชน/เบอร์โทรของลูกค้าลงไฟล์ (ระดับ Debug เปิดบน prod)
                    Log.Debug("query returned {RowCount} row(s)", dt.Rows.Count);

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

        [InvalidateSearchCache]
        [RequireLogin]
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

        [InvalidateSearchCache]
        [RequireLogin]
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

        [RequireLogin]
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

        [InvalidateSearchCache]
        [RequireLogin]
        [HttpPost]
        public async Task<IActionResult> LinkPayment([FromBody] GetApplication _GetApplication)
        {
            var actor = HttpContext.Session.GetString("EMP_CODE");
            var code = _GetApplication?.ApplicationCode;

            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest(ActionResultDto.Fail("ไม่พบเลขที่ใบคำขอ กรุณาค้นหาใหม่อีกครั้ง"));
            }

            var soapRequest = $@"<?xml version=""1.0"" encoding=""utf-8""?>
        <soap12:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap12=""http://www.w3.org/2003/05/soap-envelope"">
          <soap12:Body>
            <GenLinkWithSms xmlns=""http://tempuri.org/"">
              <AppCode>{code}</AppCode>
            </GenLinkWithSms>
          </soap12:Body>
        </soap12:Envelope>";

            // เดิมใช้ HttpClient ตัว static ร่วมกันทั้งคลาส แล้วเพิ่ม Accept header ใหม่ทุกครั้งที่กด
            // header จึงพอกขึ้นเรื่อย ๆ ตามจำนวนครั้งที่ใช้งาน
            var response = await _api.PostRawAsync("poslink", "/WebServiceGenLinkWithSms.asmx?op=GenLinkWithSms",
                                                   soapRequest, "application/soap+xml",
                                                   $"ส่งลิงก์ชำระเงิน [{code}] โดย {actor}");

            if (!response.Reached)
            {
                return StatusCode(StatusCodes.Status502BadGateway, ActionResultDto.Fail(
                    "ตอนนี้ติดต่อระบบส่งลิงก์ชำระเงินไม่ได้ กรุณาลองใหม่อีกครั้งในอีกสักครู่", response.TransportError));
            }

            if (!response.IsSuccess)
            {
                // เดิมกรณีนี้คืน statusCode เป็นข้อความ exception ให้หน้าจอ ซึ่ง FE อ่านไม่ออก
                return StatusCode(StatusCodes.Status502BadGateway, ActionResultDto.Fail(
                    "ระบบส่งลิงก์ชำระเงินไม่รับรายการนี้ ลิงก์จึงยังไม่ถูกส่ง", response.Body));
            }

            return Ok(ActionResultDto.Success($"ส่งลิงก์ชำระเงินของใบคำขอ {code} ให้ลูกค้าทาง SMS แล้ว", response.Body));
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

        [InvalidateSearchCache]
        [RequireLogin]
        [HttpPost]
        public async Task<IActionResult> PostBypassCustomer(BypassCustomer _bypassCustomer)
        {
            var actor = HttpContext.Session.GetString("EMP_CODE");
            _bypassCustomer.empCode = actor;

            var response = await _api.PostJsonAsync("posservice", "/v1/LOS/ByPassCustomer", _bypassCustomer,
                                                    $"ยกเว้นการตรวจสอบลูกค้า [{_bypassCustomer.IdCard}] โดย {actor}");

            return BuildDownstreamResult(response,
                success: $"ยกเว้นการตรวจสอบลูกค้าเลขบัตร {_bypassCustomer.IdCard} เรียบร้อย",
                rejected: "ระบบปลายทางไม่รับรายการนี้ จึงยังไม่ได้ยกเว้นการตรวจสอบ",
                unreachable: "ตอนนี้ติดต่อระบบปลายทางไม่ได้ กรุณาลองใหม่อีกครั้งในอีกสักครู่");
        }

        /// <summary>
        /// แปลงคำตอบจากระบบปลายทางให้เป็นสัญญาเดียวกับปุ่มอื่น ๆ
        ///
        /// ปลายทางกลุ่มนี้ตอบเป็น { status, message } และบางครั้งตอบ HTTP 200 พร้อม status = "BadRequest"
        /// จึงต้องดูทั้ง HTTP status และ status ในเนื้อคำตอบ ไม่งั้นจะรายงานว่าสำเร็จทั้งที่ปลายทางปฏิเสธ
        /// </summary>
        private IActionResult BuildDownstreamResult(Clients.DownstreamResponse response, string success,
                                                    string rejected, string unreachable)
        {
            if (!response.Reached)
            {
                return StatusCode(StatusCodes.Status502BadGateway,
                    ActionResultDto.Fail(unreachable, response.TransportError));
            }

            ModelResult parsed = null;
            try { parsed = JsonConvert.DeserializeObject<ModelResult>(response.Body); } catch { }

            bool accepted = response.IsSuccess
                && (parsed == null || string.IsNullOrWhiteSpace(parsed.status)
                    || string.Equals(parsed.status, "Success", StringComparison.OrdinalIgnoreCase));

            if (!accepted)
            {
                var reason = string.IsNullOrWhiteSpace(parsed?.message) ? rejected : parsed.message;
                return StatusCode(StatusCodes.Status502BadGateway,
                    ActionResultDto.Fail(reason, response.Body));
            }

            return Ok(ActionResultDto.Success(
                string.IsNullOrWhiteSpace(parsed?.message) ? success : parsed.message, response.Body));
        }

        [InvalidateSearchCache]
        [RequireLogin]
        [HttpPost]
        public async Task<IActionResult> PostBypassIMEI(BypassImei _bypassImei)
        {
            var actor = HttpContext.Session.GetString("EMP_CODE");
            _bypassImei.empCode = actor;

            var response = await _api.PostJsonAsync("posservice", "/v1/LOS/ByPassIMEI", _bypassImei,
                                                    $"ยกเว้นการตรวจสอบเครื่อง [{_bypassImei.Imei}] โดย {actor}");

            return BuildDownstreamResult(response,
                success: $"ยกเว้นการตรวจสอบเครื่องหมายเลข {_bypassImei.Imei} เรียบร้อย",
                rejected: "ระบบปลายทางไม่รับรายการนี้ จึงยังไม่ได้ยกเว้นการตรวจสอบเครื่อง",
                unreachable: "ตอนนี้ติดต่อระบบปลายทางไม่ได้ กรุณาลองใหม่อีกครั้งในอีกสักครู่");
        }

        [InvalidateSearchCache]
        [RequireLogin]
        [HttpPost]
        public async Task<IActionResult> PostChangeIMEI(ChangeImei _changeImei)
        {
            var actor = HttpContext.Session.GetString("EMP_CODE");
            _changeImei.empCode = actor;

            var changeIMEI = new ChangeIMEI
            {
                accountNo = _changeImei.accNo,
                originalSerialNo = _changeImei.oldImei,
                newSerialNo = _changeImei.newImei
            };

            var response = await _api.PostJsonAsync("c100", "/v2/SgFinance/ChangeImei", changeIMEI,
                                                    $"เปลี่ยนหมายเลขเครื่อง [{_changeImei.accNo}] โดย {actor}");

            return BuildDownstreamResult(response,
                success: $"เปลี่ยนหมายเลขเครื่องของสัญญา {_changeImei.accNo} เป็น {_changeImei.newImei} เรียบร้อย",
                rejected: "ระบบสินเชื่อไม่รับรายการนี้ หมายเลขเครื่องจึงยังไม่ถูกเปลี่ยน",
                unreachable: "ตอนนี้ติดต่อระบบสินเชื่อไม่ได้ กรุณาลองใหม่อีกครั้งในอีกสักครู่");
        }
    }
}
