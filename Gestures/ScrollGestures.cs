using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Microsoft.Samples.Kinect.BodyBasics.Gestures
{
    public class ScrollData
    {
        public static bool resetOldHand;
    }

    public class ScrollDownStart : IGestureSegment
    {
        /// <summary>
        /// Updates the current gesture.
        /// </summary>
        /// <param name="skeleton">The skeleton.</param>
        /// <returns>A GesturePartResult based on whether the gesture part has been completed.</returns>
        public GesturePartResult Update(Body skeleton)
        {
            // Hand above head
            if (skeleton.Joints[JointType.HandRight].Position.Y > skeleton.Joints[JointType.ShoulderRight].Position.Y)
            {
                // Hand in right of shoulder
                if (skeleton.Joints[JointType.HandRight].Position.X > skeleton.Joints[JointType.ShoulderRight].Position.X)
                {
                    // Hand in closed mode
                    if (skeleton.HandRightState == HandState.Closed)
                    {
                        return GesturePartResult.Succeeded;
                    }
                }
            }

            // Hand dropped
            return GesturePartResult.Failed;
        }
    }

    public class ScrollUpStart : IGestureSegment
    {
        /// <summary>
        /// Updates the current gesture.
        /// </summary>
        /// <param name="skeleton">The skeleton.</param>
        /// <returns>A GesturePartResult based on whether the gesture part has been completed.</returns>
        public GesturePartResult Update(Body skeleton)
        {
            // Hand above head
            if (skeleton.Joints[JointType.HandRight].Position.Y < skeleton.Joints[JointType.ShoulderRight].Position.Y)
            {
                // Hand in right of shoulder
                if (skeleton.Joints[JointType.HandRight].Position.X > skeleton.Joints[JointType.ShoulderRight].Position.X)
                {
                    // Hand in closed mode
                    if (skeleton.HandRightState == HandState.Closed)
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
