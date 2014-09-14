using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Microsoft.Samples.Kinect.HackISUName.Gestures
{
    public class WindowDragData
    {
        public static JointType dragHand;
        public static Point lastHandPoint;
        public static bool resetOldHand;

        public static bool checkForFling { get; set; }
    }

    public class WindowDragStart : IGestureSegment
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
                    if (skeleton.HandRightState == HandState.Closed)
                    {
                        WindowDragData.dragHand = JointType.HandRight;
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
                    if (skeleton.HandLeftState == HandState.Closed)
                    {
                        WindowDragData.dragHand = JointType.HandLeft;
                        return GesturePartResult.Succeeded;
                    }
                }
            }

            // Hand dropped
            return GesturePartResult.Failed;
        }
    }

    public class WindowDragMove : IGestureSegment
    {
        /// <summary>
        /// Updates the current gesture.
        /// </summary>
        /// <param name="skeleton">The skeleton.</param>
        /// <returns>A GesturePartResult based on whether the gesture part has been completed.</returns>
        public GesturePartResult Update(Body skeleton)
        {
            // Hand below head
            if (skeleton.Joints[WindowDragData.dragHand].Position.Y < skeleton.Joints[JointType.Head].Position.Y)
            {
                // Hand in center of chest
                if (skeleton.Joints[WindowDragData.dragHand].Position.X < skeleton.Joints[JointType.ShoulderRight].Position.X &&
                    skeleton.Joints[WindowDragData.dragHand].Position.X > skeleton.Joints[JointType.ShoulderLeft].Position.X)
                {
                    // Hand in closed mode
                    if ((WindowDragData.dragHand == JointType.HandLeft && skeleton.HandLeftState == HandState.Closed) ||
                        (WindowDragData.dragHand == JointType.HandRight && skeleton.HandRightState == HandState.Closed))
                    {
                        return GesturePartResult.Succeeded;
                    }
                }
            }

            // Hand dropped
            return GesturePartResult.Failed;
        }
    }

    public class DragFinishedGesture : IGestureSegment
    {
        /// <summary>
        /// Updates the current gesture.
        /// </summary>
        /// <param name="skeleton">The skeleton.</param>
        /// <returns>A GesturePartResult based on whether the gesture part has been completed.</returns>
        public GesturePartResult Update(Body skeleton)
        {
            // Hand in closed mode
            if ((WindowDragData.dragHand == JointType.HandLeft && skeleton.HandLeftState == HandState.Open) ||
                (WindowDragData.dragHand == JointType.HandRight && skeleton.HandRightState == HandState.Open))
            {
                return GesturePartResult.Succeeded;
            }

            // Hand dropped
            return GesturePartResult.Failed;
        }
    }
}
