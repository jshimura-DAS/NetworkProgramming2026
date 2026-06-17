using System.Net;
using System.Net.Sockets;
using System.Text;
using Common;
using JankenLib;


namespace JankenClient
{
    internal class JankenClient
    {
        public static void Main()
        {
            Console.WriteLine("=== じゃんけんクライアント ===");
            Console.WriteLine("じゃんけんの手を選んでください:");
            Console.WriteLine("0: グー");
            Console.WriteLine("1: パー");
            Console.WriteLine("2: チョキ");
            Console.Write("入力 > ");

            string? input = Console.ReadLine();

            if (string.IsNullOrEmpty(input))
            {
                Console.WriteLine("入力がありません。");
                Console.ReadKey();
                return;
            }

            Hand? selectedHand = Janken.ParseHand(input);

            if (selectedHand == null)
            {
                Console.WriteLine("無効な入力です。0, 1, 2 のいずれかを入力してください。");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"あなたの手: {Janken.GetHandName(selectedHand.Value)}");

            SocketClient(input);
            Console.ReadKey();
        }


        public static void SocketClient(string st)
        {
            //IPアドレスやポートを設定(自PC、ポート:11000）
            string hostName = Dns.GetHostName();
            IPHostEntry ipHostInfo = Dns.GetHostEntry(hostName);
            IPAddress ipAddress = ipHostInfo.AddressList[1];
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

            //外部を指定する場合
            // IPAddress ipAddress = IPAddress.Parse("172.25.91.135");
            // IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

            //ソケットを作成
            Socket socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            //接続する。失敗するとエラーで落ちる。
            try
            {
                socket.Connect(remoteEP);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Connect Failed: {e.ToString()}");
                return;
            }

            // ProtocolHandlerを使ってデータを送信
            Console.WriteLine($"\n送信データ: {st}");
            if (!ProtocolHandler.SendData(socket, st))
            {
                Console.WriteLine("送信に失敗しました。");
                socket.Close();
                return;
            }

            // ProtocolHandlerを使ってデータを受信
            var receiveResult = ProtocolHandler.ReceiveData(socket);

            if (!receiveResult.Success)
            {
                // エラーが発生した場合
                Console.WriteLine($"エラー検知: {receiveResult.ErrorMessage}");
                Console.WriteLine($"エラータイプ: {ProtocolHandler.GetErrorDescription(receiveResult.ErrorType)}");
            }
            else
            {
                // 正常に受信したデータを表示
                Console.WriteLine($"\n受信データ:");
                Console.WriteLine(receiveResult.Data);
            }

            //ソケットを終了している。
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }
    }
}
