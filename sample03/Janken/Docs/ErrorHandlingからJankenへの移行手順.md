# ErrorHandling から Janken ソリューション作成手順

## 概要
ErrorHandling ソリューションのホスト・クライアントアーキテクチャを基に、新しいアプリケーション「じゃんけん(Janken)」を作成する手順を説明します。

---

## 1. プロジェクト構成の比較

### ErrorHandling ソリューション
```
sample02/
└── ErrorHandling/
	├── Common/
	│   ├── ProtocolHandler.cs        # ネットワーク通信プロトコル
	│   └── Common.csproj
	├── Host_ErrorHandling/
	│   ├── HostErrorHandling.cs      # ホスト実装
	│   └── Host_ErrorHandling.csproj
	├── Client_ErrorHandling/
	│   ├── ClientErrorHandling.cs    # クライアント実装
	│   └── Client_ErrorHandling.csproj
	└── ErrorHandling.slnx
```

### Janken ソリューション（新規作成）
```
sample03/
└── Janken/
	├── Common/                        # ErrorHandling から参照
	│   ├── ProtocolHandler.cs
	│   └── Common.csproj
	├── JankenLib/                     # 新規作成：じゃんけんロジック
	│   ├── Janken.cs                 # じゃんけんゲームロジック
	│   └── JankenLib.csproj
	├── Jankenhost/                    # ErrorHandling から改造
	│   ├── JankenHost.cs             # ホスト実装
	│   └── Jankenhost.csproj
	├── JankenClient/                  # ErrorHandling から改造
	│   ├── JankenClient.cs           # クライアント実装
	│   └── JankenClient.csproj
	└── Janken.slnx
```

---

## 2. 新規プロジェクト作成: JankenLib

### 2.1 プロジェクト設定
**JankenLib.csproj** を作成（クラスライブラリ）

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
	<TargetFramework>net10.0</TargetFramework>
	<ImplicitUsings>enable</ImplicitUsings>
	<Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

**特徴：**
- 出力型は未指定（デフォルトがクラスライブラリ）
- ErrorHandling からの参照プロジェクト不要

### 2.2 Janken.cs の実装

**Janken.cs** に以下の内容を作成：

```csharp
namespace JankenLib;

/// <summary>
/// じゃんけんの手の種類
/// </summary>
public enum Hand
{
	Rock = 0,      // グー
	Paper = 1,     // パー
	Scissors = 2   // チョキ
}

/// <summary>
/// じゃんけんの結果
/// </summary>
public enum Result
{
	Win,    // 勝ち
	Lose,   // 負け
	Draw    // 引き分け
}

/// <summary>
/// じゃんけんゲームのロジッククラス
/// </summary>
public class Janken
{
	/// <summary>
	/// 手の名前を取得
	/// </summary>
	public static string GetHandName(Hand hand)
	{
		return hand switch
		{
			Hand.Rock => "グー",
			Hand.Paper => "パー",
			Hand.Scissors => "チョキ",
			_ => "不明"
		};
	}

	/// <summary>
	/// 文字列から手を取得
	/// </summary>
	public static Hand? ParseHand(string input)
	{
		return input.ToLower() switch
		{
			"rock" or "グー" or "0" => Hand.Rock,
			"paper" or "パー" or "1" => Hand.Paper,
			"scissors" or "チョキ" or "2" => Hand.Scissors,
			_ => null
		};
	}

	/// <summary>
	/// じゃんけんの勝敗判定
	/// </summary>
	/// <param name="player1">プレイヤー1の手</param>
	/// <param name="player2">プレイヤー2の手</param>
	/// <returns>プレイヤー1から見た結果</returns>
	public static Result Judge(Hand player1, Hand player2)
	{
		if (player1 == player2)
		{
			return Result.Draw;
		}

		return (player1, player2) switch
		{
			(Hand.Rock, Hand.Scissors) => Result.Win,
			(Hand.Paper, Hand.Rock) => Result.Win,
			(Hand.Scissors, Hand.Paper) => Result.Win,
			_ => Result.Lose
		};
	}

	/// <summary>
	/// 結果の文字列表現を取得
	/// </summary>
	public static string GetResultMessage(Result result)
	{
		return result switch
		{
			Result.Win => "勝ち",
			Result.Lose => "負け",
			Result.Draw => "引き分け",
			_ => "不明"
		};
	}

	/// <summary>
	/// ランダムな手を生成
	/// </summary>
	public static Hand GetRandomHand()
	{
		Random random = new Random();
		return (Hand)random.Next(0, 3);
	}
}
```

**JankenLib の役割：**
- じゃんけんの手の定義（列挙型）
- 勝敗判定ロジック
- 手の名前変換
- ランダムな手の生成

---

## 3. ホスト実装: Jankenhost

### 3.1 プロジェクト設定

**Jankenhost.csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
	<OutputType>Exe</OutputType>
	<TargetFramework>net10.0</TargetFramework>
	<ImplicitUsings>enable</ImplicitUsings>
	<Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
	<ProjectReference Include="..\..\..\sample02\ErrorHandling\Common\Common.csproj" />
	<ProjectReference Include="..\JankenLib\JankenLib.csproj" />
  </ItemGroup>

</Project>
```

**ErrorHandling からの変更点：**
- `Common.csproj` の参照元を相対パスに修正
- `JankenLib.csproj` を新規追加

### 3.2 JankenHost.cs の実装

**ErrorHandling (HostErrorHandling.cs) からの主要な差分：**

#### 差分1：クラス名とUI
```diff
- namespace Host_ErrorHandling { internal class HostErrorHandling {
+ namespace Jankenhost { internal class Jankenhost {

- public static void Main() {
-     Console.WriteLine("HostErrorHandling is starting...");
+ public static void Main() {
+     Console.WriteLine("=== じゃんけんホスト ===");
+     Console.WriteLine("クライアントからの接続を待っています...\n");
```

**理由：** アプリケーションの名前とUIを Janken 用にカスタマイズ

#### 差分2：IPアドレスの選択インデックス
```diff
- IPAddress ipAddress = ipHostInfo.AddressList[2];
+ IPAddress ipAddress = ipHostInfo.AddressList[1];
```

**理由：** 環境によって適切なIPアドレスのインデックスが異なるため、調整が必要

#### 差分3：ビジネスロジックの変更（最重要）

```diff
- // 大文字に変更
- string responseData = receiveResult.Data.ToUpper();
+ // クライアントの手を解析
+ Hand? clientHand = Janken.ParseHand(receiveResult.Data);
+
+ if (clientHand == null)
+ {
+     string errorMessage = "無効な手です。0, 1, 2 のいずれかを送信してください。";
+     ProtocolHandler.SendData(handler, errorMessage);
+     Console.WriteLine($"送信データ: {errorMessage}");
+
+     handler.Shutdown(SocketShutdown.Both);
+     handler.Close();
+     listener.Close();
+     return;
+ }
+
+ Console.WriteLine($"クライアントの手: {Janken.GetHandName(clientHand.Value)}");
+
+ // ホストの手をランダムに生成
+ Hand hostHand = Janken.GetRandomHand();
+ Console.WriteLine($"ホストの手: {Janken.GetHandName(hostHand)}");
+
+ // 勝敗判定（クライアントから見た結果）
+ Result result = Janken.Judge(clientHand.Value, hostHand);
+ Console.WriteLine($"結果: クライアントの{Janken.GetResultMessage(result)}");
+
+ // 結果メッセージを作成
+ string responseData = $"【じゃんけん結果】\n" +
+                       $"あなたの手: {Janken.GetHandName(clientHand.Value)}\n" +
+                       $"ホストの手: {Janken.GetHandName(hostHand)}\n" +
+                       $"結果: あなたの{Janken.GetResultMessage(result)}!";
```

**理由：** ErrorHandling ではテキスト処理が中心でしたが、Janken ではじゃんけんロジックに変更
- 文字列を手に変換
- ホストがランダムに手を選択
- 勝敗判定を実施
- 結果メッセージを構築

#### 差分4：UI出力の追加
```diff
- Console.WriteLine($"送信データ: {responseData}");
+ Console.WriteLine($"\n送信データ:\n{responseData}");
```

**理由：** マルチライン結果の表示を改善

#### 差分5：Using ディレクティブの追加
```diff
  using System.Net;
  using System.Net.Sockets;
+ using Common;
+ using JankenLib;

- namespace Host_ErrorHandling
+ namespace Jankenhost
```

**理由：** JankenLib と JankenLib の型を使用するため

---

## 4. クライアント実装: JankenClient

### 4.1 プロジェクト設定

**JankenClient.csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
	<OutputType>Exe</OutputType>
	<TargetFramework>net10.0</TargetFramework>
	<ImplicitUsings>enable</ImplicitUsings>
	<Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
	<ProjectReference Include="..\..\..\sample02\ErrorHandling\Common\Common.csproj" />
	<ProjectReference Include="..\JankenLib\JankenLib.csproj" />
  </ItemGroup>

</Project>
```

**変更内容：** ホストと同様

### 4.2 JankenClient.cs の実装

**ErrorHandling (ClientErrorHandling.cs) からの主要な差分：**

#### 差分1：Main メソッドのUIと入力処理の追加

```diff
- public static void Main()
- {
-     //今回送るHello World!
-     string st = "Hello World!Shimura";
-     Console.WriteLine("lientErrorHandling");
-     SocketClient(st);
-     Console.ReadKey();
- }
+ public static void Main()
+ {
+     Console.WriteLine("=== じゃんけんクライアント ===");
+     Console.WriteLine("じゃんけんの手を選んでください:");
+     Console.WriteLine("0: グー");
+     Console.WriteLine("1: パー");
+     Console.WriteLine("2: チョキ");
+     Console.Write("入力 > ");
+
+     string? input = Console.ReadLine();
+
+     if (string.IsNullOrEmpty(input))
+     {
+         Console.WriteLine("入力がありません。");
+         Console.ReadKey();
+         return;
+     }
+
+     Hand? selectedHand = Janken.ParseHand(input);
+
+     if (selectedHand == null)
+     {
+         Console.WriteLine("無効な入力です。0, 1, 2 のいずれかを入力してください。");
+         Console.ReadKey();
+         return;
+     }
+
+     Console.WriteLine($"あなたの手: {Janken.GetHandName(selectedHand.Value)}");
+
+     SocketClient(input);
+     Console.ReadKey();
+ }
```

**理由：** 
- ユーザーインタラクティブな入力が必要になった
- 入力値の検証を追加
- ビジネスロジックの実行前に UI で確認

#### 差分2：IPアドレス選択インデックス
```diff
- IPAddress ipAddress = ipHostInfo.AddressList[2];
+ IPAddress ipAddress = ipHostInfo.AddressList[1];
```

**理由：** ホストと同じIPアドレスを使用する必要があり、インデックスを合わせる

#### 差分3：受信データ表示の改善
```diff
- if (!receiveResult.Success)
- {
-     // エラーが発生した場合
-     Console.WriteLine($"エラー検知: {receiveResult.ErrorMessage}");
-     Console.WriteLine($"エラータイプ: {ProtocolHandler.GetErrorDescription(receiveResult.ErrorType)}");
- }
- else
- {
-     // 正常に受信したデータを表示
-     Console.WriteLine($"受信データ: {receiveResult.Data}");
- }
+ if (!receiveResult.Success)
+ {
+     // エラーが発生した場合
+     Console.WriteLine($"エラー検知: {receiveResult.ErrorMessage}");
+     Console.WriteLine($"エラータイプ: {ProtocolHandler.GetErrorDescription(receiveResult.ErrorType)}");
+ }
+ else
+ {
+     // 正常に受信したデータを表示
+     Console.WriteLine($"\n受信データ:");
+     Console.WriteLine(receiveResult.Data);
+ }
```

**理由：** マルチライン結果の表示を改善

#### 差分4：Using ディレクティブの追加
```diff
  using System.Net;
  using System.Net.Sockets;
  using System.Text;
  using Common;
+ using JankenLib;

- namespace Client_ErrorHandling
+ namespace JankenClient
```

**理由：** JankenLib の型を使用するため

---

## 5. 実装の流れ（ステップバイステップ）

### ステップ1：新規プロジェクト作成
1. JankenLib プロジェクトを作成（クラスライブラリ）
2. Janken.cs ファイルを作成し、ゲームロジックを実装

### ステップ2：ホスト実装
1. Host_ErrorHandling.csproj をコピーして Jankenhost.csproj を作成
2. プロジェクト参照を修正（Common, JankenLib を指定）
3. HostErrorHandling.cs をコピーして JankenHost.cs を作成
4. 以下の変更を実施：
   - namespace と class 名を変更
   - Using ディレクティブに JankenLib を追加
   - IPアドレスインデックスを調整
   - ビジネスロジックを大文字変換からじゃんけん判定に変更
   - UI表示を更新

### ステップ3：クライアント実装
1. Client_ErrorHandling.csproj をコピーして JankenClient.csproj を作成
2. プロジェクト参照を修正（Common, JankenLib を指定）
3. ClientErrorHandling.cs をコピーして JankenClient.cs を作成
4. 以下の変更を実施：
   - namespace を変更
   - Using ディレクティブに JankenLib を追加
   - IPアドレスインデックスを調整
   - Main メソッドでユーザー入力処理を追加
   - UI表示を更新

---

## 6. 重要な設計パターン

### 6.1 レイヤー構成

```
ネットワーク通信層
	↓
Common / ProtocolHandler
	↓
ビジネスロジック層
	↓
JankenLib / Janken
	↓
プレゼンテーション層
	↓
JankenHost / JankenClient (Main メソッド)
```

### 6.2 モジュール分離の利点

| モジュール | 責務 | 再利用可能性 |
|-----------|------|-----------|
| Common | ネットワークプロトコル | 高（ErrorHandling, Janken など複数アプリで使用） |
| JankenLib | じゃんけんロジック | 中（Janken アプリ専用） |
| JankenHost/Client | UI と通信制御 | 低（アプリケーション固有） |

### 6.3 エラーハンドリング

ErrorHandling で実装されたエラーハンドリング機構が Janken でも活用：
- ProtocolHandler が送受信時のエラーを検出
- エラータイプの分類と詳細メッセージ
- 接続異常時のグレースフルシャットダウン

---

## 7. 差分の総括表

| 項目 | ErrorHandling | Janken | 理由 |
|------|--------------|--------|------|
| 関数式ロジック | 文字列の大文字変換 | 手の解析と勝敗判定 | アプリケーション機能の違い |
| 入力値検証 | 文字列のみ | 手の有効性チェック | ビジネスルールの追加 |
| ビジネスロジック | 1行処理 | 複数ステップ処理 | 複雑度の増加 |
| クライアント入力 | ハードコード | ユーザーインタラクティブ | UX の向上 |
| IPアドレス | AddressList[2] | AddressList[1] | 環境依存 |
| ログ出力 | シンプル | 詳細（手の名前など） | デバッグ性の向上 |

---

## 8. トラブルシューティング

### 問題：クライアントがホストに接続できない
**原因：** IPアドレスのインデックスが異なる
**解決：** ホストとクライアントで AddressList インデックスを統一

### 問題：不正な手でエラーが返される
**原因：** ParseHand メソッドが null を返す
**解決：** 入力値を 0-2 の数値か、"rock", "paper", "scissors" で指定

### 問題：プロジェクト参照が見つからない
**原因：** Common.csproj の相対パスが不正
**解決：** プロジェクト構造に応じてパスを修正

---

## 9. 参考資料

- ErrorHandling ソリューション：`sample02/ErrorHandling/`
- Janken ソリューション：`sample03/Janken/`
- Common ライブラリ（共有）：`sample02/ErrorHandling/Common/`
