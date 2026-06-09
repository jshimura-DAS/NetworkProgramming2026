# Unity版じゃんけんクライアント実装ガイド

## 目次
1. [概要](#概要)
2. [Unity環境の準備](#unity環境の準備)
3. [実装方法](#実装方法)
4. [サンプルコード](#サンプルコード)
5. [注意事項](#注意事項)
6. [トラブルシューティング](#トラブルシューティング)

---

## 概要

### 現在のJankenHostの仕様
- **プロトコル**: TCP/IP
- **ポート**: 11000
- **エンコーディング**: UTF-8
- **終端マーカー**: `<EOF>`
- **通信フロー**:
  ```
  1. クライアントが接続
  2. クライアントが手（0/1/2）を送信
  3. ホストが結果メッセージを返送
  4. 接続を切断
  ```

### Unity版クライアントの目標
- 既存のJankenHostと通信できる
- UnityのUIで手を選択
- 結果をUnityのUI上に表示
- C#のSocket APIを使用

---

## Unity環境の準備

### 推奨Unity環境
- **Unityバージョン**: Unity 2021.3 LTS以降
- **Scripting Runtime Version**: .NET Standard 2.1
- **API Compatibility Level**: .NET Standard 2.1

### プロジェクト設定手順

#### 1. 新規Unityプロジェクトの作成
```
File → New Project
Template: 3D または 2D
Project name: JankenUnityClient
```

#### 2. プレイヤー設定の確認
```
Edit → Project Settings → Player → Other Settings

□ Scripting Backend: Mono (推奨) または IL2CPP
□ API Compatibility Level: .NET Standard 2.1
□ Allow unsafe Code: チェック不要
```

#### 3. 必要な名前空間の確認
Unityでは以下の名前空間が利用可能:
```csharp
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
```

---

## 実装方法

### アーキテクチャ

```
Unity GameObject
    ├── JankenClientManager (MonoBehaviour)
    │   ├── ネットワーク通信処理
    │   └── UIとの連携
    ├── UI Canvas
    │   ├── ボタン（グー、パー、チョキ）
    │   ├── 接続ボタン
    │   ├── 結果表示テキスト
    │   └── ステータス表示テキスト
    └── ProtocolHandler (非MonoBehaviour)
        └── 通信プロトコル処理
```

### ファイル構成

```
Assets/
├── Scripts/
│   ├── JankenClientManager.cs      # メインのクライアント管理
│   ├── ProtocolHandler.cs          # プロトコル処理
│   └── JankenLib.cs                # Janken列挙型等
└── Scenes/
    └── JankenScene.unity           # メインシーン
```

---

## サンプルコード

### 1. ProtocolHandler.cs

既存のCommon/ProtocolHandler.csをUnity用に移植します。

完全なコードはREADME.mdのProtocolHandlerセクションを参照してください。
主要なメソッド:
- `ReceiveData(Socket socket)`: データ受信
- `SendData(Socket socket, string data)`: データ送信
- `GetErrorDescription(ReceiveErrorType errorType)`: エラー説明取得

### 2. JankenLib.cs

```csharp
namespace JankenUnity
{
    public enum Hand
    {
        Rock = 0,      // グー
        Paper = 1,     // パー
        Scissors = 2   // チョキ
    }

    public static class JankenHelper
    {
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

        public static string HandToString(Hand hand)
        {
            return ((int)hand).ToString();
        }
    }
}
```

### 3. JankenClientManager.cs

Unity用のメインクライアントスクリプトです。

```csharp
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace JankenUnity
{
    public class JankenClientManager : MonoBehaviour
    {
        [Header("接続設定")]
        [SerializeField] private string hostAddress = "127.0.0.1";
        [SerializeField] private int port = 11000;

        [Header("UI要素")]
        [SerializeField] private Button connectButton;
        [SerializeField] private Button rockButton;
        [SerializeField] private Button paperButton;
        [SerializeField] private Button scissorsButton;
        [SerializeField] private Text statusText;
        [SerializeField] private Text resultText;

        private Socket _socket;
        private bool _isConnected = false;
        private bool _isProcessing = false;

        private void Start()
        {
            connectButton.onClick.AddListener(OnConnectButtonClicked);
            rockButton.onClick.AddListener(() => OnHandButtonClicked(Hand.Rock));
            paperButton.onClick.AddListener(() => OnHandButtonClicked(Hand.Paper));
            scissorsButton.onClick.AddListener(() => OnHandButtonClicked(Hand.Scissors));

            SetHandButtonsEnabled(false);
            UpdateStatus("未接続");
        }

        private void OnDestroy()
        {
            DisconnectFromHost();
        }

        private async void OnConnectButtonClicked()
        {
            if (_isProcessing) return;
            if (_isConnected)
                DisconnectFromHost();
            else
                await ConnectToHostAsync();
        }

        private async void OnHandButtonClicked(Hand hand)
        {
            if (!_isConnected || _isProcessing) return;
            await PlayGameAsync(hand);
        }

        private async Task ConnectToHostAsync()
        {
            _isProcessing = true;
            UpdateStatus("接続中...");
            SetHandButtonsEnabled(false);

            try
            {
                await Task.Run(() =>
                {
                    IPAddress ipAddress = IPAddress.Parse(hostAddress);
                    IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);
                    _socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    _socket.Connect(remoteEP);
                });

                _isConnected = true;
                UpdateStatus($"接続完了: {hostAddress}:{port}");
                SetHandButtonsEnabled(true);
                UpdateConnectButtonText("切断");
            }
            catch (Exception ex)
            {
                UpdateStatus("接続失敗");
                UpdateResult($"エラー: {ex.Message}");
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private void DisconnectFromHost()
        {
            if (_socket != null)
            {
                try
                {
                    if (_socket.Connected)
                        _socket.Shutdown(SocketShutdown.Both);
                    _socket.Close();
                }
                catch { }
                finally
                {
                    _socket = null;
                }
            }
            _isConnected = false;
            SetHandButtonsEnabled(false);
            UpdateStatus("切断しました");
            UpdateConnectButtonText("接続");
        }

        private async Task PlayGameAsync(Hand selectedHand)
        {
            _isProcessing = true;
            SetHandButtonsEnabled(false);
            UpdateStatus($"送信中: {JankenHelper.GetHandName(selectedHand)}");

            try
            {
                string handData = JankenHelper.HandToString(selectedHand);
                string result = await Task.Run(() =>
                {
                    if (!ProtocolHandler.SendData(_socket, handData))
                        return "送信に失敗しました。";
                    
                    var receiveResult = ProtocolHandler.ReceiveData(_socket);
                    if (!receiveResult.Success)
                        return $"エラー: {receiveResult.ErrorMessage}";
                    
                    return receiveResult.Data;
                });

                UpdateResult(result);
                UpdateStatus("対戦完了");
                DisconnectFromHost();
            }
            catch (Exception ex)
            {
                UpdateResult($"エラー: {ex.Message}");
                UpdateStatus("エラー発生");
                DisconnectFromHost();
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private void SetHandButtonsEnabled(bool enabled)
        {
            rockButton.interactable = enabled;
            paperButton.interactable = enabled;
            scissorsButton.interactable = enabled;
        }

        private void UpdateStatus(string message)
        {
            if (statusText != null)
                statusText.text = $"状態: {message}";
        }

        private void UpdateResult(string message)
        {
            if (resultText != null)
                resultText.text = message;
        }

        private void UpdateConnectButtonText(string text)
        {
            Text buttonText = connectButton.GetComponentInChildren<Text>();
            if (buttonText != null)
                buttonText.text = text;
        }
    }
}
```

---

## Unity UIの設定

### 1. Canvas の作成
```
Hierarchy右クリック → UI → Canvas
Canvas設定:
- Render Mode: Screen Space - Overlay
- Canvas Scaler: Scale With Screen Size
```

### 2. UI要素の配置

```
Canvas
├── Panel (背景)
│   ├── Title (Text): "じゃんけんゲーム"
│   ├── StatusText (Text): "状態: 未接続"
│   ├── ConnectButton (Button): "接続"
│   ├── ButtonPanel
│   │   ├── RockButton (Button): "グー"
│   │   ├── PaperButton (Button): "パー"
│   │   └── ScissorsButton (Button): "チョキ"
│   └── ResultText (Text): 結果表示エリア
```

### 3. スクリプトのアタッチ

```
1. 空のGameObjectを作成: "JankenClientManager"
2. JankenClientManager.csをアタッチ
3. Inspectorで各UI要素を設定:
   - Host Address: "127.0.0.1"
   - Port: 11000
   - Connect Button: ConnectButton
   - Rock/Paper/Scissors Button: 各ボタン
   - Status Text: StatusText
   - Result Text: ResultText
```

---

## 注意事項

### 1. スレッドとUnityのメインスレッド

**重要**: UnityのUIはメインスレッドからのみ更新可能です。

```csharp
// ❌ NG: 別スレッドから直接UI更新
await Task.Run(() => {
    statusText.text = "更新"; // エラー！
});

// ✅ OK: 別スレッドで処理、メインスレッドでUI更新
string result = await Task.Run(() => {
    // ネットワーク処理
    return "結果";
});
statusText.text = result; // メインスレッドで実行
```

### 2. ソケットのブロッキング

```csharp
// ❌ NG: メインスレッドでブロッキング
socket.Receive(buffer); // ゲームが止まる

// ✅ OK: 別スレッドで実行
await Task.Run(() => {
    socket.Receive(buffer);
});
```

### 3. エディタでの接続テスト

```
1. JankenHostを起動（Visual Studio）
2. Unityエディタでプレイモード開始
3. "接続"ボタンをクリック
4. 手を選択してプレイ
```

### 4. ビルド時の設定

#### Windowsビルド
```
File → Build Settings
Platform: Windows
Architecture: x86_64
```

#### その他のプラットフォーム
- **Android**: インターネット権限が必要
- **WebGL**: WebSocketが必要（生のSocketは使えない）
- **iOS**: アプリのネットワーク権限設定が必要

### 5. ファイアウォール設定

ホストマシンのファイアウォールでポート11000を開放する必要があります。

---

## トラブルシューティング

### 問題1: 接続できない

**症状**: "接続失敗" と表示される

**原因と対処**:
```
□ ホストアドレスの確認
  - 127.0.0.1: 同じPC上で実行
  - JankenHostが起動しているか確認

□ ポート番号の確認
  - 11000で一致しているか

□ ファイアウォールの確認
  - ポート11000が開いているか
```

### 問題2: 画面がフリーズする

**症状**: ボタンを押すとUnityが固まる

**対処**: Task.Run()で別スレッド実行を確認

### 問題3: UIが更新されない

**症状**: 接続後もステータスが変わらない

**対処**: UI更新はawait Task.Run()の外で行う

### 問題4: ビルド後に動作しない

**対処**:
```
□ API Compatibility Levelの確認
  - .NET Standard 2.1に設定
□ ビルドログの確認
□ プラットフォーム固有の制限
```

---

## 拡張機能のアイデア

### 1. 接続設定の入力フィールド
### 2. 接続状態のビジュアル表示
### 3. 再接続機能
### 4. アニメーション
### 5. サウンド効果

---

## まとめ

### 実装の難易度
**★★☆☆☆ (中程度)**

### 実装時間の目安
- **基本実装**: 2-3時間
- **UI調整**: 1-2時間
- **テスト**: 1時間
- **合計**: 約4-6時間

### 必要な知識
- ✓ Unity基礎（UI、スクリプト）
- ✓ C#基礎（クラス、async/await）
- ✓ ネットワーク基礎（TCP/IP、Socket）

### メリット
- ✅ ビジュアルなUIでプレイ可能
- ✅ マルチプラットフォーム対応（制限あり）
- ✅ 既存のJankenHostをそのまま利用
- ✅ グラフィック、サウンド等の拡張が容易

### デメリット
- ❌ Unityのインストールが必要
- ❌ スレッド処理の注意が必要
- ❌ WebGLでは生のSocketが使えない

---

## 次のステップ

1. **Unity環境の準備**
2. **基本実装**
3. **UI作成**
4. **テスト**
5. **拡張**

---

**作成日**: 2026年6月9日  
**対象プロジェクト**: NetworkProgramming2026 - sample03  
**対象Unity バージョン**: Unity 2021.3 LTS以降  
**対象.NET**: .NET Standard 2.1
