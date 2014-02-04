using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Text.RegularExpressions;

namespace MoltenMercury.ImageProcessing
{
    public sealed class ImageProcessor : ICloneable
    {
        public static readonly ImageProcessor TRANSPARENCY_PROCESSOR =        // Processor for handling transparency
            (new ImageProcessor()).DecodeSettings(MoltenMercury.Properties.Settings.Default.DimProcessor);
        public static readonly ImageProcessor DEFALT_PROCESSOR =              // Default processor
            (new ImageProcessor()).DecodeSettings(Properties.Settings.Default.DefaultProcessor);
        public static readonly ImageProcessor DEFAULT_SKIN_PROCESSOR =        // Default processor for skin color group
            (new ImageProcessor()).DecodeSettings(Properties.Settings.Default.DefaultSkinProcessor);


        public enum AdjustmentMode
        {
            Multiplication,     // M
            Addition,           // +
            Absolute,           // A
            None                // X
        }

        private ColorRGB m_baseColor;
        private AdjustmentMode m_hm = AdjustmentMode.Absolute;
        private AdjustmentMode m_sm = AdjustmentMode.Absolute;
        private AdjustmentMode m_lm = AdjustmentMode.Absolute;

        private Double m_ha;
        private Double m_sa;
        private Double m_la;
        private Double m_aa = 1;

        private Double m_step;


        public ColorRGB BaseColor
        {
            get { return m_baseColor; }
            set { m_baseColor = value; }
        }
        public AdjustmentMode HueMode
        {
            get { return m_hm; }
            set { m_hm = value; }
        }
        public AdjustmentMode SaturationMode
        {
            get { return m_sm; }
            set { m_sm = value; }
        }
        public AdjustmentMode LightnessMode
        {
            get { return m_lm; }
            set { m_lm = value; }
        }

        public Double HA
        {
            get { return m_ha; }
            set { m_ha = value; }
        }
        public Double SA
        {
            get { return m_sa; }
            set { m_sa = value; }
        }
        public Double LA
        {
            get { return m_la; }
            set { m_la = value; }
        }
        public Double AA
        {
            get { return m_aa; }
            set { m_aa = value; }
        }

        public Double Step
        {
            get { return m_step; }
            set { m_step = value; }
        }

        public Bitmap ProcessBitmap(Bitmap source)
        {

            if (HueMode == AdjustmentMode.None && SaturationMode == AdjustmentMode.None && LightnessMode == AdjustmentMode.None && AA == 1)
                return source;

            Bitmap result = new Bitmap(source.Width, source.Height);

            if (m_aa == 0)
                return result;

            for (int ix = 0; ix < result.Width; ix++)
            {
                for (int iy = 0; iy < result.Height; iy++)
                {
                        Color pixel = source.GetPixel(ix, iy);

                        if (pixel.A == 0) continue;

                        ColorRGB pixelEX = new ColorRGB(pixel);

                        pixelEX = TransformColor(pixelEX);
                        pixelEX.A = (byte)Coerce(pixel.A * m_aa, 0, 255);
                        result.SetPixel(ix, iy, pixelEX);
                }
            }

            return result;
        }

        public ColorRGB TransformColor(ColorRGB source)
        {
            if (source.H > m_baseColor.H + m_step || source.H < m_baseColor.H - m_step)
                return source;
            else
                return ColorRGB.FromHSLA(
                            Adjust(source.H, HA, m_hm),
                            Adjust(source.S, SA, m_sm),
                            Adjust(source.L, LA, m_lm),
                            source.A);
        }

        #region Encode/Decode Settings

        public String EncodeSettings()
        {
            StringBuilder sbuilder = new StringBuilder();
            sbuilder.Append("MM|");
            
            // Step
            sbuilder.AppendFormat("ST{0}|", Step);

            // Base Color
            sbuilder.AppendFormat("BC{0}|", ((Color)BaseColor).ToArgb());

            // Hue
            sbuilder.AppendFormat("H{0}{1}|", EncodeMode(m_hm), Math.Round(HA, 5));

            // Saturation
            sbuilder.AppendFormat("S{0}{1}|", EncodeMode(m_sm), Math.Round(SA, 5));

            // Lumination
            sbuilder.AppendFormat("L{0}{1}|", EncodeMode(m_lm), Math.Round(LA, 5));

            // Alpha
            sbuilder.AppendFormat("AA{0}", Math.Round(AA, 5));

            return sbuilder.ToString();
        }

        public ImageProcessor DecodeSettings(String str)
        {
            if (!str.StartsWith("MM|"))
                throw new InvalidOperationException("Unexpected Input!");

            Match exprMatch = Regex.Match(str, @"MM\|ST(?<step>[\-0-9\.]+)\|BC(?<base>[\-0-9]+)\|H(?<hm>(A|M|X|\+){1})(?<ha>[\-0-9\.]+)\|S(?<sm>(A|M|X|\+){1})(?<sa>[\-0-9\.]+)\|L(?<lm>(A|M|X|\+){1})(?<la>[\-0-9\.]+)\|AA(?<aa>[\-0-9\.]+)", RegexOptions.ExplicitCapture);

            if (!exprMatch.Success) throw new Exception();

            this.Step = double.Parse(exprMatch.Result("${step}"));
            this.BaseColor = new ColorRGB(Color.FromArgb(Int32.Parse(exprMatch.Result("${base}"))));
            this.m_hm = DecodeMode(exprMatch.Result("${hm}")[0]);
            this.m_sm = DecodeMode(exprMatch.Result("${sm}")[0]);
            this.m_lm = DecodeMode(exprMatch.Result("${lm}")[0]);

            this.m_ha = Double.Parse(exprMatch.Result("${ha}"));
            this.m_sa = Double.Parse(exprMatch.Result("${sa}"));
            this.m_la = Double.Parse(exprMatch.Result("${la}"));
            this.m_aa = Double.Parse(exprMatch.Result("${aa}"));

            return this;
        }


        private char EncodeMode(AdjustmentMode m)
        {
            switch (m)
            {
                case AdjustmentMode.Absolute: return 'A';
                case AdjustmentMode.Addition: return '+';
                case AdjustmentMode.Multiplication: return 'M';
                case AdjustmentMode.None: return 'X';
            }

            return 'X';
        }
        private AdjustmentMode DecodeMode(char c)
        {
            switch (c)
            {
                case 'A': return AdjustmentMode.Absolute;
                case '+': return AdjustmentMode.Addition;
                case 'M': return AdjustmentMode.Multiplication;
                case 'X': return AdjustmentMode.None;
            }
            throw new Exception();
        }


        public override string ToString()
        {
            return this.EncodeSettings();
        }
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        #endregion

        #region Transform Utilties

        private Double Coerce(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private Double Adjust(double value, double adjustment, AdjustmentMode mode)
        {
            if (mode == AdjustmentMode.Multiplication)
                return Coerce(value * adjustment, 0, 1);
            else if (mode == AdjustmentMode.Addition)
                return Coerce(value + adjustment, 0, 1);
            else if (mode == AdjustmentMode.Absolute)
                return Coerce(adjustment, 0, 1);
            else
                return Coerce(value, 0, 1);
        }

        #endregion

        public object Clone()
        {
            return new ImageProcessor()
            {
                BaseColor = this.BaseColor,
                HA = this.HA,
                SA = this.SA,
                LA = this.LA,
                HueMode = this.HueMode,
                SaturationMode = this.SaturationMode,
                LightnessMode = this.LightnessMode,
                Step = this.Step
            };
        }
    }
}
