using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.HackISUName.Gestures
{
    public class VolumeDownStart : IGestureSegment
    {
        /// <summary>
        /// Updates the current gesture.
        /// </summary>
        /// <param name="skeleton">The skeleton.</param>
        /// <returns>A GesturePartResult based on whether the gesture part has been completed.</returns>
        public GesturePartResult Update(Body skeleton)
        {
            // Hand above head
            if (skeleton.Joints[JointType.HandLeft].Position.Y > skeleton.Joints[JointType.ShoulderLeft].Position.Y)
            {
                // Hand in right of shoulder
                if (skeleton.Joints[JointType.HandLeft].Position.X < skeleton.Joints[JointType.ShoulderLeft].Position.X)
                {
                    // Hand in closed mode
                    if (skeleton.HandLeftState == HandState.Lasso)
                    {
                        return GesturePartResult.Succeeded;
                    }
                }
            }

            // Hand dropped
            return GesturePartResult.Failed;
        }
    }

    public class VolumeUpStart : IGestureSegment
    {
        /// <summary>
        /// Updates the current gesture.
        /// </summary>
        /// <param name="skeleton">The skeleton.</param>
        /// <returns>A GesturePartResult based on whether the gesture part has been completed.</returns>
        public GesturePartResult Update(Body skeleton)
        {
            // Hand above head
            if (skeleton.Joints[JointType.HandLeft].Position.Y < skeleton.Joints[JointType.ShoulderLeft].Position.Y)
            {
                // Hand in right of shoulder
                if (skeleton.Joints[JointType.HandLeft].Position.X < skeleton.Joints[JointType.ShoulderLeft].Position.X)
                {
                    // Hand in closed mode
                    if (skeleton.HandLeftState == HandState.Lasso)
                    {
                        return GesturePartResult.Succeeded;
                    }
                }
            }

            // Hand dropped
            return GesturePartResult.Failed;
        }
    }

    public class VolumeFinishGesture : IGestureSegment
    {
        /// <summary>
        /// Updates the current gesture.
        /// </summary>
        /// <param name="skeleton">The skeleton.</param>
        /// <returns>A GesturePartResult based on whether the gesture part has been completed.</returns>
        public GesturePartResult Update(Body skeleton)
        {
            // Hand in closed mode
            if (skeleton.HandLeftState == HandState.Open)
            {
                return GesturePartResult.Succeeded;
            }

            // Hand dropped
            return GesturePartResult.Failed;
        }
    }
}
