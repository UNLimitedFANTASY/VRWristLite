# 发布步骤

1. 将源码文件、`assets/` 和 `docs/` 提交到公开仓库 `UNLimitedFANTASY/VRWristLite`。不要提交本地生成的 `.exe` 或 `dist/`；`.gitignore` 已排除它们。
2. 在 Windows 上运行 `./package-release.ps1`。脚本会构建程序、运行自动检查，并生成 `dist/VRWristLite-v1.0.0-win64.zip`。
3. 创建 `v1.0.0` 标签和 GitHub Release。发布说明可使用 `RELEASE_NOTES.md`，附件上传第 2 步的 Windows 压缩包。
4. 发布后可按需检查其他头显、控制器及 SteamVR 版本的兼容性。

