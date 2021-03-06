﻿using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.HackISUName.Gestures
{
    public class PausePlaySegment1 : IGestureSegment
    {
        /// <summary>
        /// Updates the current gesture.
        /// </summary>
        /// <param name="skeleton">The skeleton.</param>
        /// <returns>A GesturePartResult based on whether the gesture part has been completed.</returns>
        public GesturePartResult Update(Body skeleton)
        {
            // Hand to left of shoulder
            if (skeleton.Joints[JointType.HandLeft].Position.X < skeleton.Joints[JointType.ShoulderLeft].Position.X)
            {
                // Hand in closed mode
                if (skeleton.HandLeftState == HandState.Closed)
                {
                    return GesturePartResult.Succeeded;
                }
            }
            // Hand dropped
            return GesturePartResult.Failed;
        }
    }

    public class PausePlaySegment2 : IGestureSegment
    {
        /// <summary>
        /// Updates the current gesture.
        /// </summary>
        /// <param name="skeleton">The skeleton.</param>
        /// <returns>A GesturePartResult based on whether the gesture part has been completed.</returns>
        public GesturePartResult Update(Body skeleton)
        {
            // Hand in between shoulders
            if (skeleton.Joints[JointType.HandLeft].Position.X > skeleton.Joints[JointType.ShoulderLeft].Position.X)
            {
                // Hand in closed mode
                if (skeleton.HandLeftState == HandState.Closed)
                {
                    return GesturePartResult.Succeeded;
                }
            }
            // Hand dropped
            return GesturePartResult.Failed;
        }
    }
}
