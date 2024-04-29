using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using System.Windows.Forms.Integration;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Drawing;

using AForge.Video.DirectShow;
using AForge.Math.Geometry;
using System.Collections;
using System.Windows.Media.Media3D;

using Emgu.CV.Aruco;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV;

using Grasshopper;
using Rhino.Geometry;

namespace RobimWPF
{
    /// <summary>
    /// Interaction logic for VisCam.xaml
    /// </summary>
    public partial class VisCam : Window
    {

        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoDevice1;
        //private VideoCaptureDevice videoDevice2;
        private VideoCapabilities[] snapshotCapabilities;
        private bool viewSwitch = false;
        private double aspectRatio = 0.0;
        private double anglerect;

        #region props
        // shared with CVision
        private List<int> modes;
        private int camID;

        private static List<Rhino.Geometry.Polyline> outcontours = new List<Rhino.Geometry.Polyline> ();
        private static List<Rhino.Geometry.Polyline> outcircles = new List<Rhino.Geometry.Polyline>();
        private static List<Rhino.Geometry.Polyline> outtrigs = new List<Rhino.Geometry.Polyline>();
        private static List<Rhino.Geometry.Polyline> outrects = new List<Rhino.Geometry.Polyline>();

        public static List<Rhino.Geometry.Polyline> OutContours
        {
            get { return outcontours; }
        }

        public static List<Rhino.Geometry.Polyline> OutCircles
        {
            get { return outcircles; }
        }

        public static List<Rhino.Geometry.Polyline> OutTriangles
        {
            get { return outtrigs; }
        }

        public static List<Rhino.Geometry.Polyline> OutRectangles
        {
            get { return outrects; }
        }
        /*

        public delegate void OutcontoursUpdatedEventHandler(List<Rhino.Geometry.Polyline> outcontoursUpd);
        public event OutcontoursUpdatedEventHandler OutcontoursUpdated;

        public List<Rhino.Geometry.Polyline> Outcontours
        {
            get { return outcontours; }
            set 
            { 
                outcontours = value;
                OutcontoursUpdated?.Invoke(outcontours);
            }
        }

        public delegate void OutcirclesUpdatedEventHandler(List<Rhino.Geometry.Polyline> outcirclesUpd);
        public event OutcirclesUpdatedEventHandler OutcirclesUpdated;

        public List<Rhino.Geometry.Polyline> Outcircles
        {
            get { return outcircles; }
            set
            {
                outcircles = value;
                OutcirclesUpdated?.Invoke(outcircles);
            }
        }

        public delegate void OuttrigsUpdatedEventHandler(List<Rhino.Geometry.Polyline> outtrigsUpd);
        public event OuttrigsUpdatedEventHandler OuttrigsUpdated;

        public List<Rhino.Geometry.Polyline> Outtrigs
        {
            get { return outtrigs; }
            set
            {
                outtrigs = value;
                OuttrigsUpdated?.Invoke(outtrigs);
            }
        }

        public delegate void OutrectsUpdatedEventHandler(List<Rhino.Geometry.Polyline>outrectsUpd);
        public event OutrectsUpdatedEventHandler OutrectsUpdated;

        public List<Rhino.Geometry.Polyline> Outrects
        {
            get { return outrects; }
            set
            {
                outrects = value;
                OutrectsUpdated?.Invoke(outrects);
            }
        }

        public class ListUpdatedEventAggregator
        {
            public delegate void ListsUpdatedEventHandler();
            public event ListsUpdatedEventHandler ListsUpdated;

            public void OnOutcontoursUpdated() => ListsUpdated?.Invoke();
            public void OnOutcirclesUpdated() => ListsUpdated?.Invoke();
            public void OnOuttrigsUpdated() => ListsUpdated?.Invoke();
            public void OnOutrectsUpdated() => ListsUpdated?.Invoke();
        }*/

        public List<int> Modes
        {
            get { return modes; }
            set { modes = value; }
        }

        public int CamID
        {
            get { return camID; }
            set { camID = value; }
        }

        #endregion

        public VisCam()
        {
            InitializeComponent();
            aspectRatio = this.Width / this.Height;

            this.SizeChanged += Window_SizeChanged;

            List<string> camList = new List<string>();

            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (videoDevices.Count != 0)
            {
                // add all devices to combo
                foreach (FilterInfo device in videoDevices)
                {
                    camList.Add(device.Name);
                }
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
            {
                this.Height = this.Width / aspectRatio;
            }
            else
            {
                this.Width = this.Height * aspectRatio;
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            videoDevice1 = new VideoCaptureDevice(videoDevices[camID].MonikerString);
            VideoSourcePlayer1.VideoSource = videoDevice1;
            videoDevice1.Start();
            VideoSourcePlayer1.NewFrame += video_NewFrame;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopVideo(videoDevice1);
        }

        public void StopVideo(VideoCaptureDevice vd)
        {
            if (vd != null && vd.IsRunning)
            {
                vd.SignalToStop();
                vd.WaitForStop();
            }
        }

        void video_NewFrame(object sender, ref System.Drawing.Bitmap image)
        {
            #region savetofile

            string folderPath = @"C:\Robim\";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            string imgpath = folderPath + "streaming.png";
            image.Save(imgpath);
            Mat img = CvInvoke.Imread(imgpath, Emgu.CV.CvEnum.ImreadModes.Color);

            string savepath = folderPath + "streamdet.png";
            #endregion


            if (this.Modes.Contains(0) || this.Modes.Contains(1) || this.Modes.Contains(2)|| this.Modes.Contains(3))   // geometry
            {
                img = GetRectanglesTrianglesCircles(img, System.Drawing.Color.GreenYellow);
            }

            if(this.Modes.Contains(4))   // aruco
            {
                img = DetectArucoDraw(img);
            }

            CvInvoke.Imwrite(savepath, img);

            System.Drawing.Bitmap newimage;
            byte[] buff = System.IO.File.ReadAllBytes(savepath);
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream(buff))
            {
                newimage = new System.Drawing.Bitmap(ms);
            }
            image = newimage;
        }


        #region ocvmethods

        public Mat DetectArucoDraw(Mat img)
        {

            //int markersLength = 100;

            #region Initialize Aruco parameters for markers detection
            DetectorParameters ArucoParameters = new DetectorParameters();
            ArucoParameters = DetectorParameters.GetDefault();
            #endregion

            Dictionary ArucoDict = new Dictionary(Dictionary.PredefinedDictionaryName.Dict4X4_100); // bits x bits (per marker) _ number of markers in dict

            #region Detect markers on last retrieved frame
            VectorOfInt ids = new VectorOfInt(); // name/id of the detected markers
            VectorOfVectorOfPointF corners = new VectorOfVectorOfPointF(); // corners of the detected marker
            var what = corners.ToArrayOfArray().Length;
            VectorOfVectorOfPointF rejected = new VectorOfVectorOfPointF(); // rejected contours
            ArucoInvoke.DetectMarkers(img, ArucoDict, corners, ids, ArucoParameters, rejected);
            what = corners.ToArrayOfArray().Length;
            if (corners.ToArrayOfArray().Length > 0)
            {
                List<Point3d> cs = new List<Point3d>();
                cs.Add(new Point3d(corners[0][0].X, corners[0][0].Y, 0));
                cs.Add(new Point3d(corners[0][1].X, corners[0][1].Y, 0));
                cs.Add(new Point3d(corners[0][2].X, corners[0][2].Y, 0));
                cs.Add(new Point3d(corners[0][3].X, corners[0][3].Y, 0));
                //EmguSFBuilder.qrcorners = cs;
            }
            #endregion

            //Mat cameraMatrix = new Mat(new System.Drawing.Size(3, 3), DepthType.Cv32F, 1);
            //Mat distortionMatrix = new Mat(1, 8, DepthType.Cv32F, 1);

            if (ids.Size > 0)
            {
                #region Draw detected markers
                ArucoInvoke.DrawDetectedMarkers(img, corners, ids, new MCvScalar(255, 0, 255));
                #endregion

                #region Estimate pose for each marker using camera calibration matrix and distortion coefficents
                //Mat rvecs = new Mat(); // rotation vector
                //Mat tvecs = new Mat(); // translation vector
                //ArucoInvoke.EstimatePoseSingleMarkers(corners, markersLength, cameraMatrix, distortionMatrix, rvecs, tvecs);
                //EmguSFBuilder.qrcorners = corners;
                #endregion

            }
            return img;

        }


        public Mat UpdImageWithContours(Mat img)
        {

            UMat gray = new UMat();
            UMat cannyEdges = new UMat();
            Mat triangleRectangleImage = new Mat(img.Size, DepthType.Cv8U, 3); //image to draw triangles and rectangles on
            Mat circleImage = new Mat(img.Size, DepthType.Cv8U, 3); //image to draw circles on
            Mat lineImage = new Mat(img.Size, DepthType.Cv8U, 3); //image to draw lines on

            #region preprocess
            //Convert the image to grayscale and filter out the noise
            CvInvoke.CvtColor(img, gray, ColorConversion.Bgr2Gray);

            //Remove noise
            CvInvoke.GaussianBlur(gray, gray, new System.Drawing.Size(3, 3), 1);
            #endregion

            #region circle detection
            double cannyThreshold = 180.0;
            double circleAccumulatorThreshold = 120;
            CircleF[] circles = CvInvoke.HoughCircles(gray, HoughModes.Gradient, 2.0, 20.0, cannyThreshold,
                circleAccumulatorThreshold, 5);
            #endregion

            #region Canny and edge detection
            double cannyThresholdLinking = 120.0;
            CvInvoke.Canny(gray, cannyEdges, cannyThreshold, cannyThresholdLinking);
            LineSegment2D[] lines = CvInvoke.HoughLinesP(
                cannyEdges,
                1, //Distance resolution in pixel-related units
                Math.PI / 45.0, //Angle resolution measured in radians.
                20, //threshold
                30, //min Line width
                10); //gap between lines
            #endregion



            UMat dilated = new UMat();
            Mat element = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Cross, new System.Drawing.Size(3, 3), new System.Drawing.Point(-1, -1));
            CvInvoke.Dilate(cannyEdges, dilated, element, new System.Drawing.Point(-1, -1), 4, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar(0, 0, 0));


            var dheight = dilated.Size.Height;

            #region Find triangles and rectangles
            List<Triangle2DF> triangleList = new List<Triangle2DF>();
            List<RotatedRect> boxList = new List<RotatedRect>(); //a box is a rotated rectangle
            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            {
                CvInvoke.FindContours(dilated, contours, null, RetrType.List,
                    ChainApproxMethod.ChainApproxSimple);
                int count = contours.Size;
                for (int i = 0; i < count; i++)
                {
                    using (VectorOfPoint contour = contours[i])
                    using (VectorOfPoint approxContour = new VectorOfPoint())
                    {
                        CvInvoke.ApproxPolyDP(contour, approxContour, CvInvoke.ArcLength(contour, true) * 0.05,
                            true);
                        var debugArea = CvInvoke.ContourArea(approxContour, false);
                        if (CvInvoke.ContourArea(approxContour, false) > dheight / 5  //REWRITE PORTION OF IMG HEIGHT
                        ) //only consider contours with area greater than 350
                        {
                            if (approxContour.Size == 4) //The contour has 4 vertices.
                            {
                                #region determine if all the angles in the contour are within [80, 100] degree
                                bool isRectangle = true;
                                System.Drawing.Point[] pts = approxContour.ToArray();
                                LineSegment2D[] edges = Emgu.CV.PointCollection.PolyLine(pts, true);

                                for (int j = 0; j < edges.Length; j++)
                                {
                                    double angle = Math.Abs(
                                        edges[(j + 1) % edges.Length].GetExteriorAngleDegree(edges[j]));
                                    if (angle < 80 || angle > 100)
                                    {
                                        isRectangle = false;
                                        break;
                                    }
                                }

                                #endregion

                                if (isRectangle) boxList.Add(CvInvoke.MinAreaRect(approxContour));
                            }
                        }
                    }
                }
            }
            #endregion







            #region draw lines
            //lineImage.SetTo(new MCvScalar(0));
            foreach (LineSegment2D line in lines)
                CvInvoke.Line(lineImage, line.P1, line.P2, new Bgr(System.Drawing.Color.Red).MCvScalar, 1);
            #endregion


            return img;
        }

        // detection mode: 0-rectangles, 1-circles, 2-triangles, 3-contours, 4-aruco
        public Mat GetRectanglesTrianglesCircles(Mat img, System.Drawing.Color bcolor)
        {
            Mat imgfordraw = img.Clone();
            UMat gray = new UMat();
            UMat cannyEdges = new UMat();
            Mat rectangleImage = new Mat(img.Size, DepthType.Cv8U, 3); //image to draw triangles and rectangles on

            //Convert the image to grayscale and filter out the noise
            CvInvoke.CvtColor(img, gray, ColorConversion.Bgr2Gray);

            //Remove noise
            CvInvoke.GaussianBlur(gray, gray, new System.Drawing.Size(3, 3), 1);


            #region Canny and edge and circles detection
            double cannyThreshold = 180.0;
            double cannyThresholdLinking = 120.0;
            double circleAccumulatorThreshold = 120;
           
            //                               (IInputArray image, IOutputArray circles, HoughModes method, double dp, double minDist, double param1 = 100.0, double param2 = 100.0, int minRadius = 0, int maxRadius = 0)
            CircleF[] circles = CvInvoke.HoughCircles(gray, HoughModes.Gradient, 1.3 /* dp */, 40.0 /* minDist */, 200, 100, gray.Size.Width / 30, gray.Size.Width / 4) ;

            // if circles mode
            if (this.Modes.Contains(1))
            {

                //imgfordraw.SetTo(new MCvScalar(0));
                foreach (CircleF circle in circles)
                    CvInvoke.Circle(imgfordraw, System.Drawing.Point.Round(circle.Center), (int)circle.Radius, new Bgr(System.Drawing.Color.Brown).MCvScalar, 2);
            }

            CvInvoke.Canny(gray, cannyEdges, cannyThreshold, cannyThresholdLinking);
            LineSegment2D[] lines = CvInvoke.HoughLinesP(
                cannyEdges,
                1, //Distance resolution in pixel-related units
                Math.PI / 45.0, //Angle resolution measured in radians.
                20, //threshold
                30, //min Line width
                10); //gap between lines
            #endregion

            UMat dilated = new UMat();
            UMat eroded = new UMat();
            Mat element = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Cross, new System.Drawing.Size(3, 3), new System.Drawing.Point(-1, -1));
            CvInvoke.Dilate(cannyEdges, dilated, element, new System.Drawing.Point(-1, -1), 4, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar(0, 0, 0));
            CvInvoke.Erode(dilated, eroded, element, new System.Drawing.Point(-1, -1), 4, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar(0, 0, 0));

            
            string imgpathcan = System.IO.Path.Combine(@"C:\Robim\"  + "canny.png");
            CvInvoke.Imwrite(imgpathcan, cannyEdges);

            string imgpathdil = System.IO.Path.Combine(@"C:\Robim\"  + "dilate.png");
            CvInvoke.Imwrite(imgpathdil, dilated);

            string imgpather = System.IO.Path.Combine(@"C:\Robim\" + "erode.png");
            CvInvoke.Imwrite(imgpather, eroded);

            List<System.Drawing.Point[]> drawContours = new List<System.Drawing.Point[]>();

            #region Find triangles and rectangles
            List<Triangle2DF> triangleList = new List<Triangle2DF>();
            List<RotatedRect> boxList = new List<RotatedRect>(); //a box is a rotated rectangle
            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            {
                CvInvoke.FindContours(eroded, contours, null, RetrType.List,
                    ChainApproxMethod.ChainApproxNone);
                int count = contours.Size;

                outcontours = new List<Rhino.Geometry.Polyline>();
                outcircles = new List<Rhino.Geometry.Polyline>();
                outtrigs = new List<Rhino.Geometry.Polyline>();
                outrects = new List<Rhino.Geometry.Polyline>();

                for (int i = 0; i < count; i++)
                {
                    using (VectorOfPoint contour = contours[i])
                    using (VectorOfPoint approxContour = new VectorOfPoint())
                    using (VectorOfPoint approxPreciseContour = new VectorOfPoint())
                    {
                        CvInvoke.ApproxPolyDP(contour, approxContour, CvInvoke.ArcLength(contour, true) * 0.03,
                            true);
                        CvInvoke.ApproxPolyDP(contour, approxPreciseContour, CvInvoke.ArcLength(contour, true) * 0.001,
                            true);
                        if (CvInvoke.ContourArea(approxContour, false) > 3500  //CHECK
                        ) //only consider contours with area greater than 350
                        {
                            if (approxContour.Size == 3) //The contour has 3 vertices, it is a triangle
                            {
                                System.Drawing.Point[] pts = approxContour.ToArray();
                                triangleList.Add(new Triangle2DF(
                                    pts[0],
                                    pts[1],
                                    pts[2]
                                ));
                                outtrigs.Add(ToPline(pts));
                            }

                            else if (approxContour.Size == 4) //The contour has 4 vertices.
                            {
                                #region determine if all the angles in the contour are within [80, 100] degree
                                bool isRectangle = true;
                                System.Drawing.Point[] pts = approxContour.ToArray();
                                LineSegment2D[] edges = Emgu.CV.PointCollection.PolyLine(pts, true);

                                for (int j = 0; j < edges.Length; j++)
                                {
                                    double angle = Math.Abs(
                                        edges[(j + 1) % edges.Length].GetExteriorAngleDegree(edges[j]));
                                    if (angle < 80 || angle > 100)
                                    {
                                        isRectangle = false;
                                        break;
                                    }
                                }

                                #endregion
                                outrects.Add(ToPline(pts));
                                if (isRectangle) boxList.Add(CvInvoke.MinAreaRect(approxContour));
                            }

                            else
                            {
                                System.Drawing.Point[] pts = approxPreciseContour.ToArray();
                                drawContours.Add(pts);
                                outcontours.Add(ToPline(pts));
                            }
                        }
                    }
                }

            }
            #endregion
            /*
            var box = GetMinBox(boxList);


            List<Point3d> corners = new List<Point3d>();
            CvInvoke.Polylines( img, Array.ConvertAll(box.GetVertices(), System.Drawing.Point.Round), true,
                new Bgr(bcolor).MCvScalar, 2);

            var vertices = box.GetVertices();
            foreach (System.Drawing.PointF v in vertices)
            {
                corners.Add(new Point3d(v.X, v.Y, 0));

            }*/
            /*
            Point3d mid1 = new Point3d((vertices[0].X + vertices[1].X) / 2, (vertices[0].Y + vertices[1].Y) / 2, 0);
            Point3d mid2 = new Point3d((vertices[2].X + vertices[3].X) / 2, (vertices[2].Y + vertices[3].Y) / 2, 0);

            Point3d mid3 = new Point3d((vertices[1].X + vertices[2].X) / 2, (vertices[1].Y + vertices[2].Y) / 2, 0);
            Point3d mid4 = new Point3d((vertices[3].X + vertices[0].X) / 2, (vertices[3].Y + vertices[0].Y) / 2, 0);

            Rhino.Geometry.Line line1 = new Rhino.Geometry.Line(0, 0, 0, 0, 0, 0);
            Rhino.Geometry.Line line2 = new Rhino.Geometry.Line(0, 0, 0, 0, 0, 0);

            if (mid1.X < mid2.X) { line1 = new Rhino.Geometry.Line(mid1, mid2); }
            else { line1 = new Rhino.Geometry.Line(mid2, mid1); }

            if (mid3.X < mid4.X) { line2 = new Rhino.Geometry.Line(mid3, mid4); }
            else { line2 = new Rhino.Geometry.Line(mid4, mid3); }

            Vector3d zerovec = new Vector3d(0, 10, 0);

            if (line1.Length > line2.Length)
            {
                var vec = line1.Direction;
                var anglerad = Vector3d.VectorAngle(vec, zerovec);
                anglerect = anglerad * (180 / Math.PI);
            }
            else
            {
                var vec = line2.Direction;
                var anglerad = Vector3d.VectorAngle(vec, zerovec);
                anglerect = anglerad * (180 / Math.PI);
            }

            if (img.Size.Width < 700)  // smaller camera
            {
                //EmguSFComponent.angleBrick = box.Angle;
                anglerect = box.Center.X;
                anglerect = box.Center.Y;
            }
            */

            #region drawtrianglesrectangles

            //imgfordraw.SetTo(new MCvScalar(0));

            if (this.Modes.Contains(2))  // triangles
            {
                foreach (Triangle2DF triangle in triangleList)
                {
                    CvInvoke.Polylines(imgfordraw, Array.ConvertAll(triangle.GetVertices(), System.Drawing.Point.Round),
                        true, new Bgr(System.Drawing.Color.DarkBlue).MCvScalar, 2);
                }
            }

            

            if (this.Modes.Contains(0)) // rectangles
            {
                foreach (RotatedRect box in boxList)
                {
                    CvInvoke.Polylines(imgfordraw, Array.ConvertAll(box.GetVertices(), System.Drawing.Point.Round), true,
                        new Bgr(System.Drawing.Color.DarkOrange).MCvScalar, 2);
                }
            }


            #endregion

            if (this.Modes.Contains(3)) // contours
            {
                foreach (System.Drawing.Point[] pts in drawContours)
                {
                    CvInvoke.Polylines(imgfordraw, pts, true,
                        new Bgr(System.Drawing.Color.Magenta).MCvScalar, 2);
                }
            }

            return imgfordraw;
        }

        private Rhino.Geometry.Polyline ToPline(System.Drawing.Point[] pts)
        {
            List<Point3d> ghpts = new List<Point3d>();
            foreach (System.Drawing.Point pt in pts)
            {
                ghpts.Add(new Point3d(pt.X, pt.Y, 0));
            }

            // to close the polyline, add the first point again to the end of list
            ghpts.Add(new Point3d(pts[0].X, pts[0].Y, 0));

            return new Rhino.Geometry.Polyline(ghpts);
        }

        private RotatedRect GetMinBox(List<RotatedRect> boxes)
        {
            if (boxes.Any())
            {
                List<RotatedRect> bricks = new List<RotatedRect>();
                foreach (RotatedRect box in boxes)
                {
                    if (box.Size.Width > 60 && box.Size.Width < 160)
                    {
                        bricks.Add(box);
                    }
                }
                if (bricks.Any())
                {
                    var minWidth = bricks.Min(brick => brick.Size.Width);
                    var minBrick = bricks.Where(brick => brick.Size.Width == minWidth).FirstOrDefault();
                    return minBrick;
                }
                else
                {
                    return new RotatedRect();
                }
            }
            else
            {
                return new RotatedRect();
            }
        }

        #endregion
    }
}
