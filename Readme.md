# NGOSync Unity Package

Netcode for GameObjects (NGO) + USD(任意) + XR/デスクトップ混在環境向けのサンプル／拡張コンポーネント群です。クライアント自動接続、HUD操作、Playerロール管理、VR/PC両対応移動、USDリロード同期（オプション）などをまとめています。

## ✅ 主な特徴
- 自動クライアント接続: `AutoClientBootstrap` が起動後自動で指定IPへ接続＆再接続リトライ。
- シーン内操作 HUD: `NGOHud` でIP/Port変更・Host/Client/Server起動・最近イベントログ表示。
- ネットワーク Spawn 支援: `NGOSpawner` による接続時自動プレハブ出現と USD 再ロードトリガ。
- プレイヤー役割同期: `PlayerRole` + `PlayerDecorator` による HOST / SERVER / CLIENT の彩色とラベル表示。
- 統合移動制御: `PlayerMovement` が VR(HMD) と 非VR(Keyboard+Mouse) 双方に対応。
- クライアント権限Transform: `ClientAuthTransform` でクライアント側更新を許可。
- USD連動 (任意): `UsdAutoReloaderBehaviour` + `UsdReloadNetworkSpawner` で USD Asset のファイル更新を検知しネットワーク同期用ダミー生成。
- 動的ビジュアル生成: `UsdDummyView` がサーバ指定種別(kind)に応じた Primitive / Mesh / Camera 表示。
- コンパイルガード: `HAS_UNITY_USD` で USD 依存コードをビルド種別ごとに安全に分離。

---

## 📂 ディレクトリ構成概要
```
Runtime/  … パッケージ本体スクリプト
Samples/  … サンプルシーンとプレハブ
Prefabs/  … 基本ネットワーク用プレハブ (Player 等)
DefaultNetworkPrefabs.asset … NetworkManager 用プリセット参照
```

---

## 🔧 動作前提・依存関係
| 機能             | 必要パッケージ / 条件                                                |
| ---------------- | -------------------------------------------------------------------- |
| 基本ネットワーク | Unity Netcode for GameObjects + UnityTransport                       |
| USD連動          | Unity USD ( `Unity.Formats.USD` ) & Scripting Define `HAS_UNITY_USD` |
| XR               | AR Foundation                                                        |

---

## 🛠 セットアップ手順

### Sampleシーンを動かす

1. 追加対象のUnityプロジェクトを開く
2. `Package Manager > 左上の+アイコン > Add package from disk...`を選択し、`package.json`を選択
3. [Serverのみ] USD インポート機能を使う場合: `Project Settings > Player > Scripting Define Symbols` に `HAS_UNITY_USD` を追加。 
   - ※Serverは基本的にWindows上で動作させる想定なので、**WindowsのBuild設定にのみ**追加してください。Androidのビルド設定には追加しないでください。
   - ![image](/.resources/screenshot_01.png)

#### [Server Scene]
4. `Packages/WIARS: NGO Sync/Samples/Prefabs/CafeInterior_prefab_net`のPrefabを開き、`USDAsset > Source Asset > USD File`の3点リーダボタンをクリックしてUSDファイルパスを自身のpathに設定し直す。
  - `<unitypackageのpath>/.usd/CafeInterior.usda`が元々設定されていたファイルです。
5. `Packages/WIARS: NGO Sync/Samples/Scenes/ServerScene.unity` を開く。
6. PIEでPlay開始
  - **`Build Setings > Platform`がWindowsになっていることを確認！**
7. 画面内の`Start Host`もしくは`Start Server`をクリック
   - Hostの場合、Server自身もプレイヤーになります。Game画面にはPlayer視点の映像が出力されます。
     - Windows上で起動した場合、WASD＋マウスでPlayerの操作ができます。
   - Serverの場合、Server自身はプレイヤーになりません。Game画面にはusdで定義されているCamera支店の映像が出力されます。
     - ![image](/.resources/screenshot_05.png)
8. ビルドする場合、PlatformはWindowsでビルドしてください。
  - ※ `HAS_UNITY_USD`のDefineが追加されていることを確認してください。

#### [Client Scene]
4. `Packages/WIARS: NGO Sync/Samples/Scenes/ClientScene.unity` を開く。
5. **[SnapdragonSpaces]** HMDで起動する場合は、**シーン直下に**SnapdragonSpacesで必要なGameObjectを配置してください。
   - ![image](/.resources/screenshot_00.png)
6. `AutoClientBootstrap > Auto Client Bootstrap > Server Endpoint > Server Ip`にServerシーンが動いているデバイスのIPアドレスを入力
   - **[!重要!]** 事前にServerシーンを動かすデバイスにおいて、**UDP, port=7777**の通信を許可する設定をしておくこと！（ファイアウォールなどを開けておく）
7. 別途Serverシーンを起動させておく
   - Editorの多重起動はできないので、ServerSceneをWindowsビルドし、exeから起動するのが良いです。
8.  PIEでPlay開始
9. Serverと同じコンテンツが見えればOK
    - Clientの場合、Game画面にはPlayer視点の映像が出力されます。
      - Windows上で起動した場合、WASD＋マウスでPlayerの操作ができます。
      - ![image](/.resources/screenshot_06.png)
10. ビルドする場合、
    - [PC向けClient] PlatformはWindowsでビルドしてください。
    - [HMD向けClient] PlatformはAndroidでビルドしてください。

### 自分のシーンを動かす

1. `CafeInterior_prefab_net`の作成
   1. `USD > Import as Prefab`からusdファイルをインポートする
   2. 生成されたPrefabに、以下をアタッチする
      1. Network Object
      2. Network Transform
      3. Usd Auto Reloader Behaviour
      4. Usd Reload Network Spawner
   3. `Usd Reload Network Spawner > Spawn Options > Spawnable Object Keyword List`に、SpawnできるXformのキーワード（オブジェクト名）を登録する。後述の`DummyObject`のMeshのリストと順番が一致していること。
      - ![image](/.resources/screenshot_03.png)
   4. `Usd Reload Network Spawner > Spawn Options > Dummy Network Object Prefab`に、`Packages/WIARS: NGO Sync/Prefabs/DummyObject`を登録する
   5. `DummyObject > Usd Dummy View > Mesh Table`に、キーワードリストに対応したMeshオブジェクトを登録する。usdファイル中でキーワードに該当したXformがこのMeshとしてSpawnされます。
      - ![image](/.resources/screenshot_04.png)
2. Serverシーンの設定
   1. `Packages/WIARS: NGO Sync/Samples/Scenes/ServerScene.unity` を開く。
   2. `NGOSpawner > NGO Spawner > Spawn Entries`に1で作成したPrefabを登録する。
      1. 初期位置をInitial Position等で設定する。
      2. ![image](/.resources/screenshot_02.png)
---

## 🧩 Prefab詳細

| プレハブ名                         | 主構成コンポーネント (抜粋)                                                                                      | 目的 / 機能概要                                                                                      | 主なカスタム設定項目                                                                                           |
| ---------------------------------- | ---------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------- |
| `NetworkManager`                   | `NetworkManager`, `UnityTransport`                                                                               | NGO のホスト/サーバ/クライアント開始と接続管理                                                       | Transport の Port / ConnData / NetworkPrefabs 設定                                                             |
| `NGOControlHud`                    | `NGOHud`                                                                                                         | 実行中の IP/Port 編集・ Host/Client/Server 起動・ Shutdown・最近イベント表示                         | IP / Port (HUD 内で編集可能)                                                                                   |
| `AutoClientBootstrap`              | `AutoClientBootstrap`                                                                                            | 起動後指定 IP/Port へ即時 Client 接続 + 再接続リトライ + 簡易ステータス表示                          | `serverIp` / `port` / `autoReconnect` / `retryIntervalSeconds` / `showHud`                                     |
| `Player`                           | `NetworkObject`, `PlayerRole`, `PlayerDecorator`, `PlayerMovement`, `ClientAuthTransform`, `CharacterController` | 各クライアント/Host のプレイヤー表示・役割彩色・移動操作 (VR/非VR両対応)・Transform クライアント権限 | `PlayerMovement` 各種速度/感度, `PlayerDecorator` 色設定, `ClientAuthTransform` 権限 (内部固定)                |
| `NGOSpawner`                       | `NGOSpawner` (Serverのみ動作)                                                                                    | Server 起動時に複数 NetworkObject を一括 Spawn。USD連動オブジェクトは初期リロード                    | `spawnEntries` (Prefab / Position / Rotation / Scale)                                                          |
| `DummyObject`                      | `NetworkObject`, `UsdDummyView` (+ `NetworkTransform` 推奨)                                                      | USD 階層ノードをネットワーク表示へ差し替えるダミー表示ベース                                         | `UsdDummyView > meshTable` (カスタムMesh順番)                                                                  |
| `SampleCube_prefab_net` (サンプル) | `UsdAsset`, `NetworkObject`, `NetworkTransform`, `UsdAutoReloaderBehaviour`, `UsdReloadNetworkSpawner`           | USD ファイル監視→リロード→階層走査→ダミー生成（Server側）                                            | `UsdAutoReloaderBehaviour` poll/debounce, `UsdReloadNetworkSpawner` keywordList / dummyPrefab / networkizeRoot |

### 補足
- `HAS_UNITY_USD` を定義しないビルドでは `NGOSpawner` / `UsdAutoReloaderBehaviour` / `UsdReloadNetworkSpawner` / `UsdDummyView` はダミー/無効化ログのみになり、USD 関連プレハブは機能しません。
- USDのCameraオブジェクトは Client では非表示 (Host だけ有効) になる挙動です。



## 🧩 スクリプト詳細

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

---

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

---

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
- Q: USDなしでビルドしたい → Define `HAS_UNITY_USD` を外すだけでダミー実装が使われます。ただしUSDのインポートができなくなります。

## 🔍 参考ログタグ
| タグ                         | 代表例 (一部抜粋)                                    | 主用途 / 意味                           |
| ---------------------------- | ---------------------------------------------------- | --------------------------------------- |
| `[AutoClientBootstrap]`      | NetworkManager not found / Exception while starting… | 自動接続開始/失敗/再試行関連ログ        |
| `[NGOHud]`                   | StartHost / StartClient / Shutdown                   | HUD操作結果 (起動/切断/設定変更)        |
| `[NGOSpawner]`               | spawnEntries が空 / Prefab が null をスキップ        | サーバ側スポーン処理と警告              |
| `[UsdAutoReloaderBehaviour]` | Reload(true) executed / Change detected, scheduled…  | USDファイル監視とリロード検知           |
| `[UsdReloadNetworkSpawner]`  | Spawn dummy kind=11 path=/Root/X                     | USD階層走査 & ダミー NetworkObject 生成 |
| `[UsdDummyView]`             | VisualKind changed: Mesh -> Camera                   | ダミー表示生成/種別(kind)変更           |
| `[PlayerMovement]`           | OnNetworkSpawn called. XRActive: False               | プレイヤー移動コンポーネント初期化状況  |

---

## 🧪 動作確認環境
- **Unity 2022.3.16f1**
- **Windows 11**
- **GPU:** RTX 3080 Ti

---

## 📦 依存 Unity Package
| パッケージ名                  | バージョン  |
| ----------------------------- | ----------- |
| com.unity.netcode.gameobjects | 1.7.1       |
| com.unity.transport           | 1.4.1       |
| com.unity.formats.usd         | 3.0.0-exp.5 |

---

## 👤 Authors
- XRSD Development Team, Sony Corporation  
- Primary Architect: Haruka Fujisawa

---

## 📝 変更履歴 (サンプル)
| Version | Date       | Notes            |
| ------- | ---------- | ---------------- |
| 0.1.0   | 2025-11-06 | 初版 README 追加 |
| 0.2.0   | 2025-11-11 | README 更新      |


---
