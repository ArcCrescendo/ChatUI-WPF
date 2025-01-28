# ChatUI 项目

欢迎来到 ChatUI-WPF 项目！这是一个基于 WPF 的桌面应用程序，旨在提供一个用户友好的界面，用于语音识别和文本转录。该项目使用了 Whisper 模型来实现语音到文本的转换。

## 功能

- **语音识别**：通过麦克风捕获音频并实时转录为文本。
- **多语言支持**：支持多种语言的语音识别，默认语言为中文。
- **设备选择**：用户可以选择不同的音频输入设备进行录音。

## 安装

1. **克隆仓库**

   ```bash
   git clone https://github.com/ArcCrescendo/ChatUI-WPF.git
   cd ChatUI-WPF
   ```

2. **安装依赖项**

   确保您的开发环境中安装了 .NET 8.0 SDK。

3. **下载 Whisper 模型**

   您需要下载 Whisper 模型文件并将其放置在项目的根目录中。默认情况下，模型文件名应为 `ggml-base-q8_0.bin`。您可以从以下链接下载模型：

   ```
   https://huggingface.co/datasets/ggerganov/whisper.cpp/resolve/main/ggml-base.bin
   ```

4. **构建项目**

   在项目根目录中运行以下命令以构建项目：

   ```bash
   dotnet build
   ```

## 使用

### 使用命令行

1. **运行应用程序**

   在项目根目录中运行以下命令以启动应用程序：

   ```bash
   dotnet run
   ```

2. **选择音频设备**

   启动应用程序后，您可以从界面中选择音频输入设备。

3. **开始录音**

   点击"开始录音"按钮，应用程序将开始捕获音频并实时转录为文本。

4. **查看转录结果**

   转录的文本将显示在应用程序界面中。

### 使用 Visual Studio

1. **打开项目**

   在 Visual Studio 中，选择"打开项目或解决方案"，然后导航到克隆的 ChatUI 项目目录，选择 `ChatUI.sln` 文件。

2. **构建项目**

   在 Visual Studio 中，选择"生成"菜单，然后选择"生成解决方案"以构建项目。

3. **运行应用程序**

   按下 F5 键或点击"启动"按钮以调试模式运行应用程序。您也可以选择"开始执行（不调试）"以非调试模式运行。

4. **使用应用程序**

   使用应用程序的界面选择音频设备并开始录音，查看转录结果。

## 贡献

欢迎贡献者！如果您有任何改进建议或发现了问题，请随时提交 issue 或 pull request。

## 许可证

本项目采用 MIT 许可证。详情请参阅 [LICENSE](LICENSE) 文件。

## 联系

如果您有任何问题或建议，请通过以下方式与我联系：

- 电子邮件：YoCoco2233@outlook.com
- GitHub: [ArcCrescendo](https://github.com/ArcCrescendo)

感谢您使用 ChatUI 项目！ 