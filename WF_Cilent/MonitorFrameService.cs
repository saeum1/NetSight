using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Text;

namespace WF_Client
{
    public class MonitorFrameService
    {
        public byte[] CaptureFrame()
        {
            int width = Screen.PrimaryScreen.Bounds.Width;
            int height = Screen.PrimaryScreen.Bounds.Height;

            using var bmp = new Bitmap(width, height);
            using var g = Graphics.FromImage(bmp);
            g.CopyFromScreen(0, 0, 0, 0, new Size(width, height));

            using var ms = new MemoryStream();
            var encoder = GetJpegEncoder();
            var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)ClientConfig.JpegQuality);

            bmp.Save(ms, encoder, encoderParams);

            // 패킷 구조: [타입:1byte][Width:4byte][Height:4byte][이미지데이터]
            byte[] imageData = ms.ToArray();
            byte[] packet = new byte[1 + 4 + 4 + imageData.Length];

            packet[0] = 0x02; // MonitorFrame 타입
            BitConverter.GetBytes(width).CopyTo(packet, 1);
            BitConverter.GetBytes(height).CopyTo(packet, 5);
            imageData.CopyTo(packet, 9);

            return packet;
        }

        private ImageCodecInfo GetJpegEncoder()
        {
            foreach (var codec in ImageCodecInfo.GetImageEncoders())
                if (codec.FormatID == ImageFormat.Jpeg.Guid)
                    return codec;
            return null;
        }
    }
}
