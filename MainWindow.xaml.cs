﻿//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.HackISUName
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
    using Microsoft.Samples.Kinect.HackISUName.Gestures;
    using Microsoft.Speech.AudioFormat;
    using Microsoft.Speech.Recognition;
    using Microsoft.Samples.Kinect.SpeechBasics;
    using System.Text;
    using System.Threading;

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
        private GestureListener knockGesture;
        private GestureListener knockPullGesture;
        private GestureListener slapGesture;
        private GestureListener pokeGesture;

        /// <summary>
        /// Stream for 32b-16b conversion.
        /// </summary>
        private KinectAudioStream convertStream = null;

        /// <summary>
        /// Speech recognition engine using audio data from Kinect.
        /// </summary>
        private SpeechRecognitionEngine speechEngine = null;


        private enum HandMouseStates
        {
            NONE,
            CURSOR,
            WINDOW_DRAG,
            SCROLL_UP,
            SCROLL_DOWN,
            VOLUME_UP,
            VOLUME_DOWN
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

            // grab the audio stream
            IReadOnlyList<AudioBeam> audioBeamList = this.kinectSensor.AudioSource.AudioBeams;
            System.IO.Stream audioStream = audioBeamList[0].OpenInputStream();

            // create the convert stream
            this.convertStream = new KinectAudioStream(audioStream);

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            RecognizerInfo ri = TryGetKinectRecognizer();

            if (null != ri)
            {
                this.speechEngine = new SpeechRecognitionEngine(ri.Id);

                //var directions = new Choices();
                //directions.Add(new SemanticResultValue("hide", "hide"));
                //directions.Add(new SemanticResultValue("minimize", "minimize"));
                //directions.Add(new SemanticResultValue("maximize", "maximize"));
                //directions.Add(new SemanticResultValue("snap left", "snap left"));
                //directions.Add(new SemanticResultValue("snap right", "snap right"));
                //directions.Add(new SemanticResultValue("front", "front"));
                //directions.Add(new SemanticResultValue("drag", "drag"));
                //directions.Add(new SemanticResultValue("get windows", "get windows"));

                // Grammar for snapping
                GrammarBuilder snap = new GrammarBuilder { Culture = ri.Culture };
                // Any window
                snap.Append(new Choices("snap"));
                snap.Append(new Choices("Spotify", "Genie", "Chrome", "Media Player", "Visual Studio", "Github", "Eclipse", "Word", "Notepad"));
                snap.Append(new Choices("left", "right", "down", "up"));
                var g = new Grammar(snap);
                this.speechEngine.LoadGrammar(g);


                GrammarBuilder snap2 = new GrammarBuilder { Culture = ri.Culture };
                snap2.Append(new Choices("snap"));
                snap2.AppendWildcard();
                snap2.Append(new Choices("left", "right", "down", "up"));
                var g4 = new Grammar(snap2);
                this.speechEngine.LoadGrammar(g4);


                GrammarBuilder grab1 = new GrammarBuilder { Culture = ri.Culture };
                grab1.Append(new Choices("grab"));
                grab1.Append(new Choices("Spotify", "Genie", "Chrome", "Media Player", "Visual Studio", "Github", "Eclipse", "Word", "Notepad"));
                var g1 = new Grammar(grab1);
                this.speechEngine.LoadGrammar(g1);


                GrammarBuilder drag1 = new GrammarBuilder { Culture = ri.Culture };
                drag1.Append(new Choices("grab"));
                drag1.Append(new Choices("Spotify", "Genie", "Chrome", "Media Player", "Visual Studio", "Github", "Eclipse", "Word", "Notepad"));
                var d1 = new Grammar(drag1);
                this.speechEngine.LoadGrammar(d1);

                GrammarBuilder grab2 = new GrammarBuilder { Culture = ri.Culture };
                // Any window
                grab2.Append(new Choices("grab"));
                var g2 = new Grammar(grab2);
                this.speechEngine.LoadGrammar(g2);

                GrammarBuilder dropit = new GrammarBuilder { Culture = ri.Culture };
                // Any window
                dropit.Append(new Choices("drop it"));
                var drop = new Grammar(dropit);
                this.speechEngine.LoadGrammar(drop);

                GrammarBuilder mouse = new GrammarBuilder { Culture = ri.Culture };
                // Any window
                mouse.Append(new Choices("mouse mode"));
                var mg = new Grammar(mouse);
                this.speechEngine.LoadGrammar(mg);



                GrammarBuilder click = new GrammarBuilder { Culture = ri.Culture };
                click.Append(new Choices("click", "double click", "right click"));
                var clickGram = new Grammar(click);
                this.speechEngine.LoadGrammar(clickGram);

                GrammarBuilder go = new GrammarBuilder { Culture = ri.Culture };
                go.Append(new Choices("lets hack", "shut it down"));
                var goGram = new Grammar(go);
                this.speechEngine.LoadGrammar(goGram);







                this.speechEngine.SpeechRecognized += this.SpeechRecognized;
                this.speechEngine.SpeechRecognitionRejected += this.SpeechRejected;

                // let the convertStream know speech is going active
                this.convertStream.SpeechActive = true;

                // For long recognition sessions (a few hours or more), it may be beneficial to turn off adaptation of the acoustic model. 
                // This will prevent recognition accuracy from degrading over time.
                speechEngine.UpdateRecognizerSetting("AdaptationOn", 0);

                this.speechEngine.SetInputToAudioStream(
                    this.convertStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                this.speechEngine.RecognizeAsync(RecognizeMode.Multiple);
            }
            else
            {
                this.StatusText = "No recognizer";
            }

            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // use the window object as the view model in this simple example
            this.DataContext = this;

            // initialize the components (controls) of the window
            this.InitializeComponent();

            KnockSegment1 knockSegment1 = new KnockSegment1();
            KnockSegment2 knockSegment2 = new KnockSegment2();
            KnockSegment3 knockSegment3 = new KnockSegment3();
            SlapSegment1 slapSegment1 = new SlapSegment1();
            SlapSegment2 slapSegment2 = new SlapSegment2();
            PokeSegment1 pokeSegment1 = new PokeSegment1();
            PokeSegment2 pokeSegment2 = new PokeSegment2();

            IGestureSegment[] knock = new IGestureSegment[]
            {
                knockSegment1,
                knockSegment2
            };
            IGestureSegment[] knockPull = new IGestureSegment[]
            {
                knockSegment1,
                knockSegment3
            };
            IGestureSegment[] slap = new IGestureSegment[]
            {
                slapSegment1,
                slapSegment2
            };
            IGestureSegment[] poke = new IGestureSegment[]
            {
                pokeSegment1,
                pokeSegment2
            };

            knockGesture = new GestureListener(knock);
            knockGesture.GestureRecognized += Gesture_KnockRecognized;
            knockPullGesture = new GestureListener(knockPull);
            knockPullGesture.GestureRecognized += Gesture_KnockPullRecognized;
            slapGesture = new GestureListener(slap);
            slapGesture.GestureRecognized += Gesture_SlapRecognized;
            pokeGesture = new GestureListener(poke);
            pokeGesture.GestureRecognized += Gesture_PokeRecognized;

            WindowDragStart dragSeg1 = new WindowDragStart();
            WindowDragMove dragSeg2 = new WindowDragMove();
            MouseMoveStart mouseSeg1 = new MouseMoveStart();
            ScrollUpStart scrollUpSeg1 = new ScrollUpStart();
            ScrollDownStart scrollUpSeg2 = new ScrollDownStart();
            VolumeUpStart volumeUpStart = new VolumeUpStart();
            VolumeDownStart volumeDownStart = new VolumeDownStart();
            PausePlaySegment1 pausePlaySeg1 = new PausePlaySegment1();
            PausePlaySegment2 pausePlaySeg2 = new PausePlaySegment2();
            ShowAllStart showSeg1 = new ShowAllStart();
            HideAllStart showSeg2 = new HideAllStart();
            MouseMove mouseSeg2 = new MouseMove();
            DragFinishedGesture dragFinished = new DragFinishedGesture();
            VolumeFinishGesture volumeFinished = new VolumeFinishGesture();
            ScrollFinishedGesture scrollFinished = new ScrollFinishedGesture();
            MouseFinishedGesture mouseFinished = new MouseFinishedGesture();
            
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
            IGestureSegment[] dragFinishedSequence = new IGestureSegment[]
            {
                dragFinished
            };
            IGestureSegment[] volumeFinishedSequence = new IGestureSegment[]
            {
                volumeFinished
            };
            IGestureSegment[] scrollFinishedSequence = new IGestureSegment[]
            {
                scrollFinished
            };
            IGestureSegment[] mouseFinishedSequence = new IGestureSegment[]
            {
                mouseFinished
            };

            IGestureSegment[] scrollUp = new IGestureSegment[]
            {
                scrollUpSeg1,
                scrollUpSeg2
            };
            IGestureSegment[] scrollDown = new IGestureSegment[]
            {
                scrollUpSeg2,
                scrollUpSeg1
            };

            IGestureSegment[] volumeUp = new IGestureSegment[]
            {
                volumeUpStart,
                volumeDownStart
            };
            IGestureSegment[] volumeDown = new IGestureSegment[]
            {
                volumeDownStart,
                volumeUpStart
            };
            IGestureSegment[] pausePlay = new IGestureSegment[]
            {
                pausePlaySeg1,
                pausePlaySeg2
            };

            IGestureSegment[] bringUp = new IGestureSegment[]
            {
                showSeg1,
                showSeg2
            };
            IGestureSegment[] bringDown = new IGestureSegment[]
            {
                showSeg2,
                showSeg1
            };

            windowDragGesture = new GestureListener(windowDrag);
            windowDragGesture.GestureRecognized += Gesture_DragMove;

            windowDragGestureFinish = new GestureListener(dragFinishedSequence);
            windowDragGestureFinish.GestureRecognized += Gesture_DragFinish;

            mouseMoveGesture = new GestureListener(mouseMove);
            mouseMoveGesture.GestureRecognized += Gesture_MouseMove;

            mouseMoveGestureFinish = new GestureListener(mouseFinishedSequence);
            mouseMoveGestureFinish.GestureRecognized += Gesture_MouseMoveFinish;

            scrollUpGesture = new GestureListener(scrollUp);
            scrollUpGesture.GestureRecognized += Gesture_ScrollUp;

            scrollDownGesture = new GestureListener(scrollDown);
            scrollDownGesture.GestureRecognized += Gesture_ScrollDown;

            scrollGestureFinish = new GestureListener(scrollFinishedSequence);
            scrollGestureFinish.GestureRecognized += Gesture_ScrollFinish;

            volumeUpGesture = new GestureListener(volumeUp);
            volumeUpGesture.GestureRecognized += Gesture_VolumeUp;

            volumeDownGesture = new GestureListener(volumeDown);
            volumeDownGesture.GestureRecognized += Gesture_VolumeDown;

            volumeGestureFinish = new GestureListener(volumeFinishedSequence);
            volumeGestureFinish.GestureRecognized += Gesture_VolumeFinish;

            pausePlayGesture = new GestureListener(pausePlay);
            pausePlayGesture.GestureRecognized += Gesture_PausePlay;
            showAllGesture = new GestureListener(bringUp);
            showAllGesture.GestureRecognized += Gesture_ShowAll;

            hideAllGesture = new GestureListener(bringDown);
            hideAllGesture.GestureRecognized += Gesture_HideAll;
        }

        private void Gesture_HideAll(object sender, EventArgs e)
        {
            Process[] processlist = Process.GetProcesses();
            for (int i = 0; i < processlist.Length; i++)
            {
                if (!String.IsNullOrEmpty(processlist[i].MainWindowTitle))
                {
                    window = Win32.GetForegroundWindow();
                    Win32.ShowWindow(window, Win32.SW_MINIMIZE);
                    Thread.Sleep(100);
                }
            }
        }

        private void Gesture_ShowAll(object sender, EventArgs e)
        {
            Process[] processlist = Process.GetProcesses();
            for (int i = 0; i < processlist.Length; i++)
            {
                if (!String.IsNullOrEmpty(processlist[i].MainWindowTitle))
                {
                    window = processlist[i].MainWindowHandle;
                    Win32.ShowWindow(window, Win32.SW_RESTORE);
                    Thread.Sleep(100);
                }
            }
        }

        private void Gesture_ScrollFinish(object sender, EventArgs e)
        {
            if (HandMouseState == HandMouseStates.SCROLL_DOWN || HandMouseState == HandMouseStates.SCROLL_UP)
            {
                HandMouseState = HandMouseStates.NONE;
                Win32.SendMessage(window, Win32.WM_VSCROLL, (IntPtr)Win32.SB_ENDSCROLL, IntPtr.Zero);
            }
        }

        private void Gesture_ScrollUp(object sender, EventArgs e)
        {
            HandMouseState = HandMouseStates.SCROLL_UP;
            Console.WriteLine("Trying to scroll");
        }

        private void Gesture_ScrollDown(object sender, EventArgs e)
        {
            HandMouseState = HandMouseStates.SCROLL_DOWN;
            Console.WriteLine("Trying to scroll down");
        }

        private void Gesture_VolumeFinish(object sender, EventArgs e)
        {
            if (HandMouseState == HandMouseStates.VOLUME_DOWN || HandMouseState == HandMouseStates.VOLUME_UP)
            {
                HandMouseState = HandMouseStates.NONE;
                Win32.keybd_event(Win32.VK_VOLUME_UP, 0, Win32.KEYEVENTF_KEYUP, 0);
                Win32.keybd_event(Win32.VK_VOLUME_DOWN, 0, Win32.KEYEVENTF_KEYUP, 0);
            }
        }

        private void Gesture_VolumeUp(object sender, EventArgs e)
        {
            HandMouseState = HandMouseStates.VOLUME_UP;
        }

        private void Gesture_VolumeDown(object sender, EventArgs e)
        {
            HandMouseState = HandMouseStates.VOLUME_DOWN;
        }

        private void Gesture_PausePlay(object sender, EventArgs e)
        {
            //In case user is using closed left hand to drag windows; conflicting commands or accidental clicks when right hand is cursor
            if ((HandMouseState != HandMouseStates.WINDOW_DRAG || WindowDragData.dragHand != JointType.HandLeft) && HandMouseState != HandMouseStates.CURSOR)
            {
                Win32.keybd_event(Win32.VK_VOLUME_MUTE, 0, 0, 0);
            }
        }

        public void Gesture_DragMove(object sender, EventArgs e)
        {
            HandMouseState = HandMouseStates.WINDOW_DRAG;
            WindowDragData.resetOldHand = true;
            window = Win32.GetForegroundWindow();
            physWindow = new PhysWindow(window);
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

        public void Gesture_KnockRecognized(object sender, EventArgs e)
        {
            Win32.mouse_event((int)Win32.MouseEventFlags.LeftDown, 0, 0, 0, 0);
        }

        public void Gesture_KnockPullRecognized(object sender, EventArgs e)
        {
            Win32.mouse_event((int)Win32.MouseEventFlags.LeftUp, 0, 0, 0, 0);
        }

        public void Gesture_SlapRecognized(object sender, EventArgs e)
        {
            Win32.mouse_event((int)Win32.MouseEventFlags.RightDown, 0, 0, 0, 0);
            Win32.mouse_event((int)Win32.MouseEventFlags.RightUp, 0, 0, 0, 0);
        }

        public void Gesture_PokeRecognized(object sender, EventArgs e)
        {
            Win32.mouse_event((int)Win32.MouseEventFlags.LeftDown, 0, 0, 0, 0);
            Win32.mouse_event((int)Win32.MouseEventFlags.LeftUp, 0, 0, 0, 0);
            Win32.mouse_event((int)Win32.MouseEventFlags.LeftDown, 0, 0, 0, 0);
            Win32.mouse_event((int)Win32.MouseEventFlags.LeftUp, 0, 0, 0, 0);
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        private GestureListener windowDragGesture;
        private GestureListener windowDragGestureFinish;
        private GestureListener mouseMoveGesture;
        private GestureListener mouseMoveGestureFinish;
        private Dictionary<string, IntPtr> processNameMap;
        private PhysWindow physWindow;
        private GestureListener scrollUpGesture;
        private GestureListener scrollGestureFinish;
        private GestureListener scrollDownGesture;
        private GestureListener volumeUpGesture;
        private GestureListener volumeGestureFinish;
        private GestureListener volumeDownGesture;
        private GestureListener pausePlayGesture;
        private GestureListener showAllGesture;
        private GestureListener hideAllGesture;

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
                    Body body = null;
                    double zDist = double.MaxValue;
                    foreach (Body b in this.bodies)
                    {
                        Console.WriteLine(b.Joints[JointType.Head].Position.Z + " : " + zDist);
                        if (b.Joints[JointType.Head].Position.Z < zDist && b.Joints[JointType.Head].Position.Z != 0)
                        {
                            zDist = b.Joints[JointType.Head].Position.Z;
                            body = b;
                        }
                    }
                    Console.WriteLine("Body: " + body);
                    
                    if (body != null)
                    {
                        Pen drawPen = this.bodyColors[penIndex++];

                        if (body.IsTracked)
                        {
                            Console.WriteLine(body.TrackingId);
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

                            knockGesture.Update(body);
                            knockPullGesture.Update(body);
                            slapGesture.Update(body);
                            pokeGesture.Update(body);
                            windowDragGesture.Update(body);
                            windowDragGestureFinish.Update(body);
                            mouseMoveGesture.Update(body);
                            mouseMoveGestureFinish.Update(body);
                            scrollUpGesture.Update(body);
                            scrollDownGesture.Update(body);
                            scrollGestureFinish.Update(body);
                            volumeUpGesture.Update(body);
                            volumeDownGesture.Update(body);
                            volumeGestureFinish.Update(body);
                            pausePlayGesture.Update(body);
                            showAllGesture.Update(body);
                            hideAllGesture.Update(body);

                            handMouseBehavior(body, jointPoints[JointType.HandLeft], jointPoints[JointType.HandRight],jointZs[JointType.HandLeft],jointZs[JointType.HandRight]);
                        }
                    }

                    // prevent drawing outside of our render area
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                }
            }
        }

        private int scrollCounter = 0;
        private int volumeCounter = 0;
        private void handMouseBehavior(Body body, Point leftHand, Point rightHand,float leftZ, float rightZ)
        {
            if (physWindow != null)
            {
                physWindow.update();
            }
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
                        MouseMoveData.signalHand = JointType.HandRight;
                        mousePoint = leftHand;
                        zCoordinate = leftZ;
                    }
                    else
                    {
                        MouseMoveData.signalHand = JointType.HandLeft;
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
                case HandMouseStates.SCROLL_UP:
                    scrollCounter++;

                    double maxSpeed = Math.Abs(body.Joints[JointType.ShoulderRight].Position.Y - body.Joints[JointType.HipRight].Position.Y);
                    double dist = Math.Min(maxSpeed, Math.Abs(body.Joints[JointType.ShoulderRight].Position.Y - body.Joints[JointType.HandRight].Position.Y));

                    int delay = (int)((1 - dist / maxSpeed) * 5) + 1;

                    if (scrollCounter % delay == 0)
                    {
                        Win32.SendMessage(window, Win32.WM_VSCROLL, (IntPtr)Win32.SB_LINEUP, IntPtr.Zero);
                        Console.WriteLine("Sending line up");
                    }

                    break;
                case HandMouseStates.SCROLL_DOWN:
                    scrollCounter++;

                    double maxSpeed2 = Math.Abs(body.Joints[JointType.ShoulderRight].Position.Y - body.Joints[JointType.HipRight].Position.Y);
                    double dist2 = Math.Min(maxSpeed2, Math.Abs(body.Joints[JointType.ShoulderRight].Position.Y - body.Joints[JointType.HandRight].Position.Y));

                    int delay2 = (int)((1 - dist2 / maxSpeed2) * 5) + 1;

                    if (scrollCounter % delay2 == 0)
                    {
                        Win32.SendMessage(window, Win32.WM_VSCROLL, (IntPtr)Win32.SB_LINEDOWN, IntPtr.Zero);
                        Console.WriteLine("Sending line down");
                    }
                    break;
                case HandMouseStates.VOLUME_UP:
                    volumeCounter++;

                    double maxSpeed3 = Math.Abs(body.Joints[JointType.ShoulderLeft].Position.Y - body.Joints[JointType.HipLeft].Position.Y);
                    double dist3 = Math.Min(maxSpeed3, Math.Abs(body.Joints[JointType.ShoulderLeft].Position.Y - body.Joints[JointType.HandLeft].Position.Y));

                    int delay3 = (int)((1 - dist3 / maxSpeed3) * 4) + 1;

                    if (scrollCounter % delay3 == 0)
                    {
                        Win32.keybd_event(Win32.VK_VOLUME_UP, 0, 0, 0);
                    }
                    break;
                case HandMouseStates.VOLUME_DOWN:
                    volumeCounter++;

                    double maxSpeed4 = Math.Abs(body.Joints[JointType.ShoulderLeft].Position.Y - body.Joints[JointType.HipLeft].Position.Y);
                    double dist4 = Math.Min(maxSpeed4, Math.Abs(body.Joints[JointType.ShoulderLeft].Position.Y - body.Joints[JointType.HandLeft].Position.Y));

                    int delay4 = (int)((1 - dist4 / maxSpeed4) * 8) + 1;

                    if (scrollCounter % delay4 == 0)
                    {
                        Win32.keybd_event(Win32.VK_VOLUME_DOWN, 0, 0, 0);
                    }
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

            if (physWindow != null)
            {
                Point goal = new Point(physWindow.topLeft.X + dx, physWindow.topLeft.Y + dy);
                physWindow.setPoint(goal);
                physWindow.update();
            }
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
            double dx = mousePoint.X - WindowDragData.lastHandPoint.X;
            double dy = mousePoint.Y - WindowDragData.lastHandPoint.Y;
           
            Point goal = new Point(physWindow.topLeft.X + dx, physWindow.topLeft.Y + dy);
            physWindow.addGoalPoint(goal);
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

        /// <summary>
        /// Execute un-initialization tasks.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void WindowClosing(object sender, CancelEventArgs e)
        {
            if (null != this.convertStream)
            {
                this.convertStream.SpeechActive = false;
            }

            if (null != this.speechEngine)
            {
                this.speechEngine.SpeechRecognized -= this.SpeechRecognized;
                this.speechEngine.SpeechRecognitionRejected -= this.SpeechRejected;
                this.speechEngine.RecognizeAsyncStop();
            }

            if (null != this.kinectSensor)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        /// <summary>
        /// Handler for recognized speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            processNameMap = new Dictionary<String, IntPtr>();
            Process[] processlist = Process.GetProcesses();
            foreach (Process process in processlist)
            {
                if (!String.IsNullOrEmpty(process.MainWindowTitle))
                {
                    //Console.WriteLine(process.MainWindowTitle);
                    processNameMap.Add(process.MainWindowTitle, process.MainWindowHandle);
                }
            }

            // Speech utterance confidence below which we treat speech as if it hadn't been heard
            const double ConfidenceThreshold = 0.3;

            if (e.Result.Confidence >= ConfidenceThreshold)
            {
                Console.WriteLine(e.Result.Text);
                bool snapOn = false;
                if (e.Result.Text.ToUpper().Contains("SNAP"))
                {
                    snapOn = true;
                    window = IntPtr.Zero;
                    foreach (String key in processNameMap.Keys)
                    {
                        if (key.ToUpper().Contains(e.Result.Words[1].Text.ToUpper()))
                        {
                            window = processNameMap[key];
                        }
                    }
                }
                if (window == IntPtr.Zero)
                {
                    window = Win32.GetForegroundWindow(); 
                    foreach (String key in processNameMap.Keys)
                    {
                        Console.WriteLine(key);
                    }
                }
                if (snapOn)
                {
                    physWindow = null;
                    Rect workArea = System.Windows.SystemParameters.WorkArea;
                    if (e.Result.Text.ToUpper().Contains("LEFT"))
                    {
                        Win32.SetForegroundWindow(window);
                        Win32.ShowWindow(window, Win32.SW_RESTORE);
                        Win32.SetWindowPos(window, new IntPtr(0), 0, 0, (int)workArea.Width / 2, (int)workArea.Height, Win32.SetWindowPosFlags.SWP_SHOWWINDOW);
                    }
                    if (e.Result.Text.ToUpper().Contains("RIGHT"))
                    {
                        Win32.SetForegroundWindow(window);
                        Win32.ShowWindow(window, Win32.SW_RESTORE);
                        Win32.SetWindowPos(window, new IntPtr(0), (int)workArea.Width / 2, 0, (int)workArea.Width / 2, (int)workArea.Height, Win32.SetWindowPosFlags.SWP_SHOWWINDOW);
                    }
                    if (e.Result.Text.ToUpper().Contains("UP"))
                    {
                        Win32.SetForegroundWindow(window);
                        Win32.ShowWindow(window, Win32.SW_RESTORE);
                        Win32.SetWindowPos(window, new IntPtr(0), 0, 0, (int)workArea.Width, (int)workArea.Height, Win32.SetWindowPosFlags.SWP_SHOWWINDOW);
                    }
                    if (e.Result.Text.ToUpper().Contains("DOWN"))
                    {
                        Win32.ShowWindow(window, Win32.SW_MINIMIZE);
                    }
                }

                if (e.Result.Words[e.Result.Words.Count - 1].Text.ToUpper() == "GRAB")
                {
                    window = Win32.GetForegroundWindow();
                    Gesture_DragMove(null, null);
                }
                else if (e.Result.Text.ToUpper().Contains("GRAB"))
                {
                    foreach (String key in processNameMap.Keys)
                    {
                        Rect workArea = System.Windows.SystemParameters.WorkArea;
                        if (key.ToUpper().Contains(e.Result.Words[e.Result.Words.Count - 1].Text.ToUpper()))
                        {
                            window = processNameMap[key];
                            Gesture_DragMove(null, null);
                        }
                    }
                }

                if (e.Result.Text.ToUpper().Contains("DROP IT"))
                {
                    HandMouseState = HandMouseStates.NONE;
                }

                if (e.Result.Text.ToUpper().Contains("MOUSE MODE"))
                {
                    Gesture_MouseMove(null, null);
                }


                if (e.Result.Text.ToUpper().Contains("CLICK"))
                {
                    Gesture_KnockRecognized(null, null);
                    Gesture_KnockPullRecognized(null, null);
                }
                else if (e.Result.Text.ToUpper().Contains("DOUBLE CLICK"))
                {
                    Gesture_PokeRecognized(null, null);
                }
                else if (e.Result.Text.ToUpper().Contains("RIGHT CLICK"))
                {
                    Gesture_SlapRecognized(null, null);
                }


                if (e.Result.Text.ToUpper().Contains("LETS HACK"))
                {
                    Gesture_ShowAll(null, null);
                }
                else if (e.Result.Text.ToUpper().Contains("SHUT IT DOWN"))
                {
                    Gesture_HideAll(null, null);
                }
            }
        }

        /// <summary>
        /// Handler for rejected speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void SpeechRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
        }

        /// <summary>
        /// Gets the metadata for the speech recognizer (acoustic model) most suitable to
        /// process audio from Kinect device.
        /// </summary>
        /// <returns>
        /// RecognizerInfo if found, <code>null</code> otherwise.
        /// </returns>
        private static RecognizerInfo TryGetKinectRecognizer()
        {
            IEnumerable<RecognizerInfo> recognizers;

            // This is required to catch the case when an expected recognizer is not installed.
            // By default - the x86 Speech Runtime is always expected. 
            try
            {
                recognizers = SpeechRecognitionEngine.InstalledRecognizers();
            }
            catch (COMException)
            {
                return null;
            }

            foreach (RecognizerInfo recognizer in recognizers)
            {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) && "en-US".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return recognizer;
                }
            }

            return null;
        }
    }


}
