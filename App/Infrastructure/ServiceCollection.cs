using Serilog.Events;
using Serilog;

namespace App.Infrastructure
{
	public static class ServiceCollection
	{
		public static IServiceCollection AddServiceCollection(this IServiceCollection services, IConfiguration Configuration)
		{
			#region Logging
			// เดิม path เป็น /tmp (แบบ Unix) ทั้งที่ deploy ลง Windows (D:\public\...) → ไฟล์ไปโผล่ C:\tmp
			// และระดับ log เป็น Debug บน prod ทำให้ log บวมและมีข้อมูลส่วนบุคคลติดไปด้วย
			// ตั้งค่าผ่าน Serilog:LogFilePath / Serilog:MinimumLevel ได้ ถ้าไม่ตั้งจะใช้ค่าปลอดภัยตามด้านล่าง
			var logFilePath = Configuration["Serilog:LogFilePath"];
			if (string.IsNullOrWhiteSpace(logFilePath))
			{
				logFilePath = Path.Combine(AppContext.BaseDirectory, "logs", "WebAppCancelCCO.log");
			}

			if (!Enum.TryParse(Configuration["Serilog:MinimumLevel"], true, out LogEventLevel minimumLevel))
			{
				minimumLevel = LogEventLevel.Information;
			}

			Log.Logger = new LoggerConfiguration()
						  .MinimumLevel.Is(minimumLevel)
						  .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
						  .MinimumLevel.Override("System", LogEventLevel.Information)
						  .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Error)
						  .Enrich.FromLogContext()
						  .WriteTo.File(logFilePath,
							  rollingInterval: RollingInterval.Day,
							  rollOnFileSizeLimit: true,
							  fileSizeLimitBytes: 10000000,
							  restrictedToMinimumLevel: minimumLevel,
                               outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}[{Level:u3}] ServiceName: {ServiceName}, RequestId: {RequestId}, Request Path: {RequestPath}, HTTP Method: {RequestMethod}, Message: {Message:lj} {NewLine}{Exception}")
                          .WriteTo.Console(minimumLevel,
                              outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ServiceName: {ServiceName}, RequestId: {RequestId}, Request Path: {RequestPath}, HTTP Method: {RequestMethod}, Message: {Message:lj} {NewLine}{Exception}")
                          .CreateBootstrapLogger();
			#endregion

			return services;
		}
	}
}