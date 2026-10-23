# 第三方声明

WristLite 的自有代码采用 [MIT 许可](LICENSE)。以下文件保留原项目许可：

| 内容 | 来源 | 许可 |
| --- | --- | --- |
| `assets/*.png` 中的 VR 设备图标 | [VRCX](https://github.com/vrcx-team/VRCX/tree/master/src/public/images/vr) | [MIT](LICENSE-VRCX.txt) |
| `OpenVR.cs` C# 绑定 | [Valve OpenVR，经 VRCX 仓库发布](https://github.com/vrcx-team/VRCX/blob/master/Dotnet/Overlay/OpenVR/openvr_api.cs) | [Valve BSD 三条款](LICENSE-OpenVR.txt) |

腕部摆放和按键映射参考了 VRCX。叠加层在收到 `ImageLoaded` 后再切换的顺序参考了 [VRCX-0](https://github.com/Map1en/VRCX-0/blob/master/crates/host-desktop/src/vr_overlay/openvr_backend.rs)；项目未复制 VRCX-0 的 GPL 源码。
