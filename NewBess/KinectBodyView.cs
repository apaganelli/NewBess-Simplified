using System;
using System.Collections.Generic;
using System.Windows.Media;

using Microsoft.Kinect;
using Microsoft.Kinect.Face;

using System.Windows;
using System.Globalization;
using System.Linq;
using System.Diagnostics;

namespace NewBess
{
    public class KinectBodyView : ObservableObject
    {
        private KinectSensor _sensor = null;

        /// <summary> Reader for body frames </summary>
        private BodyFrameReader _bodyFrameReader = null;

        /// <summary> Array for the bodies (Kinect will track up to 6 people simultaneously) </summary>
        private Body[] bodies = null;

        /// <summary>
        /// Radius of drawn hand circles
        /// </summary>
        private const double HandSize = 10;

        private const double CoMSize = 5;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as closed
        /// </summary>
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as opened
        /// </summary>
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as in lasso (pointer) position
        /// </summary>
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Drawing group for body rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// definition of bones
        /// </summary>
        private List<Tuple<JointType, JointType>> bones;

        /// <summary>
        /// Width of display (depth space)
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// Height of display (depth space)
        /// </summary>
        private int displayHeight;

        // Face recognition attributes and variables
        FaceFrameSource _faceSource = null;            
        FaceFrameReader _faceReader = null;
        FaceFrameResult _faceResult = null;

        ApplicationViewModel _app;

        CenterOfMass _CoM;

        float _normalization = 0;
        bool _firstTime;

        List<CameraSpacePoint> r1Joints;             // stores joint positions that characterize a gesture, reference values.
        List<CameraSpacePoint> m1Joints;             // store joint positions to check if they characterize a gesture, current values.

        List<CameraSpacePoint> r2Joints;             // stores joint positions that characterize a gesture.
        List<CameraSpacePoint> m2Joints;             // store joint positions to check if they characterize a gesture.

        List<CameraSpacePoint> r3Joints;             // stores joint positions that characterize a gesture.
        List<CameraSpacePoint> m3Joints;             // store joint positions to check if they characterize a gesture.

        List<CameraSpacePoint> r4Joints;             // stores joint positions that characterize a gesture.
        List<CameraSpacePoint> m4Joints;             // store joint positions to check if they characterize a gesture.

        List<CameraSpacePoint> r5Joints;                // stores head positions during calibration.
        CameraSpacePoint r5 = new CameraSpacePoint();   // stores mean of head position got from calibration. Used for checking trunk tilt angle.

        List<double> lmean1 = new List<double>();
        List<double> lmean2 = new List<double>();
        List<double> lmean3 = new List<double>();
        List<double> lmean4 = new List<double>();

        public Stopwatch TestStopWatch;

        public int NumFramesTest { get; set; }
        double mean1, mean2, mean3, mean4;

        string _moduleName;
        public string ModuleName
        {
            get { return _moduleName; }
            set
            {
                if(_moduleName != value) { _moduleName = value; }
            }
        }

        public KinectBodyView(ApplicationViewModel app, string name)
        {
            // Gets application pointer.
            _app = app;
            ModuleName = name;

            NumFramesTest = _app.NumFramesTest_DTW;

            mean1 = mean2 = mean3 = mean4 = 0;

            r1Joints = new List<CameraSpacePoint>();
            r2Joints = new List<CameraSpacePoint>();
            r3Joints = new List<CameraSpacePoint>();
            r4Joints = new List<CameraSpacePoint>();
            r5Joints = new List<CameraSpacePoint>();

            m1Joints = new List<CameraSpacePoint>();
            m2Joints = new List<CameraSpacePoint>();
            m3Joints = new List<CameraSpacePoint>();
            m4Joints = new List<CameraSpacePoint>();

            _CoM = new CenterOfMass(_app);

            TestStopWatch = new Stopwatch();

            // Gets Kinect sensor reference.
            _sensor = KinectSensor.GetDefault();

            // If there is an active kinect / of accessible studio library.
            if (_sensor != null)
            {
                // Opens the sensor.
                _sensor.Open();

                // open the reader for the body frames
                _bodyFrameReader = _sensor.BodyFrameSource.OpenReader();
                _bodyFrameReader.FrameArrived += this.Reader_BodyFrameArrived;

                // get the coordinate mapper
                this.coordinateMapper = _sensor.CoordinateMapper;

                // get the depth (display) extents
                FrameDescription frameDescription = _sensor.DepthFrameSource.FrameDescription;

                // get size of joint space
                this.displayWidth = frameDescription.Width;
                this.displayHeight = frameDescription.Height;

                _faceSource = new FaceFrameSource(_sensor, 0, FaceFrameFeatures.LeftEyeClosed | FaceFrameFeatures.RightEyeClosed);
                _faceReader = _faceSource.OpenReader();

                _faceReader.FrameArrived += FaceReader_FrameArrived;
            }

            // Sets flag for recording DoubleStance position references to false
            IsCalibrating = false;
            IsTesting = false;
            CreateBones();
        }

        private void FaceReader_FrameArrived(object sender, FaceFrameArrivedEventArgs e)
        {
            using(var frame = e.FrameReference.AcquireFrame())
            {
                if(frame != null)
                {
                    _faceResult = frame.FaceFrameResult;
                }
            }
        }

        /// <summary>
        /// Handles the body frame data arriving from the sensor and updates the associated gesture detector object for each body
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_BodyFrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            // Only process frames if current page is Pose Calibration or Test.
            if(! (_app.CurrentPageViewModel is PoseCalibrationViewModel))
            {
                return;
            } 

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        // creates an array of 6 bodies, which is the max number of bodies that Kinect can track simultaneously
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);

                    // visualize the new body data
                    this.UpdateBodyFrame(this.bodies);
                }
            }
        }

        int _PoseErrorCounter = 0;
        int _EyesErrorCounter = 0;
        int _NumFramesThresholdError = 9;                       // Number minimum of frames to count an error.        
        public int Delay { get; set; }                          // used for delaying the start of the tests        

        CameraSpacePoint p1 = new CameraSpacePoint();           // used for normalization calculation (tmp variable)
        CameraSpacePoint p2 = new CameraSpacePoint();           // used for normalization calculation (tmp variable)
        CameraSpacePoint r1 = new CameraSpacePoint();           // used for storing normalized value from monitored joint.
        CameraSpacePoint r2 = new CameraSpacePoint();
        CameraSpacePoint r3 = new CameraSpacePoint();
        CameraSpacePoint r4 = new CameraSpacePoint();

        /// <summary>
        /// Updates the body array with new information from the sensor
        /// Should be called whenever a new BodyFrameArrivedEvent occurs
        /// </summary>
        /// <param name="bodies">Array of bodies to update</param>
        public void UpdateBodyFrame(Body[] bodies)
        {
            PoseCalibrationViewModel DCVM = (PoseCalibrationViewModel)_app.CurrentPageViewModel;
         
            // Process delay time.
            if (Delay > 0)
            {
                DCVM.StatusText = "Delay ... " + Delay;
                Delay--;
            }
            else
            {
                if (Delay == 0)
                {
                    Delay--;
                    _firstTime = true;

                    if (IsCalibrating)
                    {
                        DCVM.StartCalibrationTime = DateTime.Now;
                        DCVM.StatusText = "Starting calibration.";
                        NumFramesTest = _app.NumFramesTest_DTW;
                        r1Joints.Clear();
                        r2Joints.Clear();
                        r3Joints.Clear();
                        r4Joints.Clear();
                        r5Joints.Clear();
                    }
                    else if (IsTesting)
                    {
                        DCVM.StartTestTime = DateTime.Now;
                        DCVM.StatusText = "Starting test.";
                        NumFramesTest = _app.NumFramesTest_DTW;
                        mean1 = mean2 = mean3 = mean4 = 0;
                        lmean1.Clear();
                        lmean2.Clear();
                        lmean3.Clear();
                        lmean4.Clear();
                        m1Joints.Clear();
                        m2Joints.Clear();
                        m3Joints.Clear();
                        m4Joints.Clear();
                        TestStopWatch.Restart();
                    }
                }
            }

            if (bodies != null)
            {
                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    // Draw a transparent background to set the render size
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                    foreach (Body body in bodies)
                    {
                        Pen drawPen = new Pen(Brushes.Red, 6);
                           
                        if (body.IsTracked)
                        {
                            _faceSource.TrackingId = body.TrackingId;
                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                            // Calculate Center of Mass using segment method.
                            CameraSpacePoint CoM = _CoM.CalculateCoM(joints);

                            if (CoM.Z < 0)
                            {
                                CoM.Z = InferredZPositionClamp;
                            }

                            DepthSpacePoint Location_CoM = coordinateMapper.MapCameraPointToDepthSpace(CoM);
                            Point CoM_Point = new Point(Location_CoM.X, Location_CoM.Y);

                            // convert the joint points to depth (display) space
                            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                            foreach (JointType jointType in joints.Keys)
                            {
                                // sometimes the depth(Z) of an inferred joint may show as negative
                                // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                                CameraSpacePoint position = joints[jointType].Position;

                                if (position.Z < 0)
                                {
                                    position.Z = InferredZPositionClamp;
                                }

                                DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                                jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                            }

                            // Calibration or Test.
                            if ((IsCalibrating || IsTesting) && Delay < 0)
                            {
                                // Normalize monitored joints based on the trunk height - Scaling
                                if (_firstTime)
                                {
                                    p1 = joints[JointType.SpineShoulder].Position;
                                    p2 = joints[JointType.SpineBase].Position;
                                    _normalization = ((float)Math.Sqrt(Math.Pow(p1.X - p2.X, 2) +
                                            Math.Pow(p1.Y - p2.Y, 2) + Math.Pow(p1.Z - p2.Z, 2)));
                                    _firstTime = false;
                                }

                                Normalize(ref r1, joints[JointType.HandLeft].Position, CoM, _normalization);
                                Normalize(ref r2, joints[JointType.HandRight].Position, CoM, _normalization);
                                Normalize(ref r3, joints[JointType.FootLeft].Position, CoM, _normalization);
                                Normalize(ref r4, joints[JointType.FootRight].Position, CoM, _normalization);

                                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                                //  C A L I B R A T I ON
                                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                                // Pose calibration, recording positions to get a reference of them when they are theoretically stable.
                                if (IsCalibrating)
                                {
                                    // Log Center of Mass position.
                                    double elapsed = (DateTime.Now - DCVM.StartCalibrationTime).TotalMilliseconds;

                                    SaveJoints3D(joints, elapsed.ToString("F0"), DCVM);

                                    DCVM.LogCalLines.Add("CoM:" + elapsed.ToString("F0") + ":" + CoM.X.ToString("N3") + ":" + CoM.Y.ToString("N3")
                                            + ":" + CoM.Z.ToString("N3"));

                                    // Get data series of the main joint positions for position.
                                    if (NumFramesTest > 0)
                                    {
                                        // Stores normalized positions.
                                        r1Joints.Add(r1);                                           // Hand left
                                        r2Joints.Add(r2);                                           // Hand right
                                        r3Joints.Add(r3);                                           // Foot left
                                        r4Joints.Add(r4);                                           // Foot right
                                        r5Joints.Add(joints[JointType.Head].Position);              // Head - check trunk angle.
                                        NumFramesTest--;
                                    }
                                    else if (NumFramesTest == 0)
                                    {                                                                                                                                  
                                        r5.X = r5Joints.Select(val => val.X).Average();             // Head position (mean)    
                                        r5.Y = r5Joints.Select(val => val.Y).Average();
                                        r5.Z = r5Joints.Select(val => val.Z).Average();
                                        NumFramesTest--;
                                    }
                                }

                                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                                // T E S T
                                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                                // Checks if user is in the right pose (balanced) or unbalanced
                                if (IsTesting)
                                {
                                    string timeStr = TestStopWatch.ElapsedMilliseconds.ToString("F0");
                                    DCVM.LogTestLines.Add("CoM:" + timeStr + ":" + CoM.X.ToString("N6") + ":" + CoM.Y.ToString("N6") + ":" + CoM.Z.ToString("N6"));

                                    // save all positions to have everything if needed.
                                    SaveJoints3D(joints, timeStr, DCVM);                                  

                                    // we are saving here the 2D positions that will be shown at screen, as well the CoM.
                                    if (_app.SaveJoints)
                                    {
                                        SaveJoints(joints, jointPoints, CoM_Point, CoM, DCVM);
                                    }

                                    if (NumFramesTest > 0)
                                    {
                                        m1Joints.Add(r1);
                                        m2Joints.Add(r2);
                                        m3Joints.Add(r3);
                                        m4Joints.Add(r4);
                                        NumFramesTest--;
                                    }
                                    else if (NumFramesTest == 0)
                                    {
                                        Compare(DCVM);
                                        NumFramesTest = _app.NumFramesTest_DTW;
                                    }

                                    // Get current positions of spineBase and Head.
                                    CameraSpacePoint spineBase = body.Joints[JointType.SpineBase].Position;
                                    CameraSpacePoint head = body.Joints[JointType.Head].Position;

                                    double currAngle = Util.ScalarProduct(r5, spineBase, head);
                                    bool tiltBodyOk = Util.ScalarProduct(r5, spineBase, head) <= _app.AnglePrecision;

                                    string status = tiltBodyOk ? "OK " : "NOK ";
                                    status += currAngle.ToString("N1");
                                    DCVM.TrunkSway = status;

                                    if(! tiltBodyOk)
                                    {
                                        if (_PoseErrorCounter > _NumFramesThresholdError)
                                        {
                                            _PoseErrorCounter = 0;
                                            DCVM.TotalErrors++;
                                            DCVM.LogTestLines.Add("JointError:" + timeStr + ":" + DCVM.TotalErrors);
                                        }
                                        else
                                        {
                                            _PoseErrorCounter++;
                                        }
                                    } else
                                    {
                                        _PoseErrorCounter = 0;
                                    }

                                }   // end execute double stance test - analyze posture.
                            }

                            // Text to show eyes status on drawing space.
                            string faceText = "EYES- ";

                            // Only analyze eyes if it has a tracked faced.
                            if (_faceResult != null)
                            {
                                string timeStr = TestStopWatch.ElapsedMilliseconds.ToString("F0"); 
                                var eyeLeftClosed = _faceResult.FaceProperties[FaceProperty.LeftEyeClosed];
                                var eyeRightClosed = _faceResult.FaceProperties[FaceProperty.RightEyeClosed];

                                if (eyeLeftClosed == DetectionResult.No || eyeRightClosed == DetectionResult.No)
                                {
                                    if (eyeLeftClosed == DetectionResult.No) faceText += "LEFT ";
                                    if (eyeRightClosed == DetectionResult.No) faceText += "RIGHT ";
                                    faceText += "OPEN";

                                    if (IsTesting && Delay < 0)
                                    {
                                        DCVM.LogTestLines.Add("EYES:" + timeStr + ":" + faceText);

                                        if (_EyesErrorCounter > _NumFramesThresholdError)
                                        {
                                            DCVM.TotalErrors++;
                                            DCVM.StatusText = "Eye(s) opened error";
                                            DCVM.LogTestLines.Add("EyesError:" + timeStr + ":" + DCVM.TotalErrors);
                                            _EyesErrorCounter = 0;
                                        }
                                        else
                                        {
                                            _EyesErrorCounter++;
                                        }
                                    }
                                }
                                else
                                {
                                    faceText += "CLOSED";

                                    if (IsTesting && Delay < 0)
                                    {
                                        DCVM.LogTestLines.Add("EYES:" + timeStr + ":" + faceText);
                                    }

                                    _EyesErrorCounter = 0;
                                }

                                dc.DrawText(new FormattedText(faceText, CultureInfo.GetCultureInfo("en-us"),
                                            FlowDirection.LeftToRight, new Typeface("Georgia"), 25, Brushes.White), new Point(10, 10));
                            }

                            this.DrawBody(joints, jointPoints, dc, drawPen, true);
                            // this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                            // this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);
                            dc.DrawEllipse(Brushes.White, null, CoM_Point, CoMSize, CoMSize);
                        }
                    }

                    // prevent drawing outside of our render area
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                }  
            }
        }

        /// <summary>
        /// Comparison between current window of frames and calibration frames using DTW for each tracked joint.
        /// </summary>
        /// <param name="DCVM">A pointer to calling class. Used for updating status texts/values</param>
        private void Compare(PoseCalibrationViewModel DCVM)
        {
            bool err1, err2, err3, err4 = false;

            string txt = "";
            err1 = ProcessDTW(ref txt, r1Joints.ToArray(),  m1Joints.ToArray(), ref lmean1, ref mean1);
            DCVM.LeftHand = txt;

            txt = "";
            err2 = ProcessDTW(ref txt, r2Joints.ToArray(), m2Joints.ToArray(), ref lmean2, ref mean2);
            DCVM.RightHand = txt;

            txt = "";
            err3 = ProcessDTW(ref txt, r3Joints.ToArray(), m3Joints.ToArray(), ref lmean3, ref mean3);
            DCVM.LeftFoot = txt;

            txt = "";
            err4 = ProcessDTW(ref txt, r4Joints.ToArray(), m4Joints.ToArray(), ref lmean4, ref mean4);
            DCVM.RightFoot = txt;

            // In general, when someone stumble on the board, there is an error in hands position as well.
            // Then, it doesn´s matter exactly where the error (loss of stability) was or how many joints it had affected,
            // it counts only 1 error.
            if (err1 || err2 || err3 || err4)
            {               
                DCVM.TotalErrors++;
                DCVM.StatusText = "Test total errors by now: " + DCVM.TotalErrors;
            }

            m1Joints.Clear();
            m2Joints.Clear();
            m3Joints.Clear();
            m4Joints.Clear();
        }

        
        /// <summary>
        /// Receives data series of joint positions and checks using DTW against calibration series.
        /// If DTW minimal cost is above 3 times the updated mean of last minutes series, returns false (error), otherwise (true). 
        /// </summary>
        /// <param name="label">Feedback message.</param>
        /// <param name="calJoints">Data serie of calibrated joint</param>
        /// <param name="testJoints">Current data serie of the joint to be checked</param>
        /// <param name="lmean">Accummulated mean values of last data series of the joint</param>
        /// <param name="mean">Current mean value that is going to be used to check DTW distance is acceptable.</param>
        /// <returns></returns>
        private bool ProcessDTW(ref string label, CameraSpacePoint[] calJoints, CameraSpacePoint[] testJoints, ref List<double> lmean, ref double mean)
        {
            bool err = false;
            DTW dtw = new DTW(calJoints, testJoints);

            if (mean == 0) mean = dtw.minCost;

            label = dtw.minCost.ToString("N1") + " / " + mean.ToString("N1");

            if (dtw.minCost < 3 * mean)
            {
                lmean.Add(dtw.minCost);
            }
            else
            {
                lmean.Add(mean * 2);
                err = true;
            }

            mean = lmean.Average();

            return err;
        }

        /// <summary>
        /// Normalize joint position (x, y, z) using the height of the trunk for scaling and distance from center.
        /// </summary>
        /// <param name="p">returned normalized point</param>
        /// <param name="c">captured joint position to be normalized</param>
        /// <param name="p_ref">central point of the body</param>
        /// <param name="n">value of normalize scale</param>
        private void Normalize(ref CameraSpacePoint p, CameraSpacePoint c, CameraSpacePoint p_ref, float n)
        {
            p = c;
            p.X = (p.X - p_ref.X) / n;
            p.Y = (p.Y - p_ref.Y) / n;
            p.Z = (p.Z - p_ref.Z) / n;
        }

        /// <summary>
        /// Real joint positions
        /// </summary>
        /// <param name="joints">Dictionary with all joint types as keys and joint structure as values</param>
        /// <param name="time">Current time in milliseconds (string).</param>
        /// <param name="DCVM">Pointer to calling class.</param>
        private void SaveJoints3D(IReadOnlyDictionary<JointType, Joint> joints, string time, PoseCalibrationViewModel DCVM)
        {
            string line = time ;
            foreach (JointType jointType in joints.Keys)
            {
                line += ":" + (int)jointType + ":";

                if (joints[jointType].TrackingState == TrackingState.Tracked) line += "T" + ":";
                else if (joints[jointType].TrackingState == TrackingState.Inferred) line += "I" + ":";
                else line += "N" + ":";

                line += joints[jointType].Position.X.ToString("N5") + ":" + joints[jointType].Position.Y.ToString("N5") + ":" + joints[jointType].Position.Z.ToString("N5");
            }

            if (IsTesting)
            {
                DCVM.LogJoints3D.Add(line);
            } else
            {
                DCVM.LogJoints3DCal.Add(line);
            }
        }

        /// <summary>
        /// How to show the joints on screen.
        /// </summary>
        /// <param name="joints"></param>
        /// <param name="jointPoints"></param>
        /// <param name="CoM_Point"></param>
        /// <param name="CoM"></param>
        /// <param name="DCVM"></param>
        private void SaveJoints(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, Point CoM_Point, CameraSpacePoint CoM, PoseCalibrationViewModel DCVM)
        {
            string line = "";
            foreach (JointType jointType in joints.Keys)
            {
                line +=  (int)jointType + ":";
                line += jointPoints[jointType].X.ToString("N5") + ":" + jointPoints[jointType].Y.ToString("N5") + ":";
            }

            // CoP_X and CoP_Y represents centre of pressure on the WBB platform X (M-L variation) Y (A-P variation).
            // CoM represents the points on real space related to center of camera.
            // Point in the depth space where to show it 2D.
            // 3D position in camera space related to center of lens.
            line += "COM_X:" + CoM_Point.X.ToString("N5") + ":COM_Y:" + CoM_Point.Y.ToString("N5");
            line += ":" + CoM.X.ToString("N5") + ":" + CoM.Y.ToString("N5") + ":" + CoM.Z.ToString("N5");
            line += ":CoP_X:" + DCVM.CalculatedCoPX.ToString("N5") + ":CoP_Y:" + DCVM.CalculatedCoPY.ToString("N5");
            
            DCVM.LogJoints2D.Add(line);            
        }

        /// <summary>
        /// Draws a body
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="drawingPen">specifies color to draw a specific body</param>
        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen, bool trackState)
        {
            // Draw the bones
            foreach (var bone in this.bones)
            {
                this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen, trackState);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Draws one bone of a body (joint to joint)
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="jointType0">first joint of bone to draw</param>
        /// <param name="jointType1">second joint of bone to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// /// <param name="drawingPen">specifies color to draw a specific bone</param>
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen, bool track)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (track && (
                joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked))
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        /// <summary>
        /// Draws a hand symbol if the hand is tracked: red circle = closed, green circle = opened; blue circle = lasso
        /// </summary>
        /// <param name="handState">state of the hand</param>
        /// <param name="handPosition">position of the hand</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
        {
            switch (handState)
            {
                case HandState.Closed:
                    drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                    break;
                case HandState.Open:
                    drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                    break;
                case HandState.Lasso:
                    drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                    break;
            }
        }

        /// <summary>
        /// Create drawing group and body colors, and body segments.
        /// </summary>
        public void CreateBones()
        {
            // a bone defined as a line between two joints
            this.bones = new List<Tuple<JointType, JointType>>();

            // Torso
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));
            // Left Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);
        }

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get { return this.imageSource; }
        }

#region Properties
        public bool IsCalibrating { get; set; }
        public bool IsTesting { get; set; }
#endregion
    }
}
