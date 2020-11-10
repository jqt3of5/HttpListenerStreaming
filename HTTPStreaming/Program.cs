using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;

namespace HTTPStreaming
{
    class Program
    {
        private static List<string> _data = new List<string>();
        static void ProducerTask()
        {
            int i = 0;
            while (true)
            {
                lock (_data)
                {
                    _data.Add("Data produced. Index: "+ i);
                }
                i++;
                Thread.Sleep(1000);
            }
        }
        static void Main(string[] args)
        {
            //Start the producer
            Task.Run(ProducerTask);
            
            var listener = new HttpListener();
            listener.Prefixes.Add("http://+:8080/session/12345/");
            listener.Start();


            while (true)
            {
                var context = listener.GetContext();
                // var request = context.Request;

                Task.Run(() =>
                {
                    int publishedIndex = 0; 
                    while (true)
                    {

                        int count = publishedIndex;
                        lock (_data)
                        {
                            count = _data.Count;
                        }

                        if (count <= publishedIndex)
                        {
                            Thread.Sleep(1000);
                            continue;
                        }
                        
                        List<string> response;
                        lock (_data)
                        {
                            response = _data.Skip(publishedIndex).ToList();
                            publishedIndex = _data.Count;
                        }

                        context.Response.SendChunked = true;
                        var offset = 0;
                        foreach (var str in response)
                        {
                            var buffer = Encoding.UTF8.GetBytes(str);
                            context.Response.ContentLength64 = buffer.Length;
                            context.Response.OutputStream.Write(buffer, offset, buffer.Length);
                            // context.Response.OutputStream.Flush();
                            offset += buffer.Length;
                        }
                    }
                });
            }
        }
    }
}