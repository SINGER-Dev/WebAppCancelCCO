namespace App.Models
{
    /// <summary>
    /// รูปแบบคำตอบมาตรฐานของทุกปุ่มที่ยิงไปแก้ข้อมูลปลายทาง
    ///
    /// เดิมแต่ละปุ่มคืนคนละแบบ (MessageReturn / RegisIMEIRespone / string ว่าง) และเมื่อปลายทาง
    /// ตอบไม่สำเร็จก็คืน object ว่างพร้อม HTTP 200 ทำให้หน้าจอขึ้น "[object Object]"
    /// แยกไม่ออกว่างานซ่อมสำเร็จหรือล้มเหลว
    /// </summary>
    public class ActionResultDto
    {
        /// <summary>งานสำเร็จหรือไม่ — ฝั่งหน้าจอตัดสินจากค่านี้ค่าเดียว</summary>
        public bool Ok { get; set; }

        /// <summary>ข้อความสำหรับแสดงให้ผู้ใช้อ่าน (ภาษาไทย บอกว่าเกิดอะไรขึ้น)</summary>
        public string Message { get; set; } = "";

        /// <summary>รายละเอียดเชิงเทคนิคสำหรับผู้ดูแลระบบ เช่น คำตอบดิบจากปลายทาง</summary>
        public string? Detail { get; set; }

        public static ActionResultDto Success(string message, string? detail = null)
            => new() { Ok = true, Message = message, Detail = detail };

        public static ActionResultDto Fail(string message, string? detail = null)
            => new() { Ok = false, Message = message, Detail = detail };
    }
}
