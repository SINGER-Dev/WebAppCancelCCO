using Microsoft.AspNetCore.Mvc.Filters;

namespace App.Filters
{
    /// <summary>
    /// ทำให้ผลค้นหาที่เก็บไว้ในหน่วยความจำถือว่าหมดอายุทันที เมื่อมีใครทำรายการที่เปลี่ยนข้อมูล
    ///
    /// ที่ต้องมีเพราะ: ถ้าล้าง cache ให้เฉพาะคนที่กดปุ่ม คนอื่นที่เปิดหน้าเดียวกันจะยังเห็น
    /// สถานะเก่าได้อีกถึง 60 วินาที เช่น A ยกเลิกใบไปแล้ว แต่ B ยังเห็นเป็นปกติและกดซ่อมซ้ำ
    ///
    /// วิธีทำ: เก็บเลขรุ่นไว้ตัวเดียว แล้วเอาไปผสมใน key ของ cache — พอเลขเปลี่ยน
    /// key เดิมก็ใช้ไม่ได้ทั้งหมดโดยไม่ต้องไล่ลบทีละรายการ
    ///
    /// ข้อจำกัด: ใช้ได้ภายในแอปหนึ่งตัวเท่านั้น ถ้าวันหนึ่งรันหลายเครื่องหลัง load balancer
    /// ต้องเปลี่ยนไปเก็บเลขรุ่นไว้ที่ส่วนกลาง (ตอนนี้ session ก็เก็บในหน่วยความจำอยู่แล้ว
    /// จึงรันได้ทีละเครื่องอยู่ดี)
    /// </summary>
    public class InvalidateSearchCacheAttribute : ActionFilterAttribute
    {
        private static long _version;

        public static long Version => Interlocked.Read(ref _version);

        public static void Bump() => Interlocked.Increment(ref _version);

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            // ทำรายการไม่สำเร็จก็ไม่ต้องล้าง — ข้อมูลยังเหมือนเดิม
            if (context.Exception == null)
            {
                Bump();
            }
        }
    }
}
