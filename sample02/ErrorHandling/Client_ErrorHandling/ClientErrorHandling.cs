using System.Net;
using System.Net.Sockets;
using System.Text;
using Common;

namespace Client_ErrorHandling
{
    internal class ClientErrorHandling
    {
        public static void Main()
        {
            //今回送るHello World!
            string st = "Hello World!Shimura";
            Console.WriteLine("lientErrorHandling");
            SocketClient(st);
            Console.ReadKey();
        }


        public static void SocketClient(string st)
        {
            //IPアドレスやポートを設定(自PC、ポート:11000）
            string hostName = Dns.GetHostName();
            IPHostEntry ipHostInfo = Dns.GetHostEntry(hostName);
            IPAddress ipAddress = ipHostInfo.AddressList[2];
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
                Console.WriteLine($"Connect Faild{e.ToString()}");
                return;
            }

            // ProtocolHandlerを使ってデータを送信
            Console.WriteLine($"送信データ: {st}");
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
                Console.WriteLine($"受信データ: {receiveResult.Data}");
            }

            //ソケットを終了している。
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }

    }
}
