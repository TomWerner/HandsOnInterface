//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.BodyBasics
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using System.Runtime.InteropServices;
    using System.Windows.Shapes;
    using Microsoft.Samples.Kinect.BodyBasics.Gestures;

    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// Radius of drawn hand circles
        /// </summary>
        private const double HandSize = 30;

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
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodies = null;

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

        /// <summary>
        /// List of colors for each body tracked
        /// </summary>
        private List<Pen> bodyColors;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        private IntPtr window;
        private GestureListener waveGesture;

        private enum HandMouseStates
        {
            NONE,
            CURSOR,
            WINDOW_DRAG
        }
        private HandMouseStates HandMouseState = HandMouseStates.NONE;
        

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            // one sensor is currently supported
            this.kinectSensor = KinectSensor.GetDefault();

            // get the coordinate mapper
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            // get the depth (display) extents
            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // get size of joint space
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;

            // open the reader for the body frames
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

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

            // populate body colors, one for each BodyIndex
            this.bodyColors = new List<Pen>();

            this.bodyColors.Add(new Pen(Brushes.Red, 6));
            this.bodyColors.Add(new Pen(Brushes.Orange, 6));
            this.bodyColors.Add(new Pen(Brushes.Green, 6));
            this.bodyColors.Add(new Pen(Brushes.Blue, 6));
            this.bodyColors.Add(new Pen(Brushes.Indigo, 6));
            this.bodyColors.Add(new Pen(Brushes.Violet, 6));

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // use the window object as the view model in this simple example
            this.DataContext = this;

            // initialize the components (controls) of the window
            this.InitializeComponent();


            WaveSegment1 waveRightSegment1 = new WaveSegment1();
            WaveSegment2 waveRightSegment2 = new WaveSegment2();
            IGestureSegment[] wave = new IGestureSegment[]
            {
                waveRightSegment1,
                waveRightSegment2,
                waveRightSegment1,
                waveRightSegment2,
                waveRightSegment1,
                waveRightSegment2
            };

            waveGesture = new GestureListener(wave);
            waveGesture.GestureRecognized += Gesture_GestureRecognized;

            WindowDragStart dragSeg1 = new WindowDragStart();
            WindowDragMove dragSeg2 = new WindowDragMove();
            MouseMoveStart mouseSeg1 = new MouseMoveStart();
            MouseMove mouseSeg2 = new MouseMove();
            FinishedGesture finished = new FinishedGesture();
            IGestureSegment[] windowDrag = new IGestureSegment[]
            {
                dragSeg1,
                dragSeg2
            };
            IGestureSegment[] mouseMove = new IGestureSegment[]
            {
                mouseSeg1,
                mouseSeg2
            };
            IGestureSegment[] finishedSequence = new IGestureSegment[]
            {
                finished
            };

            windowDragGesture = new GestureListener(windowDrag);
            windowDragGesture.GestureRecognized += Gesture_DragMove;

            windowDragGestureFinish = new GestureListener(finishedSequence);
            windowDragGestureFinish.GestureRecognized += Gesture_DragFinish;

            mouseMoveGesture = new GestureListener(mouseMove);
            mouseMoveGesture.GestureRecognized += Gesture_MouseMove;

            mouseMoveGestureFinish = new GestureListener(finishedSequence);
            mouseMoveGestureFinish.GestureRecognized += Gesture_MouseMoveFinish;
        }

        public void Gesture_DragMove(object sender, EventArgs e)
        {
            HandMouseState = HandMouseStates.WINDOW_DRAG;
            WindowDragData.resetOldHand = true;
            window = Win32.GetForegroundWindow();
        }

        public void Gesture_DragFinish(object sender, EventArgs e)
        {
            if (HandMouseState == HandMouseStates.WINDOW_DRAG)
            {
                HandMouseState = HandMouseStates.NONE;
                WindowDragData.checkForFling = true;
            }
        }

        public void Gesture_MouseMove(object sender, EventArgs e)
        {
            HandMouseState = HandMouseStates.CURSOR;
            MouseMoveData.resetOldHand = true;
        }

        public void Gesture_MouseMoveFinish(object sender, EventArgs e)
        {
            HandMouseState = HandMouseStates.NONE;
        }

        public void Gesture_GestureRecognized(object sender, EventArgs e)
        {
            Console.WriteLine("You just waved!");
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        private GestureListener windowDragGesture;
        private GestureListener windowDragGestureFinish;
        private GestureListener mouseMoveGesture;
        private GestureListener mouseMoveGestureFinish;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.imageSource;
            }
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// Execute start up tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        /// <summary>
        /// Handles the body frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    // Draw a transparent background to set the render size
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                    int penIndex = 0;
                    foreach (Body body in this.bodies)
                    {
                        Pen drawPen = this.bodyColors[penIndex++];

                        if (body.IsTracked)
                        {
                            this.DrawClippedEdges(body, dc);

                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                            // convert the joint points to depth (display) space
                            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();
                            Dictionary<JointType, float> jointZs = new Dictionary<JointType, float>();

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
                                jointZs[jointType] = position.Z;
                            }

                            this.DrawBody(joints, jointPoints, dc, drawPen);

                            this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                            this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);

                            waveGesture.Update(body);
                            windowDragGesture.Update(body);
                            windowDragGestureFinish.Update(body);
                            mouseMoveGesture.Update(body);
                            mouseMoveGestureFinish.Update(body);

                            handMouseBehavior(body, jointPoints[JointType.HandLeft], jointPoints[JointType.HandRight],jointZs[JointType.HandLeft],jointZs[JointType.HandRight]);
                        }
                    }

                    // prevent drawing outside of our render area
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                }
            }
        }

        private void handMouseBehavior(Body body, Point leftHand, Point rightHand,float leftZ, float rightZ)
        {
            switch (HandMouseState)
            {
                case HandMouseStates.NONE:
                    if (WindowDragData.checkForFling)
                    {
                        checkForFling(body, leftHand, rightHand);
                    }

                    break;
                case HandMouseStates.CURSOR:
                    Point mousePoint;
                    float zCoordinate;
                    double armLength = calculateArmLength(body);
                    if (MouseMoveData.dragHand == JointType.HandLeft)
                    {
                        mousePoint = leftHand;
                        zCoordinate = leftZ;
                    }
                    else
                    {
                        mousePoint = rightHand;
                        zCoordinate = rightZ;
                    }

                    if (MouseMoveData.resetOldHand)
                    {
                        MouseMoveData.lastHandPoint = mousePoint;
                        MouseMoveData.lastHandZ = zCoordinate;
                        MouseMoveData.resetOldHand = false;
                    }
                    double dx = mousePoint.X - MouseMoveData.lastHandPoint.X;
                    double dy = mousePoint.Y - MouseMoveData.lastHandPoint.Y;
                    double dz = zCoordinate - MouseMoveData.lastHandZ;
                    MouseMoveData.lastHandPoint = mousePoint;
                    MouseMoveData.lastHandZ = zCoordinate;

                    if (dz > armLength / 15)
                    {
                        Win32.mouse_event((int)Win32.MouseEventFlags.LeftDown,0,0,0,0);
                    }
                    else
                    {
                        double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
                        double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;

                        double multiplier = Math.Max(screenHeight / armLength, screenWidth / armLength) / 500;
                        dx *= multiplier;
                        dy *= multiplier;

                        Win32.POINT lpPoint;
                        Win32.GetCursorPos(out lpPoint);
                        Win32.SetCursorPos(lpPoint.X + (int)dx, lpPoint.Y + (int)dy);
                    }
                    break;
                case HandMouseStates.WINDOW_DRAG:
                    handleWindowDragging(body, leftHand, rightHand);
                    break;
                default:
                    break;
            }
        }

        private void handleWindowDragging(Body body, Point leftHand, Point rightHand)
        {
            Point mousePoint;
            double armLength = calculateArmLength(body);
            if (WindowDragData.dragHand == JointType.HandLeft)
            {
                mousePoint = leftHand;
            }
            else
            {
                mousePoint = rightHand;
            }

            if (WindowDragData.resetOldHand)
            {
                WindowDragData.lastHandPoint = mousePoint;
                WindowDragData.resetOldHand = false;
            }
            double dx = mousePoint.X - WindowDragData.lastHandPoint.X;
            double dy = mousePoint.Y - WindowDragData.lastHandPoint.Y;
            WindowDragData.lastHandPoint = mousePoint;

            double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;

            double multiplier = Math.Max(screenHeight / armLength, screenWidth / armLength) / 500;
            dx *= multiplier;
            dy *= multiplier;

            Win32.RECT current;
            Win32.GetWindowRect(window, out current);
            Win32.SetWindowPos(window, new IntPtr(0), current.left + (int)dx, current.top + (int)dy, -1, -1, Win32.SetWindowPosFlags.SWP_NOSIZE);

                    


        }

        private void checkForFling(Body body, Point leftHand, Point rightHand)
        {
            WindowDragData.checkForFling = false;
            Point mousePoint;
            double armLength = calculateArmLength(body);
            if (WindowDragData.dragHand == JointType.HandLeft)
            {
                mousePoint = leftHand;
            }
            else
            {
                mousePoint = rightHand;
            }
            double dx2 = mousePoint.X - WindowDragData.lastHandPoint.X;
            double dy2 = mousePoint.Y - WindowDragData.lastHandPoint.Y;
            Rect workArea = System.Windows.SystemParameters.WorkArea;
            double totalChange = Math.Sqrt(Math.Pow(dx2, 2) + Math.Pow(dy2, 2));
            if (totalChange / armLength > 10)
            {
                Console.WriteLine("FLING" + (dx2 / totalChange));
                if (Math.Abs(dx2 / totalChange) > .5)
                {
                    if (dx2 > 0)
                    {
                        Win32.SetWindowPos(window, new IntPtr(0), (int)workArea.Width / 2, 0, (int)workArea.Width / 2, (int)workArea.Height, Win32.SetWindowPosFlags.SWP_SHOWWINDOW);
                    }
                    else
                    {
                        Win32.SetWindowPos(window, new IntPtr(0), 0, 0, (int)workArea.Width / 2, (int)workArea.Height, Win32.SetWindowPosFlags.SWP_SHOWWINDOW);
                    }
                }
                else
                {
                    if (dy2 < 0)
                    {
                        Win32.ShowWindow(window, Win32.SW_MAXIMIZE);
                    }
                    else
                    {
                        Win32.ShowWindow(window, Win32.SW_MINIMIZE);
                    }
                }
            }
        }

        private double calculateArmLength(Body body)
        {
            double armLength;
            if (WindowDragData.dragHand == JointType.HandLeft)
            {
                armLength = Math.Sqrt(Math.Pow(body.Joints[JointType.HandLeft].Position.X - body.Joints[JointType.ElbowLeft].Position.X, 2) +
                                       Math.Pow(body.Joints[JointType.HandLeft].Position.Y - body.Joints[JointType.ElbowLeft].Position.Y, 2) +
                                       Math.Pow(body.Joints[JointType.HandLeft].Position.Z - body.Joints[JointType.ElbowLeft].Position.Z, 2));
            }
            else
            {
                armLength = Math.Sqrt(Math.Pow(body.Joints[JointType.HandRight].Position.X - body.Joints[JointType.ElbowRight].Position.X, 2) +
                                       Math.Pow(body.Joints[JointType.HandRight].Position.Y - body.Joints[JointType.ElbowRight].Position.Y, 2) +
                                       Math.Pow(body.Joints[JointType.HandRight].Position.Z - body.Joints[JointType.ElbowRight].Position.Z, 2));
            }

            return armLength;
        }


        private void ProcessHandGestures(Body body, Joint left, Joint right, Joint head)
        {
            if (body.HandLeftState == HandState.Closed)
            {
                //int dx = (int)(rightPoint.X - lastRightPoint.X);
                //int dy = (int)(rightPoint.Y - lastRightPoint.Y);
                //Console.WriteLine(dx + ", " + dy);
                //Win32.POINT lpPoint;
                //Win32.GetCursorPos(out lpPoint);
                //Win32.SetCursorPos(lpPoint.X + dx, lpPoint.Y + dy);

                /*
                RECT current;
                GetWindowRect(window, out current);
                SetWindowPos(window, new IntPtr(0), current.left + dx, current.top + dy, -1, -1, SetWindowPosFlags.SWP_NOSIZE);
                 */
            }
        }

        /// <summary>
        /// Draws a body
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="drawingPen">specifies color to draw a specific body</param>
        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen)
        {
            
            // Draw the bones
            foreach (var bone in this.bones)
            {
                this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
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
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
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
        unsafe private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
        {
            switch (handState)
            {
                case HandState.Closed:
                    drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                    //ShowWindow(window, 6);

                    //this is to test I have a valid handle
                    //SetWindowPos(window, new IntPtr(0), 10, 10, 1024, 350, SetWindowPosFlags.SWP_DRAWFRAME);

                    //SendMessage(window, WM_VSCROLL, (IntPtr)SB_LINEDOWN, IntPtr.Zero);
                    
                    break;

                case HandState.Open:
                    drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                    //ShowWindow(window, 9);

                    //this is to test I have a valid handle
                    //SetWindowPos(window, new IntPtr(0), 200, 10, 1024, 350, SetWindowPosFlags.SWP_DRAWFRAME);

                    //SendMessage(window, WM_VSCROLL, (IntPtr)SB_LINEUP, IntPtr.Zero);
                    
                    break;

                case HandState.Lasso:
                    drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                    break;
            }
        }

        /// <summary>
        /// Draws indicators to show which edges are clipping body data
        /// </summary>
        /// <param name="body">body to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawClippedEdges(Body body, DrawingContext drawingContext)
        {
            FrameEdges clippedEdges = body.ClippedEdges;

            if (clippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, this.displayHeight - ClipBoundsThickness, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, this.displayHeight));
            }

            if (clippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(this.displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, this.displayHeight));
            }
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }
    }
}
