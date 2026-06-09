# じゃんけんネットワークプログラム

## 概要
このプロジェクトは、ネットワーク通信を使用したじゃんけんゲームの実装です。
クライアント・サーバー方式で、ソケット通信により対戦を行います。

---

## プロジェクト構成

### 1. JankenLib（クラスライブラリ）
じゃんけんゲームのロジックを提供するライブラリプロジェクト

### 2. JankenHost（サーバー側）
じゃんけんゲームのホスト（サーバー）プログラム

### 3. JankenClient（クライアント側）
じゃんけんゲームのクライアントプログラム

### 4. Common（共通ライブラリ）
ネットワーク通信のプロトコル処理を提供

---

## JankenLibクラスライブラリ仕様

### 名前空間
```csharp
namespace JankenLib
```

### 列挙型

#### Hand（じゃんけんの手）
| 値 | 名前 | 説明 |
|---|---|---|
| 0 | Rock | グー |
| 1 | Paper | パー |
| 2 | Scissors | チョキ |

#### Result（勝敗結果）
| 値 | 説明 |
|---|---|
| Win | 勝ち |
| Lose | 負け |
| Draw | 引き分け |

### Jankenクラス

#### メソッド一覧

##### GetHandName(Hand hand)
- **説明**: 手の日本語名を取得
- **引数**: `Hand hand` - じゃんけんの手
- **戻り値**: `string` - 手の名前（"グー", "パー", "チョキ"）
- **使用例**:
  ```csharp
  string name = Janken.GetHandName(Hand.Rock); // "グー"
  ```

##### ParseHand(string input)
- **説明**: 文字列からじゃんけんの手を解析
- **引数**: `string input` - 入力文字列
- **戻り値**: `Hand?` - 解析された手、無効な場合はnull
- **対応入力**:
  - `"rock"`, `"グー"`, `"0"` → `Hand.Rock`
  - `"paper"`, `"パー"`, `"1"` → `Hand.Paper`
  - `"scissors"`, `"チョキ"`, `"2"` → `Hand.Scissors`
- **使用例**:
  ```csharp
  Hand? hand = Janken.ParseHand("0"); // Hand.Rock
  Hand? invalid = Janken.ParseHand("3"); // null
  ```

##### Judge(Hand player1, Hand player2)
- **説明**: じゃんけんの勝敗判定
- **引数**: 
  - `Hand player1` - プレイヤー1の手
  - `Hand player2` - プレイヤー2の手
- **戻り値**: `Result` - プレイヤー1から見た結果
- **判定ルール**:
  - グー > チョキ
  - パー > グー
  - チョキ > パー
  - 同じ手 → 引き分け
- **使用例**:
  ```csharp
  Result result = Janken.Judge(Hand.Rock, Hand.Scissors); // Result.Win
  ```

##### GetResultMessage(Result result)
- **説明**: 結果の文字列表現を取得
- **引数**: `Result result` - 勝敗結果
- **戻り値**: `string` - 結果メッセージ（"勝ち", "負け", "引き分け"）
- **使用例**:
  ```csharp
  string msg = Janken.GetResultMessage(Result.Win); // "勝ち"
  ```

##### GetRandomHand()
- **説明**: ランダムな手を生成
- **引数**: なし
- **戻り値**: `Hand` - ランダムに選ばれた手
- **使用例**:
  ```csharp
  Hand randomHand = Janken.GetRandomHand(); // Rock/Paper/Scissorsのいずれか
  ```

---

## じゃんけんゲーム動作仕様

### 通信プロトコル
- **プロトコル**: TCP/IP
- **ポート番号**: 11000
- **エンコーディング**: UTF-8
- **終端マーカー**: `<EOF>`
- **最大データサイズ**: 1024バイト

### ゲームフロー

#### 1. 初期化フェーズ
```
[ホスト]
1. ソケットを作成
2. ポート11000でリッスン開始
3. クライアントからの接続を待機

[クライアント]
1. ユーザーに手の入力を促す（0: グー, 1: パー, 2: チョキ）
2. 入力を検証
3. ホストに接続
```

#### 2. 対戦フェーズ
```
[クライアント → ホスト]
1. 選択した手を送信（"0", "1", または "2"）

[ホスト]
1. クライアントの手を受信
2. 受信データを検証
3. Janken.ParseHand()で手を解析
4. Janken.GetRandomHand()でホストの手を生成
5. Janken.Judge()で勝敗判定
6. 結果メッセージを作成
7. クライアントに結果を送信

[ホスト → クライアント]
結果メッセージ（以下の形式）:
【じゃんけん結果】
あなたの手: [手の名前]
ホストの手: [手の名前]
結果: あなたの[勝ち/負け/引き分け]!
```

#### 3. 終了フェーズ
```
[両方]
1. ソケットをシャットダウン
2. ソケットをクローズ
```

### 入力仕様

#### クライアント側入力
- **有効な入力**: `0`, `1`, `2`
- **無効な入力時の動作**: エラーメッセージを表示して終了

#### 入力と手の対応
| 入力 | 手 |
|---|---|
| 0 | グー（Rock） |
| 1 | パー（Paper） |
| 2 | チョキ（Scissors） |

### エラーハンドリング
- 接続失敗時: エラーメッセージを表示して終了
- 無効な手の送信: ホストがエラーメッセージを返送
- プロトコルエラー: ProtocolHandlerがエラーを検知して報告

---

## 改修内容

### JankenHost.cs の改修

#### 改修前の動作
- クライアントから文字列を受信
- 受信した文字列を大文字に変換
- クライアントに返送

#### 改修後の動作
- クライアントからじゃんけんの手（0/1/2）を受信
- ランダムにホストの手を生成
- 勝敗判定を実行
- 詳細な結果メッセージを作成して返送

#### 主な変更点

**1. usingディレクティブの追加**
```csharp
// 追加
using JankenLib;
```

**2. Main()メソッドの改修**
```csharp
// 改修前
Console.WriteLine("HostErrorHandling is starting...");

// 改修後
Console.WriteLine("=== じゃんけんホスト ===");
Console.WriteLine("クライアントからの接続を待っています...\n");
```

**3. SocketServer()メソッドの改修**

**接続確立後の処理を変更**:
```csharp
// 改修前
Socket handler = listener.Accept();

// 改修後
Socket handler = listener.Accept();
Console.WriteLine("クライアントが接続しました。\n");
```

**データ処理ロジックの変更**:
```csharp
// 改修前
// 大文字に変更
string responseData = receiveResult.Data.ToUpper();

// 改修後
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
```

**4. Listenerのクローズ処理を追加**
```csharp
// 改修後に追加
listener.Close();
```

---

### JankenClient.cs の改修

#### 改修前の動作
- 固定の文字列 "Hello World!Shimura" をサーバーに送信
- サーバーからの応答を受信して表示

#### 改修後の動作
- ユーザーにじゃんけんの手の入力を促す
- 入力値を検証
- 入力値をサーバーに送信
- サーバーから結果を受信して表示

#### 主な変更点

**1. usingディレクティブの追加**
```csharp
// 追加
using JankenLib;
```

**2. Main()メソッドの完全改修**

**改修前**:
```csharp
public static void Main()
{
	//今回送るHello World!
	string st = "Hello World!Shimura";
	Console.WriteLine("lientErrorHandling");
	SocketClient(st);
	Console.ReadKey();
}
```

**改修後**:
```csharp
public static void Main()
{
	Console.WriteLine("=== じゃんけんクライアント ===");
	Console.WriteLine("じゃんけんの手を選んでください:");
	Console.WriteLine("0: グー");
	Console.WriteLine("1: パー");
	Console.WriteLine("2: チョキ");
	Console.Write("入力 > ");

	string? input = Console.ReadLine();

	if (string.IsNullOrEmpty(input))
	{
		Console.WriteLine("入力がありません。");
		Console.ReadKey();
		return;
	}

	Hand? selectedHand = Janken.ParseHand(input);

	if (selectedHand == null)
	{
		Console.WriteLine("無効な入力です。0, 1, 2 のいずれかを入力してください。");
		Console.ReadKey();
		return;
	}

	Console.WriteLine($"あなたの手: {Janken.GetHandName(selectedHand.Value)}");

	SocketClient(input);
	Console.ReadKey();
}
```

**3. SocketClient()メソッドの改修**

**表示メッセージの改善**:
```csharp
// 改修前
Console.WriteLine($"送信データ: {st}");

// 改修後
Console.WriteLine($"\n送信データ: {st}");
```

```csharp
// 改修前
Console.WriteLine($"受信データ: {receiveResult.Data}");

// 改修後
Console.WriteLine($"\n受信データ:");
Console.WriteLine(receiveResult.Data);
```

**エラーメッセージの修正**:
```csharp
// 改修前
Console.WriteLine($"Connect Faild{e.ToString()}");

// 改修後
Console.WriteLine($"Connect Failed: {e.ToString()}");
```

---

## プロジェクトファイルの改修

### Jankenhost.csproj の改修
```xml
<!-- 追加 -->
<ItemGroup>
  <ProjectReference Include="..\..\..\sample02\ErrorHandling\Common\Common.csproj" />
  <ProjectReference Include="..\JankenLib\JankenLib.csproj" />
</ItemGroup>
```

### JankenClient.csproj の改修
```xml
<!-- 追加 -->
<ItemGroup>
  <ProjectReference Include="..\..\..\sample02\ErrorHandling\Common\Common.csproj" />
  <ProjectReference Include="..\JankenLib\JankenLib.csproj" />
</ItemGroup>
```

---

## 使用方法

### 1. プロジェクトのビルド
```powershell
# ソリューション全体をビルド
dotnet build
```

### 2. ホストの起動
```powershell
# JankenHostを起動
cd Jankenhost
dotnet run
```

### 3. クライアントの起動（別のターミナルで）
```powershell
# JankenClientを起動
cd JankenClient
dotnet run
```

### 4. ゲームのプレイ
1. クライアント側で0, 1, 2のいずれかを入力
2. Enterキーを押す
3. 結果が表示される

---

## 実行例

### ホスト側の表示
```
=== じゃんけんホスト ===
クライアントからの接続を待っています...

ホスト名: DESKTOP-XXXXX
IPアドレス一覧 (取得数: 4):
  [0] ::1 (AddressFamily: InterNetworkV6)
  [1] fe80::xxxx:xxxx:xxxx:xxxx%4 (AddressFamily: InterNetworkV6)
  [2] 192.168.1.100 (AddressFamily: InterNetwork)
  [3] fe80::xxxx:xxxx:xxxx:xxxx%10 (AddressFamily: InterNetworkV6)

使用するIPアドレス: 192.168.1.100
エンドポイント: 192.168.1.100:11000
  - IPアドレス: 192.168.1.100
  - ポート番号: 11000

クライアントが接続しました。

受信データ: 0
クライアントの手: グー
ホストの手: チョキ
結果: クライアントの勝ち

送信データ:
【じゃんけん結果】
あなたの手: グー
ホストの手: チョキ
結果: あなたの勝ち!
```

### クライアント側の表示
```
=== じゃんけんクライアント ===
じゃんけんの手を選んでください:
0: グー
1: パー
2: チョキ
入力 > 0
あなたの手: グー

送信データ: 0

受信データ:
【じゃんけん結果】
あなたの手: グー
ホストの手: チョキ
結果: あなたの勝ち!
```

---

## 技術仕様

### 開発環境
- **.NET バージョン**: .NET 10.0
- **C# バージョン**: 14.0
- **開発ツール**: Visual Studio Community 2026 (18.6.2)

### 使用技術
- **ネットワーク**: System.Net.Sockets
- **プロトコル**: TCP/IP
- **エンコーディング**: UTF-8
- **通信ライブラリ**: Common.ProtocolHandler

### 依存関係
```
JankenHost
  ├── Common (プロトコル処理)
  └── JankenLib (ゲームロジック)

JankenClient
  ├── Common (プロトコル処理)
  └── JankenLib (ゲームロジック)
```

---

## エラーケースと対応

| エラーケース | 動作 |
|---|---|
| クライアント: 無効な入力（0,1,2以外） | エラーメッセージを表示して終了 |
| クライアント: 空の入力 | エラーメッセージを表示して終了 |
| ホスト: 無効な手を受信 | エラーメッセージをクライアントに送信 |
| 接続失敗 | 例外をキャッチしてエラーメッセージを表示 |
| データサイズ超過 | ProtocolHandlerがエラーを検知 |
| EOF欠落 | ProtocolHandlerがエラーを検知 |

---

## 今後の拡張案

1. **複数回対戦**: 3回勝負、5回勝負などの実装
2. **複数クライアント対応**: 複数のクライアントが同時に対戦
3. **対戦履歴**: 勝敗の記録と統計表示
4. **GUI化**: WPFやWinFormsでのユーザーインターフェース
5. **ネットワーク対応強化**: 外部ネットワークからの接続対応
6. **認証機能**: ユーザー認証とセキュリティ強化

---

## ライセンス
このプロジェクトは教育目的で作成されています。

## 作成者
NetworkProgramming2026 - sample03

## リポジトリ
https://github.com/jshimura-DAS/NetworkProgramming2026
