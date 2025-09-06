using BoltServer.WebSockets;
using Microsoft.AspNetCore.Mvc;

namespace BoltServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScrewController : ControllerBase
    {
        private readonly WebSocketHandler _socketHandler;

        public ScrewController(WebSocketHandler socketHandler)
        {
            _socketHandler = socketHandler;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] MessageDto message)
        {
            if (string.IsNullOrEmpty(message.Text))
            {
                return BadRequest("Message text cannot be empty.");
            }

            await _socketHandler.SendMessageToAllAsync(message.Text);

            return Ok("Message sent to all connected clients.");
        }
    }

    public class MessageDto
    {
        public string Text { get; set; }
    }
}
