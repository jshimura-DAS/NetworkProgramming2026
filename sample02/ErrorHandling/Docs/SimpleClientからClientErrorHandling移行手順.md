# SimpleClient.cs から ClientErrorHandling.cs への移行手順

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

**SimpleClient.cs** (基本版) から **ClientErrorHandling.cs** (改良版) への移行により、以下の改善を実現します：

| 項目 | SimpleClient | ClientErrorHandling |
|------|-------------|-------------------|
| **エラーハンドリング** | 最小限 | 詳細なエラー検知 |
| **プロトコル** | 手動でEOF付加 | 自動でEOF付加 |
| **データ検証** | なし | サイズ・形式検証 |
| **コードの保守性** | 低い（直接Send/Receive） | 高い（ProtocolHandler使用） |
| **エラー情報** | Exception のみ | 詳細なエラータイプ |

---

## 移行前後のコード比較

### 完全なコード比較

#### SimpleClient.cs (移行前)

```csharp
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SimpleClient
{
	internal class SimpleClient
	{
		public static void Main()
		{
			//今回送るHello World!
			string st = "Hello World!Shimura";
			Console.WriteLine("SimpleClient");
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
			//Sendで送信している。
			byte[] msg = Encoding.UTF8.GetBytes(st + "<EOF>");
			socket.Send(msg);

			//Receiveで受信している。
			byte[] bytes = new byte[1024];
			int bytesRec = socket.Receive(bytes);
			string data1 = Encoding.UTF8.GetString(bytes, 0, bytesRec);
			Console.WriteLine(data1);

			//ソケットを終了している。
			socket.Shutdown(SocketShutdown.Both);
			socket.Close();
		}
	}
}
```

#### ClientErrorHandling.cs (移行後)

```csharp
using System.Net;
using System.Net.Sockets;
using System.Text;
using Common;  // ← 追加

namespace Client_ErrorHandling  // ← 変更
{
	internal class ClientErrorHandling  // ← 変更
	{
		public static void Main()
		{
			//今回送るHello World!
			string st = "Hello World!Shimura";
			Console.WriteLine("ClientErrorHandling");  // ← 変更
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

			// ProtocolHandlerを使ってデータを送信  // ← 変更開始
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
			}  // ← 変更終了

			//ソケットを終了している。
			socket.Shutdown(SocketShutdown.Both);
			socket.Close();
		}
	}
}
```

---

### 差分表示（変更箇所のみ）

```diff
--- SimpleClient.cs
+++ ClientErrorHandling.cs

 using System.Net;
 using System.Net.Sockets;
 using System.Text;
+using Common;

-namespace SimpleClient
+namespace Client_ErrorHandling
 {
-    internal class SimpleClient
+    internal class ClientErrorHandling
	 {
		 public static void Main()
		 {
			 //今回送るHello World!
			 string st = "Hello World!Shimura";
-            Console.WriteLine("SimpleClient");
+            Console.WriteLine("ClientErrorHandling");
			 SocketClient(st);
			 Console.ReadKey();
		 }

		 public static void SocketClient(string st)
		 {
			 // ... (IPアドレス設定は同じ) ...

			 try
			 {
				 socket.Connect(remoteEP);
			 }
			 catch (Exception e)
			 {
				 Console.WriteLine($"Connect Faild{e.ToString()}");
				 return;
			 }

-            //Sendで送信している。
-            byte[] msg = Encoding.UTF8.GetBytes(st + "<EOF>");
-            socket.Send(msg);
-
-            //Receiveで受信している。
-            byte[] bytes = new byte[1024];
-            int bytesRec = socket.Receive(bytes);
-            string data1 = Encoding.UTF8.GetString(bytes, 0, bytesRec);
-            Console.WriteLine(data1);
+            // ProtocolHandlerを使ってデータを送信
+            Console.WriteLine($"送信データ: {st}");
+            if (!ProtocolHandler.SendData(socket, st))
+            {
+                Console.WriteLine("送信に失敗しました。");
+                socket.Close();
+                return;
+            }
+
+            // ProtocolHandlerを使ってデータを受信
+            var receiveResult = ProtocolHandler.ReceiveData(socket);
+
+            if (!receiveResult.Success)
+            {
+                // エラーが発生した場合
+                Console.WriteLine($"エラー検知: {receiveResult.ErrorMessage}");
+                Console.WriteLine($"エラータイプ: {ProtocolHandler.GetErrorDescription(receiveResult.ErrorType)}");
+            }
+            else
+            {
+                // 正常に受信したデータを表示
+                Console.WriteLine($"受信データ: {receiveResult.Data}");
+            }

			 //ソケットを終了している。
			 socket.Shutdown(SocketShutdown.Both);
			 socket.Close();
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

---

### 変更2: 名前空間とクラス名の変更

#### 変更前
```csharp
namespace SimpleClient
{
	internal class SimpleClient
```

#### 変更後
```csharp
namespace Client_ErrorHandling
{
	internal class ClientErrorHandling
```

**理由**: プロジェクト名に合わせて名前空間とクラス名を変更

**詳細**:
- **名前空間**: `SimpleClient` → `Client_ErrorHandling`
  - プロジェクトフォルダ名と一致させる
  - コードの所在を明確にする

- **クラス名**: `SimpleClient` → `ClientErrorHandling`
  - エラーハンドリング機能があることを名前で示す
  - ファイル名と一致させる慣習に従う

---

### 変更3: コンソール出力メッセージの変更

#### 変更前
```csharp
Console.WriteLine("SimpleClient");
```

#### 変更後
```csharp
Console.WriteLine("ClientErrorHandling");
```

**理由**: プログラム名を正確に表示

**詳細**:
- ユーザーにどのプログラムが起動したかを知らせる
- デバッグ時に複数のプログラムを実行している場合に識別しやすい

---

### 変更4: データ送信部分の変更

#### 変更前（SimpleClient.cs）

```csharp
//Sendで送信している。
byte[] msg = Encoding.UTF8.GetBytes(st + "<EOF>");
socket.Send(msg);
```

**問題点**:
- ❌ 手動でEOFを追加（忘れる可能性）
- ❌ サイズチェックなし（大きすぎるデータも送信）
- ❌ エラーハンドリングなし（失敗しても気づかない）
- ❌ エンコーディング処理を毎回書く必要がある

#### 変更後（ClientErrorHandling.cs）

```csharp
// ProtocolHandlerを使ってデータを送信
Console.WriteLine($"送信データ: {st}");
if (!ProtocolHandler.SendData(socket, st))
{
	Console.WriteLine("送信に失敗しました。");
	socket.Close();
	return;
}
```

**改善点**:
- ✅ 自動でEOFを追加（`ProtocolHandler`が処理）
- ✅ サイズチェック実施（1024バイト制限）
- ✅ エラーハンドリング（`false`が返れば失敗）
- ✅ エンコーディング処理を隠蔽

**ProtocolHandler.SendData()の内部動作**:

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

### 変更5: データ受信部分の変更

#### 変更前（SimpleClient.cs）

```csharp
//Receiveで受信している。
byte[] bytes = new byte[1024];
int bytesRec = socket.Receive(bytes);
string data1 = Encoding.UTF8.GetString(bytes, 0, bytesRec);
Console.WriteLine(data1);
```

**問題点**:
- ❌ EOFの検証なし（不正なデータも受け入れる）
- ❌ サイズチェックなし
- ❌ エラー検知なし（接続切断に気づかない）
- ❌ `<EOF>`も含めて表示してしまう

#### 変更後（ClientErrorHandling.cs）

```csharp
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
```

**改善点**:
- ✅ EOF検証実施（`<EOF>`がなければエラー）
- ✅ サイズチェック実施（1024バイト制限）
- ✅ 詳細なエラー検知（4種類のエラータイプ）
- ✅ `<EOF>`を自動除去（`receiveResult.Data`に含まれない）

**ProtocolHandler.ReceiveData()の内部動作**:

```csharp
public static ReceiveResult ReceiveData(Socket socket)
{
	var result = new ReceiveResult { Success = false };
	byte[] buffer = new byte[1024];

	try
	{
		// 1. データ受信
		int bytesReceived = socket.Receive(buffer);

		// 2. 接続切断チェック
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

		// 5. EOF検証
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

## ステップバイステップ移行手順

### 前提条件

- ✅ Visual Studio 2026 がインストールされている
- ✅ .NET 10 SDK がインストールされている
- ✅ `Common` プロジェクトに `ProtocolHandler.cs` が作成済み
- ✅ `Client_ErrorHandling` プロジェクトから `Common` プロジェクトへの参照が設定済み

---

### ステップ1: プロジェクトとファイルの準備

#### 1-1. プロジェクト参照の確認

**Visual Studio 2026で確認**:

1. ソリューションエクスプローラーで `Client_ErrorHandling` プロジェクトを展開
2. **[依存関係]** → **[プロジェクト]** を確認
3. `Common` が表示されていればOK

**表示されていない場合、参照を追加**:

1. ソリューションエクスプローラーで `Client_ErrorHandling` プロジェクトを右クリック
2. **[追加]** → **[プロジェクト参照...]** を選択
3. `Common` プロジェクトにチェックを入れる
4. **[OK]** をクリック
5. ソリューションエクスプローラーで `Client_ErrorHandling` → **[依存関係]** → **[プロジェクト]** に `Common` が表示されることを確認

#### 1-2. SimpleClient.cs を開く

1. 元のファイルを開く: `sample01\SimpleHost\SimpleClient\SimpleClient.cs`
2. コード全体を選択してコピー (Ctrl+A → Ctrl+C)

#### 1-3. ClientErrorHandling.cs に貼り付け

1. 新しいファイルを開く: `Client_ErrorHandling\ClientErrorHandling.cs`
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
namespace SimpleClient
```

**変更後**:
```csharp
namespace Client_ErrorHandling
```

**操作方法**:
1. `SimpleClient` を選択 (ダブルクリック)
2. `Client_ErrorHandling` と入力

**または**: Visual Studioの名前変更機能を使用
1. `SimpleClient` を右クリック
2. **[名前の変更]** を選択
3. `Client_ErrorHandling` と入力
4. `Enter`

---

#### 2-3. クラス名の変更

**変更箇所**: 7行目

**変更前**:
```csharp
internal class SimpleClient
```

**変更後**:
```csharp
internal class ClientErrorHandling
```

**操作方法**:
1. `SimpleClient` を選択 (ダブルクリック)
2. `ClientErrorHandling` と入力

**重要**: ファイル名と一致させる

---

#### 2-4. コンソール出力の変更

**変更箇所**: 12行目（Mainメソッド内）

**変更前**:
```csharp
Console.WriteLine("SimpleClient");
```

**変更後**:
```csharp
Console.WriteLine("ClientErrorHandling");
```

**操作方法**:
1. `"SimpleClient"` を選択
2. `"ClientErrorHandling"` と入力

---

### ステップ3: データ送信部分の変更

#### 3-1. 古い送信コードを探す

**検索箇所**: `SocketClient` メソッド内の接続後

**削除する部分**（3行）:
```csharp
//Sendで送信している。
byte[] msg = Encoding.UTF8.GetBytes(st + "<EOF>");
socket.Send(msg);
```

**操作方法**:
1. 上記の3行を選択
2. `Delete` キーで削除

---

#### 3-2. 新しい送信コードを追加

**追加する場所**: 接続成功後（tryブロックの後）

**追加するコード**:
```csharp
// ProtocolHandlerを使ってデータを送信
Console.WriteLine($"送信データ: {st}");
if (!ProtocolHandler.SendData(socket, st))
{
	Console.WriteLine("送信に失敗しました。");
	socket.Close();
	return;
}
```

**完成形**（前後の文脈込み）:
```csharp
try
{
	socket.Connect(remoteEP);
}
catch (Exception e)
{
	Console.WriteLine($"Connect Faild{e.ToString()}");
	return;
}

// ← ここから追加
// ProtocolHandlerを使ってデータを送信
Console.WriteLine($"送信データ: {st}");
if (!ProtocolHandler.SendData(socket, st))
{
	Console.WriteLine("送信に失敗しました。");
	socket.Close();
	return;
}
// ← ここまで追加
```

**コードの意味**:

```csharp
// 送信前に何を送るか表示
Console.WriteLine($"送信データ: {st}");

// ProtocolHandlerで送信（戻り値で成功/失敗を判定）
if (!ProtocolHandler.SendData(socket, st))
{
	// 失敗した場合
	Console.WriteLine("送信に失敗しました。");
	socket.Close();  // ソケットを閉じる
	return;          // メソッドを終了
}
// 成功した場合はここから続行
```

---

### ステップ4: データ受信部分の変更

#### 4-1. 古い受信コードを探す

**削除する部分**（4行）:
```csharp
//Receiveで受信している。
byte[] bytes = new byte[1024];
int bytesRec = socket.Receive(bytes);
string data1 = Encoding.UTF8.GetString(bytes, 0, bytesRec);
Console.WriteLine(data1);
```

**操作方法**:
1. 上記の5行を選択
2. `Delete` キーで削除

---

#### 4-2. 新しい受信コードを追加

**追加する場所**: 送信コードの直後

**追加するコード**:
```csharp
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
```

**完成形**（前後の文脈込み）:
```csharp
if (!ProtocolHandler.SendData(socket, st))
{
	Console.WriteLine("送信に失敗しました。");
	socket.Close();
	return;
}

// ← ここから追加
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
// ← ここまで追加

//ソケットを終了している。
socket.Shutdown(SocketShutdown.Both);
socket.Close();
```

**コードの意味**:

```csharp
// ProtocolHandlerで受信（結果オブジェクトを取得）
var receiveResult = ProtocolHandler.ReceiveData(socket);

// 成功したかチェック
if (!receiveResult.Success)
{
	// 失敗の場合
	// エラーメッセージを表示
	Console.WriteLine($"エラー検知: {receiveResult.ErrorMessage}");
	// エラーの種類を表示
	Console.WriteLine($"エラータイプ: {ProtocolHandler.GetErrorDescription(receiveResult.ErrorType)}");
}
else
{
	// 成功の場合
	// 受信したデータを表示（<EOF>は除外済み）
	Console.WriteLine($"受信データ: {receiveResult.Data}");
}
```

---

### ステップ5: ビルドとエラー修正

#### 5-1. ビルドを実行

**Visual Studio 2026で**:

1. メニューバーから **[ビルド]** → **[ソリューションのビルド]** を選択（またはCtrl+Shift+B）
2. **出力** ウィンドウが自動的に開き、ビルドの進行状況が表示されます
3. エラーがある場合は **エラー一覧** ウィンドウに表示されます
   - 表示されていない場合: **[表示]** → **[エラー一覧]** (Ctrl+\, E)
4. エラー行をダブルクリックすると、該当するコード箇所にジャンプします

---

#### 5-2. よくあるエラーと修正方法

##### エラー1: "Common が見つかりません"

**エラーメッセージ**:
```
CS0246: 型または名前空間の名前 'Common' が見つかりませんでした
```

**原因**: プロジェクト参照が設定されていない

**解決方法（Visual Studio 2026）**:

1. ソリューションエクスプローラーで `Client_ErrorHandling` プロジェクトを右クリック
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

##### エラー3: "Main が複数定義されています"

**エラーメッセージ**:
```
CS0017: プログラムには複数の 'Main' メソッドが含まれています
```

**原因**: プロジェクト内に複数のMainメソッドが存在

**解決方法**:
1. 不要な `Program.cs` を削除
2. または、1つのMainメソッド以外をコメントアウト

---

### ステップ6: 最終確認

#### 6-1. コード全体のレビュー

**チェックポイント**:

- [ ] `using Common;` が追加されている
- [ ] 名前空間が `Client_ErrorHandling` になっている
- [ ] クラス名が `ClientErrorHandling` になっている
- [ ] 送信部分が `ProtocolHandler.SendData()` を使用
- [ ] 受信部分が `ProtocolHandler.ReceiveData()` を使用
- [ ] エラーハンドリングが追加されている

---

#### 6-2. 完成したコードの確認

最終的なコードは以下のようになります:

```csharp
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
			Console.WriteLine("ClientErrorHandling");
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
```

---

## 動作確認とテスト

### テスト1: 正常系のテスト

#### 手順

1. **ホスト側を起動（Visual Studio 2026）**

   - ソリューションエクスプローラーで `Host_ErrorHandling` プロジェクトを右クリック
   - **[デバッグ]** → **[新しいインスタンスを開始]** を選択
   - コンソールウィンドウが開き、以下のような出力が表示されます:
   ```
   HostErrorHandling is starting...
   (クライアント接続待機中...)
   ```

2. **クライアント側を起動（別のVisual Studioインスタンスまたは同じソリューション内）**

   **方法A: 複数のスタートアッププロジェクトを設定**
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

   **クライアント側**:
   ```
   ClientErrorHandling
   送信データ: Hello World!Shimura
   受信データ: HELLO WORLD!SHIMURA
   ```

   **ホスト側**:
   ```
   受信データ: Hello World!Shimura
   送信データ: HELLO WORLD!SHIMURA
   ```

#### 確認ポイント

- ✅ クライアントが送信したメッセージがホストに届いている
- ✅ ホストが大文字に変換して返している
- ✅ クライアントが正しく受信している
- ✅ `<EOF>` が画面に表示されていない（自動除去されている）

---

### テスト2: 大きなデータのテスト

#### 手順

**ClientErrorHandling.cs を一時的に変更**:

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

#### 確認ポイント

- ✅ データサイズチェックが動作している
- ✅ 1024バイトを超えるデータは送信されない
- ✅ エラーメッセージが表示される

**テスト後は元に戻す**:
```csharp
string st = "Hello World!Shimura";
```

---

### テスト3: サーバー未起動のテスト

#### 手順

1. **ホスト側を起動しない**
2. **クライアント側のみ起動**

#### 期待される出力

**クライアント側**:
```
ClientErrorHandling
Connect Failed System.Net.Sockets.SocketException: ...
```

#### 確認ポイント

- ✅ 接続エラーが検知される
- ✅ エラーメッセージが表示される
- ✅ プログラムが適切に終了する

---

### テスト4: 日本語データのテスト

#### 手順

**ClientErrorHandling.cs を一時的に変更**:

```csharp
string st = "こんにちは世界！これはテストです。";
```

#### 期待される出力

**クライアント側**:
```
ClientErrorHandling
送信データ: こんにちは世界！これはテストです。
受信データ: こんにちは世界！これはテストです。
```

（ホスト側で大文字変換は日本語に影響しない）

#### 確認ポイント

- ✅ 日本語が正しく送受信される
- ✅ 文字化けが発生しない
- ✅ UTF-8エンコーディングが正しく機能している

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

1. ソリューションエクスプローラーで `Client_ErrorHandling` プロジェクトを右クリック
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

1. Visual Studioで `ClientErrorHandling.cs` を開く
2. ポート番号を変更:
```csharp
IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11001);  // 11000 → 11001
```
3. `HostErrorHandling.cs` も同様に変更:
```csharp
IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11001);  // 11000 → 11001
```
4. 両方のファイルを保存 (Ctrl+S)
5. ソリューションをリビルド

**方法D: PCを再起動**

---

### 問題3: 受信データが文字化けする

**症状**:
```
受信データ: ????????????????
```

**原因**:
1. エンコーディングが一致していない
2. データが破損している

**解決方法**:

**確認1: UTF-8を使用しているか確認**

ProtocolHandler.cs:
```csharp
// 送信時
byte[] bytes = Encoding.UTF8.GetBytes(dataWithEof);

// 受信時
string receivedData = Encoding.UTF8.GetString(buffer, 0, totalBytesReceived);
```

**確認2: データが正しく送信されているか確認**

デバッグ用コードを追加:
```csharp
// 送信前
Console.WriteLine($"送信バイト数: {bytes.Length}");
Console.WriteLine($"送信データ（16進数）: {BitConverter.ToString(bytes)}");

// 受信後
Console.WriteLine($"受信バイト数: {bytesReceived}");
Console.WriteLine($"受信データ（16進数）: {BitConverter.ToString(buffer, 0, bytesReceived)}");
```

---

### 問題4: "接続が切断されました" エラー

**症状**:
```
エラー検知: 接続が切断されました。
エラータイプ: セッションを切られました
```

**原因**:
1. サーバーが先に切断した
2. ネットワークエラー
3. ファイアウォールによるブロック

**解決方法**:

**確認1: サーバーが正常に動作しているか**
```powershell
# サーバー側のログを確認
# エラーメッセージが表示されていないか確認
```

**確認2: ネットワーク接続を確認**
```powershell
# ループバックアドレスで試す
# ClientErrorHandling.cs で変更:
IPAddress ipAddress = IPAddress.Loopback;  // 127.0.0.1
```

**確認3: ファイアウォールの設定**

1. Windows検索バーで「ファイアウォール」と入力
2. **[Windows Defender ファイアウォール]** を開く
3. 左側のメニューから **[詳細設定]** をクリック
4. **[受信の規則]** を選択
5. 右側のアクションパネルから **[新しい規則...]** をクリック
6. **[ポート]** を選択して **[次へ]**
7. **[TCP]** を選択し、**[特定のローカルポート]** に `11000` と入力
8. **[接続を許可する]** を選択して完了まで進む

---

### 問題5: "データ終端マーカー<EOF>が見つかりません"

**症状**:
```
エラー検知: データ終端マーカー<EOF>が見つかりません。
エラータイプ: <EOF>が見つかりません
```

**原因**:
1. 送信側が `ProtocolHandler.SendData()` を使っていない
2. 古いコードが混在している

**解決方法**:

**確認1: 送信コードを確認**

❌ **間違い**:
```csharp
byte[] msg = Encoding.UTF8.GetBytes(st);  // EOFなし
socket.Send(msg);
```

✅ **正しい**:
```csharp
ProtocolHandler.SendData(socket, st);  // 自動でEOF付加
```

**確認2: サーバー側も確認**

HostErrorHandling.cs:
```csharp
// 送信時は必ず ProtocolHandler.SendData() を使用
ProtocolHandler.SendData(handler, responseData);
```

---

## まとめ

### 移行の重要ポイント

1. **using Common; の追加**
   - `ProtocolHandler` を使うために必須

2. **名前空間とクラス名の変更**
   - プロジェクト構造に合わせる

3. **送信コードの置き換え**
   - `socket.Send()` → `ProtocolHandler.SendData()`
   - 自動でEOF付加、サイズチェック

4. **受信コードの置き換え**
   - `socket.Receive()` → `ProtocolHandler.ReceiveData()`
   - 自動でEOF検証、エラー検知

5. **エラーハンドリングの追加**
   - `receiveResult.Success` で成功/失敗を判定
   - 詳細なエラーメッセージを表示

### 移行後のメリット

✅ **信頼性の向上**
- データの完全性を保証
- 異常なデータを検知

✅ **保守性の向上**
- コードの重複を削減
- プロトコルを一元管理

✅ **エラー対応の容易化**
- 詳細なエラー情報
- 原因の特定が簡単

✅ **拡張性の向上**
- プロトコルの変更が容易
- 新機能の追加が簡単

### 次のステップ

この移行が完了したら:

1. **ホスト側の移行**
   - `HostErrorHandling.cs` も同様に移行

2. **さらなる改善**
   - 非同期処理 (async/await)
   - 複数クライアント対応
   - ログ記録機能

3. **関連ドキュメントの参照**
   - `ClientErrorHandling解説.md`
   - `ProtocolHandler解説.md`

---

**作成日**: 2025年  
**対象**: SimpleClient.cs から ClientErrorHandling.cs への移行  
**難易度**: 初級〜中級
