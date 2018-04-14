using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace UnkoService.Controllers
{
    [Route("api/[controller]")]
    public class UnkosController : Controller
    {
        private WSServer WSServer { get; set; }

        public UnkosController(WSServer wsServer)
        {
            this.WSServer = wsServer;
        }

        // GET api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
            this.WSServer.BroadcastMessage(new Message()
            {
                MessageType = "DidUnko",
                ClientName = "UnkoButton",
                What = "Unko",
            });
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
