using System.Drawing;
using System.Windows.Forms;

namespace StarcUp
{
    public class OverlayUI
    {
        private PictureBox imageBox;
        private Label textLabel;
        private Panel container;

        public Panel Container => container;

        public void CreateUI()
        {
            // 전체 컨테이너 패널 생성
            container = new Panel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Location = new Point(0, 0)
            };

            // 이미지 박스 생성
            imageBox = new PictureBox
            {
                Size = new Size(64, 64),
                Location = new Point(0, 0),
                BackColor = Color.Transparent,
                SizeMode = PictureBoxSizeMode.StretchImage
            };

            // 샘플 이미지 생성
            Bitmap sampleImage = CreateSampleImage();
            imageBox.Image = sampleImage;

            // 텍스트 라벨 생성
            textLabel = new Label
            {
                Text = "스타크래프트 오버레이 활성화!",
                ForeColor = Color.Yellow,
                BackColor = Color.Transparent,
                Font = new Font("맑은 고딕", 12, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(74, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 컨테이너에 컨트롤들 추가
            container.Controls.Add(imageBox);
            container.Controls.Add(textLabel);
        }

        public void UpdateText(string text)
        {
            if (textLabel != null)
            {
                textLabel.Text = text;
            }
        }

        public void UpdateImage(Image image)
        {
            if (imageBox != null)
            {
                imageBox.Image = image;
            }
        }

        private Bitmap CreateSampleImage()
        {
            Bitmap bitmap = new Bitmap(64, 64);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.FillEllipse(Brushes.Blue, 0, 0, 64, 64);
                g.FillEllipse(Brushes.White, 16, 16, 32, 32);
                g.DrawString("SC", new Font("Arial", 14, FontStyle.Bold),
                    Brushes.Black, new PointF(20, 20));
            }
            return bitmap;
        }
    }
}