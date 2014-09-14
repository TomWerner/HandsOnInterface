using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Microsoft.Samples.Kinect.HackISUName.Gestures
{
    public class MouseMoveData
    {
        public static JointType dragHand;
        public static Point lastHandPoint;
        public static float lastHandZ;
        public static bool resetOldHand;

        public static JointType signalHand;

        public static bool checkForFling { get; set; }
    }

    public class MouseMoveStart : IGestureSegment
    {
        /// <summary>
        /// Updates the current gesture.
        /// </summary>
        /// <param name="skeleton">The skeleton.</param>
        /// <returns>A GesturePartResult based on whether the gesture part has been completed.</returns>
        public GesturePartResult Update(Body skeleton)
        {
            // Hand above head
            if (skeleton.Joints[JointType.HandRight].Position.Y > skeleton.Joints[JointType.Head].Position.Y)
            {
                // Hand in center of chest
                if (skeleton.Joints[JointType.HandRight].Position.X < skeleton.Joints[JointType.ShoulderRight].Position.X &&
                    skeleton.Joints[JointType.HandRight].Position.X > skeleton.Joints[JointType.ShoulderLeft].Position.X)
                {
                    // Hand in closed mode
                    if (skeleton.HandRightState == HandState.Lasso)
                    {
                        MouseMoveData.dragHand = JointType.HandRight;
                        return GesturePartResult.Succeeded;
                    }
                }
            }
            // Hand above head
            if (skeleton.Joints[JointType.HandLeft].Position.Y > skeleton.Joints[JointType.Head].Position.Y)
            {
                // Hand in center of chest
                if (skeleton.Joints[JointType.HandLeft].Position.X < skeleton.Joints[JointType.ShoulderRight].Position.X &&
                    skeleton.Joints[JointType.HandLeft].Position.X > skeleton.Joints[JointType.ShoulderLeft].Position.X)
                {
                    // Hand in closed mode
                    if (skeleton.HandLeftState == HandState.Lasso)
                    {
                        MouseMoveData.dragHand = JointType.HandLeft;
                        return GesturePartResult.Succeeded;
                    }
                }
            }

            // Hand dropped
            return GesturePartResult.Failed;
        }
    }

    public class MouseMove : IGestureSegment
    {
        /// <summary>
        /// Updates the current gesture.
        /// </summary>
        /// <param name="skeleton">The skeleton.</param>
        /// <returns>A GesturePartResult based on whether the gesture part has been completed.</returns>
        public GesturePartResult Update(Body skeleton)
        {
            // Hand below head
            if (skeleton.Joints[MouseMoveData.dragHand].Position.Y < skeleton.Joints[JointType.Head].Position.Y)
            {
                // Hand in center of chest
                if (skeleton.Joints[MouseMoveData.dragHand].Position.X < skeleton.Joints[JointType.ShoulderRight].Position.X &&
                    skeleton.Joints[MouseMoveData.dragHand].Position.X > skeleton.Joints[JointType.ShoulderLeft].Position.X)
                {
                    // Hand in closed mode
                    if ((MouseMoveData.dragHand == JointType.HandLeft && skeleton.HandLeftState == HandState.Lasso) ||
                        (MouseMoveData.dragHand == JointType.HandRight && skeleton.HandRightState == HandState.Lasso))
                    {
                        return GesturePartResult.Succeeded;
                    }
                }
            }

            // Hand dropped
            return GesturePartResult.Failed;
        }
    }

}
