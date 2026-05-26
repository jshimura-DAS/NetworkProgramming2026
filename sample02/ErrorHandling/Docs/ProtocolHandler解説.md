# ProtocolHandler クラス解説

## 目次
1. [概要](#概要)
2. [TCP/IP通信の基礎知識](#tcpip通信の基礎知識)
3. [Socket クラスについて](#socket-クラスについて)
4. [ProtocolHandler クラスの構造](#protocolhandler-クラスの構造)
5. [各メンバーの詳細説明](#各メンバーの詳細説明)
6. [使用例](#使用例)

---

## 概要

**ProtocolHandler** クラスは、ネットワーク通信において**プロトコルに基づくデータの送受信を管理**するクラスです。

このクラスの主な役割：
- ソケットからデータを受信し、検証を行う
- データを送信する際に終端マーカーを付加する
- エラーハンドリングを統一的に管理する

### 使用対象者
TCP/IP通信やネットワークプログラミング初心者向けに設計されています。

---

## TCP/IP通信の基礎知識

### TCP/IPとは？

**TCP/IP** は、インターネットで通信するための基本的なプロトコル（通信ルール）です。

```
┌─────────┐                    ┌─────────┐
│ クライアント │◄────TCP/IP通信────►│ サーバー  │
│ (送信側)  │                    │ (受信側)  │
└─────────┘                    └─────────┘
```

### TCP（Transmission Control Protocol）

**TCP** は以下の特徴があります：

| 特徴 | 説明 |
|------|------|
| **信頼性** | データの配送を保証します。失われたデータは再送されます |
| **順序性** | 送信順序通りにデータが届きます |
| **接続型** | 通信前に接続を確立し、通信後に切断します |
| **低速** | UDPより遅いですが、信頼性があります |

### 通信の流れ

```
1. 接続確立 (Three-Way Handshake)
   Client                Server
   |---SYN------->|
   |<---SYN-ACK---|
   |-----ACK----->|

2. データ送受信
   Client                Server
   |----データ----->|
   |<--ACK(確認)---|

3. 接続切断
   Client                Server
   |----FIN----->|
   |<---ACK-----|
```

---

## Socket クラスについて

### Socket とは？

**Socket** は、ネットワーク通信の「入り口」です。データの送受信を行うためのオブジェクトです。

```
┌─────────────────────┐
│   ProtocolHandler   │
│   (送受信を管理)     │
└─────────┬───────────┘
		  │
┌─────────▼───────────┐
│   Socket クラス      │
│  (データ送受信)     │
└─────────┬───────────┘
		  │
┌─────────▼───────────┐
│  TCP/IP ネットワーク │
│                     │
└─────────────────────┘
```

### Socket の主なメソッド

| メソッド | 説明 |
|---------|------|
| `Receive(buffer)` | ネットワークからデータを受信 |
| `Send(bytes)` | ネットワークへデータを送信 |
| `Connect(endpoint)` | サーバーに接続 |
| `Listen()` | 接続待機 |
| `Close()` | 接続を切断 |

### 使用例

```csharp
using System.Net.Sockets;

// TCPクライアントソケットの作成
Socket socket = new Socket(
	AddressFamily.InterNetwork,      // IPv4を使用
	SocketType.Stream,               // ストリーム型（TCP）
	ProtocolType.Tcp                 // TCPプロトコルを使用
);
```

---

## ProtocolHandler クラスの構造

### クラス図

```
ProtocolHandler (静的クラス)
├── 定数
│   ├── MaxBufferSize = 1024
│   └── EndOfFile = "<EOF>"
├── 内部クラス
│   └── ReceiveResult
│       ├── Success
│       ├── Data
│       ├── ErrorMessage
│       └── ErrorType
├── 列挙型
│   └── ReceiveErrorType
│       ├── None
│       ├── DataTooLarge
│       ├── MissingEOF
│       ├── ConnectionClosed
│       └── DataCorruption
└── 静的メソッド
	├── ReceiveData()
	├── SendData()
	├── GetErrorDescription()
	└── BuildTestString()
```

### 定数の説明

```csharp
private const int MaxBufferSize = 1024;    // 1回に受信可能な最大サイズ
private const string EndOfFile = "<EOF>"; // データの終端を示すマーカー
```

**なぜ終端マーカーが必要か？**

TCP は「ストリーム型」なので、データの境界が曖昧です。

```
例：複数のデータを送信する場合

【マーカーなし】
送信: "Hello" + "World"
受信: "HelloWorld"  ← どこが区切り目か不明

【マーカーあり】
送信: "Hello<EOF>" + "World<EOF>"
受信側で<EOF>を探す → "Hello" と "World" を分離可能
```

---

## 各メンバーの詳細説明

### 1. ReceiveResult クラス

受信結果を表すクラスです。

```csharp
public class ReceiveResult
{
	public bool Success { get; set; }              // 受信成功か？
	public string Data { get; set; }               // 受信したデータ
	public string ErrorMessage { get; set; }       // エラーメッセージ
	public ReceiveErrorType ErrorType { get; set; } // エラーの種類
}
```

**使用例**

```csharp
ReceiveResult result = ProtocolHandler.ReceiveData(socket);

if (result.Success)
{
	Console.WriteLine($"受信: {result.Data}");
}
else
{
	Console.WriteLine($"エラー: {result.ErrorMessage}");
	Console.WriteLine($"エラータイプ: {result.ErrorType}");
}
```

### 2. ReceiveErrorType 列挙型

エラーの種類を分類します。

| エラー種 | 説明 | 原因 |
|---------|------|------|
| **None** | エラーなし | 正常にデータを受信 |
| **DataTooLarge** | データが大きすぎる | 1024バイト以上のデータを受信 |
| **MissingEOF** | 終端マーカーがない | 「<EOF>」が見つからない |
| **ConnectionClosed** | 接続が切断 | クライアントが切った、またはネットワークエラー |
| **DataCorruption** | データが破損 | 予期しない例外発生 |

### 3. ReceiveData メソッド

ソケットからデータを受信し、プロトコルに基づいて検証します。

```csharp
public static ReceiveResult ReceiveData(Socket socket)
```

#### 処理フロー

```
1. 受信準備
   ├─ バッファサイズ確保 (1024バイト)
   └─ 受信バイト数カウンター初期化

2. データ受信
   └─ socket.Receive(buffer)でデータ取得

3. 検証ステップ
   ├─ 接続切断チェック
   │  (戻り値が0 → 接続切断)
   ├─ サイズチェック
   │  (1024バイト超過 → エラー)
   ├─ 文字列変換
   │  (バイト列をUTF-8文字列に変換)
   ├─ <EOF>チェック
   │  (<EOF>の位置を検索)
   └─ データ抽出
	  (<EOF>より前のデータのみ返す)

4. エラーハンドリング
   ├─ SocketException → 接続エラー
   └─ その他 → データ破損
```

#### 実装の詳細

```csharp
public static ReceiveResult ReceiveData(Socket socket)
{
	var result = new ReceiveResult { Success = false };
	byte[] buffer = new byte[MaxBufferSize]; // 1024バイトのバッファ
	int totalBytesReceived = 0;

	try
	{
		// ステップ1: データ受信
		int bytesReceived = socket.Receive(buffer);

		// ステップ2: 接続切断チェック
		if (bytesReceived == 0)
		{
			result.ErrorType = ReceiveErrorType.ConnectionClosed;
			result.ErrorMessage = "接続が切断されました。";
			return result;
		}

		totalBytesReceived = bytesReceived;

		// ステップ3: サイズチェック
		if (totalBytesReceived > MaxBufferSize)
		{
			result.ErrorType = ReceiveErrorType.DataTooLarge;
			result.ErrorMessage = $"データサイズが制限({MaxBufferSize}バイト)を超えています。";
			return result;
		}

		// ステップ4: バイト列を文字列に変換
		string receivedData = Encoding.UTF8.GetString(buffer, 0, totalBytesReceived);

		// ステップ5: <EOF>の位置確認
		int eofIndex = receivedData.IndexOf(EndOfFile);

		if (eofIndex == -1)
		{
			result.ErrorType = ReceiveErrorType.MissingEOF;
			result.ErrorMessage = "データ終端マーカー<EOF>が見つかりません。";
			return result;
		}

		// ステップ6: <EOF>より前のデータを抽出
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
```

### 4. SendData メソッド

データを送信する際に自動的に終端マーカーを付加します。

```csharp
public static bool SendData(Socket socket, string data)
```

#### 処理フロー

```
1. 送信データ準備
   ├─ 元のデータ: "Hello"
   ├─ <EOF>を追加: "Hello<EOF>"
   └─ UTF-8でバイト列に変換

2. サイズチェック
   └─ 1024バイト超過 → false返却

3. 送信
   └─ socket.Send(bytes)実行

4. エラーハンドリング
   └─ 例外発生 → false返却
```

#### 実装の詳細

```csharp
public static bool SendData(Socket socket, string data)
{
	try
	{
		// 終端マーカーを追加
		string dataWithEof = data + EndOfFile;

		// UTF-8でバイト列に変換
		byte[] bytes = Encoding.UTF8.GetBytes(dataWithEof);

		// サイズチェック
		if (bytes.Length > MaxBufferSize)
		{
			Console.WriteLine($"警告: 送信データが{MaxBufferSize}バイトを超えています。");
			return false;
		}

		// 送信実行
		socket.Send(bytes);
		return true;
	}
	catch (Exception ex)
	{
		Console.WriteLine($"送信エラー: {ex.Message}");
		return false;
	}
}
```

### 5. GetErrorDescription メソッド

エラータイプから詳細メッセージを取得します。

```csharp
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
```

### 6. BuildTestString メソッド

テスト用に1200文字の文字列を生成します。

```csharp
public static string BuildTestString()
{
	// テスト用文字列を構築
	// 1200文字をアルファベット小文字で埋めた文字列を作成
	char[] buf = new char[1200];
	for (int i = 0; i < buf.Length; i++)
	{
		buf[i] = (char)('a' + (i % 26));
	}
	string st = new string(buf);
	return st;
}
```

**出力例**
```
abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcd... (1200文字)
```

---

## 使用例

### 基本的な使用パターン

#### 例1: データ受信（基本）

```csharp
using System.Net.Sockets;
using Common;

Socket socket = new Socket(
	AddressFamily.InterNetwork,
	SocketType.Stream,
	ProtocolType.Tcp
);

// サーバーに接続（例）
// socket.Connect("192.168.1.100", 5000);

// データ受信
ReceiveResult result = ProtocolHandler.ReceiveData(socket);

if (result.Success)
{
	Console.WriteLine($"✓ 受信成功: {result.Data}");
}
else
{
	Console.WriteLine($"✗ 受信失敗: {result.ErrorMessage}");
	Console.WriteLine($"  エラー詳細: {ProtocolHandler.GetErrorDescription(result.ErrorType)}");
}
```

#### 例2: データ送信（基本）

```csharp
string message = "Hello, Server!";
bool success = ProtocolHandler.SendData(socket, message);

if (success)
{
	Console.WriteLine("✓ 送信成功");
}
else
{
	Console.WriteLine("✗ 送信失敗");
}
```

#### 例3: エラーハンドリング

```csharp
ReceiveResult result = ProtocolHandler.ReceiveData(socket);

switch (result.ErrorType)
{
	case ProtocolHandler.ReceiveErrorType.None:
		Console.WriteLine($"データ受信: {result.Data}");
		break;

	case ProtocolHandler.ReceiveErrorType.ConnectionClosed:
		Console.WriteLine("接続が切断されました");
		// 再接続処理など
		break;

	case ProtocolHandler.ReceiveErrorType.DataTooLarge:
		Console.WriteLine("受信データが大きすぎます");
		// ブロック受信など別の処理へ
		break;

	case ProtocolHandler.ReceiveErrorType.MissingEOF:
		Console.WriteLine("データ形式が不正です");
		// データ再送要求など
		break;

	case ProtocolHandler.ReceiveErrorType.DataCorruption:
		Console.WriteLine("データが破損しています");
		// ログ記録と再試行など
		break;
}
```

#### 例4: テスト用文字列の生成

```csharp
string testData = ProtocolHandler.BuildTestString();
Console.WriteLine($"テスト文字列長: {testData.Length} 文字");

// テストデータを送信
bool success = ProtocolHandler.SendData(socket, testData);
```

---

## 重要な概念まとめ

### 1. ストリーム型通信

TCP は「ストリーム型」なので、データの区切りが不明確です。
→ **終端マーカー `<EOF>` で区切りを明示**

### 2. エラーハンドリング

複数のエラーパターンが存在するため、適切に分類・処理が必要です。
→ **`ReceiveErrorType` で種別ごとに処理**

### 3. バッファオーバーフロー対策

受信バッファサイズを制限することで、予期しない大量データを防ぎます。
→ **`MaxBufferSize = 1024` で制限**

### 4. 文字エンコーディング

バイト列と文字列の相互変換には統一的なエンコーディングが必須です。
→ **`Encoding.UTF8` で統一**

---

## トラブルシューティング

### Q: 「<EOF>が見つかりません」というエラーが出ます

**原因：** 
- クライアント側で `<EOF>` を付加せずにデータ送信している
- データが途中で切れている

**対策：**
- 送信時に必ず `ProtocolHandler.SendData()` を使用する
- クライアント側も同じプロトコルを実装しているか確認

### Q: 「データサイズが制限を超えています」

**原因：**
- 1024バイトを超えるデータを受信しようとしている

**対策：**
- データを分割して送受信する
- `MaxBufferSize` の値を確認し、必要に応じて変更

### Q: 「接続が切断されました」

**原因：**
- ネットワークの問題
- サーバーが予期せず終了
- タイムアウト

**対策：**
- ネットワーク接続を確認
- サーバーが起動しているか確認
- 再接続ロジックを実装

---

## 参考資料

- [System.Net.Sockets.Socket ドキュメント](https://learn.microsoft.com/ja-jp/dotnet/api/system.net.sockets.socket)
- [TCP/IP の基礎](https://ja.wikipedia.org/wiki/TCP/IP)
- [文字エンコーディング - UTF-8](https://ja.wikipedia.org/wiki/UTF-8)
