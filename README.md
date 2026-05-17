<div align="center">

# 🎀 sorrowmoil MoeFont Archive

> 适用于 XUnity.AutoTranslator 的多世代萌系 TMP 字体资产仓库  
> 为不同 Unity/TMP 世代提供独立构建的萌系汉化字体资产。

[![Unity](https://img.shields.io/badge/Unity-2018%20–%206000-black?logo=unity)](.)
[![License](https://img.shields.io/badge/License-原字体作者保留权利-lightgrey)](.)

</div>

---

# 🖼️ 字体预览

下图为当前仓库内字体的统一渲染测试。

测试内容包含：

- 中文
- 英文
- 日文
- Unicode 特殊符号

用于展示不同字体在 XUnity/TMP 环境下的实际字形效果与风格差异。

<div align="center">

![Preview](preview/comparison.png)

</div>

---

# 📚 目录

- [📖 术语说明](#-术语说明)
- [✨ 项目特点](#-项目特点)
- [🎯 支持的 Unity 世代](#-支持的-unity-世代)
- [🖼️ 字体预览](#️-字体预览)
- [🖋️ 已包含字体](#️-已包含字体)
- [⚙️ 构建参数](#️-构建参数)
- [🔄 兼容性说明](#-兼容性说明)
- [🚀 使用方式](#-使用方式)
- [⚠️ 注意事项](#️-注意事项)
- [📜 License](#-license)

---

# 📖 术语说明

> 本文档中 **TMP** 为 **TextMesh Pro** 的缩写。  
> **TMP 字体资产** = TextMeshPro Font Asset。

---

# ✨ 项目特点

本仓库面向 Unity 游戏汉化环境，提供针对不同 Unity/TMP 世代独立构建的预制 TMP 字体资产。

所有资源均尽可能保持与对应时代编辑器、TMP 序列化结构以及资源格式的一致性，以降低跨世代兼容问题。

<details>
<summary><b>📦 展开查看完整特性说明</b></summary>

- 每个 Unity 世代均使用对应时代的 TMP 版本独立烘焙与打包  
- 不采用统一生成方案，尽可能避免跨世代兼容问题  
- 所有资源均经过结构整理，可直接用于汉化环境  
- 重点面向 XUnity.AutoTranslator 使用场景  
- 兼顾旧世代 Unity 游戏与新世代 Unity 游戏的字体兼容需求

</details>

---

# 🎯 支持的 Unity 世代

> 下列版本表示字体资产的构建环境，而非仅运行兼容范围。

| Unity 世代 | 2018 | 2019 | 2020 | 2021 | 2022 | 2023 | 6000 |
|------------|------|------|------|------|------|------|------|
| 支持状态 | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

---

# 🖋️ 已包含字体

当前仓库已为下列萌系字体制作对应世代的 TMP 字体资产：

- **Lolita**
- **Yozai**
- **Xiaolai**

---

# ⚙️ 构建参数

<details>
<summary><b>🔧 点击展开参数详情</b></summary>

所有字体均遵循近似规范构建：

- SDF32  
- Kerning Enabled  
- Fast Mode  
- 8192 × 8192 图集  
- 原始 4 万字符级以上资产  
- 多世代独立烘焙

此外：

> 不同 Unity 世代均使用对应时代的 TMP 版本进行独立构建。

此方案主要用于降低：

- Font Asset 序列化差异
- TMP 数据结构变化
- Unity 资源兼容问题
- 跨世代字体异常

</details>

---

# 🔄 兼容性说明

<details>
<summary><b>📉 低世代与高世代 TMP 的兼容关系</b></summary>

通常情况下：

- ✅ 低世代 TMP 字体资产可以向上兼容更高世代 Unity 游戏  
- ❌ 高世代 TMP 字体资产不一定能够向下兼容低世代 Unity 游戏

> [!WARNING]
> 不建议强行将高世代 TMP 字体资产用于低世代 Unity 游戏。

建议优先使用与目标游戏世代最接近的 TMP 字体资产。

</details>

---

<details>
<summary><b>⚠️ 关于 Unity 6000 世代的特别说明</b></summary>

根据目前的测试与观察结果：

- Unity 6000 世代游戏似乎仅支持由 Unity 6000/TMP 6000 构建的字体资产  
- 旧世代 TMP 字体资产在 Unity 6000 下大概率无法正常工作

> [!IMPORTANT]
> Unity 6000 游戏请务必优先使用本仓库中的 6000 专属版本。

</details>

---

# 🚀 使用方式

将对应世代的 TMP 字体资产配置到 XUnity.AutoTranslator 的 `.ini` 文件内：

```ini
OverrideFontTextMeshPro=
FallbackFontTextMeshPro=
````

通常情况下：

* `OverrideFontTextMeshPro` 用于主替换字体
* `FallbackFontTextMeshPro` 用于缺字补充字体

> 💡 更多配置细节请参考：
> [https://github.com/bbepis/XUnity.AutoTranslator](https://github.com/bbepis/XUnity.AutoTranslator)

---

# ⚠️ 注意事项

> 部分字体因字形风格原因，可能不适合严肃风格或高可读性场景，请根据游戏氛围自行选择。

本仓库更偏向：

* 汉化兼容资源归档
* TMP 历史版本保存
* Unity 多世代兼容方案
* 萌系汉化字体整理

而非长期维护型项目。

---

# 📜 License

字体版权归原字体作者所有。

本仓库仅提供：

* TMP 字体资产构建
* 多世代兼容性整理
* Unity/TMP 对应世代归档

不提供原字体版权授权。

---

<div align="center">

Preserving moe font compatibility across Unity generations.
Made with 💖 by sorrowmoil

</div>

