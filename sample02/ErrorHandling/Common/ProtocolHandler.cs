using System.Net.Sockets;
using System.Text;

namespace Common
{
    /// <summary>
    /// ネットワーク通信のプロトコルハンドリングを行うクラス
    /// </summary>
    public class ProtocolHandler
    {
        private const int MaxBufferSize = 1024;
        private const string EndOfFile = "<EOF>";

        /// <summary>
        /// データ受信結果
        /// </summary>
        public class ReceiveResult
        {
            public bool Success { get; set; }
            public string Data { get; set; } = string.Empty;
            public string ErrorMessage { get; set; } = string.Empty;
            public ReceiveErrorType ErrorType { get; set; }
        }

        /// <summary>
        /// 受信エラーの種類
        /// </summary>
        public enum ReceiveErrorType
        {
            None,
            DataTooLarge,           // 1024バイトを超えるデータ
            MissingEOF,             // EOFが無い
            ConnectionClosed,       // セッションを切られた
            DataCorruption          // データの異常
        }

        /// <summary>
        /// ソケットからデータを受信し、プロトコルに基づいて検証を行う
        /// </summary>
        /// <param name="socket">受信するソケット</param>
        /// <returns>受信結果</returns>
        public static ReceiveResult ReceiveData(Socket socket)
        {
            var result = new ReceiveResult { Success = false };
            byte[] buffer = new byte[MaxBufferSize];
            int totalBytesReceived = 0;

            try
            {
                // データを受信
                int bytesReceived = socket.Receive(buffer);

                // セッションを切られた場合
                if (bytesReceived == 0)
                {
                    result.ErrorType = ReceiveErrorType.ConnectionClosed;
                    result.ErrorMessage = "接続が切断されました。";
                    return result;
                }

                totalBytesReceived = bytesReceived;

                // 1024バイトを超えているかチェック
                if (totalBytesReceived > MaxBufferSize)
                {
                    result.ErrorType = ReceiveErrorType.DataTooLarge;
                    result.ErrorMessage = $"データサイズが制限({MaxBufferSize}バイト)を超えています。";
                    return result;
                }

                // 受信データを文字列に変換
                string receivedData = Encoding.UTF8.GetString(buffer, 0, totalBytesReceived);

                // <EOF>の位置を確認
                int eofIndex = receivedData.IndexOf(EndOfFile);

                // <EOF>が無い場合
                if (eofIndex == -1)
                {
                    result.ErrorType = ReceiveErrorType.MissingEOF;
                    result.ErrorMessage = "データ終端マーカー<EOF>が見つかりません。";
                    return result;
                }

                // <EOF>より前のデータを取得(<EOF>の後ろは無視)
                result.Data = receivedData.Substring(0, eofIndex);
                result.Success = true;
                result.ErrorType = ReceiveErrorType.None;

                return result;
            }
            catch (SocketException ex)
            {
                result.ErrorType = ReceiveErrorType.ConnectionClosed;
                result.ErrorMessage = $"ソケットエラー: {ex.Message}";
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorType = ReceiveErrorType.DataCorruption;
                result.ErrorMessage = $"データ異常: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// データを送信する(自動的に<EOF>を付加)
        /// </summary>
        /// <param name="socket">送信するソケット</param>
        /// <param name="data">送信するデータ</param>
        /// <returns>送信成功の可否</returns>
        public static bool SendData(Socket socket, string data)
        {
            try
            {
                string dataWithEof = data + EndOfFile;
                byte[] bytes = Encoding.UTF8.GetBytes(dataWithEof);

                // データサイズチェック
                if (bytes.Length > MaxBufferSize)
                {
                    Console.WriteLine($"警告: 送信データが{MaxBufferSize}バイトを超えています。");
                    return false;
                }

                socket.Send(bytes);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"送信エラー: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// エラータイプに基づいて詳細メッセージを取得
        /// </summary>
        public static string GetErrorDescription(ReceiveErrorType errorType)
        {
            return errorType switch
            {
                ReceiveErrorType.DataTooLarge => "1024バイトを超えるデータが送られています",
                ReceiveErrorType.MissingEOF => "<EOF>が見つかりません",
                ReceiveErrorType.ConnectionClosed => "セッションを切られました",
                ReceiveErrorType.DataCorruption => "データに異常があります",
                ReceiveErrorType.None => "エラーなし",
                _ => "未知のエラー"
            };
        }
    }
}
