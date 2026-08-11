using System.Data;
using App.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Serilog;

namespace App.Infrastructure
{
    /// <summary>
    /// อุ่นข้อมูลของ "วันนี้" ไว้ให้ SQL Server เป็นระยะ
    ///
    /// ที่ต้องมีเพราะ: ตาราง Application บน PROD ไม่มี index บนคอลัมน์วันที่ การค้นตามวันที่
    /// จึงต้องกวาดทั้งตาราง (2.8 ล้านแถว) — ถ้าข้อมูลยังอยู่ในหน่วยความจำของ SQL Server จะใช้เวลา
    /// ไม่ถึงวินาที แต่ถ้าหลุดออกไปแล้วจะกลับไปใช้เวลาหลายวินาที ผู้ใช้คนแรกของช่วงนั้นจึงเป็นคนรับกรรม
    ///
    /// งานนี้เข้าไปอ่านแทนเป็นระยะ ๆ เพื่อให้ข้อมูลอยู่ในหน่วยความจำตลอด — ไม่ได้เก็บผลไว้ใช้ซ้ำ
    /// จึงไม่มีเรื่องข้อมูลค้างเข้ามาเกี่ยวเลย
    ///
    /// ปิดได้โดยตั้ง Search:PrewarmMinutes = 0
    /// </summary>
    public class SearchPrewarmService : BackgroundService
    {
        private readonly IConfiguration _config;

        public SearchPrewarmService(IConfiguration config) => _config = config;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var minutes = _config.GetValue("Search:PrewarmMinutes", 3);
            if (minutes <= 0)
            {
                Log.Information("อุ่นข้อมูลเบื้องหลัง: ปิดอยู่ (Search:PrewarmMinutes = 0)");
                return;
            }

            var connectionString = _config.GetConnectionString("strConnString");
            var databaseK2 = _config.GetConnectionString("DATABASEK2");
            if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(databaseK2))
            {
                Log.Warning("อุ่นข้อมูลเบื้องหลัง: ไม่ได้ตั้งค่าการเชื่อมต่อ จึงไม่ทำงาน");
                return;
            }

            var every = TimeSpan.FromMinutes(minutes);
            Log.Information("อุ่นข้อมูลเบื้องหลัง: เริ่มทำงาน ทุก {Minutes} นาที", minutes);

            // รอสักครู่ก่อนเริ่ม ให้แอปขึ้นเรียบร้อยและไม่ไปแย่งทรัพยากรตอนเปิดเครื่อง
            try { await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken); }
            catch (OperationCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                await WarmAsync(connectionString, databaseK2, stoppingToken);

                try { await Task.Delay(every, stoppingToken); }
                catch (OperationCanceledException) { return; }
            }
        }

        private static async Task WarmAsync(string connectionString, string databaseK2, CancellationToken ct)
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(ct);

                // อ่านหน้าแรกของวันนี้ด้วยคำสั่งชุดเดียวกับที่หน้าจอใช้
                var rows = await connection.QueryAsync(new CommandDefinition(
                    SearchSql.Page(databaseK2, "SortDate DESC, ApplicationID DESC"),
                    new
                    {
                        startDate = today,
                        endDate = today,
                        status = (string)null,
                        loanTypeCate = (string)null,
                        AccountNo = (string)null,
                        ApplicationCode = (string)null,
                        ProductSerialNo = (string)null,
                        CustomerID = (string)null,
                        CustomerName = (string)null,
                        StatusRegis = (string)null,
                        offset = 0,
                        pageSize = 20
                    },
                    commandTimeout: 180,
                    cancellationToken: ct));

                Log.Information("อุ่นข้อมูลเบื้องหลัง: อ่านของวันที่ {Date} เสร็จใน {Ms} ms ({Rows} แถว)",
                    today, sw.ElapsedMilliseconds, rows.Count());
            }
            catch (OperationCanceledException)
            {
                // ปิดแอป — ไม่ต้องรายงานเป็นข้อผิดพลาด
            }
            catch (Exception ex)
            {
                // ล้มเหลวก็แค่ข้ามรอบนี้ไป ไม่ควรทำให้แอปพัง
                Log.Warning(ex, "อุ่นข้อมูลเบื้องหลัง: รอบนี้ไม่สำเร็จ ({Ms} ms)", sw.ElapsedMilliseconds);
            }
        }
    }
}
