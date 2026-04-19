// 生成 app.ico - 运行: dotnet script GenIcon.csx 或直接编译运行
// 需要: dotnet add package System.Drawing.Common --version 8.0.0

using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

var outPath = args.Length > 0 ? args[0] : Path.Combine("..", "app.ico");

void DrawIcon(int size, string filePath)
{
    using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
    using var g = Graphics.FromImage(bmp);
    g.SmoothingMode = SmoothingMode.AntiAlias;
    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
    g.Clear(Color.Transparent);

    // 绿色渐变圆形
    using var gradBrush = new LinearGradientBrush(
        new Point(0, 0), new Point(size, size),
        Color.FromArgb(255, 0x27, 0xAE, 0x60),
        Color.FromArgb(255, 0x2E, 0xCC, 0x71));
    g.FillEllipse(gradBrush, 0, 0, size, size);

    // 白色 G
    var fontSize = (int)(size * 0.55);
    using var font = new Font("Segoe UI", fontSize, FontStyle.Bold);
    using var textBrush = new SolidBrush(Color.White);
    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
    g.DrawString("G", font, textBrush, new RectangleF(0, 0, size, size), sf);

    using var ms = new MemoryStream();
    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
    ms.Position = 0;
    using var sysIcon = Icon.FromHandle(bmp.GetHicon());
    using var fs = File.Create(filePath);
    sysIcon.Save(fs);
}

// 生成多尺寸 ICO（256, 48, 32, 16）
var sizes = new[] { 256, 48, 32, 16 };
var tmpFiles = new List<string>();

foreach (var size in sizes)
{
    var tmp = Path.Combine(Path.GetTempPath(), $"glm_{size}.ico");
    DrawIcon(size, tmp);
    tmpFiles.Add(tmp);
}

// 合并为多尺寸 ICO
CombineIcons(tmpFiles, outPath);

foreach (var f in tmpFiles)
    try { File.Delete(f); } catch { }

Console.WriteLine($"图标已生成: {Path.GetFullPath(outPath)}");

void CombineIcons(List<string> iconFiles, string outputPath)
{
    using var writer = new BinaryWriter(File.Create(outputPath));

    // ICO 头
    writer.Write((short)0);     // 保留
    writer.Write((short)1);     // 类型: 图标
    writer.Write((short)iconFiles.Count); // 数量

    var images = new List<(byte[] data, int size)>();
    foreach (var f in iconFiles)
    {
        var bytes = File.ReadAllBytes(f);
        using var ms = new MemoryStream(bytes);
        using var bmp = new Bitmap(ms);
        images.Add((bytes, bmp.Size.Width));
    }

    // 目录条目
    int dataOffset = 6 + iconFiles.Count * 16;
    foreach (var (data, size) in images)
    {
        writer.Write((byte)(size >= 256 ? 0 : size));  // 宽度
        writer.Write((byte)(size >= 256 ? 0 : size));  // 高度
        writer.Write((byte)0);      // 调色板
        writer.Write((byte)0);      // 保留
        writer.Write((short)1);     // 色彩平面
        writer.Write((short)32);    // 位深度
        writer.Write(data.Length);  // 数据大小
        writer.Write(dataOffset);   // 偏移
        dataOffset += data.Length;
    }

    // 图像数据
    foreach (var (data, _) in images)
        writer.Write(data);
}
