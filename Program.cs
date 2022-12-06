using System;
using System.Net.Sockets;
using System.Threading;
using SharpDX.XInput;

namespace VibrateTest {
    class Program {
        public static short LeftTrigger { get; set; }
        public static short RightTrigger { get; set; }

        private static void Main(string[] args) {
            if (args.Length == 0) {
                Console.WriteLine("please start with 'client' or 'server'!");
            } else {
                switch (args[0]) {
                    case "server": {
                        ReadFromController();
                        StartServer();
                        break;
                    }
                    case "client": {
                        StartClient(args[1], 7777);
                        break;
                    }
                    default:
                        Console.WriteLine("please start with 'client' or 'server'!");
                        break;
                }
            }
        }

        private static void StartServer() {
            TcpListener tcpListener = new TcpListener(7777);
            tcpListener.Start();
            Console.WriteLine("Server started on port 7777!");
            while (true) {
                try {
                    TcpClient acceptTcpClient = tcpListener.AcceptTcpClient();
                    Console.WriteLine("connected server!");
                    while (acceptTcpClient.Connected) {
                        FromShort(LeftTrigger, out byte left1, out byte left2);
                        FromShort(RightTrigger, out byte right1, out byte right2);
                        acceptTcpClient.Client.Send(new byte[] {
                            left1, left2,
                            right1, right2
                        });
                        Console.WriteLine($"Sending Left: {LeftTrigger} Right: {RightTrigger}");
                        Thread.Sleep(10);
                    }
                } catch (Exception e) {
                    Console.WriteLine("lost connection or something else! " + e.Message);
                }
            }
        }

        private static void ReadFromController() {
            new Thread(() => {
                Controller controller = new Controller(UserIndex.One);
                while (true) {
                    State state = controller.GetState();
                    LeftTrigger = state.Gamepad.LeftTrigger;
                    RightTrigger = state.Gamepad.RightTrigger;
                    Thread.Sleep(1);
                }
            }).Start();
        }

        private static void StartClient(string ip, short port) {
            Controller controller = new Controller(UserIndex.One);
            while (true) {
                try {
                    TcpClient tcpClient = new TcpClient();
                    tcpClient.Connect(ip, port);
                    Console.WriteLine($"Connected to client: {ip}:{port}");
                    while (tcpClient.Connected) {
                        byte[] bytes = new byte[4];
                        tcpClient.Client.Receive(bytes);

                        short left = ToShort(bytes[0], bytes[1]);
                        short right = ToShort(bytes[2], bytes[3]);
                        controller.SetVibration(new Vibration() {
                            LeftMotorSpeed = (ushort) ((left * 256) + 255),
                            RightMotorSpeed = (ushort) ((right * 256) + 255),
                        });
                        Console.WriteLine($"Updated vibration: Left: {left} right: {right}");
                    }
                } catch (Exception e) {
                    Console.WriteLine("lost connection or something else! " + e.Message);
                    Thread.Sleep(100);
                }
            }
        }

        static short ToShort(short byte1, short byte2) => (short) ((byte2 << 8) + byte1);

        static void FromShort(short number, out byte byte1, out byte byte2) {
            byte2 = (byte) (number >> 8);
            byte1 = (byte) (number & 255);
        }
    }
}