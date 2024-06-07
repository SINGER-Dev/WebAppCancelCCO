using App.Middleware;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers
{
	[ApiController]
	[ServiceFilter(typeof(LogRequestOnActionFilterAttribute))]
	//[ServiceFilter(typeof(LogResponseOnResultFilterAttribute))]
	[Produces("application/json")]
	public abstract class ApiControllerBase : ControllerBase
	{

	}

}
