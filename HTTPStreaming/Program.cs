using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;

namespace HTTPStreaming
{
    class Program
    {
        static async void ConsumerTask()
        {
            var handler = new HttpClientHandler();
            var client = new HttpClient(handler);

            while (true)
            {
                Console.WriteLine("New connection");
                var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://localhost:8080/session/12345"),
                    HttpCompletionOption.ResponseHeadersRead); 
                    var stream = await response.Content.ReadAsStreamAsync();
                    var readStream = new StreamReader(stream);
                    while (!readStream.EndOfStream)
                    {
                        var buffer = new char[100];
                        var length = await readStream.ReadAsync(buffer);
                        Console.WriteLine("length: " + length);
                        Console.WriteLine(buffer);     
                    }
                   
            }
        }
        static void Main(string[] args)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8080/session/12345/");
            listener.Start();

            //Start the producer
            Task.Run(ConsumerTask);

            while (true)
            {
                var context = listener.GetContext();
                
                Task.Run(() =>
                {
                    int index = 0; 
                    context.Response.SendChunked = true;
                    context.Response.KeepAlive = true;
                    context.Response.ContentType = "Application/octet-stream";
                    
                    while (true)
                    {
                        var buffer = Encoding.UTF8.GetBytes($"Content with data with index: {index}");
                        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                        context.Response.OutputStream.Flush();
                        index += 1;
                        Thread.Sleep(1000); 
                    }
                });
            }
        }
    }
}