using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Parameters;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Robim.RobimUI
{
    public class TrackComponent : GH_Component
    {
        int trackLength = 0;
        int robotDomain = 0;
        int robotZeroPos = 0;
        Boolean saved = false;
        public TrackComponent()
          : base("Custom Track", "CTrack",
              "Custom track parameters setup",
              "Robim", "Components")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("Track data", "Track data", "Custom track data", GH_ParamAccess.list);
            pManager.AddMeshParameter("Track geo", "Track geo", "Track mesh geometry", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var border = (trackLength - robotDomain) / 2;
            var minRange = -(robotZeroPos - border);
            var maxRange = robotDomain + minRange;
            List<int> output = new List<int>();
            output.Add(minRange);
            output.Add(maxRange);

            Interval trackL = new Interval(-250, 250);
            Interval trackW = new Interval(-robotZeroPos, trackLength - robotZeroPos);
            Interval trackH = new Interval(-120, 0);

            Rhino.Geometry.Box trackGeo = new Rhino.Geometry.Box(Plane.WorldXY, trackL, trackW, trackH);
            Mesh trackMesh = new Mesh();
            trackMesh = Mesh.CreateFromBox(trackGeo, 5, 5, 5);

            DA.SetDataList(0, output);
            DA.SetData(1, trackMesh);
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("6c8c0e0b-a49b-4667-8d8f-e09cf8aac6d7"); }
        }

        
        TrackUI form;
        public void DisWpf(GH_Canvas sender)
        {
            form = new Robim.RobimUI.TrackUI();
            form.Savebtn.Click += Save_Click;
            form.Closebtn.Click += Close_Click;
            form.Loaded += Load_Values;
            form.Topmost = true;
            form.Show();
        }

        public override void CreateAttributes()
        {
            m_attributes = new ComponentButton(this, "Custom track");
        }

        private void Save_Click(object sender, EventArgs e)
        {   //change to TryParse() later
            trackLength = Convert.ToInt32(form.trackLengthInput.Text);
            robotDomain = Convert.ToInt32(form.robotDomainInput.Text);
            robotZeroPos = Convert.ToInt32(form.zeroPosInput.Text);
            form.Close();
            ExpireSolution(true);
        }

        private void Close_Click(object sender, EventArgs e)
        {
            form.Close();
            ExpireSolution(true);
        }

        private void Load_Values(object sender, EventArgs e)
        {
            form.trackLengthInput.Text = trackLength.ToString();
            form.robotDomainInput.Text = robotDomain.ToString();
            form.zeroPosInput.Text = robotZeroPos.ToString();
        }

        public class ComponentButton : GH_ComponentAttributes
        {
            private bool mouseOver;
            public string buttonName;
            public GH_Component GH_Componenta;
            public ComponentButton(GH_Component owner, string name) : base(owner)
            {
                mouseOver = false;
                buttonName = name;
                GH_Componenta = owner;
            }
            protected override void Layout()
            {
                base.Layout();

                Rectangle rec0 = GH_Convert.ToRectangle(Bounds);
                rec0.Height += 22;
                Rectangle rec1 = rec0;
                rec1.X = rec0.Left + 2;
                rec1.Y = rec0.Bottom - 22;
                rec1.Width = (rec0.Width) - 4;
                rec1.Height = 22;
                rec1.Inflate(-2, -2);
                Bounds = rec0;
                ButtonBounds = rec1;
            }
            private Rectangle ButtonBounds { get; set; }
            public override void ExpireLayout()
            {
                base.ExpireLayout();
                // Destroy any data you have that becomes
                // invalid when the layout expires.
            }
            protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
            {
                base.Render(canvas, graphics, channel);
                // Render the parameter capsule and any additional text on top of it.
                if (channel == GH_CanvasChannel.Objects)
                {
                    GH_Capsule button = GH_Capsule.CreateTextCapsule(ButtonBounds, ButtonBounds, GH_Palette.White, buttonName, 2, 0);
                    button.Render(graphics, Selected, Owner.Locked, false);
                    if (mouseOver)
                    {
                        button.RenderEngine.RenderBackground_Alternative(graphics, Color.FromArgb(200, Color.Gray), false);
                        button.RenderEngine.RenderText(graphics, Color.White);
                    }
                    button.Dispose();
                }
            }
            public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                if (e.Button == MouseButtons.Left && mouseOver)
                {
                    switch (buttonName)
                    {
                        case "Custom track":
                            (Owner as TrackComponent)?.DisWpf(sender);
                            break;
                    }

                    return GH_ObjectResponse.Handled;
                }
                return base.RespondToMouseDown(sender, e);
            }
            public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                System.Drawing.Point pt = GH_Convert.ToPoint(e.CanvasLocation);
                if (e.Button != MouseButtons.None)
                {
                    return base.RespondToMouseMove(sender, e);
                }
                if (ButtonBounds.Contains(pt))
                {
                    if (mouseOver != true)
                    {
                        mouseOver = true;
                        sender.Invalidate();
                    }
                    return GH_ObjectResponse.Capture;
                }
                if (mouseOver != false)
                {
                    mouseOver = false;
                    sender.Invalidate();
                }
                return GH_ObjectResponse.Release;
            }
        }

    }
}