# NGOSync Unity Package

Netcode for GameObjects (NGO) + USD(任意) + XR/デスクトップ混在環境向けのサンプル／拡張コンポーネント群です。クライアント自動接続、HUD操作、Playerロール管理、VR/PC両対応移動、USDリロード同期（オプション）などをまとめています。

## ✅ 主な特徴
- 自動クライアント接続: `AutoClientBootstrap` が起動後自動で指定IPへ接続＆再接続リトライ。
- シーン内操作 HUD: `NGOHud` でIP/Port変更・Host/Client/Server起動・最近イベントログ表示。
- ネットワーク Spawn 支援: `NGOSpawner` による起動時プレハブ出現と USD 再ロードトリガ。
- プレイヤー役割同期: `PlayerRole` + `PlayerDecorator` による HOST / SERVER / CLIENT の彩色とラベル表示。
- 統合移動制御: `PlayerMovement` が VR(HMD) と 非VR(Keyboard+Mouse) 双方に対応。
- クライアント権限Transform: `ClientAuthTransform` でクライアント側更新を許可。
- USD連動 (任意): `UsdAutoReloaderBehaviour` + `UsdReloadNetworkSpawner` で USD Asset のファイル更新を検知しネットワーク同期用ダミー生成。
- 動的ビジュアル生成: `UsdDummyView` がサーバ指定種別(kind)に応じた Primitive / Mesh / Camera 表示。
- コンパイルガード: `HAS_UNITY_USD` で USD 依存コードをビルド種別ごとに安全に分離。

## 📂 ディレクトリ構成概要
```
Runtime/  … パッケージ本体スクリプト
Samples/  … サンプルシーンとプレハブ
Prefabs/  … 基本ネットワーク用プレハブ (Player 等)
DefaultNetworkPrefabs.asset … NetworkManager 用プリセット参照
```

## 🔧 動作前提・依存関係
| 機能             | 必要パッケージ / 条件                                                |
| ---------------- | -------------------------------------------------------------------- |
| 基本ネットワーク | Unity Netcode for GameObjects + UnityTransport                       |
| USD連動          | Unity USD ( `Unity.Formats.USD` ) & Scripting Define `HAS_UNITY_USD` |
| VR入力           | XR Management / OpenXR / XR Interaction など環境依存                 |
| 新Input          | Unity Input System パッケージ (PlayerMovement で使用)                |

## 🛠 セットアップ手順
1. 追加対象のUnityプロジェクトを開く
2. `Package Manager > 左上の+アイコン > Add package from disk...`を選択し、`package.json`を選択
3. [Serverのみ] USD 機能を使う場合: `Project Settings > Player > Scripting Define Symbols` に `HAS_UNITY_USD` を追加。
4. [Server] `Samples/Scenes/ServerScene.unity` を開く。
5. [Client] `ClientScene.unity` を開く。
6. [Server] シーンに `NetworkObject` + `NGOSpawner` をアタッチしたGame Objectを追加（ServerSceneでは`NGOSpawner`に相当）
  - `NGOSpawner > NetworkObject`に手順12で作成するプレハブを登録
7. [Server] シーンに`NetworkManager` + `UnityTransport`+`NGOHud`をアタッチしたGame Objectを追加（ServerSceneでは`NetworkBootstrap`に相当）
8. [Client] シーンに`NetworkManager` + `UnityTransport`をアタッチしたGame Objectを追加（ServerSceneでは`NetworkBootstrap`に相当）
9.  [Client] シーンに `AutoClientBootstrap` を追加したGame Objectを追加（ClientSceneでは`AutoClientBootstrap`に相当） IP/Port を設定。
10. [Server/Client] Player プレハブに `NetworkObject`, `PlayerRole`, `PlayerDecorator`, `PlayerMovement`, `ClientAuthTransform` を追加(`Prefabs > Player`に相当)
11. [Server] `NetworkObject`+`NetworkTransform`+`UsdDummyView`をアタッチした`DummyObject` を作成(`Prefabs > DummyObject`に相当)
  - `UsdDummyView > MeshTable`にSpawn予定のMeshを登録。**※手順12の`SpawnableObjectKeywordList`と順番を合わせること**
12. [Server] USDオブジェクト同期したい場合は: USDオブジェクト生成対象のGameObjectに `UsdAsset` + `NetworkObject` + `NetworkTransform` + `UsdAutoReloaderBehaviour` +  `UsdReloadNetworkSpawner`を追加(`Samples > Prefabs > SampleCube_prefab_net`に相当)
  - `UsdAutoReloaderBehaviour > UsdAsset > USDFile`に自身のusdファイルパスを登録
  - `UsdReloadNetworkSpawner > DummyNetworkObjectPrefab`に`DummyObject`プレハブを登録  
  - `UsdReloadNetworkSpawner > SpawnableObjectKeywordList`にSpawn対象となるオブジェクトのusdファイル上での名前を登録 **※手順11の`MeshTable`と順番を合わせること**


## 🧩 コンポーネント詳細
### AutoClientBootstrap
- 指定 `serverIp`, `port` へ自動接続。IL2CPP PostProcess 完了を数フレーム待機後 `StartClient()`。
- 切断時の自動再接続 (間隔 `retryIntervalSeconds`) オプション。
- 接続状態 HUD (簡易 OnGUI) 表示。

### NGOHud
- 実行中に IP / Port 入力・ Host / Client / Server 起動 / Shutdown。
- 最新イベント (接続/切断/サーバ開始停止) 最大8行表示。
- PlayerPrefs で前回設定保持。

### NGOSpawner
- Server上で `NetworkObject` プレハブを生成し `Spawn(true)` で全クライアント同期。
- 生成物に `UsdAutoReloaderBehaviour` があれば即 `TryReload()`。

### PlayerRole / PlayerDecorator
- `PlayerRole`: サーバ側で所有者が Server(Host含む)かどうかを見て `Role` を `HOST` / `SERVER` / `CLIENT` に設定。
- `PlayerDecorator`: 役割とオーナー情報を TextMesh と Body/Nose の色に反映 (Host強調色あり)。

### PlayerMovement
- 所有プレイヤーのみ操作可能。
- VR: HMD forward + 左スティック移動 / 右スティック水平回転。
- Desktop: WASD + マウス横回転 + 縦視点制限。`CharacterController` 前提。

### ClientAuthTransform
- `NetworkTransform` 継承。`OnIsServerAuthoritative()` を `false` にしクライアント権限に。

### UsdAutoReloaderBehaviour (HAS_UNITY_USD 時)
- USD ファイルの最終更新時刻をポーリングし変更検知 + デバウンス後 `usdAsset.Reload(true)`。
- `OnUsdReloaded` イベントで他コンポーネント通知。
- シンボル未定義時はダミー実装で安全にスキップ。

### UsdReloadNetworkSpawner (HAS_UNITY_USD 時)
- USD再ロード契機で階層を走査し `MeshRenderer` / `Camera` を持つノードを非表示化し、対応するネットワークダミー (`UsdDummyView` プレハブ) を `Spawn()`。
- キーワードリストで kind (10+index) を割り当て → クライアント側 `meshTable` で任意メッシュ選択可能。

### UsdDummyView
- サーバ書き込みのみ許可された変数で USDパス / Transform / 色 / 種別(kind) を同期。
- kind により `PrimitiveType` / 事前登録Mesh / Camera を生成。Collider除去。

## 🚀 典型的な利用フロー
### Host/Client モード
1. Host側でシーン再生 → HUDで `Start Host`。
2. クライアント側: `AutoClientBootstrap` が自動で接続 (または HUDで IP設定後 `Start Client`)。
3. PlayerRole が各所有者に応じて `HOST` / `CLIENT` ラベルと色を反映。

### USD 同期
1. サーバ(Host) シーンで USD ファイルを編集中に保存。
2. `UsdAutoReloaderBehaviour` が変更検知→リロードイベント。
3. `UsdReloadNetworkSpawner` が現行 USD 階層を走査し、ダミー NetworkObject 群を再生成。
4. クライアント側は `UsdDummyView` 経由で Transform/色/見た目(kind) を受信し表示更新。

## 🧪 テスト/デバッグのポイント
- 接続失敗時: Consoleログ `[AutoClientBootstrap]` を確認 (Port/IP/Transport設定)。
- Transform同期遅延: `ClientAuthTransform` 権限設定と `NetworkTransform` の Sync 設定見直し。
- USDが反映されない: Define `HAS_UNITY_USD` / ファイルパス / `usdAsset.usdFullPath` の存在確認。

## ⚙ Scripting Define `HAS_UNITY_USD`
- 定義あり: 実際の USD 読み込みとファイル監視を有効化。
- 定義なし: ダミークラスでビルドを壊さず機能スキップ（クライアント軽量化に有効）。

## 📦 インポート (unitypackage)
1. 追加対象のUnityプロジェクトを開く
2. `Package Manager > 左上の+アイコン > Add package from disk...`を選択し、`package.json`を選択
  - 依存パッケージは自動でインストールされます。

## 📁 サンプル
- シーン: `Samples/Scenes/ServerScene.unity`, `Samples/Scenes/ClientScene.unity`
- プレハブ: `Samples/Prefabs/` 内にスクリプト使用例。

## ❓ FAQ (抜粋)
- Q: 接続先を実行中に変えたい → HUDで変更後に再度 Start Client。
- Q: クライアントの Transform 権限を全員に与えたい → `ClientAuthTransform` を対象オブジェクトに追加。
- Q: USDなしでビルドしたい → Define を外すだけでダミー実装が使われます。

## 🔍 参考ログタグ
| タグ                    | 例                    | 用途                 |
| ----------------------- | --------------------- | -------------------- |
| `[AutoClientBootstrap]` | 接続/再接続処理       | クライアント自動接続 |
| `[NGOHud]`              | StartHost/Disconnect  | インタラクション結果 |
| `[USD]`                 | Reload(true) executed | USDリロード監視      |
| `[USD-Net]`             | Spawn dummy kind=…    | USD同期生成          |


## 🧪 動作確認環境
- **Unity 2022.3.16f1**
- **Windows 11**
- **GPU:** RTX 3080 Ti

## 📦 依存 Unity Package
| パッケージ名                  | バージョン  |
| ----------------------------- | ----------- |
| com.unity.netcode.gameobjects | 1.7.1       |
| com.unity.transport           | 1.4.1       |
| com.unity.formats.usd         | 3.0.0-exp.5 |
| com.unity.inputsystem         | 1.7.0       |

---

## 👤 Authors
- XRSD Development Team, Sony Corporation  
- Primary Architect: Haruka Fujisawa

---

## 📝 変更履歴 (サンプル)
| Version | Date       | Notes            |
| ------- | ---------- | ---------------- |
| 0.1.0   | 2025-11-06 | 初版 README 追加 |

---
