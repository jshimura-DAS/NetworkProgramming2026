# SimpleHost.sln から ErrorHandling.sln への移行手順書

## 目次
1. [概要](#概要)
2. [移行前の準備](#移行前の準備)
3. [SimpleHostの構造理解](#simplehostの構造理解)
4. [ErrorHandlingの構造理解](#errorhandlingの構造理解)
5. [移行手順（詳細）](#移行手順詳細)
6. [Visual Studioでの操作手順](#visual-studioでの操作手順)
7. [コードの変更点](#コードの変更点)
8. [動作確認](#動作確認)
9. [トラブルシューティング](#トラブルシューティング)

---

## 概要

### このドキュメントの目的

**SimpleHost.sln**（基本的なソケット通信）から、**ErrorHandling.sln**（エラーハンドリング機能付きソケット通信）への移行方法を、初心者にもわかりやすく解説します。

### 移行の必要性

| SimpleHost | ErrorHandling |
|-----------|--------------|
| 基本的な送受信のみ | エラー検知機能 |
| エラー処理が簡易的 | 詳細なエラーハンドリング |
| EOFマーカーなし | `<EOF>`による終端管理 |
| データサイズ制限なし | 1024バイト制限 |
| 単一ファイル構成 | 共通クラス分離 |

### 移行後の利点

✅ **信頼性の向上**: データの完全性を保証  
✅ **エラー検知**: 異常な通信を即座に発見  
✅ **保守性の向上**: 共通コードの再利用  
✅ **拡張性**: 新しいプロトコルの追加が容易  

---

## 移行前の準備

### 1. バックアップの作成

**Windowsエクスプローラーでの操作**:

1. エクスプローラーで `D:\__EX_DATA\ネットワークプログラミング\NetworkProgramming2026` フォルダを開く
2. `sample01` フォルダを右クリック → **[コピー]**
3. 同じフォルダ内で右クリック → **[貼り付け]**
4. コピーされたフォルダ名を `sample01_backup` に変更

### 2. 必要なツールの確認

- ✅ Visual Studio 2026 (18.6以降)
- ✅ .NET 10 SDK
- ✅ Git (バージョン管理用)

### 3. 現在のコードの動作確認

移行前に、SimpleHostが正しく動作することを確認します。

```
1. ホスト側を起動
2. クライアント側を起動
3. 正常に通信できることを確認
```

---

## SimpleHostの構造理解

### ディレクトリ構造

```
sample01/
├── SimpleHost.sln
├── Host_SimpleHost/
│   ├── Host_SimpleHost.csproj
│   └── HostSimpleHost.cs
└── Client_SimpleHost/
	├── Client_SimpleHost.csproj
	└── ClientSimpleHost.cs
```

### SimpleHostのコード特徴

#### ホスト側 (HostSimpleHost.cs)

```csharp
// 基本的な受信
int bytesRec = handler.Receive(bytes);
string data = Encoding.UTF8.GetString(bytes, 0, bytesRec);
Console.WriteLine(data);

// 基本的な送信
byte[] msg = Encoding.UTF8.GetBytes(data.ToUpper());
handler.Send(msg);
```

**特徴**:
- 直接`Receive()`と`Send()`を使用
- エラーチェックなし
- EOFマーカーなし

#### クライアント側 (ClientSimpleHost.cs)

```csharp
// 基本的な送信
byte[] msg = Encoding.UTF8.GetBytes(st);
socket.Send(msg);

// 基本的な受信
byte[] bytes = new byte[1024];
int bytesRec = socket.Receive(bytes);
string data = Encoding.UTF8.GetString(bytes, 0, bytesRec);
Console.WriteLine(data);
```

**特徴**:
- 単純な送受信
- プロトコル規定なし
- エラーハンドリング最小限

---

## ErrorHandlingの構造理解

### ディレクトリ構造

```
sample02/ErrorHandling/
├── ErrorHandling.sln (新規作成)
├── Host_ErrorHandling/
│   ├── Host_ErrorHandling.csproj
│   └── HostErrorHandling.cs
├── Client_ErrorHandling/
│   ├── Client_ErrorHandling.csproj
│   └── ClientErrorHandling.cs
└── Common/
	├── Common.csproj (新規)
	└── ProtocolHandler.cs (新規)
```

### ErrorHandlingのコード特徴

#### 共通クラス (ProtocolHandler.cs)

```csharp
// 統一された送信
public static bool SendData(Socket socket, string data)
{
	string dataWithEof = data + "<EOF>";
	// サイズチェック、エラーハンドリング
}

// 統一された受信
public static ReceiveResult ReceiveData(Socket socket)
{
	// EOF検証、サイズチェック、エラー検知
}
```

**特徴**:
- プロトコルの一元管理
- `<EOF>`による終端管理
- 1024バイト制限
- 詳細なエラー情報

---

## 移行手順（詳細）

### 全体の流れ

```
ステップ1: 新しいディレクトリ構造を作成
	↓
ステップ2: SimpleHostのコードをコピー
	↓
ステップ3: 共通クラス (ProtocolHandler) を作成
	↓
ステップ4: ホスト側のコードを修正
	↓
ステップ5: クライアント側のコードを修正
	↓
ステップ6: ソリューションファイルを作成
	↓
ステップ7: ビルドとテスト
```

---

## Visual Studioでの操作手順

### ステップ1: 新しいソリューションの作成

#### 1-1. Visual Studioを起動

1. Visual Studio 2026を起動
2. **[新しいプロジェクトの作成]** をクリック

#### 1-2. ソリューションの作成

**方法A: 空のソリューションから開始**

1. **[空のソリューション]** を選択
2. ソリューション名: `ErrorHandling`
3. 場所: `D:\__EX_DATA\ネットワークプログラミング\NetworkProgramming2026\sample02\ErrorHandling`
4. **[作成]** をクリック

**方法B: 既存のプロジェクトがある場合**

1. Visual Studioで何も開いていない状態から
2. **[ファイル]** → **[新規作成]** → **[プロジェクト]**
3. **[空のソリューション]** を選択

---

### ステップ2: ホストプロジェクトの追加

#### 2-1. 新しいプロジェクトを追加

1. ソリューションエクスプローラーで **ソリューション名** を右クリック
2. **[追加]** → **[新しいプロジェクト]** を選択
3. **[コンソールアプリ]** を選択
4. プロジェクト名: `Host_ErrorHandling`
5. **[次へ]** → **[作成]**

#### 2-2. SimpleHostからコードをコピー

1. 既存の `HostSimpleHost.cs` を開く
2. コード全体をコピー
3. 新しい `Program.cs` (または `HostErrorHandling.cs`) に貼り付け
4. 名前空間とクラス名を変更

```csharp
// 変更前
namespace Host_SimpleHost

// 変更後
namespace Host_ErrorHandling
```

---

### ステップ3: クライアントプロジェクトの追加

#### 3-1. 新しいプロジェクトを追加

1. ソリューションエクスプローラーで **ソリューション名** を右クリック
2. **[追加]** → **[新しいプロジェクト]** を選択
3. **[コンソールアプリ]** を選択
4. プロジェクト名: `Client_ErrorHandling`
5. **[次へ]** → **[作成]**

#### 3-2. SimpleHostからコードをコピー

同様に、`ClientSimpleHost.cs` からコードをコピー

---

### ステップ4: 共通クラスライブラリの作成

#### 4-1. クラスライブラリプロジェクトを追加

1. ソリューションエクスプローラーで **ソリューション名** を右クリック
2. **[追加]** → **[新しいプロジェクト]** を選択
3. **[クラス ライブラリ]** を選択
4. プロジェクト名: `Common`
5. **[次へ]** → **[作成]**

#### 4-2. ProtocolHandler.cs を作成

1. `Common` プロジェクトを右クリック
2. **[追加]** → **[クラス]**
3. 名前: `ProtocolHandler.cs`
4. 以下のコードを記述

```csharp
using System.Net.Sockets;
using System.Text;

namespace Common
{
	public class ProtocolHandler
	{
		private const int MaxBufferSize = 1024;
		private const string EndOfFile = "<EOF>";

		// ... (ProtocolHandlerの完全なコードを記述)
	}
}
```

---

### ステップ5: プロジェクト参照の追加

#### 5-1. ホストプロジェクトからCommonを参照

1. `Host_ErrorHandling` プロジェクトを右クリック
2. **[追加]** → **[プロジェクト参照]**
3. `Common` にチェックを入れる
4. **[OK]**

#### 5-2. クライアントプロジェクトからCommonを参照

同様に、`Client_ErrorHandling` プロジェクトからも `Common` を参照

---

### ステップ6: コードの修正（ホスト側）

#### 6-1. usingディレクティブの追加

```csharp
using System.Net;
using System.Net.Sockets;
using System.Text;
using Common; // ← 追加
```

#### 6-2. データ受信部分を修正

**変更前**:
```csharp
int bytesRec = handler.Receive(bytes);
string data1 = Encoding.UTF8.GetString(bytes, 0, bytesRec);
Console.WriteLine(data1);
```

**変更後**:
```csharp
var receiveResult = ProtocolHandler.ReceiveData(handler);

if (!receiveResult.Success)
{
	Console.WriteLine($"エラー検知: {receiveResult.ErrorMessage}");
	handler.Shutdown(SocketShutdown.Both);
	handler.Close();
	listener.Close();
	return;
}

Console.WriteLine($"受信データ: {receiveResult.Data}");
```

#### 6-3. データ送信部分を修正

**変更前**:
```csharp
data1 = data1.ToUpper();
byte[] msg = Encoding.UTF8.GetBytes(data1);
handler.Send(msg);
```

**変更後**:
```csharp
string responseData = receiveResult.Data.ToUpper();

if (!ProtocolHandler.SendData(handler, responseData))
{
	Console.WriteLine("送信に失敗しました。");
}
```

---

### ステップ7: コードの修正（クライアント側）

#### 7-1. usingディレクティブの追加

```csharp
using System.Net;
using System.Net.Sockets;
using System.Text;
using Common; // ← 追加
```

#### 7-2. データ送信部分を修正

**変更前**:
```csharp
byte[] msg = Encoding.UTF8.GetBytes(st);
socket.Send(msg);
```

**変更後**:
```csharp
Console.WriteLine($"送信データ: {st}");
if (!ProtocolHandler.SendData(socket, st))
{
	Console.WriteLine("送信に失敗しました。");
	socket.Close();
	return;
}
```

#### 7-3. データ受信部分を修正

**変更前**:
```csharp
byte[] bytes = new byte[1024];
int bytesRec = socket.Receive(bytes);
string data1 = Encoding.UTF8.GetString(bytes, 0, bytesRec);
Console.WriteLine(data1);
```

**変更後**:
```csharp
var receiveResult = ProtocolHandler.ReceiveData(socket);

if (!receiveResult.Success)
{
	Console.WriteLine($"エラー検知: {receiveResult.ErrorMessage}");
}
else
{
	Console.WriteLine($"受信データ: {receiveResult.Data}");
}
```

---

### ステップ8: ビルドと実行

#### 8-1. ソリューション全体をビルド

1. **[ビルド]** → **[ソリューションのビルド]** (Ctrl+Shift+B)
2. **出力** ウィンドウでエラーがないか確認

#### 8-2. 複数のプロジェクトを起動

1. ソリューションを右クリック
2. **[プロパティ]**
3. **[共通プロパティ]** → **[スタートアップ プロジェクト]**
4. **[マルチ スタートアップ プロジェクト]** を選択
5. `Host_ErrorHandling`: **開始**
6. `Client_ErrorHandling`: **開始**
7. **[OK]**

#### 8-3. デバッグ実行

1. **F5** キーを押す
2. ホストとクライアントの両方が起動する

---

## コードの変更点

### 変更点一覧表

| 項目 | SimpleHost | ErrorHandling |
|------|-----------|--------------|
| **送信** | `socket.Send(bytes)` | `ProtocolHandler.SendData(socket, string)` |
| **受信** | `socket.Receive(bytes)` | `ProtocolHandler.ReceiveData(socket)` |
| **EOFマーカー** | なし | `<EOF>` を自動付加 |
| **サイズチェック** | なし | 1024バイト制限 |
| **エラー情報** | Exception のみ | ReceiveResult オブジェクト |
| **プロジェクト構成** | 2プロジェクト | 3プロジェクト（Common追加） |

---

### 詳細な変更マトリクス

#### ホスト側の変更

| 行番号 | 変更前 | 変更後 | 理由 |
|-------|-------|-------|------|
| 4 | `using System.Text;` | `using System.Text;`<br>`using Common;` | ProtocolHandler を使用 |
| 50-52 | `int bytesRec = handler.Receive(bytes);`<br>`string data1 = Encoding.UTF8.GetString(...);` | `var receiveResult = ProtocolHandler.ReceiveData(handler);` | エラーハンドリング追加 |
| 54-56 | `data1 = data1.ToUpper();`<br>`byte[] msg = Encoding.UTF8.GetBytes(data1);`<br>`handler.Send(msg);` | `string responseData = receiveResult.Data.ToUpper();`<br>`ProtocolHandler.SendData(handler, responseData);` | プロトコル統一 |

#### クライアント側の変更

| 行番号 | 変更前 | 変更後 | 理由 |
|-------|-------|-------|------|
| 4 | `using System.Text;` | `using System.Text;`<br>`using Common;` | ProtocolHandler を使用 |
| 46-47 | `byte[] msg = Encoding.UTF8.GetBytes(st + "<EOF>");`<br>`socket.Send(msg);` | `ProtocolHandler.SendData(socket, st);` | EOF自動付加 |
| 49-52 | `byte[] bytes = new byte[1024];`<br>`int bytesRec = socket.Receive(bytes);`<br>`string data1 = Encoding.UTF8.GetString(...);` | `var receiveResult = ProtocolHandler.ReceiveData(socket);` | エラーハンドリング追加 |

---

## 動作確認

### チェックリスト

#### ビルドの確認

- [ ] ソリューション全体がエラーなくビルドできる
- [ ] 警告が表示されていない（または意図した警告のみ）
- [ ] 3つのプロジェクトすべてがビルド成功

#### 実行の確認

- [ ] ホスト側が正常に起動する
- [ ] クライアント側が正常に起動する
- [ ] クライアントからホストに接続できる
- [ ] メッセージが正しく送受信される
- [ ] 受信したメッセージが大文字に変換されている

#### エラーハンドリングの確認

**テスト1: 正常系**
```
1. ホスト起動
2. クライアント起動
3. "Hello World" を送信
4. "HELLO WORLD" を受信
→ ✅ 正常に動作
```

**テスト2: 大きなデータ**
```csharp
// ClientErrorHandling.cs で変更
string st = new string('A', 2000); // 2000文字
```
→ ❌ "送信に失敗しました" と表示されるべき

**テスト3: サーバー未起動**
```
1. ホストを起動しない
2. クライアントのみ起動
→ ❌ "Connect Failed" と表示されるべき
```

**テスト4: 日本語送信**
```csharp
string st = "こんにちは世界";
```
→ ✅ 正常に送受信できるべき

---

## トラブルシューティング

### ビルドエラー

#### エラー: "Common が見つかりません"

**原因**: プロジェクト参照が設定されていない

**解決方法**:
```powershell
# コマンドラインで参照を追加
dotnet add Host_ErrorHandling\Host_ErrorHandling.csproj reference Common\Common.csproj
dotnet add Client_ErrorHandling\Client_ErrorHandling.csproj reference Common\Common.csproj
```

または、Visual Studioで:
1. プロジェクトを右クリック
2. **[追加]** → **[プロジェクト参照]**
3. `Common` にチェック

---

#### エラー: "ProtocolHandler が見つかりません"

**原因**: using ディレクティブがない

**解決方法**:
```csharp
using Common; // ← この行を追加
```

---

#### エラー: "TargetFramework が一致しません"

**原因**: プロジェクトのターゲットフレームワークが異なる

**解決方法**:
全てのプロジェクトの `.csproj` ファイルを確認:
```xml
<TargetFramework>net10.0</TargetFramework>
```

---

### 実行時エラー

#### エラー: "ポートが既に使用されています"

**原因**: 前回の実行が正しく終了していない

**解決方法**:
```powershell
# Windowsでポート11000を使用しているプロセスを確認
netstat -ano | findstr :11000

# プロセスIDを確認してタスクマネージャーで終了
```

---

#### エラー: "接続が拒否されました"

**原因**: ホストが起動していない

**解決方法**:
1. ホスト側を先に起動
2. ホストが "Accept待機中" と表示されるのを確認
3. その後、クライアントを起動

---

#### エラー: "EOF が見つかりません"

**原因**: 古いコードが混在している

**解決方法**:
- 全ての送信で `ProtocolHandler.SendData()` を使用していることを確認
- 直接 `socket.Send()` を使用していないか確認

---

### デバッグのヒント

#### Visual Studioでのデバッグ

**ブレークポイントの設定**:
```csharp
// この行にブレークポイントを設定
var receiveResult = ProtocolHandler.ReceiveData(socket); // ← F9
```

**変数の監視**:
1. ブレークポイントで停止
2. **イミディエイトウィンドウ** (Ctrl+Alt+I) で変数を確認

```csharp
// イミディエイトウィンドウで実行
? receiveResult.Success
? receiveResult.Data
? receiveResult.ErrorType
```

---

#### ログ出力の追加

```csharp
// デバッグ用のログを追加
Console.WriteLine($"[DEBUG] 接続先: {remoteEP}");
Console.WriteLine($"[DEBUG] 送信データサイズ: {st.Length}文字");
Console.WriteLine($"[DEBUG] 受信結果: {receiveResult.Success}");
```

---

## 移行チェックリスト

### プロジェクト構成

- [ ] ErrorHandling.sln が作成されている
- [ ] Host_ErrorHandling プロジェクトが存在
- [ ] Client_ErrorHandling プロジェクトが存在
- [ ] Common プロジェクトが存在
- [ ] プロジェクト参照が正しく設定されている

### ファイル

- [ ] HostErrorHandling.cs が存在
- [ ] ClientErrorHandling.cs が存在
- [ ] ProtocolHandler.cs が存在
- [ ] 全ての .csproj ファイルが存在

### コード修正

- [ ] using Common; が追加されている
- [ ] 送信コードが ProtocolHandler.SendData() に変更
- [ ] 受信コードが ProtocolHandler.ReceiveData() に変更
- [ ] エラーハンドリングが追加されている
- [ ] EOF マーカーが自動付加されている

### 動作確認

- [ ] ビルドが成功する
- [ ] ホストが起動する
- [ ] クライアントが起動する
- [ ] 通信が成功する
- [ ] エラー検知が動作する

---

## 付録A: 差分コード例

### ホスト側の完全な変更差分

```diff
--- HostSimpleHost.cs (SimpleHost版)
+++ HostErrorHandling.cs (ErrorHandling版)

 using System.Net;
 using System.Net.Sockets;
 using System.Text;
+using Common;

-namespace Host_SimpleHost
+namespace Host_ErrorHandling
 {
-    internal class HostSimpleHost
+    internal class HostErrorHandling
	 {
		 // ... (中略)

		 Socket handler = listener.Accept();

-        // 任意の処理
-        int bytesRec = handler.Receive(bytes);
-        string data1 = Encoding.UTF8.GetString(bytes, 0, bytesRec);
-        Console.WriteLine(data1);
+        // データの受信と検証
+        var receiveResult = ProtocolHandler.ReceiveData(handler);
+        
+        if (!receiveResult.Success)
+        {
+            Console.WriteLine($"エラー検知: {receiveResult.ErrorMessage}");
+            handler.Shutdown(SocketShutdown.Both);
+            handler.Close();
+            listener.Close();
+            return;
+        }
+        
+        Console.WriteLine($"受信データ: {receiveResult.Data}");

-        data1 = data1.ToUpper();
-        byte[] msg = Encoding.UTF8.GetBytes(data1);
-        handler.Send(msg);
+        string responseData = receiveResult.Data.ToUpper();
+        
+        if (!ProtocolHandler.SendData(handler, responseData))
+        {
+            Console.WriteLine("送信に失敗しました。");
+        }
	 }
 }
```

---

### クライアント側の完全な変更差分

```diff
--- ClientSimpleHost.cs (SimpleHost版)
+++ ClientErrorHandling.cs (ErrorHandling版)

 using System.Net;
 using System.Net.Sockets;
 using System.Text;
+using Common;

-namespace Client_SimpleHost
+namespace Client_ErrorHandling
 {
-    internal class ClientSimpleHost
+    internal class ClientErrorHandling
	 {
		 // ... (中略)

-        byte[] msg = Encoding.UTF8.GetBytes(st);
-        socket.Send(msg);
+        Console.WriteLine($"送信データ: {st}");
+        if (!ProtocolHandler.SendData(socket, st))
+        {
+            Console.WriteLine("送信に失敗しました。");
+            socket.Close();
+            return;
+        }

-        byte[] bytes = new byte[1024];
-        int bytesRec = socket.Receive(bytes);
-        string data1 = Encoding.UTF8.GetString(bytes, 0, bytesRec);
-        Console.WriteLine(data1);
+        var receiveResult = ProtocolHandler.ReceiveData(socket);
+        
+        if (!receiveResult.Success)
+        {
+            Console.WriteLine($"エラー検知: {receiveResult.ErrorMessage}");
+        }
+        else
+        {
+            Console.WriteLine($"受信データ: {receiveResult.Data}");
+        }
	 }
 }
```

---

## 付録B: PowerShellスクリプト（一括移行）

以下のスクリプトを使用すると、コマンド一発で移行できます。

```powershell
# ErrorHandling_Migration.ps1

# ===== 設定 =====
$sourceDir = "D:\__EX_DATA\ネットワークプログラミング\NetworkProgramming2026\sample01"
$targetDir = "D:\__EX_DATA\ネットワークプログラミング\NetworkProgramming2026\sample02\ErrorHandling"

Write-Host "SimpleHost から ErrorHandling への移行を開始します..." -ForegroundColor Green

# ===== ステップ1: ディレクトリ作成 =====
Write-Host "`n[ステップ1] ディレクトリ構造を作成中..." -ForegroundColor Yellow
New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
New-Item -ItemType Directory -Path "$targetDir\Host_ErrorHandling" -Force | Out-Null
New-Item -ItemType Directory -Path "$targetDir\Client_ErrorHandling" -Force | Out-Null
New-Item -ItemType Directory -Path "$targetDir\Common" -Force | Out-Null
Write-Host "✓ ディレクトリ作成完了" -ForegroundColor Green

# ===== ステップ2: ソリューション作成 =====
Write-Host "`n[ステップ2] ソリューションを作成中..." -ForegroundColor Yellow
Set-Location $targetDir
dotnet new sln -n ErrorHandling --force
Write-Host "✓ ソリューション作成完了" -ForegroundColor Green

# ===== ステップ3: プロジェクト作成 =====
Write-Host "`n[ステップ3] プロジェクトを作成中..." -ForegroundColor Yellow

Set-Location "$targetDir\Host_ErrorHandling"
dotnet new console -n Host_ErrorHandling -f net10.0 --force
Set-Location $targetDir

Set-Location "$targetDir\Client_ErrorHandling"
dotnet new console -n Client_ErrorHandling -f net10.0 --force
Set-Location $targetDir

Set-Location "$targetDir\Common"
dotnet new classlib -n Common -f net10.0 --force
Set-Location $targetDir

Write-Host "✓ プロジェクト作成完了" -ForegroundColor Green

# ===== ステップ4: ソリューションに追加 =====
Write-Host "`n[ステップ4] プロジェクトをソリューションに追加中..." -ForegroundColor Yellow
dotnet sln ErrorHandling.sln add Host_ErrorHandling\Host_ErrorHandling.csproj
dotnet sln ErrorHandling.sln add Client_ErrorHandling\Client_ErrorHandling.csproj
dotnet sln ErrorHandling.sln add Common\Common.csproj
Write-Host "✓ プロジェクト追加完了" -ForegroundColor Green

# ===== ステップ5: プロジェクト参照追加 =====
Write-Host "`n[ステップ5] プロジェクト参照を追加中..." -ForegroundColor Yellow
dotnet add Host_ErrorHandling\Host_ErrorHandling.csproj reference Common\Common.csproj
dotnet add Client_ErrorHandling\Client_ErrorHandling.csproj reference Common\Common.csproj
Write-Host "✓ プロジェクト参照追加完了" -ForegroundColor Green

# ===== ステップ6: ビルド確認 =====
Write-Host "`n[ステップ6] ビルドを確認中..." -ForegroundColor Yellow
dotnet build ErrorHandling.sln

if ($LASTEXITCODE -eq 0) {
	Write-Host "`n✓ 移行が完了しました!" -ForegroundColor Green
	Write-Host "`n次の手順:" -ForegroundColor Cyan
	Write-Host "  1. ProtocolHandler.cs を Common プロジェクトに作成"
	Write-Host "  2. ホスト・クライアントのコードを修正"
	Write-Host "  3. 再度ビルドして動作確認"
} else {
	Write-Host "`n⚠ ビルドに失敗しました。エラーを確認してください。" -ForegroundColor Red
}
```

### スクリプトの使用方法

```powershell
# スクリプトを保存
# D:\ErrorHandling_Migration.ps1

# 実行
cd "D:\"
.\ErrorHandling_Migration.ps1
```

---

## まとめ

この手順書に従うことで、**SimpleHost.sln** から **ErrorHandling.sln** へスムーズに移行できます。

### 重要なポイント

1. **段階的に移行**: 一度に全てを変更せず、1つずつ確認
2. **バックアップ**: 必ず元のコードをバックアップ
3. **動作確認**: 各ステップごとにビルド・実行を確認
4. **共通クラス**: ProtocolHandlerで通信ロジックを統一

### 次のステップ

移行が完了したら、以下のドキュメントも参照してください:

- `ClientErrorHandling解説.md` - クライアント側の詳細解説
- `ClientErrorHandling図解.md` - 図解による理解
- `ClientErrorHandlingリファレンス.md` - クイックリファレンス

---

**作成日**: 2025年  
**対象**: SimpleHost から ErrorHandling への移行  
**難易度**: 初級〜中級
