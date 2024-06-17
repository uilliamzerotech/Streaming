using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Streaming;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Streaming.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FibController : ControllerBase
    {
        private static ConcurrentBag<StreamWriter> _clients = new ConcurrentBag<StreamWriter>();

        [HttpGet]
        [Route("streaming")]
        public IActionResult Streaming()
        {
            return new StreamResult(
                (stream, cancelToken) => {
                    var wait = cancelToken.WaitHandle;
                    var client = new StreamWriter(stream);
                    _clients.Add(client);

                    wait.WaitOne();

                    StreamWriter ignore;
                    _clients.TryTake(out ignore);
                },
                HttpContext.RequestAborted);
        }

        private async Task WriteOnStream(string data)
        {
            foreach (var client in _clients)
            {
                string jsonData = string.Format("{0}\n", JsonSerializer.Serialize(new { data }));
                await client.WriteAsync(jsonData);
                await client.FlushAsync();
            }
        }

        private async Task<int> GetFib(int n)
        {
            int fibA = 1;
            int fibB = 1;

            for (int i = 2; i <= n; i++)
            {
                int fib = fibA + fibB;
                fibA = fibB;
                fibB = fib;

                await WriteOnStream($"{i} => {fib}");
            }

            return fibB;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<int>> Get(int id)
        {
            var fib = await GetFib(id);
            return fib;
        }

        [HttpGet]
        public async Task<ActionResult<int>> Get()
        {
            var fib = await GetFib(100);
            return fib;
        }
    }
}