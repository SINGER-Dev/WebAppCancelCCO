using App.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;

namespace App.Filters
{
    /// <summary>
    /// บังคับว่าต้องล็อกอินก่อนถึงจะเรียก action ได้
    ///
    /// เดิม action ที่ยิงไปแก้ข้อมูลปลายทาง (ส่งสถานะ, ลงทะเบียนเครื่อง, ยกเลิกใบคำขอ, bypass ฯลฯ)
    /// ไม่ได้เช็ค session เลย ใครยิง POST เข้ามาตรง ๆ ก็สั่งงานระบบปลายทางได้
    /// ใส่ attribute นี้ที่ action แทนการ copy โค้ดเช็ค session ไปทีละที่
    /// </summary>
    public class RequireLoginAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var empCode = context.HttpContext.Session.GetString("EMP_CODE");
            if (!string.IsNullOrEmpty(empCode))
            {
                return;
            }

            var request = context.HttpContext.Request;

            Log.Warning("ปฏิเสธการเรียก {Path} เพราะยังไม่ได้ล็อกอิน (จาก {RemoteIp})",
                request.Path.Value, context.HttpContext.Connection.RemoteIpAddress?.ToString());

            // เรียกจากหน้าเว็บด้วย AJAX → ตอบ 401 พร้อมข้อความให้ฝั่งหน้าจอเอาไปแสดง/เด้งไป login
            // เปิดหน้าตรง ๆ → ส่งกลับหน้า login ตามเดิม
            bool wantsJson =
                string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
                || (request.Headers.Accept.ToString()?.Contains("application/json", StringComparison.OrdinalIgnoreCase) ?? false)
                || (request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) ?? false);

            if (wantsJson)
            {
                context.Result = new JsonResult(ActionResultDto.Fail("หมดเวลาใช้งานแล้ว กรุณาเข้าสู่ระบบอีกครั้ง"))
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
            }
            else
            {
                context.Result = new RedirectToActionResult("Index", "Login", null);
            }
        }
    }
}
