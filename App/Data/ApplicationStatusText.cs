namespace App.Data
{
    /// <summary>
    /// คำไทยของสถานะใบคำขอ เก็บไว้ที่เดียว
    ///
    /// ในฐานข้อมูล ApplicationStatusID เป็นรหัสอังกฤษล้วน (CLOSED / CANCELLED / ...)
    /// แต่ผู้ใช้เรียกสถานะเป็นภาษาไทยเสมอ — "ยกเลิก" "ปิดงาน" "รอลงนาม" — และตารางเดิม
    /// กรองฝั่งเบราว์เซอร์จึงพิมพ์คำไทยแล้วเจอ พอย้ายมาแบ่งหน้าที่ server ความสามารถนี้หายไป
    ///
    /// รายชื่อนี้ถูกใช้ 3 ที่ และต้องเป็นชุดเดียวกันทั้งหมด ไม่งั้นจะเพี้ยนกันเอง:
    ///   1. ตัวเลือกในช่อง "สถานะ" ของฟอร์มค้นหา (Index.cshtml)
    ///   2. คำไทยที่แสดงใต้ป้ายสถานะบนตาราง (ส่งไปกับ SearchRowDto.StatusText)
    ///   3. การค้นในผลลัพธ์ — พิมพ์คำไทยแล้วต้องเจอแถวที่อยู่สถานะนั้น
    /// </summary>
    public static class ApplicationStatusText
    {
        /// <summary>สถานะหนึ่งตัว — รหัสที่เก็บจริง คำไทยที่คนใช้เรียก และกลุ่มที่โชว์ในตัวเลือก</summary>
        public sealed record Status(string Code, string Thai, string Group, bool Selectable = true);

        /// <summary>
        /// เรียงตามลำดับที่อยากให้เห็นในช่องเลือกสถานะ
        ///
        /// NEW ไม่ให้เลือกในฟอร์ม เพราะใบที่สถานะ NEW ยังไม่มีข้อมูลใน ApplicationExtend สักใบ
        /// ผลค้นหาจึงเป็นศูนย์เสมอ ใส่ไว้จะทำให้เข้าใจผิดว่าไม่มีใบคำขอในสถานะนั้น
        /// แต่ยังต้องมีคำไทยไว้ เผื่อเจอค่านี้ในข้อมูลจะได้แสดงและค้นได้เหมือนตัวอื่น
        /// </summary>
        public static readonly IReadOnlyList<Status> All = new[]
        {
            new Status("CLOSED",       "ปิดงาน",          "ปิดงานแล้ว"),
            new Status("CANCELLED",    "ยกเลิก",          "ปิดงานแล้ว"),
            new Status("REJECTED",     "ปฏิเสธ",          "ปิดงานแล้ว"),
            new Status("CLOSING",      "กำลังปิดงาน",     "ระหว่างดำเนินการ"),
            new Status("DRAFT",        "ร่าง",            "ระหว่างดำเนินการ"),
            new Status("SUBMITTED",    "ส่งพิจารณา",      "ระหว่างดำเนินการ"),
            new Status("REVIEWING",    "กำลังพิจารณา",    "ระหว่างดำเนินการ"),
            new Status("NCB",          "ตรวจ NCB",        "ระหว่างดำเนินการ"),
            new Status("NEEDMOREINFO", "ขอเอกสารเพิ่ม",   "ระหว่างดำเนินการ"),
            new Status("REVISING",     "ส่งกลับให้แก้ไข", "ระหว่างดำเนินการ"),
            new Status("PENDING",      "รอดำเนินการ",     "ระหว่างดำเนินการ"),
            new Status("NEW",          "ใหม่",            "ระหว่างดำเนินการ", Selectable: false),
        };

        private static readonly Dictionary<string, string> ThaiByCode =
            All.ToDictionary(s => s.Code, s => s.Thai, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// คำไทยของรหัสสถานะ — คืนค่าว่างถ้าไม่รู้จักรหัสนั้น
        /// (ไม่คืนตัวรหัสกลับมา เพราะจุดที่เรียกใช้แสดงรหัสอยู่แล้ว จะได้ไม่ขึ้นซ้ำสองครั้ง)
        /// </summary>
        public static string Thai(string? code)
        {
            var key = (code ?? "").Trim();
            return key.Length > 0 && ThaiByCode.TryGetValue(key, out var thai) ? thai : "";
        }

        /// <summary>ตัวเลือกสำหรับช่องเลือกสถานะ จัดกลุ่มตามลำดับใน All</summary>
        public static IEnumerable<IGrouping<string, Status>> SelectableGroups() =>
            All.Where(s => s.Selectable).GroupBy(s => s.Group);
    }
}
