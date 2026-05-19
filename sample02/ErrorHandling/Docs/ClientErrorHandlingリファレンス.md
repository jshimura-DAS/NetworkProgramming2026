# ClientErrorHandling クイックリファレンス

## 目次
- [基本用語](#基本用語)
- [重要なクラス](#重要なクラス)
- [コード断片集](#コード断片集)
- [トラブルシューティング](#トラブルシューティング)

---

## 基本用語

| 用語 | 読み方 | 意味 | 例 |
|------|--------|------|-----|
| TCP | ティーシーピー | 信頼性の高い通信プロトコル | - |
| IP | アイピー | インターネットプロトコル | - |
| Socket | ソケット | ネットワーク通信の出入り口 | 電話機のようなもの |
| Port | ポート | プログラムを識別する番号 | 11000 |
| IPAddress | アイピーアドレス | コンピュータの住所 | 192.168.1.100 |
| EndPoint | エンドポイント | IPアドレス + ポート | 192.168.1.100:11000 |
| Buffer | バッファ | データを一時的に保存する場所 | byte[1024] |
| EOF | イーオーエフ | End Of File（データ終端） | `<EOF>` |
| Encoding | エンコーディング | 文字コード | UTF-8 |

---

## 重要なクラス

### System.Net.Sockets.Socket

ネットワーク通信の基本クラス

| メソッド/プロパティ | 説明 | 戻り値 |
|-------------------|------|--------|
| `Connect(EndPoint)` | サーバーに接続 | void |
| `Send(byte[])` | データを送信 | int (送信バイト数) |
| `Receive(byte[])` | データを受信 | int (受信バイト数) |
| `Shutdown(SocketShutdown)` | 通信を停止 | void |
| `Close()` | ソケットを閉じる | void |
| `AddressFamily` | アドレスファミリーを取得 | AddressFamily |

### System.Net.IPAddress

IPアドレスを表すクラス

| メソッド/プロパティ | 説明 | 戻り値 |
|-------------------|------|--------|
| `Parse(string)` | 文字列からIPアドレスを作成 | IPAddress |
| `AddressFamily` | IPv4かIPv6かを取得 | AddressFamily |

### System.Net.Dns

DNS関連の操作を行うクラス

| メソッド | 説明 | 戻り値 |
|---------|------|--------|
| `GetHostName()` | 自分のホスト名を取得 | string |
| `GetHostEntry(string)` | ホスト情報を取得 | IPHostEntry |

### System.Net.IPEndPoint

エンドポイント（IPアドレス + ポート）を表すクラス

| プロパティ | 説明 | 型 |
|----------|------|-----|
| `Address` | IPアドレス | IPAddress |
| `Port` | ポート番号 | int |

### System.Text.Encoding

文字列とバイト配列の相互変換

| メソッド | 説明 | 戻り値 |
|---------|------|--------|
| `UTF8.GetBytes(string)` | 文字列→バイト配列 | byte[] |
| `UTF8.GetString(byte[], int, int)` | バイト配列→文字列 | string |

---

## コード断片集

### 基本的な接続パターン

#### パターン1: ローカルホストに接続

```csharp
// 自分のPCに接続
string hostName = Dns.GetHostName();
IPHostEntry ipHostInfo = Dns.GetHostEntry(hostName);
IPAddress ipAddress = ipHostInfo.AddressList[2];
IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

Socket socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
socket.Connect(remoteEP);
```

#### パターン2: 指定したIPアドレスに接続

```csharp
// 特定のIPアドレスに接続
IPAddress ipAddress = IPAddress.Parse("192.168.1.100");
IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
socket.Connect(remoteEP);
```

#### パターン3: ループバックアドレス（開発用）

```csharp
// ループバックアドレス（127.0.0.1）を使用
IPAddress ipAddress = IPAddress.Loopback;
IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
socket.Connect(remoteEP);
```

---

### データ送受信パターン

#### パターン1: ProtocolHandlerを使用（推奨）

```csharp
// 送信
string message = "Hello World!";
if (!ProtocolHandler.SendData(socket, message))
{
	Console.WriteLine("送信失敗");
	return;
}

// 受信
var result = ProtocolHandler.ReceiveData(socket);
if (result.Success)
{
	Console.WriteLine($"受信: {result.Data}");
}
else
{
	Console.WriteLine($"エラー: {result.ErrorMessage}");
}
```

#### パターン2: 直接送受信（非推奨）

```csharp
// 送信
string message = "Hello<EOF>";
byte[] sendBytes = Encoding.UTF8.GetBytes(message);
socket.Send(sendBytes);

// 受信
byte[] buffer = new byte[1024];
int bytesRec = socket.Receive(buffer);
string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRec);
Console.WriteLine(receivedData);
```

---

### エラーハンドリングパターン

#### パターン1: try-catch（接続エラー）

```csharp
try
{
	socket.Connect(remoteEP);
	Console.WriteLine("接続成功");
}
catch (SocketException ex)
{
	Console.WriteLine($"接続エラー: {ex.Message}");
	return;
}
catch (Exception ex)
{
	Console.WriteLine($"予期しないエラー: {ex.Message}");
	return;
}
```

#### パターン2: 戻り値チェック（送信エラー）

```csharp
if (!ProtocolHandler.SendData(socket, message))
{
	Console.WriteLine("送信に失敗しました");
	socket.Close();
	return;
}
```

#### パターン3: 結果オブジェクトチェック（受信エラー）

```csharp
var result = ProtocolHandler.ReceiveData(socket);
if (!result.Success)
{
	switch (result.ErrorType)
	{
		case ProtocolHandler.ReceiveErrorType.DataTooLarge:
			Console.WriteLine("データが大きすぎます");
			break;
		case ProtocolHandler.ReceiveErrorType.MissingEOF:
			Console.WriteLine("EOFが見つかりません");
			break;
		case ProtocolHandler.ReceiveErrorType.ConnectionClosed:
			Console.WriteLine("接続が切断されました");
			break;
		default:
			Console.WriteLine($"エラー: {result.ErrorMessage}");
			break;
	}
}
```

---

### ソケットの終了パターン

#### パターン1: 正常終了

```csharp
// 通信を停止
socket.Shutdown(SocketShutdown.Both);

// ソケットを閉じる
socket.Close();
```

#### パターン2: エラー時の終了

```csharp
try
{
	socket.Shutdown(SocketShutdown.Both);
}
catch (Exception ex)
{
	// Shutdownがエラーでも続行
	Console.WriteLine($"Shutdown エラー: {ex.Message}");
}
finally
{
	// 必ず Close を実行
	socket.Close();
}
```

#### パターン3: usingステートメント（.NET 5以降）

```csharp
using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
socket.Connect(remoteEP);

// ... 通信処理 ...

// usingブロックを抜けると自動的にDisposeされる
```

---

### デバッグ用コード

#### 接続情報の表示

```csharp
Console.WriteLine($"ホスト名: {Dns.GetHostName()}");
IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
Console.WriteLine("IPアドレス一覧:");
for (int i = 0; i < ipHostInfo.AddressList.Length; i++)
{
	Console.WriteLine($"  [{i}] {ipHostInfo.AddressList[i]}");
}
```

#### 送受信データの16進数表示

```csharp
byte[] data = Encoding.UTF8.GetBytes("Hello");
Console.WriteLine("16進数:");
foreach (byte b in data)
{
	Console.Write($"{b:X2} "); // 16進数2桁で表示
}
Console.WriteLine();
```

#### データサイズの確認

```csharp
string message = "Hello World!";
byte[] bytes = Encoding.UTF8.GetBytes(message);
Console.WriteLine($"文字数: {message.Length}");
Console.WriteLine($"バイト数: {bytes.Length}");
```

---

## トラブルシューティング

### 接続エラー

| エラーメッセージ | 原因 | 解決方法 |
|----------------|------|---------|
| "接続が拒否されました" | サーバーが起動していない | サーバーを先に起動 |
| "接続がタイムアウト" | IPアドレスが間違っている | IPアドレスを確認 |
| "ホストに到達できません" | ネットワーク切断 | ネットワーク接続を確認 |
| "ポートが既に使用されています" | 他のプログラムが使用中 | 別のポート番号を使用 |

### 送信エラー

| エラーメッセージ | 原因 | 解決方法 |
|----------------|------|---------|
| "送信に失敗しました" | データが1024バイト超 | データを分割 |
| "接続が切断されました" | サーバーが切断した | 再接続 |
| "送信バッファがいっぱい" | 大量のデータを高速送信 | 送信間隔を空ける |

### 受信エラー

| エラーメッセージ | 原因 | 解決方法 |
|----------------|------|---------|
| "EOFが見つかりません" | 送信側がEOF付け忘れ | SendData()を使用 |
| "データが大きすぎます" | 1024バイト超のデータ | データ分割プロトコル実装 |
| "接続が切断されました" | サーバーが切断 | エラー処理を追加 |

### プログラムが動かない

| 症状 | 原因 | 解決方法 |
|------|------|---------|
| 接続後に止まる | Receive()で待機中 | サーバーがデータを送っているか確認 |
| 例外が発生して終了 | エラーハンドリング不足 | try-catchを追加 |
| データが文字化け | エンコーディング不一致 | UTF-8を使用 |
| メモリリーク | Close()忘れ | 必ずClose()を呼ぶ |

---

## よくある質問 (FAQ)

### Q1: AddressList[2]はなぜ[2]なのか？

**A:** コンピュータには複数のIPアドレスが割り当てられることがあります。

```
[0]: ::1              (IPv6ループバック)
[1]: fe80::xxxx       (IPv6リンクローカル)
[2]: 192.168.1.100    (IPv4) ← これを使いたい
```

環境によって異なるため、実際には以下のようにフィルタすべき:

```csharp
IPAddress ipAddress = ipHostInfo.AddressList
	.First(ip => ip.AddressFamily == AddressFamily.InterNetwork);
```

### Q2: なぜShutdownとCloseの両方が必要？

**A:**
- **Shutdown**: 相手に「通信終了」を通知
- **Close**: 自分側のリソースを解放

両方実行することで、正しくクリーンアップできます。

### Q3: 日本語を送信するとバイト数が増えるのはなぜ？

**A:** UTF-8エンコーディングでは、日本語1文字が3バイトになります。

```
"Hello"   → 5バイト (1文字1バイト)
"こんにちは" → 15バイト (1文字3バイト)
```

### Q4: 1024バイトの制限は変更できる？

**A:** はい。`ProtocolHandler.cs` の `MaxBufferSize` を変更すれば可能です。

```csharp
private const int MaxBufferSize = 2048; // 2048バイトに変更
```

ただし、**クライアントとサーバーの両方で同じ値にする必要があります**。

### Q5: 非同期で通信したい場合は？

**A:** `async/await` を使用します。

```csharp
// 非同期接続
await socket.ConnectAsync(remoteEP);

// 非同期送信
await socket.SendAsync(buffer, SocketFlags.None);

// 非同期受信
int bytesRec = await socket.ReceiveAsync(buffer, SocketFlags.None);
```

---

## コーディング規約

### 推奨事項

✅ **DO**
- `ProtocolHandler` を使用してデータ送受信
- エラーハンドリングを必ず実装
- `Shutdown()` と `Close()` を必ず呼ぶ
- IPアドレスは設定ファイルから読み込む
- UTF-8エンコーディングを使用

❌ **DON'T**
- 直接 `socket.Send()` / `Receive()` を使用
- エラーを無視
- `Close()` を忘れる
- ハードコードでIPアドレスを埋め込む
- Shift-JISなど他のエンコーディングを使う

### 命名規則

| 対象 | 規則 | 例 |
|------|------|-----|
| Socket変数 | `socket` | `Socket socket` |
| IPAddress変数 | `ipAddress` | `IPAddress ipAddress` |
| EndPoint変数 | `remoteEP` / `localEP` | `IPEndPoint remoteEP` |
| バッファ変数 | `buffer` / `bytes` | `byte[] buffer` |
| 受信データ変数 | `receivedData` | `string receivedData` |

---

## パフォーマンスのヒント

### 最適化のポイント

1. **バッファサイズ**
   ```csharp
   // 適切なサイズを設定
   byte[] buffer = new byte[1024]; // 小さすぎず大きすぎず
   ```

2. **接続の再利用**
   ```csharp
   // 複数回通信する場合、接続を維持
   socket.Connect(remoteEP);
   for (int i = 0; i < 10; i++)
   {
	   ProtocolHandler.SendData(socket, $"Message {i}");
   }
   socket.Close();
   ```

3. **非同期処理**
   ```csharp
   // UIがブロックされないように非同期化
   await socket.ConnectAsync(remoteEP);
   ```

---

## セキュリティの注意点

### 脆弱性を避けるために

⚠️ **警告**
- 受信データを検証せずに実行しない
- SQLインジェクション対策
- バッファオーバーフロー対策（1024バイト制限）

```csharp
// 危険な例
string command = receiveResult.Data;
Process.Start(command); // ← 絶対にしない！

// 安全な例
if (receiveResult.Success)
{
	// データを検証してから使用
	if (IsValidData(receiveResult.Data))
	{
		ProcessData(receiveResult.Data);
	}
}
```

---

## 参考リンク

### Microsoft公式ドキュメント

- [Socket クラス](https://learn.microsoft.com/ja-jp/dotnet/api/system.net.sockets.socket)
- [IPAddress クラス](https://learn.microsoft.com/ja-jp/dotnet/api/system.net.ipaddress)
- [TCP/IP の概要](https://learn.microsoft.com/ja-jp/dotnet/framework/network-programming/)

### 関連ファイル

- `ClientErrorHandling.cs` - クライアント実装
- `HostErrorHandling.cs` - サーバー実装
- `ProtocolHandler.cs` - プロトコル処理
- `ClientErrorHandling解説.md` - 詳細解説
- `ClientErrorHandling図解.md` - 図解資料

---

**最終更新**: 2025年  
**対象バージョン**: .NET 10  
**作成者**: ネットワークプログラミング学習用
