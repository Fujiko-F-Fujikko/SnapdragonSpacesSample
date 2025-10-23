## 動作環境

* Unity 2022.3.16f1 (※マイナーバージョンまで一致させないと動かない事例あり)
* Snapdragon Spaces 1.0.3
* OpenXR Plugin 1.10.0

## セットアップ
0. `Packages\manifest.json`の`com.qualcomm.snapdragon.spaces`のpathを、自分の環境で`com.qualcomm.snapdragon.spaces-1.0.3.tgz`があるpathに書き換える。
1. プロジェクトを開く。
2. `Assets/Prefabs/kitchen_prefab_net.prefab` > `USD Asset` > `USD File`を自分の環境のusdファイルパスに差し替える。右横の三点リーダーマーク(...)をクリックして編集できる。
3. `Assets/Prefabs/kitchen_prefab_net.prefab` > `USD Asset` > `Reimport from USD`をクリックしてReimportする。("USD Asset"というヘッダの下に３つ並んでいるアイコンの、ゴミ箱マークの左の矢印アイコンをクリック)
4. ポップアップが出るが、OKをクリック。※Reimportを行うとprefabが作り直されてNetcodeのserver-client間で使用する固有IDが更新されるので、Reimport後はServer/Client側両方リビルドが必要。
5. `Assets/Prefabs/kitchen_prefab_net.prefab` > `Child Local Transform Sync` > `Target`に、Server-Client間でTransformの変更を同期させたいオブジェクトを指定する（`kitchen_prefab_net`オブジェクト以下のもの。ex. CupCRed_1）。
6. `Assets/Scenes/ClientScene.unity`を開く。
7. シーン内にある`AutoClientBootstrap` > `AutoClientBootstrap` > `Server ip`を、ServerSceneを起動するPCのIPアドレスに書き換える。

## ビルド

### Client

1. `Assets/Scenes/ClientScene.unity`を開く
2. `Build Settings`を開く
3. `Scenes in Build`にClientSceneを含める
4. Platformは`Android`にして`Switch Platform`する。
5. Androidデバイスを接続した状態で、`Build and Run`を実行。

### Server

* ビルドしなくてもServerとしては機能します。
* **【重要】** PIEでServerSceneを実行する場合、実行前に`Build Settings`から`Windows`に`Switch Platform`しておくこと! ※これを忘れるとUSDの同期が機能しなくなります。

* ビルドする場合は、`Scenes in Build`にServerSceneを含め、Platformを`Windows`にしてビルドしてください。


## 実行

1. ServerとClientを同一ネットワークで起動する（どちらが先に起動してもOK）
2. Server側で、IPアドレスを`127.0.0.1`, portを `7777`になっていることを確認し、`Start Server`ボタンをクリック
3. Server, Client両方に同じUSDのオブジェクトが見えるはず。
4. 実行中にUSDファイルが変更された場合、ほぼ遅延なくシーン上（Server,Client両方）に反映されるはず。※現状変更が反映できるのは、セットアップの手順5で指定したオブジェクトのTransform情報(Location/Rotation/Scale)のみ


## シーン説明

* Assets/Scenes/SampleScene.unity: Cubeが出るだけのサンプルシーン
* Assets/Scenes/ServerScene.unity: Netcodeのサーバ用のシーン（Windows上で動かす想定）
* Assets/Scenes/ClientScene.unity: Netcodeのクライアント用のシーン（Android上で動かす想定）
