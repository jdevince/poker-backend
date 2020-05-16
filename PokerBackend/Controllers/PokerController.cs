using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PokerBackend.Hubs;
using PokerBackend.Services;

namespace PokerBackend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PokerController : ControllerBase
    {
        private readonly ILogger<PokerController> _logger;
        private PokerService _pokerService;

        public PokerController(ILogger<PokerController> logger, PokerService pokerService)
        {
            _logger = logger;
            _pokerService = pokerService;
        }

        [HttpPost("create-game")]
        public ActionResult CreateGame()
        {
            string newGameCode = _pokerService.CreateNewGame();
            return Ok(newGameCode);
        }

        [HttpGet("keep-alive")]
        public ActionResult KeepAlive()
        {
            return Ok();
        }
    }
}
