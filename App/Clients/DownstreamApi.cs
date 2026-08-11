using System.Text;
using Newtonsoft.Json;
using Serilog;

namespace App.Clients
{
    /// <summary>ผลลัพธ์ดิบจากการเรียกระบบปลายทาง</summary>
    public class DownstreamResponse
    {
        public bool IsSuccess { get; init; }
        public int StatusCode { get; init; }
        public string Body { get; init; } = "";
        /// <summary>มีค่าเมื่อเรียกไม่ถึงปลายทางเลย (timeout / ต่อไม่ติด)</summary>
        public string? TransportError { get; init; }

        public bool Reached => TransportError == null;
    }

    public interface IDownstreamApi
    {
        /// <param name="client">ชื่อปลายทางที่ลงทะเบียนไว้ใน Program.cs — c100 / esig / posservice / sgb</param>
        /// <param name="caller">ชื่อปุ่มที่เรียก ใช้ในการอ่าน log ย้อนหลัง</param>
        Task<DownstreamResponse> PostJsonAsync(string client, string path, object body, string caller,
                                               CancellationToken ct = default);
    }

    public class DownstreamApi : IDownstreamApi
    {
        private readonly IHttpClientFactory _factory;

        public DownstreamApi(IHttpClientFactory factory) => _factory = factory;

        public async Task<DownstreamResponse> PostJsonAsync(string client, string path, object body, string caller,
                                                           CancellationToken ct = default)
        {
            var json = JsonConvert.SerializeObject(body);
            var http = _factory.CreateClient(client);

            // BaseAddress ของบางปลายทางมี path นำหน้าอยู่ (เช่น .../c100) ถ้า path ที่ส่งเข้ามา
            // ขึ้นต้นด้วย "/" HttpClient จะถือว่าเป็น absolute path แล้วตัด path เดิมของ BaseAddress ทิ้ง
            // ตัด "/" นำหน้าออกเพื่อให้ต่อท้าย BaseAddress เสมอ ไม่ว่าผู้เรียกจะเขียนแบบไหน
            var relativePath = path.TrimStart('/');
            var url = new Uri(http.BaseAddress!, relativePath);

            try
            {
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var response = await http.PostAsync(relativePath, content, ct);

                // เดิมเขียน SerializeObject(response.Content.ReadAsStringAsync()) ซึ่ง log ตัว Task
                // ไม่ใช่เนื้อคำตอบ ทำให้ไล่ย้อนหลังไม่ได้เลยว่าปลายทางตอบอะไร
                var responseBody = await response.Content.ReadAsStringAsync(ct);

                Log.Information("{Caller} → {Url} : {Status} | request={Request} | response={Response}",
                    caller, url, (int)response.StatusCode, json, Truncate(responseBody));

                return new DownstreamResponse
                {
                    IsSuccess = response.IsSuccessStatusCode,
                    StatusCode = (int)response.StatusCode,
                    Body = responseBody
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{Caller} → {Url} : เรียกไม่ถึงปลายทาง | request={Request}", caller, url, json);

                return new DownstreamResponse
                {
                    IsSuccess = false,
                    StatusCode = 0,
                    Body = "",
                    TransportError = ex.Message
                };
            }
        }

        private static string Truncate(string s) => s.Length <= 4000 ? s : s[..4000] + "…(ตัดที่ 4000 ตัวอักษร)";
    }
}
