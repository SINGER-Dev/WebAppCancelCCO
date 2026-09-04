namespace App.Models
{
    /// <summary>
    /// รูปแบบข้อมูลที่หน้าค้นหาส่งกลับให้เบราว์เซอร์ (JSON)
    /// เดิม server เรนเดอร์ HTML ทั้งตารางแล้วส่งมา ทำให้ payload ใหญ่หลายร้อย KB ต่อหน้า
    /// </summary>
    public class SearchResultDto
    {
        public bool Success { get; set; } = true;
        public string? Message { get; set; }
        public List<SearchRowDto> Data { get; set; } = new();
        public SearchMetaDto Meta { get; set; } = new();
    }

    public class SearchMetaDto
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public int TotalPages { get; set; }
        public int[] PageSizes { get; set; } = Array.Empty<int>();

        /// <summary>เวลาที่อ่านข้อมูลชุดนี้จากฐานข้อมูลจริง — แสดงบนหน้าจอให้รู้ว่าข้อมูลสดแค่ไหน</summary>
        public string GeneratedAt { get; set; } = "";

        /// <summary>อายุของข้อมูล (วินาที) — 0 = เพิ่งอ่านสด</summary>
        public int AgeSeconds { get; set; }

        /// <summary>คอลัมน์ที่กำลังเรียง และทิศทาง — ให้หน้าจอวาดลูกศรได้ถูก</summary>
        public string Sort { get; set; } = "date";
        public string Dir { get; set; } = "desc";

        /// <summary>
        /// การค้นในผลลัพธ์ไล่ดูไม่ครบทั้งชุด เพราะชุดผลลัพธ์ใหญ่เกินเพดาน
        /// ต้องบอกบนหน้าจอ ไม่งั้นผู้ใช้จะเข้าใจว่า "ไม่มี" ทั้งที่จริงคือ "ยังไม่ได้ดูถึง"
        /// </summary>
        public bool QuickTruncated { get; set; }

        /// <summary>จำนวนแถวที่การค้นในผลลัพธ์ไล่ดูจริง (มีความหมายเมื่อ QuickTruncated = true)</summary>
        public int QuickScanned { get; set; }
    }

    /// <summary>คำขอปิดใบร่างซ้ำ — ใช้ ApplicationID (PK) เพราะใบร่างขยะอาจไม่มี ApplicationCode</summary>
    public class CloseDuplicateDraftRq
    {
        public string? ApplicationID { get; set; }
    }

    /// <summary>ข้อมูลใบที่ใช้ตรวจก่อนปิด (map จาก Dapper) — สถานะ + RefCode ของใบร่างที่จะปิด</summary>
    public class DraftInfoRow
    {
        public string? Status { get; set; }
        public string? RefCode { get; set; }
        public string? AppCode { get; set; }
    }

    public class SearchRowDto
    {
        public string? ApplicationCode { get; set; }
        public string? RefCode { get; set; }
        public string? ApplicationDate { get; set; }

        public string? AccountNo { get; set; }
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerMobile { get; set; }

        public string? SaleDepCode { get; set; }
        public string? SaleDepName { get; set; }
        public string? SaleName { get; set; }
        public string? SaleTelephoneNo { get; set; }

        public string? ProductModelName { get; set; }
        public string? ProductSerialNo { get; set; }

        public string? ApplicationStatusId { get; set; }

        /// <summary>
        /// คำไทยของสถานะใบคำขอ — แสดงใต้ป้ายรหัสสถานะ และเป็นคำที่ช่องค้นในผลลัพธ์รับด้วย
        /// (ว่างได้ ถ้าเจอรหัสที่ยังไม่มีคำไทยกำกับไว้ใน ApplicationStatusText)
        /// </summary>
        public string? StatusText { get; set; }

        public string? LineStatus { get; set; }
        public string? SignedStatus { get; set; }
        public string? StatusReceived { get; set; }
        public string? NumRegis { get; set; }
        public string? NumDoc { get; set; }
        public string? NewNum { get; set; }
        public string? PayNum { get; set; }

        public string? LoanTypeCate { get; set; }
        public string? OuCode { get; set; }

        /// <summary>
        /// เงื่อนไขว่าปุ่มซ่อมแต่ละตัวจะแสดงหรือไม่ — คำนวณที่ server เพื่อให้กติกาอยู่ที่เดียว
        /// (เดิมเขียนเป็น if ซ้อนกันอยู่ในไฟล์ Razor ทำให้แก้ยากและตรวจสอบไม่ได้)
        /// </summary>
        public bool CanPushStatusClosed { get; set; }
        public bool CanGenEsignature { get; set; }
        public bool CanRegisImei { get; set; }
        public bool CanRepushNewSale { get; set; }

        /// <summary>ใบนี้มีสัญญาซ้ำ จึงกดซ่อมได้</summary>
        public bool CanFixDuplicateContract { get; set; }

        /// <summary>ใบคำขอถูกยกเลิกแล้ว แต่ e-contract ยังไม่รับรู้ จึงต้องแจ้งซ้ำ</summary>
        public bool CanRenotifyCancel { get; set; }

        /// <summary>เหตุผลที่ยังกดลงทะเบียนเครื่องไม่ได้ (null = กดได้ หรือทำไปแล้ว) — ใช้อธิบายบนหน้าจอ</summary>
        public string? RegisBlockedReason { get; set; }

        /// <summary>
        /// ใบคำขอนี้มี RefCode (เลข REQ) ซ้ำกับใบอื่นในผลค้นหาชุดเดียวกัน — เกิดจาก SGF+/LOS
        /// สร้างใบซ้ำต่อ REQ เดียว (ใบจริง 1 + ใบร่างขยะที่เหลือ) ใช้ขึ้น badge เตือนที่หน้าจอ
        /// </summary>
        public bool IsDuplicateReq { get; set; }

        /// <summary>จำนวนใบทั้งหมดที่ใช้ RefCode เดียวกันในผลค้นหาชุดนี้ (>1 เมื่อ IsDuplicateReq)</summary>
        public int DuplicateReqCount { get; set; }

        /// <summary>ApplicationID (PK) — ใช้ปิดใบร่างขยะที่ไม่มี ApplicationCode ได้ (ApplicationCode อาจว่าง)</summary>
        public string? ApplicationID { get; set; }

        /// <summary>
        /// ใบนี้เป็น "ใบร่างขยะ" ที่ปิดได้ = REQ ซ้ำ + status DRAFT + ในกลุ่ม RefCode เดียวกัน
        /// มีใบจริง (ApplicationCode + เดินหน้าแล้ว) อยู่ด้วย จึงปิดใบร่างนี้ทิ้งได้อย่างปลอดภัย
        /// </summary>
        public bool CanCloseDuplicateDraft { get; set; }
    }
}
