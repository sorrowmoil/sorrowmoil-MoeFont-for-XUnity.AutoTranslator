<div align="center">

[![中文](https://img.shields.io/badge/Lang-中文-red?style=for-the-badge)](./readme.md)
[![English](https://img.shields.io/badge/Lang-English-blue?style=for-the-badge)](./readme_en.md)

</div>

<div align="center">

# 🎀 sorrowmoil MoeFont Archive

> XUnity.AutoTranslator 向け多世代萌え系 TMP フォントアセットアーカイブ  
> Unity/TMP 世代ごとに独立構築された萌え系中国語化フォントアセットを提供します。

[![Unity](https://img.shields.io/badge/Unity-5.x%20–%206000-black?logo=unity)](.)
[![License](https://img.shields.io/badge/License-原著作者に権利が帰属します-lightgrey)](.)

</div>

---

# 🖼️ フォントプレビュー

このリポジトリに含まれる全フォントの統一レンダリングテストです。

テスト内容：

- 中国語
- 英語
- 日本語
- Unicode 特殊記号

XUnity/TMP 環境での実際の字形とスタイルの違いを示します。

<div align="center">

![Preview](preview/comparison.png)
![Preview](preview/comparison-2.png)
![Preview](preview/comparison-3.png)

</div>

---

# 📚 目次

- [📖 用語説明](#-用語説明)
- [✨ 特徴](#-特徴)
- [🎯 サポートする Unity 世代](#-サポートする-unity-世代)
- [🖼️ フォントプレビュー](#️-フォントプレビュー)
- [🖋️ 収録フォント](#️-収録フォント)
- [⚙️ ビルド設定](#️-ビルド設定)
- [🔄 互換性に関する注意](#-互換性に関する注意)
- [🚀 使い方](#-使い方)
- [⚠️ 注意事項](#️-注意事項)
- [📜 ライセンス](#-ライセンス)

---

# 📖 用語説明

> 本文書における **TMP** は **TextMesh Pro** の略称です。  
> **TMP フォントアセット** = TextMeshPro Font Asset。

---

# ✨ 特徴

このリポジトリは Unity ゲームの中国語化環境向けに、Unity/TMP 世代ごとに独立構築された既製の TMP フォントアセットを提供します。

すべてのアセットは、該当世代のエディター、TMP シリアライズ構造、リソース形式との一貫性を可能な限り保ち、世代間の互換性問題を低減します。

<details>
<summary><b>📦 詳細な特徴一覧を展開</b></summary>

- 各 Unity 世代は、その時代に対応する TMP バージョンで個別にベイク・パッケージ化されます  
- 統一生成方式を採用せず、世代間互換性問題を極力回避します  
- すべてのリソースは構造化され、すぐに中国語化環境で使用可能です  
- 主に XUnity.AutoTranslator の利用シーンを想定しています  
- 旧世代と新世代の両方の Unity ゲームのフォント互換性ニーズに対応します

</details>

---

# 🎯 サポートする Unity 世代

> 以下のバージョンはフォントアセットのビルド環境を示し、実行互換範囲のみを示すものではありません。  
> 具体的なビルドバージョン番号は各リリースの説明をご覧ください。

| Unity 世代 |  5.x | 2017 | 2018 | 2019 | 2020 | 2021 | 2022 | 2023 | 6000 |
|------------|------|------|------|------|------|------|------|------|------|
| サポート状況 | ⚠️  |⚠️  | ✅   | ✅   | ✅   | ✅   | ✅   | ✅   | ✅   |

> ⚠️ Unity 5.x 2017 の互換性は保証されません（[互換性に関する注意](#-互換性に関する注意)参照）

---

# 🖋️ 収録フォント

以下の萌え系フォントについて、全サポート世代向けの TMP フォントアセットが作成されています：

- **Lolita**
- **Yozai**
- **Xiaolai**
- **851LakeusNightWriting-Regular**
- **LXGWWenKaiGB-Regular**
- **maokenwanguzhanhei**
- **Ark Pixel 12px Mono**
- **StarLoveSweety**
- **StarLoveSweety**
  
---

# ⚙️ ビルド設定

<details>
<summary><b>🔧 クリックしてビルドパラメータを展開</b></summary>

すべてのフォントは以下の近似仕様で構築されています：

- SDF32  
- カーニング有効  
- Fast Mode  
- 8192 × 8192 アトラス  
- 4 万文字以上の元アセット  
- 多世代独立ベイク  
- **LZ4 圧縮**（全世代で適用、以前の LZMA から変更；ディスク使用量は増加）  
- ~~**Multi Atlas**（Unity 2021 以上で有効）~~
> <I> テストの結果、**Multi Atlas**をオンにすると、ゲーム内で翻訳が表示されなくなることがあります。 </i>

さらに：

> 異なる Unity 世代は、それぞれの時代の TMP バージョンを用いて個別に構築されています。

この方式は主に以下の問題を低減します：

- Font Asset のシリアライズ差異
- TMP データ構造の変化
- Unity リソース互換性問題
- 世代間フォント異常

</details>

---

# 🔄 互換性に関する注意

<details>
<summary><b>⚠️ Unity 5.x 2017 世代に関する特記事項</b></summary>

Unity 2017 世代のフォントアセットは **2017.1.0b2** に基づいて構築されていますが、検証可能な Unity 2017 ゲーム環境を確保できていないため、**その互換性は保証されません**。  
Unity 5.x 世代のフォントアセットは **5.2.0f** に基づいて構築されていますが、検証可能な Unity 2017 ゲーム環境を確保できていないため、**その互換性は保証されません**。  
可能な場合はご自身でテストし、結果をフィードバックいただくことを推奨します。

</details>

---

<details>
<summary><b>📉 低世代と高世代 TMP の互換関係</b></summary>

一般的に：

- ✅ 低世代の TMP フォントアセットは、より新しい世代の Unity ゲームに上位互換する可能性があります  
- ❌ 高世代の TMP フォントアセットは、より古い世代の Unity ゲームに下位互換するとは限りません

> [!WARNING]
> 高世代の TMP フォントアセットを低世代の Unity ゲームに強制的に使用することは推奨しません。

対象ゲームの世代に最も近い TMP フォントアセットを優先的に使用してください。

</details>

---

<details>
<summary><b>⚠️ Unity 6000 世代に関する特記事項</b></summary>

このリポジトリで提供される Unity 6000 世代のフォントアセットは、**互換性スキーム（Repackage）** を用いて構築されています。これは、超大規模文字セットにおけるネイティブビルドで発生しうる問題を回避するためのものです。  
このスキームはテストでより高い安定性が確認されています。詳細な技術情報はリリース内の `UNITY6000_NOTICE.txt` をご覧ください。

> [!IMPORTANT]
> Unity 6000 ゲームでは、必ずこのリポジトリの **6000 専用バージョン** を使用してください。

</details>

---

# 🚀 使い方

対応する世代の TMP フォントアセットを XUnity.AutoTranslator の `.ini` ファイルに設定します：

```ini
OverrideFontTextMeshPro=
FallbackFontTextMeshPro=
```

通常：

* `OverrideFontTextMeshPro` はメインの置換フォントに使用します
* `FallbackFontTextMeshPro` は不足文字の補完フォントに使用します

> 💡 設定の詳細は以下を参照してください：  
> [https://github.com/bbepis/XUnity.AutoTranslator](https://github.com/bbepis/XUnity.AutoTranslator)

---

# ⚠️ 注意事項

> 一部のフォントは字形のスタイルにより、シリアスな雰囲気や高可読性が求められる場面には適さない場合があります。ゲームの雰囲気に応じて選択してください。

このリポジトリは以下の目的に重点を置いています：

* 中国語化互換性リソースのアーカイブ
* TMP 歴史的バージョンの保存
* Unity 多世代互換性ソリューション
* 萌え系中国語化フォントの整理

長期的なメンテナンスプロジェクトではありません。

---

# 📜 ライセンス

フォントの著作権は原著作者に帰属します。

このリポジトリは以下を提供するのみです：

* TMP フォントアセットのビルド
* 多世代互換性の整理
* Unity/TMP 世代別アーカイブ

原著フォントのライセンスを提供するものではありません。

---

<div align="center">

Unity 世代を超えて萌えフォントの互換性を守ります。  
Made with 💖 by sorrowmoil

</div>
