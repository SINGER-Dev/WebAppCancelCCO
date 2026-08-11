namespace App.Data
{
    /// <summary>
    /// SQL ของหน้าค้นหา เก็บไว้ที่เดียว เพราะถูกใช้จาก 2 ที่ —
    /// หน้าจอค้นหา และงานเบื้องหลังที่คอยอุ่นข้อมูลไว้ให้ SQL Server
    /// </summary>
    public static class SearchSql
    {
        public static string Page(string DATABASEK2, string orderBy) => @$"
DECLARE
    @TodayStart    DATETIME = CASE WHEN NULLIF(@startDate,'') IS NULL THEN NULL ELSE CAST(@startDate AS DATE) END,
    @TomorrowStart DATETIME = CASE WHEN NULLIF(@endDate,'')   IS NULL THEN NULL ELSE DATEADD(DAY, 1, CAST(@endDate AS DATE)) END;

WITH ranked AS (
SELECT
    a.ApplicationID, a.ApplicationCode, a.AccountNo, a.SaleDepCode, a.SaleDepName,
    CONVERT(NVARCHAR, a.ApplicationDate, 20) AS ApplicationDate, a.ApplicationDate AS SortDate,
    a.ProductModelName, a.CustomerID, a.SaleName, a.SaleTelephoneNo,
    a.ProductSerialNo, a.ApplicationStatusID,
    cus.FirstName, cus.LastName, cus.MobileNo1,
    appex.RefCode, appex.OU_Code, appex.loanTypeCate,
    ROW_NUMBER() OVER (PARTITION BY a.ApplicationCode
                       ORDER BY a.ApplicationDate DESC, a.ApplicationID DESC) AS rn
FROM {DATABASEK2}.[Application] a WITH (NOLOCK)
INNER JOIN {DATABASEK2}.[ApplicationExtend] appex WITH (NOLOCK) ON appex.ApplicationID = a.ApplicationID
LEFT JOIN {DATABASEK2}.[Customer] cus WITH (NOLOCK) ON cus.CustomerID = a.CustomerID
WHERE a.ApplicationDate >= '2024-05-01'
  AND (@TodayStart      IS NULL OR a.ApplicationDate >= @TodayStart)
  AND (@TomorrowStart   IS NULL OR a.ApplicationDate <  @TomorrowStart)
  AND (@status          IS NULL OR a.ApplicationStatusID = @status)
  AND (@loanTypeCate    IS NULL OR appex.loanTypeCate    = @loanTypeCate)
  AND (@AccountNo       IS NULL OR a.AccountNo           = @AccountNo)
  AND (@ApplicationCode IS NULL OR a.ApplicationCode = @ApplicationCode OR appex.RefCode = @ApplicationCode)
  AND (@ProductSerialNo IS NULL OR a.ProductSerialNo    = @ProductSerialNo)
  AND (@CustomerID      IS NULL OR a.CustomerID         = @CustomerID)
  AND (@CustomerName    IS NULL OR cus.FirstName + ' ' + cus.LastName LIKE '%' + @CustomerName + '%')
  AND (@StatusRegis IS NULL
       OR (@StatusRegis = '1' AND EXISTS (
               SELECT 1 FROM {DATABASEK2}.[ApplicationRegisIMIE] r WITH (NOLOCK)
               WHERE r.IMEI = a.ProductSerialNo
                 AND r.Status IN ('REGISTER DEVICE SUCCESS','ALREADY REGISTERED')))
       OR (@StatusRegis = '0' AND NOT EXISTS (
               SELECT 1 FROM {DATABASEK2}.[ApplicationRegisIMIE] r WITH (NOLOCK)
               WHERE r.IMEI = a.ProductSerialNo
                 AND r.Status IN ('REGISTER DEVICE SUCCESS','ALREADY REGISTERED'))))
),
paged AS (
    SELECT *,
           COUNT(*)     OVER ()                                       AS TotalRows,
           ROW_NUMBER() OVER (ORDER BY {orderBy}) AS seq
    FROM ranked WHERE rn = 1
)
SELECT ApplicationID, ApplicationCode, AccountNo, SaleDepCode, SaleDepName, ApplicationDate,
       ProductModelName, CustomerID, SaleName, SaleTelephoneNo, ProductSerialNo, ApplicationStatusID,
       FirstName, LastName, MobileNo1, RefCode, OU_Code, loanTypeCate, TotalRows
FROM paged
WHERE seq > @offset AND seq <= @offset + @pageSize
ORDER BY seq;";

        public static string Contract(string DATABASEK2, string SGCESIGNATURE) => @$"
SELECT documentno, signedStatus, statusReceived, numdoc FROM (
    SELECT c.documentno, c.signedStatus,
           -- คอลัมน์นี้ชนิดไม่เหมือนกันในแต่ละสภาพแวดล้อม (DEV เก็บ '1'/'0' ส่วน PROD เป็น bit true/false)
           -- แปลงให้เป็น '1'/'0' ตั้งแต่ใน SQL เพื่อให้ฝั่งโปรแกรมเทียบค่าได้แบบเดียวเสมอ
           CASE WHEN LOWER(ISNULL(CONVERT(NVARCHAR(10), c.statusReceived), '0')) IN ('1', 'true')
                THEN '1' ELSE '0' END AS statusReceived,
           COUNT(*)     OVER (PARTITION BY c.documentno) AS numdoc,
           ROW_NUMBER() OVER (PARTITION BY c.documentno ORDER BY c.createdAt DESC) AS rn
    FROM {SGCESIGNATURE}.[contracts] c WITH (NOLOCK)
    WHERE c.documentno IN @codes
) x WHERE rn = 1;";

        public static string NewSale(string DATABASEK2) => @$"
SELECT ARM_ACC_NO, newnum, arm_Loaded_flag FROM (
    SELECT n.ARM_ACC_NO, n.arm_Loaded_flag,
           COUNT(*)     OVER (PARTITION BY n.ARM_ACC_NO) AS newnum,
           ROW_NUMBER() OVER (PARTITION BY n.ARM_ACC_NO ORDER BY n.CREATED_DATE DESC) AS rn
    FROM {DATABASEK2}.[ARM_T_NEWSALES] n WITH (NOLOCK)
    WHERE n.CREATED_USER = 'SG Finance' AND n.ARM_ACC_NO IN @accounts
) x WHERE rn = 1;";

        public static string Payment(string DATABASEK2) => @$"
SELECT ARM_ACC_NO, paynum, ARM_RECEIPT_STAT FROM (
    SELECT p.ARM_ACC_NO, p.ARM_RECEIPT_STAT,
           COUNT(*)     OVER (PARTITION BY p.ARM_ACC_NO) AS paynum,
           ROW_NUMBER() OVER (PARTITION BY p.ARM_ACC_NO ORDER BY p.CREATED_DATE DESC) AS rn
    FROM {DATABASEK2}.[ARM_T_PAYMENT] p WITH (NOLOCK)
    WHERE p.CREATED_USER = 'SG Finance' AND p.ARM_ACC_NO IN @accounts
) x WHERE rn = 1;";

        public static string Regis(string DATABASEK2) => @$"
SELECT IMEI, [Status] FROM (
    SELECT r.IMEI, r.[Status],
           ROW_NUMBER() OVER (PARTITION BY r.IMEI ORDER BY r.ID DESC) AS rn
    FROM {DATABASEK2}.[ApplicationRegisIMIE] r WITH (NOLOCK)
    WHERE r.IMEI IN @serials
      AND r.[Status] IN ('REGISTER DEVICE SUCCESS','ALREADY REGISTERED')
) x WHERE rn = 1;";

    }
}
