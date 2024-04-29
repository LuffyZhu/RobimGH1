using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Parameters;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using System;
using System.Windows.Forms;
using Robim;
using Robim.Grasshopper;

public class SpecialLinkedAttributes : GH_LinkedParamAttributes
{
    public SpecialLinkedAttributes(IGH_Param param, IGH_Attributes parent) : base(param, parent) { }
    public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
    {
        if (e.Button == MouseButtons.Left)
        {
            Form form = new Form();
            form.Width = 400;
            form.Height = 400;
            form.StartPosition = FormStartPosition.Manual;
            Grasshopper.GUI.GH_WindowsFormUtil.CenterFormOnCursor(form, true);

            ListBox listBox = new ListBox();
            listBox.Text = "Error";
            listBox.Width = 350;
            listBox.Height = 100;
            listBox.Left = 25;
            listBox.Top = 100;
            listBox.Items.Add(CreateProgram.reportwarnings[0]);
            Button optionA = new Button();
            Button optionB = new Button();
            optionA.Click += new EventHandler(OptionAClick);
            optionB.Click += new EventHandler(OptionBClick);
            optionA.Text = "Option A";
            optionB.Text = "Option B";
            optionA.Width = 100;
            optionB.Width = 100;
            optionA.Height = 32;
            optionB.Height = 32;
            optionA.Left = 50;
            optionB.Left = 50;
            optionA.Top = 50;
            optionB.Top = form.ClientSize.Height - optionB.Height - 50;

            form.Controls.Add(optionA);
            form.Controls.Add(optionB);
            form.Controls.Add(listBox);

            form.ShowDialog(sender.FindForm());
            return GH_ObjectResponse.Handled;
        }
        return base.RespondToMouseDoubleClick(sender, e);
    }

    private void OptionAClick(object sender, EventArgs e)
    {
        (sender as Control).FindForm().DialogResult = DialogResult.OK;

        Param_Integer param = Owner as Param_Integer;
        param.RecordUndoEvent("Set Option");
        param.PersistentData.Clear();
        param.PersistentData.Append(new GH_Integer(1));
        param.ExpireSolution(true);
    }
    private void OptionBClick(object sender, EventArgs e)
    {
        (sender as Control).FindForm().DialogResult = DialogResult.OK;

        Param_Integer param = Owner as Param_Integer;
        param.RecordUndoEvent("Set Option");
        param.PersistentData.Clear();
        param.PersistentData.Append(new GH_Integer(2));
        param.ExpireSolution(true);
    }
}

public class SpecialComponent : GH_Component
{
    public SpecialComponent() : base("Special Component", "SpecComp", "Special component showing winforms override", "Robim", "Components") { }
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
        pManager.AddIntegerParameter("Option", "O", "Option parameter", GH_ParamAccess.item, 1);

        if (Attributes == null) CreateAttributes();
        Params.Input[0].Attributes = new SpecialLinkedAttributes(Params.Input[0], this.Attributes);
    }
    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Output", "O", "Output value", GH_ParamAccess.item);
    }
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        int option = 0;
        if (!DA.GetData(0, ref option)) return;

        switch (option)
        {
            case 1:
                DA.SetData(0, "A");
                return;
            case 2:
                DA.SetData(0, "B");
                return;
            default:
                DA.SetData(0, "Unknown");
                return;
        }
    }
    public override Guid ComponentGuid
    {
        get { return new Guid("{DE314B90-AA73-4793-AB1C-051659B486E9}"); }
    }
}