using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NintrollerLib
{
    public struct ClassicController : INintrollerState
    {
        public static class InputNames
        {
            public const string A      = "ccA";
            public const string B      = "ccB";
            public const string X      = "ccX";
            public const string Y      = "ccY";

            public const string UP     = "ccUP";
            public const string DOWN   = "ccDOWN";
            public const string LEFT   = "ccLEFT";
            public const string RIGHT  = "ccRIGHT";

            public const string L      = "ccL";
            public const string R      = "ccR";
            public const string ZL     = "ccZL";
            public const string ZR     = "ccZR";

            public const string LX     = "ccLX";
            public const string LY     = "ccLY";
            public const string RX     = "ccRX";
            public const string RY     = "ccRY";

            public const string LUP    = "ccLUP";
            public const string LDOWN  = "ccLDOWN";
            public const string LLEFT  = "ccLLEFT";
            public const string LRIGHT = "ccLRIGHT";

            public const string RUP    = "ccRUP";
            public const string RDOWN  = "ccRDOWN";
            public const string RLEFT  = "ccRLEFT";
            public const string RRIGHT = "ccRRIGHT";

            public const string LT     = "ccLT";
            public const string RT     = "ccRT";
            public const string LFULL  = "ccLFULL";
            public const string RFULL  = "ccRFULL";

            public const string SELECT = "ccSELECT";
            public const string START  = "ccSTART";
            public const string HOME   = "ccHOME";
        }

#if DEBUG
        private bool _debugViewActive;
        public bool DebugViewActive
        {
            get
            {
                return _debugViewActive;
            }
            set
            {
                _debugViewActive = value;
            }
        }
#endif

        public Wiimote wiimote;
        public Joystick LJoy, RJoy;
        public Trigger L, R;
        public bool A, B, X, Y;
        public bool Up, Down, Left, Right;
        public bool ZL, ZR, LFull, RFull;
        public bool Plus, Minus, Home;

        public ClassicController(Wiimote wm)
        {
            this = new ClassicController();
            wiimote = wm;
        }

        public bool Start
        {
            get { return Plus; }
            set { Plus = value; }
        }

        public bool Select
        {
            get { return Minus; }
            set { Minus = value; }
        }

        public void Update(byte[] data)
        {
            int offset = 0;
            switch ((InputReport)data[0])
            {
                case InputReport.BtnsExt:
                case InputReport.BtnsExtB:
                    offset = 3;
                    break;
                case InputReport.BtnsAccExt:
                    offset = 6;
                    break;
                case InputReport.BtnsIRExt:
                    offset = 13;
                    break;
                case InputReport.BtnsAccIRExt:
                    offset = 16;
                    break;
                case InputReport.ExtOnly:
                    offset = 1;
                    break;
                default:
                    return;
            }

            if (offset > 0)
            {
                // Buttons
                A     = (data[offset + 5] & 0x10) == 0;
                B     = (data[offset + 5] & 0x40) == 0;
                X     = (data[offset + 5] & 0x08) == 0;
                Y     = (data[offset + 5] & 0x20) == 0;
                LFull = (data[offset + 4] & 0x20) == 0;  // Until the Click
                RFull = (data[offset + 4] & 0x02) == 0;  // Until the Click
                ZL    = (data[offset + 5] & 0x80) == 0;
                ZR    = (data[offset + 5] & 0x04) == 0;
                Plus  = (data[offset + 4] & 0x04) == 0;
                Minus = (data[offset + 4] & 0x10) == 0;
                Home  = (data[offset + 4] & 0x08) == 0;

                // Dpad
                Up    = (data[offset + 5] & 0x01) == 0;
                Down  = (data[offset + 4] & 0x40) == 0;
                Left  = (data[offset + 5] & 0x02) == 0;
                Right = (data[offset + 4] & 0x80) == 0;

                // Joysticks
                LJoy.rawX = (byte)(data[offset] & 0x3F);
                LJoy.rawY = (byte)(data[offset + 1] & 0x03F);
                RJoy.rawX = (byte)(data[offset + 2] >> 7 | (data[offset + 1] & 0xC0) >> 5 | (data[offset] & 0xC0) >> 3);
                RJoy.rawY = (byte)(data[offset + 2] & 0x1F);

                // Triggers
                L.rawValue = (byte)(((data[offset + 2] & 0x60) >> 2) | (data[offset + 3] >> 5));
                R.rawValue = (byte)(data[offset + 3] & 0x1F);
                L.full = LFull;
                R.full = RFull;

                // Normalize
                LJoy.Normalize();
                RJoy.Normalize();
                L.Normalize();
                R.Normalize();
            }

            wiimote.Update(data);
        }

        public float GetValue(string input)
        {
            throw new NotImplementedException();
        }

        public void SetCalibration(Calibrations.CalibrationPreset preset)
        {
            wiimote.SetCalibration(preset);

            switch (preset)
            {
                case Calibrations.CalibrationPreset.Default:
                    //LJoy.Calibrate(Calibrations.Defaults.ClassicControllerDefault.LJoy);
                    //RJoy.Calibrate(Calibrations.Defaults.ClassicControllerDefault.RJoy);
                    //L.Calibrate(Calibrations.Defaults.ClassicControllerDefault.L);
                    //R.Calibrate(Calibrations.Defaults.ClassicControllerDefault.R);
                    SetCalibration(Calibrations.Defaults.ClassicControllerDefault);
                    break;

                case Calibrations.CalibrationPreset.Modest:
                    SetCalibration(Calibrations.Moderate.ClassicControllerModest);
                    break;

                case Calibrations.CalibrationPreset.Extra:
                    SetCalibration(Calibrations.Extras.ClassicControllerExtra);
                    break;

                case Calibrations.CalibrationPreset.Minimum:
                    SetCalibration(Calibrations.Minimum.ClassicControllerMinimal);
                    break;

                case Calibrations.CalibrationPreset.None:
                    SetCalibration(Calibrations.None.ClassicControllerRaw);
                    break;
            }
        }

        public void SetCalibration(INintrollerState from)
        {
            if (from.CalibrationEmpty)
            {
                // don't apply empty calibrations
                return;
            }

            if (from.GetType() == typeof(ClassicController))
            {
                LJoy.Calibrate(((ClassicController)from).LJoy);
                RJoy.Calibrate(((ClassicController)from).RJoy);
                L.Calibrate(((ClassicController)from).L);
                R.Calibrate(((ClassicController)from).R);
            }
            else if (from.GetType() == typeof(Wiimote))
            {
                wiimote.SetCalibration(from);
            }
        }

        public void SetCalibration(string calibrationString)
        {
            if (calibrationString.Count(c => c == '0') > 5)
            {
                // don't set empty calibrations
                return;
            }

            string[] components = calibrationString.Split(new char[] { ':' });

            foreach (string component in components)
            {
                if (component.StartsWith("joyL"))
                {
                    string[] joyLConfig = component.Split(new char[] { '|' });

                    for (int jL = 1; jL < joyLConfig.Length; jL++)
                    {
                        int value = 0;
                        if (int.TryParse(joyLConfig[jL], out value))
                        {
                            switch (jL)
                            {
                                case 1: LJoy.centerX = value; break;
                                case 2: LJoy.minX = value; break;
                                case 3: LJoy.maxX = value; break;
                                case 4: LJoy.deadX = value; break;
                                case 5: LJoy.centerY = value; break;
                                case 6: LJoy.minY = value; break;
                                case 7: LJoy.maxY = value; break;
                                case 8: LJoy.deadY = value; break;
                                default: break;
                            }
                        }
                    }
                }
                else if (component.StartsWith("joyR"))
                {
                    string[] joyRConfig = component.Split(new char[] { '|' });

                    for (int jR = 1; jR < joyRConfig.Length; jR++)
                    {
                        int value = 0;
                        if (int.TryParse(joyRConfig[jR], out value))
                        {
                            switch (jR)
                            {
                                case 1: RJoy.centerX = value; break;
                                case 2: RJoy.minX = value; break;
                                case 3: RJoy.maxX = value; break;
                                case 4: RJoy.deadX = value; break;
                                case 5: RJoy.centerY = value; break;
                                case 6: RJoy.minY = value; break;
                                case 7: RJoy.maxY = value; break;
                                case 8: RJoy.deadY = value; break;
                                default: break;
                            }
                        }
                    }
                }
                else if (component.StartsWith("tl"))
                {
                    string[] triggerLConfig = component.Split(new char[] { '|' });

                    for (int tl = 1; tl < triggerLConfig.Length; tl++)
                    {
                        int value = 0;
                        if (int.TryParse(triggerLConfig[tl], out value))
                        {
                            switch (tl)
                            {
                                case 1: L.min = value; break;
                                case 2: L.max = value; break;
                                default: break;
                            }
                        }
                    }
                }
                else if (component.StartsWith("tr"))
                {
                    string[] triggerRConfig = component.Split(new char[] { '|' });

                    for (int tr = 1; tr < triggerRConfig.Length; tr++)
                    {
                        int value = 0;
                        if (int.TryParse(triggerRConfig[tr], out value))
                        {
                            switch (tr)
                            {
                                case 1: R.min = value; break;
                                case 2: R.max = value; break;
                                default: break;
                            }
                        }
                    }
                }
            }
        }

        public string GetCalibrationString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("-cla");
            sb.Append(":joyL");
                sb.Append("|"); sb.Append(LJoy.centerX);
                sb.Append("|"); sb.Append(LJoy.minX);
                sb.Append("|"); sb.Append(LJoy.maxX);
                sb.Append("|"); sb.Append(LJoy.deadX);
                sb.Append("|"); sb.Append(LJoy.centerY);
                sb.Append("|"); sb.Append(LJoy.minY);
                sb.Append("|"); sb.Append(LJoy.maxY);
                sb.Append("|"); sb.Append(LJoy.deadY);
            sb.Append(":joyR");
                sb.Append("|"); sb.Append(RJoy.centerX);
                sb.Append("|"); sb.Append(RJoy.minX);
                sb.Append("|"); sb.Append(RJoy.maxX);
                sb.Append("|"); sb.Append(RJoy.deadX);
                sb.Append("|"); sb.Append(RJoy.centerY);
                sb.Append("|"); sb.Append(RJoy.minY);
                sb.Append("|"); sb.Append(RJoy.maxY);
                sb.Append("|"); sb.Append(RJoy.deadY);
            sb.Append(":tl");
                sb.Append("|"); sb.Append(L.min);
                sb.Append("|"); sb.Append(L.max);
            sb.Append(":tr");
                sb.Append("|"); sb.Append(R.min);
                sb.Append("|"); sb.Append(R.max);

            return sb.ToString();
        }

        public bool CalibrationEmpty
        {
            get
            {
                if (LJoy.maxX == 0 && LJoy.maxY == 0 && RJoy.maxX == 0 && RJoy.maxY == 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public IEnumerator<KeyValuePair<string, float>> GetEnumerator()
        {
            // Wiimote
            foreach (var input in wiimote)
            {
                yield return input;
            }

            yield return new KeyValuePair<string, float>(InputNames.A, A ? 1.0f : 0.0f);
            yield return new KeyValuePair<string, float>(InputNames.B, B ? 1.0f : 0.0f);
            yield return new KeyValuePair<string, float>(InputNames.X, X ? 1.0f : 0.0f);
            yield return new KeyValuePair<string, float>(InputNames.Y, Y ? 1.0f : 0.0f);

            yield return new KeyValuePair<string, float>(InputNames.L, L.value > 0 ? 1.0f : 0.0f);
            yield return new KeyValuePair<string, float>(InputNames.R, R.value > 0 ? 1.0f : 0.0f);
            yield return new KeyValuePair<string, float>(InputNames.ZL, ZL ? 1.0f : 0.0f);
            yield return new KeyValuePair<string, float>(InputNames.ZR, ZR ? 1.0f : 0.0f);

            yield return new KeyValuePair<string, float>(InputNames.UP, Up ? 1.0f : 0.0f);
            yield return new KeyValuePair<string, float>(InputNames.DOWN, Down ? 1.0f : 0.0f);
            yield return new KeyValuePair<string, float>(InputNames.LEFT, Left ? 1.0f : 0.0f);
            yield return new KeyValuePair<string, float>(InputNames.RIGHT, Right ? 1.0f : 0.0f);

            yield return new KeyValuePair<string, float>(InputNames.START, Start ? 1.0f : 0.0f);
            yield return new KeyValuePair<string, float>(InputNames.SELECT, Select ? 1.0f : 0.0f);
            yield return new KeyValuePair<string, float>(InputNames.HOME, Home ? 1.0f : 0.0f);

            L.Normalize();
            R.Normalize();
            yield return new KeyValuePair<string, float>(InputNames.LFULL, L.full ? 1.0f : 0.0f);
            yield return new KeyValuePair<string, float>(InputNames.RFULL, R.full ? 1.0f : 0.0f);
            yield return new KeyValuePair<string, float>(InputNames.LT, L.value);
            yield return new KeyValuePair<string, float>(InputNames.RT, R.value);

            LJoy.Normalize();
            RJoy.Normalize();
            yield return new KeyValuePair<string, float>(InputNames.LX, LJoy.X);
            yield return new KeyValuePair<string, float>(InputNames.LY, LJoy.Y);
            yield return new KeyValuePair<string, float>(InputNames.RX, RJoy.X);
            yield return new KeyValuePair<string, float>(InputNames.RY, RJoy.X);

            yield return new KeyValuePair<string, float>(InputNames.LUP, LJoy.Y > 0f ? LJoy.Y : 0.0f);
            yield return new KeyValuePair<string, float>(InputNames.LDOWN, LJoy.Y > 0f ? 0.0f : -LJoy.Y); // These are inverted
            yield return new KeyValuePair<string, float>(InputNames.LLEFT, LJoy.X > 0f ? 0.0f : -LJoy.X); // because they
            yield return new KeyValuePair<string, float>(InputNames.LRIGHT, LJoy.X > 0f ? LJoy.X : 0.0f);

            yield return new KeyValuePair<string, float>(InputNames.RUP, RJoy.Y > 0f ? RJoy.Y : 0.0f);
            yield return new KeyValuePair<string, float>(InputNames.RDOWN, RJoy.Y > 0f ? 0.0f : -RJoy.Y); // represents how far the
            yield return new KeyValuePair<string, float>(InputNames.RLEFT, RJoy.X > 0f ? 0.0f : -RJoy.X); // input is left or down
            yield return new KeyValuePair<string, float>(InputNames.RRIGHT, RJoy.X > 0f ? RJoy.X : 0.0f);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
