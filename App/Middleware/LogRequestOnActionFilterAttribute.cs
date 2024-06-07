using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;

namespace App.Middleware;

public class LogRequestOnActionFilterAttribute : IAsyncActionFilter
{
	public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
	{
		var method = context.HttpContext.Request.Method.ToString();
		var origin = context.HttpContext.Request.Headers.Origin;
		var path = context.HttpContext.Request.Path;
		var contentType = context.HttpContext.Request.ContentType;
		var controller = context.Controller.GetType().Name;
		var action = context.HttpContext.Request.RouteValues["action"];

		//Log.Information($"{method} {origin}{path} {contentType}");
		if("PingController" != controller)
		{
            Log.Information($"[{controller}] [{action}]");
        }
		
		// clear request body
		await next();
	}
}