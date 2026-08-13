/* =============================================================================
   WebAppCancelCCO — index ที่แนะนำสำหรับหน้าค้นหาใบคำขอ (Home/Search)
   -----------------------------------------------------------------------------
   จัดทำ : 2026-08-10
   สำหรับ: DBA พิจารณาและเป็นผู้รันเท่านั้น  ** ห้ามรันบน PROD โดยทีมพัฒนา **
   ทดสอบ : รันบน DEV (SG-K2DEV) ก่อน แล้วเทียบ execution plan ก่อน/หลัง

   ที่มา : query ของหน้าค้นหาถูกเขียนใหม่ให้ "คัดใบคำขอที่ตรงเงื่อนไขก่อน แล้วค่อย
           ดึงข้อมูลประกอบเฉพาะชุดนั้น" index ด้านล่างคือตัวที่รองรับรูปแบบใหม่นี้

   หมายเหตุก่อนรัน
   - สคริปต์เป็นแบบ idempotent (ตรวจก่อนว่ามีชื่อ index นี้อยู่แล้วหรือยัง)
     แต่ **ไม่ได้ตรวจว่ามี index อื่นที่คลุมคอลัมน์เดียวกันอยู่แล้วหรือไม่**
     กรุณาตรวจด้วย sp_helpindex / sys.indexes ก่อน เพื่อไม่ให้เกิด index ซ้ำซ้อน
   - SQL Server ที่ใช้อยู่คือ 2012 (11.0.6020) — ONLINE = ON ใช้ได้เฉพาะ Enterprise
     ถ้าเป็น Standard ให้รันนอกเวลาทำการเพราะจะล็อกตารางระหว่างสร้าง
   - ประเมินพื้นที่ดิสก์ก่อนสร้างบนตารางใหญ่ (Application, AUTO_SALE_POS_SERIAL)
   ============================================================================= */

/* -----------------------------------------------------------------------------
   1) Application — ตัวหลักที่สุด
   หน้าค้นหากรองด้วย ApplicationDate เป็นหลัก แล้วเรียงด้วย ApplicationDate DESC
   (ของเดิมเขียนเป็น CONVERT(date, ApplicationDate, 23) >= ... ซึ่งใช้ index ไม่ได้
    ตอนนี้แก้เป็น ApplicationDate >= @from AND ApplicationDate < @to แล้ว)
   ----------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_Application_ApplicationDate'
                 AND object_id = OBJECT_ID('[K2CCO].[dbo].[Application]'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Application_ApplicationDate
        ON [K2CCO].[dbo].[Application] (ApplicationDate DESC)
        INCLUDE (ApplicationID, ApplicationCode, AccountNo, CustomerID,
                 ApplicationStatusID, ProductSerialNo, SaleDepCode, SaleDepName,
                 ProductID, ProductModelName, SaleName, SaleTelephoneNo, ApprovedDate);
END
GO

/* ค้นด้วยเลขที่ใบคำขอ / เลขที่สัญญา / serial โดยไม่ใส่ช่วงวันที่ */
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_Application_ApplicationCode'
                 AND object_id = OBJECT_ID('[K2CCO].[dbo].[Application]'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Application_ApplicationCode
        ON [K2CCO].[dbo].[Application] (ApplicationCode) INCLUDE (ApplicationID, ApplicationDate);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_Application_AccountNo'
                 AND object_id = OBJECT_ID('[K2CCO].[dbo].[Application]'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Application_AccountNo
        ON [K2CCO].[dbo].[Application] (AccountNo) INCLUDE (ApplicationID, ApplicationDate);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_Application_ProductSerialNo'
                 AND object_id = OBJECT_ID('[K2CCO].[dbo].[Application]'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Application_ProductSerialNo
        ON [K2CCO].[dbo].[Application] (ProductSerialNo) INCLUDE (ApplicationID, ApplicationDate);
END
GO

/* -----------------------------------------------------------------------------
   2) ApplicationExtend — join 1:1 กับ Application ทุกครั้ง + ใช้กรอง loanTypeCate
      และค้นด้วย RefCode
   ----------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_ApplicationExtend_ApplicationID'
                 AND object_id = OBJECT_ID('[K2CCO].[dbo].[ApplicationExtend]'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ApplicationExtend_ApplicationID
        ON [K2CCO].[dbo].[ApplicationExtend] (ApplicationID)
        INCLUDE (RefCode, OU_Code, loanTypeCate);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_ApplicationExtend_RefCode'
                 AND object_id = OBJECT_ID('[K2CCO].[dbo].[ApplicationExtend]'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ApplicationExtend_RefCode
        ON [K2CCO].[dbo].[ApplicationExtend] (RefCode) INCLUDE (ApplicationID);
END
GO

/* -----------------------------------------------------------------------------
   3) ApplicationRegisIMIE — ถูกเรียกแบบ "หา 1 แถวต่อ 1 IMEI" (OUTER APPLY TOP 1)
      บน DEV พบว่ามี 155 IMEI ที่มีหลายแถว จึงต้องมี index ตรงนี้ให้ seek ได้
   ----------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_ApplicationRegisIMIE_IMEI_Status'
                 AND object_id = OBJECT_ID('[K2CCO].[dbo].[ApplicationRegisIMIE]'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ApplicationRegisIMIE_IMEI_Status
        ON [K2CCO].[dbo].[ApplicationRegisIMIE] (IMEI, [Status]);
END
GO

/* -----------------------------------------------------------------------------
   4) ARM_T_NEWSALES / ARM_T_PAYMENT — ตอนนี้ผูกด้วยเลขที่สัญญาของชุดที่ค้นเจอ
      (เดิมกวาดด้วยช่วงวันที่ ทำให้ได้ข้อมูลไม่ครบเมื่อรายการถูกสร้างคนละวันกับใบคำขอ)
   ----------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_ARM_T_NEWSALES_AccNo'
                 AND object_id = OBJECT_ID('[K2CCO].[dbo].[ARM_T_NEWSALES]'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ARM_T_NEWSALES_AccNo
        ON [K2CCO].[dbo].[ARM_T_NEWSALES] (ARM_ACC_NO)
        INCLUDE (arm_Loaded_flag, CREATED_DATE)
        WHERE CREATED_USER = 'SG Finance';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_ARM_T_PAYMENT_AccNo'
                 AND object_id = OBJECT_ID('[K2CCO].[dbo].[ARM_T_PAYMENT]'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ARM_T_PAYMENT_AccNo
        ON [K2CCO].[dbo].[ARM_T_PAYMENT] (ARM_ACC_NO)
        INCLUDE (ARM_RECEIPT_STAT, CREATED_DATE)
        WHERE CREATED_USER = 'SG Finance';
END
GO

/* -----------------------------------------------------------------------------
   5) contracts (ฝั่ง SGC-ESIGNATURE — คนละเซิร์ฟเวอร์ ต้องให้ DBA ฝั่งนั้นรัน)
      ** สำคัญที่สุดสำหรับ PROD ** เพราะ query ใหม่ผูก contracts ด้วย documentno
      แทนการกรองด้วยช่วงวันที่ ถ้าไม่มี index บน documentno การ join ข้ามเซิร์ฟเวอร์
      จะกลายเป็นการดึงตารางทั้งก้อนมาทำงานฝั่ง K2
   ----------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_contracts_documentno'
                 AND object_id = OBJECT_ID('[dbo].[contracts]'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_contracts_documentno
        ON [dbo].[contracts] (documentno)
        INCLUDE (signedStatus, statusReceived, createdAt);
END
GO

/* -----------------------------------------------------------------------------
   11) LOG_TRANSACTTION_SGFINANCE — ผลการแจ้งยกเลิกไปยัง e-contract
   หน้าค้นหาอ่านตารางนี้ทุกครั้ง เพื่อดูว่าใบคำขอที่ยกเลิกแล้วใบไหนแจ้งไปไม่ถึง
   (ใช้แสดงจรวด "แจ้งยกเลิกไปยัง e-contract ซ้ำ")

   ตารางมี ~511,000 แถว และมี index เดียวคือ clustered PK (No, OrderID)
   query กรองด้วย OrderID + Type ซึ่งไม่ได้ขึ้นต้นด้วย No จึง seek ไม่ได้
   วัดบน DEV : Clustered Index Scan กวาดทั้งตาราง ~90 ms ต่อครั้ง
   ----------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_LOG_TRANSACTTION_SGFINANCE_OrderID_Type'
                 AND object_id = OBJECT_ID('[K2CCO].[dbo].[LOG_TRANSACTTION_SGFINANCE]'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_LOG_TRANSACTTION_SGFINANCE_OrderID_Type
        ON [K2CCO].[dbo].[LOG_TRANSACTTION_SGFINANCE] (OrderID, [Type])
        INCLUDE (StatusCode);
END
GO

/* -----------------------------------------------------------------------------
   วิธีตรวจผลก่อน/หลัง
   -----------------------------------------------------------------------------
   SET STATISTICS IO, TIME ON;
   -- แล้วรัน query ของหน้าค้นหา พร้อมเปิด Actual Execution Plan
   -- ก่อน : Application = Clustered Index Scan / Table Scan
   -- หลัง : Application = Index Seek บน IX_Application_ApplicationDate

   วิธีถอย (rollback)
   -----------------------------------------------------------------------------
   DROP INDEX IX_Application_ApplicationDate ON [K2CCO].[dbo].[Application];
   -- ฯลฯ ตามชื่อ index ที่สร้าง (การลบ index ไม่กระทบข้อมูล)
   ============================================================================= */
