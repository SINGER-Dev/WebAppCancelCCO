using System.Collections.Concurrent;
using System.Globalization;
using Dapper;
using Microsoft.Data.SqlClient;
using Serilog;

namespace App.Infrastructure
{
    /// <summary>
    /// แปลง "วันที่เริ่มค้นหา" เป็น "ApplicationID ต่ำสุดที่เป็นไปได้"
    ///
    /// ที่ต้องมี — ตาราง Application ไม่มี index บน ApplicationDate การค้นตามช่วงวันที่
    /// จึงต้องกวาดทั้งตาราง (บน PROD 2.87 ล้านแถว ใช้เวลา ~3 วินาทีต่อครั้ง)
    /// แต่ตารางมี clustered index บน ApplicationID อยู่แล้ว ถ้าใส่เงื่อนไข ApplicationID >= X
    /// เข้าไปด้วย SQL Server จะ seek ตรงจุดแล้วอ่านเฉพาะส่วนท้ายของตาราง
    ///
    /// ทำไมถึงไม่ทำให้ข้อมูลหาย — เป็นเรื่องตรรกะล้วน ไม่ได้อาศัยว่าข้อมูลเรียงสวย
    ///   แถวที่ ApplicationDate >= วันที่เริ่มค้น  ย่อมมี ApplicationDate >= ต้นเดือนของวันนั้นด้วย
    ///   ดังนั้น ApplicationID ของมันต้อง >= ค่าต่ำสุดของ ID ที่วันที่ >= ต้นเดือน เสมอ
    /// (สำคัญ เพราะข้อมูลจริงมีการลงวันที่ย้อนหลัง — ID ใหม่แต่วันที่เก่า มีอยู่จริงหลายพันแถว
    ///  วิธีนี้จึงตั้งใจใช้แค่ "ขอบล่าง" ไม่ใช้ขอบบน และไม่ใช้การไล่แบ่งครึ่งซึ่งต้องการข้อมูลเรียง)
    ///
    /// ค่าที่ได้ไม่มีวันหมดอายุ — แถวที่เพิ่มใหม่ได้ ID สูงขึ้นเสมอ ค่าต่ำสุดจึงไม่ลดลง
    /// ถ้าแถวที่ถือค่าต่ำสุดถูกลบ ค่าจริงจะสูงขึ้น ของที่เก็บไว้กลายเป็นค่าที่ต่ำกว่าความจริง
    /// ซึ่งยังปลอดภัย แค่ทำให้อ่านมากกว่าที่จำเป็นนิดหน่อย
    /// </summary>
    public class ApplicationIdBounds
    {
        private readonly string _connectionString;
        private readonly string _databaseK2;

        // key = "yyyy-MM" ของเดือนที่เป็นขอบล่าง
        private readonly ConcurrentDictionary<string, int> _cache = new();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        /// <summary>0 = ไม่ต้องใส่เงื่อนไข ApplicationID (ใช้เมื่อหาค่าไม่ได้ ให้ทำงานแบบเดิม)</summary>
        public const int NoBound = 0;

        public ApplicationIdBounds(string connectionString, string databaseK2)
        {
            _connectionString = connectionString;
            _databaseK2 = databaseK2;
        }

        public int CachedCount => _cache.Count;

        /// <summary>
        /// คืน ApplicationID ต่ำสุดที่ปลอดภัยสำหรับวันที่เริ่มค้นที่ระบุ
        /// ถ้าหาไม่ได้ (ต่อฐานไม่ติด / ไม่มีข้อมูล) คืน NoBound เพื่อให้ค้นหาแบบเดิมต่อไปได้
        /// </summary>
        public async Task<int> GetLowerBoundAsync(string startDate)
        {
            if (string.IsNullOrWhiteSpace(startDate)) return NoBound;
            if (!DateTime.TryParse(startDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var from))
            {
                return NoBound;
            }

            // ปัดลงเป็นต้นเดือน เพื่อให้ค่าที่เก็บใช้ซ้ำได้ทั้งเดือน แทนที่จะเก็บทีละวัน
            var monthStart = new DateTime(from.Year, from.Month, 1);
            var key = monthStart.ToString("yyyy-MM", CultureInfo.InvariantCulture);

            if (_cache.TryGetValue(key, out var cached)) return cached;

            // กันหลายคนวิ่งไปคำนวณค่าเดียวกันพร้อมกัน (การหาค่านี้ต้องกวาดตาราง จึงแพง)
            var gate = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync();
            try
            {
                if (_cache.TryGetValue(key, out cached)) return cached;

                var sw = System.Diagnostics.Stopwatch.StartNew();
                using var connection = new SqlConnection(_connectionString);
                var value = await connection.ExecuteScalarAsync<int?>(new CommandDefinition(
                    $"SELECT MIN(ApplicationID) FROM {_databaseK2}.[Application] WITH (NOLOCK) WHERE ApplicationDate >= @MonthStart",
                    new { MonthStart = monthStart }, commandTimeout: 300));

                var bound = value ?? NoBound;
                _cache[key] = bound;

                Log.Information("ขอบล่าง ApplicationID ของเดือน {Month} = {Bound} (ใช้เวลาหา {Ms} ms, เก็บไว้ใช้ตลอด)",
                    key, bound, sw.ElapsedMilliseconds);
                return bound;
            }
            catch (Exception ex)
            {
                // หาไม่ได้ก็ไม่ควรทำให้ค้นหาไม่ได้ — ถอยไปใช้วิธีเดิม
                Log.Error(ex, "หาขอบล่าง ApplicationID ของเดือน {Month} ไม่สำเร็จ จะค้นหาแบบเดิมแทน", key);
                return NoBound;
            }
            finally
            {
                gate.Release();
            }
        }
    }
}
