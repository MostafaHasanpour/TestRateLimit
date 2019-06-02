using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace TestRateLimit.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        // GET api/values
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class ClientRateLimitController : Controller
    {
        private readonly ClientRateLimitOptions _options;
        private readonly IClientPolicyStore _clientPolicyStore;

        public ClientRateLimitController(IOptions<ClientRateLimitOptions> optionsAccessor, IClientPolicyStore clientPolicyStore)
        {
            _options = optionsAccessor.Value;
            _clientPolicyStore = clientPolicyStore;
        }

        [HttpGet]
        public async Task<ActionResult> Get()
        {
            var result = await _clientPolicyStore.GetAsync($"{_options.ClientPolicyPrefix}_cl-key-1");
            return Ok(result);
        }

        [HttpPost]
        public async Task Post()
        {
            var id = $"{_options.ClientPolicyPrefix}_cl-key-1";
            var clPolicy = await _clientPolicyStore.GetAsync(id);
            clPolicy.Rules.Add(new RateLimitRule
            {
                Endpoint = "*/api/testpolicyupdate",
                Period = "1h",
                Limit = 100
            });
            await _clientPolicyStore.SetAsync(id, clPolicy);
        }
    }
}
