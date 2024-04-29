using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Rhino.Geometry;

using Emgu.CV.Aruco;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV;

using System.IO;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI;
using static Alea.LibDeviceEx;
using System.Windows.Controls.Primitives;
using RobimWPF;
using AForge.Video.DirectShow;
using System.Security.Cryptography;
using Grasshopper.Kernel.Data;

namespace Robim.Grasshopper
{
    public class CVision : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CVision class.
        /// </summary>
        public CVision()
          : base("RobimCV2d", "CV",
              "Connect to USB camera and extract geometric primitives information",
              "Robim", "Vision")
        {
        }

        public static List<int> modes = new List<int>();
        public static int camID;

        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoDevice;
        private double scale;

        public VisCam vsc;


        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("cid", "cid", "camera id", GH_ParamAccess.item);
            pManager[0].Optional = true;

            pManager.AddIntegerParameter("mode", "mode", "detection mode: 0-rectangles, 1-circles, 2-triangles, 3-contours, 4-aruco", GH_ParamAccess.list);
            pManager[1].Optional = true;

            pManager.AddNumberParameter("scale", "sc", "curve scaling (measured)", GH_ParamAccess.item);
            pManager[2].Optional = true;

            pManager.AddBooleanParameter("reset", "r", "reset the component", GH_ParamAccess.item);
            pManager[3].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("cameras", "cams", "cameras list", GH_ParamAccess.list);
            //pManager[0]

            pManager.AddGeometryParameter("output", "out", "detection output dependent on the mode", GH_ParamAccess.tree);
            //pManager[1]
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            #region getdata

            DA.GetData(0, ref camID);
            DA.GetDataList(1, modes);
            DA.GetData(2, ref scale);

            #endregion

            #region getcameras
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

            DA.SetDataList(0, camList);
            #endregion
            /*
            Bitmap testimg = null;

            string folderPath = @"C:\Robim\";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            string imgpath = folderPath + "tempcv.png";
            string imgpathAR = folderPath + "tempcvaruco.png";
            Mat img = CvInvoke.Imread(imgpath, Emgu.CV.CvEnum.ImreadModes.Color);
            Mat arucoimg = DetectArucoDraw(img);
            arucoimg.Save(imgpathAR);

            string imgpathdef = folderPath + "temp.png";
            string imgpathgeo = folderPath + "tempgeo.png";
            Mat imgdef = CvInvoke.Imread(imgpathdef, Emgu.CV.CvEnum.ImreadModes.Color);
            Mat imggeo = UpdImageWithContours(imgdef);
            imggeo.Save(imgpathgeo);*/

            var triangles = VisCam.OutTriangles.ToList();
            var rectangles = VisCam.OutRectangles.ToList();
            var contours = VisCam.OutContours.ToList();
            var circles = VisCam.OutCircles.ToList();

            var suptriangles = FastCenterFilter(triangles, 0.6);
            var suprectangles = FastCenterFilter(rectangles, 0.6);
            var supcontours = FastCenterFilter(contours, 0.6);
            var supcircles = FastCenterFilter(circles, 0.6);

            // detection mode: 0-rectangles, 1-circles, 2-triangles, 3-contours, 4-aruco

            DataTree<Polyline> outtree = new DataTree<Polyline>();
            outtree.AddRange(suprectangles, new GH_Path(0));
            outtree.AddRange(supcircles, new GH_Path(1));
            outtree.AddRange(suptriangles, new GH_Path(2));
            outtree.AddRange(supcontours, new GH_Path(3));

            DA.SetDataTree(1, outtree);


        }

        public List<Polyline> FastCenterFilter(List<Polyline> initlist, double thres)
        {
            thres = 2;
            List<Polyline> result = new List<Polyline>();
            HashSet<Point3d> processedCenters = new HashSet<Point3d>();

            foreach (Polyline p in initlist)
            {
                var amp = AreaMassProperties.Compute(p.ToPolylineCurve());
                Point3d center = amp.Centroid; 
                bool isDuplicate = false;

                foreach (Point3d processedCenter in processedCenters)
                {
                    if (center.DistanceTo(processedCenter) <= thres)
                    {
                        isDuplicate = true;
                        break;
                    }
                }

                if (!isDuplicate)
                {
                    processedCenters.Add(center);
                    result.Add(p);
                }
            }

            return result;
        }


        public List<Polyline> FastNonMaxSuppr(List<Polyline> initlist, double iouthres)
        {
            List<Polyline> result = new List<Polyline>();
            List<int> existingClasses = new List<int>();
            Dictionary<int, IOUpolyline> maxCurvDict = new Dictionary<int, IOUpolyline>();

            foreach (Polyline polyline in initlist)
            {
                // init IOUpolyline obj
                IOUpolyline ioupline = new IOUpolyline();
                ioupline.polyline = polyline;
                IOUpolyline.calcAreaCenter(ioupline);

                bool iouMatch = false;

                foreach (IOUpolyline maxCurv in maxCurvDict.Values)
                {
                    double iou = IOUpolyline.calcIOU(ioupline, maxCurv);

                    // if match
                    if (iou > iouthres)
                    {
                        iouMatch = true;
                        ioupline.pclass = maxCurv.pclass;
                        ioupline.maxAreaInMyClass = ioupline.area > maxCurv.area;
                        maxCurv.maxAreaInMyClass = !ioupline.maxAreaInMyClass;
                    }
                }
                
                if (!iouMatch)
                {
                    int newclass = existingClasses.Count;
                    ioupline.pclass = newclass;
                    ioupline.maxAreaInMyClass = true;
                    maxCurvDict[newclass] = ioupline;
                    existingClasses.Add(newclass);
                }
            }

            // Only add the maximum area polylines to the result
            foreach (int pclass in existingClasses)
            {
                IOUpolyline maxCurv = maxCurvDict[pclass];
                result.Add(maxCurv.polyline);
            }

            return result;
        }


        public List<Polyline> NonMaxSuppr(List<Polyline> initlist, double iouthres)
        {
            List<Polyline> result = new List<Polyline>();
            List<int> existingClasses = new List<int>();
            List<IOUpolyline> iouplines = new List<IOUpolyline>();
            Dictionary<int, IOUpolyline> maxCurvDict = new Dictionary<int, IOUpolyline>();

            // Iterate over each polyline in the input list
            foreach (Polyline polyline in initlist)
            {
                IOUpolyline ioupline = new IOUpolyline();
                ioupline.polyline = polyline;
                IOUpolyline.calcAreaCenter(ioupline);

                if (existingClasses.Count == 0) 
                { 
                    ioupline.pclass = 0;
                    ioupline.maxAreaInMyClass = true;
                    existingClasses.Add(0);
                    maxCurvDict[0] = ioupline;
                }
                else
                {
                    Dictionary<int, IOUpolyline> newDict = new Dictionary<int, IOUpolyline>(maxCurvDict);

                    foreach (IOUpolyline maxCurv in maxCurvDict.Values)
                    {
                        double iou = IOUpolyline.calcIOU(ioupline, maxCurv);

                        if (iou > iouthres)
                        {
                            ioupline.pclass = maxCurv.pclass;
                            ioupline.maxAreaInMyClass = false;

                            if (ioupline.area > maxCurv.area)
                            {
                                // ioupline is our new maxcurv for this class 
                                ioupline.maxAreaInMyClass = true;
                                maxCurv.maxAreaInMyClass = false;
                                newDict[maxCurv.pclass] = ioupline;
                            }
                            maxCurv.maxAreaInMyClass = false;
                        }
                    }

                    maxCurvDict = new Dictionary<int, IOUpolyline>(newDict);

                    // if didn't iou-match with any existing maxcurves
                    if (ioupline.pclass == -1)
                    {
                        // assign new class 
                        int newclass = existingClasses.Count + 1;
                        ioupline.pclass = newclass;
                        ioupline.maxAreaInMyClass = true;
                        maxCurvDict[newclass] = ioupline;
                    }


                }

                iouplines.Add(ioupline);
            }
            var maxes = maxCurvDict.Values;
            result = maxes.Select(m => m.polyline).ToList();

            return result;
        
        }



        public override void CreateAttributes()
        {
            m_attributes = new Attributes_Custom(this);
        }

        public class Attributes_Custom : GH_ComponentAttributes
        {
            public Attributes_Custom(GH_Component owner) : base(owner) { }

            protected override void Layout()
            {
                base.Layout();

                System.Drawing.Rectangle rec0 = GH_Convert.ToRectangle(Bounds);
                rec0.Height += 22;

                System.Drawing.Rectangle rec1 = rec0;
                rec1.Y = rec1.Bottom - 22;
                rec1.Height = 22;
                rec1.Inflate(-2, -2);

                Bounds = rec0;
                ButtonBounds = rec1;
            }
            private System.Drawing.Rectangle ButtonBounds { get; set; }

            protected override void Render(GH_Canvas canvas, System.Drawing.Graphics graphics, GH_CanvasChannel channel)
            {
                base.Render(canvas, graphics, channel);

                if (channel == GH_CanvasChannel.Objects)
                {
                    GH_Capsule button = GH_Capsule.CreateTextCapsule(ButtonBounds, ButtonBounds, GH_Palette.Black, "Camera", 2, 0);
                    button.Render(graphics, Selected, Owner.Locked, false);
                    button.Dispose();
                }
            }
            public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    System.Drawing.RectangleF rec = ButtonBounds;
                    if (rec.Contains(e.CanvasLocation))
                    {
                        var viscam = new VisCam();
                        viscam.Modes = modes;
                        viscam.CamID = camID;
                        viscam.Show();
                        return GH_ObjectResponse.Handled;
                    }
                }
                return base.RespondToMouseDown(sender, e);
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources._2d;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("FFE96050-4202-442D-BB2A-754C02D9343C"); }
        }




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
                        if (CvInvoke.ContourArea(approxContour, false) > dheight/5  //REWRITE PORTION OF IMG HEIGHT
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


    }

    public class IOUpolyline
    {
        public Polyline polyline;
        public int pclass;
        public double area;
        public bool maxAreaInMyClass;
        public Point3d center;

        public static void calcAreaCenter(IOUpolyline pline)
        {
            var amp = AreaMassProperties.Compute(pline.polyline.ToPolylineCurve());
            pline.area = amp.Area;
            pline.center = amp.Centroid;
        }

        public static double calcIOU (IOUpolyline p1, IOUpolyline p2)
        {
            var intersectionCrvs = Curve.CreateBooleanIntersection(p1.polyline.ToPolylineCurve(), p2.polyline.ToPolylineCurve(), 0.1);
            var unionCrvs = Curve.CreateBooleanUnion(new List<PolylineCurve>() { p1.polyline.ToPolylineCurve(), p2.polyline.ToPolylineCurve() }, 0.1);

            var ampIC = AreaMassProperties.Compute(Curve.JoinCurves(intersectionCrvs));
            var ampUC = AreaMassProperties.Compute(Curve.JoinCurves(unionCrvs));

            if (ampIC != null && ampUC != null) 
            { 
                return ampIC.Area / ampUC.Area; 
            } 
            else return 0;
            
        }
    }
}