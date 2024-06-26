using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NintrollerLib
{
    public struct Wiimote : INintrollerState
    {
        public static class InputNames
        {
            public const string A           = "wA";
            public const string B           = "wB";
            public const string ONE         = "wONE";
            public const string TWO         = "wTWO";

            // dpad when wiimote is vertical
            public const string UP          = "wUP";
            public const string DOWN        = "wDOWN";
            public const string LEFT        = "wLEFT";
            public const string RIGHT       = "wRIGHT";

            public const string MINUS       = "wMINUS";
            public const string PLUS        = "wPLUS";
            public const string HOME        = "wHOME";

            // Accelerometer
            public const string ACC_X       = "wAccX";
            public const string ACC_Y       = "wAccY";
            public const string ACC_Z       = "wAccZ";

            // for quick movement
            public const string ACC_UP       = "wACCUP";
            public const string ACC_DOWN     = "wACCDOWN";
            public const string ACC_LEFT     = "wACCLEFT";
            public const string ACC_RIGHT    = "wACCRIGHT";
            public const string ACC_FORWARD  = "wACCFORWARD";
            public const string ACC_BACKWARD = "wACCBACKWARD";
            public const string ACC_SHAKE_X  = "wACCSHAKEX";
            public const string ACC_SHAKE_Y  = "wACCSHAKEY";
            public const string ACC_SHAKE_Z  = "wACCSHAKEZ";

            // tilting the controler with the wrist
            public const string TILT_RIGHT  = "wTILTRIGHT";
            public const string TILT_LEFT   = "wTILTLEFT";
            public const string TILT_UP     = "wTILTUP";
            public const string TILT_DOWN   = "wTILTDOWN";
            public const string FACE_UP     = "wTILTFACEUP";
            public const string FACE_DOWN   = "wTILTFACEDOWN";

            // Pointer from IR camera
            public const string IR_X        = "wIRX";
            public const string IR_Y        = "wIRY";
            public const string IR_UP       = "wIRUP";
            public const string IR_DOWN     = "wIRDOWN";
            public const string IR_LEFT     = "wIRLEFT";
            public const string IR_RIGHT    = "wIRRIGHT";
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

        public CoreButtons buttons;
        public Accelerometer accelerometer;
        public IR irSensor;
        //INintrollerState extension;

        public Wiimote(byte[] rawData)
        {
            buttons = new CoreButtons();
            accelerometer = new Accelerometer();
            irSensor = new IR();
            //extension = null;

#if DEBUG
            _debugViewActive = false;
#endif

            Update(rawData);
        }

        public void Update(byte[] data)
        {
            buttons.Parse(data, 1);
            accelerometer.Parse(data, 3);
            irSensor.Parse(data, 3);

            accelerometer.Normalize();
        }

        public float GetValue(string input)
        {
            throw new NotImplementedException();
        }

        public void SetCalibration(Calibrations.CalibrationPreset preset)
        {
            switch (preset)
            {
                case Calibrations.CalibrationPreset.Default:
                    //accelerometer.Calibrate(Calibrations.Defaults.WiimoteDefault.accelerometer);
                    SetCalibration(Calibrations.Defaults.WiimoteDefault);
                    break;

                case Calibrations.CalibrationPreset.Modest:
                    SetCalibration(Calibrations.Moderate.WiimoteModest);
                    break;

                case Calibrations.CalibrationPreset.Extra:
                    SetCalibration(Calibrations.Extras.WiimoteExtra);
                    break;

                case Calibrations.CalibrationPreset.Minimum:
                    SetCalibration(Calibrations.Minimum.WiimoteMinimal);
                    break;

                case Calibrations.CalibrationPreset.None:
                    SetCalibration(Calibrations.None.WiimoteRaw);
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

            if (from.GetType() == typeof(Wiimote))
            {
                accelerometer.Calibrate(((Wiimote)from).accelerometer);
                irSensor.boundingArea = ((Wiimote)from).irSensor.boundingArea;
            }
            else if (from.GetType() == typeof(Nunchuk))
            {
                accelerometer.Calibrate(((Nunchuk)from).wiimote.accelerometer);
                irSensor.boundingArea = ((Nunchuk)from).wiimote.irSensor.boundingArea;
            }
            else if (from.GetType() == typeof(ClassicController))
            {
                accelerometer.Calibrate(((ClassicController)from).wiimote.accelerometer);
                irSensor.boundingArea = ((ClassicController)from).wiimote.irSensor.boundingArea;
            }
            else if (from.GetType() == typeof(ClassicControllerPro))
            {
                accelerometer.Calibrate(((ClassicControllerPro)from).wiimote.accelerometer);
                irSensor.boundingArea = ((ClassicControllerPro)from).wiimote.irSensor.boundingArea;
            }
        }

        public void SetCalibration(string calibrationString)
        {
            if (calibrationString.Count(c => c == '0') > 5)
            {
                // don't set empty calibrations
                return;
            }

            string[] components = calibrationString.Split(new char[] {':'});

            foreach (string component in components)
            {
                if (component.StartsWith("acc"))
                {
                    string[] accConfig = component.Split(new char[] { '|' });

                    for (int a = 1; a < accConfig.Length; a++)
                    {
                        int value = 0;
                        if (int.TryParse(accConfig[a], out value))
                        {
                            switch (a)
                            {
                                case 1:  accelerometer.centerX = value; break;
                                case 2:  accelerometer.minX    = value; break;
                                case 3:  accelerometer.maxX    = value; break;
                                case 4:  accelerometer.deadX   = value; break;
                                case 5:  accelerometer.centerY = value; break;
                                case 6:  accelerometer.minY    = value; break;
                                case 7:  accelerometer.maxY    = value; break;
                                case 8:  accelerometer.deadY   = value; break;
                                case 9:  accelerometer.centerZ = value; break;
                                case 10: accelerometer.minZ    = value; break;
                                case 11: accelerometer.maxZ    = value; break;
                                case 12: accelerometer.deadZ   = value; break;
                            }
                        }
                    }
                }
                else if (component.StartsWith("irSqr"))
                {
                    SquareBoundry sBoundry = new SquareBoundry();
                    string[] sqrConfig = component.Split(new char[] { '|' });

                    for (int s = 1; s < sqrConfig.Length; s++)
                    {
                        int value = 0;
                        if (int.TryParse(sqrConfig[s], out value))
                        {
                            switch (s)
                            {
                                case 1: sBoundry.center_x = value; break;
                                case 2: sBoundry.center_y = value; break;
                                case 3: sBoundry.width = value; break;
                                case 4: sBoundry.height = value; break;
                            }
                        }
                    }

                    irSensor.boundingArea = sBoundry;
                }
                else if (component.StartsWith("irCir"))
                {
                    CircularBoundry sBoundry = new CircularBoundry();
                    string[] cirConfig = component.Split(new char[] { '|' });

                    for (int c = 1; c < cirConfig.Length; c++)
                    {
                        int value = 0;
                        if (int.TryParse(cirConfig[c], out value))
                        {
                            switch (c)
                            {
                                case 1: sBoundry.center_x = value; break;
                                case 2: sBoundry.center_y = value; break;
                                case 3: sBoundry.radius = value; break;
                            }
                        }
                    }

                    irSensor.boundingArea = sBoundry;
                }
            }
        }

        /// <summary>
        /// Creates a string containing the calibration settings for the Wiimote.
        /// String is in the following format 
        /// -wm:acc|centerX|minX|minY|deadX|centerY|[...]:ir
        /// </summary>
        /// <returns>String representing the Wiimote's calibration settings.</returns>
        public string GetCalibrationString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("-wm");
                sb.Append(":acc");
                    sb.Append("|"); sb.Append(accelerometer.centerX);
                    sb.Append("|"); sb.Append(accelerometer.minX);
                    sb.Append("|"); sb.Append(accelerometer.maxX);
                    sb.Append("|"); sb.Append(accelerometer.deadX);

                    sb.Append("|"); sb.Append(accelerometer.centerY);
                    sb.Append("|"); sb.Append(accelerometer.minY);
                    sb.Append("|"); sb.Append(accelerometer.maxY);
                    sb.Append("|"); sb.Append(accelerometer.deadY);

                    sb.Append("|"); sb.Append(accelerometer.centerZ);
                    sb.Append("|"); sb.Append(accelerometer.minZ);
                    sb.Append("|"); sb.Append(accelerometer.maxZ);
                    sb.Append("|"); sb.Append(accelerometer.deadZ);
                
            if (irSensor.boundingArea != null)
            {
                if (irSensor.boundingArea is SquareBoundry)
                {
                    SquareBoundry sqr = (SquareBoundry)irSensor.boundingArea;
                    sb.Append(":irSqr");
                        sb.Append("|"); sb.Append(sqr.center_x);
                        sb.Append("|"); sb.Append(sqr.center_y);
                        sb.Append("|"); sb.Append(sqr.width);
                        sb.Append("|"); sb.Append(sqr.height);
                }
                else if (irSensor.boundingArea is CircularBoundry)
                {
                    CircularBoundry cir = (CircularBoundry)irSensor.boundingArea;
                    sb.Append(":irCir");
                        sb.Append("|"); sb.Append(cir.center_x);
                        sb.Append("|"); sb.Append(cir.center_y);
                        sb.Append("|"); sb.Append(cir.radius);
                }
            }

            return sb.ToString();
        }

        public bool CalibrationEmpty
        {
            get 
            { 
                if (accelerometer.maxX == 0 && accelerometer.maxY == 0 && accelerometer.maxZ == 0)
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
            // Buttons
            yield return new KeyValuePair<string, float>(InputNames.PLUS, buttons.Plus ? 1.0f : 0.0f);
            yield return new KeyValuePair<string, float>(InputNames.MINUS, buttons.Minus ? 1.0f : 0.0f);
            yield return new KeyValuePair<string, float>(InputNames.HOME, buttons.Home ? 1.0f : 0.0f);

            yield return new KeyValuePair<string, float>(InputNames.A, buttons.A ? 1.0f : 0.0f);
            yield return new KeyValuePair<string, float>(InputNames.B, buttons.B ? 1.0f : 0.0f);
            yield return new KeyValuePair<string, float>(InputNames.ONE, buttons.One ? 1.0f : 0.0f);
            yield return new KeyValuePair<string, float>(InputNames.TWO, buttons.Two ? 1.0f : 0.0f);

            // D-Pad
            yield return new KeyValuePair<string, float>(InputNames.UP, buttons.Up ? 1.0f : 0.0f);
            yield return new KeyValuePair<string, float>(InputNames.DOWN, buttons.Down ? 1.0f : 0.0f);
            yield return new KeyValuePair<string, float>(InputNames.LEFT, buttons.Left ? 1.0f : 0.0f);
            yield return new KeyValuePair<string, float>(InputNames.RIGHT, buttons.Right ? 1.0f : 0.0f);

            // IR Sensor
            irSensor.Normalize();
            yield return new KeyValuePair<string, float>(InputNames.IR_X, irSensor.X);
            yield return new KeyValuePair<string, float>(InputNames.IR_Y, irSensor.Y);
            yield return new KeyValuePair<string, float>(InputNames.IR_UP, irSensor.Y > 0 ? irSensor.Y : 0);
            yield return new KeyValuePair<string, float>(InputNames.IR_DOWN, irSensor.Y > 0 ? -irSensor.Y : 0);
            yield return new KeyValuePair<string, float>(InputNames.IR_LEFT, irSensor.X < 0 ? -irSensor.X : 0);
            yield return new KeyValuePair<string, float>(InputNames.IR_RIGHT, irSensor.X > 0 ? irSensor.X : 0);

            // Accelerometer
            accelerometer.Normalize();
            yield return new KeyValuePair<string, float>(InputNames.ACC_X, accelerometer.X);
            yield return new KeyValuePair<string, float>(InputNames.ACC_Y, accelerometer.Y);
            yield return new KeyValuePair<string, float>(InputNames.ACC_Z, accelerometer.Z);
            yield return new KeyValuePair<string, float>(InputNames.TILT_LEFT, accelerometer.X < 0 ? -accelerometer.X : 0);
            yield return new KeyValuePair<string, float>(InputNames.TILT_RIGHT, accelerometer.X > 0 ? accelerometer.X : 0);
            yield return new KeyValuePair<string, float>(InputNames.TILT_UP, accelerometer.Y > 0 ? accelerometer.Y : 0);
            yield return new KeyValuePair<string, float>(InputNames.TILT_DOWN, accelerometer.Y < 0 ? -accelerometer.Y : 0);
            yield return new KeyValuePair<string, float>(InputNames.FACE_UP, accelerometer.Z > 0 ? accelerometer.Z : 0);
            yield return new KeyValuePair<string, float>(InputNames.FACE_DOWN, accelerometer.Z < 0 ? -accelerometer.Z : 0);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
