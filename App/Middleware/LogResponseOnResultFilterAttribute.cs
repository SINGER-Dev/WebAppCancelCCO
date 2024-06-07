using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;

namespace App.Middleware;

public class LogResponseOnResultFilterAttribute : IAsyncResultFilter
{
	public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
	{
		var ResponseStatus = context.Result.ToString();
		// PayloadResult = string.IsNullOrEmpty(responseBody) ? ErrorMessageConstant.EmptyBody : responseBody
		//Log.Information($"[{context.Controller.GetType().Name}] [{context.HttpContext.Request.RouteValues["action"]}]");
		if (context.Result is not EmptyResult)
		{
			await next();
		}
		else
		{
			context.Cancel = true;
		}
	}
}