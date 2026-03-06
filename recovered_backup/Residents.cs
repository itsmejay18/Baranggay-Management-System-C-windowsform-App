using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace baranggaysystem1;

public class Residents : Form
{
	private IContainer components = null;

	public Residents()
	{
		InitializeComponent();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		this.components = new System.ComponentModel.Container();
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(800, 450);
		this.Text = "Residents";
	}
}
