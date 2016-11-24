using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using SampleApplication.Exceptions;

namespace SampleApplication.Controllers
{
    [Route("api/[controller]")]
    public class ExceptionsController : Controller
    {
        [HttpGet]
        public IActionResult Exception()
        {
            throw new Exception();
        }

        [HttpGet("argumentnull")]
        public IActionResult ArgumentNullException(string param)
        {
            throw new ArgumentNullException(nameof(param));
        }

        [HttpGet("keynotfound")]
        public IActionResult KeyNotFoundException()
        {
            throw new KeyNotFoundException();
        }

        [HttpGet("custom")]
        public IActionResult CustomException()
        {
            throw new CustomException();
        }
    }
}
