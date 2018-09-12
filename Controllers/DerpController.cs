using System;
using Microsoft.AspNetCore.Mvc;

namespace VideoKategoriseringsApi.Controllers
{
    [Route("api/derp")]
    public class DerpController : Controller
    {
        public IActionResult Get()
        {
            return Ok($"Hej.\n{Grejor[(new System.Random()).Next(Grejor.Length)]}");
        }

        public IActionResult Post()
        {
            return Forbid();
        }

        private readonly string[] Grejor =
        {
            "Lukas har alltid ISO på 200. Han skyller på knapparna.",
            "Martin har typ en Japan."
        };
    }
}