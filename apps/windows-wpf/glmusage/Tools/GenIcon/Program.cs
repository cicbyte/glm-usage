using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

var outPath = args.Length > 0 ? args[0] : Path.Combine("..", "app.ico");

var sizes = new[] { 256, 48, 32, 16 };
var images = new List<(byte[] pngBytes, int size)>();

foreach (var size in sizes)
{
    using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
    using var g = Graphics.FromImage(bmp);
    g.SmoothingMode = SmoothingMode.AntiAlias;
    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
    g.Clear(Color.Transparent);

    // 绿色渐变圆形
    using var grad = new LinearGradientBrush(
        new Point(0, 0), new Point(size, size),
        Color.FromArgb(255, 0x27, 0xAE, 0x60),
        Color.FromArgb(255, 0x2E, 0xCC, 0x71));
    g.FillEllipse(grad, 0, 0, size, size);

    // 白色 G
    var fontSize = (int)(size * 0.55);
    using var font = new Font("Segoe UI", fontSize, FontStyle.Bold);
    using var textBrush = new SolidBrush(Color.White);
    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
    g.DrawString("G", font, textBrush, new RectangleF(0, 0, size, size), sf);

    // 转为 PNG 字节
    using var ms = new MemoryStream();
    bmp.Save(ms, ImageFormat.Png);
    images.Add((ms.ToArray(), size));
}

// 构建标准 ICO 文件（PNG 格式嵌入）
using var fs = File.Create(outPath);
using var writer = new BinaryWriter(fs);

// ICO 头
writer.Write((short)0);     // 保留
writer.Write((short)1);     // 类型: 图标
writer.Write((short)images.Count);

// 计算偏移
int headerSize = 6 + images.Count * 16;
int dataOffset = headerSize;

// 目录条目
foreach (var (pngBytes, size) in images)
{
    writer.Write((byte)(size >= 256 ? 0 : size));  // 宽度
    writer.Write((byte)(size >= 256 ? 0 : size));  // 高度
    writer.Write((byte)0);      // 调色板数
    writer.Write((byte)0);      // 保留
    writer.Write((short)1);     // 色彩平面
    writer.Write((short)32);    // 位深度
    writer.Write(pngBytes.Length);  // 数据大小
    writer.Write(dataOffset);   // 数据偏移
    dataOffset += pngBytes.Length;
}

// PNG 图像数据
foreach (var (pngBytes, _) in images)
    writer.Write(pngBytes);

Console.WriteLine($"图标已生成: {Path.GetFullPath(outPath)} ({new FileInfo(outPath).Length} bytes)");
