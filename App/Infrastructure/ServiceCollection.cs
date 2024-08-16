using Serilog.Events;
using Serilog;

namespace App.Infrastructure
{
	public static class ServiceCollection
	{
		public static IServiceCollection AddServiceCollection(this IServiceCollection services, IConfiguration Configuration)
		{
			#region Logging
			Log.Logger = new LoggerConfiguration()
						  .MinimumLevel.Debug()
						  .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
						  .MinimumLevel.Override("System", LogEventLevel.Information)
						  .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Error)
						  .Enrich.FromLogContext()
						  .WriteTo.File($"/tmp/WebAppCancelCCO.log",
							  rollingInterval: RollingInterval.Day,
							  rollOnFileSizeLimit: true,
							  fileSizeLimitBytes: 10000000,
							  restrictedToMinimumLevel: LogEventLevel.Debug,
                               outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}[{Level:u3}] ServiceName: {ServiceName}, RequestId: {RequestId}, Request Path: {RequestPath}, HTTP Method: {RequestMethod}, Message: {Message:lj} {NewLine}{Exception}")
                          .WriteTo.Console(LogEventLevel.Debug,
                              outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ServiceName: {ServiceName}, RequestId: {RequestId}, Request Path: {RequestPath}, HTTP Method: {RequestMethod}, Message: {Message:lj} {NewLine}{Exception}")
                          .CreateBootstrapLogger();
			#endregion

			return services;
		}
	}
}