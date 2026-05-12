using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SimpleHost
{
    internal class SimpleHost
    {
        public static void Main()
        {
            Console.WriteLine("SimpleHost is starting...");
            SocketServer();
        }

        public static void SocketServer()
        {
            //ここからIPアドレスやポートの設定
            // 着信データ用のデータバッファー。
            byte[] bytes = new byte[1024];
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            Console.WriteLine($"ホスト名: {ipHostInfo.HostName}");
            Console.WriteLine($"IPアドレス一覧 (取得数: {ipHostInfo.AddressList.Length}):");
            for (int i = 0; i < ipHostInfo.AddressList.Length; i++)
            {
                Console.WriteLine($"  [{i}] {ipHostInfo.AddressList[i]} (AddressFamily: {ipHostInfo.AddressList[i].AddressFamily})");
            }

            IPAddress ipAddress = ipHostInfo.AddressList[2];
            Console.WriteLine($"\n使用するIPアドレス: {ipAddress}");

            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);
            Console.WriteLine($"エンドポイント: {localEndPoint}");
            Console.WriteLine($"  - IPアドレス: {localEndPoint.Address}");
            Console.WriteLine($"  - ポート番号: {localEndPoint.Port}");
            Console.WriteLine();
            //ここまでIPアドレスやポートの設定

            //ソケットの作成
            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            //通信の受け入れ準備
            listener.Bind(localEndPoint);
            listener.Listen(10);

            //通信の確率
            Socket handler = listener.Accept();

            // 任意の処理
            //データの受取をReceiveで行う。
            int bytesRec = handler.Receive(bytes);
            string data1 = Encoding.UTF8.GetString(bytes, 0, bytesRec);
            Console.WriteLine(data1);

            //大文字に変更
            data1 = data1.ToUpper();

            //クライアントにSendで返す。
            byte[] msg = Encoding.UTF8.GetBytes(data1);
            handler.Send(msg);

            //ソケットの終了
            handler.Shutdown(SocketShutdown.Both);
            handler.Close();
        }
    }
}
