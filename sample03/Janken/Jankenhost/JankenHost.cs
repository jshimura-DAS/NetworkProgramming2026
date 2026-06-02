using System.Net;
using System.Net.Sockets;
using Common;

namespace Jankenhost
{
    internal class Jankenhost
    {
        public static void Main()
        {
            Console.WriteLine("HostErrorHandling is starting...");
            SocketServer();
        }

        public static void SocketServer()
        {
            //ここからIPアドレスやポートの設定
            // 着信データ用のデータバッファー。
            //byte[] bytes = new byte[1024];
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

            // データの受信と検証(ProtocolHandlerを使用)
            var receiveResult = ProtocolHandler.ReceiveData(handler);

            if (!receiveResult.Success)
            {
                // エラーが発生した場合
                Console.WriteLine($"エラー検知: {receiveResult.ErrorMessage}");
                Console.WriteLine($"エラータイプ: {ProtocolHandler.GetErrorDescription(receiveResult.ErrorType)}");

                // 接続を切り、セッションを終了
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
                listener.Close();
                return;
            }

            // 正常に受信したデータを表示
            Console.WriteLine($"受信データ: {receiveResult.Data}");

            // 大文字に変更
            string responseData = receiveResult.Data.ToUpper();

            // クライアントにProtocolHandlerを使って返す
            if (!ProtocolHandler.SendData(handler, responseData))
            {
                Console.WriteLine("送信に失敗しました。");
            }
            else
            {
                Console.WriteLine($"送信データ: {responseData}");
            }

            //ソケットの終了
            handler.Shutdown(SocketShutdown.Both);
            handler.Close();
        }

    }
}
