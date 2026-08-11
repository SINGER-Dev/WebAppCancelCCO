using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace App.Controllers
{
    /// <summary>
    /// ส่งไฟล์โดยเขียนลงสายส่งทีละส่วน แทนการสร้างไฟล์ทั้งก้อนไว้ในหน่วยความจำก่อนส่ง
    /// </summary>
    public class FileCallbackResult : IActionResult
    {
        private readonly string _contentType;
        private readonly Func<Stream, Task> _writeAsync;

        public FileCallbackResult(string contentType, Func<Stream, Task> writeAsync)
        {
            _contentType = contentType;
            _writeAsync = writeAsync;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            var response = context.HttpContext.Response;
            response.ContentType = _contentType;
            await _writeAsync(response.Body);
        }
    }
}
