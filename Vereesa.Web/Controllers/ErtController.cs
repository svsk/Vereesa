
using Microsoft.AspNetCore.Mvc;
using Vereesa.Core.Infrastructure;

[ApiController]
[Route("[controller]")]
public class ErtController : ControllerBase
{
	[HttpGet("decode")]
	public ActionResult<string> DecodeGZip(string raw)
	{
		try
		{
			return Compressor.Unzip(raw);
		}
		catch
		{
			return BadRequest();
		}
	}
}
