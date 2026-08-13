using System.Collections.Concurrent;

namespace App.Infrastructure
{
    /// <summary>
    /// เก็บสถิติเวลาของหน้าค้นหาไว้ในหน่วยความจำ
    ///
    /// ที่ต้องมีเพราะเดิมเราเขียนเวลาแต่ละครั้งลง log แต่ไม่มีใครดูรวม
    /// เวลาถกกันว่า "ช้าตรงไหน" จึงเถียงกันด้วยความรู้สึก ไม่ใช่ตัวเลข
    ///
    /// จงใจเก็บในหน่วยความจำ ไม่แตะฐานข้อมูล — เพราะการเขียนลงตารางทุก request
    /// คือการเพิ่ม write ให้กับสิ่งที่เรากำลังพยายามทำให้เร็วขึ้น
    /// ข้อแลกคือค่าจะหายเมื่อรีสตาร์ทแอป ซึ่งยอมรับได้สำหรับการดูแนวโน้มระยะสั้น
    /// </summary>
    public static class SearchMetrics
    {
        /// <summary>จำนวนครั้งล่าสุดที่เก็บไว้ต่อรูปแบบการค้น</summary>
        private const int Capacity = 500;

        private static readonly ConcurrentDictionary<string, Bucket> Buckets = new();

        public static DateTime StartedAt { get; } = DateTime.Now;

        /// <summary>
        /// บันทึกผลการค้น 1 ครั้ง
        /// </summary>
        /// <param name="shape">รูปแบบการค้น เช่น "วันนี้" / "ช่วงวันที่" / "ระบุเลขเจาะจง"</param>
        /// <param name="pageMs">เวลาของ query หลัก</param>
        /// <param name="enrichMs">เวลาของการดึงข้อมูลประกอบ</param>
        /// <param name="totalMs">เวลารวมที่ผู้ใช้รอ</param>
        /// <param name="fromCache">อ่านจากแคชได้เลยหรือไม่</param>
        public static void Record(string shape, long pageMs, long enrichMs, long totalMs, bool fromCache)
        {
            Buckets.GetOrAdd(shape, _ => new Bucket()).Add(pageMs, enrichMs, totalMs, fromCache);
        }

        public static IReadOnlyList<ShapeStat> Snapshot() =>
            Buckets.Select(kv => kv.Value.ToStat(kv.Key))
                   .OrderByDescending(s => s.Count)
                   .ToList();

        public static void Reset() => Buckets.Clear();

        private sealed class Bucket
        {
            private readonly object _lock = new();
            private readonly List<Sample> _samples = new(Capacity);
            private int _next;
            private long _cacheHits;
            private long _total;

            public void Add(long pageMs, long enrichMs, long totalMs, bool fromCache)
            {
                lock (_lock)
                {
                    _total++;
                    if (fromCache) _cacheHits++;

                    var s = new Sample(pageMs, enrichMs, totalMs);
                    if (_samples.Count < Capacity) _samples.Add(s);
                    else
                    {
                        // เก็บแบบวงกลม ทับตัวเก่าสุดเมื่อเต็ม เพื่อไม่ให้หน่วยความจำโตไม่จำกัด
                        _samples[_next] = s;
                        _next = (_next + 1) % Capacity;
                    }
                }
            }

            public ShapeStat ToStat(string shape)
            {
                List<Sample> copy;
                long hits, total;
                lock (_lock)
                {
                    copy = new List<Sample>(_samples);
                    hits = _cacheHits;
                    total = _total;
                }

                return new ShapeStat
                {
                    Shape = shape,
                    Count = total,
                    CacheHitPercent = total == 0 ? 0 : (int)Math.Round(hits * 100.0 / total),
                    TotalP50 = Percentile(copy.Select(x => x.TotalMs), 50),
                    TotalP95 = Percentile(copy.Select(x => x.TotalMs), 95),
                    PageP50 = Percentile(copy.Where(x => x.TotalMs >= 0).Select(x => x.PageMs), 50),
                    EnrichP50 = Percentile(copy.Select(x => x.EnrichMs), 50),
                    EnrichP95 = Percentile(copy.Select(x => x.EnrichMs), 95)
                };
            }

            private static long Percentile(IEnumerable<long> values, int p)
            {
                var list = values.Where(v => v >= 0).OrderBy(v => v).ToList();
                if (list.Count == 0) return 0;
                var index = (int)Math.Ceiling(p / 100.0 * list.Count) - 1;
                return list[Math.Clamp(index, 0, list.Count - 1)];
            }

            private readonly record struct Sample(long PageMs, long EnrichMs, long TotalMs);
        }
    }

    public class ShapeStat
    {
        public string Shape { get; set; } = "";
        public long Count { get; set; }
        public int CacheHitPercent { get; set; }
        public long TotalP50 { get; set; }
        public long TotalP95 { get; set; }
        public long PageP50 { get; set; }
        public long EnrichP50 { get; set; }
        public long EnrichP95 { get; set; }
    }
}
