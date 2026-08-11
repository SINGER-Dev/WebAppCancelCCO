namespace App.Models
{
    /// <summary>
    /// ผลการยกเลิกใบคำขอ พร้อมรายละเอียดว่าแต่ละขั้นเป็นอย่างไร
    ///
    /// ที่ต้องบอกเป็นราย ๆ ขั้น เพราะการยกเลิกไม่ได้ทำแค่อย่างเดียว แต่ไล่ทำหลายระบบต่อกัน
    /// และบางขั้น (ยกเลิกงานใน K2 / ยกเลิกสัญญาใน LMS) ย้อนกลับไม่ได้
    /// ถ้าพังกลางทางแล้วบอกแค่ "ไม่สำเร็จ" ผู้ใช้จะไม่รู้ว่าอะไรทำไปแล้วบ้าง
    /// และหน้าจออาจแสดงสถานะไม่ตรงกับความจริงในระบบปลายทาง
    /// </summary>
    public class CancelResultDto : ActionResultDto
    {
        public List<CancelStepDto> Steps { get; set; } = new();
    }

    public class CancelStepDto
    {
        /// <summary>ชื่อขั้นตอนที่ผู้ใช้เข้าใจได้ เช่น "ยกเลิกสัญญาในระบบสินเชื่อ"</summary>
        public string Name { get; set; } = "";

        /// <summary>ok = สำเร็จ · failed = ล้มเหลว · skipped = ไม่ต้องทำ · pending = ยังไม่ได้ทำ</summary>
        public string Status { get; set; } = "pending";

        /// <summary>คำอธิบายเพิ่มเติม เช่น เหตุผลที่ข้าม หรือข้อความผิดพลาดจากปลายทาง</summary>
        public string? Detail { get; set; }

        /// <summary>true = ขั้นนี้แก้ข้อมูลในระบบอื่นแล้วย้อนกลับเองไม่ได้</summary>
        public bool Irreversible { get; set; }
    }

    /// <summary>ตัวช่วยบันทึกผลแต่ละขั้นระหว่างยกเลิก</summary>
    public class CancelStepRecorder
    {
        private readonly List<CancelStepDto> _steps = new();

        public List<CancelStepDto> Steps => _steps;

        public CancelStepDto Begin(string name, bool irreversible = false)
        {
            var step = new CancelStepDto { Name = name, Irreversible = irreversible };
            _steps.Add(step);
            return step;
        }

        public void Ok(CancelStepDto step, string? detail = null)
        {
            step.Status = "ok";
            step.Detail = detail;
        }

        public void Failed(CancelStepDto step, string? detail = null)
        {
            step.Status = "failed";
            step.Detail = detail;
        }

        public void Skipped(CancelStepDto step, string? reason = null)
        {
            step.Status = "skipped";
            step.Detail = reason;
        }

        /// <summary>ขั้นที่ทำไปแล้วและย้อนกลับไม่ได้ — ใช้เตือนผู้ใช้เมื่อขั้นหลังพัง</summary>
        public bool HasIrreversibleDone =>
            _steps.Any(s => s.Irreversible && s.Status == "ok");

        public bool AnyFailed => _steps.Any(s => s.Status == "failed");
    }
}
