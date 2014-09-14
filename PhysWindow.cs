using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Microsoft.Samples.Kinect.BodyBasics
{
    public class PhysWindow
    {
        private static double MAX_SPEED = 30;

        /// <summary>
        /// Called when you start a drag sequence
        /// </summary>
        /// <param name="startingRect"></param>
        public PhysWindow(IntPtr window)
        {
            Win32.RECT current;
            Win32.GetWindowRect(window, out current);
            topLeft = new Point(current.left, current.top);
            Console.WriteLine("Initial topleft: " + topLeft);
            this.windowPtr = window;
            width = (current.right - current.left);
            height = (current.bottom - current.top);
        }

        public void addGoalPoint(Point goal)
        {
            velocity = new Vector(goal.X - topLeft.X, goal.Y - topLeft.Y);
            if (velocity.Length > 5)
                velocity.Normalize();
            else
                velocity = new Vector(0, 0);
            velocity *= MAX_SPEED;
        }

        public void setPoint(Point goal)
        {
            topLeft = goal;
            velocity = new Vector(0, 0);
        }

        public void update()
        {
            Win32.RECT current;
            Win32.GetWindowRect(windowPtr, out current);
            width = (current.right - current.left);
            height = (current.bottom - current.top);
            
            topLeft.X += velocity.X;
            topLeft.Y += velocity.Y;
            for (int i = 0; i < MAX_SPEED && Math.Abs(velocity.X) > .01 && velocity.X < 0; velocity.X += .01, i++);
            for (int i = 0; i < MAX_SPEED && Math.Abs(velocity.X) > .01 && velocity.X > 0; velocity.X -= .01, i++);
            for (int i = 0; i < MAX_SPEED && Math.Abs(velocity.Y) > .01 && velocity.Y < 0; velocity.Y += .01, i++);
            for (int i = 0; i < MAX_SPEED && Math.Abs(velocity.Y) > .01 && velocity.Y > 0; velocity.Y -= .01, i++);

            double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;

            if (topLeft.X + width > screenWidth)
                velocity.X *= -1;
            if (topLeft.X < 0)
                velocity.X *= -1;
            if (topLeft.Y + width > screenHeight)
                velocity.Y *= -1;
            if (topLeft.Y < 0)
                velocity.Y *= -1;

            Win32.SetWindowPos(windowPtr, new IntPtr(0), (int)topLeft.X, (int)topLeft.Y, -1, -1, Win32.SetWindowPosFlags.SWP_NOSIZE);
        }

        public Point topLeft;
        private Vector velocity;
        private IntPtr windowPtr;
        private double width;
        private double height;
    }
}
