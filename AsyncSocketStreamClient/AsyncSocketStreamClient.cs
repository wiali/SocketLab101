﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace SocketLab101
{
    internal class StateObject
    {
        /// <summary>Handle for communicate to client</summary>
        public Socket Handler = null;

        /// <summary>Buffer size</summary>
        public const int BufferSize = 1024;

        /// <summary>Buffer of each send</summary>
        public byte[] BufferPerSend = new byte[BufferSize];

        /// <summary>Stream for send</summary>
        public NetworkStream SendStream { get; set; }

        /// <summary>Stream for recv</summary>
        public NetworkStream RecvStream { get; set; }
    }

    public class AsyncSocketStreamClient
    {
        // The port number for the remote device.
        private const int port = 11000;

        // ManualResetEvent instances signal completion.
        private static ManualResetEvent connectDone =
            new ManualResetEvent(false);
        private static ManualResetEvent sendDone =
            new ManualResetEvent(false);
        private static ManualResetEvent receiveDone =
            new ManualResetEvent(false);

        // The response from the remote device.
        private static String response = String.Empty;

        private static void StartClient()
        {
            // Connect to a remote device.
            try {
                // Establish the remote endpoint for the socket.
                // The name of the 
                // remote device is "host.contoso.com".
                IPHostEntry ipHostInfo = Dns.GetHostEntry("127.0.0.1");
                IPAddress ipAddress = ipHostInfo.AddressList.FirstOrDefault(
                            info => info.AddressFamily == AddressFamily.InterNetwork);
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

                // Create a TCP/IP socket.
                Socket client = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.
                client.BeginConnect(remoteEP,
                    new AsyncCallback(ConnectCallback), client);
                connectDone.WaitOne();

                string msg = @"What is CNN Student News? CNN Student News is a ten - minute, commercial - free, daily news program designed for middle and high school classes. It is produced by the journalists at CNN. This award - winning show and its companion website are available free of charge throughout the school year. Where can I find CNN Student News? You can see it as a streamed video or download it as a free podcast, both available on our website, CNNStudentNews.com.The show is available Monday through Friday during the school year. The program is free and accessible to anyone who wants to watch; there are no subscription charges, sign - ups, or contracts to complete. How do I get advance information about each day's show? The Daily Email offers information on the major stories we'll be covering that day. On our homepage, you can sign up for this free, Daily Email. You can also check the Daily Transcript to see what stories are in the show. Remember that CNN Student News is a news program that presents current events and issues in the real world. We strongly advise you to preview each program before showing students, as you are the best judge of the appropriateness of its news content for your specific class.<EOF>";

                // Send test data to the remote device.
                Send(client, msg);
                sendDone.WaitOne();

                // Receive the response from the remote device.
                Receive(client);
                receiveDone.WaitOne();

                // Write the response to the console.
                Console.WriteLine($"Recv: {response}");

                // Release the socket.
                client.Shutdown(SocketShutdown.Both);
                client.Close();

            }
            catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        private static void ConnectCallback(IAsyncResult ar)
        {
            try {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.
                client.EndConnect(ar);

                Console.WriteLine("Socket connected to {0}",
                    client.RemoteEndPoint.ToString());

                // Signal that the connection has been made.
                connectDone.Set();
            }
            catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Receive(Socket client)
        {
            try {
                // Create the state object.
                StateObject state = new StateObject();
                state.Handler = client;
                state.RecvStream = new NetworkStream(client);

                // Begin receiving the data from the remote device.
                if (state.RecvStream.CanRead) {
                    state.RecvStream.BeginRead(state.BufferPerSend, 0, StateObject.BufferSize, new AsyncCallback(ReceiveCallback), state);
                }
                else {
                    Console.WriteLine("Can't Recv from NetworkStream");
                }
            }
            catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            try {
                // Retrieve the state object and the client socket 
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.Handler;
                int bytesRead = state.RecvStream.EndRead(ar);

                while (state.RecvStream.DataAvailable) {
                    state.RecvStream.BeginRead(state.BufferPerSend, 0, StateObject.BufferSize, new AsyncCallback(ReceiveCallback), state);
                }

                using (state.RecvStream) {
                    using (StreamReader sr = new StreamReader(state.RecvStream)) {
                        Console.WriteLine("Recv: " + sr.ReadLine());
                    }
                }

                receiveDone.Set();
            }
            catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Send(Socket client, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);
            StateObject state = new StateObject();
            state.Handler = client;
            state.SendStream = new NetworkStream(client);

            // Begin sending the data to the remote device.
            state.SendStream.BeginWrite(byteData, 0, byteData.Length, new AsyncCallback(SendCallback), state);

        }

        private static void SendCallback(IAsyncResult ar)
        {
            try {
                // Retrieve the socket from the state object.
                StateObject state = (StateObject)ar.AsyncState;

                // Complete sending the data to the remote device.
                using (state.SendStream) {
                    state.SendStream.EndWrite(ar);
                }

                // Signal that all bytes have been sent.
                sendDone.Set();
            }
            catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        public static int Main(String[] args)
        {
            Thread.Sleep(1000);
            StartClient();
            Console.ReadLine();
            return 0;
        }
    }
}
