using Microsoft.Kinect;
using Microsoft.Samples.Kinect.HackISUName.Gestures;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.HackISUName
{
    public class KnockGestureData
    {
        public static double startDistance = 0.0;
        public static double endDistance = 0.0;
    }
    public class KnockSegment1 : IGestureSegment
    {
        /// <summary>
        /// Updates the current gesture.
        /// </summary>
        /// <param name="skeleton">The skeleton.</param>
        /// <returns>A GesturePartResult based on whether the gesture part has been completed.</returns>
        public GesturePartResult Update(Body skeleton)
        {
            JointType signalElbow;
            JointType signalShoulder;
            HandState handState;
            if (MouseMoveData.signalHand == JointType.HandRight)
            {
                signalElbow = JointType.ElbowRight;
                signalShoulder = JointType.ShoulderRight;
                handState = skeleton.HandRightState;
            }
            else
            {
                signalElbow = JointType.ElbowLeft;
                signalShoulder = JointType.ShoulderLeft;
                handState = skeleton.HandLeftState;
            }

            // Hand above elbow
            if (skeleton.Joints[MouseMoveData.signalHand].Position.Y > skeleton.Joints[signalElbow].Position.Y && handState == HandState.Closed)
            {
                KnockGestureData.startDistance = Math.Sqrt(Math.Pow(skeleton.Joints[MouseMoveData.signalHand].Position.X - skeleton.Joints[signalShoulder].Position.X, 2) +
                                       Math.Pow(skeleton.Joints[MouseMoveData.signalHand].Position.Y - skeleton.Joints[signalShoulder].Position.Y, 2) +
                                       Math.Pow(skeleton.Joints[MouseMoveData.signalHand].Position.Z - skeleton.Joints[signalShoulder].Position.Z, 2));
                return GesturePartResult.Succeeded;
            }

            // Hand low
            return GesturePartResult.Failed;
        }
    }

    public class KnockSegment2 : IGestureSegment
    {
        /// <summary>
        /// Updates the current gesture.
        /// </summary>
        /// <param name="skeleton">The skeleton.</param>
        /// <returns>A GesturePartResult based on whether the gesture part has been completed.</returns>
        public GesturePartResult Update(Body skeleton)
        {
            JointType signalElbow;
            JointType signalShoulder;
            HandState handState;
            if (MouseMoveData.signalHand == JointType.HandRight)
            {
                signalElbow = JointType.ElbowRight;
                signalShoulder = JointType.ShoulderRight;
                handState = skeleton.HandRightState;
            }
            else
            {
                signalElbow = JointType.ElbowLeft;
                signalShoulder = JointType.ShoulderLeft;
                handState = skeleton.HandLeftState;
            }

            // Hand above elbow
            if (skeleton.Joints[MouseMoveData.signalHand].Position.Y > skeleton.Joints[signalElbow].Position.Y && handState == HandState.Closed)
            {
                KnockGestureData.endDistance = Math.Sqrt(Math.Pow(skeleton.Joints[MouseMoveData.signalHand].Position.X - skeleton.Joints[signalShoulder].Position.X, 2) +
                                       Math.Pow(skeleton.Joints[MouseMoveData.signalHand].Position.Y - skeleton.Joints[signalShoulder].Position.Y, 2) +
                                       Math.Pow(skeleton.Joints[MouseMoveData.signalHand].Position.Z - skeleton.Joints[signalShoulder].Position.Z, 2));
                if (KnockGestureData.endDistance - KnockGestureData.startDistance > KnockGestureData.startDistance / 12 )
                {
                    return GesturePartResult.Succeeded;
                }
            }

            // Hand dropped
            return GesturePartResult.Failed;
        }
    }

    public class KnockSegment3 : IGestureSegment
    {
        /// <summary>
        /// Updates the current gesture.
        /// </summary>
        /// <param name="skeleton">The skeleton.</param>
        /// <returns>A GesturePartResult based on whether the gesture part has been completed.</returns>
        public GesturePartResult Update(Body skeleton)
        {
            JointType signalElbow;
            JointType signalShoulder;
            HandState handState;
            if (MouseMoveData.signalHand == JointType.HandRight)
            {
                signalElbow = JointType.ElbowRight;
                signalShoulder = JointType.ShoulderRight;
                handState = skeleton.HandRightState;
            }
            else
            {
                signalElbow = JointType.ElbowLeft;
                signalShoulder = JointType.ShoulderLeft;
                handState = skeleton.HandLeftState;
            }

            // Hand above elbow
            if (skeleton.Joints[MouseMoveData.signalHand].Position.Y > skeleton.Joints[signalElbow].Position.Y && handState == HandState.Closed)
            {
                KnockGestureData.endDistance = Math.Sqrt(Math.Pow(skeleton.Joints[MouseMoveData.signalHand].Position.X - skeleton.Joints[signalShoulder].Position.X, 2) +
                                       Math.Pow(skeleton.Joints[MouseMoveData.signalHand].Position.Y - skeleton.Joints[signalShoulder].Position.Y, 2) +
                                       Math.Pow(skeleton.Joints[MouseMoveData.signalHand].Position.Z - skeleton.Joints[signalShoulder].Position.Z, 2));
                if (KnockGestureData.endDistance - KnockGestureData.startDistance < KnockGestureData.startDistance / -12)
                {
                    return GesturePartResult.Succeeded;
                }
            }

            // Hand dropped
            return GesturePartResult.Failed;
        }
    }
}