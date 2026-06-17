using System.Net;
using System.Net.Sockets;
using Common;
using JankenLib;

namespace Jankenhost
{
    internal class Jankenhost
    {
        public static void Main()
        {
            Console.WriteLine("=== じゃんけんホスト ===");
            Console.WriteLine("クライアントからの接続を待っています...\n");
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

            IPAddress ipAddress = ipHostInfo.AddressList[1];
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
            Console.WriteLine("クライアントが接続しました。\n");

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

            // クライアントの手を解析
            Hand? clientHand = Janken.ParseHand(receiveResult.Data);

            if (clientHand == null)
            {
                string errorMessage = "無効な手です。0, 1, 2 のいずれかを送信してください。";
                ProtocolHandler.SendData(handler, errorMessage);
                Console.WriteLine($"送信データ: {errorMessage}");

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
                listener.Close();
                return;
            }

            Console.WriteLine($"クライアントの手: {Janken.GetHandName(clientHand.Value)}");

            // ホストの手をランダムに生成
            Hand hostHand = Janken.GetRandomHand();
            Console.WriteLine($"ホストの手: {Janken.GetHandName(hostHand)}");

            // 勝敗判定（クライアントから見た結果）
            Result result = Janken.Judge(clientHand.Value, hostHand);
            Console.WriteLine($"結果: クライアントの{Janken.GetResultMessage(result)}");

            // 結果メッセージを作成
            string responseData = $"【じゃんけん結果】\n" +
                                  $"あなたの手: {Janken.GetHandName(clientHand.Value)}\n" +
                                  $"ホストの手: {Janken.GetHandName(hostHand)}\n" +
                                  $"結果: あなたの{Janken.GetResultMessage(result)}!";

            // クライアントにProtocolHandlerを使って返す
            if (!ProtocolHandler.SendData(handler, responseData))
            {
                Console.WriteLine("送信に失敗しました。");
            }
            else
            {
                Console.WriteLine($"\n送信データ:\n{responseData}");
            }

            //ソケットの終了
            handler.Shutdown(SocketShutdown.Both);
            handler.Close();
            listener.Close();
        }

    }
}
