# 字体与素材授权

## 字体

- `Assets/Cishan/Art/Fonts/NotoSansSC-Regular.ttf`
- `Assets/Cishan/Art/Fonts/NotoSerifSC-Regular.ttf`

来源：Windows 系统字体目录中的 Noto Sans SC / Noto Serif SC 可变字体（NotoSansSC-VF.ttf、
NotoSerifSC-VF.ttf），复制并重命名以便 Unity 识别。

授权：SIL Open Font License 1.1（OFL-1.1），版权归 Google Inc. 所有。
OFL 允许嵌入、修改与再分发，要求保留版权与许可声明，且不得单独售卖字体本身。

> 发布前请从 https://fonts.google.com/noto 获取官方 OFL.txt 一并附上，或替换为项目组已授权的
> 思源黑体 / 思源宋体。当前文件为可变字体，Unity 以默认字重渲染；正式版本建议导出静态字重
> 并按需子集化以减小包体（两个字重合计约 43 MB）。

## 贴图

`Assets/Cishan/Art/Textures` 下的 20 张 PNG 由 `tools/make_unity_textures.py` 程序化生成，
无第三方素材，可随项目自由使用。

## 代码与场景

本次交付的 C#、Shader 与程序化场景为项目定制内容，随《瓷从泥生》项目使用。
