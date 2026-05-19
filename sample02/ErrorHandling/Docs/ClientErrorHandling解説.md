# ClientErrorHandling クラス 詳細解説

## 目次
1. [概要](#概要)
2. [TCP/IP通信の基礎知識](#tcpip通信の基礎知識)
3. [必要な名前空間(using)の解説](#必要な名前空間usingの解説)
4. [クラス全体の構造](#クラス全体の構造)
5. [Mainメソッドの解説](#mainメソッドの解説)
6. [SocketClientメソッドの詳細解説](#socketclientメソッドの詳細解説)
7. [実行の流れ](#実行の流れ)
8. [エラーハンドリング](#エラーハンドリング)
9. [ProtocolHandlerクラスとの連携](#protocolhandlerクラスとの連携)

---

## 概要

**ClientErrorHandlingクラス**は、TCP/IP通信を使ってサーバー（ホスト）と通信を行う**クライアント側のプログラム**です。

### このプログラムの役割
- サーバーに接続する
- メッセージを送信する
- サーバーからの応答を受信する
- 通信エラーを適切に処理する

### 現実世界の例え
- **郵便に例えると**: あなたが手紙を書いて友人に送り、返事を待つ側
- **電話に例えると**: あなたが電話をかけて、相手が出るのを待ち、会話する側

---

## TCP/IP通信の基礎知識

### TCP/IPとは？

**TCP/IP**は、インターネットやネットワークでコンピュータ同士が通信するための約束事（プロトコル）です。

#### TCPの特徴
- **信頼性が高い**: データが確実に届く
- **順序を保証**: 送った順番通りに届く
- **接続型**: 電話のように相手と接続してから会話する

### ネットワーク通信の基本用語

#### 1. IPアドレス
コンピュータの**住所**のようなもの。

```
例: 192.168.1.100
```

- 各コンピュータに割り当てられた番号
- これを使って通信相手を特定する

#### 2. ポート番号
1つのコンピュータ内の**部屋番号**のようなもの。

```
例: 11000
```

- 1つのコンピュータで複数のプログラムが同時に通信できる
- 0～65535までの番号がある
- このプログラムでは **ポート11000** を使用

#### 3. ソケット (Socket)
ネットワーク通信の**出入り口**のようなもの。

```
イメージ: 電話機
```

- データの送受信を行う道具
- 電話機で例えると、電話をかけたり受けたりする機械そのもの

#### 4. エンドポイント (EndPoint)
**IPアドレス + ポート番号** の組み合わせ。

```
例: 192.168.1.100:11000
```

- 完全な通信先の住所
- 「どのコンピュータの、どのポート」を明確に指定

---

## 必要な名前空間(using)の解説

```csharp
using System.Net;
using System.Net.Sockets;
using System.Text;
using Common;
```

### 各名前空間の役割

| 名前空間 | 役割 | 使用する主なクラス |
|---------|------|------------------|
| `System.Net` | ネットワークの基本機能 | `Dns`, `IPAddress`, `IPEndPoint` |
| `System.Net.Sockets` | ソケット通信 | `Socket`, `SocketType`, `ProtocolType` |
| `System.Text` | 文字列とバイト配列の変換 | `Encoding` |
| `Common` | 独自のプロトコル処理 | `ProtocolHandler` |

### 初心者向けの理解

```csharp
using System.Net;          // ← ネットワークの「住所録」を使うため
using System.Net.Sockets;  // ← ネットワークの「電話機」を使うため
using System.Text;         // ← 「日本語」と「バイナリデータ」を変換するため
using Common;              // ← 私たちが作った「通信ルール」を使うため
```

---

## クラス全体の構造

```csharp
namespace Client_ErrorHandling
{
	internal class ClientErrorHandling
	{
		// エントリーポイント - プログラムの開始地点
		public static void Main() { ... }

		// 実際の通信処理を行うメソッド
		public static void SocketClient(string st) { ... }
	}
}
```

### キーワードの説明

- **`namespace`**: クラスをグループ化する「フォルダ」のようなもの
- **`internal`**: このクラスは同じプロジェクト内でのみ使える
- **`class`**: プログラムの設計図
- **`public static`**: どこからでも呼び出せる共有メソッド

---

## Mainメソッドの解説

```csharp
public static void Main()
{
	//今回送るHello World!
	string st = "Hello World!Shimura";
	Console.WriteLine("ClientErrorHandling");
	SocketClient(st);
	Console.ReadKey();
}
```

### 1行ずつの解説

#### `string st = "Hello World!Shimura";`
```csharp
string st = "Hello World!Shimura";
```

- **目的**: サーバーに送信するメッセージを変数に格納
- **変数名**: `st` (stringの略)
- **内容**: "Hello World!Shimura" という文字列

#### `Console.WriteLine("ClientErrorHandling");`
```csharp
Console.WriteLine("ClientErrorHandling");
```

- **目的**: プログラムが起動したことを画面に表示
- **出力**: コンソール画面に「ClientErrorHandling」と表示される

#### `SocketClient(st);`
```csharp
SocketClient(st);
```

- **目的**: 実際の通信処理を行うメソッドを呼び出す
- **引数**: `st` (送信するメッセージ)を渡す

#### `Console.ReadKey();`
```csharp
Console.ReadKey();
```

- **目的**: 何かキーを押すまで画面を閉じない
- **理由**: すぐに画面が閉じると結果が見えないため

---

## SocketClientメソッドの詳細解説

### メソッド全体の流れ

```
1. 接続先の情報を設定
2. ソケットを作成
3. サーバーに接続
4. データを送信
5. データを受信
6. ソケットを閉じる
```

---

### ステップ1: 接続先の情報を設定

```csharp
//IPアドレスやポートを設定(自PC、ポート:11000）
string hostName = Dns.GetHostName();
IPHostEntry ipHostInfo = Dns.GetHostEntry(hostName);
IPAddress ipAddress = ipHostInfo.AddressList[2];
IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);
```

#### 詳細解説

##### `string hostName = Dns.GetHostName();`

```csharp
string hostName = Dns.GetHostName();
```

- **`Dns`**: Domain Name System（インターネットの電話帳）
- **`GetHostName()`**: 自分のコンピュータの名前を取得
- **結果例**: "DESKTOP-ABC123"

**現実の例え**: 自分の家の名前を確認する

##### `IPHostEntry ipHostInfo = Dns.GetHostEntry(hostName);`

```csharp
IPHostEntry ipHostInfo = Dns.GetHostEntry(hostName);
```

- **`GetHostEntry(hostName)`**: コンピュータ名からIPアドレス情報を取得
- **`IPHostEntry`**: コンピュータのネットワーク情報が入った「カルテ」

**含まれる情報**:
- ホスト名
- IPアドレスのリスト（複数ある場合も）
- エイリアス（別名）

##### `IPAddress ipAddress = ipHostInfo.AddressList[2];`

```csharp
IPAddress ipAddress = ipHostInfo.AddressList[2];
```

- **`AddressList`**: コンピュータに割り当てられた全てのIPアドレスのリスト
- **`[2]`**: 配列の3番目の要素（配列は0から始まる）
- **理由**: IPv6やループバックアドレスを避けて、実際に使うIPv4アドレスを選ぶ

**IPアドレスの例**:
```
[0] ::1                    (IPv6ループバック)
[1] fe80::xxxx:xxxx:xxxx   (IPv6リンクローカル)
[2] 192.168.1.100          (IPv4 ← これを使う)
```

##### `IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);`

```csharp
IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);
```

- **`IPEndPoint`**: 接続先の完全な住所（IPアドレス + ポート番号）
- **`remoteEP`**: remote EndPoint（リモートエンドポイント）の略
- **`11000`**: 使用するポート番号

**現実の例え**:
```
IPアドレス: 東京都渋谷区○○町1-2-3
ポート番号: 101号室
→ IPEndPoint: 東京都渋谷区○○町1-2-3-101号室
```

---

#### 補足: 外部サーバーに接続する場合

```csharp
//外部を指定する場合
// IPAddress ipAddress = IPAddress.Parse("172.25.91.135");
// IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);
```

コメントアウトされているこのコードは、**別のコンピュータに接続する場合**に使います。

- **`IPAddress.Parse("172.25.91.135")`**: 文字列のIPアドレスを変換
- **用途**: 学校や会社のネットワーク内の別のPCに接続

---

### ステップ2: ソケットを作成

```csharp
//ソケットを作成
Socket socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
```

#### 詳細解説

##### `Socket`クラス

```csharp
Socket socket = new Socket(...);
```

- **`Socket`**: ネットワーク通信の「電話機」を作成
- **`socket`**: 変数名（この電話機に付けた名前）

##### 3つのパラメータ

```csharp
new Socket(
	ipAddress.AddressFamily,  // ① アドレスファミリー
	SocketType.Stream,        // ② ソケットタイプ
	ProtocolType.Tcp          // ③ プロトコル
);
```

| パラメータ | 値 | 意味 |
|-----------|---|------|
| ① アドレスファミリー | `ipAddress.AddressFamily` | IPv4かIPv6かを自動判定 |
| ② ソケットタイプ | `SocketType.Stream` | データを「川の流れ」のように連続して送る |
| ③ プロトコル | `ProtocolType.Tcp` | TCP通信を使う |

**初心者向けの理解**:
```
「どんな種類の電話機を作るか」を決めている
→ TCP/IP通信ができる電話機を作成
```

---

### ステップ3: サーバーに接続

```csharp
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
```

#### 詳細解説

##### try-catchブロック

```csharp
try
{
	// エラーが起きるかもしれない処理
}
catch (Exception e)
{
	// エラーが起きた時の処理
}
```

- **`try`**: 「試しにやってみる」ブロック
- **`catch`**: エラーが起きた時の「受け止め」ブロック
- **理由**: 接続に失敗してもプログラムが止まらないようにする

**現実の例え**:
```
try {
	電話をかける
}
catch {
	「相手が電話に出ません」と表示
}
```

##### `socket.Connect(remoteEP);`

```csharp
socket.Connect(remoteEP);
```

- **`Connect`**: サーバーに接続を試みる
- **引数**: `remoteEP`（接続先の住所）
- **処理時間**: サーバーが応答するまで待つ（ブロッキング処理）

**このメソッドが行うこと**:
1. サーバーに「接続したい」というリクエストを送る
2. サーバーが「OK」と応答するのを待つ
3. 接続が確立されるまで処理が止まる

##### エラーハンドリング

```csharp
catch (Exception e)
{
	Console.WriteLine($"Connect Faild{e.ToString()}");
	return;
}
```

- **`Exception e`**: 発生したエラー情報を受け取る変数
- **`e.ToString()`**: エラーの詳細情報を文字列化
- **`return`**: メソッドを終了（これ以上処理を続けない）

**エラーが起きる原因**:
- サーバーが起動していない
- ファイアウォールでブロックされている
- IPアドレスやポート番号が間違っている
- ネットワークケーブルが抜けている

---

### ステップ4: データを送信

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

#### 詳細解説

##### `Console.WriteLine($"送信データ: {st}");`

```csharp
Console.WriteLine($"送信データ: {st}");
```

- **文字列補間**: `$"..."` で変数を埋め込める
- **目的**: 何を送信するか画面に表示
- **出力例**: "送信データ: Hello World!Shimura"

##### `ProtocolHandler.SendData(socket, st)`

```csharp
if (!ProtocolHandler.SendData(socket, st))
```

- **`ProtocolHandler`**: 共通クラス（通信のルールを管理）
- **`SendData`**: データ送信専用メソッド
- **第1引数**: `socket`（どの通信路を使うか）
- **第2引数**: `st`（送信するメッセージ）
- **戻り値**: `true`（成功）/ `false`（失敗）

**`SendData`メソッドが自動で行うこと**:
1. メッセージの最後に `<EOF>` を追加
2. 文字列をバイト配列に変換
3. データサイズが1024バイト以下かチェック
4. ソケット経由で送信

**送信されるデータ**:
```
"Hello World!Shimura<EOF>"
```

##### エラーハンドリング

```csharp
if (!ProtocolHandler.SendData(socket, st))
{
	Console.WriteLine("送信に失敗しました。");
	socket.Close();
	return;
}
```

- **`!`**: NOT演算子（false の場合に実行）
- **`socket.Close()`**: ソケットを閉じる（通信終了）
- **`return`**: メソッドを終了

**失敗する原因**:
- データが1024バイトを超えている
- 接続が切断されている

---

### ステップ5: データを受信

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

#### 詳細解説

##### `ProtocolHandler.ReceiveData(socket)`

```csharp
var receiveResult = ProtocolHandler.ReceiveData(socket);
```

- **`ReceiveData`**: データ受信専用メソッド
- **`receiveResult`**: 受信結果を格納するオブジェクト
- **`var`**: 型を自動推論（実際は`ReceiveResult`型）

**`receiveResult`に含まれる情報**:
```csharp
{
	Success = true/false,           // 成功したか？
	Data = "受信した文字列",        // 受信データ
	ErrorMessage = "エラーメッセージ", // エラーの説明
	ErrorType = エラーの種類         // エラータイプ
}
```

##### 成功／失敗の判定

```csharp
if (!receiveResult.Success)
{
	// 失敗の処理
}
else
{
	// 成功の処理
}
```

- **`receiveResult.Success`**: `true`（成功）/ `false`（失敗）
- **`!`**: 否定（失敗した場合に実行）

##### エラー時の処理

```csharp
Console.WriteLine($"エラー検知: {receiveResult.ErrorMessage}");
Console.WriteLine($"エラータイプ: {ProtocolHandler.GetErrorDescription(receiveResult.ErrorType)}");
```

**エラーの種類**:
1. **DataTooLarge**: 1024バイトを超えるデータ
2. **MissingEOF**: `<EOF>` が見つからない
3. **ConnectionClosed**: 接続が切断された
4. **DataCorruption**: データに異常がある

**表示例**:
```
エラー検知: データ終端マーカー<EOF>が見つかりません。
エラータイプ: <EOF>が見つかりません
```

##### 成功時の処理

```csharp
Console.WriteLine($"受信データ: {receiveResult.Data}");
```

- **`receiveResult.Data`**: 受信した文字列（`<EOF>`は除外済み）
- **出力例**: "受信データ: HELLO WORLD!SHIMURA"

---

### ステップ6: ソケットを閉じる

```csharp
//ソケットを終了している。
socket.Shutdown(SocketShutdown.Both);
socket.Close();
```

#### 詳細解説

##### `socket.Shutdown(SocketShutdown.Both);`

```csharp
socket.Shutdown(SocketShutdown.Both);
```

- **`Shutdown`**: 通信を正式に終了する手続き
- **`SocketShutdown.Both`**: 送信と受信の両方を停止

**`SocketShutdown`の種類**:
| 値 | 意味 |
|----|------|
| `Send` | 送信のみ停止 |
| `Receive` | 受信のみ停止 |
| `Both` | 送信と受信の両方を停止 |

**現実の例え**:
```
電話を切る前に「じゃあまたね」と言う（礼儀正しい切断）
```

##### `socket.Close();`

```csharp
socket.Close();
```

- **`Close`**: ソケットを完全に閉じる
- **目的**: システムリソースを解放する

**`Shutdown`と`Close`の違い**:
```
Shutdown: 相手に「通信終了」を伝える
Close:    自分側のリソースを片付ける
```

**重要**: 両方を必ず実行する（リソースリークを防ぐため）

---

## 実行の流れ

### フローチャート

```
[開始]
  ↓
[Main: メッセージを準備]
  ↓
[SocketClient 呼び出し]
  ↓
[IPアドレス・ポート取得]
  ↓
[ソケット作成]
  ↓
[サーバーに接続] ← 失敗 → [エラー表示して終了]
  ↓ 成功
[データ送信] ← 失敗 → [エラー表示して終了]
  ↓ 成功
[データ受信]
  ↓
[成功？] → No → [エラー表示]
  ↓ Yes
[受信データ表示]
  ↓
[ソケットを閉じる]
  ↓
[終了]
```

### シーケンス図（時系列）

```
クライアント                     サーバー
	|                               |
	|-- 接続リクエスト -------------->|
	|<------------- 接続OK ----------|
	|                               |
	|-- "Hello World!Shimura<EOF>" ->|
	|                               | (大文字変換)
	|<-- "HELLO WORLD!SHIMURA<EOF>" |
	|                               |
	|-- 切断リクエスト -------------->|
	|<------------ 切断OK ------------|
	|                               |
```

---

## エラーハンドリング

### このプログラムで考慮されているエラー

#### 1. 接続エラー

```csharp
try {
	socket.Connect(remoteEP);
}
catch (Exception e) {
	Console.WriteLine($"Connect Faild{e.ToString()}");
	return;
}
```

**発生する状況**:
- サーバーが起動していない
- ネットワークが切断されている
- ファイアウォールがブロックしている

**対処**:
- エラーメッセージを表示
- プログラムを安全に終了

#### 2. 送信エラー

```csharp
if (!ProtocolHandler.SendData(socket, st)) {
	Console.WriteLine("送信に失敗しました。");
	socket.Close();
	return;
}
```

**発生する状況**:
- データが1024バイトを超えている
- 接続が切断された

**対処**:
- エラーメッセージを表示
- ソケットを閉じて終了

#### 3. 受信エラー

```csharp
if (!receiveResult.Success) {
	Console.WriteLine($"エラー検知: {receiveResult.ErrorMessage}");
	Console.WriteLine($"エラータイプ: {ProtocolHandler.GetErrorDescription(receiveResult.ErrorType)}");
}
```

**発生する状況**:
- `<EOF>` が見つからない
- データサイズが1024バイト超
- 接続が切断された
- データが壊れている

**対処**:
- 詳細なエラー情報を表示
- ソケットは閉じるが、プログラムは続行

---

## ProtocolHandlerクラスとの連携

### ProtocolHandlerの役割

**ClientErrorHandling**クラスは、データの送受信に**ProtocolHandler**クラスを使用しています。

#### なぜ分けるのか？

**理由**:
1. **再利用性**: ホスト側でも同じコードが使える
2. **保守性**: 通信ルールの変更が1箇所で済む
3. **責任の分離**: ネットワークの詳細を隠蔽できる

### SendDataメソッドの内部動作

```csharp
ProtocolHandler.SendData(socket, st)
```

**内部で行われること**:
```csharp
1. string dataWithEof = st + "<EOF>";           // ① EOFを追加
2. byte[] bytes = Encoding.UTF8.GetBytes(...);  // ② バイト配列に変換
3. if (bytes.Length > 1024) return false;       // ③ サイズチェック
4. socket.Send(bytes);                          // ④ 送信
5. return true;                                 // ⑤ 成功を返す
```

**文字列とバイト配列の変換**:
```
"Hello" → [72, 101, 108, 108, 111] (UTF8バイト列)
```

### ReceiveDataメソッドの内部動作

```csharp
var result = ProtocolHandler.ReceiveData(socket)
```

**内部で行われること**:
```csharp
1. byte[] buffer = new byte[1024];              // ① バッファ準備
2. int bytesReceived = socket.Receive(buffer);  // ② データ受信
3. string data = Encoding.UTF8.GetString(...);  // ③ 文字列に変換
4. int eofIndex = data.IndexOf("<EOF>");        // ④ EOFの位置を検索
5. result.Data = data.Substring(0, eofIndex);   // ⑤ EOFより前を取得
6. return result;                               // ⑥ 結果を返す
```

**エラーチェックの順序**:
1. 接続が切断されていないか？
2. データサイズが1024バイト以下か？
3. `<EOF>` が含まれているか？

---

## 補足: バイトとエンコーディング

### なぜバイト配列に変換するのか？

**ネットワークはバイト（0と1）しか送れない**

```
ネットワーク = 道路
バイト配列 = トラック
文字列 = 荷物
```

文字列をそのままでは送れないので、バイト配列という「トラック」に積んで送る。

### UTF-8エンコーディング

```csharp
Encoding.UTF8.GetBytes("Hello");
```

**UTF-8**: 世界中の文字を扱える文字コード

| 文字 | バイト表現 |
|------|-----------|
| H | 72 |
| e | 101 |
| l | 108 |
| 日 | 230 151 165 (3バイト) |

**重要**: 日本語は1文字で3バイト使うことがある

---

## まとめ

### このクラスの重要なポイント

1. **ソケット通信の基本手順**
   - ソケット作成 → 接続 → 送信 → 受信 → 切断

2. **エラー処理の重要性**
   - `try-catch`でエラーをキャッチ
   - 戻り値で成功/失敗を確認

3. **共通クラスの活用**
   - `ProtocolHandler`で通信ルールを統一
   - コードの重複を避ける

4. **リソース管理**
   - `Shutdown`と`Close`で正しく終了
   - メモリリークを防ぐ

### 初心者が押さえるべきこと

#### 最重要:
- ソケットは「ネットワークの電話機」
- TCP/IPは「信頼できる通信方法」
- エラー処理は必須

#### 理解すべき流れ:
```
作成 → 接続 → 送信 → 受信 → 切断
```

#### よくある間違い:
1. `Shutdown`や`Close`を忘れる → リソースリーク
2. エラー処理を書かない → プログラムがクラッシュ
3. バイト数を考えない → 日本語でサイズオーバー

---

## 参考: 用語集

| 用語 | 意味 |
|------|------|
| **TCP** | Transmission Control Protocol（信頼性の高い通信方式） |
| **IP** | Internet Protocol（インターネットの住所システム） |
| **ソケット** | ネットワーク通信の出入り口 |
| **ポート** | コンピュータ内のプログラムを識別する番号 |
| **エンドポイント** | IPアドレス + ポート番号 |
| **バイト** | データの最小単位（8ビット） |
| **UTF-8** | 文字コード（Unicode の一種） |
| **EOF** | End Of File（データの終端マーク） |

---

## 次のステップ

このクラスを理解したら、次は以下を学びましょう:

1. **HostErrorHandlingクラス**（サーバー側）の理解
2. **非同期通信**（async/await）
3. **複数クライアントの同時接続**
4. **UDP通信**（TCPとの違い）

---

**作成日**: 2025年
**対象**: TCP/IP通信とC#のプログラミング初心者
**関連ファイル**: `ClientErrorHandling.cs`, `ProtocolHandler.cs`
