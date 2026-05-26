# SimpleHost.cs から HostErrorHandling.cs への移行手順

## 目次
1. [概要](#概要)
2. [移行前後のコード比較](#移行前後のコード比較)
3. [変更点の詳細解説](#変更点の詳細解説)
4. [ステップバイステップ移行手順](#ステップバイステップ移行手順)
5. [動作確認とテスト](#動作確認とテスト)
6. [トラブルシューティング](#トラブルシューティング)

---

## 概要

### 移行の目的

**SimpleHost.cs** (基本版) から **HostErrorHandling.cs** (改良版) への移行により、以下の改善を実現します：

| 項目 | SimpleHost | HostErrorHandling |
|------|-----------|-------------------|
| **エラーハンドリング** | なし | 詳細なエラー検知 |
| **プロトコル検証** | なし | EOF・サイズ検証 |
| **異常データ対応** | 受け入れる | 検知して切断 |
| **セキュリティ** | 低い | 高い（不正データ拒否） |
| **保守性** | 低い（直接Send/Receive） | 高い（ProtocolHandler使用） |
| **エラー情報** | なし | 詳細なエラータイプ |

---

## 移行前後のコード比較

### 完全なコード比較

#### SimpleHost.cs (移行前)

```csharp
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
```

#### HostErrorHandling.cs (移行後)

```csharp
using System.Net;
using System.Net.Sockets;
using System.Text;
using Common;  // ← 追加

namespace Host_ErrorHandling  // ← 変更
{
	internal class HostErrorHandling  // ← 変更
	{
		public static void Main()
		{
			Console.WriteLine("HostErrorHandling is starting...");  // ← 変更
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

			// データの受信と検証(ProtocolHandlerを使用)  // ← 変更開始
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
			}  // ← 変更終了

			//ソケットの終了
			handler.Shutdown(SocketShutdown.Both);
			handler.Close();
		}
	}
}
```

---

### 差分表示（変更箇所のみ）

```diff
--- SimpleHost.cs
+++ HostErrorHandling.cs

 using System.Net;
 using System.Net.Sockets;
 using System.Text;
+using Common;

-namespace SimpleHost
+namespace Host_ErrorHandling
 {
-    internal class SimpleHost
+    internal class HostErrorHandling
	 {
		 public static void Main()
		 {
-            Console.WriteLine("SimpleHost is starting...");
+            Console.WriteLine("HostErrorHandling is starting...");
			 SocketServer();
		 }

		 public static void SocketServer()
		 {
			 // ... (IPアドレス設定は同じ) ...

			 //通信の確率
			 Socket handler = listener.Accept();

-            // 任意の処理
-            //データの受取をReceiveで行う。
-            int bytesRec = handler.Receive(bytes);
-            string data1 = Encoding.UTF8.GetString(bytes, 0, bytesRec);
-            Console.WriteLine(data1);
-
-            //大文字に変更
-            data1 = data1.ToUpper();
-
-            //クライアントにSendで返す。
-            byte[] msg = Encoding.UTF8.GetBytes(data1);
-            handler.Send(msg);
+            // データの受信と検証(ProtocolHandlerを使用)
+            var receiveResult = ProtocolHandler.ReceiveData(handler);
+
+            if (!receiveResult.Success)
+            {
+                // エラーが発生した場合
+                Console.WriteLine($"エラー検知: {receiveResult.ErrorMessage}");
+                Console.WriteLine($"エラータイプ: {ProtocolHandler.GetErrorDescription(receiveResult.ErrorType)}");
+
+                // 接続を切り、セッションを終了
+                handler.Shutdown(SocketShutdown.Both);
+                handler.Close();
+                listener.Close();
+                return;
+            }
+
+            // 正常に受信したデータを表示
+            Console.WriteLine($"受信データ: {receiveResult.Data}");
+
+            // 大文字に変更
+            string responseData = receiveResult.Data.ToUpper();
+
+            // クライアントにProtocolHandlerを使って返す
+            if (!ProtocolHandler.SendData(handler, responseData))
+            {
+                Console.WriteLine("送信に失敗しました。");
+            }
+            else
+            {
+                Console.WriteLine($"送信データ: {responseData}");
+            }

			 //ソケットの終了
			 handler.Shutdown(SocketShutdown.Both);
			 handler.Close();
		 }
	 }
 }
```

---

## 変更点の詳細解説

### 変更1: usingディレクティブの追加

#### 変更前
```csharp
using System.Net;
using System.Net.Sockets;
using System.Text;
```

#### 変更後
```csharp
using System.Net;
using System.Net.Sockets;
using System.Text;
using Common;  // ← 追加
```

**理由**: `ProtocolHandler` クラスを使用するため、`Common` 名前空間をインポート

**詳細**:
- `Common` 名前空間には `ProtocolHandler` クラスが含まれている
- この1行を追加しないと、コンパイルエラーが発生する
- クライアント側と同じプロトコルハンドラーを使用することで一貫性を保つ

---

### 変更2: 名前空間とクラス名の変更

#### 変更前
```csharp
namespace SimpleHost
{
	internal class SimpleHost
```

#### 変更後
```csharp
namespace Host_ErrorHandling
{
	internal class HostErrorHandling
```

**理由**: プロジェクト名に合わせて名前空間とクラス名を変更

**詳細**:
- **名前空間**: `SimpleHost` → `Host_ErrorHandling`
  - プロジェクトフォルダ名と一致させる
  - コードの所在を明確にする

- **クラス名**: `SimpleHost` → `HostErrorHandling`
  - エラーハンドリング機能があることを名前で示す
  - ファイル名と一致させる慣習に従う

---

### 変更3: コンソール出力メッセージの変更

#### 変更前
```csharp
Console.WriteLine("SimpleHost is starting...");
```

#### 変更後
```csharp
Console.WriteLine("HostErrorHandling is starting...");
```

**理由**: プログラム名を正確に表示

**詳細**:
- ユーザーにどのプログラムが起動したかを知らせる
- デバッグ時に複数のプログラムを実行している場合に識別しやすい
- ログファイルに記録する際に識別が容易

---

### 変更4: データ受信部分の変更（最重要）

#### 変更前（SimpleHost.cs）

```csharp
// 任意の処理
//データの受取をReceiveで行う。
int bytesRec = handler.Receive(bytes);
string data1 = Encoding.UTF8.GetString(bytes, 0, bytesRec);
Console.WriteLine(data1);
```

**問題点**:
- ❌ データの検証なし（不正なデータも受け入れる）
- ❌ EOF検証なし（どこまでが有効なデータか不明）
- ❌ サイズチェックなし（大きすぎるデータも受信）
- ❌ 切断検知なし（`bytesRec == 0` のチェックなし）
- ❌ `<EOF>` も含めて表示してしまう

**セキュリティリスク**:
```
攻撃者が以下を送信可能:
- EOFなしの無限ストリーム → サーバーがハング
- 1024バイト超のデータ → バッファオーバーフローの可能性
- 不正な形式のデータ → 予期しない動作
```

#### 変更後（HostErrorHandling.cs）

```csharp
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
```

**改善点**:
- ✅ データの完全性検証（EOF必須）
- ✅ サイズチェック実施（1024バイト制限）
- ✅ 切断検知（`bytesReceived == 0` を検知）
- ✅ エラー時は接続を切断（セキュリティ）
- ✅ `<EOF>` を自動除去
- ✅ 詳細なエラー情報を取得

**エラー検知の流れ**:

```
データ受信
	↓
┌───────────────────┐
│ ReceiveData()     │
│ - サイズチェック   │
│ - EOF検証         │
│ - 切断検知        │
└───────────────────┘
	↓
  成功？
	├─ YES → データを処理
	└─ NO  → エラー処理
			  ├─ エラー表示
			  ├─ 接続切断
			  └─ 終了
```

**ProtocolHandler.ReceiveData() の内部動作**:

```csharp
public static ReceiveResult ReceiveData(Socket socket)
{
	var result = new ReceiveResult { Success = false };
	byte[] buffer = new byte[1024];

	try
	{
		// 1. データ受信
		int bytesReceived = socket.Receive(buffer);

		// 2. 切断検知（最重要！）
		if (bytesReceived == 0)
		{
			result.ErrorType = ReceiveErrorType.ConnectionClosed;
			result.ErrorMessage = "接続が切断されました。";
			return result;
		}

		// 3. サイズチェック
		if (bytesReceived > 1024)
		{
			result.ErrorType = ReceiveErrorType.DataTooLarge;
			result.ErrorMessage = "データサイズが制限(1024バイト)を超えています。";
			return result;
		}

		// 4. 文字列に変換
		string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesReceived);

		// 5. EOF検証（プロトコル遵守チェック）
		int eofIndex = receivedData.IndexOf("<EOF>");
		if (eofIndex == -1)
		{
			result.ErrorType = ReceiveErrorType.MissingEOF;
			result.ErrorMessage = "データ終端マーカー<EOF>が見つかりません。";
			return result;
		}

		// 6. データ抽出（EOFより前のみ）
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

---

### 変更5: エラーハンドリングの追加（新規）

#### SimpleHost.cs にはなかった処理

```csharp
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
```

**理由**: 不正なデータを受け入れないセキュアなサーバーにするため

**詳細**:

**1. エラー情報の表示**
```csharp
Console.WriteLine($"エラー検知: {receiveResult.ErrorMessage}");
Console.WriteLine($"エラータイプ: {ProtocolHandler.GetErrorDescription(receiveResult.ErrorType)}");
```
- サーバー管理者に何が起きたかを通知
- ログに記録することで後から分析可能
- デバッグ時に問題の特定が容易

**2. 接続の切断**
```csharp
handler.Shutdown(SocketShutdown.Both);
handler.Close();
listener.Close();
```
- `handler.Shutdown(SocketShutdown.Both)`: 送受信を停止
- `handler.Close()`: クライアントとの接続を閉じる
- `listener.Close()`: リスニングソケットも閉じる

**3. 早期リターン**
```csharp
return;
```
- 不正なデータの場合、それ以上処理しない
- セキュリティ上重要（不正なデータを処理しない）

**エラーの種類と対応**:

| エラータイプ | 意味 | 対応 |
|------------|------|------|
| `ConnectionClosed` | クライアントが切断 | ログに記録、次のクライアント待機 |
| `DataTooLarge` | 1024バイト超 | 拒否、接続切断 |
| `MissingEOF` | EOF未検出 | 拒否、接続切断 |
| `DataCorruption` | データ異常 | 拒否、接続切断 |

---

### 変更6: データ処理部分の変更

#### 変更前（SimpleHost.cs）

```csharp
//大文字に変更
data1 = data1.ToUpper();
```

**問題点**:
- 変数の再利用（`data1` を上書き）
- 元のデータと変換後のデータが区別できない

#### 変更後（HostErrorHandling.cs）

```csharp
// 正常に受信したデータを表示
Console.WriteLine($"受信データ: {receiveResult.Data}");

// 大文字に変更
string responseData = receiveResult.Data.ToUpper();
```

**改善点**:
- ✅ 受信データを表示（何を受け取ったか明確）
- ✅ 新しい変数 `responseData` を使用（元データは保持）
- ✅ 処理の流れが明確

---

### 変更7: データ送信部分の変更

#### 変更前（SimpleHost.cs）

```csharp
//クライアントにSendで返す。
byte[] msg = Encoding.UTF8.GetBytes(data1);
handler.Send(msg);
```

**問題点**:
- ❌ EOFを手動で追加していない（プロトコル違反）
- ❌ サイズチェックなし
- ❌ エラーハンドリングなし
- ❌ 送信内容のログなし

#### 変更後（HostErrorHandling.cs）

```csharp
// クライアントにProtocolHandlerを使って返す
if (!ProtocolHandler.SendData(handler, responseData))
{
	Console.WriteLine("送信に失敗しました。");
}
else
{
	Console.WriteLine($"送信データ: {responseData}");
}
```

**改善点**:
- ✅ 自動でEOF追加（プロトコル遵守）
- ✅ サイズチェック実施
- ✅ エラーハンドリング（送信失敗を検知）
- ✅ 送信内容のログ出力

**ProtocolHandler.SendData() の内部動作**:

```csharp
public static bool SendData(Socket socket, string data)
{
	try
	{
		// 1. EOFを自動で追加
		string dataWithEof = data + "<EOF>";

		// 2. UTF-8バイト配列に変換
		byte[] bytes = Encoding.UTF8.GetBytes(dataWithEof);

		// 3. サイズチェック
		if (bytes.Length > 1024)
		{
			Console.WriteLine($"警告: 送信データが1024バイトを超えています。");
			return false;  // 失敗を通知
		}

		// 4. 送信
		socket.Send(bytes);
		return true;  // 成功を通知
	}
	catch (Exception ex)
	{
		Console.WriteLine($"送信エラー: {ex.Message}");
		return false;  // 失敗を通知
	}
}
```

---

### 変更8: 不要な変数の削除

#### 変更前（SimpleHost.cs）

```csharp
// 着信データ用のデータバッファー。
byte[] bytes = new byte[1024];
```

この変数は `handler.Receive(bytes)` で使用されていました。

#### 変更後（HostErrorHandling.cs）

```csharp
// 着信データ用のデータバッファー。
byte[] bytes = new byte[1024];  // ← 宣言されているが未使用
```

**理由**: `ProtocolHandler.ReceiveData()` が内部でバッファを管理するため不要

**推奨**: この行は削除可能（後述の最適化セクションで説明）

---

## ステップバイステップ移行手順

### 前提条件

- ✅ Visual Studio 2026 がインストールされている
- ✅ .NET 10 SDK がインストールされている
- ✅ `Common` プロジェクトに `ProtocolHandler.cs` が作成済み
- ✅ `Host_ErrorHandling` プロジェクトから `Common` プロジェクトへの参照が設定済み

---

### ステップ1: プロジェクトとファイルの準備

#### 1-1. プロジェクト参照の確認

**Visual Studio 2026で確認**:

1. ソリューションエクスプローラーで `Host_ErrorHandling` プロジェクトを展開
2. **[依存関係]** → **[プロジェクト]** を確認
3. `Common` が表示されていればOK

**表示されていない場合、参照を追加**:

1. ソリューションエクスプローラーで `Host_ErrorHandling` プロジェクトを右クリック
2. **[追加]** → **[プロジェクト参照...]** を選択
3. `Common` プロジェクトにチェックを入れる
4. **[OK]** をクリック
5. ソリューションエクスプローラーで `Host_ErrorHandling` → **[依存関係]** → **[プロジェクト]** に `Common` が表示されることを確認

#### 1-2. SimpleHost.cs を開く

1. 元のファイルを開く: `sample01\SimpleHost\SimpleHost\SimpleHost.cs`
2. コード全体を選択してコピー (Ctrl+A → Ctrl+C)

#### 1-3. HostErrorHandling.cs に貼り付け

1. 新しいファイルを開く: `Host_ErrorHandling\HostErrorHandling.cs`
2. 既存の内容を全て削除
3. コピーしたコードを貼り付け (Ctrl+V)

---

### ステップ2: 基本的な名前の変更

#### 2-1. usingディレクティブの追加

**変更箇所**: ファイルの先頭

**変更前**:
```csharp
using System.Net;
using System.Net.Sockets;
using System.Text;
```

**変更後**:
```csharp
using System.Net;
using System.Net.Sockets;
using System.Text;
using Common;  // ← この行を追加
```

**操作方法**:
1. 3行目の `using System.Text;` の後ろにカーソルを置く
2. `Enter` キーを押して改行
3. `using Common;` と入力

---

#### 2-2. 名前空間の変更

**変更箇所**: 5行目

**変更前**:
```csharp
namespace SimpleHost
```

**変更後**:
```csharp
namespace Host_ErrorHandling
```

**操作方法**:
1. `SimpleHost` を選択 (ダブルクリック)
2. `Host_ErrorHandling` と入力

---

#### 2-3. クラス名の変更

**変更箇所**: 7行目

**変更前**:
```csharp
internal class SimpleHost
```

**変更後**:
```csharp
internal class HostErrorHandling
```

**操作方法**:
1. `SimpleHost` を選択 (ダブルクリック)
2. `HostErrorHandling` と入力

**重要**: ファイル名と一致させる

---

#### 2-4. コンソール出力の変更

**変更箇所**: 9行目（Mainメソッド内）

**変更前**:
```csharp
Console.WriteLine("SimpleHost is starting...");
```

**変更後**:
```csharp
Console.WriteLine("HostErrorHandling is starting...");
```

**操作方法**:
1. `"SimpleHost is starting..."` を選択
2. `"HostErrorHandling is starting..."` と入力

---

### ステップ3: データ受信部分の変更

#### 3-1. 古い受信コードを探す

**検索箇所**: `handler.Accept()` の直後

**削除する部分**（5行）:
```csharp
// 任意の処理
//データの受取をReceiveで行う。
int bytesRec = handler.Receive(bytes);
string data1 = Encoding.UTF8.GetString(bytes, 0, bytesRec);
Console.WriteLine(data1);
```

**操作方法**:
1. 上記の5行を選択
2. `Delete` キーで削除

---

#### 3-2. 新しい受信コードを追加

**追加する場所**: `handler.Accept()` の直後

**追加するコード**:
```csharp
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
```

**完成形**（前後の文脈込み）:
```csharp
//通信の確率
Socket handler = listener.Accept();

// ← ここから追加
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
// ← ここまで追加
```

**コードの意味**:

```csharp
// ProtocolHandlerで受信（結果オブジェクトを取得）
var receiveResult = ProtocolHandler.ReceiveData(handler);

// 成功したかチェック
if (!receiveResult.Success)
{
	// 失敗の場合
	// エラー情報を表示
	Console.WriteLine($"エラー検知: {receiveResult.ErrorMessage}");
	Console.WriteLine($"エラータイプ: {ProtocolHandler.GetErrorDescription(receiveResult.ErrorType)}");

	// セキュリティ対策：不正なクライアントは切断
	handler.Shutdown(SocketShutdown.Both);  // 送受信を停止
	handler.Close();                        // クライアント接続を閉じる
	listener.Close();                       // リスニングソケットも閉じる
	return;                                 // メソッドを終了
}

// 成功の場合はここから続行
Console.WriteLine($"受信データ: {receiveResult.Data}");
```

---

### ステップ4: データ処理部分の変更

#### 4-1. 大文字変換の変更

**変更前**:
```csharp
//大文字に変更
data1 = data1.ToUpper();
```

**変更後**:
```csharp
// 大文字に変更
string responseData = receiveResult.Data.ToUpper();
```

**操作方法**:
1. 古い2行を削除
2. 新しい2行を追加

**重要**: 変数名を `data1` から `responseData` に変更

---

### ステップ5: データ送信部分の変更

#### 5-1. 古い送信コードを探す

**削除する部分**（3行）:
```csharp
//クライアントにSendで返す。
byte[] msg = Encoding.UTF8.GetBytes(data1);
handler.Send(msg);
```

**操作方法**:
1. 上記の3行を選択
2. `Delete` キーで削除

---

#### 5-2. 新しい送信コードを追加

**追加する場所**: 大文字変換の直後

**追加するコード**:
```csharp
// クライアントにProtocolHandlerを使って返す
if (!ProtocolHandler.SendData(handler, responseData))
{
	Console.WriteLine("送信に失敗しました。");
}
else
{
	Console.WriteLine($"送信データ: {responseData}");
}
```

**完成形**（前後の文脈込み）:
```csharp
// 大文字に変更
string responseData = receiveResult.Data.ToUpper();

// ← ここから追加
// クライアントにProtocolHandlerを使って返す
if (!ProtocolHandler.SendData(handler, responseData))
{
	Console.WriteLine("送信に失敗しました。");
}
else
{
	Console.WriteLine($"送信データ: {responseData}");
}
// ← ここまで追加

//ソケットの終了
handler.Shutdown(SocketShutdown.Both);
handler.Close();
```

**コードの意味**:

```csharp
// ProtocolHandlerで送信（戻り値で成功/失敗を判定）
if (!ProtocolHandler.SendData(handler, responseData))
{
	// 失敗の場合
	Console.WriteLine("送信に失敗しました。");
	// エラーログを記録（実運用では重要）
}
else
{
	// 成功の場合
	// 何を送ったかログに記録
	Console.WriteLine($"送信データ: {responseData}");
}
```

---

### ステップ6: ビルドとエラー修正

#### 6-1. ビルドを実行

**Visual Studio 2026で**:

1. メニューバーから **[ビルド]** → **[ソリューションのビルド]** を選択（またはCtrl+Shift+B）
2. **出力** ウィンドウが自動的に開き、ビルドの進行状況が表示されます
3. エラーがある場合は **エラー一覧** ウィンドウに表示されます
   - 表示されていない場合: **[表示]** → **[エラー一覧]** (Ctrl+\, E)
4. エラー行をダブルクリックすると、該当するコード箇所にジャンプします

---

#### 6-2. よくあるエラーと修正方法

##### エラー1: "Common が見つかりません"

**エラーメッセージ**:
```
CS0246: 型または名前空間の名前 'Common' が見つかりませんでした
```

**原因**: プロジェクト参照が設定されていない

**解決方法（Visual Studio 2026）**:

1. ソリューションエクスプローラーで `Host_ErrorHandling` プロジェクトを右クリック
2. **[追加]** → **[プロジェクト参照...]** を選択
3. `Common` プロジェクトにチェックを入れる
4. **[OK]** をクリック
5. 再度ビルドを実行: **[ビルド]** → **[ソリューションのビルド]** (Ctrl+Shift+B)

---

##### エラー2: "ProtocolHandler が見つかりません"

**エラーメッセージ**:
```
CS0103: 現在のコンテキストに 'ProtocolHandler' という名前は存在しません
```

**原因**: `using Common;` が追加されていない

**解決方法**:
ファイルの先頭に以下を追加:
```csharp
using Common;
```

---

##### エラー3: "listener が現在のコンテキストに存在しません"

**エラーメッセージ**:
```
CS0103: 現在のコンテキストに 'listener' という名前は存在しません
```

**原因**: `listener.Close()` を追加したが、`listener` のスコープ外

**解決方法**:
`listener` がメソッド内で宣言されているか確認。エラー処理内で閉じる必要がある場合は、`listener` をメソッドスコープ全体で有効にする。

---

##### エラー4: "data1 が現在のコンテキストに存在しません"

**エラーメッセージ**:
```
CS0103: 現在のコンテキストに 'data1' という名前は存在しません
```

**原因**: `data1` を `responseData` に変更し忘れている

**解決方法**:
すべての `data1` を `responseData` に置き換える（送信部分）。

---

### ステップ7: 最終確認とコード最適化

#### 7-1. 未使用変数の削除（オプション）

**削除対象**:
```csharp
byte[] bytes = new byte[1024];
```

**理由**: `ProtocolHandler.ReceiveData()` が内部でバッファを管理するため不要

**削除前**:
```csharp
public static void SocketServer()
{
	//ここからIPアドレスやポートの設定
	// 着信データ用のデータバッファー。
	byte[] bytes = new byte[1024];  // ← この行は不要
	IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
	// ...
}
```

**削除後**:
```csharp
public static void SocketServer()
{
	//ここからIPアドレスやポートの設定
	IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
	// ...
}
```

---

#### 7-2. コード全体のレビュー

**チェックポイント**:

- [ ] `using Common;` が追加されている
- [ ] 名前空間が `Host_ErrorHandling` になっている
- [ ] クラス名が `HostErrorHandling` になっている
- [ ] 受信部分が `ProtocolHandler.ReceiveData()` を使用
- [ ] エラーハンドリングが追加されている
- [ ] 送信部分が `ProtocolHandler.SendData()` を使用
- [ ] 送信成功/失敗のログ出力がある
- [ ] 未使用変数 `bytes` が削除されている（オプション）

---

#### 7-3. 完成したコードの確認

最終的なコードは以下のようになります:

```csharp
using System.Net;
using System.Net.Sockets;
using System.Text;
using Common;

namespace Host_ErrorHandling
{
	internal class HostErrorHandling
	{
		public static void Main()
		{
			Console.WriteLine("HostErrorHandling is starting...");
			SocketServer();
		}

		public static void SocketServer()
		{
			//ここからIPアドレスやポートの設定
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
```

---

## 動作確認とテスト

### テスト1: 正常系のテスト

#### 手順

1. **ホスト側を起動（Visual Studio 2026）**

   - ソリューションエクスプローラーで `Host_ErrorHandling` プロジェクトを右クリック
   - **[デバッグ]** → **[新しいインスタンスを開始]** を選択
   - コンソールウィンドウが開き、以下のような出力が表示されます:
   出力例:
   ```
   HostErrorHandling is starting...
   ホスト名: YOUR-PC
   IPアドレス一覧 (取得数: 4):
	 [0] ::1 (AddressFamily: InterNetworkV6)
	 [1] fe80::xxxx (AddressFamily: InterNetworkV6)
	 [2] 192.168.1.10 (AddressFamily: InterNetwork)
	 [3] 169.254.xxx.xxx (AddressFamily: InterNetwork)

   使用するIPアドレス: 192.168.1.10
   エンドポイント: 192.168.1.10:11000
	 - IPアドレス: 192.168.1.10
	 - ポート番号: 11000

   (クライアント接続待機中...)
   ```

2. **クライアント側を起動（別のVisual Studioインスタンスまたは同じソリューション内）**

   **方法A: 複数のスタートアッププロジェクトを設定（推奨）**
   - ソリューションを右クリック → **[プロパティ]**
   - **[共通プロパティ]** → **[スタートアッププロジェクト]**
   - **[マルチスタートアッププロジェクト]** を選択
   - `Host_ErrorHandling` と `Client_ErrorHandling` の両方を **[開始]** に設定
   - **[OK]** をクリック
   - F5キーを押すと両方のプログラムが起動します

   **方法B: 別々に起動**
   - ホストを起動した状態で、もう一度ソリューションエクスプローラーから `Client_ErrorHandling` を右クリック
   - **[デバッグ]** → **[新しいインスタンスを開始]** を選択

3. **期待される出力**

   **ホスト側**:
   ```
   受信データ: Hello World!Shimura
   送信データ: HELLO WORLD!SHIMURA
   ```

   **クライアント側**:
   ```
   ClientErrorHandling
   送信データ: Hello World!Shimura
   受信データ: HELLO WORLD!SHIMURA
   ```

#### 確認ポイント

- ✅ ホストがクライアントの接続を受け入れている
- ✅ ホストが受信したメッセージを表示している
- ✅ ホストが大文字に変換している
- ✅ クライアントが正しく受信している
- ✅ `<EOF>` が画面に表示されていない（自動除去されている）

---

### テスト2: EOF無しデータのテスト

#### 手順

**クライアント側のコードを一時的に変更**:

ClientErrorHandling.cs:
```csharp
// ProtocolHandler.SendData() を使わずに直接送信
byte[] msg = Encoding.UTF8.GetBytes(st);  // EOFなし
socket.Send(msg);
```

#### 期待される出力

**ホスト側**:
```
エラー検知: データ終端マーカー<EOF>が見つかりません。
エラータイプ: <EOF>が見つかりません
(プログラム終了)
```

**クライアント側**:
```
ClientErrorHandling
送信データ: Hello World!Shimura
エラー検知: 接続が切断されました。
エラータイプ: セッションを切られました
```

#### 確認ポイント

- ✅ ホストがEOF無しデータを検知している
- ✅ ホストがエラーメッセージを表示している
- ✅ ホストが接続を切断している
- ✅ クライアントが切断を検知している

**テスト後は元に戻す**

---

### テスト3: 大きなデータのテスト

#### 手順

**クライアント側のコードを一時的に変更**:

ClientErrorHandling.cs:
```csharp
public static void Main()
{
	// 大きなデータを生成（2000文字）
	string st = new string('A', 2000);
	Console.WriteLine("ClientErrorHandling");
	SocketClient(st);
	Console.ReadKey();
}
```

#### 期待される出力

**クライアント側**:
```
ClientErrorHandling
送信データ: AAAAAAA... (2000文字)
警告: 送信データが1024バイトを超えています。
送信に失敗しました。
```

**ホスト側**:
```
(クライアント接続待機中...)
(何も受信しない)
```

#### 確認ポイント

- ✅ クライアント側でデータサイズチェックが動作している
- ✅ 1024バイトを超えるデータは送信されない
- ✅ エラーメッセージが表示される
- ✅ ホストに不正なデータが届かない

**テスト後は元に戻す**:
```csharp
string st = "Hello World!Shimura";
```

---

### テスト4: クライアント切断のテスト

#### 手順

**クライアント側のコードを一時的に変更**:

ClientErrorHandling.cs:
```csharp
// データを送信せずにすぐ切断
socket.Connect(remoteEP);
socket.Shutdown(SocketShutdown.Both);
socket.Close();
```

#### 期待される出力

**ホスト側**:
```
エラー検知: 接続が切断されました。
エラータイプ: セッションを切られました
(プログラム終了)
```

#### 確認ポイント

- ✅ ホストがクライアントの切断を検知している
- ✅ ホストが適切にエラー処理している
- ✅ ホストが正常終了している

**テスト後は元に戻す**

---

### テスト5: 日本語データのテスト

#### 手順

**クライアント側のコードを一時的に変更**:

ClientErrorHandling.cs:
```csharp
string st = "こんにちは世界！テストメッセージです。";
```

#### 期待される出力

**ホスト側**:
```
受信データ: こんにちは世界！テストメッセージです。
送信データ: こんにちは世界！テストメッセージです。
```

（大文字変換は日本語に影響しない）

**クライアント側**:
```
ClientErrorHandling
送信データ: こんにちは世界！テストメッセージです。
受信データ: こんにちは世界！テストメッセージです。
```

#### 確認ポイント

- ✅ 日本語が正しく送受信される
- ✅ 文字化けが発生しない
- ✅ UTF-8エンコーディングが正しく機能している

**テスト後は元に戻す**

---

## トラブルシューティング

### 問題1: ビルドエラー "Common が見つかりません"

**症状**:
```
error CS0246: 型または名前空間の名前 'Common' が見つかりませんでした
```

**原因**:
1. プロジェクト参照が設定されていない
2. `Common` プロジェクトがビルドされていない

**解決方法**:

**方法A: Visual Studio 2026でプロジェクト参照を追加**

1. ソリューションエクスプローラーで `Host_ErrorHandling` プロジェクトを右クリック
2. **[追加]** → **[プロジェクト参照...]** を選択
3. `Common` プロジェクトにチェックを入れる
4. **[OK]** をクリック

**方法B: ソリューション全体をリビルド**

1. メニューバーから **[ビルド]** → **[ソリューションのリビルド]** を選択
2. これによりすべてのプロジェクトが再ビルドされます

---

### 問題2: 実行時エラー "ポートが既に使用されています"

**症状**:
```
System.Net.Sockets.SocketException: 通常、各ソケット アドレスに対してプロトコル、ネットワーク アドレス、またはポートのどれか 1 つのみを使用できます。
```

**原因**:
前回の実行でポート11000が解放されていない

**解決方法**:

**方法A: Visual Studio 2026でデバッグセッションを停止**

1. Visual Studioで実行中のプログラムがある場合、**[デバッグ]** → **[デバッグの停止]** (Shift+F5) をクリック
2. すべてのVisual Studioのインスタンスでホストプログラムが停止していることを確認

**方法B: タスクマネージャーでプロセスを終了**

1. タスクマネージャーを開く (Ctrl+Shift+Esc)
2. **[詳細]** タブを選択
3. `Host_ErrorHandling.exe` または `HostErrorHandling.exe` を探す
4. 見つかったら右クリックして **[タスクの終了]** を選択

**方法C: 別のポート番号を使用**

1. Visual Studioで `HostErrorHandling.cs` を開く
2. ポート番号を変更:
```csharp
IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11001);  // 11000 → 11001
```
3. `ClientErrorHandling.cs` も同様に変更:
```csharp
IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11001);  // 11000 → 11001
```
4. 両方のファイルを保存 (Ctrl+S)
5. ソリューションをリビルド

**方法D: PCを再起動**

---

### 問題3: "listener が現在のコンテキストに存在しません"

**症状**:
```
error CS0103: 現在のコンテキストに 'listener' という名前は存在しません
```

**原因**:
エラー処理内で `listener.Close()` を呼んでいるが、`listener` のスコープ外

**解決方法**:

**確認**: `listener` が宣言されているスコープを確認

❌ **間違い**:
```csharp
public static void SocketServer()
{
	// ...
	{
		Socket listener = new Socket(...);  // ブロック内で宣言
	}

	// エラー処理
	listener.Close();  // ← エラー：スコープ外
}
```

✅ **正しい**:
```csharp
public static void SocketServer()
{
	// ...
	Socket listener = new Socket(...);  // メソッドスコープで宣言

	// エラー処理
	listener.Close();  // ← OK
}
```

---

### 問題4: "data1 が現在のコンテキストに存在しません"

**症状**:
```
error CS0103: 現在のコンテキストに 'data1' という名前は存在しません
```

**原因**:
古いコードの `data1` を削除したが、参照箇所が残っている

**解決方法**:

**検索**: Ctrl+F で `data1` を検索

**置き換え**: すべての `data1` を以下に置き換え:
- 受信データ: `receiveResult.Data`
- 送信データ: `responseData`

**例**:

❌ **間違い**:
```csharp
Console.WriteLine($"送信データ: {data1}");
```

✅ **正しい**:
```csharp
Console.WriteLine($"送信データ: {responseData}");
```

---

### 問題5: "接続が切断されました" が頻繁に発生

**症状**:
```
エラー検知: 接続が切断されました。
エラータイプ: セッションを切られました
```

**原因**:
1. クライアント側が正しいプロトコルを使っていない
2. ネットワークエラー
3. クライアントが先に切断している

**解決方法**:

**確認1: クライアント側のコードを確認**

✅ **正しいクライアント**:
```csharp
// ProtocolHandlerを使用
ProtocolHandler.SendData(socket, st);
var receiveResult = ProtocolHandler.ReceiveData(socket);
```

❌ **間違ったクライアント**:
```csharp
// 直接Send/Receive（EOFなし）
socket.Send(Encoding.UTF8.GetBytes(st));
socket.Receive(bytes);
```

**確認2: ネットワーク接続**

ループバックアドレスで試す:
```csharp
IPAddress ipAddress = IPAddress.Loopback;  // 127.0.0.1
```

**確認3: クライアントのタイムアウト設定**

ClientErrorHandling.cs:
```csharp
socket.SendTimeout = 5000;  // 5秒
socket.ReceiveTimeout = 5000;  // 5秒
```

---

### 問題6: 受信データに "<EOF>" が含まれている

**症状**:
```
受信データ: Hello World!<EOF>
```

**原因**:
`receiveResult.Data` ではなく、変換前のデータを表示している

**解決方法**:

❌ **間違い**:
```csharp
string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
Console.WriteLine($"受信データ: {receivedData}");  // EOFが含まれる
```

✅ **正しい**:
```csharp
var receiveResult = ProtocolHandler.ReceiveData(handler);
Console.WriteLine($"受信データ: {receiveResult.Data}");  // EOFは除外済み
```

---

## まとめ

### 移行の重要ポイント

1. **using Common; の追加**
   - `ProtocolHandler` を使うために必須

2. **名前空間とクラス名の変更**
   - プロジェクト構造に合わせる

3. **受信コードの置き換え**
   - `socket.Receive()` → `ProtocolHandler.ReceiveData()`
   - 自動でEOF検証、サイズチェック

4. **エラーハンドリングの追加**
   - `receiveResult.Success` で成功/失敗を判定
   - 不正なデータは拒否して切断

5. **送信コードの置き換え**
   - `socket.Send()` → `ProtocolHandler.SendData()`
   - 自動でEOF付加、サイズチェック

6. **セキュリティの向上**
   - 不正なデータを検知
   - 不正なクライアントは切断

### 移行後のメリット

✅ **セキュリティの向上**
- 不正なデータを受け入れない
- プロトコル違反を検知

✅ **信頼性の向上**
- データの完全性を保証
- 異常なデータを検知

✅ **保守性の向上**
- コードの重複を削減
- プロトコルを一元管理

✅ **デバッグの容易化**
- 詳細なエラー情報
- ログ出力の充実

✅ **運用の容易化**
- エラー原因の特定が簡単
- トラブルシューティングが容易

### SimpleHost と HostErrorHandling の比較表

| 機能 | SimpleHost | HostErrorHandling |
|------|-----------|-------------------|
| **EOF検証** | なし | あり（必須） |
| **サイズ検証** | なし | あり（1024バイト） |
| **切断検知** | なし | あり |
| **エラー情報** | なし | 詳細な4種類 |
| **不正データ対応** | 受け入れる | 拒否・切断 |
| **ログ出力** | 最小限 | 充実 |
| **セキュリティ** | 低い | 高い |
| **デバッグ容易性** | 低い | 高い |

### 次のステップ

この移行が完了したら:

1. **さらなる改善**
   - 複数クライアント対応（ループ処理）
   - 非同期処理 (async/await)
   - ログファイル記録機能

2. **関連ドキュメントの参照**
   - `HostErrorHandling解説.md`（作成予定）
   - `ProtocolHandler解説.md`（作成予定）
   - `SimpleClientからClientErrorHandling移行手順.md`

3. **実践的な機能追加**
   - データベース連携
   - 認証機能
   - 暗号化通信

---

**作成日**: 2025年  
**対象**: SimpleHost.cs から HostErrorHandling.cs への移行  
**難易度**: 初級〜中級
